using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using esp32webapp.Data;
using esp32webapp.Models;

namespace esp32webapp.Services;

public class MqttListenerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMqttClient _client;
    private readonly ILogger<MqttListenerService> _logger;

    public MqttListenerService(IServiceScopeFactory scopeFactory, ILogger<MqttListenerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _client = new MqttFactory().CreateMqttClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("test.mosquitto.org", 1883)
            .WithClientId("dotnet-backend-aa-bp")
            .Build();

        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;

        // Ak spojenie spadne alebo sa nepodarí, skúšame to znova každých 5 sekúnd
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_client.IsConnected)
            {
                try
                {
                    await _client.ConnectAsync(options, stoppingToken);
                    _logger.LogInformation("MQTT pripojený");

                    await _client.SubscribeAsync("dom/#", cancellationToken: stoppingToken);
                    _logger.LogInformation("Odoberám tému dom/#");
                }
                catch (OperationCanceledException)
                {
                    break; // aplikácia sa vypína
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Pripojenie k MQTT zlyhalo: {Sprava}. Skúsim znova o 5 s.", ex.Message);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;
        string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("Prišlo [{Topic}]: {Payload}", topic, payload);

        // očakávame dom/{uzol}/{typ}; príkazy a odpovede (4 časti) ignorujeme
        var parts = topic.Split('/');
        if (parts.Length != 3 || parts[0] != "dom") return;

        string nodeId = parts[1];
        string type = parts[2];

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (type == "rfid")
        {
            await HandleRfidAsync(db, nodeId, payload.Trim());
            return;
        }

        if (!double.TryParse(payload, CultureInfo.InvariantCulture, out double value))
            return;

        db.SensorReadings.Add(new SensorReading
        {
            NodeId = nodeId,
            SensorType = type,
            Value = value,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task HandleRfidAsync(AppDbContext db, string nodeId, string uid)
    {
        var card = await db.RfidCards.FirstOrDefaultAsync(c => c.Uid == uid);
        bool granted = card != null && card.IsActive;

        db.AccessLogs.Add(new AccessLog
        {
            NodeId = nodeId,
            Uid = uid,
            Granted = granted,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = new MqttApplicationMessageBuilder()
            .WithTopic($"dom/{nodeId}/rfid/odpoved")
            .WithPayload(granted ? "OK" : "DENY")
            .Build();
        await _client.PublishAsync(response);

        _logger.LogInformation("RFID {Uid} na uzle {Node}: {Vysledok}",
            uid, nodeId, granted ? "POVOLENÉ" : "ZAMIETNUTÉ");
    }
}