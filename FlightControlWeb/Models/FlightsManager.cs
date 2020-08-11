using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightControlWeb.Models
{
    public class FlightsManager : IFlightsManager
    {

        // Singleton concuurent dictionaries for flight plans, servers and plans id's from servers
        private static ConcurrentDictionary<string, FlightPlan> flightPlans;
        private static ConcurrentDictionary<string, Server> servers;
        //maps from flight plan id to server id
        private static ConcurrentDictionary<string, string> idFromServers;

        // Constructs manager with dependency injection
        public FlightsManager(ConcurrentDictionary<string, FlightPlan> flightPlanDict,
            ConcurrentDictionary<string, Server> serversDict,
            ConcurrentDictionary<string, string> idFromServersDict)
        {
            flightPlans = flightPlanDict;
            servers = serversDict;
            idFromServers = idFromServersDict;
        }

        // Gets flight plan from server if key exists in plans dictionary, else if it's 
        // inside the plans returned from other servers gets the from the specific server.
        public async Task<FlightPlan> GetFlightPlan(string key)
        {
            FlightPlan plan = null;
            if (flightPlans.ContainsKey(key))
            {
                plan = flightPlans[key];
            }
            else if (idFromServers.ContainsKey(key))
            {
                var server = servers[idFromServers[key]];
                plan = await GetFlightPlanFromServer(key, server);
            }
            return plan;
        }

        // Insert flight plan to dictionary
        public string InsertFlightPlan(FlightPlan flightPlan)
        {
            if (!IsValidFlightPlan(flightPlan))
            {
                throw new Exception("Not a valid flight plan");
            }
            // Checks if date and tine are in format and throws execption if not.
            DateTime.Parse(flightPlan.Location.DateTime);
            string flightId = null;
            // Tries 30 times to give a unique id that is not in dictionary.
            int numOfTries = 30;
            int i = 0;
            for (; i < numOfTries; i++)
            {
                flightId = MakeUniqueId();
                if (!flightPlans.ContainsKey(flightId))
                {
                    flightPlans[flightId] = flightPlan;
                    break;
                }
            }
            if (i == numOfTries)
            {
                flightId = null;
            }
            return flightId;
        }

        // Returns true if it's a valid flightplan
        private bool IsValidFlightPlan(FlightPlan flightPlan)
        {
            // Checks if it's null or it's fields or if longitude and latitude not in valid range.
            if (flightPlan == null || flightPlan.CompanyName == null
                || flightPlan.Location == null || flightPlan.Location.DateTime == null ||
                flightPlan.Segments == null ||
                !IsValidLonLat(flightPlan.Location.Longitude, flightPlan.Location.Latitude))
            {
                return false;
            }
            // Goes over all segments in the flight plan and checks if they ara valid.
            foreach (var segment in flightPlan.Segments)
            {
                if (!IsValidLonLat(segment.Longitude, segment.Latitude) ||
                    segment.TimeSpanSeconds <= 0)
                {
                    return false;
                }
            }
            return true;
        }


        // Makes 8 characters unique id.
        private string MakeUniqueId()
        {
            var random = new Random();
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string numbers = "0123456789";
            string id = "";
            for (int i = 0; i < 2; i++)
            {
                id += characters[random.Next(characters.Length)];
            }
            for (int j = 0; j < 6; j++)
            {
                id += numbers[random.Next(numbers.Length)];
            }
            return id;
        }

        // Delete flight plan from DB.
        public string DeleteFlight(string id)
        {
            if (!flightPlans.ContainsKey(id))
            {
                return "not inside";
            }
            var plan = new FlightPlan();

            bool removed = flightPlans.Remove(id, out plan);
            if (removed)
            {
                return "success";
            }
            else
            {
                return "failed to remove";
            }
        }

        // Get all flights from server and if isExternal==true checks other servers connected to 
        // server too
        public async Task<IEnumerable<Flight>> GetAllFlights(string dateTime, bool isExternal)
        {

            var currentFlights = new List<Flight>();
            // get flights from server if is external
            if (isExternal)
            {
                var fromServers = await GetFlightsFromServers(dateTime);
                foreach (var flight in fromServers)
                {
                    flight.IsExternal = true;
                }
                currentFlights.AddRange(fromServers);
            }
            isExternal = false;

            // Converts the dateTime string to the type in UTC time.
            DateTime givenTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(dateTime));

            // goes over all flight plans and checks if flight is active at given time
            // if it's active put the flight with the current location in the list
            foreach (KeyValuePair<string, FlightPlan> idAndPlan in flightPlans)
            {
                var newFlight = AddFlightFromThisServer(idAndPlan, givenTime, isExternal);
                //it's a relevant flight for the time
                if (newFlight != null)
                {
                    currentFlights.Add(newFlight);
                }
            }
            return currentFlights;
        }

        // Adds flight from this server to be returned to client.
        private Flight AddFlightFromThisServer(KeyValuePair<string, FlightPlan> idAndPlan, DateTime givenTime, bool isExternal)
        {
            string initialTimeToParse = idAndPlan.Value.Location.DateTime;
            var initialTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(initialTimeToParse));
            int comparison = initialTime.CompareTo(givenTime);
            // Or time is at the beginning of flight or it's inside a running flight
            if (comparison == 0)
            {
                return new Flight(idAndPlan.Key, false, idAndPlan.Value);
            }
            else if (comparison < 0)
            {
                // get flight with the current location
                var flight = GetFlightWithCurrentLocation(initialTime, givenTime, idAndPlan.Value,
                    isExternal, idAndPlan.Key);
                if (flight != null)
                {
                    return flight;
                }
            }
            return null;
        }

        // Sets current location of the flight.
        private Flight GetFlightWithCurrentLocation(DateTime initialTime, DateTime givenTime,
            FlightPlan plan, bool isExternal, string id)
        {
            var initialLocation =
                new Tuple<double, double>(plan.Location.Longitude, plan.Location.Latitude);
            // go over all segments of flight and checks of it's inside it
            foreach (var segment in plan.Segments)
            {
                double seconds = segment.TimeSpanSeconds;
                var endLocation = new Tuple<double, double>(segment.Longitude, segment.Latitude);
                DateTime endTime = initialTime.AddSeconds(seconds);
                // if it's inside the segment get the current location according to time
                if (givenTime >= initialTime && givenTime < endTime)
                {
                    // Gets the current location.
                    var currentLocation = 
                        Interpolation(initialLocation, endLocation,
                        initialTime, givenTime, seconds);
                    var flight = new Flight(id, isExternal, plan)
                    {
                        Longitude = currentLocation.Item1,
                        Latitude = currentLocation.Item2
                    };
                    return flight;
                }
                initialTime = endTime;
                initialLocation = endLocation;
            }
            return null;
        }

        private Tuple<double, double> Interpolation(Tuple<double, double> firstLocation,
            Tuple<double, double> secondLocation, DateTime begin, DateTime now, double totalSeconds)
        {
            if (firstLocation.Equals(secondLocation))
            {
                return firstLocation;
            }
            // time from beginning to given time
            var difference = now.Subtract(begin);
            // Relative time difference.
            double relativeDifference = difference.TotalSeconds / totalSeconds;

            // Calculates the wanted longitude and latitude from initial location
            //(according to relative differnece of time)
            double wantedLongitude = firstLocation.Item1 + (secondLocation.Item1 - firstLocation.Item1)
                * relativeDifference;
            double wantedLatitude = firstLocation.Item2 + (secondLocation.Item2 - firstLocation.Item2)
                            * relativeDifference;

            return new Tuple<double, double>(wantedLongitude, wantedLatitude);
        }

        // Gets all servers' url from DB.
        public IEnumerable<Server> GetAllServers()
        {
            return servers.Values.AsEnumerable();
        }

        // Inserts new server to DB.
        public string InsertServer(Server server)
        {
            if (!servers.ContainsKey(server.ServerId))
            {
                servers[server.ServerId] = server;
                return "Success";
            }
            return "ID Already inside";
        }

        // Deletes server from DB (if exists).
        public string DeleteServer(string id)
        {
            var server = new Server();
            bool removed = servers.Remove(id, out server);
            if (removed)
            {
                return "Success";
            }
            else
            {
                return "Not Inside";
            }
        }

        // Get flight plan from specific server with http request.
        private async Task<FlightPlan> GetFlightPlanFromServer(string planId, Server server)
        {
            FlightPlan flightPlan = null;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None
            };

            dynamic response = await MakeRequest(server.ServerURL +
                "/api/FlightPlan/" + planId);
            if (response != null)
            {
                flightPlan = MakeFlightPlanFromJson(response);
            }
            return flightPlan;
        }

        // Gets the flights from all servers.
        private async Task<List<Flight>> GetFlightsFromServers(string relativeTo)
        {
            var serversFlights = new List<Flight>();
            var flightsFromServer = new List<Flight>();
            foreach (Server server in servers.Values)
            {
                try
                {
                    flightsFromServer = await GetFlightsFromServer(relativeTo, server);
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (flightsFromServer != null && flightsFromServer.Count > 0)
                    {
                        serversFlights.AddRange(flightsFromServer);
                        flightsFromServer.Clear();
                    }
                }
            }
            return serversFlights;
        }

        // Get flights from specific server (or returns null if connection didn't succeed).
        private async Task<List<Flight>> GetFlightsFromServer(string relativeTo, Server server)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None
            };
            var flights = new List<Flight>();
            try
            {
                dynamic response = await MakeRequest(server.ServerURL +
                "/api/Flights?relative_to=" + relativeTo);

                if (response == null)
                {
                    return flights;
                }
                string responseStr = response.ToString();
                //doesn't have any flights
                if (!responseStr.Contains("flight_id"))
                {
                    return flights;
                }

                foreach (var item in response)
                {
                    AddNewFlightFromOtherServers(flights, item, server.ServerId);
                }
                return flights;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Gets new flight from servers as json elements, parse them, put them in the flights
        // list to be returned to client and add them to idFromServers dictionary.
        private void AddNewFlightFromOtherServers(List<Flight> flights, JToken jsonFlight,
            string serverId)
        {
            var newFlight = MakeFlightFromJson(jsonFlight);
            if (newFlight != null && !flightPlans.ContainsKey(newFlight.FlightId))
            {
                flights.Add(newFlight);
                if (!idFromServers.ContainsKey(newFlight.FlightId))
                {
                    idFromServers.TryAdd(newFlight.FlightId, serverId);
                }
            }
        }

        // Make a flight object from json.
        private Flight MakeFlightFromJson(JToken flight)
        {
            if (flight == null)
            {
                return null;
            }
            try
            {
                int passengers = (int)flight["passengers"];
                string flightId = (string)flight["flight_id"];
                double longitude = (double)flight["longitude"];
                double latitude = (double)flight["latitude"];
                string companyName = (string)flight["company_name"];
                string dateTime = (string)flight["date_time"];
                bool isExternal = (bool)flight["is_external"];
                // if it's not a valid date and time throws exception

                if (flightId == null || companyName == null || dateTime == null ||
                   !IsValidLonLat(longitude, latitude))
                {
                    return null;
                }
                // throws exception if not a valid date time
                DateTime.Parse(dateTime);
                return new Flight(flightId, longitude, latitude, passengers,
                    companyName, dateTime, isExternal);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Checks if longitude and latitude are in valid range.
        public bool IsValidLonLat(double longitude, double latitude)
        {
            //latitude values (Y-values) range between -90 and +90 degrees.
            return longitude >= -180 && longitude <= 180 && latitude >= -90 && latitude <= 90;
        }

        // Make flight plan from json if it's valid.
        private FlightPlan MakeFlightPlanFromJson(JToken flightPlan)
        {
            if(flightPlan == null)
            {
                return null;
            }
            int passengers = (int)flightPlan["passengers"];
            string companyName = (string)flightPlan["company_name"];
            double longitude = (double)flightPlan["initial_location"]["longitude"];
            double latitude = (double)flightPlan["initial_location"]["latitude"];
            string dateTime = (string)flightPlan["initial_location"]["date_time"];

            if (companyName == null || dateTime == null)
            {
                return null;
            }

            // if it's not a valid date and time throws exception
            DateTime.Parse(dateTime);
            var location = new InitialLocation(longitude, latitude, dateTime);
            var jsonSegments = (JArray)flightPlan["segments"];
            var segments = new List<Segment>();
            foreach (var segment in jsonSegments)
            {
                double longitudeSegment = (double)segment["longitude"];
                double latitudeSegment = (double)segment["latitude"];
                double timeSpan = (double)segment["timespan_seconds"];
                if (!IsValidLonLat(longitudeSegment, latitudeSegment) || timeSpan <= 0)
                {
                    return null;
                }
                var newSegment = new Segment(longitudeSegment, latitudeSegment, timeSpan);
                segments.Add(newSegment);
            }

            return new FlightPlan(passengers, companyName, location, segments);
        }

        // Make http request with specific url.
        private async Task<dynamic> MakeRequest(string url)
        {
            var client = new HttpClient
            {
                //check if throws timeout exception
                Timeout = TimeSpan.FromSeconds(20)
            };
            var result = await client.GetStringAsync(url);
            dynamic json = JsonConvert.DeserializeObject(result);
            return json;
        }
    }
}
