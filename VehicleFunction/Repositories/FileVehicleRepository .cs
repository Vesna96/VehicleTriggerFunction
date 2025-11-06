using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace VehicleFunction.Repositories
{
    public class FileVehicleRepository : IVehicleRepository
    {
        private readonly ILogger<FileVehicleRepository> _logger;
        private readonly string _blobConnectionString;
        private readonly string _containerName = "my-container";

        public FileVehicleRepository(ILogger<FileVehicleRepository> logger)
        {
            _logger = logger;
            _blobConnectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
        }

        public async Task SaveAsync(List<VehicleAd> ads)
        {
            var filePath = await SaveToCsvAsync(ads);

            await UploadCsvToBlobAsync(filePath);
        }


        public async Task<string> SaveToCsvAsync(List<VehicleAd> ads)
        {
            string tempDir = Path.GetTempPath();
            string fileName = $"vehicle_ads_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(tempDir, fileName);

            try
            {
                using (var writer = new StreamWriter(filePath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    await csv.WriteRecordsAsync(ads);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving CSV file: {ex.Message}");
                throw new Exception("Error saving CSV file", ex);
            }
            return filePath;
        }

        private async Task UploadCsvToBlobAsync(string localCsvPath)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                var source = "polovniautomobili";
                string folderPath = $"nonprod/vehicles/cars/year={DateTime.UtcNow:yyyy}/month={DateTime.UtcNow:MM}/day={DateTime.UtcNow:dd}/{source}";
                string blobFileName = Path.GetFileName(localCsvPath);
                string blobPath = $"{folderPath}/{blobFileName}";

                var blobClient = containerClient.GetBlobClient(blobPath);

                using var fileStream = File.OpenRead(localCsvPath);
                await blobClient.UploadAsync(fileStream, overwrite: true);

                _logger.LogInformation($"Uploaded CSV to Blob Storage at path: {blobPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading CSV to Blob: {ex.Message}");
                throw;
            }
        }
    }
}
