using System.Collections.Generic;
using System.Linq;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        private readonly IFlightsManager manager;

        public ServersController(IFlightsManager manager)
        {
            this.manager = manager;
        }


        // GET: api/Servers -  Get all servers from DB.
        [HttpGet]
        public ActionResult<IEnumerable<Server>> GetAllServers()
        {
            var servers = manager.GetAllServers();
            if (servers.Any())
            {
                return Ok(servers);
            }
            return NotFound("No servers in DB");
        }
        
        // POST: api/Servers - Insert new Server to DB.
        [HttpPost]
        public ActionResult<string> InsertNewServer([FromBody] Server server)
        {
            // Removes '/' from end of url (like http://localhost:5006/)
            string url = server.ServerURL;
            if (url.EndsWith('/'))
            {
                server.ServerURL = url.Remove(url.Length-1);
            }
            string response = manager.InsertServer(server);
            if(response == "Success")
            {
                return Ok(response);
            }
            // already inside
            return BadRequest(response);
        }


        // DELETE: api/ApiWithActions/5 -  Deletes Server if exists.
        [HttpDelete("{id}")]
        public ActionResult<string> DeleteServer(string id)
        {
            string response = manager.DeleteServer(id);
            if (response == "success")
            {
                return Ok(response);
            }
            // not inside
            return BadRequest(response);
        }
    }
}
