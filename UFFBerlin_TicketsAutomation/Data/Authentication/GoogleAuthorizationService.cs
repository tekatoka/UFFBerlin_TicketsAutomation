using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.PeopleService.v1;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Util;

public class GoogleAuthorizationService
{
    private UserCredential _credential;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly GoogleAuthorizationCodeFlow _authorizationFlow;

    public GoogleAuthorizationService(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;

        using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
        {
            var clientSecrets = GoogleClientSecrets.Load(stream).Secrets;

            _authorizationFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[]
                {
                    DriveService.Scope.Drive,
                    GmailService.Scope.GmailSend,
                    GmailService.Scope.GmailReadonly,
                    PeopleServiceService.Scope.UserinfoProfile
                },
                DataStore = new FileDataStore("GmailTokenStore", true)
            });
        }
    }

    // Start the sign-in process and get the authorization URL
    public string GetAuthorizationUrl(string redirectUri)
    {
        var authorizationUrl = _authorizationFlow.CreateAuthorizationCodeRequest(redirectUri).Build();
        return authorizationUrl?.ToString();
    }

    public async Task<UserCredential> GetGoogleCredentialAsync()
    {
        var userToken = _httpContextAccessor?.HttpContext?.Session?.GetString("user_access_token");

        if (!string.IsNullOrEmpty(userToken) && _credential != null && !_credential.Token.IsExpired(SystemClock.Default))
        {
            return _credential;
        }
        else
        {
            // Attempt to load the token from the data store if not already in session
            var tokenResponse = await _authorizationFlow.LoadTokenAsync("user", CancellationToken.None);
            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _credential = new UserCredential(_authorizationFlow, "user", tokenResponse);
                await StoreTokenInSessionAsync(tokenResponse); // Store the token in session
                return _credential;
            }

            throw new InvalidOperationException("User is not authenticated. Please sign in.");
        }
    }

    // Store tokens in session
    private async Task StoreTokenInSessionAsync(TokenResponse tokenResponse)
    {
        _httpContextAccessor.HttpContext.Session.SetString("user_access_token", tokenResponse.AccessToken);

        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            _httpContextAccessor.HttpContext.Session.SetString("user_refresh_token", tokenResponse.RefreshToken);
        }

        await _authorizationFlow.DataStore.StoreAsync("user", tokenResponse);
    }


    public async Task<UserCredential> ExchangeAuthorizationCodeForTokensAsync(string code, string redirectUri)
    {
        TokenResponse tokenResponse = await _authorizationFlow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
        _credential = new UserCredential(_authorizationFlow, "user", tokenResponse);

        // Store the token in session
        _httpContextAccessor.HttpContext.Session.SetString("user_access_token", tokenResponse.AccessToken);

        return _credential;
    }

    public async Task SignOutUser()
    {
        if (_httpContextAccessor?.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Session.Remove("user_access_token");
            _httpContextAccessor.HttpContext.Session.Clear();
        }

        // Optionally clear any stored tokens in the FileDataStore
        await _authorizationFlow.DataStore.ClearAsync();
        _navigationManager.NavigateTo("/sign-in", true);  // Redirect to sign-in page and force reload
    }
}
