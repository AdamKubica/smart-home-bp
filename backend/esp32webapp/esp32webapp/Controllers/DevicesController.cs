using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using esp32webapp.Services;

namespace esp32webapp.Controllers;

public record LightCommand(bool On);

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    // ID uzla smie obsahovať len písmená, čísla, '-' a '_'.
    // Znaky '/', '+' a '#' majú v MQTT témach špeciálny význam, preto ich nepustíme.
    private static readonly Regex NodeIdPattern = new("^[A-Za-z0-9_-]{1,32}$");

    private readonly MqttListenerService _mqtt;

    public DevicesController(MqttListenerService mqtt)
    {
        _mqtt = mqtt;
    }

    // POST api/devices/izba1/light
    // Telo: { "on": true }  -> zapne svetlo, { "on": false } -> vypne
    [HttpPost("{nodeId}/light")]
    public async Task<IActionResult> SetLight(string nodeId, [FromBody] LightCommand command)
    {
        if (!NodeIdPattern.IsMatch(nodeId))
            return BadRequest("Neplatné ID uzla.");

        string topic = $"dom/{nodeId}/svetlo/prikaz";
        string payload = command.On ? "ON" : "OFF";

        bool sent = await _mqtt.PublishAsync(topic, payload);
        if (!sent)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Backend nie je pripojený k MQTT brokeru.");

        return Ok(new { topic, payload });
    }
}