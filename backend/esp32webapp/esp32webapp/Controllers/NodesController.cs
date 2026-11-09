using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esp32webapp.Data;
using esp32webapp.Models;

namespace esp32webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly AppDbContext _db;

    public NodesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/nodes
    // Zoznam všetkých uzlov, ich typ, stav online/offline a čas poslednej správy.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Node>>> GetAll()
    {
        var nodes = await _db.Nodes
            .OrderBy(n => n.NodeId)
            .ToListAsync();

        return Ok(nodes);
    }
}