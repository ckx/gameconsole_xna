#region File Description
//-----------------------------------------------------------------------------
// CommandEventArgs.cs
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
    /// Represents the method that will handle an command event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">CommandEventArgs that contains the command,
    /// all aguments passed on the command and a timestamp.</param>
    public delegate void CommandHandler(object sender, CommandEventArgs e);

    /// <summary>
    /// Defines the EventArgs for the command event.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp when the event is raised and the command is executed.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// The command.
        /// </summary>
        public Command Command { get; private set; }

        /// <summary>
        /// The arguments passed on the command.
        /// </summary>
        public string[] Args { get; private set; }

        /// <summary>
        /// Creates a new instance of the CommandEventArgs.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="args">The arguments passed on the command.</param>
        public CommandEventArgs(Command command, string[] args)
        {
            Time = DateTime.Now;
            Command = command;
            Args = args;
        }
    }
}
