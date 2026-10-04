using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCRAP.infrastructure.data;

namespace SCRAP.API
{
    public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TenantErpDbContext _tenantDb;
        private readonly MasterErpDbContext _masterDb;

        public ApiKeyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            TenantErpDbContext tenantDb,
            MasterErpDbContext masterDb)
            : base(options, logger, encoder)
        {
            _tenantDb = tenantDb;
            _masterDb = masterDb;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-User", out var userHeader))
                return AuthenticateResult.NoResult();

            var rawUser = userHeader.ToString();
            var username = rawUser.Contains(',')
                ? rawUser.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last()
                : rawUser.Trim();

            // 1. Check if SuperAdmin in Master DB
            var superAdmin = await _masterDb.SuperAdmins.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (superAdmin != null)
            {
                var superClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, superAdmin.Username),
                    new Claim(ClaimTypes.NameIdentifier, superAdmin.Id.ToString()),
                    new Claim(ClaimTypes.Role, "Superadmin"),
                    new Claim("BranchId", "")
                };
                var superIdentity = new ClaimsIdentity(superClaims, Scheme.Name);
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(superIdentity), Scheme.Name));
            }

            // 2. Otherwise check Tenant DB
            var user = await _tenantDb.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null)
                return AuthenticateResult.Fail("Invalid user header");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role,           user.Role.ToString()),
                new Claim("BranchId", user.BranchId.HasValue ? user.BranchId.Value.ToString() : "")
            };

            var identity  = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket    = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
