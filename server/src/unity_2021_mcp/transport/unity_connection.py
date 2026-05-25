"""Unity connection transport module.

Handles TCP communication between the MCP server and the Unity Editor plugin.
The Unity plugin listens on a TCP port; this module connects to it and
relays commands/responses.
"""

import asyncio
import json
import logging
from pathlib import Path
from typing import Any

logger = logging.getLogger("unity_2021_mcp.transport")


class UnityConnection:
    """Manages the TCP connection to the Unity Editor plugin."""

    def __init__(self, host: str = "127.0.0.1", port: int = 8765, timeout: float = 30.0):
        self.host = host
        self.port = port
        self.timeout = timeout
        self._reader: asyncio.StreamReader | None = None
        self._writer: asyncio.StreamWriter | None = None
        self._connected = False
        self._lock = asyncio.Lock()
        self._auth_token: str | None = None

    @property
    def connected(self) -> bool:
        return self._connected

    async def connect(self) -> bool:
        """Attempt to connect to the Unity plugin TCP server."""
        try:
            # Try to discover port and auth token from lock file first
            lock_info = self._discover_lock_info()
            port = lock_info.get("port", self.port) if lock_info else self.port
            self._auth_token = lock_info.get("authToken") if lock_info else None

            self._reader, self._writer = await asyncio.wait_for(
                asyncio.open_connection(self.host, port),
                timeout=5.0,
            )

            # Send authentication handshake
            if self._auth_token:
                auth_msg = json.dumps({"auth": self._auth_token}) + "\n"
                self._writer.write(auth_msg.encode("utf-8"))
                await self._writer.drain()
            else:
                logger.warning("No auth token found in lock file, connection may be rejected")
                auth_msg = json.dumps({"auth": ""}) + "\n"
                self._writer.write(auth_msg.encode("utf-8"))
                await self._writer.drain()

            self._connected = True
            logger.info(f"Connected to Unity plugin at {self.host}:{port}")
            return True
        except (ConnectionRefusedError, asyncio.TimeoutError, OSError) as e:
            self._connected = False
            logger.debug(f"Unity connection failed: {e}")
            return False

    async def disconnect(self) -> None:
        """Close the connection to Unity."""
        if self._writer:
            try:
                self._writer.close()
                await self._writer.wait_closed()
            except Exception:
                pass
        self._reader = None
        self._writer = None
        self._connected = False

    async def send_command(self, command: dict[str, Any]) -> dict[str, Any]:
        """Send a command to Unity and wait for the response.

        Args:
            command: The command dictionary to send.

        Returns:
            The response dictionary from Unity.

        Raises:
            ConnectionError: If not connected to Unity.
            TimeoutError: If the command times out.
        """
        if not self._connected or not self._writer or not self._reader:
            raise ConnectionError(
                "Not connected to Unity. Ensure the Unity Editor is open "
                "with the MCP plugin active."
            )

        async with self._lock:
            try:
                # Send command as JSON line
                payload = json.dumps(command) + "\n"
                self._writer.write(payload.encode("utf-8"))
                await self._writer.drain()

                # Read response line
                response_line = await asyncio.wait_for(
                    self._reader.readline(),
                    timeout=self.timeout,
                )

                if not response_line:
                    self._connected = False
                    raise ConnectionError("Unity connection closed unexpectedly.")

                return json.loads(response_line.decode("utf-8"))

            except asyncio.TimeoutError:
                raise TimeoutError(
                    f"Command timed out after {self.timeout}s. "
                    "The Unity operation may still be running."
                )
            except (ConnectionResetError, BrokenPipeError) as e:
                self._connected = False
                raise ConnectionError(f"Unity connection lost: {e}")

    def _discover_lock_info(self) -> dict | None:
        """Try to discover the Unity plugin port and auth token from lock files."""
        lock_dir = Path.home() / ".unity-mcp"
        if not lock_dir.exists():
            return None

        # Find the most recently modified lock file (most likely the active Unity instance)
        lock_files = list(lock_dir.glob("unity-mcp-port-*.json"))
        if not lock_files:
            return None

        lock_files.sort(key=lambda f: f.stat().st_mtime, reverse=True)

        for lock_file in lock_files:
            try:
                data = json.loads(lock_file.read_text())
                port = data.get("port")
                if port and isinstance(port, int):
                    logger.debug(f"Discovered Unity port {port} from lock file {lock_file.name}")
                    return data
            except (json.JSONDecodeError, OSError):
                continue

        return None
