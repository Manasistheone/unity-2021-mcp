"""Unity tool definitions for the MCP server.

Each tool relays commands to the Unity Editor plugin via TCP.
When Unity is not connected, tools return helpful error messages.
"""

import logging
from typing import Any

from unity_2021_mcp.transport.unity_connection import UnityConnection

logger = logging.getLogger("unity_2021_mcp.tools")

# Module-level connection reference (set by server.py during startup)
_unity_connection: UnityConnection | None = None


def set_connection(conn: UnityConnection) -> None:
    """Set the Unity connection instance for all tools."""
    global _unity_connection
    _unity_connection = conn


def get_connection() -> UnityConnection:
    """Get the Unity connection, raising if not available."""
    if _unity_connection is None:
        raise RuntimeError("Unity connection not initialized.")
    return _unity_connection


async def _relay_to_unity(tool_name: str, params: dict[str, Any]) -> dict[str, Any]:
    """Relay a tool call to Unity via the TCP connection.

    If Unity is not connected, attempts to connect first.
    Returns the Unity response or an error dict.
    """
    conn = get_connection()

    if not conn.connected:
        connected = await conn.connect()
        if not connected:
            return {
                "success": False,
                "error": (
                    "Not connected to Unity Editor. "
                    "Please ensure Unity is open with the MCP plugin active, "
                    "then try again."
                ),
            }

    command = {"tool": tool_name, "params": params}

    try:
        response = await conn.send_command(command)
        return response
    except ConnectionError as e:
        return {"success": False, "error": str(e)}
    except TimeoutError as e:
        return {"success": False, "error": str(e)}
