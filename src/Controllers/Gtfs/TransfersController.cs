using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class TransfersController : ControllerBase
{
    private readonly ITransfersService _transfersService;

    public TransfersController(ITransfersService transfersService)
    {
        _transfersService = transfersService;
    }

    [HttpGet("transfers")]
    public async Task<ActionResult<List<Transfer>>> GetAll()
    {
        return await _transfersService.GetAllAsync();
    }

    [HttpGet("transfers/from/{fromStopId}")]
    public async Task<ActionResult<List<Transfer>>> GetByFromStopId(string fromStopId)
    {
        List<Transfer>? tranfers = await _transfersService.GetByFromStopIdAsync(fromStopId);

        if (tranfers == null)
            return NotFound();

        return tranfers;
    }
}