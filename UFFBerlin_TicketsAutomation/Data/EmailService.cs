using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class EmailService
    {
        private readonly GmailService _service;

        public EmailService()
        {
            _service = GetGmailService();
        }

        private GmailService GetGmailService()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // Authorize for both Gmail and Google Drive scopes
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailSend, DriveService.Scope.Drive },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true)).Result;
            }

            // Create the Gmail service
            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "UFFBerlin_TicketsAutomation",
            });
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, List<string> attachmentPaths)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Your Name", "your-email@gmail.com"));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            foreach (var path in attachmentPaths)
            {
                bodyBuilder.Attachments.Add(path);
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var stream = new MemoryStream();
            await message.WriteToAsync(stream);
            var rawMessage = Convert.ToBase64String(stream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = rawMessage
            };

            var request = _service.Users.Messages.Send(gmailMessage, "me");
            await request.ExecuteAsync();
        }
    }
}
