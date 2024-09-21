using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class EmailService
    {
        private readonly GmailService _service;

        public EmailService()
        {
            // Initialize the Gmail API service with OAuth2 authentication
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailSend },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("GmailTokenStore", true)).Result;
            }

            // Create the Gmail service
            _service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Your App Name",
            });
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, List<string> attachmentPaths)
        {
            // Create the email message
            var emailMessage = new MimeMessage();

            //TODO!!! Change for productive use with UFFB! Ukrainian Film Festival Berlin / info@uffberlin.de
            emailMessage.From.Add(new MailboxAddress("Dana Bondarenko", "tekatoka@gmail.com")); // sender's name and email
            emailMessage.To.Add(new MailboxAddress("", toEmail)); // recipient's email
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
