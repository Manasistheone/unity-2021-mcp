using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// TCP transport client that listens for connections from the Python MCP server.
    /// Requires a shared-secret handshake before accepting commands.
    /// </summary>
    public class TcpTransportClient : IMcpTransportClient
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cts;
        private Thread _listenThread;
        private readonly object _writeLock = new object();

        private int _port;
        private string _authToken;
        private TransportState _state;

        public string TransportName => "TCP";
        public TransportState State => _state;
        public int Port => _port;

        public event Action<string> OnCommandReceived;
        public event Action<TransportState> OnStateChanged;

        public TcpTransportClient(int port = 8765)
        {
            _port = port;
            _authToken = GenerateAuthToken();
            _state = new TransportState("TCP");
        }

        private static string GenerateAuthToken()
        {
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        public Task<bool> StartAsync()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();

                _state.IsConnected = false;
                _state.Error = null;

                WritePortLockFile();

                _listenThread = new Thread(ListenForConnections)
                {
                    IsBackground = true,
                    Name = "MCP-TCP-Listen"
                };
                _listenThread.Start();

                McpLog.Info($"TCP listener started on port {_port}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _state.Error = ex.Message;
                _state.IsConnected = false;
                OnStateChanged?.Invoke(_state);
                McpLog.Error($"Failed to start TCP listener: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public Task StopAsync()
        {
            _cts?.Cancel();

            try { _listener?.Stop(); } catch { }
            try { _client?.Close(); } catch { }

            _listener = null;
            _client = null;
            _stream = null;
            _reader = null;
            _writer = null;

            _state.IsConnected = false;
            _state.Error = null;
            OnStateChanged?.Invoke(_state);

            RemovePortLockFile();
            McpLog.Info("TCP listener stopped");
            return Task.CompletedTask;
        }

        public Task<bool> VerifyAsync()
        {
            bool connected = _client != null && _client.Connected;
            return Task.FromResult(connected);
        }

        /// <summary>
        /// Sends a response back to the Python MCP server. Thread-safe.
        /// </summary>
        public void SendResponse(string jsonResponse)
        {
            lock (_writeLock)
            {
                if (_writer == null)
                {
                    McpLog.Warning("Cannot send response: no active connection");
                    return;
                }

                try
                {
                    _writer.WriteLine(jsonResponse);
                    _writer.Flush();
                }
                catch (Exception ex)
                {
                    McpLog.Error($"Failed to send response: {ex.Message}");
                }
            }
        }

        private void ListenForConnections()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    McpLog.Info("Waiting for Python MCP server connection...");
                    TcpClient client = _listener.AcceptTcpClient();

                    // Close any previous connection
                    try { _client?.Close(); } catch { }

                    _client = client;
                    _stream = client.GetStream();
                    _reader = new StreamReader(_stream, Encoding.UTF8);

                    lock (_writeLock)
                    {
                        _writer = new StreamWriter(_stream, new UTF8Encoding(false))
                        {
                            AutoFlush = false,
                            NewLine = "\n"
                        };
                    }

                    _state.IsConnected = true;
                    _state.Error = null;
                    OnStateChanged?.Invoke(_state);
                    McpLog.Info("Python MCP server connected, awaiting authentication...");

                    // Authenticate the connection
                    if (!AuthenticateClient())
                    {
                        McpLog.Warning("Client failed authentication, disconnecting");
                        try { _client?.Close(); } catch { }
                        _state.IsConnected = false;
                        OnStateChanged?.Invoke(_state);
                        continue;
                    }

                    McpLog.Info("Python MCP server authenticated");

                    // Read commands on this thread (blocking)
                    ReadCommands();

                    // Connection lost
                    _state.IsConnected = false;
                    OnStateChanged?.Invoke(_state);
                    McpLog.Info("Python MCP server disconnected");
                }
            }
            catch (SocketException) when (_cts.Token.IsCancellationRequested)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    McpLog.Error($"Listen thread error: {ex.Message}");
                    _state.Error = ex.Message;
                    OnStateChanged?.Invoke(_state);
                }
            }
        }

        private void ReadCommands()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // ReadLine blocks until a full line arrives (no lock needed - only this thread reads)
                    string line = _reader.ReadLine();

                    if (line == null)
                    {
                        // Connection closed by remote
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    McpLog.Info($"Received command ({line.Length} chars)");
                    OnCommandReceived?.Invoke(line);
                }
            }
            catch (IOException ex)
            {
                McpLog.Info($"Read ended: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                // Stream closed
            }
        }

        private bool AuthenticateClient()
        {
            try
            {
                // Client must send: {"auth": "<token>"} as the first line
                string line = _reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                    return false;

                var authMsg = JObject.Parse(line);
                string token = authMsg["auth"]?.ToString();
                return string.Equals(token, _authToken, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                McpLog.Warning($"Authentication error: {ex.Message}");
                return false;
            }
        }

        private void WritePortLockFile()
        {
            try
            {
                string lockDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".unity-mcp");
                Directory.CreateDirectory(lockDir);

                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                string lockFile = Path.Combine(lockDir, $"unity-mcp-port-{pid}.json");

                var lockData = new JObject
                {
                    ["port"] = _port,
                    ["pid"] = pid,
                    ["startTime"] = DateTime.UtcNow.ToString("o"),
                    ["unityVersion"] = Application.unityVersion,
                    ["authToken"] = _authToken
                };

                File.WriteAllText(lockFile, lockData.ToString());
                McpLog.Info($"Port lock file written for PID {pid}");
            }
            catch (Exception ex)
            {
                McpLog.Warning($"Failed to write port lock file: {ex.Message}");
            }
        }

        private void RemovePortLockFile()
        {
            try
            {
                string lockDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".unity-mcp");
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                string lockFile = Path.Combine(lockDir, $"unity-mcp-port-{pid}.json");
                if (File.Exists(lockFile)) File.Delete(lockFile);
            }
            catch { }
        }
    }
}
