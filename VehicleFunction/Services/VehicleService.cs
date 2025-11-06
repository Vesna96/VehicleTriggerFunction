using Newtonsoft.Json;
using System.Globalization;
using VehicleFunction.Models;
using VehicleFunction.Repositories;

public class VehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly HttpClient _httpClient;

    public VehicleService(IVehicleRepository repository, HttpClient httpClient)
    {
        _repository = repository;
        _httpClient = httpClient;
    }

    // Fetch one page
    public async Task<List<VehicleAd>> FetchAdsAsync(int page, string source)
    {
        string apiUrl = GetApiUrl(source, page);
        var response = await _httpClient.GetStringAsync(apiUrl);
        var data = JsonConvert.DeserializeObject<ApiResponse>(response);

        if (data?.Classifieds != null)
        {
            foreach (var ad in data.Classifieds)
            {
                ad.Source = source;
                ad.CreatedAt = ParseCreatedAt(ad.TagBlock);
            }
        }

        return data?.Classifieds != null ? new List<VehicleAd>(data.Classifieds) : new List<VehicleAd>();
    }

    // Fetch all pages
    public async Task<List<VehicleAd>> FetchAllAdsAsync(string source)
    {
        var allAds = new List<VehicleAd>();
        int page = 1;
        List<VehicleAd> pageAds;

        do
        {
            pageAds = await FetchAdsAsync(page, source);
            allAds.AddRange(pageAds);
            page++;
        } while (pageAds.Count > 0);

        return allAds;
    }

    public Task SaveAdsAsync(List<VehicleAd> ads)
    {
        return _repository.SaveAsync(ads);
    }

    private DateTime ParseCreatedAt(string tagBlock)
    {
        if (string.IsNullOrWhiteSpace(tagBlock))
            return DateTime.UtcNow;

        var parts = tagBlock.Split(',');
        var datePart = parts.Last().Trim();

        if (DateTime.TryParseExact(datePart, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        return DateTime.UtcNow;
    }

    private string GetApiUrl(string source, int page)
    {
        if (source == "Polovniautomobili")
        {
            return $"https://www.polovniautomobili.com/json/v1/getLast24hAds/26/{page}";
        }

        throw new ArgumentException("Unknown source");
    }
}
