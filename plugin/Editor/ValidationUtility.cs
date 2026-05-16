using System;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Provides validation functions for user-configurable settings
    /// such as port numbers and URLs.
    /// </summary>
    public static class ValidationUtility
    {
        public const int MinPort = 1024;
        public const int MaxPort = 65535;

        /// <summary>
        /// Validates that a port number is within the acceptable range [1024, 65535].
        /// </summary>
        /// <param name="port">The port number to validate.</param>
        /// <returns>
        /// A tuple where isValid is true if the port is in range, and errorMessage
        /// contains a description of the validation failure when isValid is false.
        /// </returns>
        public static (bool isValid, string errorMessage) ValidatePort(int port)
        {
            if (port < MinPort || port > MaxPort)
            {
                return (false, $"Port must be between {MinPort} and {MaxPort}");
            }

            return (true, null);
        }

        /// <summary>
        /// Validates that a URL starts with "http://" or "https://" and has a non-empty host segment.
        /// </summary>
        /// <param name="url">The URL string to validate.</param>
        /// <returns>
        /// A tuple where isValid is true if the URL is valid, and errorMessage
        /// contains a description of the validation failure when isValid is false.
        /// </returns>
        public static (bool isValid, string errorMessage) ValidateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return (false, "URL cannot be empty");
            }

            string host;

            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                host = url.Substring("https://".Length);
            }
            else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                host = url.Substring("http://".Length);
            }
            else
            {
                return (false, "URL must start with \"http://\" or \"https://\"");
            }

            // Extract host segment (everything before the first '/', '?', '#', or ':' after scheme)
            int hostEnd = host.IndexOfAny(new[] { '/', '?', '#', ':' });
            string hostSegment = hostEnd >= 0 ? host.Substring(0, hostEnd) : host;

            if (string.IsNullOrWhiteSpace(hostSegment))
            {
                return (false, "URL must contain a non-empty host");
            }

            return (true, null);
        }
    }
}
