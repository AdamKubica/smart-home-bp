namespace esp32webapp.Models;

public class SensorReading
{
    public int Id { get; set; }
    public string NodeId { get; set; } = "";
    public string SensorType { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}