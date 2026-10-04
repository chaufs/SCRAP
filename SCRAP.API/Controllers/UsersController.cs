using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.infrastructure.data;
using SCRAP.domain.entities;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        private readonly ILogger<UsersController> _logger;
        public UsersController(TenantErpDbContext db, ILogger<UsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserManagement>>> GetAll() => await _db.Users.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<UserManagement>> Get(int id)
        {
            var item = await _db.Users.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (request.Role == UserRole.Admin)
                return BadRequest(new Models.ErrorResponse { Message = "Admin cannot assign the Admin role." });

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new Models.ErrorResponse { Message = "Username and password are required." });

            var employee = await _db.Set<Employee>().FindAsync(request.EmployeeId);
            if (employee is null)
                return BadRequest(new Models.ErrorResponse { Message = "Employee not found." });
            if (employee.UserId != null)
                return BadRequest(new Models.ErrorResponse { Message = "This employee is already linked to a login account." });

            var firstName = employee.FirstName.Trim();
            var lastName = employee.LastName.Trim();
            var middleName = string.IsNullOrWhiteSpace(employee.MiddleName) ? null : employee.MiddleName.Trim();

            var user = new UserManagement
            {
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password)),
                Role = request.Role,
                IsActive = request.IsActive
            };

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new Models.ErrorResponse { Message = "Database update error", Detail = ex.InnerException?.Message ?? ex.Message });
            }

            employee.UserId = user.Id;
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            if (request.Role == UserRole.Admin)
                return BadRequest(new Models.ErrorResponse { Message = "Admin cannot assign the Admin role." });

            var existing = await _db.Users.FindAsync(id);
            if (existing is null) return NotFound();
            if (existing.Role == UserRole.Admin)
                return BadRequest(new Models.ErrorResponse { Message = "Admin accounts cannot be managed here." });

            existing.Username = request.Username;
            existing.Email = request.Email;
            existing.Role = request.Role;
            existing.IsActive = request.IsActive;

            // Only overwrite the password if a new one was actually provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                existing.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password));
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Users.FindAsync(id);
            if (item == null) return NotFound();
            if (item.Role == UserRole.Admin)
                return BadRequest(new Models.ErrorResponse { Message = "Admin accounts cannot be managed here." });

            // unlink from employee before deleting so the employee record survives
            var employee = await _db.Set<Employee>().FirstOrDefaultAsync(e => e.UserId == item.Id);
            if (employee != null) employee.UserId = null;

            _db.Users.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        public class CreateUserRequest
        {
            public int EmployeeId { get; set; }
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public UserRole Role { get; set; }
            public bool IsActive { get; set; } = true;
        }

        public class UpdateUserRequest
        {
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Password { get; set; }
            public UserRole Role { get; set; }
            public bool IsActive { get; set; }
        }
    }
}