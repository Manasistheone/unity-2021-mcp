# Python Server

The Python MCP server lives in `server/src/unity_2021_mcp/`. It handles MCP protocol communication with AI clients and relays commands to the Unity plugin over TCP.

## Entry Point

The CLI command `mcp-for-unity-2021` is defined in `pyproject.toml`:

```toml
[project.scripts]
mcp-for-unity-2021 = "unity_2021_mcp.main:main"
```

## CLI Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--transport` | `stdio` | Transport mode: `stdio` or `http` |
| `--host` | `127.0.0.1` | HTTP bind address (http mode only) |
| `--port` | `8080` | HTTP server port (http mode only) |
| `--unity-port` | `8765` | TCP port for Unity plugin connection |
| `--unity-host` | `127.0.0.1` | Host for Unity plugin connection |

## Module Overview

### `main.py`

Parses CLI arguments, sets up logging, creates the FastMCP server, and runs it in the selected transport mode.

### `server.py`

Registers all MCP tools with FastMCP. Each tool is an async function that relays to Unity:

```python
@mcp.tool()
async def manage_gameobject(action: str, name: str = "", ...):
    """Create, modify, transform, and delete GameObjects."""
    return await _relay_to_unity("manage_gameobject", {
        "action": action,
        "name": name,
        ...
    })
```

On startup, it auto-connects to the Unity plugin with exponential backoff.

### `transport/unity_connection.py`

Manages the TCP connection to the Unity plugin:

- **Port discovery**: Reads `~/.unity-mcp/unity-mcp-port-*.json` lock files to find the port.
- **Authentication**: Sends the auth token from the lock file as the first message.
- **Command protocol**: JSON-line format — one JSON object per line, newline-delimited.
- **Thread safety**: Uses an asyncio lock to serialize concurrent tool calls.
- **Timeout**: 30-second default per command (configurable).

### `core/config.py`

Defines `ServerConfig` dataclass and `parse_args()` for CLI argument handling.

### `logging_config.py`

Sets up a rotating file logger:

- **Location**: `~/.unity-mcp/logs/unity-2021-mcp.log`
- **Max size**: 512 KB per file
- **Backups**: 2 rotated files
- **Console**: Only warnings and errors go to stderr (to avoid interfering with stdio transport)

### `tools/registry.py`

Contains the `_relay_to_unity()` helper that:
1. Gets the active Unity connection.
2. Sends the command as JSON.
3. Waits for the response.
4. Returns the result or raises an error.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `fastmcp` | >=3.0.0, <4 | MCP protocol server implementation |
| `httpx` | >=0.27.0 | HTTP client library |
| `pydantic` | >=2.0.0 | Data validation and settings |

### Dev Dependencies

| Package | Purpose |
|---------|---------|
| `pytest` | Test runner |
| `pytest-asyncio` | Async test support |
| `hypothesis` | Property-based testing |

## Running Tests

```bash
cd server/
pip install -e ".[dev]"
pytest -v
```

Tests run without Unity or a network connection.

---

← [Available Tools](Available-Tools) | [Unity Plugin](Unity-Plugin) →
