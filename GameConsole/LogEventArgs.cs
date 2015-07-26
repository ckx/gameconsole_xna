#region Using Statements
using System;
#endregion

namespace VosSoft.Xna.GameConsole
{
    /// <summary>
    /// Represents the method that will handle an log event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">LogEventsArgs that contains an log entry.</param>
    public delegate void LogHandler(object sender, LogEventArgs e);

    /// <summary>
    /// Defines the EventsArgs for the log event.
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// The log entry.
        /// </summary>
        public LogEntry Entry { get; private set; }

        /// <summary>
        /// Creates a new instance of the LogEventArgs.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        public LogEventArgs(LogEntry entry)
        {
            Entry = entry;
        }
    }
}