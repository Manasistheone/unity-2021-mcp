"""Server configuration module."""

import argparse
from dataclasses import dataclass, field


@dataclass
class ServerConfig:
    """Configuration for the Unity MCP 2021 server."""

    transport: str = "stdio"
    host: str = "127.0.0.1"
    port: int = 8080
    unity_port: int = 8765
    unity_host: str = "127.0.0.1"
    command_timeout: float = 30.0
    reconnect_delays: list[float] = field(
        default_factory=lambda: [0.0, 1.0, 5.0, 15.0, 30.0]
    )


def parse_args() -> ServerConfig:
    """Parse command-line arguments into a ServerConfig."""
    parser = argparse.ArgumentParser(
        prog="mcp-for-unity-2021",
        description="MCP server for Unity 2021.3 LTS Editor integration.",
    )
    parser.add_argument(
        "--transport",
        choices=["stdio", "http"],
        default="stdio",
        help="Transport mode: stdio (default) or http.",
    )
    parser.add_argument(
        "--port",
        type=int,
        default=8080,
        help="HTTP server port (only used with --transport http). Default: 8080.",
    )
    parser.add_argument(
        "--unity-port",
        type=int,
        default=8765,
        help="TCP port for Unity plugin connection. Default: 8765.",
    )
    parser.add_argument(
        "--host",
        type=str,
        default="127.0.0.1",
        help="HTTP server bind address (only used with --transport http). Default: 127.0.0.1.",
    )
    parser.add_argument(
        "--unity-host",
        type=str,
        default="127.0.0.1",
        help="Host for Unity plugin connection. Default: 127.0.0.1.",
    )

    args = parser.parse_args()
    return ServerConfig(
        transport=args.transport,
        host=args.host,
        port=args.port,
        unity_port=args.unity_port,
        unity_host=args.unity_host,
    )
