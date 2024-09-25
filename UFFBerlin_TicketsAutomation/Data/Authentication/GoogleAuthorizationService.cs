using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

            if (!string.IsNullOrEmpty(userToken) && _credential != null && !_credential.Token.IsExpired(SystemClock.Default))
            {
                // Reuse token from the session to create Google credentials
                return _credential;
            }
            else
            {
                // Load client secrets from the credentials.json file
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    var clientSecrets = GoogleClientSecrets.Load(stream).Secrets;

                    // Initialize the OAuth flow
                    var authorizationFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = clientSecrets,
                        Scopes = new[]
                        {
                            DriveService.Scope.Drive,
                            GmailService.Scope.GmailSend,
                            GmailService.Scope.GmailReadonly,
                            PeopleServiceService.Scope.UserinfoProfile // This will allow you to get user profile information
                        },
                        DataStore = new FileDataStore("GmailTokenStore", true)
                    });

                    // Perform the OAuth flow and store credentials
                    _credential = await new AuthorizationCodeInstalledApp(authorizationFlow, new LocalServerCodeReceiver())
                        .AuthorizeAsync("user", CancellationToken.None);

                    // Save token to session
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
