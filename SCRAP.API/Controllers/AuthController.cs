using Microsoft.AspNetCore.Mvc;
using SCRAP.infrastructure.data;
using SCRAP.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        private readonly ILogger<AuthController> _logger;
        public AuthController(MasterErpDbContext db, ILogger<AuthController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new Models.ErrorResponse { Message = "Username and password required" });

            _logger.LogInformation("Login attempt for username: {Username}", request.Username);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Username}", request.Username);
                return Unauthorized(new Models.ErrorResponse { Message = "Invalid credentials" });
            }

            // Passwords are stored as Base64-encoded strings in PasswordHash (simple placeholder).
            // Compare by encoding the provided password the same way.
            var providedHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password));
            if (user.PasswordHash != providedHash)
            {
                _logger.LogWarning("Login failed - password mismatch for user: {Username}", request.Username);
                return Unauthorized(new Models.ErrorResponse { Message = "Invalid credentials" });
            }

            _logger.LogInformation("Login successful for user: {Username} (Role={Role})", user.Username, user.Role);

            return Ok(new
            {
                Username = user.Username,
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.MiddleName)
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
        }
    }
}