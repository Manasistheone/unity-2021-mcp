"""Entry point for the Unity MCP 2021 Server.

This module provides the main() function used as the CLI entry point
for the `mcp-for-unity-2021` command defined in pyproject.toml.
"""

from unity_2021_mcp.core.config import parse_args
from unity_2021_mcp.logging_config import setup_logging
from unity_2021_mcp.server import create_server


def main() -> None:
    """Main entry point for the mcp-for-unity-2021 CLI command."""
    logger = setup_logging()
    config = parse_args()

    logger.info(
        f"Unity MCP 2021 Server starting (transport={config.transport}, "
        f"unity={config.unity_host}:{config.unity_port})"
    )

    mcp = create_server(
        unity_host=config.unity_host,
        unity_port=config.unity_port,
        timeout=config.command_timeout,
    )

    if config.transport == "stdio":
        logger.info("Running in stdio transport mode")
        mcp.run(transport="stdio")
    else:
        logger.info(f"Running in HTTP transport mode on port {config.port}")
        mcp.run(transport="sse", host=config.host, port=config.port)


if __name__ == "__main__":
    main()
