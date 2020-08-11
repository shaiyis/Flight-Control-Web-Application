using System.Collections.Generic;
using System.Threading.Tasks;


namespace FlightControlWeb.Models
{
    public interface IFlightsManager
    {
        // Gets flight plan from DB by id.
        Task<FlightPlan> GetFlightPlan(string key);

        // Inserts new Flight plan to DB.
        string InsertFlightPlan(FlightPlan flightPlan);

        // Deletes flight (plan) from DB.
        string DeleteFlight(string id);

        // Get all flights from DB.
        Task<IEnumerable<Flight>> GetAllFlights(string dateTime, bool isExternal);

        // Get all Server urls from DB.
        IEnumerable<Server> GetAllServers();

        // Insert new Server to DB.
        string InsertServer(Server server);

        // Deletes Server from DB by id.
        string DeleteServer(string id);
    }
}
