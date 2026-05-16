# Troubleshooting

## Common Issues

### Plugin won't compile in Unity

**Symptoms:** Errors in the Unity Console after importing the plugin.

**Solutions:**
- Verify you're using Unity **2021.3.x** (not 2022 or newer).
- Ensure `com.unity.nuget.newtonsoft-json` is installed via Package Manager.
- Check that the `.asmdef` file (`UnityMcp2021.Editor.asmdef`) is present in the `Editor/` folder.

---

### Server can't find Unity

**Symptoms:** Python server logs "Unity connection failed" or times out.

**Solutions:**
- Ensure the Unity Editor is open with the plugin active.
- Check that `~/.unity-mcp/` contains a `unity-mcp-port-*.json` lock file.
- Verify the port in the lock file matches what the plugin is listening on.
- Delete stale lock files from old Unity sessions that didn't shut down cleanly.

---

### Connection rejected / Auth failure

**Symptoms:** "Client failed authentication, disconnecting" in Unity Console.

**Solutions:**
- The auth token is regenerated every time the plugin starts. If the Python server has a cached old token, it will be rejected.
- Delete stale lock files in `~/.unity-mcp/` and restart both the plugin and server.
- Ensure only one Unity instance is writing lock files (or that the server picks the correct one).

---

### Connection drops after script compilation

**Symptoms:** Connection lost after editing and saving a C# script in Unity.

**This is handled automatically.** The `McpAutoStart` component restarts the listener after domain reload. The Python server will reconnect on the next command.

If auto-reconnect fails:
- Check the Unity Console for `[AutoStart]` log messages.
- Manually click **Stop** then **Start** in the MCP Editor window.
- Enable **Debug Logging** for verbose output.

---

### Commands timing out

**Symptoms:** "Command timed out after 30s" error.

**Solutions:**
- Commands execute on Unity's main thread. If the editor is busy (asset import, long compilation), commands queue.
- Check Unity Console for errors during command execution.
- Avoid sending commands during large asset imports or Play Mode transitions.

---

### "Not connected to Unity" error

**Symptoms:** Python server raises `ConnectionError` when trying to send a command.

**Solutions:**
- Ensure the Unity plugin is started (green or orange status in the Editor window).
- The Python server auto-reconnects, but the first command after a disconnect may fail.
- Try the command again — the server will reconnect automatically.

---

### HTTP mode not accessible from network

**Symptoms:** Can't reach the server from another machine when using `--transport http`.

**Solutions:**
- By default, the server binds to `127.0.0.1` (localhost only).
- To expose to the network: `mcp-for-unity-2021 --transport http --host 0.0.0.0`
- **Warning:** This has no authentication on the HTTP layer. Only use on trusted networks.

---

### Unity Console flooded with logs

**Symptoms:** Too many `[MCP 2021]` messages in the Console.

**Solutions:**
- Disable **Debug Logging** in the MCP Editor window (Window → MCP for Unity 2021).
- Only errors and warnings are logged when debug is off.

---

## Diagnostic Steps

1. **Check Unity Console** — Look for `[MCP 2021]` prefixed messages.
2. **Check lock files** — `ls ~/.unity-mcp/` should show `unity-mcp-port-*.json`.
3. **Check Python logs** — `cat ~/.unity-mcp/logs/unity-2021-mcp.log`.
4. **Enable debug** — Toggle debug logging in the Unity Editor window.
5. **Test connectivity** — From your MCP client, ask "Get the Unity editor state".

## Getting Help

If you're still stuck, open an issue on [GitHub](https://github.com/Manasistheone/unity-2021-mcp/issues) with:
- Your OS and Unity version
- The error message from Unity Console or Python logs
- Steps to reproduce

---

← [Configuration](Configuration) | [Home](Home)
