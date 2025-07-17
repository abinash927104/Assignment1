namespace WorkflowStateMachineApi.Models;

public record WorkflowAction
{
    public string Id { get; init; }
    public bool Enabled { get; init; } = true;
    public List<string> FromStates { get; init; } = new();
    public string ToState { get; init; }
}
