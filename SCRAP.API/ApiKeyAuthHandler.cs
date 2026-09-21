using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SCRAP.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SCRAP.API
{
    // A very small API-key based auth handler for local WinForms usage.
    // The WinForms client can pass header 'X-Api-User' with a username and the handler will create a ClaimsPrincipal
    public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly MasterErpDbContext _db;

        public ApiKeyAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, MasterErpDbContext db)
            : base(options, logger, encoder)
        {
            _db = db;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-User", out var userHeader))
            {
                return AuthenticateResult.NoResult();
            }

            var username = userHeader.ToString();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid user header");
            }

            var claims = new List<Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.Name, user.Username),
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
