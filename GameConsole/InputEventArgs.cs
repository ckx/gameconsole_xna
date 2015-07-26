#region Using Statements
using System;
#endregion

namespace VosSoft.Xna.GameConsole
{
    /// <summary>
    /// Represents the method that will handle an user input event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">InputEventsArgs that contains the user input and a timestamp.</param>
    public delegate void InputHandler(object sender, InputEventArgs e);

    /// <summary>
    /// Defines the EventArgs for the input event.
    /// </summary>
    public class InputEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the timestamp when the event is raised and the input is send to the console.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the input text.
        /// </summary>
        public string Input { get; private set; }

        /// <summary>
        /// Gets or sets if the input will be executed (default true).
        /// </summary>
        public bool Execute { get; set; }

        /// <summary>
        /// Gets or sets if the input will be added to the log (default true).
        /// </summary>
        public bool AddToLog { get; set; }

        /// <summary>
        /// Creates a new instance of the InputEventArgs.
        /// </summary>
        /// <param name="input">The input text.</param>
        public InputEventArgs(string input)
        {
            Time = DateTime.Now;
            Input = input;
            Execute = true;
            AddToLog = true;
        }
    }
}