namespace WorkflowStateMachineApi.Models;

public record State
{
    public string Id { get; init; }
    public bool IsInitial { get; init; }
    public bool IsFinal { get; init; }
    public bool Enabled { get; init; } = true;
}
