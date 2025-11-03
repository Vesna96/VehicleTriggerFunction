using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VehicleFunction.Services;

namespace VehicleFunction
{
    public class VehicleFunction
    {
        private readonly ILogger _logger;
        private readonly VehicleService _vehicleService;

        public VehicleFunction(ILoggerFactory loggerFactory, VehicleService vehicleService)
        {
            _logger = loggerFactory.CreateLogger<VehicleFunction>();
            _vehicleService = vehicleService;
        }

        [Function("VehicleFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var allAds = await _vehicleService.FetchAllAdsAsync("Polovniautomobili");

            if (allAds.Count > 0)
            {
                var filePath = await _vehicleService.SaveAdsToCsvAsync(allAds); 
                await _vehicleService.UploadCsvAsync(filePath);

                await _vehicleService.InsertAdsToDatabaseAsync(allAds);

                _logger.LogInformation($"Total collected ads: {allAds.Count}");             
            }
            else
            {
                _logger.LogInformation("No vehicle ads fetched in total.");
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}