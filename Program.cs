using Microsoft.AspNetCore.Mvc;
using WorkflowStateMachineApi.Models;

// In-memory workflow state machine API for demo/interview. See README for details.
// Models are defined in WorkflowStateMachineApi.Models
// No persistence; all data is lost on restart.
//
// Endpoints:
//   - POST   /definitions         : Create a workflow definition
//   - GET    /definitions/{id}    : Get a workflow definition
//   - GET    /definitions         : List all definitions
//   - POST   /instances           : Start a workflow instance
//   - POST   /instances/{iid}/actions/{aid} : Execute action on instance
//   - GET    /instances           : List all instances
//   - GET    /instances/{id}      : Get instance details
//   - GET    /definitions/{id}/states  : List states for a definition
//   - GET    /definitions/{id}/actions : List actions for a definition
//
// Assumptions:
//   - No authentication
//   - No concurrency handling
//   - No incremental definition editing
//   - Minimal error handling for brevity
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory stores for workflow definitions and instances
var workflowDefinitions = new Dictionary<string, WorkflowDefinition>();
var workflowInstances = new Dictionary<string, WorkflowInstance>();

// Create a new workflow definition (with validation)
app.MapPost("/definitions", ([FromBody] WorkflowDefinition def) =>
{
    // Validate required fields
    if (string.IsNullOrWhiteSpace(def.Id))
        return Results.BadRequest("Definition Id is required.");

    if (workflowDefinitions.ContainsKey(def.Id))
        return Results.BadRequest($"Definition '{def.Id}' already exists.");

    if (def.States == null || def.States.Count == 0)
        return Results.BadRequest("At least one state is required.");

    if (def.Actions == null)
        def.Actions = new List<WorkflowAction>();

    // Ensure state IDs are unique
    if (def.States.Select(s => s.Id).Distinct().Count() != def.States.Count)
        return Results.BadRequest("Duplicate state IDs are not allowed.");

    // Must have exactly one initial state
    if (def.States.Count(s => s.IsInitial) != 1)
        return Results.BadRequest("There must be exactly one initial state.");

    // Validate actions reference valid states
    foreach (var action in def.Actions)
    {
        if (!def.States.Any(s => s.Id == action.ToState))
            return Results.BadRequest($"Action '{action.Id}' targets unknown state '{action.ToState}'.");

        foreach (var from in action.FromStates)
        {
            if (!def.States.Any(s => s.Id == from))
                return Results.BadRequest($"Action '{action.Id}' has unknown source state '{from}'.");
        }
    }

    workflowDefinitions[def.Id] = def;
    return Results.Created($"/definitions/{def.Id}", def);
});

// Retrieve a workflow definition by ID
app.MapGet("/definitions/{id}", (string id) =>
{
    if (!workflowDefinitions.TryGetValue(id, out var def))
        return Results.NotFound();
    return Results.Ok(def);
});

// List all workflow definitions
app.MapGet("/definitions", () => Results.Ok(workflowDefinitions.Values));

// Start a new workflow instance for a given definition
app.MapPost("/instances", ([FromBody] StartInstanceRequest req) =>
{
    if (!workflowDefinitions.TryGetValue(req.DefinitionId, out var def))
        return Results.NotFound($"Definition '{req.DefinitionId}' not found.");

    var initialState = def.States.Single(s => s.IsInitial);
    var instanceId = Guid.NewGuid().ToString();
    var instance = new WorkflowInstance
    {
        Id = instanceId,
        DefinitionId = req.DefinitionId,
        CurrentState = initialState.Id
    };
    workflowInstances[instanceId] = instance;
    return Results.Created($"/instances/{instanceId}", instance);
});

// Execute an action on a workflow instance (with validation)
app.MapPost("/instances/{instanceId}/actions/{actionId}", (string instanceId, string actionId) =>
{
    if (!workflowInstances.TryGetValue(instanceId, out var inst))
        return Results.NotFound();

    if (!workflowDefinitions.TryGetValue(inst.DefinitionId, out var def))
        return Results.Problem($"Definition '{inst.DefinitionId}' not found.", statusCode: 500);

    var action = def.Actions.FirstOrDefault(a => a.Id == actionId);
    if (action == null)
        return Results.BadRequest($"Action '{actionId}' not found in this workflow.");

    if (!action.Enabled)
        return Results.BadRequest($"Action '{actionId}' is disabled.");

    var currentState = def.States.First(s => s.Id == inst.CurrentState);
    if (currentState.IsFinal)
        return Results.BadRequest("Cannot execute action: instance is in a final state.");

    if (!action.FromStates.Contains(inst.CurrentState))
        return Results.BadRequest($"Action '{actionId}' is not allowed from state '{inst.CurrentState}'.");

    var nextState = def.States.FirstOrDefault(s => s.Id == action.ToState);
    if (nextState == null)
        return Results.BadRequest($"Target state '{action.ToState}' not found.");

    inst.CurrentState = nextState.Id;
    inst.History.Add(new HistoryEntry { ActionId = action.Id });
    return Results.Ok(inst);
});

// List all workflow instances
app.MapGet("/instances", () => Results.Ok(workflowInstances.Values));

// Get details of a workflow instance
app.MapGet("/instances/{id}", (string id) =>
{
    if (!workflowInstances.TryGetValue(id, out var inst))
        return Results.NotFound();
    return Results.Ok(inst);
});

// List states for a workflow definition
app.MapGet("/definitions/{id}/states", (string id) =>
{
    if (!workflowDefinitions.TryGetValue(id, out var def))
        return Results.NotFound();
    return Results.Ok(def.States);
});

// List actions for a workflow definition
app.MapGet("/definitions/{id}/actions", (string id) =>
{
    if (!workflowDefinitions.TryGetValue(id, out var def))
        return Results.NotFound();
    return Results.Ok(def.Actions);
});

app.Run();

// Minimal record for starting an instance
record StartInstanceRequest
{
    public string DefinitionId { get; init; }
}
