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
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("test.mosquitto.org", 1883)
            .WithClientId("dotnet-backend")
            .Build();

        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;

        await _client.ConnectAsync(options, stoppingToken);
        _logger.LogInformation("MQTT pripojený");

        await _client.SubscribeAsync("dom/#", cancellationToken: stoppingToken);
        _logger.LogInformation("Odoberám tému dom/#");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;
        string payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("Prišlo [{Topic}]: {Payload}", topic, payload);

        var parts = topic.Split('/');
        if (parts.Length < 3 || parts[0] != "dom") return;

        string nodeId = parts[1];
        string sensorType = parts[2];

        if (parts.Length > 3) return;

        if (!double.TryParse(payload, System.Globalization.CultureInfo.InvariantCulture, out double value))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.SensorReadings.Add(new SensorReading
        {
            NodeId = nodeId,
            SensorType = sensorType,
            Value = value,
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}