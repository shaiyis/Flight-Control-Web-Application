using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightsManager manager;

        public FlightsController(IFlightsManager manager)
        {
            this.manager = manager;
        }

        // GET: api/Flights?relative_to=<DATE_TIME> - get all flights fron DB 
        // if "sync_all" is in the request we get the flights from other servers connected to 
        // the server too.
        [HttpGet]       
        public async Task<ActionResult<IEnumerable<Flight>>> GetAllFlights
            ([FromQuery(Name = "relative_to")]string relativeTo)
        {
            // Checks if the words "sync_all" is in the query - checks other servers if it's true.
            string request = Request.QueryString.Value;
            bool isExternal = request.Contains("sync_all");
            IEnumerable<Flight> flights = null;
            try
            {
                flights = await manager.GetAllFlights(relativeTo, isExternal);
            }
            // Problem with connection to other servers.
            catch (HttpRequestException)
            {
                return BadRequest("problem in request to servers");
            }
            // Problem with date and time format.
            catch (FormatException)
            {
                return BadRequest("Date and time not in format");
            }
            catch (Exception e)
            {
                // Timeout - servers didn't bring the flights fast enough
                if(e.Message == "The operation was canceled.")
                {
                    return BadRequest("Timeout - server didn't bring the flights");
                }
                return BadRequest(e.Message);
            }
            // Returns list of flights from server/s.
            return Ok(flights);
        }

        // DELETE: api/ApiWithActions/5 - Deletes flight (plan) from the server. (if it exists).
        [HttpDelete("{id}")]
        public ActionResult<string> DeleteFlight(string id)
        {
            string answer = manager.DeleteFlight(id);
            if (answer == "success")
            {
                return Ok();
            }
            else
            {
                return NotFound("Flight is not in DB so can't be deleted");
            }
        }
    }
}
