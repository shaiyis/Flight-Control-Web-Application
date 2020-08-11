using System;
using System.Net.Http;
using System.Threading.Tasks;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightPlanController : ControllerBase
    {

        private readonly IFlightsManager manager;

        public FlightPlanController(IFlightsManager manager)
        {
            this.manager = manager;
        }

        // GET: api/FlightPlans/5 -   gets flight plan from DB or from servers we're connected to.
        [HttpGet("{id}", Name = "GetFlightPlan")]
        public async Task<ActionResult<FlightPlan>> GetFlightPlan(string id)
        {
            try
            {
                var plan = await manager.GetFlightPlan(id);
                // If we didn't find the plan we return Not Found
                if (plan == null)
                {
                    return NotFound("Didn't find the flight selected");
                }
                // Returns the plan
                return Ok(plan);
            }
            // If exceptions has been thrown due to ussie with http connection - 
            // returns bad request.
            catch (HttpRequestException)
            {
                return BadRequest("Problem with http request to other servers");
            }
            // Timeout - the servers connected to this server didn't bring the id fast enough.
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // POST: api/FlightPlans - add flight plan to DB
        [HttpPost]
        public ActionResult<string> AddFlightPlan([FromBody] FlightPlan plan)
        {
            try
            {
                // Returns id that DB gave the plan or null if it's already inside.
                string id = manager.InsertFlightPlan(plan);
                
                if (id != null)
                {
                    return Ok(id);
                }
                else
                {
                    // Had id in the dictionary already 
                    // (after trying to give a unique id for 30 times)
                    return BadRequest("Had id in the dictionary already. Try again!");
                }
            }
            // If date and time not in format exception has thrown and we return bad request.
            catch (FormatException)
            {
                return BadRequest("Date and time not in format");
            }
            // If any other exception is thrown we return bad request with a message.
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }
    }
}
