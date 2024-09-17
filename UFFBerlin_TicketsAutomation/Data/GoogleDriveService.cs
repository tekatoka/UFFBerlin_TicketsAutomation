using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class GoogleDriveService
    {
        private readonly DriveService _service;

        public GoogleDriveService()
        {
            _service = GetDriveService();
        }

        private DriveService GetDriveService()
        {
            UserCredential credential;

            // Using GoogleWebAuthorizationBroker for OAuth 2.0 credentials
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // Authorize the user via Google Web Authorization Broker (OAuth 2.0 flow)
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive, GmailService.Scope.GmailSend }, // Adjust scopes as needed
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true)).Result;
            }

            // Create the Google Drive service
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "UFFBerlin_TicketsAutomation",
            });
        }

        // Method to create a folder on Google Drive
        //public async Task<string> CreateFolderAsync(string folderName, string parentFolderId)
        //{
        //    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        //    {
        //        Name = folderName,
        //        MimeType = "application/vnd.google-apps.folder",
        //        Parents = new List<string> { parentFolderId }
        //    };

        //    var request = _service.Files.Create(fileMetadata);
        //    request.Fields = "id";
        //    var file = await request.ExecuteAsync();
        //    return file.Id;
        //}

        public async Task<string> CreateFolderAsync(string folderName, string parentFolderId)
        {
            // Ensure the parentFolderId is correct and does not have extra query parameters
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId } // Cleaned up parentFolderId
            };

            var request = _service.Files.Create(fileMetadata);
            request.Fields = "id";

            try
            {
                var file = await request.ExecuteAsync();
                return file.Id;
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"Error creating folder: {ex.Message}");
                throw;
            }
        }

        // Method to move files from source folder to destination folder on Google Drive
        public async Task MoveFilesToFolderAsync(string sourceFolderId, string destinationFolderId, int amount)
        {
            var listRequest = _service.Files.List();
            listRequest.Q = $"'{sourceFolderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name, parents)";
            var files = (await listRequest.ExecuteAsync()).Files;

            foreach (var file in files.Take(amount))
            {
                var previousParents = string.Join(",", file.Parents);
                var updateRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), file.Id);
                updateRequest.AddParents = destinationFolderId;
                updateRequest.RemoveParents = previousParents;
                await updateRequest.ExecuteAsync();
            }
        }

        // Method to list all subfolders in a specified parent folder on Google Drive
        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFoldersAsync(string parentFolderId)
        {
            var listRequest = _service.Files.List();
            listRequest.Q = $"'{parentFolderId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";
            var result = await listRequest.ExecuteAsync();

            // Convert the IList to a List before returning
            return result.Files.ToList();
        }

        // Method to download all files in a specific folder on Google Drive
        public async Task<List<string>> DownloadFilesFromFolderAsync(string folderId)
        {
            var listRequest = _service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";
            var files = (await listRequest.ExecuteAsync()).Files;

            var attachmentPaths = new List<string>();
            foreach (var file in files)
            {
                var request = _service.Files.Get(file.Id);
                var stream = new MemoryStream();
                await request.DownloadAsync(stream);

                // Save the file to a temporary path
                var filePath = Path.Combine(Path.GetTempPath(), file.Name);
                await File.WriteAllBytesAsync(filePath, stream.ToArray());
                attachmentPaths.Add(filePath);
            }
            return attachmentPaths;
        }

        public async Task MoveFolderAsync(string folderId, string destinationParentFolderId)
        {
            // Get the current parent of the folder (this is required to remove the folder from the current parent)
            var getRequest = _service.Files.Get(folderId);
            getRequest.Fields = "parents";
            var file = await getRequest.ExecuteAsync();

            var previousParents = string.Join(",", file.Parents);

            // Move the folder by updating its parent to the new destination folder
            var updateRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), folderId);
            updateRequest.AddParents = destinationParentFolderId;
            updateRequest.RemoveParents = previousParents;

            await updateRequest.ExecuteAsync();
        }
    }
}
