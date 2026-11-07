using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esp32webapp.Data;
using esp32webapp.Models;

namespace esp32webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SensorsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/sensors/latest
    // Posledná nameraná hodnota pre každý uzol a typ senzora.
    [HttpGet("latest")]
    public async Task<ActionResult<IEnumerable<SensorReading>>> GetLatest()
    {
        var latestIds = _db.SensorReadings
            .GroupBy(r => new { r.NodeId, r.SensorType })
            .Select(g => g.Max(r => r.Id));

        var latest = await _db.SensorReadings
            .Where(r => latestIds.Contains(r.Id))
            .OrderBy(r => r.NodeId).ThenBy(r => r.SensorType)
            .ToListAsync();

        return Ok(latest);
    }

    // GET api/sensors/izba1/teplota?hours=24
    // História meraní jedného senzora za posledných N hodín.
    [HttpGet("{nodeId}/{sensorType}")]
    public async Task<ActionResult<IEnumerable<SensorReading>>> GetHistory(
        string nodeId, string sensorType, [FromQuery] int hours = 24)
    {
        if (hours < 1 || hours > 24 * 30)
            return BadRequest("Parameter hours musí byť v rozsahu 1 až 720.");

        var from = DateTime.UtcNow.AddHours(-hours);

        var history = await _db.SensorReadings
            .Where(r => r.NodeId == nodeId && r.SensorType == sensorType && r.Timestamp >= from)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();

        return Ok(history);
    }
}