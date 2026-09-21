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
        private readonly MasterErpDbContext _db;
        public EmployeesController(MasterErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Set<Employee>().AsNoTracking()
                .OrderBy(x => x.FullName)
                .ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var emp = await _db.Set<Employee>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return emp is null ? NotFound() : Ok(emp);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var total = await _db.Set<Employee>().CountAsync();
            var active = await _db.Set<Employee>().CountAsync(x => x.Status == EmploymentStatus.Active);
            var byDepartment = await _db.Set<Employee>()
                .Where(x => x.Status == EmploymentStatus.Active)
                .GroupBy(x => x.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new { total, active, byDepartment });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (string.IsNullOrWhiteSpace(employee.FullName)) return BadRequest("Full name is required.");
            if (string.IsNullOrWhiteSpace(employee.Position)) return BadRequest("Position is required.");
            if (employee.PayRate < 0) return BadRequest("Pay rate cannot be negative.");

            employee.EmployeeCode = await GenerateNextEmployeeCode();

            if (employee.DateHired == default) employee.DateHired = DateTime.UtcNow;

            _db.Add(employee);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = employee.Id }, employee);
        }

        private async Task<string> GenerateNextEmployeeCode()
        {
            var lastNumber = await _db.Set<Employee>()
                .Where(e => e.EmployeeCode.StartsWith("EMP-"))
                .Select(e => e.EmployeeCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in lastNumber)
            {
                var numPart = code.Replace("EMP-", "");
                if (int.TryParse(numPart, out var n) && n > max) max = n;
            }

            return $"EMP-{(max + 1):D4}"; // EMP-0001, EMP-0002, ...
        }



        // Deactivate rather than delete — you want employment history kept for audits
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromQuery] EmploymentStatus status = EmploymentStatus.Resigned)
        {
            var existing = await _db.Set<Employee>().FindAsync(id);
            if (existing is null) return NotFound();

            existing.Status = status;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }
        [HttpPost("with-account")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateWithAccount([FromBody] CreateEmployeeWithAccountRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("Full name is required.");
            if (string.IsNullOrWhiteSpace(request.Position)) return BadRequest("Position is required.");
            if (request.PayRate < 0) return BadRequest("Pay rate cannot be negative.");

            if (request.CreateAccount)
            {
                if (request.Role is null || request.Role == UserRole.Admin)
                    return BadRequest("A valid non-admin role is required to create a login account.");
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Username and password are required to create a login account.");

                var usernameTaken = await _db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
                if (usernameTaken) return BadRequest($"Username '{request.Username}' is already taken.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var employee = new Employee
                {
                    EmployeeCode = await GenerateNextEmployeeCode(),
                    FullName = request.FullName.Trim(),
                    ContactNumber = request.ContactNumber,
                    EmailAddress = request.EmailAddress,
                    Address = request.Address,
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
                    var nameParts = employee.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstName = nameParts.Length > 0 ? nameParts[0] : employee.FullName;
                    var lastName = nameParts.Length > 1 ? nameParts[^1] : "";
                    var middleName = nameParts.Length > 2 ? string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2)) : null;

                    var user = new UserManagement
                    {
                        FirstName = firstName,
                        MiddleName = middleName,
                        LastName = lastName,
                        Username = request.Username!.Trim(),
                        Email = request.AccountEmail ?? request.EmailAddress ?? "",
                        PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password!)),
                        Role = request.Role!.Value,
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

        public class CreateEmployeeWithAccountRequest
        {
            public string FullName { get; set; } = "";
            public string? ContactNumber { get; set; }
            public string? EmailAddress { get; set; }
            public string? Address { get; set; }
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
            public UserRole? Role { get; set; }
        }
    }
}