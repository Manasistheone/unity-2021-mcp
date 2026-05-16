using System;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Marks a class as an MCP resource handler for auto-discovery by the CommandRegistry.
    /// The class must expose a public static HandleCommand(JObject) method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpForUnityResourceAttribute : Attribute
    {
        /// <summary>
        /// The resource name used to route requests to this resource.
        /// If null, the CommandRegistry derives the name from the class name
        /// by converting PascalCase to snake_case (e.g., EditorState → editor_state).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Human-readable description of what this resource provides.
        /// Used by AI assistants to understand resource capabilities.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Create an MCP resource attribute with auto-generated resource name.
        /// The resource name will be derived from the class name (PascalCase → snake_case).
        /// </summary>
        public McpForUnityResourceAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// Create an MCP resource attribute with an explicit resource name.
        /// </summary>
        /// <param name="name">The resource name (e.g., "editor_state")</param>
        public McpForUnityResourceAttribute(string name)
        {
            Name = name;
        }
    }
}
