# Configuration

## Server Settings

All server settings are configured via CLI arguments to `mcp-for-unity-2021`:

| Setting | CLI Argument | Default | Description |
|---------|-------------|---------|-------------|
| Transport mode | `--transport` | `stdio` | `stdio` or `http` |
| HTTP bind address | `--host` | `127.0.0.1` | Address for HTTP server |
| HTTP port | `--port` | `8080` | Port for HTTP server |
| Unity TCP host | `--unity-host` | `127.0.0.1` | Unity plugin host |
| Unity TCP port | `--unity-port` | `8765` | Unity plugin port |

### Internal Settings (not configurable via CLI)

| Setting | Value | Location |
|---------|-------|----------|
| Command timeout | 30 seconds | `core/config.py` |
| Reconnect delays | 0, 1, 5, 15, 30 seconds | `core/config.py` |
| Log file max size | 512 KB | `logging_config.py` |
| Log backup count | 2 files | `logging_config.py` |

## Unity Plugin Settings

Configured via the Editor window (**Window → MCP for Unity 2021**):

| Setting | Storage | Default |
|---------|---------|---------|
| TCP Port | `EditorPrefs` (`UnityMcp2021_Port`) | 8765 |
| Debug Logging | `EditorPrefs` (`UnityMcp2021_Debug`) | false |

## File Locations

| File | Path | Purpose |
|------|------|---------|
| Lock file | `~/.unity-mcp/unity-mcp-port-{pid}.json` | Port discovery + auth token |
| Log file | `~/.unity-mcp/logs/unity-2021-mcp.log` | Server logs (rotating) |
| Log backups | `~/.unity-mcp/logs/unity-2021-mcp.log.1`, `.2` | Rotated log files |

`~` refers to the user's home directory:
- **Windows**: `C:\Users\<username>\`
- **macOS**: `/Users/<username>/`
- **Linux**: `/home/<username>/`

## MCP Client Configuration Examples

### Kiro (`.kiro/settings/mcp.json`)

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
        "manage_editor",
        "find_gameobjects",
        "read_console",
        "refresh_unity"
      ]
    }
  }
}
```

### Claude Desktop / Cursor / VS Code

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

### Disabling the Server

Add `"disabled": true` to the server entry:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": ["..."],
      "disabled": true
    }
  }
}
```

---

← [Adding Custom Tools](Adding-Custom-Tools) | [Troubleshooting](Troubleshooting) →
