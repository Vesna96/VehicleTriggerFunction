using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger executed at: {DateTime.Now}");

            var allAds = await _vehicleService.FetchAllAdsAsync("Polovniautomobili"); 

            if (allAds.Count > 0)
            {
                await _vehicleService.SaveAdsAsync(allAds);

                _logger.LogInformation($"Total collected ads: {allAds.Count}");
            }
            else
            {
                _logger.LogInformation("No vehicle ads fetched.");
            }

            if (myTimer.ScheduleStatus != null)
            {
                _logger.LogInformation($"Next schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}