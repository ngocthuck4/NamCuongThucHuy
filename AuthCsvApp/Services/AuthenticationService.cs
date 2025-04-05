using Microsoft.AspNetCore.Http;

namespace AuthCsvApp.Services
{
    public class AuthenticationService
    {
        private readonly ISession _session;

        public AuthenticationService(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session;
        }

        public bool IsAdminAuthenticated(out string adminUsername)
        {
            adminUsername = _session.GetString("Username");
            var role = _session.GetString("Role");
            return !string.IsNullOrEmpty(adminUsername) && role == "Admin";
        }
    }
}