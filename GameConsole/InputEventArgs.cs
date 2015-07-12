#region File Description
//-----------------------------------------------------------------------------
// InputEventArgs.cs
//
// Game Console
// Copyright (c) 2009 VosSoft
//-----------------------------------------------------------------------------
#endregion
#region License
//-----------------------------------------------------------------------------
// The MIT License (MIT)
//
// Copyright (c) 2009 VosSoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------
#endregion

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
