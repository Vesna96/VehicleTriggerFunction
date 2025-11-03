using VehicleFunction.Models;
using VehicleFunction.Repositories;

namespace VehicleFunction.Services
{
    public class VehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }
        public async Task InsertAdsToDatabaseAsync(List<VehicleAd> ads)
        {
            await _vehicleRepository.InsertAdsToDatabaseAsync(ads);
        }

        public async Task<List<VehicleAd>> FetchAllAdsAsync(string source)
        {
            var allAds = new List<VehicleAd>();
            int currentPage = 1;

            while (true)
            {
                var ads = await _vehicleRepository.FetchAdsAsync(currentPage, source);
                if (ads.Count == 0)
                {
                    break;
                }

                allAds.AddRange(ads);
                currentPage++;
            }

            return allAds;
        }

        public async Task<string> SaveAdsToCsvAsync(List<VehicleAd> ads)
        {
            return await _vehicleRepository.SaveToCsvAsync(ads);
        }

        public async Task UploadCsvAsync(string localCsvPath)
        {
            await _vehicleRepository.UploadCsvToBlobAsync(localCsvPath);
        }
    }
}