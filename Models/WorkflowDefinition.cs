namespace WorkflowStateMachineApi.Models;

public record WorkflowDefinition
{
    public string Id { get; init; }
    public List<State> States { get; init; } = new();
    public List<WorkflowAction> Actions { get; set; } = new();
}
