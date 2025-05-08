using BiuroPodrozy.Exceptions;
using BiuroPodrozy.Models.DTOs;
using BiuroPodrozy.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiuroPodrozy.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService service) : ControllerBase
{
    [HttpGet("{id}/trips")]    //Ten endpoint będzie pobierał wszystkie wycieczki powiązane z konkretnym klientem
    public async Task<IActionResult> GetTripsByClientId(
        [FromRoute] int id
    )
    {
        try
        {
            return Ok(await service.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost]  //Ten endpoint utworzy nowy rekord klienta
    public async Task<IActionResult> CreateClient(
        [FromBody] ClientCreateDTO body
    )
    {
        var client = await service.CreateClientAsync(body);
        return Created($"clients/{client.IdClient}", client);
    }

    [HttpPut("{clientid}/trips/{tripId}")]  // Ten endpoint zarejestruje klienta na konkretną wycieczkę.
    public async Task<IActionResult> RegisterClientToTrip(
        [FromRoute] int clientId, [FromRoute] int tripId
    )
    {
        try
        {
            var result = await service.RegisterClientToTripAsync(clientId, tripId);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpDelete("{id}/trips/{tripId}")] //Ten endpoint usunie rejestrację klienta z wycieczki.
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        try
        {
            await service.UnregisterClientFromTripAsync(id, tripId);
            return Ok($"Klient {id} został wypisany z wycieczki {tripId}.");
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}