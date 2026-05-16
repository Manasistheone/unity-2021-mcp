# Architecture

## System Diagram

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

## Data Flow

1. The MCP client sends a tool call (e.g., `manage_gameobject`) to the Python server.
2. The Python server wraps it as a JSON command and sends it over TCP to the Unity plugin.
3. The Unity plugin dispatches the command to the appropriate tool handler on the main thread.
4. The handler executes Unity API calls and returns a result.
5. The result flows back through TCP → Python → MCP client.

## Transport Modes

| Mode | Client ↔ Server | Server ↔ Unity | Use Case |
|------|-----------------|----------------|----------|
| **stdio** | stdin/stdout | TCP socket | Single AI agent, local |
| **HTTP** | HTTP/SSE | TCP socket | Multiple agents, remote |

## Security Model

### Localhost-Only Binding

- The Unity plugin TCP listener binds to `127.0.0.1` (loopback only). No external network access.
- The Python HTTP server defaults to `127.0.0.1`. Configurable via `--host` for advanced use cases.

### Token-Based Authentication

On startup, the Unity plugin:
1. Generates a 32-byte cryptographic random token.
2. Writes it to a lock file at `~/.unity-mcp/unity-mcp-port-{pid}.json`.
3. Requires connecting clients to send `{"auth": "<token>"}` as the first message.
4. Rejects connections with invalid or missing tokens.

The Python server reads the token from the lock file and sends it automatically on connect.

### No Payload Logging

Debug logs only show message sizes (e.g., "Received command (142 chars)"), never raw content. This prevents accidental exposure of file paths, script content, or user data in log files.

### Lock File Format

```json
{
  "port": 8765,
  "pid": 12345,
  "startTime": "2026-05-16T10:00:00.000Z",
  "unityVersion": "2021.3.45f1",
  "authToken": "base64-encoded-random-bytes"
}
```

Lock files are automatically cleaned up when the plugin stops.

---

← [Home](Home) | [Setup Guide](Setup-Guide) →
