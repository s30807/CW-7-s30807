using BiuroPodrozy.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiuroPodrozy.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IDbService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        return Ok(await service.GetAllTripsAsync());
    }
}