namespace esp32webapp.Models;

public class AccessLog
{
    public int Id { get; set; }
    public string NodeId { get; set; } = "";
    public string Uid { get; set; } = "";
    public bool Granted { get; set; }
    public DateTime Timestamp { get; set; }
}