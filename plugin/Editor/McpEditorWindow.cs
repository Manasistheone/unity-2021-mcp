using System;
using UnityEditor;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Editor window for MCP for Unity 2021.
    /// Provides UI to start/stop the TCP listener, view connection status,
    /// and configure settings.
    /// </summary>
    public class McpEditorWindow : EditorWindow
    {
        // Use the shared McpServerManager for transport/dispatcher lifecycle
        private static TcpTransportClient _transport => McpServerManager.Transport;
        private static bool IsRunning => McpServerManager.IsRunning;

        private int _port = 8765;
        private bool _debugLogging;
        private Vector2 _scrollPos;
        private string _statusMessage = "Stopped";
        private Color _statusColor = Color.gray;

        [MenuItem("Window/MCP for Unity 2021")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpEditorWindow>("MCP for Unity 2021");
            window.minSize = new Vector2(350, 250);
        }

        private void OnEnable()
        {
            _port = EditorPrefs.GetInt("UnityMcp2021_Port", 8765);
            _debugLogging = EditorPrefs.GetBool("UnityMcp2021_Debug", false);
            McpLog.DebugEnabled = _debugLogging;

            if (_transport != null)
            {
                _transport.OnStateChanged += OnTransportStateChanged;
                UpdateStatusFromTransport();
            }
        }

        private void OnDisable()
        {
            if (_transport != null)
            {
                _transport.OnStateChanged -= OnTransportStateChanged;
            }
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawStatus();
            EditorGUILayout.Space(10);
            DrawControls();
            EditorGUILayout.Space(10);
            DrawSettings();
            EditorGUILayout.Space(10);
            DrawInfo();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MCP for Unity 2021", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Version {UnityMcp2021Info.Version}", EditorStyles.miniLabel);
        }

        private void DrawStatus()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(60));

            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = _statusColor;
            style.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(_statusMessage, style);
            EditorGUILayout.EndHorizontal();

            if (_transport != null && _transport.State.IsConnected)
            {
                EditorGUILayout.LabelField($"  Port: {_port}");
                EditorGUILayout.LabelField($"  Registered Commands: {CommandRegistry.RegisteredCommandCount}");
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !IsRunning;
            if (GUILayout.Button("Start", GUILayout.Height(30)))
            {
                StartServer();
            }

            GUI.enabled = IsRunning;
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                StopServer();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            GUI.enabled = !IsRunning;
            int newPort = EditorGUILayout.IntField("TCP Port", _port);
            if (newPort != _port && newPort >= 1024 && newPort <= 65535)
            {
                _port = newPort;
                EditorPrefs.SetInt("UnityMcp2021_Port", _port);
            }
            GUI.enabled = true;

            bool newDebug = EditorGUILayout.Toggle("Debug Logging", _debugLogging);
            if (newDebug != _debugLogging)
            {
                _debugLogging = newDebug;
                EditorPrefs.SetBool("UnityMcp2021_Debug", _debugLogging);
                McpLog.DebugEnabled = _debugLogging;
            }
        }

        private void DrawInfo()
        {
            EditorGUILayout.LabelField("Connection Info", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The Python MCP server connects to this TCP listener.\n" +
                "Configure your MCP client (Kiro, Claude, Cursor) to use:\n" +
                "  command: uv run --project <server-path> mcp-for-unity-2021 --transport stdio\n\n" +
                "The server will auto-discover this port via ~/.unity-mcp/ lock files.",
                MessageType.Info);
        }

        private async void StartServer()
        {
            if (IsRunning) return;

            bool started = await McpServerManager.StartAsync(_port);
            if (started)
            {
                _transport.OnStateChanged += OnTransportStateChanged;
                _statusMessage = "Listening (waiting for connection)";
                _statusColor = new Color(1f, 0.7f, 0f); // Orange
            }
            else
            {
                _statusMessage = $"Failed: {_transport?.State.Error ?? "Unknown error"}";
                _statusColor = Color.red;
            }

            Repaint();
        }

        private async void StopServer()
        {
            if (!IsRunning) return;

            _transport.OnStateChanged -= OnTransportStateChanged;
            await McpServerManager.StopAsync();

            _statusMessage = "Stopped";
            _statusColor = Color.gray;

            Repaint();
        }

        private void OnTransportStateChanged(TransportState state)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (state.IsConnected)
                {
                    _statusMessage = "Connected";
                    _statusColor = Color.green;
                }
                else if (!string.IsNullOrEmpty(state.Error))
                {
                    _statusMessage = $"Error: {state.Error}";
                    _statusColor = Color.red;
                }
                else if (IsRunning)
                {
                    _statusMessage = "Listening (waiting for connection)";
                    _statusColor = new Color(1f, 0.7f, 0f);
                }
                else
                {
                    _statusMessage = "Stopped";
                    _statusColor = Color.gray;
                }

                Repaint();
            });
        }

        private void UpdateStatusFromTransport()
        {
            if (_transport == null) return;

            if (_transport.State.IsConnected)
            {
                _statusMessage = "Connected";
                _statusColor = Color.green;
            }
            else if (IsRunning)
            {
                _statusMessage = "Listening (waiting for connection)";
                _statusColor = new Color(1f, 0.7f, 0f);
            }
        }

        private void OnDestroy()
        {
            // Don't stop the server when the window is closed - keep it running
        }
    }
}
