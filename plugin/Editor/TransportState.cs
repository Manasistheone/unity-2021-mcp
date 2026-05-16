namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Represents the current state of an MCP transport connection.
    /// Used by IMcpTransportClient implementations to report connection status.
    /// </summary>
    public class TransportState
    {
        /// <summary>
        /// Gets or sets whether the transport is currently connected to the MCP server.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the name of the transport (e.g., "TCP", "WebSocket").
        /// </summary>
        public string TransportName { get; set; }

        /// <summary>
        /// Gets or sets the error message if the transport is in an error state.
        /// Null or empty when no error is present.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the connected MCP server instance.
        /// Null when not connected.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Creates a new TransportState with default disconnected values.
        /// </summary>
        public TransportState()
        {
            IsConnected = false;
            TransportName = string.Empty;
            Error = null;
            ServerId = null;
        }

        /// <summary>
        /// Creates a new TransportState with the specified transport name.
        /// </summary>
        /// <param name="transportName">The name of the transport.</param>
        public TransportState(string transportName)
        {
            IsConnected = false;
            TransportName = transportName;
            Error = null;
            ServerId = null;
        }
    }
}
