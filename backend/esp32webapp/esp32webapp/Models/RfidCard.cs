using System.ComponentModel.DataAnnotations;

namespace esp32webapp.Models;

public class RfidCard
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string Uid { get; set; } = "";

    public string OwnerName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}