using UnityEngine;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Logging facade for the MCP 2021 plugin.
    /// Wraps Unity Debug.Log/LogWarning/LogError with a consistent prefix
    /// and supports a debug toggle for verbose logging (raw payloads, timing).
    /// </summary>
    public static class McpLog
    {
        private const string Prefix = "[MCP 2021]";

        /// <summary>
        /// When true, enables verbose logging including raw command payloads,
        /// transport-level handshake details, and command dispatch timing.
        /// Toggled from the Editor Window settings.
        /// </summary>
        public static bool DebugEnabled { get; set; }

        /// <summary>
        /// Logs an error message to the Unity Console.
        /// Always logged regardless of DebugEnabled state.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }

        /// <summary>
        /// Logs a warning message to the Unity Console.
        /// Always logged regardless of DebugEnabled state.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        /// <summary>
        /// Logs an informational message to the Unity Console.
        /// Only logged when DebugEnabled is true.
        /// </summary>
        /// <param name="message">The info message to log.</param>
        public static void Info(string message)
        {
            if (DebugEnabled)
            {
                Debug.Log($"{Prefix} {message}");
            }
        }
    }
}
