using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "manage_editor", AutoRegister = true)]
    [ToolParameter("action", "The action: play, pause, stop, step, undo, redo, get_state, compile")]
    public static class ManageEditorTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            string action = toolParams.GetRequired<string>("action");

            switch (action.ToLowerInvariant())
            {
                case "get_state":
                    return new
                    {
                        success = true,
                        result = new
                        {
                            unityVersion = Application.unityVersion,
                            isPlaying = EditorApplication.isPlaying,
                            isPaused = EditorApplication.isPaused,
                            isCompiling = EditorApplication.isCompiling,
                            currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                            platform = EditorUserBuildSettings.activeBuildTarget.ToString()
                        }
                    };
                case "play":
                    EditorApplication.isPlaying = true;
                    return new { success = true, result = "Entered Play Mode" };
                case "pause":
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                    return new { success = true, result = $"Paused: {EditorApplication.isPaused}" };
                case "stop":
                    EditorApplication.isPlaying = false;
                    return new { success = true, result = "Exited Play Mode" };
                case "step":
                    EditorApplication.Step();
                    return new { success = true, result = "Stepped one frame" };
                case "undo":
                    Undo.PerformUndo();
                    return new { success = true, result = "Undo performed" };
                case "redo":
                    Undo.PerformRedo();
                    return new { success = true, result = "Redo performed" };
                case "compile":
                    AssetDatabase.Refresh();
                    return new { success = true, result = "Compilation triggered" };
                default:
                    return new { success = false, error = $"Unknown action: '{action}'" };
            }
        }
    }
}
