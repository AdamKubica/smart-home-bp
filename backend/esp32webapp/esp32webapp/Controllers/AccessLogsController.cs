using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esp32webapp.Data;

namespace esp32webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AccessLogsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/accesslogs?count=50
    // Posledné pokusy o prístup cez RFID aj s menom majiteľa karty.
    [HttpGet]
    public async Task<IActionResult> GetLatest([FromQuery] int count = 50)
    {
        if (count < 1 || count > 500)
            return BadRequest("Parameter count musí byť v rozsahu 1 až 500.");

        var logs = await _db.AccessLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .Select(a => new
            {
                a.Id,
                a.NodeId,
                a.Uid,
                Owner = _db.RfidCards
                    .Where(c => c.Uid == a.Uid)
                    .Select(c => c.OwnerName)
                    .FirstOrDefault(),
                a.Granted,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }
}