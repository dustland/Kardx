using UnityEngine;

namespace Kardx.Utils
{
    /// <summary>
    /// A simple logger implementation that directly uses Unity's Debug class.
    /// This is a simpler alternative to the thread-safe UnityLogger when cross-thread logging is not needed.
    /// </summary>
    public class SimpleLogger : ILogger
    {
        private readonly string prefix;

        /// <summary>
        /// Creates a new instance of the SimpleLogger class.
        /// </summary>
        /// <param name="prefix">Optional prefix to add to all log messages.</param>
        public SimpleLogger(string prefix = null)
        {
            this.prefix = prefix;
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            Debug.Log(FormatMessage(message));
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message)
        {
            Debug.LogWarning(FormatMessage(message));
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message)
        {
            Debug.LogError(FormatMessage(message));
        }

        /// <summary>
        /// Formats a message with the prefix, if one was specified.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The formatted message.</returns>
        private string FormatMessage(string message)
        {
            return string.IsNullOrEmpty(prefix) ? message : $"[{prefix}] {message}";
        }

        /// <summary>
        /// Creates a new SimpleLogger with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to add to all log messages.</param>
        /// <returns>A new SimpleLogger instance.</returns>
        public static SimpleLogger CreateWithPrefix(string prefix)
        {
            return new SimpleLogger(prefix);
        }
    }
}
