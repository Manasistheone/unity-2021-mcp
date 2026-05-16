"""Tests for the logging configuration module."""

import logging
from logging.handlers import RotatingFileHandler
from pathlib import Path

from unity_2021_mcp.logging_config import (
    BACKUP_COUNT,
    MAX_LOG_BYTES,
    get_log_directory,
    setup_logging,
)


def test_max_log_bytes_is_512kb():
    """Verify max log file size is 512 KB."""
    assert MAX_LOG_BYTES == 512 * 1024


def test_backup_count_is_2():
    """Verify backup count is 2."""
    assert BACKUP_COUNT == 2


def test_log_directory_is_under_unity_mcp():
    """Verify log directory is under ~/.unity-mcp/logs/."""
    log_dir = get_log_directory()
    assert log_dir == Path.home() / ".unity-mcp" / "logs"


def test_setup_logging_creates_rotating_file_handler():
    """Verify setup_logging creates a RotatingFileHandler with correct settings."""
    logger = setup_logging()

    # Find the rotating file handler
    file_handlers = [
        h for h in logger.handlers if isinstance(h, RotatingFileHandler)
    ]
    assert len(file_handlers) == 1

    handler = file_handlers[0]
    assert handler.maxBytes == 512 * 1024
    assert handler.backupCount == 2


def test_setup_logging_is_idempotent():
    """Verify calling setup_logging multiple times doesn't add duplicate handlers."""
    logger = setup_logging()
    handler_count = len(logger.handlers)

    # Call again
    logger2 = setup_logging()
    assert logger2 is logger
    assert len(logger2.handlers) == handler_count


def test_setup_logging_returns_named_logger():
    """Verify the logger is named for the package."""
    logger = setup_logging()
    assert logger.name == "unity_2021_mcp"
