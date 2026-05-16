# Adding Custom Tools

You can extend Unity 2021 MCP with your own tools. A custom tool requires changes in both the Unity plugin (C#) and the Python server.

## Step 1: Create the Unity Handler

Create a new C# file in any `Editor/` folder in your Unity project.

```csharp
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "my_custom_tool", AutoRegister = true)]
    [ToolParameter("action", "The action to perform: greet, count")]
    [ToolParameter("target", "Target name (optional)")]
    public static class MyCustomTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            var action = toolParams.GetRequired<string>("action");
            var target = toolParams.Get<string>("target", "World");

            switch (action)
            {
                case "greet":
                    return new { success = true, message = $"Hello, {target}!" };

                case "count":
                    int count = GameObject.FindObjectsOfType<GameObject>().Length;
                    return new { success = true, count = count };

                default:
                    return new { success = false, error = $"Unknown action: {action}" };
            }
        }
    }
}
```

### Key Rules

- The class must be `public static`.
- It must have `[McpForUnityTool(Name = "...", AutoRegister = true)]`.
- `[ToolParameter]` attributes go on the **class**, not the method.
- The method signature must be exactly: `public static object HandleCommand(JObject @params)`.
- The tool name must be unique across all registered tools.
- Return an anonymous object — it will be serialized to JSON automatically.

### Async Handlers

For long-running operations, you can return a `Task<object>`:

```csharp
public static async Task<object> HandleCommand(JObject @params)
{
    await Task.Delay(1000); // Simulate work
    return new { success = true, result = "done" };
}
```

The dispatcher will poll the task until completion.

## Step 2: Register in the Python Server

Add a new tool function in `server/src/unity_2021_mcp/server.py`:

```python
@mcp.tool()
async def my_custom_tool(action: str, target: str = "") -> dict:
    """My custom tool that does something in Unity.

    Args:
        action: The action to perform (greet, count).
        target: Target name (optional, default: World).
    """
    return await _relay_to_unity("my_custom_tool", {
        "action": action,
        "target": target,
    })
```

### Key Rules

- The function name becomes the MCP tool name visible to AI clients.
- The tool name in `_relay_to_unity()` must match the `Name` in the C# `[McpForUnityTool]` attribute.
- The docstring becomes the tool description shown to AI clients.
- Parameter type hints and defaults are used by FastMCP to generate the tool schema.

## Step 3: Test

1. Restart Unity (or wait for domain reload) to pick up the new C# handler.
2. Restart the Python server to register the new tool.
3. From your MCP client, ask the AI to use your tool:

> "Use my_custom_tool to greet Alice"

## Auto-Approve (Optional)

To skip confirmation prompts for your custom tool, add it to the `autoApprove` list in your MCP client config:

```json
{
  "mcpServers": {
    "unity-2021-mcp": {
      "command": "uv",
      "args": ["run", "--project", "<path>/server", "mcp-for-unity-2021", "--transport", "stdio"],
      "autoApprove": ["my_custom_tool"]
    }
  }
}
```

---

← [Unity Plugin](Unity-Plugin) | [Configuration](Configuration) →
