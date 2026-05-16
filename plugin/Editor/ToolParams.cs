using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Wraps a JObject containing command parameters and provides typed accessors
    /// with automatic snake_case/camelCase bidirectional key normalization.
    /// For example, querying "pageSize" will also check "page_size" and vice versa.
    /// </summary>
    public class ToolParams
    {
        private readonly JObject _params;

        /// <summary>
        /// Creates a new ToolParams instance wrapping the given JObject.
        /// </summary>
        /// <param name="parameters">The JObject containing command parameters. If null, an empty JObject is used.</param>
        public ToolParams(JObject parameters)
        {
            _params = parameters ?? new JObject();
        }

        /// <summary>
        /// Gets a required parameter value. Throws if the key is not found after normalization.
        /// </summary>
        /// <typeparam name="T">The expected type of the parameter value.</typeparam>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <returns>The parameter value converted to type T.</returns>
        /// <exception cref="ArgumentException">Thrown when the key is not found in the parameters.</exception>
        public T GetRequired<T>(string key)
        {
            JToken token = FindToken(key);
            if (token == null)
            {
                throw new ArgumentException(
                    string.Format("Required parameter '{0}' not found.", key));
            }
            return token.ToObject<T>();
        }

        /// <summary>
        /// Gets a parameter value with a default fallback if the key is not found.
        /// </summary>
        /// <typeparam name="T">The expected type of the parameter value.</typeparam>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <param name="defaultValue">The value to return if the key is not found.</param>
        /// <returns>The parameter value converted to type T, or the default value.</returns>
        public T Get<T>(string key, T defaultValue)
        {
            JToken token = FindToken(key);
            if (token == null || token.Type == JTokenType.Null)
            {
                return defaultValue;
            }
            return token.ToObject<T>();
        }

        /// <summary>
        /// Gets an integer parameter value with a default fallback.
        /// </summary>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <param name="defaultValue">The value to return if the key is not found. Defaults to 0.</param>
        /// <returns>The integer value, or the default.</returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            return Get<int>(key, defaultValue);
        }

        /// <summary>
        /// Gets a float parameter value with a default fallback.
        /// </summary>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <param name="defaultValue">The value to return if the key is not found. Defaults to 0f.</param>
        /// <returns>The float value, or the default.</returns>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return Get<float>(key, defaultValue);
        }

        /// <summary>
        /// Gets a boolean parameter value with a default fallback.
        /// </summary>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <param name="defaultValue">The value to return if the key is not found. Defaults to false.</param>
        /// <returns>The boolean value, or the default.</returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return Get<bool>(key, defaultValue);
        }

        /// <summary>
        /// Gets the raw JToken for a parameter key, or null if not found.
        /// </summary>
        /// <param name="key">The parameter key (snake_case or camelCase).</param>
        /// <returns>The raw JToken, or null if the key is not found.</returns>
        public JToken GetRaw(string key)
        {
            return FindToken(key);
        }

        /// <summary>
        /// Finds a token by key, trying the original key first, then the alternate
        /// case format (snake_case ↔ camelCase).
        /// </summary>
        private JToken FindToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            // Try the original key first
            JToken token = _params[key];
            if (token != null)
            {
                return token;
            }

            // Try the alternate case format
            string alternateKey = GetAlternateKey(key);
            if (!string.IsNullOrEmpty(alternateKey) && alternateKey != key)
            {
                token = _params[alternateKey];
                if (token != null)
                {
                    return token;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the alternate case format for a key.
        /// If the key is snake_case (contains underscores), returns camelCase.
        /// If the key is camelCase (no underscores, has internal uppercase), returns snake_case.
        /// </summary>
        private static string GetAlternateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            if (key.Contains("_"))
            {
                // snake_case → camelCase
                return SnakeCaseToCamelCase(key);
            }
            else
            {
                // camelCase → snake_case
                return CamelCaseToSnakeCase(key);
            }
        }

        /// <summary>
        /// Converts a snake_case string to camelCase.
        /// Example: "page_size" → "pageSize"
        /// </summary>
        internal static string SnakeCaseToCamelCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
            {
                return snakeCase;
            }

            string[] parts = snakeCase.Split('_');
            if (parts.Length <= 1)
            {
                return snakeCase;
            }

            StringBuilder sb = new StringBuilder();
            // First part stays lowercase
            sb.Append(parts[0].ToLowerInvariant());

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(parts[i][0]));
                    if (parts[i].Length > 1)
                    {
                        sb.Append(parts[i].Substring(1).ToLowerInvariant());
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a camelCase string to snake_case.
        /// Example: "pageSize" → "page_size"
        /// </summary>
        internal static string CamelCaseToSnakeCase(string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
            {
                return camelCase;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < camelCase.Length; i++)
            {
                char c = camelCase[i];
                if (char.IsUpper(c))
                {
                    // Insert underscore before uppercase letter (unless it's the first char)
                    if (i > 0)
                    {
                        // Handle consecutive uppercase (e.g., "getURL" → "get_url")
                        bool prevIsUpper = char.IsUpper(camelCase[i - 1]);
                        bool nextIsLower = (i + 1 < camelCase.Length) && char.IsLower(camelCase[i + 1]);

                        if (!prevIsUpper || nextIsLower)
                        {
                            sb.Append('_');
                        }
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
