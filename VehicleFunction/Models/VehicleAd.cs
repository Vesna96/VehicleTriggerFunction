using Newtonsoft.Json;

public class VehicleAd
{
    public int AdID { get; set; }
    public string Title { get; set; }
    public string Price { get; set; }
    public string Year { get; set; }
    public string ModelName { get; set; }
    public string FuelType { get; set; }
    public int Mileage { get; set; }
    public int Power { get; set; }
    public string City { get; set; }
    public string Region { get; set; }
    public string BrandName { get; set; }
    public string[] PhotoLink { get; set; }
    public string Source { get; set; }

    [JsonProperty("tag_block")]
    public string TagBlock { get; set; }

    public DateTime CreatedAt { get; set; }

    // FK fields
    public int BrandId { get; set; }
    public int ModelId { get; set; }
    public int FuelTypeId { get; set; }
    public int LocationId { get; set; }
}
