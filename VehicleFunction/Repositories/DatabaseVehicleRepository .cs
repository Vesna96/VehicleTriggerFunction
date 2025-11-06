using Dapper;
using Microsoft.Data.SqlClient;
using VehicleFunction.Repositories;

public class DatabaseVehicleRepository : IVehicleRepository
{
    public async Task SaveAsync(List<VehicleAd> ads)
    {
        var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var brandCache = new Dictionary<string, int>();
        var modelCache = new Dictionary<(string model, int brandId), int>();
        var fuelCache = new Dictionary<string, int>();
        var locationCache = new Dictionary<(string city, string region), int>();

        foreach (var ad in ads)
        {
            // 1️⃣ Brand
            if (!brandCache.TryGetValue(ad.BrandName, out var brandId))
            {
                brandId = await connection.ExecuteScalarAsync<int>(@"
                    IF EXISTS (SELECT 1 FROM Brand WHERE BrandName = @Name)
                        SELECT BrandId FROM Brand WHERE BrandName = @Name
                    ELSE
                        INSERT INTO Brand (BrandName) VALUES (@Name); SELECT SCOPE_IDENTITY();",
                    new { Name = ad.BrandName });
                brandCache[ad.BrandName] = brandId;
            }

            // 2️⃣ VehicleModel
            var modelKey = (ad.ModelName, brandId);
            if (!modelCache.TryGetValue(modelKey, out var modelId))
            {
                modelId = await connection.ExecuteScalarAsync<int>(@"
                    IF EXISTS (SELECT 1 FROM VehicleModel WHERE VehicleModelName = @Name AND BrandId = @BrandId)
                        SELECT VehicleModelId FROM VehicleModel WHERE VehicleModelName = @Name AND BrandId = @BrandId
                    ELSE
                        INSERT INTO VehicleModel (VehicleModelName, BrandId) VALUES (@Name, @BrandId); SELECT SCOPE_IDENTITY();",
                    new { Name = ad.ModelName, BrandId = brandId });
                modelCache[modelKey] = modelId;
            }

            // 3️⃣ FuelType
            if (!fuelCache.TryGetValue(ad.FuelType, out var fuelTypeId))
            {
                fuelTypeId = await connection.ExecuteScalarAsync<int>(@"
                    IF EXISTS (SELECT 1 FROM FuelType WHERE Type = @Type)
                        SELECT FuelTypeId FROM FuelType WHERE Type = @Type
                    ELSE
                        INSERT INTO FuelType (Type) VALUES (@Type); SELECT SCOPE_IDENTITY();",
                    new { Type = ad.FuelType });
                fuelCache[ad.FuelType] = fuelTypeId;
            }

            // 4️⃣ Location
            var locationKey = (ad.City, ad.Region);
            if (!locationCache.TryGetValue(locationKey, out var locationId))
            {
                locationId = await connection.ExecuteScalarAsync<int>(@"
                    IF EXISTS (SELECT 1 FROM Location WHERE City = @City AND Region = @Region)
                        SELECT LocationId FROM Location WHERE City = @City AND Region = @Region
                    ELSE
                        INSERT INTO Location (City, Region) VALUES (@City, @Region); SELECT SCOPE_IDENTITY();",
                    new { City = ad.City, Region = ad.Region });
                locationCache[locationKey] = locationId;
            }

            // Save Ads ids
            ad.BrandId = brandId;
            ad.ModelId = modelId;
            ad.FuelTypeId = fuelTypeId;
            ad.LocationId = locationId;
        }

        // 5️⃣ Bulk insert VehicleAd
        var sql = @"
            INSERT INTO VehicleAd 
            (AdId, Title, Price, Year, Mileage, Power, BrandId, ModelId, FuelTypeId, LocationId, Source, CreatedAt)
            VALUES (@AdID, @Title, @Price, @Year, @Mileage, @Power, @BrandId, @ModelId, @FuelTypeId, @LocationId, @Source, @CreatedAt)";

        await connection.ExecuteAsync(sql, ads);
    }
}
