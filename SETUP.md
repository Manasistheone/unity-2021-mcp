# Local MCP Server Setup Guide

## Prerequisites

- **Python 3.10+** with `pip` or `uv` package manager
- **Unity 2021.3 LTS** (any patch version)
- **MCP Client**  one of: Claude Desktop, Cursor, VS Code with Copilot, Kiro, or any MCP-compatible client

## Project Structure

```
unity-2021-mcp/              # Repository root
├─ server/                   # Python MCP server
│   pyproject.toml
│   src/
│      unity_2021_mcp/
│          __init__.py
│          main.py          # CLI entry point
│          server.py        # FastMCP server + tool registration
│          logging_config.py
│          core/
│             config.py    # CLI argument parsing
│          transport/
│             unity_connection.py  # TCP relay to Unity
│          tools/
│              registry.py  # Tool relay logic
│   tests/
├─ plugin/                   # Unity Editor plugin
│   package.json
│   Editor/
│      UnityMcp2021.Editor.asmdef
│      TcpTransportClient.cs      # TCP listener (port 8765)
│      CommandDispatcher.cs       # Routes commands to handlers
│      CommandRegistry.cs         # Auto-discovers tool handlers
│      UnityMainThreadDispatcher.cs # Main thread marshalling
│      McpEditorWindow.cs         # Window > MCP for Unity 2021
│      ManageEditorTool.cs        # Editor state tool
│      ManageSceneTool.cs         # Scene management tool
│      FindGameObjectsTool.cs     # GameObject search tool
│      ReadConsoleTool.cs         # Console output tool
│      RefreshUnityTool.cs        # Asset refresh tool
│      McpServerManager.cs        # Shared server lifecycle manager
│      McpAutoStart.cs            # Auto-restart after domain reload
│      (supporting classes)
├─ README.md
└─ SETUP.md
```

---

## Step 1: Install the Python MCP Server

### Option A: Using `uv` (recommended)

```bash
cd unity-2021-mcp/server
uv pip install -e ".[dev]"
```

### Option B: Using `pip`

```bash
cd unity-2021-mcp/server
pip install -e ".[dev]"
```

This installs the `mcp-for-unity-2021` CLI command.

---

## Step 2: Install the Unity Plugin

1. Open your Unity 2021.3 project
2. Open **Window  Package Manager**
3. Click the **+** button  **Add package from disk...**
4. Navigate to the `plugin/package.json` file inside this repository
5. Click **Open**  Unity will import the plugin

Alternatively, add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.coplay.unity-2021-mcp": "file:<absolute-path-to-repo>/plugin"
  }
}
```

---

## Step 3: Configure the MCP Client

### For Kiro (recommended)

Add to `.kiro/settings/mcp.json` in your workspace:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": [
        "run",
        "--project",
        "<absolute-path-to-repo>/server",
        "mcp-for-unity-2021",
        "--transport",
        "stdio"
      ]
    }
  }
}
```

To **disable** the server without removing the config, add `"disabled": true`:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": [
        "run",
        "--project",
        "<absolute-path-to-repo>/server",
        "mcp-for-unity-2021",
        "--transport",
        "stdio"
      ],
      "disabled": true
    }
  }
}
```

### Auto-Approve Tools

To skip confirmation prompts for Unity MCP tools, add `"autoApprove"` with the tool names:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": [
        "run",
        "--project",
        "<absolute-path-to-repo>/server",
        "mcp-for-unity-2021",
        "--transport",
        "stdio"
      ],
      "autoApprove": [
        "manage_gameobject",
        "manage_scene",
        "manage_script",
        "manage_material",
        "manage_components",
        "manage_asset",
        "manage_prefabs",
        "manage_editor",
        "read_console",
        "refresh_unity",
        "run_tests",
        "find_gameobjects",
        "execute_menu_item",
        "batch_execute"
      ]
    }
  }
}
```

This allows the AI to execute Unity operations without requiring manual approval each time.

---

### For Claude Desktop / Cursor / VS Code

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": ["run", "--project", "<absolute-path-to-repo>/server", "mcp-for-unity-2021", "--transport", "stdio"]
    }
  }
}
```

### HTTP Mode (multi-agent, remote)

Start the server manually:

```bash
mcp-for-unity-2021 --transport http --port 8080
```

Then configure your MCP client:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "url": "http://localhost:8080"
    }
  }
}
```

---

## Step 4: Connect Unity to the MCP Server

1. In Unity, open **Window  MCP for Unity 2021**
2. Set the TCP port (default: 8765)
3. Click **Start**  the plugin begins listening for connections
4. Status shows "Listening (waiting for connection)" (orange)
5. When the Python server connects, status changes to **Connected** (green)

The plugin writes a port lock file to `~/.unity-mcp/unity-mcp-port-{pid}.json` for auto-discovery by the Python server.

### Auto-Reconnect After Script Compilation

The plugin includes automatic reconnection support. When Unity recompiles scripts (domain reload), all threads are aborted including the TCP listener. The `McpAutoStart` component uses `[InitializeOnLoad]` to automatically restart the listener after every domain reload.

- You only need to click **Start** once per Unity session
- The server will auto-recover after script compilations without manual intervention
- The auto-start state is tracked via `SessionState` (persists across domain reloads but resets when Unity is closed)

---

## Step 5: Verify the Connection

From your MCP client (Kiro, Claude, Cursor, etc.), ask the AI:

> "Get the Unity editor state"

The AI should invoke the `manage_editor` tool with action `get_state` and return:
- Unity version
- Current scene name
- Play mode status
- Build platform

---

## Available Tools

| Tool | Description |
|------|-------------|
| `manage_gameobject` | Create, modify, transform, and delete GameObjects |
| `manage_scene` | Load, save, and query scene hierarchy |
| `manage_script` | Create, read, and modify C# scripts |
| `manage_material` | Create and modify materials and shaders |
| `manage_components` | Add, remove, and configure components |
| `manage_asset` | Create, search, and organize assets |
| `manage_prefabs` | Create, instantiate, and modify prefabs |
| `manage_editor` | Control Editor state, play mode, undo/redo |
| `read_console` | Read Unity Console output |
| `refresh_unity` | Refresh the asset database |
| `run_tests` | Run Unity Test Framework tests |
| `find_gameobjects` | Search GameObjects by various criteria |
| `execute_menu_item` | Execute Unity Editor menu items |
| `batch_execute` | Execute multiple operations in one call |

---

## Configuration Reference

| Setting | Default | Range / Format |
|---------|---------|----------------|
| TCP Port (stdio mode) | 8765 | 1024-65535 |
| HTTP Port | 8080 | 1024-65535 |
| Command timeout | 30 seconds | - |
| Log file max size | 512 KB | 2 backup files |
| Log location | ~/.unity-mcp/logs/ | - |

---

## Architecture

```
MCP Client (Kiro/Claude/Cursor)
    | stdio (stdin/stdout)
Python MCP Server (FastMCP 3.x)
    | TCP socket (port 8765)
Unity Editor Plugin (C# / IMGUI)
    | Unity Editor API
Unity 2021.3 LTS
```

- **Stdio mode:** Client -> Python (stdin/stdout) -> Unity (TCP socket)
- **HTTP mode:** Client -> Python (HTTP/SSE) -> Unity (WebSocket)

---

## Disabling the MCP Server

### From Kiro config

Add `"disabled": true` to the server entry in `.kiro/settings/mcp.json`. Kiro will not spawn the server process.

### From Unity

Click **Stop** in the MCP for Unity 2021 Editor window. The TCP listener will close and the port lock file will be removed.

---

## Troubleshooting

### Plugin won't compile in Unity

- Verify you're using Unity **2021.3.x** (not 2022+)
- Ensure `com.unity.nuget.newtonsoft-json` is installed (check Package Manager)

### Server can't find Unity

- Ensure the Unity Editor is open with the plugin active
- Check that `~/.unity-mcp/` contains a `unity-mcp-port-*.json` lock file
- Verify the port in the lock file matches what the plugin is listening on
- Delete stale lock files from old Unity sessions

### Connection drops after script compilation

- This is handled automatically by `McpAutoStart.cs` — the listener restarts after domain reload
- If auto-reconnect fails, check the Unity Console for `[AutoStart]` log messages
- You can manually restart by clicking **Stop** then **Start** in the MCP Editor window
- Enable **Debug Logging** in the MCP Editor window for verbose output

### Connection drops (other causes)

- Check the Unity Console for error logs
- Ensure Unity is not in a long blocking operation (asset import)
- Verify the Python MCP server process is still running

### Commands timing out

- Default timeout is 30 seconds
- Commands execute on Unity's main thread  if the editor is busy, commands queue
- Check Unity Console for errors during command execution

---

## Running Tests

### Python server tests

```bash
cd unity-2021-mcp/server
pytest
```

### Unity plugin tests

1. Open Unity Test Runner: **Window  General  Test Runner**
2. Select **EditMode** tab
3. Run all tests in the MCP assembly

---

## Adding Custom Tools

1. Create a new C# class in your Unity project's Editor folder
2. Mark the class with `[McpForUnityTool(Name = "my_tool", AutoRegister = true)]`
3. Add `[ToolParameter("param_name", "description")]` attributes to the **class**
4. Add a `public static object HandleCommand(JObject @params)` method
5. The tool is auto-discovered on next Editor reload

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
        // ... implementation
        return new { success = true, result = "done" };
    }
}
```

**Note:** `[ToolParameter]` attributes go on the **class**, not the method.