# Unity MCP 2021

A standalone MCP (Model Context Protocol) server for Unity 2021.3 LTS. This project enables AI assistants (Claude, Cursor, VS Code, etc.) to interact with the Unity Editor through the MCP protocol.

## Architecture

The system consists of two components:

- **Python MCP Server** (`server/`) — Handles MCP protocol communication with AI clients and relays commands to the Unity plugin via TCP or WebSocket.
- **Unity Editor Plugin** (`plugin/`) — Receives commands from the Python server and executes them against the Unity Editor API using IMGUI for the Editor window.

## Prerequisites

- **Python 3.10+** with `uv` or `pip` for package management
- **Unity 2021.3 LTS** (any 2021.3.x patch version)
- An MCP-compatible AI client (Claude Desktop, Cursor, VS Code with MCP extension, etc.)

## Installation

### Python MCP Server

```bash
cd server/

# Using uv (recommended)
uv pip install -e .

# Or using pip
pip install -e .
```

The server installs a CLI command `mcp-for-unity-2021` that can be used as the MCP server entry point.

### Unity Plugin

1. Open your Unity 2021.3 project
2. Copy the `plugin/` folder into your project's `Packages/` directory, or use Unity's Package Manager:
   - Open **Window > Package Manager**
   - Click **+** > **Add package from disk...**
   - Navigate to `plugin/package.json` and select it
3. The plugin will compile and appear under **Window > MCP for Unity 2021**

## Configuration

### Transport Modes

The server supports two transport modes:

| Mode | Use Case | Protocol |
|------|----------|----------|
| **stdio** | Single AI agent, local usage | TCP socket between server and plugin |
| **HTTP** | Multiple AI agents, remote usage | WebSocket between server and plugin |

### Running the Server

```bash
# Stdio mode (default, for single-agent local usage)
mcp-for-unity-2021 --transport stdio

# HTTP mode (for multi-agent or remote usage)
mcp-for-unity-2021 --transport http --port 8080
```

### MCP Client Configuration

#### Claude Desktop

Add to your Claude Desktop MCP configuration:

```json
{
    "mcpServers": {
        "unity-2021-mcp": {
            "command": "uvx",
            "args": ["--from", "unity-2021-mcp-server", "mcp-for-unity-2021", "--transport", "stdio"]
        }
    }
}
```

#### Cursor / VS Code

Add to your MCP client settings:

```json
{
    "mcpServers": {
        "unity-2021-mcp": {
            "command": "uvx",
            "args": ["--from", "unity-2021-mcp-server", "mcp-for-unity-2021", "--transport", "stdio"]
        }
    }
}
```

### Unity Plugin Settings

Open **Window > MCP for Unity 2021** in the Unity Editor to:

- View connection status (connected / disconnected / error)
- Configure the server port (default: 8765, range: 1024–65535)
- Configure the HTTP server URL for WebSocket mode
- Select transport mode (stdio or HTTP)
- Auto-configure MCP clients
- Enable debug logging for troubleshooting

## Usage Examples

Once connected, AI assistants can use the following tools:

### Scene Management

```
Create a new empty GameObject named "Player" at position (0, 1, 0)
```

### Material and Shaders

```
Create a new material with the default lit shader for my render pipeline
```

### Script Management

```
Create a new C# MonoBehaviour script called "PlayerController" in Assets/Scripts/
```

### Asset Operations

```
Search for all prefab assets in the project containing "Enemy"
```

### Editor Control

```
Enter Play Mode to test the current scene
```

## Supported Tools

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

## Render Pipeline Support

The plugin automatically detects your project's render pipeline and adapts:

- **Built-in Render Pipeline** — Uses Standard shader
- **URP (Universal Render Pipeline)** — Uses Universal Render Pipeline/Lit shader
- **HDRP (High Definition Render Pipeline)** — Uses HDRP/Lit shader

## Troubleshooting

- **Plugin not compiling**: Ensure you're using Unity 2021.3.x. The plugin requires the `com.unity.nuget.newtonsoft-json` package.
- **Connection issues**: Check that the Python server is running and the port matches between server and plugin settings.
- **Enable debug logging**: Toggle the debug option in the MCP Editor window to see raw command payloads and timing information.

## License

See the LICENSE file for details.
