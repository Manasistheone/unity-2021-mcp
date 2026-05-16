"""Logging configuration for Unity MCP 2021 Server.

Configures a rotating file handler with:
- Maximum file size: 512 KB per file
- Maximum backup files: 2
- Log location: OS-specific log directory (~/.unity-mcp/logs/)
"""

import logging
import os
import sys
from logging.handlers import RotatingFileHandler
from pathlib import Path

# Constants for rotating file handler
MAX_LOG_BYTES = 512 * 1024  # 512 KB
BACKUP_COUNT = 2
LOG_DIR_NAME = "logs"
LOG_FILE_NAME = "unity-2021-mcp.log"


def get_log_directory() -> Path:
    """Get the OS-specific log directory for the MCP server.

    Returns ~/.unity-mcp/logs/ on all platforms.
    """
    home = Path.home()
    log_dir = home / ".unity-mcp" / LOG_DIR_NAME
    return log_dir


def setup_logging(level: int = logging.INFO) -> logging.Logger:
    """Configure logging with a rotating file handler.

    Sets up:
    - Rotating file handler: 512 KB max per file, 2 backup files
    - Console handler for stderr output
    - Consistent format across all handlers

    Args:
        level: The logging level to use. Defaults to INFO.

    Returns:
        The configured root logger for the unity_2021_mcp package.
    """
    logger = logging.getLogger("unity_2021_mcp")
    logger.setLevel(level)

    # Avoid adding duplicate handlers if called multiple times
    if logger.handlers:
        return logger

    formatter = logging.Formatter(
        fmt="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    # Rotating file handler
    log_dir = get_log_directory()
    log_dir.mkdir(parents=True, exist_ok=True)
    log_file = log_dir / LOG_FILE_NAME

    file_handler = RotatingFileHandler(
        filename=str(log_file),
        maxBytes=MAX_LOG_BYTES,
        backupCount=BACKUP_COUNT,
        encoding="utf-8",
    )
    file_handler.setLevel(level)
    file_handler.setFormatter(formatter)
    logger.addHandler(file_handler)

    # Console handler (stderr so it doesn't interfere with stdio transport)
    console_handler = logging.StreamHandler(sys.stderr)
    console_handler.setLevel(logging.WARNING)
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)

    return logger
