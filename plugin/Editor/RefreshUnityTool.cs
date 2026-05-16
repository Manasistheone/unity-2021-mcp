using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Built-in tool handler for refreshing the Unity asset database.
    /// </summary>
    [McpForUnityTool(Name = "refresh_unity", AutoRegister = true)]
    public static class RefreshUnityTool
    {
        public static object HandleCommand(JObject @params)
        {
            AssetDatabase.Refresh();
            return new { success = true, result = "Asset database refreshed" };
        }
    }
}
