using Google.Apis.Gmail.v1;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using MimeKit;
using UFFBerlin_TicketsAutomation.Data.Authentication;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class EmailService
    {
        private readonly GoogleAuthorizationService _googleAuthService;
        private GmailService _service;
        private string _cachedSenderEmail;
        private string _cachedSenderName;

        public string SenderEmail => _cachedSenderEmail;
        public string SenderName => _cachedSenderName;

        public EmailService(GoogleAuthorizationService googleAuthService)
        {
            _googleAuthService = googleAuthService;
        }

        private async Task<GmailService> InitializeGmailServiceAsync()
        {
            if (_service == null)
            {
                var credential = await _googleAuthService.GetGoogleCredentialAsync();

                var grantedScopes = credential.Token.Scope;

                _service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "UFFBerlin_TicketsAutomation"
                });

                // Fetch user profile once during initialization
                //await FetchUserProfileAsync();
            }
            return _service;
        }

        public async Task FetchUserProfileAsync()
        {
            await InitializeGmailServiceAsync();

            // Get user's email from Gmail API
            var request = _service.Users.GetProfile("me");
            var profile = await request.ExecuteAsync();
            _cachedSenderEmail = profile.EmailAddress;

            // Fetch user's name using the People API
            var peopleService = new PeopleServiceService(new BaseClientService.Initializer
            {
                HttpClientInitializer = await _googleAuthService.GetGoogleCredentialAsync(),
                ApplicationName = "UFFBerlin_TicketsAutomation"
            });

            // Request the authenticated user's profile information
            var personRequest = peopleService.People.Get("people/me");
            personRequest.PersonFields = "names";
            var person = await personRequest.ExecuteAsync();

            // Retrieve the full name if available
            _cachedSenderName = person.Names?.FirstOrDefault()?.DisplayName ?? "Unknown";
        }

        public async Task SendEmailAsync(string recepientEmail, string subject, string body, List<string> attachmentPaths)
        {
            // Initialize Gmail service before sending an email
            await InitializeGmailServiceAsync();

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_cachedSenderName, _cachedSenderEmail));
            emailMessage.To.Add(new MailboxAddress("", recepientEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };

            // Add attachments
            foreach (var path in attachmentPaths)
            {
                bodyBuilder.Attachments.Add(path);
            }

            emailMessage.Body = bodyBuilder.ToMessageBody();

            // Prepare the email to be sent via the Gmail API
            using var stream = new MemoryStream();
            await emailMessage.WriteToAsync(stream);
            var rawMessage = Convert.ToBase64String(stream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = rawMessage
            };

            // Send the email
            var request = _service.Users.Messages.Send(gmailMessage, "me");
            await request.ExecuteAsync();
        }
    }
}