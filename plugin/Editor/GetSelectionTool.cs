using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "get_selection", AutoRegister = true)]
    [ToolParameter("include_components", "Whether to include component details on selected GameObjects")]
    [ToolParameter("include_children", "Whether to include direct children names")]
    public static class GetSelectionTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            bool includeComponents = toolParams.Get<bool>("include_components", true);
            bool includeChildren = toolParams.Get<bool>("include_children", false);

            // Check for hierarchy selection (GameObjects)
            GameObject[] selectedGameObjects = Selection.gameObjects;

            // Check for project window selection (Assets)
            Object[] selectedAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);

            if ((selectedGameObjects == null || selectedGameObjects.Length == 0) &&
                (selectedAssets == null || selectedAssets.Length == 0))
            {
                return new { success = true, result = new { message = "Nothing is selected.", selectionType = "none" } };
            }

            var result = new Dictionary<string, object>();

            // Hierarchy selection
            if (selectedGameObjects != null && selectedGameObjects.Length > 0)
            {
                var gameObjects = new List<object>();
                foreach (GameObject go in selectedGameObjects)
                {
                    var goInfo = new Dictionary<string, object>
                    {
                        ["name"] = go.name,
                        ["path"] = GetGameObjectPath(go),
                        ["tag"] = go.tag,
                        ["layer"] = LayerMask.LayerToName(go.layer),
                        ["active"] = go.activeSelf,
                        ["position"] = new[] { go.transform.position.x, go.transform.position.y, go.transform.position.z },
                        ["rotation"] = new[] { go.transform.eulerAngles.x, go.transform.eulerAngles.y, go.transform.eulerAngles.z },
                        ["scale"] = new[] { go.transform.localScale.x, go.transform.localScale.y, go.transform.localScale.z },
                        ["childCount"] = go.transform.childCount
                    };

                    if (includeComponents)
                    {
                        var components = new List<object>();
                        foreach (Component comp in go.GetComponents<Component>())
                        {
                            if (comp == null) continue;
                            components.Add(new { type = comp.GetType().Name, fullType = comp.GetType().FullName });
                        }
                        goInfo["components"] = components;
                    }

                    if (includeChildren && go.transform.childCount > 0)
                    {
                        var children = new List<string>();
                        for (int i = 0; i < go.transform.childCount; i++)
                        {
                            children.Add(go.transform.GetChild(i).name);
                        }
                        goInfo["children"] = children;
                    }

                    gameObjects.Add(goInfo);
                }

                result["selectionType"] = "hierarchy";
                result["gameObjects"] = gameObjects;
                result["count"] = gameObjects.Count;
            }

            // Project window selection (assets)
            if (selectedAssets != null && selectedAssets.Length > 0)
            {
                // Filter out assets that are also scene GameObjects to avoid duplicates
                var assets = new List<object>();
                foreach (Object asset in selectedAssets)
                {
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    assets.Add(new
                    {
                        name = asset.name,
                        type = asset.GetType().Name,
                        path = assetPath
                    });
                }

                if (assets.Count > 0)
                {
                    // If we already have hierarchy selection, mark as mixed
                    if (result.ContainsKey("selectionType"))
                    {
                        result["selectionType"] = "mixed";
                    }
                    else
                    {
                        result["selectionType"] = "project";
                    }
                    result["assets"] = assets;
                    result["assetCount"] = assets.Count;
                }
            }

            result["success"] = true;
            return new { success = true, result };
        }

        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
