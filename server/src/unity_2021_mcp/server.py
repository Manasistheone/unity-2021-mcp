"""FastMCP server creation and tool registration.

This module creates the FastMCP server instance and registers all Unity tools.
"""

import asyncio
import logging
from contextlib import asynccontextmanager
from typing import Any

from fastmcp import FastMCP

from unity_2021_mcp.transport.unity_connection import UnityConnection
from unity_2021_mcp.tools.registry import set_connection, _relay_to_unity

logger = logging.getLogger("unity_2021_mcp.server")


def create_server(unity_host: str = "127.0.0.1", unity_port: int = 8765, timeout: float = 30.0) -> FastMCP:
    """Create and configure the FastMCP server with all Unity tools.

    Args:
        unity_host: Host where the Unity plugin is listening.
        unity_port: Port where the Unity plugin is listening.
        timeout: Command timeout in seconds.

    Returns:
        Configured FastMCP server instance.
    """
    # Create and register the Unity connection
    connection = UnityConnection(host=unity_host, port=unity_port, timeout=timeout)
    set_connection(connection)

    @asynccontextmanager
    async def server_lifespan(app):
        """Auto-connect to Unity on server startup with background retry."""
        async def connect_loop():
            delays = [0.0, 1.0, 2.0, 5.0, 10.0, 15.0, 30.0]
            attempt = 0
            while True:
                connected = await connection.connect()
                if connected:
                    logger.info("Auto-connected to Unity Editor")
                    break
                delay = delays[min(attempt, len(delays) - 1)]
                logger.debug(f"Unity not available, retrying in {delay}s...")
                await asyncio.sleep(delay)
                attempt += 1

        task = asyncio.create_task(connect_loop())
        try:
            yield {}
        finally:
            task.cancel()
            await connection.disconnect()

    mcp = FastMCP(
        name="Unity MCP 2021",
        instructions=(
            "This server provides tools to interact with the Unity 2021.3 LTS Editor. "
            "Use these tools to manage GameObjects, scenes, scripts, materials, "
            "components, assets, prefabs, and editor state. "
            "The Unity Editor must be open with the MCP plugin active for tools to work."
        ),
        lifespan=server_lifespan,
    )

    # --- Tool Definitions ---

    @mcp.tool()
    async def manage_gameobject(
        action: str,
        name: str = "",
        position: list[float] | None = None,
        rotation: list[float] | None = None,
        scale: list[float] | None = None,
        parent: str = "",
        tag: str = "",
        layer: str = "",
        active: bool = True,
    ) -> dict[str, Any]:
        """Create, modify, transform, and delete GameObjects in the Unity scene.

        Args:
            action: The action to perform: create, delete, find, modify, duplicate, rename.
            name: Name of the GameObject.
            position: World position as [x, y, z].
            rotation: Euler rotation as [x, y, z].
            scale: Local scale as [x, y, z].
            parent: Name or path of the parent GameObject.
            tag: Tag to assign.
            layer: Layer to assign.
            active: Whether the GameObject should be active.
        """
        params = {"action": action, "name": name, "active": active}
        if position:
            params["position"] = position
        if rotation:
            params["rotation"] = rotation
        if scale:
            params["scale"] = scale
        if parent:
            params["parent"] = parent
        if tag:
            params["tag"] = tag
        if layer:
            params["layer"] = layer
        return await _relay_to_unity("manage_gameobject", params)

    @mcp.tool()
    async def manage_scene(
        action: str,
        scene_name: str = "",
        path: str = "",
        save: bool = False,
    ) -> dict[str, Any]:
        """Load, save, and query scene hierarchy.

        Args:
            action: The action: load, save, new, get_hierarchy, get_active.
            scene_name: Name of the scene.
            path: Path to the scene asset.
            save: Whether to save the current scene before loading a new one.
        """
        params = {"action": action}
        if scene_name:
            params["scene_name"] = scene_name
        if path:
            params["path"] = path
        if save:
            params["save"] = save
        return await _relay_to_unity("manage_scene", params)

    @mcp.tool()
    async def manage_script(
        action: str,
        script_name: str = "",
        path: str = "",
        content: str = "",
        class_name: str = "",
        base_class: str = "MonoBehaviour",
    ) -> dict[str, Any]:
        """Create, read, and modify C# scripts.

        Args:
            action: The action: create, read, modify, delete, list.
            script_name: Name of the script file (without .cs extension).
            path: Directory path for the script (relative to Assets/).
            content: Full script content for create/modify.
            class_name: Class name (defaults to script_name if empty).
            base_class: Base class for new scripts. Default: MonoBehaviour.
        """
        params = {"action": action, "base_class": base_class}
        if script_name:
            params["script_name"] = script_name
        if path:
            params["path"] = path
        if content:
            params["content"] = content
        if class_name:
            params["class_name"] = class_name
        return await _relay_to_unity("manage_script", params)

    @mcp.tool()
    async def manage_material(
        action: str,
        name: str = "",
        shader: str = "",
        color: list[float] | None = None,
        properties: dict[str, Any] | None = None,
        path: str = "",
    ) -> dict[str, Any]:
        """Create and modify materials and shaders.

        Args:
            action: The action: create, modify, get, list, assign.
            name: Material name.
            shader: Shader name (auto-detected for render pipeline if empty).
            color: Main color as [r, g, b, a] (0-1 range).
            properties: Additional shader properties as key-value pairs.
            path: Asset path for the material.
        """
        params = {"action": action}
        if name:
            params["name"] = name
        if shader:
            params["shader"] = shader
        if color:
            params["color"] = color
        if properties:
            params["properties"] = properties
        if path:
            params["path"] = path
        return await _relay_to_unity("manage_material", params)

    @mcp.tool()
    async def manage_components(
        action: str,
        gameobject: str = "",
        component_type: str = "",
        properties: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        """Add, remove, and configure components on GameObjects.

        Args:
            action: The action: add, remove, get, list, modify.
            gameobject: Name or path of the target GameObject.
            component_type: Full type name of the component.
            properties: Component property values to set.
        """
        params = {"action": action}
        if gameobject:
            params["gameobject"] = gameobject
        if component_type:
            params["component_type"] = component_type
        if properties:
            params["properties"] = properties
        return await _relay_to_unity("manage_components", params)

    @mcp.tool()
    async def manage_asset(
        action: str,
        path: str = "",
        search_pattern: str = "",
        asset_type: str = "",
        new_path: str = "",
    ) -> dict[str, Any]:
        """Create, search, and organize assets.

        Args:
            action: The action: search, move, copy, delete, import, get_info.
            path: Asset path (relative to Assets/).
            search_pattern: Search pattern for finding assets.
            asset_type: Filter by asset type (e.g., Prefab, Material, Texture2D).
            new_path: Destination path for move/copy operations.
        """
        params = {"action": action}
        if path:
            params["path"] = path
        if search_pattern:
            params["search_pattern"] = search_pattern
        if asset_type:
            params["asset_type"] = asset_type
        if new_path:
            params["new_path"] = new_path
        return await _relay_to_unity("manage_asset", params)

    @mcp.tool()
    async def manage_prefabs(
        action: str,
        name: str = "",
        path: str = "",
        source_gameobject: str = "",
        position: list[float] | None = None,
        rotation: list[float] | None = None,
    ) -> dict[str, Any]:
        """Create, instantiate, and modify prefabs.

        Args:
            action: The action: create, instantiate, unpack, get_info, apply_overrides.
            name: Prefab name.
            path: Asset path for the prefab.
            source_gameobject: GameObject to create prefab from.
            position: Instantiation position as [x, y, z].
            rotation: Instantiation rotation as [x, y, z].
        """
        params = {"action": action}
        if name:
            params["name"] = name
        if path:
            params["path"] = path
        if source_gameobject:
            params["source_gameobject"] = source_gameobject
        if position:
            params["position"] = position
        if rotation:
            params["rotation"] = rotation
        return await _relay_to_unity("manage_prefabs", params)

    @mcp.tool()
    async def manage_editor(
        action: str,
        state: str = "",
    ) -> dict[str, Any]:
        """Control Editor state, play mode, undo/redo.

        Args:
            action: The action: play, pause, stop, step, undo, redo, get_state, compile.
            state: Target state for state transitions.
        """
        params = {"action": action}
        if state:
            params["state"] = state
        return await _relay_to_unity("manage_editor", params)

    @mcp.tool()
    async def read_console(
        count: int = 50,
        log_type: str = "all",
        clear: bool = False,
    ) -> dict[str, Any]:
        """Read Unity Console output.

        Args:
            count: Maximum number of log entries to return. Default: 50.
            log_type: Filter by type: all, log, warning, error. Default: all.
            clear: Whether to clear the console after reading.
        """
        params = {"count": count, "log_type": log_type, "clear": clear}
        return await _relay_to_unity("read_console", params)

    @mcp.tool()
    async def refresh_unity() -> dict[str, Any]:
        """Refresh the Unity asset database. Call after creating or modifying files."""
        return await _relay_to_unity("refresh_unity", {})

    @mcp.tool()
    async def run_tests(
        test_mode: str = "editmode",
        filter_pattern: str = "",
        category: str = "",
    ) -> dict[str, Any]:
        """Run Unity Test Framework tests.

        Args:
            test_mode: Test mode: editmode or playmode. Default: editmode.
            filter_pattern: Filter tests by name pattern.
            category: Filter tests by category.
        """
        params = {"test_mode": test_mode}
        if filter_pattern:
            params["filter_pattern"] = filter_pattern
        if category:
            params["category"] = category
        return await _relay_to_unity("run_tests", params)

    @mcp.tool()
    async def find_gameobjects(
        search_by: str = "name",
        query: str = "",
        tag: str = "",
        layer: str = "",
        component: str = "",
        include_inactive: bool = False,
    ) -> dict[str, Any]:
        """Search GameObjects by various criteria.

        Args:
            search_by: Search method: name, tag, layer, component. Default: name.
            query: Search query string.
            tag: Tag to search for (when search_by is 'tag').
            layer: Layer to search for (when search_by is 'layer').
            component: Component type to search for (when search_by is 'component').
            include_inactive: Whether to include inactive GameObjects.
        """
        params = {"search_by": search_by, "include_inactive": include_inactive}
        if query:
            params["query"] = query
        if tag:
            params["tag"] = tag
        if layer:
            params["layer"] = layer
        if component:
            params["component"] = component
        return await _relay_to_unity("find_gameobjects", params)

    @mcp.tool()
    async def execute_menu_item(menu_path: str) -> dict[str, Any]:
        """Execute a Unity Editor menu item.

        Args:
            menu_path: Full menu path (e.g., 'File/Save Project', 'GameObject/Create Empty').
        """
        return await _relay_to_unity("execute_menu_item", {"menu_path": menu_path})

    @mcp.tool()
    async def batch_execute(commands: list[dict[str, Any]]) -> dict[str, Any]:
        """Execute multiple operations in one call.

        Args:
            commands: List of command objects, each with 'tool' and 'params' keys.
        """
        return await _relay_to_unity("batch_execute", {"commands": commands})

    logger.info("All Unity tools registered successfully")
    return mcp




