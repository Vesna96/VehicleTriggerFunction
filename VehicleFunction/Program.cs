using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VehicleFunction.Repositories;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<IVehicleRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var option = configuration["StorageOption"];

            return option switch
            {
                "Database" => new DatabaseVehicleRepository(),
                _ => new FileVehicleRepository(sp.GetRequiredService<ILogger<FileVehicleRepository>>())
            };
        });

        services.AddScoped<VehicleService>();
    })
    .Build();

host.Run();
