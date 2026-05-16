# Setup Guide

## Step 1: Install the Python MCP Server

### Using `uv` (recommended)

```bash
cd unity-2021-mcp/server
uv pip install -e ".[dev]"
```

### Using `pip`

```bash
cd unity-2021-mcp/server
pip install -e ".[dev]"
```

This installs the `mcp-for-unity-2021` CLI command.

## Step 2: Install the Unity Plugin

1. Open your Unity 2021.3 project.
2. Open **Window → Package Manager**.
3. Click the **+** button → **Add package from disk...**
4. Navigate to the `plugin/package.json` file inside this repository.
5. Click **Open** — Unity will import the plugin.

Alternatively, add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.coplay.unity-2021-mcp": "file:<absolute-path-to-repo>/plugin"
  }
}
```

## Step 3: Configure the MCP Client

Replace `<absolute-path-to-repo>` with the actual path where you cloned the repository.

### Kiro

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

### Claude Desktop / Cursor / VS Code

Add to your MCP client configuration:

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

## Step 4: Connect Unity to the MCP Server

1. In Unity, open **Window → MCP for Unity 2021**.
2. Set the TCP port (default: 8765).
3. Click **Start** — the plugin begins listening for connections.
4. Status shows "Listening (waiting for connection)" (orange).
5. When the Python server connects, status changes to **Connected** (green).

### Auto-Reconnect

The plugin automatically restarts after Unity script compilation (domain reload). You only need to click **Start** once per Unity session.

## Step 5: Verify

From your MCP client, ask the AI:

> "Get the Unity editor state"

The AI should invoke `manage_editor` with action `get_state` and return Unity version, scene name, play mode status, and build platform.

---

← [Architecture](Architecture) | [Available Tools](Available-Tools) →
