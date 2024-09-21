using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class CSVService
    {
        private readonly GoogleDriveService _googleDriveService;
        private readonly SettingsService _settingsService;

        public CSVService(GoogleDriveService googleDriveService, SettingsService settingsService)
        {
            _googleDriveService = googleDriveService;
            _settingsService = settingsService;
        }

        public async Task ExtractDataFromCsvAsync(Stream csvStream, Action<string> logAction)
        {
            try
            {
                using (var reader = new StreamReader(csvStream))
                {
                    string line;
                    int totalRows = 0;
                    int processedRows = 0;

                    // Fetch the latest folder IDs dynamically
                    string sourceFolderId = _settingsService.Settings.SourceFolderId;
                    string destinationFolderId = _settingsService.Settings.DestinationFolderId;

                    // Check if source folder exists and is accessible (by folder ID)
                    bool sourceFolderExists = await _googleDriveService.FolderExistsAsync(sourceFolderId);
                    if (!sourceFolderExists)
                    {
                        logAction("Error: Source folder does not exist or is not accessible.");
                        throw new InvalidOperationException("Source folder not found or inaccessible.");
                    }

                    // Process each row and log the progress
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        totalRows++;
                        var values = line.Split(',');

                        if (values.Length >= 2)
                        {
                            var email = values[0].Trim();
                            if (int.TryParse(values[1], out int fileCount))
                            {
                                // Check if the destination folder for this email already exists (by name and parent folder)
                                bool folderExists;
                                try
                                {
                                    folderExists = await _googleDriveService.FolderExistsAsync(email, destinationFolderId);
                                }
                                catch (GoogleApiException ex)
                                {
                                    logAction($"Error: Unable to access destination folder: {ex.Message}");
                                    throw;  // Stop processing by throwing the exception after logging
                                }

                                if (folderExists)
                                {
                                    // Log error and skip file assignment
                                    logAction($"Error: Folder for {email} already exists. Skipping file assignment.");
                                    continue;
                                }

                                // Create a new folder for the user if it doesn't exist
                                var userFolderId = await _googleDriveService.CreateFolderAsync(email, destinationFolderId);
                                logAction($"Created folder for {email}.");

                                try
                                {
                                    // Move files from the source folder to the new folder
                                    await _googleDriveService.MoveFilesToFolderAsync(sourceFolderId, userFolderId, fileCount, logAction);
                                    logAction($"Moved {fileCount} files for {email}.");
                                }
                                catch (InvalidOperationException ex)
                                {
                                    logAction($"Error: {ex.Message}");
                                    throw new InvalidOperationException(ex.Message);  // Stop processing after logging the error
                                }
                            }
                        }

                        processedRows++;
                        logAction($"Processed row {processedRows}/{totalRows} - Remaining: {totalRows - processedRows}");
                    }

                    logAction($"CSV processing completed. {processedRows}/{totalRows} rows processed.");
                }
            }
            catch (Exception ex)
            {
                // Log any general exceptions and stop processing
                logAction($"Error: {ex.Message}");
                throw;  // Ensure the error is re-thrown to stop processing
            }
        }


    }
}
