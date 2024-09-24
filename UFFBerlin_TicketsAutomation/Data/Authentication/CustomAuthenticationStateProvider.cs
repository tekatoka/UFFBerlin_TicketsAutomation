using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;


namespace UFFBerlin_TicketsAutomation.Data.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var userToken = _httpContextAccessor.HttpContext.Session.GetString("user_access_token");

            ClaimsIdentity identity;
            if (!string.IsNullOrEmpty(userToken))
            {
                // User is authenticated
                identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, "User"), // You can include more claims if necessary
            }, "apiauth_type");
            }
            else
            {
                // User is not authenticated
                identity = new ClaimsIdentity();
            }

            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }

        public void MarkUserAsAuthenticated(string username)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "apiauth_type");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void MarkUserAsLoggedOut()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }
}