using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esp32webapp.Data;
using esp32webapp.Models;

namespace esp32webapp.Controllers;

public record CardCreate(string Uid, string OwnerName, bool IsActive = true);
public record CardUpdate(string OwnerName, bool IsActive);

[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    // UID karty má 4, 7 alebo 10 bajtov zapísaných šestnástkovo, teda 8 až 20 znakov
    private static readonly Regex UidPattern = new("^[0-9A-F]{8,20}$");

    private readonly AppDbContext _db;

    public CardsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/cards
    // Zoznam všetkých kariet zoradený podľa mena majiteľa.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RfidCard>>> GetAll()
    {
        var cards = await _db.RfidCards
            .OrderBy(c => c.OwnerName)
            .ToListAsync();

        return Ok(cards);
    }

    // GET api/cards/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RfidCard>> Get(int id)
    {
        var card = await _db.RfidCards.FindAsync(id);
        if (card == null)
            return NotFound();

        return Ok(card);
    }

    // GET api/cards/unknown
    // Karty, ktoré niekto priložil k čítačke, ale v systéme nie sú.
    // Novú kartu tak stačí priložiť a jej UID sa objaví v tomto zozname.
    [HttpGet("unknown")]
    public async Task<IActionResult> GetUnknown()
    {
        var unknown = await _db.AccessLogs
            .Where(a => !_db.RfidCards.Any(c => c.Uid == a.Uid))
            .GroupBy(a => a.Uid)
            .Select(g => new
            {
                Uid = g.Key,
                LastSeen = g.Max(a => a.Timestamp),
                Attempts = g.Count()
            })
            .OrderByDescending(x => x.LastSeen)
            .ToListAsync();

        return Ok(unknown);
    }

    // POST api/cards
    // Telo: { "uid": "7A738DA5", "ownerName": "Adam", "isActive": true }
    [HttpPost]
    public async Task<ActionResult<RfidCard>> Create(CardCreate input)
    {
        string uid = input.Uid.Trim().ToUpperInvariant();

        if (!UidPattern.IsMatch(uid))
            return BadRequest("UID musí mať 8 až 20 šestnástkových znakov, napríklad 7A738DA5.");

        if (string.IsNullOrWhiteSpace(input.OwnerName))
            return BadRequest("Meno majiteľa je povinné.");

        if (await _db.RfidCards.AnyAsync(c => c.Uid == uid))
            return Conflict($"Karta {uid} už v systéme existuje.");

        var card = new RfidCard
        {
            Uid = uid,
            OwnerName = input.OwnerName.Trim(),
            IsActive = input.IsActive
        };

        _db.RfidCards.Add(card);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = card.Id }, card);
    }

    // PUT api/cards/5
    // Telo: { "ownerName": "Adam", "isActive": false } -> karta sa zablokuje
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RfidCard>> Update(int id, CardUpdate input)
    {
        if (string.IsNullOrWhiteSpace(input.OwnerName))
            return BadRequest("Meno majiteľa je povinné.");

        var card = await _db.RfidCards.FindAsync(id);
        if (card == null)
            return NotFound();

        card.OwnerName = input.OwnerName.Trim();
        card.IsActive = input.IsActive;
        await _db.SaveChangesAsync();

        return Ok(card);
    }

    // DELETE api/cards/5
    // Záznamy v logu prístupov ostanú, pamätajú si UID, nie odkaz na kartu.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var card = await _db.RfidCards.FindAsync(id);
        if (card == null)
            return NotFound();

        _db.RfidCards.Remove(card);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}