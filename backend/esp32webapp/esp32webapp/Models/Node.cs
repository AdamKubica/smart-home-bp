using System.ComponentModel.DataAnnotations;

namespace esp32webapp.Models;

public class Node
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string NodeId { get; set; } = "";

    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
}