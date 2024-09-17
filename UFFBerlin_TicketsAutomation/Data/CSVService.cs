using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class CSVService
    {
        // Asynchronous method to extract data from the uploaded CSV file
        public async Task<List<(string email, int fileCount)>> ExtractDataFromCsvAsync(Stream csvStream)
        {
            var result = new List<(string email, int fileCount)>();

            using (var reader = new StreamReader(csvStream)) // StreamReader works asynchronously here
            {
                string line;

                // Read each line asynchronously
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var values = line.Split(',');

                    if (values.Length >= 2)
                    {
                        var email = values[0].Trim();
                        if (int.TryParse(values[1], out int fileCount))
                        {
                            result.Add((email, fileCount));
                        }
                    }
                }
            }

            return result;
        }
    }
}
