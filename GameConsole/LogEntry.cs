#region Using Statements
using System;
#endregion

namespace VosSoft.Xna.GameConsole
{
    /// <summary>
    /// Defines a log entry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the time format for the log entry.
        /// </summary>
        public static string TimeFormat = "HH:mm:ss";

        /// <summary>
        /// Gets the timestamp of the log entry.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        public byte Level { get; private set; }

        /// <summary>
        /// Creates a new log entry.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="level">the log level.</param>
        public LogEntry(string message, byte level)
        {
            Time = DateTime.Now;
            Message = message.Replace('\n', ' ');
            Level = level;
        }

        /// <summary>
        /// Creates a new log entry with the default log level 0.
        /// </summary>
        /// <param name="message">The log message.</param>
        public LogEntry(string message)
            : this(message, 0) {}

        /// <summary>
        /// Returns a string of the current log entry.
        /// </summary>
        /// <returns>A string that represents the current log entry.</returns>
        public override string ToString()
        {
            return string.Format("[{0}] {2} [{3}]", Time.ToString(TimeFormat), TimeFormat, Message,
                Level);
        }

        /// <summary>
        /// Returns a string of the current log entry.
        /// </summary>
        /// <param name="showTime">True if the timestamp should be included, otherwise false.</param>
        /// <param name="showLevel">True if the log level should be included, otherwise false.</param>
        /// <returns>A string that represents the current log entry.</returns>
        public string ToString(bool showTime, bool showLevel)
        {
            string entry = "";

            if (showTime) {
                entry += "[" + Time.ToString(TimeFormat) + "] ";
            }
            entry += Message;
            if (showLevel) {
                entry += " [" + Level + "]";
            }

            return entry;
        }
    }
}