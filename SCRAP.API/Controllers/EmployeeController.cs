using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public EmployeesController(TenantErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin)
                .Select(u => u.Id)
                .ToListAsync();

            var employees = await _db.Set<Employee>().AsNoTracking()
                .Where(x => x.UserId == null || !admins.Contains(x.UserId.Value))
                .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var emp = await _db.Set<Employee>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return emp is null ? NotFound() : Ok(emp);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin)
                .Select(u => u.Id)
                .ToListAsync();

            var query = _db.Set<Employee>().Where(x => x.UserId == null || !admins.Contains(x.UserId.Value));

            var total = await query.CountAsync();
            var active = await query.CountAsync(x => x.Status == EmploymentStatus.Active);
            var byDepartment = await query
                .Where(x => x.Status == EmploymentStatus.Active)
                .GroupBy(x => x.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new { total, active, byDepartment });
        }

        [HttpPost("with-account")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateWithAccount([FromBody] CreateEmployeeWithAccountRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName)) return BadRequest("First name is required.");
            if (string.IsNullOrWhiteSpace(request.LastName)) return BadRequest("Last name is required.");
            if (string.IsNullOrWhiteSpace(request.Position)) return BadRequest("Position is required.");
            if (request.PayRate < 0) return BadRequest("Pay rate cannot be negative.");

            if (request.CreateAccount)
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Username and password are required to create a login account.");

                if (!TryGetRoleFromDepartment(request.Department, out _))
                    return BadRequest("The department does not have an automatic system role mapping.");

                var usernameTaken = await _db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
                if (usernameTaken) return BadRequest($"Username '{request.Username}' is already taken.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var employee = new Employee
                {
                    EmployeeCode = await GenerateNextEmployeeCode(),
                    FirstName = request.FirstName.Trim(),
                    MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
                    LastName = request.LastName.Trim(),
                    ContactNumber = request.ContactNumber,
                    EmailAddress = request.EmailAddress,
                    Street   = request.Street,
                    Barangay = request.Barangay,
                    City     = request.City,
                    Province = request.Province,
                    Country  = request.Country,
                    Position = request.Position.Trim(),
                    Department = request.Department,
                    DateHired = request.DateHired == default ? DateTime.UtcNow : request.DateHired,
                    Status = EmploymentStatus.Active,
                    PayType = request.PayType,
                    PayRate = request.PayRate,
                    Notes = request.Notes
                };

                _db.Add(employee);
                await _db.SaveChangesAsync();

                if (request.CreateAccount)
                {
                    TryGetRoleFromDepartment(request.Department, out var assignedRole);
                    var user = new UserManagement
                    {
                        FirstName = employee.FirstName,
                        MiddleName = employee.MiddleName,
                        LastName = employee.LastName,
                        Username = request.Username!.Trim(),
                        Email = request.AccountEmail ?? request.EmailAddress ?? "",
                        PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password!)),
                        Role = assignedRole,
                        IsActive = true
                    };

                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();

                    employee.UserId = user.Id;
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return CreatedAtAction(nameof(Get), new { id = employee.Id }, employee);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Error creating employee", detail = ex.Message });
            }
        }

        private static bool TryGetRoleFromDepartment(string? department, out UserRole role)
        {
            role = default;
            var value = department?.Trim().ToLowerInvariant();

            if (value is "tech" or "technical" or "technical staff")
            {
                role = UserRole.TechStaff;
                return true;
            }

            if (value is "sales" or "sales staff")
            {
                role = UserRole.SalesStaff;
                return true;
            }

            if (value is "manager" or "management")
            {
                role = UserRole.Manager;
                return true;
            }

            return false;
        }

        private async Task<string> GenerateNextEmployeeCode()
        {
            var codes = await _db.Set<Employee>()
                .Where(e => e.EmployeeCode.StartsWith("EMP-"))
                .Select(e => e.EmployeeCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in codes)
            {
                var numPart = code.Replace("EMP-", "");
                if (int.TryParse(numPart, out var n) && n > max) max = n;
            }

            return $"EMP-{(max + 1):D4}"; // EMP-0001, EMP-0002, ...
        }

        // Deactivate rather than delete — you want employment history kept for audits
        [HttpPost("{id:int}/deactivate")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Deactivate(int id, [FromQuery] EmploymentStatus status = EmploymentStatus.Resigned)
        {
            var existing = await _db.Set<Employee>().FindAsync(id);
            if (existing is null) return NotFound();

            existing.Status = status;

            // Deactivate linked user login account
            if (existing.UserId.HasValue)
            {
                var user = await _db.Users.FindAsync(existing.UserId.Value);
                if (user != null) user.IsActive = false;
            }
            else if (!string.IsNullOrWhiteSpace(existing.EmailAddress))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == existing.EmailAddress.ToLower());
                if (user != null) user.IsActive = false;
            }

            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpPut("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _db.Set<Employee>().FindAsync(id);
            if (employee == null) return NotFound(new { message = "Employee not found." });

            if (string.IsNullOrWhiteSpace(request.FirstName)) return BadRequest("First name is required.");
            if (string.IsNullOrWhiteSpace(request.LastName)) return BadRequest("Last name is required.");
            if (string.IsNullOrWhiteSpace(request.Position)) return BadRequest("Position is required.");
            if (request.PayRate < 0) return BadRequest("Pay rate cannot be negative.");

            employee.FirstName = request.FirstName.Trim();
            employee.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.ContactNumber = request.ContactNumber;
            employee.EmailAddress = request.EmailAddress;
            employee.Street = request.Street;
            employee.Barangay = request.Barangay;
            employee.City = request.City;
            employee.Province = request.Province;
            employee.Country = request.Country;
            employee.Position = request.Position.Trim();
            employee.Department = request.Department;
            employee.PayType = request.PayType;
            employee.PayRate = request.PayRate;
            employee.Status = request.Status;
            employee.Notes = request.Notes;

            // Sync user account active status if employee has a linked login account
            bool shouldBeActive = (request.Status == EmploymentStatus.Active && request.IsActive);
            UserManagement? user = null;
            if (employee.UserId.HasValue)
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee.UserId.Value);
            }
            if (user == null && !string.IsNullOrWhiteSpace(employee.EmailAddress))
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == employee.EmailAddress.ToLower());
                if (user != null) employee.UserId = user.Id;
            }

            if (user != null)
            {
                user.IsActive = shouldBeActive;
            }

            await _db.SaveChangesAsync();
            return Ok(employee);
        }

        [HttpPost("{id:int}/toggle-status")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await _db.Set<Employee>().FindAsync(id);
            if (employee == null) return NotFound(new { message = "Employee not found." });

            bool isCurrentlyActive = (employee.Status == EmploymentStatus.Active);
            employee.Status = isCurrentlyActive ? EmploymentStatus.Resigned : EmploymentStatus.Active;

            // Sync linked login account
            UserManagement? user = null;
            if (employee.UserId.HasValue)
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee.UserId.Value);
            }
            if (user == null && !string.IsNullOrWhiteSpace(employee.EmailAddress))
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == employee.EmailAddress.ToLower());
                if (user != null) employee.UserId = user.Id;
            }

            if (user != null)
            {
                user.IsActive = !isCurrentlyActive;
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                employee.Id,
                employee.EmployeeCode,
                Status = employee.Status.ToString(),
                IsActive = (employee.Status == EmploymentStatus.Active),
                UserAccountDisabled = (user != null && !user.IsActive)
            });
        }

        public class UpdateEmployeeRequest
        {
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string? ContactNumber { get; set; }
            public string? EmailAddress { get; set; }
            public string? Street   { get; set; }
            public string? Barangay { get; set; }
            public string? City     { get; set; }
            public string? Province { get; set; }
            public string? Country  { get; set; }
            public string Position { get; set; } = "";
            public string Department { get; set; } = "";
            public PayType PayType { get; set; }
            public decimal PayRate { get; set; }
            public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
            public bool IsActive { get; set; } = true;
            public string? Notes { get; set; }
        }

        public class CreateEmployeeWithAccountRequest
        {
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string? ContactNumber { get; set; }
            public string? EmailAddress { get; set; }
            public string? Street   { get; set; }
            public string? Barangay { get; set; }
            public string? City     { get; set; }
            public string? Province { get; set; }
            public string? Country  { get; set; }
            public string Position { get; set; } = "";
            public string Department { get; set; } = "";
            public DateTime DateHired { get; set; }
            public PayType PayType { get; set; }
            public decimal PayRate { get; set; }
            public string? Notes { get; set; }

            public bool CreateAccount { get; set; }
            public string? Username { get; set; }
            public string? AccountEmail { get; set; }
            public string? Password { get; set; }
        }
    }
}