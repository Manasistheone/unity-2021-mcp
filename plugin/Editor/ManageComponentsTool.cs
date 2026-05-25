using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "manage_components", AutoRegister = true)]
    [ToolParameter("action", "The action: get, list, add, remove, modify")]
    [ToolParameter("gameobject", "Name or path of the target GameObject")]
    [ToolParameter("component_type", "Full or short type name of the component")]
    [ToolParameter("properties", "Component property values to set (for modify action)")]
    [ToolParameter("include_fields", "Whether to include fields in get output (default true)")]
    [ToolParameter("include_properties", "Whether to include properties in get output (default true)")]
    public static class ManageComponentsTool
    {
        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            string action = toolParams.GetRequired<string>("action");

            switch (action.ToLowerInvariant())
            {
                case "get": return GetComponentProperties(toolParams);
                case "list": return ListComponents(toolParams);
                case "add": return AddComponent(toolParams);
                case "remove": return RemoveComponent(toolParams);
                case "modify": return ModifyComponent(toolParams);
                default:
                    return new { success = false, error = $"Unknown action: '{action}'" };
            }
        }

        private static object GetComponentProperties(ToolParams toolParams)
        {
            string gameobjectPath = toolParams.GetRequired<string>("gameobject");
            string componentType = toolParams.GetRequired<string>("component_type");
            bool includeFields = toolParams.Get<bool>("include_fields", true);
            bool includeProperties = toolParams.Get<bool>("include_properties", true);

            GameObject go = FindGameObject(gameobjectPath);
            if (go == null)
                return new { success = false, error = $"GameObject '{gameobjectPath}' not found" };

            Component comp = FindComponent(go, componentType);
            if (comp == null)
                return new { success = false, error = $"Component '{componentType}' not found on '{go.name}'" };

            var result = new Dictionary<string, object>
            {
                ["gameObject"] = go.name,
                ["componentType"] = comp.GetType().Name,
                ["fullType"] = comp.GetType().FullName
            };

            if (includeFields)
            {
                result["fields"] = GetSerializedFields(comp);
            }

            if (includeProperties)
            {
                result["properties"] = GetPublicProperties(comp);
            }

            return new { success = true, result };
        }

        private static object ListComponents(ToolParams toolParams)
        {
            string gameobjectPath = toolParams.GetRequired<string>("gameobject");

            GameObject go = FindGameObject(gameobjectPath);
            if (go == null)
                return new { success = false, error = $"GameObject '{gameobjectPath}' not found" };

            var components = new List<object>();
            foreach (Component comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                components.Add(new
                {
                    type = comp.GetType().Name,
                    fullType = comp.GetType().FullName,
                    enabled = IsComponentEnabled(comp)
                });
            }

            return new { success = true, result = new { gameObject = go.name, components, count = components.Count } };
        }

        private static object AddComponent(ToolParams toolParams)
        {
            string gameobjectPath = toolParams.GetRequired<string>("gameobject");
            string componentType = toolParams.GetRequired<string>("component_type");

            GameObject go = FindGameObject(gameobjectPath);
            if (go == null)
                return new { success = false, error = $"GameObject '{gameobjectPath}' not found" };

            Type type = FindType(componentType);
            if (type == null)
                return new { success = false, error = $"Component type '{componentType}' not found" };

            Undo.RecordObject(go, $"Add {componentType}");
            Component comp = go.AddComponent(type);
            if (comp == null)
                return new { success = false, error = $"Failed to add component '{componentType}'" };

            return new { success = true, result = $"Added '{type.Name}' to '{go.name}'" };
        }

        private static object RemoveComponent(ToolParams toolParams)
        {
            string gameobjectPath = toolParams.GetRequired<string>("gameobject");
            string componentType = toolParams.GetRequired<string>("component_type");

            GameObject go = FindGameObject(gameobjectPath);
            if (go == null)
                return new { success = false, error = $"GameObject '{gameobjectPath}' not found" };

            Component comp = FindComponent(go, componentType);
            if (comp == null)
                return new { success = false, error = $"Component '{componentType}' not found on '{go.name}'" };

            Undo.DestroyObjectImmediate(comp);
            return new { success = true, result = $"Removed '{componentType}' from '{go.name}'" };
        }

        private static object ModifyComponent(ToolParams toolParams)
        {
            string gameobjectPath = toolParams.GetRequired<string>("gameobject");
            string componentType = toolParams.GetRequired<string>("component_type");
            JToken propertiesToken = toolParams.GetRaw("properties");

            if (propertiesToken == null || propertiesToken.Type != JTokenType.Object)
                return new { success = false, error = "Missing or invalid 'properties' parameter (must be an object)" };

            GameObject go = FindGameObject(gameobjectPath);
            if (go == null)
                return new { success = false, error = $"GameObject '{gameobjectPath}' not found" };

            Component comp = FindComponent(go, componentType);
            if (comp == null)
                return new { success = false, error = $"Component '{componentType}' not found on '{go.name}'" };

            Undo.RecordObject(comp, $"Modify {componentType}");

            var propsObj = (JObject)propertiesToken;
            var modified = new List<string>();
            var errors = new List<string>();

            foreach (var kvp in propsObj)
            {
                string fieldName = kvp.Key;
                JToken value = kvp.Value;

                if (TrySetFieldOrProperty(comp, fieldName, value))
                {
                    modified.Add(fieldName);
                }
                else
                {
                    errors.Add($"Could not set '{fieldName}'");
                }
            }

            EditorUtility.SetDirty(comp);

            return new { success = true, result = new { modified, errors, componentType = comp.GetType().Name } };
        }

        #region Helpers

        private static GameObject FindGameObject(string nameOrPath)
        {
            // Try finding by full path first
            GameObject go = GameObject.Find(nameOrPath);
            if (go != null) return go;

            // Try finding by path with leading slash
            go = GameObject.Find("/" + nameOrPath);
            return go;
        }

        private static Component FindComponent(GameObject go, string componentType)
        {
            foreach (Component comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                string fullTypeName = comp.GetType().FullName;

                if (string.Equals(typeName, componentType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fullTypeName, componentType, StringComparison.OrdinalIgnoreCase))
                {
                    return comp;
                }
            }
            return null;
        }

        private static Type FindType(string typeName)
        {
            // Search all loaded assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type == null) continue;
                        if (string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(type.FullName, typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (typeof(Component).IsAssignableFrom(type))
                                return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { }
            }
            return null;
        }

        private static bool IsComponentEnabled(Component comp)
        {
            if (comp is Behaviour behaviour) return behaviour.enabled;
            if (comp is Renderer renderer) return renderer.enabled;
            if (comp is Collider collider) return collider.enabled;
            return true;
        }

        private static List<object> GetSerializedFields(Component comp)
        {
            var fields = new List<object>();
            Type type = comp.GetType();

            // Use SerializedObject for accurate Unity serialization data
            var serializedObj = new SerializedObject(comp);
            var iterator = serializedObj.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Skip the m_Script field
                if (iterator.name == "m_Script") continue;

                fields.Add(new
                {
                    name = iterator.name,
                    displayName = iterator.displayName,
                    type = iterator.propertyType.ToString(),
                    value = GetSerializedPropertyValue(iterator)
                });
            }

            serializedObj.Dispose();
            return fields;
        }

        private static object GetSerializedPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Enum:
                    return prop.enumNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0
                        ? prop.enumNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null
                        ? new { name = prop.objectReferenceValue.name, type = prop.objectReferenceValue.GetType().Name }
                        : null;
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return new { r = c.r, g = c.g, b = c.b, a = c.a };
                case SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return new { x = v2.x, y = v2.y };
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return new { x = v3.x, y = v3.y, z = v3.z };
                case SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
                case SerializedPropertyType.Rect:
                    var rect = prop.rectValue;
                    return new { x = rect.x, y = rect.y, width = rect.width, height = rect.height };
                case SerializedPropertyType.Bounds:
                    var bounds = prop.boundsValue;
                    return new
                    {
                        center = new { x = bounds.center.x, y = bounds.center.y, z = bounds.center.z },
                        size = new { x = bounds.size.x, y = bounds.size.y, z = bounds.size.z }
                    };
                case SerializedPropertyType.Quaternion:
                    var q = prop.quaternionValue;
                    return new { x = q.x, y = q.y, z = q.z, w = q.w };
                case SerializedPropertyType.Vector2Int:
                    var v2i = prop.vector2IntValue;
                    return new { x = v2i.x, y = v2i.y };
                case SerializedPropertyType.Vector3Int:
                    var v3i = prop.vector3IntValue;
                    return new { x = v3i.x, y = v3i.y, z = v3i.z };
                case SerializedPropertyType.ArraySize:
                    return prop.intValue;
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue != null
                        ? $"AnimationCurve ({prop.animationCurveValue.length} keys)"
                        : null;
                case SerializedPropertyType.Generic:
                    // For complex types, try to read child properties
                    return GetGenericPropertyValue(prop);
                default:
                    return $"({prop.propertyType})";
            }
        }

        private static object GetGenericPropertyValue(SerializedProperty prop)
        {
            if (prop.isArray)
            {
                var items = new List<object>();
                for (int i = 0; i < Mathf.Min(prop.arraySize, 20); i++)
                {
                    var element = prop.GetArrayElementAtIndex(i);
                    items.Add(GetSerializedPropertyValue(element));
                }
                return new { arraySize = prop.arraySize, items };
            }

            // For non-array generic types, read immediate children
            var children = new Dictionary<string, object>();
            var copy = prop.Copy();
            var end = copy.GetEndProperty();
            bool enterChildren = true;

            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;
                children[copy.name] = GetSerializedPropertyValue(copy);
            }

            return children.Count > 0 ? (object)children : $"({prop.propertyType})";
        }

        private static List<object> GetPublicProperties(Component comp)
        {
            var properties = new List<object>();
            Type type = comp.GetType();

            // Get public instance properties declared on the component type (not inherited from MonoBehaviour/Component)
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (PropertyInfo prop in props)
            {
                if (!prop.CanRead) continue;

                // Skip indexers
                if (prop.GetIndexParameters().Length > 0) continue;

                try
                {
                    object value = prop.GetValue(comp);
                    properties.Add(new
                    {
                        name = prop.Name,
                        type = prop.PropertyType.Name,
                        value = SafeSerializeValue(value),
                        canWrite = prop.CanWrite
                    });
                }
                catch (Exception)
                {
                    properties.Add(new
                    {
                        name = prop.Name,
                        type = prop.PropertyType.Name,
                        value = "(error reading value)",
                        canWrite = prop.CanWrite
                    });
                }
            }

            return properties;
        }

        private static object SafeSerializeValue(object value)
        {
            if (value == null) return null;

            Type type = value.GetType();

            // Primitives and strings
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value;

            // Enums
            if (type.IsEnum)
                return value.ToString();

            // Unity types
            if (value is Vector2 v2) return new { x = v2.x, y = v2.y };
            if (value is Vector3 v3) return new { x = v3.x, y = v3.y, z = v3.z };
            if (value is Vector4 v4) return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
            if (value is Color c) return new { r = c.r, g = c.g, b = c.b, a = c.a };
            if (value is Quaternion q) return new { x = q.x, y = q.y, z = q.z, w = q.w };
            if (value is UnityEngine.Object unityObj)
                return unityObj != null ? new { name = unityObj.name, type = unityObj.GetType().Name } : null;

            // Collections - just return count
            if (value is System.Collections.ICollection collection)
                return $"[Collection: {collection.Count} items]";

            // Fallback to ToString
            return value.ToString();
        }

        private static bool TrySetFieldOrProperty(Component comp, string name, JToken value)
        {
            Type type = comp.GetType();

            // Try SerializedProperty first (most reliable for Unity serialization)
            var serializedObj = new SerializedObject(comp);
            var prop = serializedObj.FindProperty(name);
            if (prop != null)
            {
                bool set = TrySetSerializedProperty(prop, value);
                if (set)
                {
                    serializedObj.ApplyModifiedProperties();
                    serializedObj.Dispose();
                    return true;
                }
                serializedObj.Dispose();
            }
            else
            {
                serializedObj.Dispose();
            }

            // Fallback to reflection
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    object converted = value.ToObject(field.FieldType);
                    field.SetValue(comp, converted);
                    return true;
                }
                catch { }
            }

            PropertyInfo propInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null && propInfo.CanWrite)
            {
                try
                {
                    object converted = value.ToObject(propInfo.PropertyType);
                    propInfo.SetValue(comp, converted);
                    return true;
                }
                catch { }
            }

            return false;
        }

        private static bool TrySetSerializedProperty(SerializedProperty prop, JToken value)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = value.ToObject<int>();
                        return true;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = value.ToObject<bool>();
                        return true;
                    case SerializedPropertyType.Float:
                        prop.floatValue = value.ToObject<float>();
                        return true;
                    case SerializedPropertyType.String:
                        prop.stringValue = value.ToObject<string>();
                        return true;
                    case SerializedPropertyType.Enum:
                        if (value.Type == JTokenType.String)
                        {
                            string enumStr = value.ToObject<string>();
                            for (int i = 0; i < prop.enumNames.Length; i++)
                            {
                                if (string.Equals(prop.enumNames[i], enumStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    prop.enumValueIndex = i;
                                    return true;
                                }
                            }
                        }
                        else if (value.Type == JTokenType.Integer)
                        {
                            prop.enumValueIndex = value.ToObject<int>();
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Color:
                        if (value.Type == JTokenType.Array)
                        {
                            var arr = value.ToObject<float[]>();
                            if (arr.Length >= 4)
                                prop.colorValue = new Color(arr[0], arr[1], arr[2], arr[3]);
                            else if (arr.Length >= 3)
                                prop.colorValue = new Color(arr[0], arr[1], arr[2]);
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector3:
                        if (value.Type == JTokenType.Array)
                        {
                            var arr = value.ToObject<float[]>();
                            if (arr.Length >= 3)
                                prop.vector3Value = new Vector3(arr[0], arr[1], arr[2]);
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector2:
                        if (value.Type == JTokenType.Array)
                        {
                            var arr = value.ToObject<float[]>();
                            if (arr.Length >= 2)
                                prop.vector2Value = new Vector2(arr[0], arr[1]);
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
