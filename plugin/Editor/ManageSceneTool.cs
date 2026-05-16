using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "manage_scene", AutoRegister = true)]
    [ToolParameter("action", "The action: get_hierarchy, get_active, load, save, new")]
    [ToolParameter("scene_name", "Name of the scene")]
    [ToolParameter("path", "Path to the scene asset")]
    public static class ManageSceneTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            string action = toolParams.GetRequired<string>("action");

            switch (action.ToLowerInvariant())
            {
                case "get_hierarchy":
                    return GetHierarchy();
                case "get_active":
                    var scene = SceneManager.GetActiveScene();
                    return new
                    {
                        success = true,
                        result = new
                        {
                            name = scene.name,
                            path = scene.path,
                            isDirty = scene.isDirty,
                            rootCount = scene.rootCount
                        }
                    };
                case "save":
                    EditorSceneManager.SaveOpenScenes();
                    return new { success = true, result = "Scene saved" };
                case "load":
                    string scenePath = toolParams.Get<string>("path", "");
                    if (string.IsNullOrEmpty(scenePath))
                        return new { success = false, error = "Missing 'path' parameter for load action" };
                    EditorSceneManager.OpenScene(scenePath);
                    return new { success = true, result = $"Loaded scene: {scenePath}" };
                case "new":
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    return new { success = true, result = "Created new scene" };
                default:
                    return new { success = false, error = $"Unknown action: '{action}'" };
            }
        }

        private static object GetHierarchy()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var hierarchy = new List<object>();
            foreach (var root in roots)
                hierarchy.Add(BuildHierarchyNode(root, 0, 3));
            return new { success = true, result = new { sceneName = scene.name, rootCount = roots.Length, hierarchy } };
        }

        private static object BuildHierarchyNode(GameObject go, int depth, int maxDepth)
        {
            var children = new List<object>();
            if (depth < maxDepth)
                for (int i = 0; i < go.transform.childCount; i++)
                    children.Add(BuildHierarchyNode(go.transform.GetChild(i).gameObject, depth + 1, maxDepth));
            return new
            {
                name = go.name,
                active = go.activeSelf,
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                childCount = go.transform.childCount,
                components = GetComponentNames(go),
                children
            };
        }

        private static List<string> GetComponentNames(GameObject go)
        {
            var names = new List<string>();
            foreach (var comp in go.GetComponents<Component>())
                if (comp != null) names.Add(comp.GetType().Name);
            return names;
        }
    }
}
