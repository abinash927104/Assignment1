# Workflow State Machine API

A minimal .NET 8 backend service for defining and running configurable workflow state machines.

## Quick Start

1. **Build and run:**
   ```sh
   dotnet run
   ```
   The API will start on http://localhost:5000 (or as configured).

2. **No database required:**
   All data is stored in memory and lost on restart.

---

## API Overview

### 1. Create a Workflow Definition

- **POST** `/definitions`
- **Body:**
```json
{
  "id": "leave-approval",
  "states": [
    { "id": "draft", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "submitted", "isInitial": false, "isFinal": false, "enabled": true },
    { "id": "approved", "isInitial": false, "isFinal": true, "enabled": true },
    { "id": "rejected", "isInitial": false, "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "submit", "enabled": true, "fromStates": ["draft"], "toState": "submitted" },
    { "id": "approve", "enabled": true, "fromStates": ["submitted"], "toState": "approved" },
    { "id": "reject", "enabled": true, "fromStates": ["submitted"], "toState": "rejected" }
  ]
}
```
- **Sample curl:**
```sh
curl -X POST http://localhost:5000/definitions \
  -H "Content-Type: application/json" \
  -d @definition.json
```

### 2. List Workflow Definitions
- **GET** `/definitions`

### 3. Get a Workflow Definition
- **GET** `/definitions/{id}`

### 4. Start a Workflow Instance
- **POST** `/instances`
- **Body:**
```json
{ "definitionId": "leave-approval" }
```
- **Sample curl:**
```sh
curl -X POST http://localhost:5000/instances \
  -H "Content-Type: application/json" \
  -d '{"definitionId":"leave-approval"}'
```

### 5. List Workflow Instances
- **GET** `/instances`

### 6. Get a Workflow Instance
- **GET** `/instances/{id}`

### 7. Execute an Action on an Instance
- **POST** `/instances/{instanceId}/actions/{actionId}`
- **Sample curl:**
```sh
curl -X POST http://localhost:5000/instances/{instanceId}/actions/submit
```

### 8. List States or Actions for a Definition
- **GET** `/definitions/{id}/states`
- **GET** `/definitions/{id}/actions`

---

## More Sample Requests

### 1. Create a Workflow Definition (inline JSON)
```sh
curl -X POST http://localhost:5000/definitions \
  -H "Content-Type: application/json" \
  -d '{
    "id": "onboarding",
    "states": [
      { "id": "start", "isInitial": true, "isFinal": false, "enabled": true },
      { "id": "in-progress", "isInitial": false, "isFinal": false, "enabled": true },
      { "id": "completed", "isInitial": false, "isFinal": true, "enabled": true }
    ],
    "actions": [
      { "id": "begin", "enabled": true, "fromStates": ["start"], "toState": "in-progress" },
      { "id": "finish", "enabled": true, "fromStates": ["in-progress"], "toState": "completed" }
    ]
  }'
```

### 2. Get All Workflow Definitions
```sh
curl http://localhost:5000/definitions
```

### 3. Get a Specific Workflow Definition
```sh
curl http://localhost:5000/definitions/onboarding
```

### 4. Start a Workflow Instance
```sh
curl -X POST http://localhost:5000/instances \
  -H "Content-Type: application/json" \
  -d '{"definitionId":"onboarding"}'
```
**Response:**
```json
{
  "id": "b1c2d3e4-5678-1234-9abc-def012345678",
  "definitionId": "onboarding",
  "currentState": "start",
  "history": []
}
```

### 5. List All Workflow Instances
```sh
curl http://localhost:5000/instances
```

### 6. Get a Workflow Instance (with history)
```sh
curl http://localhost:5000/instances/b1c2d3e4-5678-1234-9abc-def012345678
```
**Response:**
```json
{
  "id": "b1c2d3e4-5678-1234-9abc-def012345678",
  "definitionId": "onboarding",
  "currentState": "start",
  "history": []
}
```

### 7. Execute an Action (move to next state)
```sh
curl -X POST http://localhost:5000/instances/b1c2d3e4-5678-1234-9abc-def012345678/actions/begin
```
**Response:**
```json
{
  "id": "b1c2d3e4-5678-1234-9abc-def012345678",
  "definitionId": "onboarding",
  "currentState": "in-progress",
  "history": [
    { "actionId": "begin", "timestamp": "2024-06-07T12:34:56.789Z" }
  ]
}
```

### 8. List States for a Definition
```sh
curl http://localhost:5000/definitions/onboarding/states
```

### 9. List Actions for a Definition
```sh
curl http://localhost:5000/definitions/onboarding/actions
```

### 10. Error Example: Try to execute a disabled action
Suppose you have an action with `"enabled": false`:
```sh
curl -X POST http://localhost:5000/instances/b1c2d3e4-5678-1234-9abc-def012345678/actions/disabledAction
```
**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Action 'disabledAction' is disabled."
}
```

---

## Model Reference

### WorkflowDefinition
- `id`: string
- `states`: array of State
- `actions`: array of WorkflowAction

### State
- `id`: string
- `isInitial`: bool
- `isFinal`: bool
- `enabled`: bool

### WorkflowAction
- `id`: string
- `enabled`: bool
- `fromStates`: array of state IDs
- `toState`: string (state ID)

### WorkflowInstance
- `id`: string
- `definitionId`: string
- `currentState`: string (state ID)
- `history`: array of HistoryEntry

### HistoryEntry
- `actionId`: string
- `timestamp`: string (ISO 8601)

---

## Assumptions & Limitations
- **In-memory only:** All data is lost on restart.
- **No authentication or authorization.**
- **No concurrency handling.**
- **No incremental editing of definitions.**
- **Minimal error handling for brevity.**
- **No UI provided.**

---

## Example Usage Flow
1. Create a workflow definition (see above).
2. Start an instance:
   ```sh
   curl -X POST http://localhost:5000/instances \
     -H "Content-Type: application/json" \
     -d '{"definitionId":"leave-approval"}'
   ```
3. Execute an action (e.g., submit):
   ```sh
   curl -X POST http://localhost:5000/instances/{instanceId}/actions/submit
   ```
4. Inspect the instance:
   ```sh
   curl http://localhost:5000/instances/{instanceId}
   ```

---

## Project Structure
- `Program.cs` — Main API logic and endpoints
- `Models/` — Data models for workflows, states, actions, instances, and history

---

## Contact
For questions or feedback, please contact the author. 