# Available Tools

Unity 2021 MCP exposes 14 tools to AI assistants. Each tool maps to a C# handler in the Unity plugin.

## Tool Summary

| Tool | Description |
|------|-------------|
| `manage_gameobject` | Create, modify, transform, and delete GameObjects |
| `manage_scene` | Load, save, and query scene hierarchy |
| `manage_script` | Create, read, and modify C# scripts |
| `manage_material` | Create and modify materials and shaders |
| `manage_components` | Add, remove, and configure components |
| `manage_asset` | Create, search, and organize assets |
| `manage_prefabs` | Create, instantiate, and modify prefabs |
| `manage_editor` | Control Editor state, play mode, undo/redo |
| `read_console` | Read Unity Console output |
| `refresh_unity` | Refresh the asset database |
| `run_tests` | Run Unity Test Framework tests |
| `find_gameobjects` | Search GameObjects by various criteria |
| `execute_menu_item` | Execute Unity Editor menu items |
| `batch_execute` | Execute multiple operations in one call |

---

## Tool Details

### `manage_gameobject`

Create, modify, transform, and delete GameObjects.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `create`, `delete`, `modify`, `get_info` |
| `name` | string | GameObject name |
| `position` | float[] | `[x, y, z]` world position |
| `rotation` | float[] | `[x, y, z]` Euler angles |
| `scale` | float[] | `[x, y, z]` local scale |
| `parent` | string | Parent GameObject name |

**Examples:**
```
Create a cube named "Player" at position (0, 1, 0)
Delete the GameObject named "OldEnemy"
Move "Player" to position (5, 0, 3)
```

---

### `manage_scene`

Load, save, create, and query scenes.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `get_hierarchy`, `load`, `save`, `new` |
| `scene_path` | string | Path to scene asset (e.g., `Assets/Scenes/Main.unity`) |
| `cursor` | int | Pagination cursor for large hierarchies |
| `page_size` | int | Items per page (default: 50) |

---

### `manage_editor`

Control editor state.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `get_state`, `play`, `pause`, `stop`, `undo`, `redo` |

**Returns** (for `get_state`):
- Unity version
- Current scene name
- Play mode status
- Build platform

---

### `find_gameobjects`

Search for GameObjects by name, tag, layer, or component.

| Parameter | Type | Description |
|-----------|------|-------------|
| `search_by` | string | `name`, `tag`, `layer`, `component` |
| `query` | string | Search string |
| `tag` | string | Tag to search for (when `search_by` is `tag`) |

---

### `manage_script`

Create and read C# scripts.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `create`, `read` |
| `script_name` | string | Name of the script (without `.cs`) |
| `path` | string | Asset path (e.g., `Assets/Scripts/`) |
| `content` | string | Full script content (for `create`) |

---

### `manage_material`

Create and modify materials. Automatically detects the project's render pipeline:

- **Built-in** → Standard shader
- **URP** → Universal Render Pipeline/Lit
- **HDRP** → HDRP/Lit

---

### `manage_components`

Add, remove, and configure components on GameObjects.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `add`, `remove`, `get`, `set_property` |
| `game_object` | string | Target GameObject name |
| `component_type` | string | Component type name |

---

### `manage_asset`

Create, search, and organize project assets.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `search`, `create_folder`, `move`, `delete` |
| `query` | string | Search query |
| `path` | string | Asset path |

---

### `manage_prefabs`

Create, instantiate, and modify prefabs.

| Parameter | Type | Description |
|-----------|------|-------------|
| `action` | string | `create`, `instantiate`, `apply_overrides` |
| `name` | string | Prefab name |
| `path` | string | Asset path |

---

### `read_console`

Read Unity Console log output.

| Parameter | Type | Description |
|-----------|------|-------------|
| `log_type` | string | `all`, `error`, `warning`, `info` |
| `count` | int | Number of entries to return |

---

### `refresh_unity`

Force an `AssetDatabase.Refresh()`. No parameters required.

---

### `run_tests`

Execute Unity Test Framework tests.

| Parameter | Type | Description |
|-----------|------|-------------|
| `mode` | string | `editmode`, `playmode` |
| `filter` | string | Test name filter |

---

### `execute_menu_item`

Execute any Unity Editor menu item by path.

| Parameter | Type | Description |
|-----------|------|-------------|
| `menu_path` | string | Full menu path (e.g., `GameObject/3D Object/Cube`) |

---

### `batch_execute`

Execute multiple tool commands in a single call for efficiency.

| Parameter | Type | Description |
|-----------|------|-------------|
| `commands` | array | Array of `{tool, params}` objects |

**Example:**
```json
{
  "commands": [
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "A"}},
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "B"}}
  ]
}
```

---

← [Setup Guide](Setup-Guide) | [Python Server](Python-Server) →
