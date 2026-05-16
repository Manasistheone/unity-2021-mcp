using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Reflection-based command registry that auto-discovers tool and resource handlers
    /// marked with McpForUnityTool or McpForUnityResource attributes.
    /// Initializes lazily on first use by scanning all loaded assemblies.
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, CommandHandler> _handlers =
            new Dictionary<string, CommandHandler>(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static readonly object _lock = new object();

        /// <summary>
        /// Represents a registered command handler with its invoke delegate and metadata.
        /// </summary>
        private class CommandHandler
        {
            public string CommandName { get; set; }
            public Type DeclaringType { get; set; }
            public MethodInfo Method { get; set; }
            public bool IsAsync { get; set; }
        }

        /// <summary>
        /// Gets the number of registered command handlers.
        /// Triggers initialization if not already done.
        /// </summary>
        public static int RegisteredCommandCount
        {
            get
            {
                EnsureInitialized();
                return _handlers.Count;
            }
        }

        /// <summary>
        /// Ensures the registry is initialized. Thread-safe, idempotent.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                DiscoverAndRegister();
                _initialized = true;
            }
        }

        /// <summary>
        /// Executes a command by name, dispatching to the registered handler.
        /// </summary>
        /// <param name="commandName">The command name (snake_case).</param>
        /// <param name="parameters">The JObject containing command parameters.</param>
        /// <returns>The result from the handler, either directly or awaited if async.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the command name is not registered in the registry.
        /// </exception>
        public static async Task<object> ExecuteCommand(string commandName, JObject parameters)
        {
            EnsureInitialized();

            if (!_handlers.TryGetValue(commandName, out CommandHandler handler))
            {
                throw new InvalidOperationException(
                    string.Format("Unknown command: '{0}' is not registered in the CommandRegistry.", commandName));
            }

            object result = handler.Method.Invoke(null, new object[] { parameters });

            if (handler.IsAsync)
            {
                Task<object> task = (Task<object>)result;
                return await task;
            }

            return result;
        }

        /// <summary>
        /// Checks whether a command name is registered.
        /// </summary>
        /// <param name="commandName">The command name to check.</param>
        /// <returns>True if the command is registered, false otherwise.</returns>
        public static bool IsCommandRegistered(string commandName)
        {
            EnsureInitialized();
            return _handlers.ContainsKey(commandName);
        }

        /// <summary>
        /// Gets all registered command names.
        /// </summary>
        /// <returns>A collection of registered command names.</returns>
        public static IEnumerable<string> GetRegisteredCommands()
        {
            EnsureInitialized();
            return _handlers.Keys;
        }

        /// <summary>
        /// Resets the registry, clearing all handlers and allowing re-initialization.
        /// Primarily used for testing purposes.
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Manually registers a handler for a given command name.
        /// Used for testing or programmatic registration.
        /// </summary>
        /// <param name="commandName">The command name to register.</param>
        /// <param name="type">The type containing the HandleCommand method.</param>
        internal static void RegisterHandler(string commandName, Type type)
        {
            MethodInfo method = FindHandleCommandMethod(type);
            if (method == null)
            {
                McpLog.Warning(string.Format(
                    "Cannot register '{0}': type '{1}' does not have a valid public static HandleCommand(JObject) method.",
                    commandName, type.FullName));
                return;
            }

            bool isAsync = IsAsyncHandler(method);

            _handlers[commandName] = new CommandHandler
            {
                CommandName = commandName,
                DeclaringType = type,
                Method = method,
                IsAsync = isAsync
            };
        }

        /// <summary>
        /// Scans all loaded assemblies for types with McpForUnityTool or McpForUnityResource
        /// attributes and registers those with valid HandleCommand methods.
        /// </summary>
        private static void DiscoverAndRegister()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                // Skip dynamic assemblies and known framework assemblies
                if (assembly.IsDynamic)
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Some assemblies may fail to load types; use what we can
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    TryRegisterType(type);
                }
            }
        }

        /// <summary>
        /// Attempts to register a single type if it has the appropriate attribute.
        /// </summary>
        private static void TryRegisterType(Type type)
        {
            string commandName = null;

            // Check for McpForUnityTool attribute
            McpForUnityToolAttribute toolAttr = GetAttribute<McpForUnityToolAttribute>(type);
            if (toolAttr != null)
            {
                commandName = DeriveCommandName(type, toolAttr.Name);
            }

            // Check for McpForUnityResource attribute (only if no tool attribute found)
            if (commandName == null)
            {
                McpForUnityResourceAttribute resourceAttr = GetAttribute<McpForUnityResourceAttribute>(type);
                if (resourceAttr != null)
                {
                    commandName = DeriveCommandName(type, resourceAttr.Name);
                }
            }

            if (commandName == null)
            {
                return;
            }

            // Validate HandleCommand method exists
            MethodInfo method = FindHandleCommandMethod(type);
            if (method == null)
            {
                McpLog.Warning(string.Format(
                    "Type '{0}' is marked with MCP attribute but does not have a valid " +
                    "public static HandleCommand(JObject) method. Skipping registration.",
                    type.FullName));
                return;
            }

            bool isAsync = IsAsyncHandler(method);

            _handlers[commandName] = new CommandHandler
            {
                CommandName = commandName,
                DeclaringType = type,
                Method = method,
                IsAsync = isAsync
            };

            McpLog.Info(string.Format(
                "Registered command '{0}' → {1}.HandleCommand ({2})",
                commandName, type.FullName, isAsync ? "async" : "sync"));
        }

        /// <summary>
        /// Derives the command name from the attribute's Name property or the class name.
        /// </summary>
        /// <param name="type">The type being registered.</param>
        /// <param name="attributeName">The Name property from the attribute, or null.</param>
        /// <returns>The derived command name in snake_case.</returns>
        private static string DeriveCommandName(Type type, string attributeName)
        {
            if (!string.IsNullOrEmpty(attributeName))
            {
                return attributeName;
            }

            return NamingUtility.PascalToSnakeCase(type.Name);
        }

        /// <summary>
        /// Finds the public static HandleCommand(JObject) method on a type.
        /// Supports both sync (returns object) and async (returns Task&lt;object&gt;) signatures.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The MethodInfo if found, null otherwise.</returns>
        private static MethodInfo FindHandleCommandMethod(Type type)
        {
            MethodInfo method = type.GetMethod(
                "HandleCommand",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(JObject) },
                null);

            if (method == null)
            {
                return null;
            }

            // Validate return type: must be object or Task<object>
            Type returnType = method.ReturnType;
            if (returnType == typeof(object))
            {
                return method;
            }

            if (returnType == typeof(Task<object>))
            {
                return method;
            }

            // Also accept Task<T> where T is assignable to object (covers most cases)
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return method;
            }

            return null;
        }

        /// <summary>
        /// Determines whether a HandleCommand method is async (returns Task-based type).
        /// </summary>
        private static bool IsAsyncHandler(MethodInfo method)
        {
            Type returnType = method.ReturnType;
            if (returnType == typeof(Task<object>))
            {
                return true;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Safely gets a custom attribute from a type, returning null if not found.
        /// </summary>
        private static T GetAttribute<T>(Type type) where T : Attribute
        {
            try
            {
                object[] attrs = type.GetCustomAttributes(typeof(T), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return (T)attrs[0];
                }
            }
            catch (Exception)
            {
                // Silently skip types that fail attribute inspection
            }

            return null;
        }
    }
}
