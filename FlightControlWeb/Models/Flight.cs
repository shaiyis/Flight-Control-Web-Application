using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class Flight
    {
        [JsonPropertyName("flight_id")]
        public string FlightId { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("passengers")]
        public int Passengers { get; set; }

        [JsonPropertyName("company_name")]
        public string CompanyName { get; set; }

        [JsonPropertyName("date_time")]
        public string DateTime { get; set; }

        [JsonPropertyName("is_external")]
        public bool IsExternal { get; set; }

        public Flight(string id, bool isExternal, FlightPlan plan)
        {
            FlightId = id;
            IsExternal = isExternal;
            Longitude = plan.Location.Longitude;
            Latitude = plan.Location.Latitude;
            Passengers = plan.Passengers;
            CompanyName = plan.CompanyName;
            DateTime = plan.Location.DateTime;
        }

        [JsonConstructor]
        public Flight(string flightId, double longitude, double latitude, int passengers,
            string companyName, string dateTime, bool isExternal)
        {
            FlightId = flightId;
            Longitude = longitude;
            Latitude = latitude;
            Passengers = passengers;
            CompanyName = companyName;
            DateTime = dateTime;
            IsExternal = isExternal;
        }

    }
}
