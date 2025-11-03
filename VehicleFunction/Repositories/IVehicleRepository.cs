using VehicleFunction.Models;

namespace VehicleFunction.Repositories
{
    public interface IVehicleRepository
    {
        Task<List<VehicleAd>> FetchAdsAsync(int page, string source);
        Task<string> SaveToCsvAsync(List<VehicleAd> ads);
        Task InsertAdsToDatabaseAsync(List<VehicleAd> ads);
        Task UploadCsvToBlobAsync(string localCsvPath);
        
    }
}
