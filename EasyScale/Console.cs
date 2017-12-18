using System.Collections.Generic;
using UnityEngine;

namespace Lench.EasyScale
{
    /// <summary>
    /// A console to display Unity's debug logs in-game.
    /// </summary>
    public class Console : MonoBehaviour
    {
        private const int Margin = 20;

        // Visual elements:

        private static readonly Dictionary<LogType, Color> LogTypeColors = new Dictionary<LogType, Color>
        {
            {LogType.Assert, Color.white},
            {LogType.Error, Color.red},
            {LogType.Exception, Color.red},
            {LogType.Log, Color.white},
            {LogType.Warning, Color.yellow}
        };

        private readonly GUIContent _clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
        private bool _collapse;
        private readonly GUIContent _collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

        private readonly List<Log> _logs = new List<Log>();
        private Vector2 _scrollPosition;
        private bool _show;
        private readonly Rect _titleBarRect = new Rect(0, 0, 10000, 20);

        /// <summary>
        ///     The hotkey to show and hide the console window.
        /// </summary>
        public KeyCode ToggleKey = KeyCode.BackQuote;

        private Rect _windowRect = new Rect(Margin, Margin, Screen.width - Margin * 2, Screen.height - Margin * 2);

        // ReSharper disable once UnusedMember.Local
        private void OnEnable()
        {
            Application.RegisterLogCallback(HandleLog);
        }

        private void OnDisable()
        {
            Application.RegisterLogCallback(null);
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                _show = !_show;
        }

        private void OnGUI()
        {
            if (!_show)
                return;

            _windowRect = GUILayout.Window(123456, _windowRect, ConsoleWindow, "Console");
        }

        /// <summary>
        ///     A window that displayss the recorded logs.
        /// </summary>
        /// <param name="windowId">Window ID.</param>
        private void ConsoleWindow(int windowId)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // Iterate through the recorded logs.
            for (var i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];

                // Combine identical messages if collapse option is chosen.
                if (_collapse)
                {
                    var messageSameAsPrevious = i > 0 && log.Message == _logs[i - 1].Message;

                    if (messageSameAsPrevious)
                        continue;
                }

                GUI.contentColor = LogTypeColors[log.Type];
                GUILayout.Label(log.Message);
            }

            GUILayout.EndScrollView();

            GUI.contentColor = Color.white;

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_clearLabel))
                _logs.Clear();

            _collapse = GUILayout.Toggle(_collapse, _collapseLabel, GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();

            // Allow the window to be dragged by its title bar.
            GUI.DragWindow(_titleBarRect);
        }

        /// <summary>
        ///     Records a log from the log callback.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="stackTrace">Trace of where the message came from.</param>
        /// <param name="type">Type of message (error, exception, warning, assert).</param>
        private void HandleLog(string message, string stackTrace, LogType type)
        {
            _logs.Add(new Log
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type
            });
        }

        private struct Log
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
        }
    }
}