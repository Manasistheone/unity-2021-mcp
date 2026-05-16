using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "manage_gameobject", AutoRegister = true)]
    [ToolParameter("action", "The action: create, delete, find, modify, duplicate, rename")]
    [ToolParameter("name", "Name of the GameObject")]
    [ToolParameter("position", "World position as [x, y, z]")]
    [ToolParameter("rotation", "Euler rotation as [x, y, z]")]
    [ToolParameter("scale", "Local scale as [x, y, z]")]
    [ToolParameter("parent", "Name or path of the parent GameObject")]
    [ToolParameter("tag", "Tag to assign")]
    [ToolParameter("layer", "Layer to assign")]
    [ToolParameter("active", "Whether the GameObject should be active")]
    public static class ManageGameObjectTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            string action = toolParams.GetRequired<string>("action");

            switch (action.ToLowerInvariant())
            {
                case "create": return CreateGameObject(toolParams);
                case "delete": return DeleteGameObject(toolParams);
                case "modify": return ModifyGameObject(toolParams);
                case "duplicate": return DuplicateGameObject(toolParams);
                case "rename": return RenameGameObject(toolParams);
                default:
                    return new { success = false, error = $"Unknown action: '{action}'" };
            }
        }

        private static object CreateGameObject(ToolParams toolParams)
        {
            string name = toolParams.Get<string>("name", "GameObject");
            string parent = toolParams.Get<string>("parent", "");
            string tag = toolParams.Get<string>("tag", "");
            string layer = toolParams.Get<string>("layer", "");
            bool active = toolParams.Get<bool>("active", true);

            // Determine primitive type from name hints
            GameObject go;
            string lowerName = name.ToLower();
            if (lowerName.Contains("sphere"))
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            else if (lowerName.Contains("cube"))
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            else if (lowerName.Contains("cylinder"))
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            else if (lowerName.Contains("capsule"))
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            else if (lowerName.Contains("plane"))
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            else if (lowerName.Contains("quad"))
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            else
                go = new GameObject();

            go.name = name;

            // Position
            JToken posToken = toolParams.GetRaw("position");
            if (posToken != null && posToken.Type == JTokenType.Array)
            {
                var arr = posToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.position = new Vector3(arr[0], arr[1], arr[2]);
            }

            // Rotation
            JToken rotToken = toolParams.GetRaw("rotation");
            if (rotToken != null && rotToken.Type == JTokenType.Array)
            {
                var arr = rotToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.eulerAngles = new Vector3(arr[0], arr[1], arr[2]);
            }

            // Scale
            JToken scaleToken = toolParams.GetRaw("scale");
            if (scaleToken != null && scaleToken.Type == JTokenType.Array)
            {
                var arr = scaleToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.localScale = new Vector3(arr[0], arr[1], arr[2]);
            }

            // Parent
            if (!string.IsNullOrEmpty(parent))
            {
                GameObject parentGo = GameObject.Find(parent);
                if (parentGo != null)
                    go.transform.SetParent(parentGo.transform, true);
            }

            // Tag
            if (!string.IsNullOrEmpty(tag))
            {
                try { go.tag = tag; } catch { }
            }

            // Layer
            if (!string.IsNullOrEmpty(layer))
            {
                int layerIndex = LayerMask.NameToLayer(layer);
                if (layerIndex >= 0) go.layer = layerIndex;
            }

            go.SetActive(active);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            return new
            {
                success = true,
                result = new
                {
                    name = go.name,
                    position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                    instanceId = go.GetInstanceID()
                }
            };
        }

        private static object DeleteGameObject(ToolParams toolParams)
        {
            string name = toolParams.Get<string>("name", "");
            if (string.IsNullOrEmpty(name))
                return new { success = false, error = "Missing 'name' parameter" };

            GameObject go = GameObject.Find(name);
            if (go == null)
                return new { success = false, error = $"GameObject '{name}' not found" };

            Undo.DestroyObjectImmediate(go);
            return new { success = true, result = $"Deleted '{name}'" };
        }

        private static object ModifyGameObject(ToolParams toolParams)
        {
            string name = toolParams.Get<string>("name", "");
            if (string.IsNullOrEmpty(name))
                return new { success = false, error = "Missing 'name' parameter" };

            GameObject go = GameObject.Find(name);
            if (go == null)
                return new { success = false, error = $"GameObject '{name}' not found" };

            Undo.RecordObject(go.transform, $"Modify {name}");

            JToken posToken = toolParams.GetRaw("position");
            if (posToken != null && posToken.Type == JTokenType.Array)
            {
                var arr = posToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.position = new Vector3(arr[0], arr[1], arr[2]);
            }

            JToken rotToken = toolParams.GetRaw("rotation");
            if (rotToken != null && rotToken.Type == JTokenType.Array)
            {
                var arr = rotToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.eulerAngles = new Vector3(arr[0], arr[1], arr[2]);
            }

            JToken scaleToken = toolParams.GetRaw("scale");
            if (scaleToken != null && scaleToken.Type == JTokenType.Array)
            {
                var arr = scaleToken.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                    go.transform.localScale = new Vector3(arr[0], arr[1], arr[2]);
            }

            bool active = toolParams.Get<bool>("active", go.activeSelf);
            go.SetActive(active);

            return new
            {
                success = true,
                result = new
                {
                    name = go.name,
                    position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
                }
            };
        }

        private static object DuplicateGameObject(ToolParams toolParams)
        {
            string name = toolParams.Get<string>("name", "");
            if (string.IsNullOrEmpty(name))
                return new { success = false, error = "Missing 'name' parameter" };

            GameObject go = GameObject.Find(name);
            if (go == null)
                return new { success = false, error = $"GameObject '{name}' not found" };

            GameObject copy = Object.Instantiate(go);
            copy.name = go.name + " (Copy)";
            Undo.RegisterCreatedObjectUndo(copy, $"Duplicate {name}");

            return new { success = true, result = new { name = copy.name, instanceId = copy.GetInstanceID() } };
        }

        private static object RenameGameObject(ToolParams toolParams)
        {
            string name = toolParams.Get<string>("name", "");
            string newName = toolParams.Get<string>("new_name", "");
            if (string.IsNullOrEmpty(name))
                return new { success = false, error = "Missing 'name' parameter" };
            if (string.IsNullOrEmpty(newName))
                return new { success = false, error = "Missing 'new_name' parameter" };

            GameObject go = GameObject.Find(name);
            if (go == null)
                return new { success = false, error = $"GameObject '{name}' not found" };

            Undo.RecordObject(go, $"Rename {name}");
            go.name = newName;
            return new { success = true, result = $"Renamed '{name}' to '{newName}'" };
        }
    }
}
