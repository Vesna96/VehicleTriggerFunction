namespace VehicleFunction.Repositories
{
    public interface IVehicleRepository
    {
        Task SaveAsync(List<VehicleAd> ads);
    }
}
