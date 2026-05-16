using System.Threading.Tasks;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Manages the lifecycle of the MCP TCP transport and command dispatcher.
    /// Shared between McpEditorWindow (manual start/stop) and McpAutoStart (domain reload recovery).
    /// </summary>
    public static class McpServerManager
    {
        private static TcpTransportClient _transport;
        private static CommandDispatcher _dispatcher;

        public static TcpTransportClient Transport => _transport;
        public static bool IsRunning => _transport != null;

        public static async Task<bool> StartAsync(int port)
        {
            if (IsRunning) return true;

            _transport = new TcpTransportClient(port);
            _dispatcher = new CommandDispatcher(_transport);

            bool started = await _transport.StartAsync();
            if (started)
            {
                McpAutoStart.MarkServerRunning();
                return true;
            }
            else
            {
                _dispatcher.Dispose();
                _dispatcher = null;
                _transport = null;
                return false;
            }
        }

        public static async Task StopAsync()
        {
            if (!IsRunning) return;

            await _transport.StopAsync();
            _dispatcher.Dispose();

            _transport = null;
            _dispatcher = null;
            McpAutoStart.MarkServerStopped();
        }
    }
}
