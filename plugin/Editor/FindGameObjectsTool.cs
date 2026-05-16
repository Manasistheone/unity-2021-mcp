using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "find_gameobjects", AutoRegister = true)]
    [ToolParameter("search_by", "Search method: name, tag, layer, component")]
    [ToolParameter("query", "Search query string")]
    [ToolParameter("tag", "Tag to search for")]
    [ToolParameter("component", "Component type to search for")]
    [ToolParameter("include_inactive", "Include inactive GameObjects")]
    public static class FindGameObjectsTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            string searchBy = toolParams.Get<string>("search_by", "name");
            string query = toolParams.Get<string>("query", "");
            string tag = toolParams.Get<string>("tag", "");
            string component = toolParams.Get<string>("component", "");
            bool includeInactive = toolParams.Get<bool>("include_inactive", false);

            var results = new List<object>();
            switch (searchBy.ToLowerInvariant())
            {
                case "name":
                    FindByName(query, includeInactive, results);
                    break;
                case "tag":
                    FindByTag(string.IsNullOrEmpty(tag) ? query : tag, results);
                    break;
                case "component":
                    FindByComponent(string.IsNullOrEmpty(component) ? query : component, includeInactive, results);
                    break;
                default:
                    FindByName(query, includeInactive, results);
                    break;
            }
            return new { success = true, result = new { count = results.Count, gameObjects = results } };
        }

        private static void FindByName(string query, bool includeInactive, List<object> results)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                SearchRecursive(root, query, includeInactive, results);
        }

        private static void SearchRecursive(GameObject go, string query, bool includeInactive, List<object> results)
        {
            if (!includeInactive && !go.activeInHierarchy) return;
            if (string.IsNullOrEmpty(query) || go.name.ToLower().Contains(query.ToLower()))
                results.Add(GameObjectToInfo(go));
            for (int i = 0; i < go.transform.childCount; i++)
                SearchRecursive(go.transform.GetChild(i).gameObject, query, includeInactive, results);
        }

        private static void FindByTag(string tag, List<object> results)
        {
            if (string.IsNullOrEmpty(tag)) return;
            try
            {
                foreach (var go in GameObject.FindGameObjectsWithTag(tag))
                    results.Add(GameObjectToInfo(go));
            }
            catch (UnityException) { }
        }

        private static void FindByComponent(string componentName, bool includeInactive, List<object> results)
        {
            if (string.IsNullOrEmpty(componentName)) return;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                SearchByComponentRecursive(root, componentName, includeInactive, results);
        }

        private static void SearchByComponentRecursive(GameObject go, string componentName, bool includeInactive, List<object> results)
        {
            if (!includeInactive && !go.activeInHierarchy) return;
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name.ToLower().Contains(componentName.ToLower()))
                {
                    results.Add(GameObjectToInfo(go));
                    break;
                }
            }
            for (int i = 0; i < go.transform.childCount; i++)
                SearchByComponentRecursive(go.transform.GetChild(i).gameObject, componentName, includeInactive, results);
        }

        private static object GameObjectToInfo(GameObject go)
        {
            var componentNames = new List<string>();
            foreach (var comp in go.GetComponents<Component>())
                if (comp != null) componentNames.Add(comp.GetType().Name);
            return new
            {
                name = go.name,
                path = GetGameObjectPath(go),
                active = go.activeSelf,
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                components = componentNames
            };
        }

        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
            return path;
        }
    }
}
