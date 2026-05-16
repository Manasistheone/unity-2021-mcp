using System.Text;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Utility class for converting between naming conventions.
    /// Used by CommandRegistry to derive command names from class names.
    /// </summary>
    public static class NamingUtility
    {
        /// <summary>
        /// Converts a PascalCase class name to a snake_case command name.
        /// 
        /// Rules:
        /// - Inserts underscore before a capital letter that follows a lowercase letter or digit.
        /// - Inserts underscore between a run of consecutive capitals and the next lowercase letter,
        ///   keeping the last capital with the following lowercase (e.g., HTTPClient → http_client).
        /// - Inserts underscore between a letter and a digit boundary (e.g., Unity2021 → unity_2021).
        /// - Inserts underscore between a digit and a letter boundary (e.g., Vector3D → vector_3d).
        /// - Output is always lowercase with no leading or trailing underscores.
        /// 
        /// Examples:
        ///   ManageGameobject → manage_gameobject
        ///   ManageScene → manage_scene
        ///   HTTPClient → http_client
        ///   URPShader → urp_shader
        ///   Unity2021 → unity_2021
        ///   Vector3D → vector_3d
        /// </summary>
        /// <param name="name">The PascalCase class name to convert.</param>
        /// <returns>The snake_case command name, or the original string if null or empty.</returns>
        public static string PascalToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var sb = new StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];

                if (i == 0)
                {
                    sb.Append(char.ToLowerInvariant(current));
                    continue;
                }

                char prev = name[i - 1];
                bool currentIsUpper = char.IsUpper(current);
                bool currentIsDigit = char.IsDigit(current);
                bool prevIsUpper = char.IsUpper(prev);
                bool prevIsDigit = char.IsDigit(prev);
                bool prevIsLower = char.IsLower(prev);

                if (currentIsUpper)
                {
                    // Insert underscore if previous is lowercase or digit
                    // e.g., manage[G]ameobject, vector3[D]
                    if (prevIsLower || prevIsDigit)
                    {
                        sb.Append('_');
                    }
                    // Insert underscore if previous is uppercase and next is lowercase
                    // e.g., HTT[P]Client → http_client (underscore before P because next is lowercase 'l'... 
                    // actually: H-T-T-P-C-l-i-e-n-t → we want http_client
                    // At 'C' (index 4): prev='P' is upper, next='l' is lower → insert underscore before 'C'
                    else if (prevIsUpper && i + 1 < name.Length && char.IsLower(name[i + 1]))
                    {
                        sb.Append('_');
                    }

                    sb.Append(char.ToLowerInvariant(current));
                }
                else if (currentIsDigit)
                {
                    // Insert underscore if previous is a letter (upper or lower)
                    // e.g., Unity[2]021, Vector[3]D
                    if (prevIsUpper || prevIsLower)
                    {
                        sb.Append('_');
                    }

                    sb.Append(current);
                }
                else
                {
                    // current is lowercase
                    // Insert underscore if previous is a digit
                    // e.g., 2021[x] (digit to lowercase boundary) - though uncommon in PascalCase
                    if (prevIsDigit)
                    {
                        sb.Append('_');
                    }

                    sb.Append(current);
                }
            }

            // Trim any leading/trailing underscores (safety net)
            string result = sb.ToString().Trim('_');

            return result;
        }
    }
}
