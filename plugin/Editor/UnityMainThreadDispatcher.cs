using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Dispatches actions to the Unity main thread.
    /// Unity API calls must happen on the main thread; this class provides
    /// a queue that is drained during EditorApplication.update.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMainThreadDispatcher
    {
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();
        private static readonly object _lock = new object();

        static UnityMainThreadDispatcher()
        {
            EditorApplication.update += DrainQueue;
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread during the next update.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                _actionQueue.Enqueue(action);
            }
        }

        private static void DrainQueue()
        {
            Action[] actions;
            lock (_lock)
            {
                if (_actionQueue.Count == 0) return;
                actions = _actionQueue.ToArray();
                _actionQueue.Clear();
            }

            foreach (Action action in actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    McpLog.Error($"Main thread action failed: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
