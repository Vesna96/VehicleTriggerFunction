//using Azure.Storage.Blobs;
//using CsvHelper;
//using CsvHelper.Configuration;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System.Globalization;
//using System.Net.Http;
//using VehicleFunction.Models;
//using Dapper;
//using Microsoft.Data.SqlClient;


//namespace VehicleFunction.Repositories
//{
//    public class VehicleRepository : IVehicleRepository
//    {
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly ILogger<VehicleRepository> _logger;
//        private readonly string _blobConnectionString;
//        private readonly string _containerName = "my-container";

//        public VehicleRepository(IHttpClientFactory httpClientFactory, ILogger<VehicleRepository> logger)
//        {
//            _httpClientFactory = httpClientFactory;
//            _logger = logger;
//            _blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
//        }

//        public async Task<List<VehicleAd>> FetchAdsAsync(int page, string source)
//        {
//            _logger.LogInformation($"Fetching ads for page {page} from source {source}");

//            string apiUrl = GetApiUrl(source, page);
//            try
//            {
//                var client = _httpClientFactory.CreateClient();
//                var response = await client.GetStringAsync(apiUrl);
//                var data = JsonConvert.DeserializeObject<ApiResponse>(response);

//                if (data?.Classifieds != null)
//                {
//                    foreach (var ad in data.Classifieds)
//                    {
//                        ad.Source = source;
//                        ad.CreatedAt = ParseCreatedAt(ad.TagBlock);
//                    }
//                }

//                return data?.Classifieds != null ? new List<VehicleAd>(data.Classifieds) : new List<VehicleAd>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error fetching data from API: {ex.Message}");
//                throw new Exception("Error fetching data from API", ex);
//            }
//        }

//        private DateTime ParseCreatedAt(string tagBlock)
//        {
//            if (string.IsNullOrWhiteSpace(tagBlock))
//                return DateTime.UtcNow;

//            var parts = tagBlock.Split(',');
//            var datePart = parts.Last().Trim();

//            if (DateTime.TryParseExact(datePart, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
//            {
//                return parsedDate;
//            }

//            return DateTime.UtcNow;
//        }

//        public async Task InsertAdsToDatabaseAsync(List<VehicleAd> ads)
//        {
//            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            foreach (var ad in ads)
//            {
//                // 1️ Brand
//                var brandId = await connection.ExecuteScalarAsync<int>(@"
//            IF EXISTS (SELECT 1 FROM Brand WHERE BrandName = @Name)
//                SELECT BrandId FROM Brand WHERE BrandName = @Name
//            ELSE
//                INSERT INTO Brand (BrandName) VALUES (@Name); SELECT SCOPE_IDENTITY();",
//                    new { Name = ad.BrandName });

//                // 2️ VehicleModel
//                var modelId = await connection.ExecuteScalarAsync<int>(@"
//            IF EXISTS (SELECT 1 FROM VehicleModel WHERE VehicleModelName = @Name AND BrandId = @BrandId)
//                SELECT VehicleModelId FROM VehicleModel WHERE VehicleModelName = @Name AND BrandId = @BrandId
//            ELSE
//                INSERT INTO VehicleModel (VehicleModelName, BrandId) VALUES (@Name, @BrandId); SELECT SCOPE_IDENTITY();",
//                    new { Name = ad.ModelName, BrandId = brandId });

//                // 3️ FuelType
//                var fuelTypeId = await connection.ExecuteScalarAsync<int>(@"
//            IF EXISTS (SELECT 1 FROM FuelType WHERE Type = @Type)
//                SELECT FuelTypeId FROM FuelType WHERE Type = @Type
//            ELSE
//                INSERT INTO FuelType (Type) VALUES (@Type); SELECT SCOPE_IDENTITY();",
//                    new { Type = ad.FuelType });

//                // 4️ Location
//                var locationId = await connection.ExecuteScalarAsync<int>(@"
//            IF EXISTS (SELECT 1 FROM Location WHERE City = @City AND Region = @Region)
//                SELECT LocationId FROM Location WHERE City = @City AND Region = @Region
//            ELSE
//                INSERT INTO Location (City, Region) VALUES (@City, @Region); SELECT SCOPE_IDENTITY();",
//                    new { City = ad.City, Region = ad.Region });

//                // 5️ VehicleAd
//                var sql = @"
//            INSERT INTO VehicleAd 
//            (AdId, Title, Price, Year, Mileage, Power, BrandId, ModelId, FuelTypeId, LocationId, Source, CreatedAt)
//            VALUES (@AdId, @Title, @Price, @Year, @Mileage, @Power, @BrandId, @ModelId, @FuelTypeId, @LocationId, @Source, @CreatedAt)";

//                await connection.ExecuteAsync(sql, new
//                {
//                    ad.AdID,
//                    ad.Title,
//                    ad.Price,
//                    ad.Year,
//                    ad.Mileage,
//                    ad.Power,
//                    BrandId = brandId,
//                    ModelId = modelId,
//                    FuelTypeId = fuelTypeId,
//                    LocationId = locationId,
//                    ad.Source,
//                    ad.CreatedAt
//                });
//            }
//        }

//        public async Task<string> SaveToCsvAsync(List<VehicleAd> ads)
//        {
//            string tempDir = Path.GetTempPath();
//            string fileName = $"vehicle_ads_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
//            string filePath = Path.Combine(tempDir, fileName);

//            try
//            {
//                using (var writer = new StreamWriter(filePath))
//                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
//                {
//                    await csv.WriteRecordsAsync(ads);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error saving CSV file: {ex.Message}");
//                throw new Exception("Error saving CSV file", ex);
//            }
//            return filePath;
//        }

//        public async Task UploadCsvToBlobAsync(string localCsvPath)
//        {
//            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
//            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
//            var source = "polovniautomobili";

//            string folderPath = $"nonprod/vehicles/cars/year={DateTime.UtcNow:yyyy}/month={DateTime.UtcNow:MM}/day={DateTime.UtcNow:dd}/{source}";
//            string blobFileName = Path.GetFileName(localCsvPath);
//            string blobPath = $"{folderPath}/{blobFileName}";

//            var blobClient = containerClient.GetBlobClient(blobPath);

//            using var fileStream = File.OpenRead(localCsvPath);
//            await blobClient.UploadAsync(fileStream, overwrite: true);
//        }

//        private string GetApiUrl(string source, int page)
//        {
//            if (source == "Polovniautomobili")
//            {
//                return $"https://www.polovniautomobili.com/json/v1/getLast24hAds/26/{page}";
//            }

//            throw new ArgumentException("Unknown source");
//        }


//    }

//}

