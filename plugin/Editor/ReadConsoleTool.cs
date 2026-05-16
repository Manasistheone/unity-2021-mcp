using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    [McpForUnityTool(Name = "read_console", AutoRegister = true)]
    [ToolParameter("count", "Maximum entries to return")]
    [ToolParameter("log_type", "Filter: all, log, warning, error")]
    [ToolParameter("clear", "Clear console after reading")]
    public static class ReadConsoleTool
    {
        private static readonly List<LogEntry> _logBuffer = new List<LogEntry>();
        private static bool _listening;
        private const int MaxBufferSize = 500;

        private class LogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
            public DateTime Timestamp;
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (!_listening)
            {
                Application.logMessageReceived += OnLogMessage;
                _listening = true;
            }
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            lock (_logBuffer)
            {
                _logBuffer.Add(new LogEntry
                {
                    Message = message,
                    StackTrace = stackTrace,
                    Type = type,
                    Timestamp = DateTime.Now
                });
                while (_logBuffer.Count > MaxBufferSize)
                    _logBuffer.RemoveAt(0);
            }
        }

        public static object HandleCommand(JObject @params)
        {
            var toolParams = new ToolParams(@params);
            int count = toolParams.Get<int>("count", 50);
            string logType = toolParams.Get<string>("log_type", "all");
            bool clear = toolParams.Get<bool>("clear", false);

            var entries = new List<object>();
            lock (_logBuffer)
            {
                int startIndex = Math.Max(0, _logBuffer.Count - count);
                for (int i = startIndex; i < _logBuffer.Count; i++)
                {
                    var entry = _logBuffer[i];
                    if (MatchesFilter(entry.Type, logType))
                    {
                        entries.Add(new
                        {
                            message = entry.Message,
                            type = entry.Type.ToString(),
                            timestamp = entry.Timestamp.ToString("HH:mm:ss.fff")
                        });
                    }
                }
                if (clear) _logBuffer.Clear();
            }
            return new { success = true, result = new { entries, totalBuffered = _logBuffer.Count } };
        }

        private static bool MatchesFilter(LogType type, string filter)
        {
            switch (filter.ToLowerInvariant())
            {
                case "all": return true;
                case "log": return type == LogType.Log;
                case "warning": return type == LogType.Warning;
                case "error": return type == LogType.Error || type == LogType.Exception;
                default: return true;
            }
        }
    }
}
