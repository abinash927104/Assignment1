namespace WorkflowStateMachineApi.Models;

public record HistoryEntry
{
    public string ActionId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
