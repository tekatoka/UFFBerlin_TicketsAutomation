using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class CSVService
    {
        private readonly GoogleDriveService _googleDriveService;
        private readonly string _destinationFolderId;
        private readonly string _sourceFolderId;

        public CSVService(GoogleDriveService googleDriveService, IConfiguration configuration)
        {
            _googleDriveService = googleDriveService;
            _destinationFolderId = configuration["GoogleApi:DestinationFolderId"];
            _sourceFolderId = configuration["GoogleApi:SourceFolderId"]; // Source folder for files
        }

        public async Task ExtractDataFromCsvAsync(Stream csvStream, Action<string> logAction)
        {
            using (var reader = new StreamReader(csvStream))
            {
                string line;
                int totalRows = 0;
                int processedRows = 0;

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
                            // Check if the folder for this email already exists
                            bool folderExists = await _googleDriveService.FolderExistsAsync(email, _destinationFolderId);

                            if (folderExists)
                            {
                                // Log error and skip file assignment
                                logAction($"Error: Folder for {email} already exists. Skipping file assignment.");
                                continue;
                            }

                            // Create a new folder for the user if it doesn't exist
                            var userFolderId = await _googleDriveService.CreateFolderAsync(email, _destinationFolderId);
                            logAction($"Created folder for {email}.");

                            try
                            {
                                // Move files from the source folder to the new folder
                                await _googleDriveService.MoveFilesToFolderAsync(_sourceFolderId, userFolderId, fileCount, logAction);
                                logAction($"Moved {fileCount} files for {email}.");
                            }
                            catch (InvalidOperationException ex)
                            {
                                logAction(ex.Message);
                                break; // Stop processing if there are not enough files
                            }
                        }
                    }

                    processedRows++;
                    logAction($"Processed row {processedRows}/{totalRows} - Remaining: {totalRows - processedRows}");
                }

                logAction($"CSV processing completed. {processedRows}/{totalRows} rows processed.");
            }
        }

    }
}
