using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class FlightPlan
    {
        [JsonPropertyName("passengers")]
        public int Passengers { get; set; }

        [JsonPropertyName("company_name")]
        public string CompanyName { get; set; }

        [JsonPropertyName("initial_location")]
        public InitialLocation Location { get; set; }

        [JsonPropertyName("segments")]
        public List<Segment> Segments { get; set; }

        [JsonConstructor]
        public FlightPlan(int passengers, string companyName, InitialLocation location, List<Segment> segments)
        {
            Passengers = passengers;
            CompanyName = companyName;
            Location = location;
            Segments = segments;
        }

        public FlightPlan()
        {
        }
    }
}
