using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly GoogleAuthorizationService _googleAuthorizationService;
    private readonly string callbackUrl = "https://localhost:5001/api/auth/oauth-callback"; //uffb.danabond.eu //localhost:5001

    public AuthController(GoogleAuthorizationService googleAuthorizationService)
    {
        _googleAuthorizationService = googleAuthorizationService;
    }

    [HttpGet("signin")]
    public IActionResult SignIn()
    {
        string redirectUri = callbackUrl; // Replace with your actual redirect URI
        string authorizationUrl = _googleAuthorizationService.GetAuthorizationUrl(redirectUri);
        return Redirect(authorizationUrl);
    }

    [HttpGet("oauth-callback")]
    public async Task<IActionResult> OAuthCallback(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Code is missing.");
        }

        try
        {
            string redirectUri = callbackUrl; // Replace with your actual redirect URI
            await _googleAuthorizationService.ExchangeAuthorizationCodeForTokensAsync(code, redirectUri);
            return Redirect("/");
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred during the OAuth callback: {ex.Message}");
        }
    }
}
