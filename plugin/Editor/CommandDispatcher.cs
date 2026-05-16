using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Dispatches incoming commands from the Python MCP server to the CommandRegistry
    /// and sends responses back via the transport.
    /// </summary>
    public class CommandDispatcher
    {
        private readonly TcpTransportClient _transport;
        private static CommandDispatcher _instance;

        public static CommandDispatcher Instance => _instance;

        public CommandDispatcher(TcpTransportClient transport)
        {
            _transport = transport;
            _transport.OnCommandReceived += HandleCommand;
            _instance = this;
        }

        private void HandleCommand(string commandJson)
        {
            // Queue execution on the main thread since Unity API is not thread-safe
            UnityMainThreadDispatcher.Enqueue(() => ExecuteOnMainThread(commandJson));
        }

        private void ExecuteOnMainThread(string commandJson)
        {
            string requestId = null;
            try
            {
                JObject request = JObject.Parse(commandJson);
                requestId = request["id"]?.ToString();
                string toolName = request["tool"]?.ToString();
                JObject parameters = request["params"] as JObject ?? new JObject();

                if (string.IsNullOrEmpty(toolName))
                {
                    SendError(requestId, "Missing 'tool' field in command");
                    return;
                }

                McpLog.Info($"Executing command: {toolName}");

                if (!CommandRegistry.IsCommandRegistered(toolName))
                {
                    SendError(requestId, $"Unknown tool: '{toolName}'. Available: {string.Join(", ", CommandRegistry.GetRegisteredCommands())}");
                    return;
                }

                // Execute synchronously - CommandRegistry.ExecuteCommand returns Task<object>
                // but our handlers are sync, so we can get the result directly
                var task = CommandRegistry.ExecuteCommand(toolName, parameters);
                
                // For sync handlers, the task completes immediately
                if (task.IsCompleted)
                {
                    object result = task.Result;
                    SendSuccess(requestId, result);
                }
                else
                {
                    // For async handlers, we need to poll
                    // Use a coroutine-like approach via update polling
                    PollAsyncResult(requestId, task);
                }
            }
            catch (Exception ex)
            {
                McpLog.Error($"Command execution failed: {ex.Message}\n{ex.StackTrace}");
                SendError(requestId, ex.Message);
            }
        }

        private void PollAsyncResult(string requestId, System.Threading.Tasks.Task<object> task)
        {
            // Poll on next update frames
            void CheckCompletion()
            {
                if (task.IsCompleted)
                {
                    if (task.IsFaulted)
                    {
                        SendError(requestId, task.Exception?.InnerException?.Message ?? "Unknown async error");
                    }
                    else
                    {
                        SendSuccess(requestId, task.Result);
                    }
                }
                else
                {
                    // Re-queue to check again next frame
                    UnityMainThreadDispatcher.Enqueue(CheckCompletion);
                }
            }
            UnityMainThreadDispatcher.Enqueue(CheckCompletion);
        }

        private void SendSuccess(string requestId, object result)
        {
            var response = new JObject
            {
                ["success"] = true,
                ["result"] = result != null ? JToken.FromObject(result) : JValue.CreateNull()
            };
            if (requestId != null)
                response["id"] = requestId;

            string json = response.ToString(Newtonsoft.Json.Formatting.None);
            McpLog.Info($"Sending response ({json.Length} chars)");
            _transport.SendResponse(json);
        }

        private void SendError(string requestId, string errorMessage)
        {
            var response = new JObject
            {
                ["success"] = false,
                ["error"] = errorMessage
            };
            if (requestId != null)
                response["id"] = requestId;

            _transport.SendResponse(response.ToString(Newtonsoft.Json.Formatting.None));
        }

        public void Dispose()
        {
            if (_transport != null)
                _transport.OnCommandReceived -= HandleCommand;
            _instance = null;
        }
    }
}
