using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var userToken = _httpContextAccessor.HttpContext?.Session?.GetString("user_access_token");

        ClaimsIdentity identity = string.IsNullOrEmpty(userToken)
            ? new ClaimsIdentity()  // No user is logged in, create an empty identity
            : new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "GoogleUser")  // Example claim, modify as needed
            }, "Bearer");

        var claimsPrincipal = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(claimsPrincipal));
    }

    // Method to manually trigger the authentication state change
    public void NotifyAuthenticationStateChanged()
    {
        var authState = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(authState);
    }

    // Method to set the user's authentication state manually
    public void MarkUserAsAuthenticated(string userName)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userName)
        }, "Bearer");

        var claimsPrincipal = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    // Method to mark the user as logged out
    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }
}
