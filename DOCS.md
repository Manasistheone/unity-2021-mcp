# Unity 2021 MCP — Documentation

## Overview

Unity 2021 MCP is a bridge between AI assistants and the Unity 2021.3 LTS Editor using the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/). It allows AI tools like Claude, Cursor, Kiro, and VS Code Copilot to create GameObjects, manage scenes, write scripts, and control the editor programmatically.

---

## Architecture

```
┌─────────────────────────────┐
│  MCP Client                 │
│  (Kiro / Claude / Cursor)   │
└──────────┬──────────────────┘
           │ stdio or HTTP/SSE
┌──────────▼──────────────────┐
│  Python MCP Server          │
│  (FastMCP 3.x)              │
│  server/src/unity_2021_mcp/ │
└──────────┬──────────────────┘
           │ TCP socket (port 8765)
           │ Authenticated with shared secret
┌──────────▼──────────────────┐
│  Unity Editor Plugin        │
│  (C# / IMGUI)              │
│  plugin/Editor/             │
└──────────┬──────────────────┘
           │ Unity Editor API
┌──────────▼──────────────────┐
│  Unity 2021.3 LTS           │
└─────────────────────────────┘
```

### Data Flow

1. The MCP client sends a tool call (e.g., `manage_gameobject`) to the Python server.
2. The Python server wraps it as a JSON command and sends it over TCP to the Unity plugin.
3. The Unity plugin dispatches the command to the appropriate tool handler on the main thread.
4. The handler executes Unity API calls and returns a result.
5. The result flows back through TCP → Python → MCP client.

### Security Model

- **TCP binding**: The Unity plugin listens on `127.0.0.1` only (loopback). No network exposure.
- **HTTP binding**: The Python server defaults to `127.0.0.1`. Configurable via `--host` for advanced use.
- **Authentication**: On startup, the Unity plugin generates a cryptographic random token and writes it to a lock file (`~/.unity-mcp/unity-mcp-port-{pid}.json`). The Python server reads this token and sends it as the first message after connecting. Unauthenticated connections are rejected.
- **No payload logging**: Debug logs only show message sizes, never raw content.

---

## Project Structure

```
unity-2021-mcp/
├── server/                          # Python MCP server
│   ├── pyproject.toml               # Package config, dependencies, entry point
│   ├── uv.lock                      # Locked dependency versions
│   ├── src/unity_2021_mcp/
│   │   ├── __init__.py              # Package version
│   │   ├── main.py                  # CLI entry point (mcp-for-unity-2021)
│   │   ├── server.py                # FastMCP server, tool registration
│   │   ├── logging_config.py        # Rotating file logger setup
│   │   ├── core/
│   │   │   └── config.py            # CLI argument parsing, ServerConfig
│   │   ├── transport/
│   │   │   └── unity_connection.py  # TCP client to Unity plugin
│   │   └── tools/
│   │       └── registry.py          # Command relay to Unity
│   └── tests/
│       ├── test_logging_config.py
│       └── test_placeholder.py
├── plugin/                          # Unity Editor plugin (UPM package)
│   ├── package.json                 # Unity Package Manager manifest
│   └── Editor/
│       ├── TcpTransportClient.cs    # TCP listener + auth handshake
│       ├── CommandDispatcher.cs     # Routes commands to handlers
│       ├── CommandRegistry.cs       # Auto-discovers [McpForUnityTool] classes
│       ├── McpServerManager.cs      # Shared server lifecycle
│       ├── McpAutoStart.cs          # Auto-restart after domain reload
│       ├── McpEditorWindow.cs       # Window > MCP for Unity 2021
│       ├── McpLog.cs                # Logging facade
│       ├── ManageEditorTool.cs      # Editor state tool
│       ├── ManageSceneTool.cs       # Scene management tool
│       ├── ManageGameObjectTool.cs  # GameObject CRUD tool
│       ├── FindGameObjectsTool.cs   # GameObject search tool
│       ├── ReadConsoleTool.cs       # Console output tool
│       ├── RefreshUnityTool.cs      # Asset refresh tool
│       └── (supporting utilities)
├── README.md                        # Quick start
├── SETUP.md                         # Detailed setup guide
└── DOCS.md                          # This file
```

---

## Python Server

### Entry Point

The CLI command `mcp-for-unity-2021` is defined in `pyproject.toml`:

```toml
[project.scripts]
mcp-for-unity-2021 = "unity_2021_mcp.main:main"
```

### CLI Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--transport` | `stdio` | Transport mode: `stdio` or `http` |
| `--host` | `127.0.0.1` | HTTP bind address (http mode only) |
| `--port` | `8080` | HTTP server port (http mode only) |
| `--unity-port` | `8765` | TCP port for Unity plugin connection |
| `--unity-host` | `127.0.0.1` | Host for Unity plugin connection |

### Key Modules

#### `server.py` — Tool Registration

Registers all MCP tools with FastMCP. Each tool maps to a Unity command:

```python
@mcp.tool()
async def manage_gameobject(action: str, name: str = "", ...):
    return await _relay_to_unity("manage_gameobject", {...})
```

#### `transport/unity_connection.py` — TCP Client

- Connects to the Unity plugin's TCP listener
- Auto-discovers port from lock files in `~/.unity-mcp/`
- Sends auth token as first message after connecting
- Sends JSON-line commands and reads JSON-line responses
- Thread-safe with asyncio lock

#### `logging_config.py` — Logging

- Rotating file handler: `~/.unity-mcp/logs/unity-2021-mcp.log`
- Max 512 KB per file, 2 backup files
- Console handler (stderr) for warnings and errors only
- Logger name: `unity_2021_mcp`

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `fastmcp` | >=3.0.0, <4 | MCP protocol implementation |
| `httpx` | >=0.27.0 | HTTP client (for future use) |
| `pydantic` | >=2.0.0 | Data validation |

Dev dependencies: `pytest`, `pytest-asyncio`, `hypothesis`

---

## Unity Plugin

### Package Info

- **Name**: `com.coplay.unity-2021-mcp`
- **Unity version**: 2021.3+
- **Dependency**: `com.unity.nuget.newtonsoft-json` (for JSON parsing)

### How Commands Are Dispatched

1. `TcpTransportClient` receives a JSON line from the Python server.
2. `CommandDispatcher.HandleCommand()` queues execution on the main thread.
3. `CommandRegistry.ExecuteCommand()` looks up the handler by tool name.
4. The handler (e.g., `ManageGameObjectTool.HandleCommand()`) executes Unity API calls.
5. The result is serialized to JSON and sent back via TCP.

### Auto-Discovery

Tool handlers are auto-registered using reflection. Any class marked with `[McpForUnityTool]` and containing a `public static object HandleCommand(JObject @params)` method is discovered on editor load:

```csharp
[McpForUnityTool(Name = "my_tool", AutoRegister = true)]
[ToolParameter("action", "The action to perform")]
public static class MyTool
{
    public static object HandleCommand(JObject @params)
    {
        var toolParams = new ToolParams(@params);
        string action = toolParams.GetRequired<string>("action");
        // ... Unity API calls ...
        return new { success = true };
    }
}
```

### Main Thread Safety

Unity's API is not thread-safe. All commands received on the TCP thread are marshalled to the main thread via `UnityMainThreadDispatcher`, which drains a queue during `EditorApplication.update`.

### Domain Reload Handling

When Unity recompiles scripts, all threads are killed. `McpAutoStart` uses `[InitializeOnLoad]` + `SessionState` to automatically restart the TCP listener after every domain reload without user intervention.

### Lock File

On startup, the plugin writes `~/.unity-mcp/unity-mcp-port-{pid}.json`:

```json
{
  "port": 8765,
  "pid": 12345,
  "startTime": "2026-05-16T10:00:00.000Z",
  "unityVersion": "2021.3.45f1",
  "authToken": "<random-base64-token>"
}
```

The Python server reads this to auto-discover the port and authenticate.

---

## Available Tools

### `manage_gameobject`
Create, modify, transform, and delete GameObjects.

| Parameter | Description |
|-----------|-------------|
| `action` | `create`, `delete`, `modify`, `get_info` |
| `name` | GameObject name |
| `position` | `[x, y, z]` array |
| `rotation` | `[x, y, z]` Euler angles |
| `scale` | `[x, y, z]` scale |
| `parent` | Parent GameObject name |

### `manage_scene`
Load, save, create, and query scenes.

| Parameter | Description |
|-----------|-------------|
| `action` | `get_hierarchy`, `load`, `save`, `new` |
| `scene_path` | Path to scene asset |
| `cursor` / `page_size` | Pagination for large hierarchies |

### `manage_editor`
Control editor state.

| Parameter | Description |
|-----------|-------------|
| `action` | `get_state`, `play`, `pause`, `stop`, `undo`, `redo` |

### `find_gameobjects`
Search for GameObjects by name, tag, layer, or component.

| Parameter | Description |
|-----------|-------------|
| `search_by` | `name`, `tag`, `layer`, `component` |
| `query` | Search string |

### `manage_script`
Create and read C# scripts.

| Parameter | Description |
|-----------|-------------|
| `action` | `create`, `read` |
| `script_name` | Name of the script |
| `path` | Asset path |
| `content` | Script content (for create) |

### `manage_material`
Create and modify materials with automatic render pipeline detection.

### `manage_components`
Add, remove, and configure components on GameObjects.

### `manage_asset`
Create, search, and organize project assets.

### `manage_prefabs`
Create, instantiate, and modify prefabs.

### `read_console`
Read Unity Console log output (errors, warnings, info).

### `refresh_unity`
Force an `AssetDatabase.Refresh()`.

### `run_tests`
Execute Unity Test Framework tests (EditMode/PlayMode).

### `execute_menu_item`
Execute any Unity Editor menu item by path.

### `batch_execute`
Execute multiple tool commands in a single call for efficiency.

---

## Running Tests

```bash
cd server/
pip install -e ".[dev]"
pytest -v
```

All tests run without Unity or a network connection.

---

## Adding Custom Tools

### In the Unity Plugin

1. Create a new C# file in any `Editor/` folder.
2. Add the `[McpForUnityTool]` attribute with a unique name.
3. Add `[ToolParameter]` attributes to describe parameters.
4. Implement `public static object HandleCommand(JObject @params)`.

```csharp
[McpForUnityTool(Name = "my_custom_tool", AutoRegister = true)]
[ToolParameter("action", "The action to perform")]
[ToolParameter("target", "Target object name")]
public static class MyCustomTool
{
    public static object HandleCommand(JObject @params)
    {
        var toolParams = new ToolParams(@params);
        var action = toolParams.GetRequired<string>("action");
        var target = toolParams.Get<string>("target", "");

        // Your Unity API logic here
        return new { success = true, message = $"Did {action} on {target}" };
    }
}
```

### In the Python Server

Add a new tool function in `server.py`:

```python
@mcp.tool()
async def my_custom_tool(action: str, target: str = "") -> dict:
    """Description of what this tool does."""
    return await _relay_to_unity("my_custom_tool", {
        "action": action,
        "target": target,
    })
```

The tool name in Python must match the `Name` in the C# `[McpForUnityTool]` attribute.

---

## Configuration Reference

| Setting | Default | Notes |
|---------|---------|-------|
| Unity TCP port | 8765 | Configurable in Editor window and via `--unity-port` |
| HTTP server port | 8080 | Only used with `--transport http` |
| HTTP bind address | 127.0.0.1 | Use `--host 0.0.0.0` to expose to network (not recommended) |
| Command timeout | 30 seconds | Per-command timeout for Unity responses |
| Log file location | `~/.unity-mcp/logs/` | Rotating, 512 KB max |
| Lock file location | `~/.unity-mcp/` | Auto-cleaned on shutdown |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Plugin won't compile | Ensure Unity 2021.3.x and `com.unity.nuget.newtonsoft-json` is installed |
| Server can't find Unity | Check `~/.unity-mcp/` for lock files; ensure plugin is started |
| Connection rejected | Lock file may be stale — delete old `unity-mcp-port-*.json` files |
| Commands timeout | Unity main thread may be blocked (asset import, long operation) |
| Auth failure after restart | Plugin generates a new token each start — server reconnects automatically |

---

## License

MIT — see [LICENSE](https://github.com/Manasistheone/unity-2021-mcp/blob/main/LICENSE) for details.
