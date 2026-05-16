using System.Threading.Tasks;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Interface for MCP transport clients that handle communication
    /// between the Unity plugin and the Python MCP server.
    /// Supports both TCP (stdio mode) and WebSocket (HTTP mode) transports.
    /// </summary>
    public interface IMcpTransportClient
    {
        /// <summary>
        /// Gets the human-readable name of this transport (e.g., "TCP", "WebSocket").
        /// </summary>
        string TransportName { get; }

        /// <summary>
        /// Gets the current state of the transport connection.
        /// </summary>
        TransportState State { get; }

        /// <summary>
        /// Starts the transport client, initiating a connection to the MCP server.
        /// Uses async/await patterns compatible with Unity 2021.3's Task-based async support.
        /// </summary>
        /// <returns>True if the transport started successfully; false otherwise.</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Stops the transport client, closing the connection and cleaning up resources.
        /// Any pending commands should be failed with error responses rather than silently discarded.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Verifies that the transport connection is alive and functional.
        /// Can be used for health checks and connection validation.
        /// </summary>
        /// <returns>True if the connection is verified and healthy; false otherwise.</returns>
        Task<bool> VerifyAsync();
    }
}
