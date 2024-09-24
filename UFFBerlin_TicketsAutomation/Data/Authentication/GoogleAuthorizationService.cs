using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Components;

namespace UFFBerlin_TicketsAutomation.Data.Authentication
{
    public class GoogleAuthorizationService
    {
        private UserCredential _credential;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NavigationManager _navigationManager;

        public GoogleAuthorizationService(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _navigationManager = navigationManager;
        }

        public async Task<UserCredential> GetGoogleCredentialAsync()
        {
            var userToken = _httpContextAccessor.HttpContext.Session.GetString("user_access_token");

            if (!string.IsNullOrEmpty(userToken) && _credential != null && _credential.Token.IsExpired(SystemClock.Default) == false)
            {
                // Reuse token from the session to create Google credentials
                return _credential;
            }
            else
            {
                // If no token exists in the session, or if it has expired, perform OAuth authentication flow
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        new[] { DriveService.Scope.Drive, GmailService.Scope.GmailSend, GmailService.Scope.GmailReadonly, "https://www.googleapis.com/auth/userinfo.profile" },
                        "user",
                        CancellationToken.None,
                        new FileDataStore("GmailTokenStore", true)
                    );

                    // Save the token in the session (can be refreshed later if needed)
                    _httpContextAccessor.HttpContext.Session.SetString("user_access_token", _credential.Token.AccessToken);

                    return _credential;
                }
            }
        }


        public async Task SignOutUser()
        {
            _httpContextAccessor.HttpContext.Session.Remove("user_access_token");

            // Optionally clear any stored tokens in the FileDataStore
            var tokenStore = new FileDataStore("GmailTokenStore", true);
            await tokenStore.ClearAsync();

            _navigationManager.NavigateTo("/sign-in", true);  // Redirect to sign-in page and force reload
        }
    }
}
