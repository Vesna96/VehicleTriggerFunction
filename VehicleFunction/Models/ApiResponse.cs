namespace VehicleFunction.Models
{
    public class ApiResponse
    {
        public int TotalResults { get; set; }
        public VehicleAd[] Classifieds { get; set; }
    }
}
