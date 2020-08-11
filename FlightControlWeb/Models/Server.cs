using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    [Serializable]
    public class Server
    {
        [JsonPropertyName("ServerId")]
        public string ServerId { get; set; }

        [JsonPropertyName("ServerURL")]
        public string ServerURL { get; set; }

        [JsonConstructor]
        public Server(string serverId, string serverURL)
        {
            ServerId = serverId;
            ServerURL = serverURL;
        }

        public Server() { }
    }
}
