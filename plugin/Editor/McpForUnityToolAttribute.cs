using System;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Marks a class as an MCP tool handler for auto-discovery by the CommandRegistry.
    /// The class must expose a public static HandleCommand(JObject) method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpForUnityToolAttribute : Attribute
    {
        /// <summary>
        /// The tool/command name used to route requests to this tool.
        /// If null, the CommandRegistry derives the name from the class name
        /// by converting PascalCase to snake_case (e.g., ManageGameobject → manage_gameobject).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Controls whether this tool is automatically registered with the MCP server.
        /// Defaults to true so most tools opt-in automatically.
        /// Set to false for tools that should only be registered on demand.
        /// </summary>
        public bool AutoRegister { get; set; }

        /// <summary>
        /// Human-readable description of what this tool does.
        /// Used by AI assistants to understand tool capabilities.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Enables the polling middleware for long-running tools.
        /// When true, Unity returns a PendingResponse and the Python side
        /// polls using <see cref="PollAction"/> until completion.
        /// </summary>
        public bool RequiresPolling { get; set; }

        /// <summary>
        /// The action name to use when polling for status. Defaults to "status".
        /// </summary>
        public string PollAction { get; set; }

        /// <summary>
        /// Create an MCP tool attribute with auto-generated command name.
        /// The command name will be derived from the class name (PascalCase → snake_case).
        /// </summary>
        public McpForUnityToolAttribute()
        {
            Name = null;
            AutoRegister = true;
            RequiresPolling = false;
            PollAction = "status";
        }

        /// <summary>
        /// Create an MCP tool attribute with an explicit command name.
        /// </summary>
        /// <param name="name">The command name (e.g., "manage_gameobject")</param>
        public McpForUnityToolAttribute(string name)
        {
            Name = name;
            AutoRegister = true;
            RequiresPolling = false;
            PollAction = "status";
        }
    }
}
