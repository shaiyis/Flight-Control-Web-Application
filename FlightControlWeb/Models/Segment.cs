using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class Segment
    {
        [JsonPropertyName ("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("timespan_seconds")]
        public double TimeSpanSeconds { get; set; }

        [JsonConstructor]
        public Segment(double longitude, double latitude, double timeSpanSeconds)
        {
            Longitude = longitude;
            Latitude = latitude;
            TimeSpanSeconds = timeSpanSeconds;
        }

        public Segment()
        {
        }

    }
}
