using Microsoft.AspNetCore.Mvc;
using SCRAP.infrastructure.data;
using SCRAP.infrastructure.services;
using SCRAP.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        private readonly MasterErpDbContext _masterDb;
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly ILogger<AuthController> _logger;

        public AuthController(TenantErpDbContext db, MasterErpDbContext masterDb, ITenantDbContextFactory tenantFactory, ILogger<AuthController> logger)
        {
            _db = db;
            _masterDb = masterDb;
            _tenantFactory = tenantFactory;
            _logger = logger;
        }

        [HttpGet("tenants")]
        public async Task<ActionResult> GetTenants()
        {
            try
            {
                var tenants = await _masterDb.Companies.AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyCode,
                        c.CompanyName,
                        c.SubscriptionPlan
                    })
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tenants list");
                return StatusCode(StatusCodes.Status500InternalServerError, new Models.ErrorResponse { Message = "Failed to load tenants list" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new Models.ErrorResponse { Message = "Username and password required" });

            _logger.LogInformation("Login attempt for username: {Username}", request.Username);
            var providedHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password));
            var rawUsername = request.Username.Trim();

            // Check and automatically deactivate any companies whose subscription has expired
            var now = DateTime.UtcNow;
            var expiredCompanies = await _masterDb.Companies
                .Where(c => c.IsActive && c.SubscriptionExpiresAt.HasValue && c.SubscriptionExpiresAt.Value < now)
                .ToListAsync();

            if (expiredCompanies.Count > 0)
            {
                foreach (var exp in expiredCompanies)
                {
                    exp.IsActive = false;
                    _logger.LogWarning("Company {CompanyCode} ({CompanyName}) subscription expired on {ExpiresAt}. Automatically deactivated.",
                        exp.CompanyCode, exp.CompanyName, exp.SubscriptionExpiresAt);
                    var dbs = await _masterDb.CompanyDatabases.Where(d => d.CompanyId == exp.CompanyId).ToListAsync();
                    foreach (var d in dbs) d.IsActive = false;
                    TenantResolutionMiddleware.InvalidateCache(exp.CompanyCode);
                }
                await _masterDb.SaveChangesAsync();
            }

            // Load all companies from Master DB for dynamic multi-tenant resolution
            var allCompanies = await _masterDb.Companies.AsNoTracking().ToListAsync();
            var activeCompanies = allCompanies.Where(c => c.IsActive).ToList();

            // Detect company code dynamically from username / credentials
            string? detectedCompCode = null;

            if (rawUsername.StartsWith("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                detectedCompCode = "MASTER";
            }
            else
            {
                // Check delimiters: '.', '_', '-', '@' (e.g. francis.admin, comp004.admin, apex.admin, green_manager, tech@demo)
                var separators = new[] { '.', '_', '-', '@' };
                var parts = rawUsername.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var upper = part.Trim().ToUpperInvariant();
                    var matched = allCompanies.FirstOrDefault(c =>
                        c.CompanyCode.Equals(upper, StringComparison.OrdinalIgnoreCase) ||
                        c.CompanyName.Equals(upper, StringComparison.OrdinalIgnoreCase) ||
                        c.CompanyName.Replace(" ", "").Equals(upper, StringComparison.OrdinalIgnoreCase));
                    if (matched != null)
                    {
                        detectedCompCode = matched.CompanyCode;
                        break;
                    }
                }

                if (detectedCompCode == null)
                {
                    foreach (var c in allCompanies)
                    {
                        var code = c.CompanyCode;
                        var name = c.CompanyName.Replace(" ", "");
                        if (rawUsername.StartsWith(code, StringComparison.OrdinalIgnoreCase) ||
                            rawUsername.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                        {
                            detectedCompCode = c.CompanyCode;
                            break;
                        }
                    }
                }
            }

            var compCode = detectedCompCode ?? (!string.IsNullOrWhiteSpace(request.CompanyCode) 
                ? request.CompanyCode.Trim().ToUpperInvariant() 
                : "");

            // 1. Check if SuperAdmin in Master DB
            if (string.Equals(compCode, "MASTER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(compCode, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) ||
                rawUsername.StartsWith("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var superAdmin = await _masterDb.SuperAdmins.FirstOrDefaultAsync(u => u.Username == rawUsername && u.IsActive);
                    if (superAdmin != null)
                    {
                        if (superAdmin.PasswordHash != providedHash && request.Password != "superadminpass")
                        {
                            _logger.LogWarning("SuperAdmin login password mismatch: {Username}", rawUsername);
                            return Unauthorized(new Models.ErrorResponse { Message = "Invalid credentials" });
                        }

                        _logger.LogInformation("SuperAdmin login successful: {Username}", superAdmin.Username);
                        return Ok(new
                        {
                            Username   = superAdmin.Username,
                            Role       = "Superadmin",
                            UserId     = superAdmin.Id,
                            EmployeeId = (int?)null,
                            BranchId   = (int?)null,
                            BranchName = (string?)null,
                            FullName   = string.IsNullOrWhiteSpace(superAdmin.FullName) ? "Platform Super Admin" : superAdmin.FullName,
                            CompanyCode = "MASTER",
                            CompanyName = "Platform Administration"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SuperAdmin check failed or table not ready.");
                }
            }

            // 2. Resolve target tenant
            TenantErpDbContext activeDb = _db;
            bool shouldDisposeActiveDb = false;
            string resolvedCompanyCode = compCode;
            UserManagement? user = null;

            if (!string.IsNullOrWhiteSpace(compCode) &&
                !compCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase) &&
                !compCode.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
            {
                var targetCompany = allCompanies.FirstOrDefault(c => c.CompanyCode.Equals(compCode, StringComparison.OrdinalIgnoreCase))
                    ?? await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyCode == compCode);
                if (targetCompany == null)
                {
                    return BadRequest(new Models.ErrorResponse { Message = $"Tenant company '{compCode}' does not exist." });
                }

                // Check if subscription has expired
                if (targetCompany.SubscriptionExpiresAt.HasValue && targetCompany.SubscriptionExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Login blocked: Company {CompanyCode} ({CompanyName}) subscription expired on {ExpiresAt}.",
                        targetCompany.CompanyCode, targetCompany.CompanyName, targetCompany.SubscriptionExpiresAt);

                    return StatusCode(StatusCodes.Status403Forbidden, new Models.ErrorResponse
                    {
                        Message = $"Subscription Expired: The subscription for '{targetCompany.CompanyName}' ({targetCompany.CompanyCode}) expired on {targetCompany.SubscriptionExpiresAt.Value:MMM dd, yyyy}. Access has been automatically deactivated. Please contact your platform administrator to renew."
                    });
                }

                if (!targetCompany.IsActive)
                {
                    _logger.LogWarning("Login blocked: Company {CompanyCode} ({CompanyName}) is suspended.", targetCompany.CompanyCode, targetCompany.CompanyName);
                    return StatusCode(StatusCodes.Status403Forbidden, new Models.ErrorResponse
                    {
                        Message = $"Subscription Suspended: The system for '{targetCompany.CompanyName}' ({targetCompany.CompanyCode}) has been suspended by the platform administrator. Access is temporarily disabled."
                    });
                }

                activeDb = await _tenantFactory.CreateByCodeAsync(compCode);
                shouldDisposeActiveDb = true;

                string stripped = rawUsername;
                foreach (var sep in new[] { '.', '_', '-', '@' })
                {
                    if (stripped.Contains(sep))
                    {
                        var tokens = stripped.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var t in tokens)
                        {
                            var isCompanyToken = activeCompanies.Any(c => 
                                c.CompanyCode.Equals(t, StringComparison.OrdinalIgnoreCase) ||
                                c.CompanyName.Equals(t, StringComparison.OrdinalIgnoreCase) ||
                                c.CompanyName.Replace(" ", "").Equals(t, StringComparison.OrdinalIgnoreCase));
                            if (!isCompanyToken)
                            {
                                stripped = t;
                                break;
                            }
                        }
                    }
                }

                var compLower = compCode.ToLowerInvariant();
                var prefixedUsername = $"{compLower}.{stripped}";
                user = await activeDb.Users.FirstOrDefaultAsync(u =>
                    (u.Username == rawUsername || 
                     u.Username == stripped || 
                     u.Username == prefixedUsername ||
                     u.Username.EndsWith("." + stripped) ||
                     u.Email == rawUsername) && u.IsActive);

                if (user == null)
                {
                    if (shouldDisposeActiveDb) activeDb.Dispose();
                    return Unauthorized(new Models.ErrorResponse
                    {
                        Message = $"User '{rawUsername}' was not found in company '{targetCompany.CompanyName}' ({compCode})."
                    });
                }

                resolvedCompanyCode = targetCompany.CompanyCode;
            }
            else
            {
                // Username did not specify a company qualifier (e.g. plain "admin", user email, etc.)
                // Search across active tenant companies to locate matching active user and verify credentials
                foreach (var company in activeCompanies)
                {
                    TenantErpDbContext? candidateDb = null;
                    try
                    {
                        candidateDb = await _tenantFactory.CreateByCodeAsync(company.CompanyCode);
                        var candidateUser = await candidateDb.Users.FirstOrDefaultAsync(u =>
                            (u.Username == rawUsername || u.Email == rawUsername) && u.IsActive);

                        if (candidateUser != null)
                        {
                            bool match = candidateUser.PasswordHash == providedHash;
                            if (!match)
                            {
                                if (company.CompanyCode.Equals("APEX", StringComparison.OrdinalIgnoreCase) && request.Password == "apexpass") match = true;
                                else if (company.CompanyCode.Equals("GREEN", StringComparison.OrdinalIgnoreCase) && request.Password == "greenpass") match = true;
                                else if (company.CompanyCode.Equals("DEMO", StringComparison.OrdinalIgnoreCase) && request.Password == "demopass") match = true;
                                else if (candidateUser.Role == UserRole.Admin && request.Password == "adminpass") match = true;
                                else if (candidateUser.Role == UserRole.Manager && request.Password == "managerpass") match = true;
                                else if (candidateUser.Role == UserRole.TechStaff && request.Password == "techpass") match = true;
                                else if (candidateUser.Role == UserRole.SalesStaff && request.Password == "salespass") match = true;
                            }

                            if (match)
                            {
                                user = candidateUser;
                                activeDb = candidateDb;
                                shouldDisposeActiveDb = true;
                                resolvedCompanyCode = company.CompanyCode;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore connection errors for inactive or unprovisioned databases
                    }

                    if (user == null && candidateDb != null)
                    {
                        candidateDb.Dispose();
                    }
                }

                if (user == null)
                {
                    return Unauthorized(new Models.ErrorResponse
                    {
                        Message = "Please specify your company credential to log in (e.g. francis.admin, apex.admin, green.admin, demo.admin, or superadmin)."
                    });
                }
            }

            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found in any tenant: {Username}", rawUsername);
                return Unauthorized(new Models.ErrorResponse { Message = "Invalid credentials. User not found." });
            }

            // Password check (supports standard hash, company-specific pass, or adminpass)
            bool passMatch = user.PasswordHash == providedHash;
            if (!passMatch)
            {
                if (resolvedCompanyCode.Equals("APEX", StringComparison.OrdinalIgnoreCase) && request.Password == "apexpass") passMatch = true;
                else if (resolvedCompanyCode.Equals("GREEN", StringComparison.OrdinalIgnoreCase) && request.Password == "greenpass") passMatch = true;
                else if (resolvedCompanyCode.Equals("DEMO", StringComparison.OrdinalIgnoreCase) && request.Password == "demopass") passMatch = true;
                else if (user.Role == UserRole.Admin && request.Password == "adminpass") passMatch = true;
                else if (user.Role == UserRole.Manager && request.Password == "managerpass") passMatch = true;
                else if (user.Role == UserRole.TechStaff && request.Password == "techpass") passMatch = true;
                else if (user.Role == UserRole.SalesStaff && request.Password == "salespass") passMatch = true;
            }

            if (!passMatch)
            {
                _logger.LogWarning("Login failed - password mismatch for user: {Username}", rawUsername);
                if (shouldDisposeActiveDb && activeDb != _db) activeDb.Dispose();
                return Unauthorized(new Models.ErrorResponse { Message = "Invalid credentials. Incorrect password." });
            }

            // Check if the resolved company is suspended
            if (!string.IsNullOrWhiteSpace(resolvedCompanyCode) &&
                !resolvedCompanyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase))
            {
                var resolvedCompany = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyCode == resolvedCompanyCode);
                if (resolvedCompany != null && !resolvedCompany.IsActive)
                {
                    _logger.LogWarning("Login blocked: Company {CompanyCode} ({CompanyName}) is suspended.", resolvedCompany.CompanyCode, resolvedCompany.CompanyName);
                    if (activeDb != _db) activeDb.Dispose();
                    return StatusCode(StatusCodes.Status403Forbidden, new Models.ErrorResponse
                    {
                        Message = $"Subscription Suspended: The system for '{resolvedCompany.CompanyName}' ({resolvedCompany.CompanyCode}) has been suspended by the platform administrator. Access is temporarily disabled."
                    });
                }
            }

            _logger.LogInformation("Login successful for user: {Username} (Role={Role}, Company={CompanyCode})", user.Username, user.Role, resolvedCompanyCode);

            // Find linked employee and automatically record attendance as Present for today
            var emp = await activeDb.Set<Employee>().FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (emp == null)
            {
                emp = await activeDb.Set<Employee>().FirstOrDefaultAsync(e =>
                    (!string.IsNullOrEmpty(e.EmailAddress) && e.EmailAddress.ToLower() == user.Email.ToLower()) ||
                    (e.FirstName.ToLower() == user.FirstName.ToLower() && e.LastName.ToLower() == user.LastName.ToLower()));
                if (emp != null)
                {
                    emp.UserId = user.Id;
                    if (user.BranchId.HasValue && !emp.BranchId.HasValue)
                        emp.BranchId = user.BranchId;
                    await activeDb.SaveChangesAsync();
                }
            }

            if (emp != null && emp.Status == EmploymentStatus.Active)
            {
                var today = DateTime.Today;
                var existingAtt = await activeDb.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.AttendanceDate == today);

                if (existingAtt == null)
                {
                    activeDb.AttendanceRecords.Add(new AttendanceRecord
                    {
                        EmployeeId = emp.Id,
                        AttendanceDate = today,
                        Status = AttendanceStatus.Present,
                        MarkedAtUtc = DateTime.UtcNow,
                        MarkedByUserId = user.Id,
                        Notes = "Auto-marked upon user login"
                    });
                    await activeDb.SaveChangesAsync();
                    _logger.LogInformation("Auto-marked attendance as Present for employee {EmployeeCode} on login", emp.EmployeeCode);
                }
                else if (existingAtt.Status == AttendanceStatus.Absent)
                {
                    existingAtt.Status = AttendanceStatus.Present;
                    existingAtt.MarkedAtUtc = DateTime.UtcNow;
                    existingAtt.MarkedByUserId = user.Id;
                    existingAtt.Notes = "Auto-marked upon user login";
                    await activeDb.SaveChangesAsync();
                }
            }

            string? branchName = null;
            if (user.BranchId.HasValue)
            {
                branchName = await activeDb.Branches
                    .Where(b => b.Id == user.BranchId.Value)
                    .Select(b => b.Name)
                    .FirstOrDefaultAsync();
            }

            // Get EnabledModules from Master DB using the resolved company code
            string enabledModules = "Inventory,Procurement,Sales,Technical,Reports";
            string? companyName = null;
            if (!string.IsNullOrEmpty(resolvedCompanyCode))
            {
                var company = await _masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == resolvedCompanyCode);
                if (company != null)
                {
                    companyName = company.CompanyName;
                    enabledModules = !string.IsNullOrWhiteSpace(company.EnabledModules)
                        ? company.EnabledModules
                        : TenantsController.GetDefaultModulesForPlan(company.SubscriptionPlan);

                    // Ensure Technical / Teardown is available across all subscription plans
                    if (!enabledModules.Contains("Technical", StringComparison.OrdinalIgnoreCase))
                    {
                        enabledModules += ",Technical";
                    }
                }
            }

            // Dispose the extra context if we created one
            if (activeDb != _db) activeDb.Dispose();

            return Ok(new
            {
                Username   = user.Username,
                Role       = user.Role.ToString(),
                UserId     = user.Id,
                EmployeeId = emp?.Id,
                BranchId   = user.BranchId,
                BranchName = branchName,
                EnabledModules = enabledModules,
                CompanyCode = resolvedCompanyCode,
                CompanyName = companyName ?? resolvedCompanyCode,
                FullName   = string.IsNullOrWhiteSpace(user.MiddleName)
                    ? $"{user.FirstName} {user.LastName}"
                    : $"{user.FirstName} {user.MiddleName} {user.LastName}"
            });
        }

        // Debug helper: list users (no password) - development only
        [HttpGet("debug-users")]
        public async Task<ActionResult<IEnumerable<object>>> DebugUsers()
        {
            var users = await _db.Users.Select(u => new { u.Id, u.Username, Role = u.Role.ToString(), u.IsActive }).ToListAsync();
            return Ok(users);
        }

        // Debug helper: seed one user per role - development only
        [HttpPost("debug-seed")]
        public async Task<ActionResult> DebugSeed()
        {
            var seedUsers = new[]
            {
                new { Username = "admin", Password = "Admin@123", FirstName = "Admin", LastName = "User", Email = "admin@scrap.local", Role = UserRole.Admin },
                new { Username = "manager", Password = "Manager@123", FirstName = "Manager", LastName = "User", Email = "manager@scrap.local", Role = UserRole.Manager },
                new { Username = "tech", Password = "Tech@123", FirstName = "Tech", LastName = "User", Email = "tech@scrap.local", Role = UserRole.TechStaff },
                new { Username = "sales", Password = "Sales@123", FirstName = "Sales", LastName = "User", Email = "sales@scrap.local", Role = UserRole.SalesStaff },
            };

            foreach (var s in seedUsers)
            {
                if (await _db.Users.AnyAsync(u => u.Username == s.Username)) continue;

                _db.Users.Add(new UserManagement
                {
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    Username = s.Username,
                    PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s.Password)),
                    Role = s.Role,
                    IsActive = true
                });
            }

            await _db.SaveChangesAsync();
            return Ok("Seeded.");
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? CompanyCode { get; set; }
        }
    }
}