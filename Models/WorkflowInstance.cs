namespace WorkflowStateMachineApi.Models;

public class WorkflowInstance
{
    public string Id { get; set; }
    public string DefinitionId { get; set; }
    public string CurrentState { get; set; }
    public List<HistoryEntry> History { get; init; } = new();
}
