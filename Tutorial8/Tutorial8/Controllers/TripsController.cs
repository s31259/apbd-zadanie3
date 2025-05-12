using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetAllTripsAsync()
        {
            var trips = await _tripsService.GetAllTripsAsync();
            return Ok(trips);
        }

        [HttpGet("clients/{id}/trips")]
        public async Task<IActionResult> GetClientTripsAsync(int id)
        {
            if (!await _tripsService.DoesClientExist(id))
            {
                return NotFound($"Client with given ID - {id} doesn't exist");
            }
            
            try
            {
                var trips = await _tripsService.GetClientTripsAsync(id);
                return Ok(trips);
            }
            catch (NotFoundException nfe)
            {
                return NotFound(nfe.Message);
            }
        }
        [HttpPost("clients")]
        public async Task<IActionResult> CreateNewClientAsync(ClientDTO clientDTO)
        {
            var clientId = await _tripsService.CreateNewClientAsync(clientDTO);
            return Created(Request.Path.Value ?? "api/clients", clientId);
        }

        [HttpPut("clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientOnTripAsync(int id, int tripId)
        {
            if (!await _tripsService.DoesClientExist(id))
            {
                return NotFound($"Client with given ID - {id} doesn't exist");
            }
            
            if (!await _tripsService.DoesTripExist(tripId))
            {
                return NotFound($"Trip with given ID - {tripId} doesn't exist");
            }

            try
            {
                await _tripsService.RegisterClientOnTripAsync(id, tripId);
                return NoContent();
            }
            catch (ConflictException ce)
            {
                return Conflict(ce.Message);
            }
        }
        
        [HttpDelete("clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientRegistrationAsync(int id, int tripId)
        {
            if (!await _tripsService.DoesRegistrationExist(id, tripId))
            {
                return NotFound($"Registration with given IDs: Client - {id}, Trip - {tripId} doesn't exist");
            }
            
            await _tripsService.DeleteClientRegistrationAsync(id, tripId);
            return NoContent();
        }
    }
}
