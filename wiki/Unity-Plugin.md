# Unity Plugin

The Unity Editor plugin lives in `plugin/Editor/`. It's a UPM (Unity Package Manager) package that receives commands from the Python server and executes them against the Unity Editor API.

## Package Info

| Field | Value |
|-------|-------|
| **Name** | `com.coplay.unity-2021-mcp` |
| **Unity version** | 2021.3+ |
| **Dependency** | `com.unity.nuget.newtonsoft-json` |

## Key Components

### `TcpTransportClient.cs`

TCP listener that accepts connections from the Python server.

- Binds to `IPAddress.Loopback` (127.0.0.1 only)
- Generates a random auth token on startup
- Writes port + token to lock file at `~/.unity-mcp/unity-mcp-port-{pid}.json`
- Validates the first message from connecting clients against the token
- Reads JSON-line commands and fires `OnCommandReceived` events
- Thread-safe response sending via write lock

### `CommandDispatcher.cs`

Routes incoming commands to the correct handler.

- Receives raw JSON from the transport layer
- Queues execution on the Unity main thread (via `UnityMainThreadDispatcher`)
- Parses the `tool` and `params` fields from the JSON
- Calls `CommandRegistry.ExecuteCommand()`
- Sends success/error responses back

### `CommandRegistry.cs`

Auto-discovers and manages tool handlers.

- On editor load, scans all assemblies for classes with `[McpForUnityTool]` attribute
- Validates each class has a `public static object HandleCommand(JObject @params)` method
- Supports both sync and async handlers
- Provides `IsCommandRegistered()` and `GetRegisteredCommands()` for introspection

### `UnityMainThreadDispatcher.cs`

Marshals actions from background threads to Unity's main thread.

- Maintains a thread-safe queue of `Action` delegates
- Drains the queue during `EditorApplication.update`
- Required because Unity's API is not thread-safe

### `McpAutoStart.cs`

Handles automatic restart after domain reload (script compilation).

- Uses `[InitializeOnLoad]` to run on every domain reload
- Tracks "was running" state via `SessionState` (survives reloads, resets on Unity close)
- Automatically restarts the TCP listener without user intervention

### `McpServerManager.cs`

Shared lifecycle manager for the server components.

- Creates and manages `TcpTransportClient` and `CommandDispatcher`
- Provides `StartAsync()` / `StopAsync()` API
- Used by both `McpEditorWindow` and `McpAutoStart`

### `McpEditorWindow.cs`

The Editor window UI (**Window → MCP for Unity 2021**).

- Shows connection status with color indicator
- Port configuration (saved to `EditorPrefs`)
- Start/Stop buttons
- Debug logging toggle

### `McpLog.cs`

Logging facade wrapping `Debug.Log` with a `[MCP 2021]` prefix.

- `Error()` / `Warning()` — always logged
- `Info()` — only logged when `DebugEnabled` is true
- Debug toggle controlled from the Editor window

## Command Protocol

### Request (Python → Unity)

```json
{
  "id": "unique-request-id",
  "tool": "manage_gameobject",
  "params": {
    "action": "create",
    "name": "Player",
    "position": [0, 1, 0]
  }
}
```

### Success Response (Unity → Python)

```json
{
  "id": "unique-request-id",
  "success": true,
  "result": { ... }
}
```

### Error Response (Unity → Python)

```json
{
  "id": "unique-request-id",
  "success": false,
  "error": "Description of what went wrong"
}
```

## Tool Handler Utilities

### `ToolParams`

Helper class for reading parameters with snake_case ↔ camelCase conversion:

```csharp
var toolParams = new ToolParams(@params);
string action = toolParams.GetRequired<string>("action");  // throws if missing
string name = toolParams.Get<string>("name", "");          // default if missing
JToken raw = toolParams.GetRaw("position");                // raw JToken access
```

### `Pagination`

Generic pagination support for large result sets:

```csharp
var page = Pagination<T>.FromList(items, cursor: 0, pageSize: 50);
return page.ToJObject();
```

### `NamingUtility`

Converts between snake_case and camelCase for parameter name matching.

### `RenderPipelineUtility`

Detects the active render pipeline (Built-in, URP, HDRP) for material creation.

---

← [Python Server](Python-Server) | [Adding Custom Tools](Adding-Custom-Tools) →
