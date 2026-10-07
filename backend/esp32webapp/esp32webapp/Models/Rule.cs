namespace esp32webapp.Models;

public class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string ConditionSensor { get; set; } = "";
    public string ConditionOperator { get; set; } = "";
    public double ConditionValue { get; set; }
    public string ActionTopic { get; set; } = "";
    public string ActionPayload { get; set; } = "";
    public bool IsActive { get; set; } = true;
}