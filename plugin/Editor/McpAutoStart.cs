using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Automatically restarts the MCP TCP listener after a domain reload.
    /// Unity 2021 aborts all threads during script recompilation (domain reload),
    /// which kills the TCP listener. This class ensures it restarts seamlessly.
    /// </summary>
    [InitializeOnLoad]
    public static class McpAutoStart
    {
        private const string AutoStartKey = "UnityMcp2021_AutoStart";
        private const string PortKey = "UnityMcp2021_Port";

        static McpAutoStart()
        {
            // Only auto-start if the server was previously running (user had clicked Start)
            // We use a session state flag to track this across domain reloads
            if (SessionState.GetBool(AutoStartKey, false))
            {
                EditorApplication.delayCall += AutoRestart;
            }
        }

        private static async void AutoRestart()
        {
            // Check if already running (shouldn't be after domain reload, but just in case)
            if (McpServerManager.IsRunning)
                return;

            int port = EditorPrefs.GetInt(PortKey, 8765);
            McpLog.Info($"[AutoStart] Restarting MCP TCP listener on port {port} after domain reload...");

            bool success = await McpServerManager.StartAsync(port);
            if (success)
            {
                McpLog.Info("[AutoStart] MCP TCP listener restarted successfully.");
            }
            else
            {
                McpLog.Error("[AutoStart] Failed to restart MCP TCP listener.");
            }
        }

        /// <summary>
        /// Call this when the server is started manually to enable auto-restart.
        /// </summary>
        public static void MarkServerRunning()
        {
            SessionState.SetBool(AutoStartKey, true);
        }

        /// <summary>
        /// Call this when the server is stopped manually to disable auto-restart.
        /// </summary>
        public static void MarkServerStopped()
        {
            SessionState.SetBool(AutoStartKey, false);
        }
    }
}
