using BiuroPodrozy.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiuroPodrozy.Controllers;

[ApiController]
[Route("trips")]
public class TripsController(IDbService service) : ControllerBase
{
    [HttpGet]   //Ten endpoint będzie pobierał wszystkie dostępne wycieczki wraz z ich podstawowymi informacjami
    public async Task<IActionResult> GetTrips()
    {
        return Ok(await service.GetAllTripsAsync());
    }
}