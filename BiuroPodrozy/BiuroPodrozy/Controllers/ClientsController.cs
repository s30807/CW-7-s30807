using BiuroPodrozy.Exceptions;
using BiuroPodrozy.Services;
using Microsoft.AspNetCore.Mvc;

namespace BiuroPodrozy.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService service) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClientById(
        [FromRoute] int id
    )
    {
        try
        {
            return Ok(await service.GetClientDetailsByIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}