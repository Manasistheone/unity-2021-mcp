using System;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Defines a parameter for a custom MCP tool.
    /// Applied to the tool class (AllowMultiple = true) to declare each parameter
    /// the tool accepts, including its name, description, type, and default value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ToolParameterAttribute : Attribute
    {
        /// <summary>
        /// The parameter name as exposed to AI assistants (e.g., "action", "page_size").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Human-readable description of what this parameter does.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The parameter type as a string (e.g., "string", "int", "bool", "float", "object").
        /// Used for schema generation and documentation.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Whether this parameter is required for tool invocation.
        /// Defaults to false.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Default value for the parameter as a string representation.
        /// Null indicates no default value.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Create a tool parameter attribute with the specified name.
        /// </summary>
        /// <param name="name">The parameter name (e.g., "action")</param>
        public ToolParameterAttribute(string name)
        {
            Name = name;
            Type = "string";
            Required = false;
            DefaultValue = null;
        }

        /// <summary>
        /// Create a tool parameter attribute with name and description.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="description">Human-readable description</param>
        public ToolParameterAttribute(string name, string description)
        {
            Name = name;
            Description = description;
            Type = "string";
            Required = false;
            DefaultValue = null;
        }
    }
}
