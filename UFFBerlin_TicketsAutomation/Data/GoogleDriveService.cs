using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UFFBerlin_TicketsAutomation.Data.Authentication;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class GoogleDriveService
    {
        private readonly GoogleAuthorizationService _googleAuthService;
        private DriveService? _service;

        public GoogleDriveService(GoogleAuthorizationService googleAuthService)
        {
            _googleAuthService = googleAuthService;
        }

        private async Task InitializeDriveServiceAsync()
        {
            if (_service == null)
            {
                var credential = await _googleAuthService.GetGoogleCredentialAsync();

                _service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "UFFBerlin_TicketsAutomation"
                });
            }
        }

        public async Task<string> CreateFolderAsync(string folderName, string parentFolderId)
        {
            await InitializeDriveServiceAsync();

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId }
            };

            var request = _service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = await request.ExecuteAsync();

            return file.Id;
        }

        public async Task MoveFilesToFolderAsync(string sourceFolderId, string destinationFolderId, int fileCount, Action<string> logAction, string email)
        {
            var listRequest = _service.Files.List();
            listRequest.Q = $"'{sourceFolderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name, parents)";
            var files = (await listRequest.ExecuteAsync()).Files;

            if (files.Count < fileCount)
            {
                logAction($"Error: Not enough files in the source folder. Expected {fileCount}, but found {files.Count}. Stopping process.");
                throw new InvalidOperationException($"Not enough files in the source folder. Expected {fileCount}, but found {files.Count}.");
            }

            foreach (var file in files.Take(fileCount))
            {
                var previousParents = file.Parents != null ? string.Join(",", file.Parents) : null;

                if (string.IsNullOrEmpty(previousParents))
                {
                    logAction($"Error: File {file.Name} does not have a parent.");
                    continue; // Skip the file if it has no parent
                }

                try
                {
                    var updateRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), file.Id);
                    updateRequest.AddParents = destinationFolderId;
                    updateRequest.RemoveParents = previousParents;
                    await updateRequest.ExecuteAsync();

                    //// Step 1: Remove the file from its current parent folder
                    //var removeParentRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), file.Id);
                    //removeParentRequest.RemoveParents = previousParents; // Explicitly remove the file from the current parent
                    //removeParentRequest.Fields = "id, parents";

                    //await removeParentRequest.ExecuteAsync();

                    //// Step 2: Add the file to the new parent folder
                    //var addParentRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), file.Id);
                    //addParentRequest.AddParents = destinationFolderId; // Add the file to the destination parent folder
                    //addParentRequest.Fields = "id, parents";

                    //await addParentRequest.ExecuteAsync();

                    logAction($"Successfully moved file {file.Name} to folder {email} (id: {destinationFolderId})");
                }
                catch (Google.GoogleApiException ex)
                {
                    logAction($"Error moving file {file.Name}: {ex.Message}");
                    throw; // Rethrow the exception for further handling if needed
                }
            }
        }

        // Method to list all subfolders in a specified parent folder on Google Drive
        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFoldersAsync(string parentFolderId)
        {
            await InitializeDriveServiceAsync();

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

            await InitializeDriveServiceAsync();

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
            await InitializeDriveServiceAsync();

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

        public async Task<bool> FolderExistsAsync(string folderId, string parentFolderId = null)
        {
            await InitializeDriveServiceAsync();

            try
            {
                if (parentFolderId == null)
                {
                    // If no parent folder ID is provided, assume we are checking by folderId directly
                    var request = _service.Files.Get(folderId);
                    request.Fields = "id";

                    // Try to execute the request to check if the folder exists by its ID
                    var file = await request.ExecuteAsync();

                    return file != null;
                }
                else
                {
                    // If parent folder ID is provided, we are checking by folder name in a specific parent folder
                    var listRequest = _service.Files.List();
                    listRequest.Q = $"'{parentFolderId}' in parents and name = '{folderId}' and trashed = false";
                    listRequest.Fields = "files(id, name)";

                    var result = await listRequest.ExecuteAsync();
                    return result.Files.Count > 0;
                }
            }
            catch (Google.GoogleApiException ex)
            {
                // Handle the folder not found case
                if (ex.Error.Code == 404)
                {
                    return false; // Folder not found
                }
                else
                {
                    throw; // Re-throw other Google API exceptions
                }
            }
        }
    }
}
