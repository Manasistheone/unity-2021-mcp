# Unity 2021 MCP

A standalone MCP (Model Context Protocol) server for Unity 2021.3 LTS. This project enables AI assistants (Claude, Cursor, Kiro, VS Code Copilot) to interact with the Unity Editor through the MCP protocol.

## Quick Links

- [Architecture](Architecture) — System design, data flow, and security model
- [Setup Guide](Setup-Guide) — Installation and configuration for all platforms
- [Available Tools](Available-Tools) — All 14 tools with parameters and examples
- [Python Server](Python-Server) — Server internals, CLI args, modules
- [Unity Plugin](Unity-Plugin) — Plugin internals, command dispatch, auto-discovery
- [Adding Custom Tools](Adding-Custom-Tools) — How to extend with your own tools
- [Configuration](Configuration) — All settings and defaults
- [Troubleshooting](Troubleshooting) — Common issues and solutions

## Overview

The system consists of two components:

- **Python MCP Server** (`server/`) — Handles MCP protocol communication with AI clients and relays commands to the Unity plugin via TCP.
- **Unity Editor Plugin** (`plugin/`) — Receives commands from the Python server and executes them against the Unity Editor API.

## Prerequisites

- **Python 3.10+** with `uv` or `pip`
- **Unity 2021.3 LTS** (any patch version)
- An MCP-compatible AI client (Claude Desktop, Cursor, Kiro, VS Code, etc.)

## Project Structure

```
unity-2021-mcp/
├── server/                          # Python MCP server
│   ├── pyproject.toml               # Package config, dependencies, entry point
│   ├── uv.lock                      # Locked dependency versions
│   ├── src/unity_2021_mcp/          # Source package
│   └── tests/                       # Pytest test suite
├── plugin/                          # Unity Editor plugin (UPM package)
│   ├── package.json                 # Unity Package Manager manifest
│   └── Editor/                      # All C# source files
├── README.md
├── SETUP.md
└── DOCS.md
```

## License

MIT — see [LICENSE](https://github.com/Manasistheone/unity-2021-mcp/blob/main/LICENSE) for details.
