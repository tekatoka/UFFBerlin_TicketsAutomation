using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UFFBerlin_TicketsAutomation.Data.Authentication;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly GoogleAuthorizationService _googleAuthorizationService;

    public AuthController(GoogleAuthorizationService googleAuthorizationService)
    {
        _googleAuthorizationService = googleAuthorizationService;
    }

    [HttpGet("signin")]
    public async Task<IActionResult> SignIn()
    {
        await _googleAuthorizationService.GetGoogleCredentialAsync();
        return Redirect("/");  // Redirect back to the home page after sign-in
    }
}
