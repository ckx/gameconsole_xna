#region File Description
//-----------------------------------------------------------------------------
// GameConsole.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace VosSoft.Xna.GameConsole
{
    /// <summary>
    /// Defines a highly customizable game console, which can be used with any XNA application.
    /// </summary>
    public sealed class GameConsole : DrawableGameComponent
    {
        #region Fields

        const int defaultCapacity = 1000;
        const int defaultLines = 8;
        const string noDescription = "No description found.";

        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont font, defaultFont;
        Texture2D blank;

        Texture2D backgroundTexture;
        //Color backgroundColor = Color.Black;
        Color backgroundColor = new Color(0, 0, 0, 192);
        //float backgroundAlpha = 0.75f;

        Color logDefaultColor = Color.LightGray;
        Color inputColor = Color.White;
        //float textAlpha = 1.0f;

        Rectangle bounds, boundsWindow;

        Vector2 fontSize;
        int maxVisibleLines, charsPerLine;

        float padding = 4.0f;
        Vector2 textOrigin;

        List<LogEntry> log;
        IEnumerable<LogEntry> logSelected;
        int maxLogEntries;

        Dictionary<byte, bool> logLevelVisibility;
        Dictionary<byte, Color> logLevelColors;

        bool autoScroll = true;
        bool inputEnabled = true;

        string prefix = "> ";
        string input = "";

        float cursorBottomMargin;

        List<string> inputHistory;

        int logSelectedCount, visibleLines, currentLine;
        int cursorPosition, inputHistoryPosition;

        bool isOpen, isOpening, isClosing;
        bool showLogTime = false, showLogLevel = false;
        bool isFullscreen;
        bool reportOnError = true;
        bool logDebugMessages;

        //KeyMap keyMap;
        Keys? closeKey;
        KeyboardState currentKeyboardState = new KeyboardState(), lastKeyboardState;

        GameConsoleAnimation openingAnimation = GameConsoleAnimation.FadeSlideTop;
        GameConsoleAnimation closingAnimation = GameConsoleAnimation.FadeSlideTop;
        float openingAnimationTime = 0.5f;
        float closingAnimationTime = 0.5f;
        //float animationTime, animationPercentage, animationBackgroundAlpha, animationTextAlpha;
        float animationTime, animationPercentage;
        int animationPosition;

        float cursorBlinkTime = 1.0f, currentCursorBlinkTime;

        private MouseState currentMouseState;
        private int oldScrollWheelValue;

        string _pasteResult = "";

        #endregion

        #region Properties

        /// <summary>
        /// <para>Gets or sets the SpriteFont of the game console.</para>
        /// <para>This should be a console font where all character have the same width,
        /// otherwise the cursor and some other functions may not work properly.</para>
        /// <para>The default internal font will be used if this is set to null (default).</para>
        /// </summary>
        public SpriteFont Font
        {
            get { return font; }
            set
            {
                if (value != null)
                {
                    font = value;
                    calculateFontSize();
                    calculateTextArea();
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the background texture of the game console.</para>
        /// <para>The texture will always be sized to the bounds of the game console.</para>
        /// <para>If this is set to null, only the background color will be used (default).</para>
        /// </summary>
        public Texture2D BackgroundTexture
        {
            get { return backgroundTexture; }
            set
            {
                backgroundTexture = value ?? blank;
            }
        }

        /// <summary>
        /// <para>Gets or sets the background color of the game console.</para>
        /// <para>If the background texture is set, this will be used as an color overlay
        /// for the texture. If the texture is set to null, only this color will be used.</para>
        /// <para>The default background color is black.</para>
        /// </summary>
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        ///// <summary>
        ///// <para>The alpha channel value for the background.</para>
        ///// <value>The value can be set from 0.0 to 1.0 for 0 to 100 % opacity.</value>
        ///// <para>The default background alpha is 0.75.</para>
        ///// </summary>
        //public float BackgroundAlpha
        //{
        //    get { return backgroundAlpha; }
        //    set
        //    {
        //        backgroundAlpha = MathHelper.Clamp(value, 0.0f, 1.0f);
        //    }
        //}

        /// <summary>
        /// <para>The default text color for the log entries
        /// if no other color is set with the SetLogLevelColor method.</para>
        /// <para>The default color for the log text is light gray.</para>
        /// </summary>
        public Color LogDefaultColor
        {
            get { return logDefaultColor; }
            set { logDefaultColor = value; }
        }

        /// <summary>
        /// <para>The text color for the input field.</para>
        /// <para>The default input text color is white.</para>
        /// </summary>
        public Color InputColor
        {
            get { return inputColor; }
            set { inputColor = value; }
        }

        ///// <summary>
        ///// <para>The alpha channel value for all the text used inside the game console.</para>
        ///// <value>The value can be set from 0.0 to 1.0 for 0 to 100 % opacity.</value>
        ///// <para>The default text alpha is 1.0.</para>
        ///// </summary>
        //public float TextAlpha
        //{
        //    get { return textAlpha; }
        //    set
        //    {
        //        textAlpha = MathHelper.Clamp(value, 0.0f, 1.0f);
        //    }
        //}

        /// <summary>
        /// Gets or sets the bounds of the game console, including the position and size (in pixels).
        /// </summary>
        public Rectangle Bounds
        {
            get { return bounds; }
            set
            {
                if (value != Rectangle.Empty && value.Height - padding * 2.0f >= fontSize.Y && value.Width > fontSize.X)
                {
                    bounds = value;
                    calculateTextArea();
                    isFullscreen = false;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the padding between the text inside the game console and the edges of the bounds (in pixels).</para>
        /// <para>The default padding is 4.0 pixels.</para>
        /// </summary>
        public float Padding
        {
            get { return padding; }
            set
            {
                if (value >= 0.0f)
                {
                    padding = value;
                    calculateTextArea();
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the maximum log entries that can be stored to the log.</para>
        /// <para>If there will be more log entries added then this value, the last entry will drop out of the log.</para>
        /// <para>If this is set to 0 (default), there is no limit for the log entry count.</para>
        /// </summary>
        public int MaxLogEntries
        {
            get { return maxLogEntries; }
            set
            {
                maxLogEntries = value > 0 ? value : 0;
                if (maxLogEntries == 0)
                {
                    log.Capacity = defaultCapacity;
                }
                else
                {
                    if (log.Count > maxLogEntries)
                    {
                        log.RemoveRange(0, log.Count - maxLogEntries);
                        currentLine = 0;
                        updateView(false);
                    }
                    log.Capacity = maxLogEntries;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets if the game console should always scroll down if a new entry is added to the log.</para>
        /// <para>The default value is true, so the log will automatically scroll down.</para>
        /// </summary>
        public bool AutoScroll
        {
            get { return autoScroll; }
            set
            {
                autoScroll = value;
                if (autoScroll)
                {
                    updateView(true);
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets if the input field is enabled.</para>
        /// <para>The default value is true.</para>
        /// </summary>
        public bool InputEnabled
        {
            get { return inputEnabled; }
            set
            {
                inputEnabled = value;
                calculateTextArea();
            }
        }

        /// <summary>
        /// <para>Gets or sets the prefix left to the input field.</para>
        /// <para>The default prefix is <c>"> "</c>.</para>
        /// </summary>
        public string Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        /// <summary>
        /// Gets or sets the text of the input field.
        /// </summary>
        public string Input
        {
            get { return input; }
            set
            {
                input = value;
                if (cursorPosition > input.Length)
                {
                    cursorPosition = input.Length;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the button margin of the cursor (in pixels).</para>
        /// <para>The default value is 0 pixels.</para>
        /// </summary>
        public float CursorBottomMargin
        {
            get { return cursorBottomMargin; }
            set { cursorBottomMargin = value; }
        }

        /// <summary>
        /// Gets the count of all log entries.
        /// </summary>
        public int Count
        {
            get { return log.Count; }
        }

        /// <summary>
        /// Gets the count of all visible log entries.
        /// </summary>
        public int CountVisible
        {
            get { return logSelectedCount; }
        }

        /// <summary>
        /// <para>Gets or sets the visible text lines of the game console, including the input field (if enabled).</para>
        /// <para>Default there are 8 lines visible.</para>
        /// </summary>
        public int Lines
        {
            get
            {
                return (int)((bounds.Height - padding * 2.0f) / fontSize.Y);
            }
            set
            {
                if (value > 0)
                {
                    Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width,
                        (int)(fontSize.Y * value + padding * 2.0f));
                }
            }
        }

        /// <summary>
        /// Gets or sets the cursor position.
        /// </summary>
        public int CursorPosition
        {
            get { return cursorPosition; }
            set
            {
                cursorPosition = (int)MathHelper.Clamp(value, 0, input.Length);
            }
        }

        /// <summary>
        /// Gets if the game console is open.
        /// </summary>
        public bool IsOpen
        {
            get { return isOpen; }
        }

        /// <summary>
        /// Gets if the game console is opening.
        /// </summary>
        public bool IsOpening
        {
            get { return isOpening; }
        }

        /// <summary>
        /// Gets if the game console is closing.
        /// </summary>
        public bool IsClosing
        {
            get { return isClosing; }
        }

        /// <summary>
        /// Gets or sets if the log time is visible (default true).
        /// </summary>
        public bool ShowLogTime
        {
            get { return showLogTime; }
            set { showLogTime = value; }
        }

        /// <summary>
        /// Gets or sets if the log level is visible (default true).
        /// </summary>
        public bool ShowLogLevel
        {
            get { return showLogLevel; }
            set { showLogLevel = value; }
        }

        /// <summary>
        /// Gets or sets if the game console is running in fullscreen (default false).
        /// </summary>
        public bool IsFullscreen
        {
            get { return isFullscreen; }
            set
            {
                if (value)
                {
                    boundsWindow = bounds;
                    Bounds = new Rectangle(0, 0, GraphicsDevice.Viewport.TitleSafeArea.Width,
                        GraphicsDevice.Viewport.TitleSafeArea.Height);
                }
                else
                {
                    Bounds = boundsWindow;
                }
                isFullscreen = value;
            }
        }

        /// <summary>
        /// Gets or sets if any error messages should be logged.
        /// </summary>
        public bool ReportOnError
        {
            get { return reportOnError; }
            set { reportOnError = value; }
        }

        /// <summary>
        /// Gets or sets if the debug messages (log level 255) should be added to the log (default false).
        /// </summary>
        public bool LogDebugMessages
        {
            get { return logDebugMessages; }
            set { logDebugMessages = value; }
        }

        ///// <summary>
        ///// <para>Gets or sets the key map for the current game console.</para>
        ///// <para>If the key map is set to null, the default key map will be used.</para>
        ///// </summary>
        //public KeyMap KeyMap
        //{
        //    get { return keyMap; }
        //    set
        //    {
        //        keyMap = value ?? KeyMap.DefaultKeyMap;
        //    }
        //}

        /// <summary>
        /// <para>Gets or sets the opening animation of the game console.</para>
        /// <para>The default opening animation is set to fade and slide from the top.</para>
        /// </summary>
        public GameConsoleAnimation OpeningAnimation
        {
            get { return openingAnimation; }
            set { openingAnimation = value; }
        }

        /// <summary>
        /// <para>Gets or sets the closing animation of the game console.</para>
        /// <para>The default closing animation is set to fade and slide to the top.</para>
        /// </summary>
        public GameConsoleAnimation ClosingAnimation
        {
            get { return closingAnimation; }
            set { closingAnimation = value; }
        }

        /// <summary>
        /// <para>Gets or sets the time for the opening animation of the game console (in seconds).</para>
        /// <para>The default opening animation time is set to 0.5 seconds.</para>
        /// </summary>
        public float OpeningAnimationTime
        {
            get { return openingAnimationTime; }
            set
            {
                openingAnimationTime = value > 0.0f ? value : 0.5f;
            }
        }

        /// <summary>
        /// <para>Gets or sets the time for the closing animation (in seconds).</para>
        /// <para>The default closing animation time is set to 0.5 seconds.</para>
        /// </summary>
        public float ClosingAnimationTime
        {
            get { return closingAnimationTime; }
            set
            {
                closingAnimationTime = value > 0.0f ? value : 0.5f;
            }
        }

        /// <summary>
        /// <para>Gets or sets the blinking time period for the cursor (in seconds).</para>
        /// <para>If this value is set to 0.0 seconds the cursor is always visible.</para>
        /// <para>The default blinking time period is set to 1.0 seconds.</para>
        /// </summary>
        public float CursorBlinkTime
        {
            get { return cursorBlinkTime; }
            set
            {
                if (value >= 0.0f)
                {
                    cursorBlinkTime = value;
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// The dictionary to store all the commands and command event handlers.
        /// </summary>
        Dictionary<string, Command> commands;

        /// <summary>
        /// Occurs after the game console has been initialized.
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// Occurs before the game console opens, thereby the opening can be canceled.
        /// </summary>
        public event CancelEventHandler Opening;

        /// <summary>
        /// Occurs when the game console opens.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Occurs before the game console will be closed, thereby the closing can be canceled.
        /// </summary>
        public event CancelEventHandler Closing;

        /// <summary>
        /// Occurs when the game console closes.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Occurs every time the input text has changed.
        /// </summary>
        public event InputHandler InputChanged;

        /// <summary>
        /// Occurs after the user hits enter inside the input field.
        /// </summary>
        public event InputHandler InputEntered;

        /// <summary>
        /// Occurs after a new log entry was added to the log.
        /// </summary>
        public event LogHandler LogEntryAdded;

        #endregion

        #region Initialization

        /// <summary>
        /// <para>Creates a new game console with the default bounds and colors.</para>
        /// <para>The console will be slide from the top with 100 % width and 8 lines height.</para>
        /// </summary>
        /// <param name="game">Reference to the game.</param>
        public GameConsole(Game game)
            : base(game)
        {
            Game.Components.Add(this);
            content = new ResourceContentManager(Game.Services, GameConsoleContent.ResourceManager);

            log = new List<LogEntry>(defaultCapacity);
            commands = new Dictionary<string, Command>();
            inputHistory = new List<string>();

            logLevelVisibility = new Dictionary<byte, bool>();
            logLevelColors = new Dictionary<byte, Color>();

            System.Threading.Thread.CurrentThread.CurrentCulture =
                System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

            //KeyMap.LoadKeyMaps(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "KeyMaps"));
            //KeyMap = keyMap == null || keyMap == String.Empty ? KeyMap.DefaultKeyMap : KeyMap.GetKeyMap(keyMap);

            Enabled = Visible = false;
        }

        /// <summary>
        /// Creates a new game console.
        /// </summary>
        /// <param name="game">Reference to the game.</param>
        /// <param name="bounds"><para>The bounds of the game console, including the position and size (in pixels).</para>
        /// <para>Can be null, then the default bounds will be used (100 % width and 8 lines height).</para></param>
        /// <param name="backgroundColor">The background color of the game console.</param>
        //public GameConsole(Game game, Rectangle? bounds, Color backgroundColor, float backgroundAlpha)
        //    : this(game)
        public GameConsole(Game game, Rectangle? bounds, Color backgroundColor)
            : this(game)
        {
            this.bounds = bounds ?? Rectangle.Empty;
            this.backgroundColor = backgroundColor;
            //this.backgroundAlpha = backgroundAlpha;
        }

        /// <summary>
        /// Creates a new game console.
        /// </summary>
        /// <param name="game">Reference to the game.</param>
        /// <param name="bounds"><para>The bounds of the game console, including the position and size (in pixels).</para>
        /// <para>Can be null, then the default bounds will be used (100 % width and 8 lines height).</para></param>
        /// <param name="backgroundTexture"><para>The background texture.</para>
        /// <para>Can be null, then the default background color (black) will be used for the background.</para></param>
        //public GameConsole(Game game, Rectangle? bounds, Texture2D backgroundTexture, float backgroundAlpha, string keyMap)
        //    : this(game, bounds, Color.White, backgroundAlpha)
        public GameConsole(Game game, Rectangle? bounds, Texture2D backgroundTexture)
            : this(game, bounds, Color.White)
        {
            this.backgroundTexture = backgroundTexture;
        }

        /// <summary>
        /// Initialize the game console and adds some internal commands to the command dictionary.
        /// </summary>
        public override void Initialize()
        {
            EventInput.Initialize(Game.Window);
            EventInput.CharEntered += OnCharEntered;
            EventInput.KeyDown += OnKeyDown;

            currentMouseState = new MouseState();

            //AddCommand("con_help", delegate(object sender, CommandEventArgs e)
            //{
            //    Log("help is coming... soon! ;)");
            //}, "Shows the help.");

            AddCommand("con_info", delegate(object sender, CommandEventArgs e)
            {
                Log(String.Format("Log entries visible: {0}/{1}", logSelectedCount + 1, log.Count));
                Log("History entries: " + inputHistory.Count);
                Log("Commands registered: " + commands.Count);
            }, "Shows some internal console informations.");

            AddCommand("commands", delegate(object sender, CommandEventArgs e)
            {
                IEnumerable<Command> commandsSelected;
                if (e.Args.Length > 0)
                {
                    commandsSelected = from c in commands.Values
                                       where c.Name.Contains(e.Args[0])
                                       orderby c.Name
                                       select c;
                    if (commandsSelected.Count() > 0)
                    {
                        Log("List of " + commandsSelected.Count().ToString()
                            + " appropriate commands for '" + e.Args[0] + "':");
                    }
                    else
                    {
                        Log("No appropriate commands found for '" + e.Args[0] + "'.");
                        //e.Command.AddToHistory = false;
                    }
                }
                else
                {
                    commandsSelected = from c in commands.Values
                                       orderby c.Name
                                       select c;
                    Log("List of all " + commands.Count.ToString() + " commands:");
                }
                int i = 0;
                foreach (Command command in commandsSelected)
                {
                    Log(String.Format("{0:000}: {1} => {2}", ++i, command.Name, command.Manual[0]));
                }
            }, "Lists all currently registered commands with their first description line.",
            "commands <part of the command> - lists only the appropriate commands.");

            AddCommand("man", delegate(object sender, CommandEventArgs e)
            {
                if (e.Args.Length > 0)
                {
                    if (commands.ContainsKey(e.Args[0]))
                    {
                        Log("Manual for the command '" + e.Args[0] + "':");
                        foreach (string descriptionLine in commands[e.Args[0]].Manual)
                        {
                            Log(descriptionLine);
                        }
                    }
                    else
                    {
                        Log("Command '" + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else
                {
                    ExecManual("man");
                }
            }, "Displays the manual of the provided command.",
            "man <command>");

            AddCommand("history", delegate(object sender, CommandEventArgs e)
            {
                int endIndex = 0;
                if (e.Args.Length > 0)
                {
                    int count = int.Parse(e.Args[0]);
                    endIndex = count <= inputHistory.Count ? inputHistory.Count - count : 0;
                }
                Log("There are " + inputHistory.Count.ToString() + " entries in the input history.");
                for (int i = inputHistory.Count - 1; i >= endIndex; i--)
                {
                    Log(String.Format("{0:000}: {1}", inputHistory.Count - i, inputHistory[i]));
                }
            }, "Lists the input history.",
            "history <number> - lists only the last <number> entries.");

            AddCommand("!", delegate(object sender, CommandEventArgs e)
            {
                int exeIndex = inputHistory.Count - 1;
                if (e.Args.Length > 0)
                {
                    int number = int.Parse(e.Args[0]);
                    exeIndex -= number;
                    if (exeIndex < 0 || exeIndex > inputHistory.Count - 1)
                    {
                        return;
                    }
                }
                if (inputHistory.Count > 0)
                {
                    Execute(inputHistory[exeIndex]);
                }
            }, false, false, "Executes the last command again.",
            "! <number> - executes the specified command form the input history again.");

            AddCommand("close", delegate(object sender, CommandEventArgs e)
            {
                Close();
            }, false, false, "Closes the console.");

            AddCommand("exit", delegate(object sender, CommandEventArgs e)
            {
                Game.Exit();
            }, false, false, "Exit the game.");

            AddCommand("clear", delegate(object sender, CommandEventArgs e)
            {
                if (e.Args.Length > 0)
                {
                    if (e.Args[0] == "history")
                    {
                        ClearHistory();
                    }
                    else if (e.Args[0] == "commands")
                    {
                        ClearCommands();
                    }
                    else
                    {
                        Log("Command 'clear " + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else
                {
                    Clear();
                }
            }, "Clears the console log, the input history or the command list.",
            "clear - clears the log.",
            "clear history - clears the input history.",
            "clear commands - clears all registered commands.");

            AddCommand("con_set", delegate(object sender, CommandEventArgs e)
            {
                if (e.Args.Length > 0)
                {
                    if (e.Args[0] == "color" && e.Args.Length > 1)
                    {
                        if (e.Args[1] == "background")
                        {
                            BackgroundColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]), byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "log")
                        {
                            LogDefaultColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]), byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "input")
                        {
                            InputColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]), byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "level")
                        {
                            SetLogLevelColor(byte.Parse(e.Args[2]), new Color(byte.Parse(e.Args[3]), byte.Parse(e.Args[4]), byte.Parse(e.Args[5])));
                        }
                        else
                        {
                            Log("Command 'con_set color " + e.Args[1] + "' not found.", 1);
                            //e.Command.AddToHistory = false;
                        }
                    }
                    else if (e.Args[0] == "alpha" && e.Args.Length > 1)
                    {
                        if (e.Args[1] == "background")
                        {
                            //BackgroundAlpha = float.Parse(e.Args[2]);
                            backgroundColor.A = byte.Parse(e.Args[2]);
                        }
                        //else if (e.Args[1] == "text")
                        //{
                        //    //TextAlpha = float.Parse(e.Args[2]);
                        //    inputColor.A = byte.Parse(e.Args[2]);
                        //}
                        else
                        {
                            Log("Command 'con_set alpha " + e.Args[1] + "' not found.", 1);
                            //e.Command.AddToHistory = false;
                        }
                    }
                    else if (e.Args[0] == "bounds")
                    {
                        Bounds = new Rectangle(int.Parse(e.Args[1]), int.Parse(e.Args[2]),
                        int.Parse(e.Args[3]), int.Parse(e.Args[4]));
                    }
                    else if (e.Args[0] == "padding")
                    {
                        Padding = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "margin")
                    {
                        CursorBottomMargin = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "blink")
                    {
                        CursorBlinkTime = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "prefix")
                    {
                        Prefix = e.Args[1] + " ";
                    }
                    else if (e.Args[0] == "lines")
                    {
                        Lines = int.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "timeformat")
                    {
                        LogEntry.TimeFormat = e.Args[1];
                    }
                    else
                    {
                        Log("Command 'con_set " + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else
                {
                    ExecManual("con_set");
                }
            }, "Sets properties of the console.",
            "con_set color background <r:byte> <g:byte> <b:byte> - sets the background color.",
            "con_set color log <r:byte> <g:byte> <b:byte> - sets the default log text color.",
            "con_set color input <r:byte> <g:byte> <b:byte> - sets the input text color.",
            "con_set color level <level:byte> <r:byte> <g:byte> <b:byte> - sets the log level color.",
            "con_set alpha background <byte> - sets the background alpha (0-255).",
            "con_set bounds <x:int> <y:int> <width:int> <height:int>",
            "con_set padding <float> - sets the padding inside the bounds.",
            "con_set margin <float> - sets the bottom margin of the cursor.",
            "con_set blink <float> - sets the blink speed of the cursor (in seconds).",
            "con_set prefix <string> - sets the prefix of the input line.",
            "con_set lines <int> - sets the maximum visible lines.",
            "con_set timeformat <string> - sets the time format for the timestamp.");
            //}, "Sets properties of the console.",
            //"con_set color background <r:byte> <g:byte> <b:byte> - sets the background color.",
            //"con_set color log <r:byte> <g:byte> <b:byte> - sets the default log text color.",
            //"con_set color input <r:byte> <g:byte> <b:byte> - sets the input text color.",
            //"con_set color level <level:byte> <r:byte> <g:byte> <b:byte> - sets the log level color.",
            //"con_set alpha background <float> - sets the background alpha (0-1).",
            //"con_set alpha text <float> - sets the text alpha (0-1).",
            //"con_set bounds <x:int> <y:int> <width:int> <height:int>",
            //"con_set padding <float> - sets the padding inside the bounds.",
            //"con_set margin <float> - sets the bottom margin of the cursor.",
            //"con_set blink <float> - sets the blink speed of the cursor (in seconds).",
            //"con_set prefix <string> - sets the prefix of the input line.",
            //"con_set lines <int> - sets the maximum visible lines.",
            //"con_set timeformat <string> - sets the time format for the timestamp.");

            AddCommand("con_tog", delegate(object sender, CommandEventArgs e)
            {
                if (e.Args.Length > 0)
                {
                    if (e.Args[0] == "time")
                    {
                        ShowLogTime = !ShowLogTime;
                        Log("ShowLogTime = " + showLogTime.ToString(), 255);
                    }
                    else if (e.Args[0] == "level")
                    {
                        if (e.Args.Length > 1)
                        {
                            byte logLevel = byte.Parse(e.Args[1]);
                            bool logLevelInvisible = !GetLogLevelVisibility(logLevel);
                            SetLogLevelVisibility(logLevel, logLevelInvisible);
                            Log("LogLevelVisibility[" + logLevel.ToString() + "] = " + logLevelInvisible.ToString(), 255);
                        }
                        else
                        {
                            ShowLogLevel = !ShowLogLevel;
                            Log("ShowLogLevel = " + showLogLevel.ToString(), 255);
                        }
                    }
                    else if (e.Args[0] == "autoscroll")
                    {
                        AutoScroll = !AutoScroll;
                        Log("AutoScroll = " + AutoScroll.ToString(), 255);
                    }
                    else if (e.Args[0] == "fullscreen")
                    {
                        IsFullscreen = !IsFullscreen;
                        Log("IsFullscreen = " + IsFullscreen.ToString(), 255);
                    }
                    else if (e.Args[0] == "")
                    {
                        
                    }
                    else
                    {
                        Log("Command 'con_toggle " + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else
                {
                    ExecManual("con_tog");
                }
            }, "Toggles boolean properties of the console.",
            "con_toggle time - toggles the visibility of the log timestamp.",
            "con_toggle level - toggles the visibility of the log level.",
            "con_toggle level <byte> - toggles the provided log level visibility.",
            "con_toggle autoscroll - toggles the auto scrolling for the log.",
            "con_toggle fullscreen - toggles the fullscreen mode of the console.");

            AddCommand("save", delegate(object sender, CommandEventArgs e)
            {
                if (e.Args.Length > 0)
                {
                    StreamWriter file = File.CreateText(e.Args[0]);
                    foreach (LogEntry entry in log)
                    {
                        file.WriteLine(entry.ToString());
                    }
                    file.Close();
                    Log("Log successfully saved: " + e.Args[0]);
                }
                else
                {
                    StreamWriter file = File.CreateText("con_log");
                    foreach (LogEntry entry in log)
                    {
                        file.WriteLine(entry.ToString());
                    }
                    file.Close();
                    Log("Log successfully saved: " + "con_log");
                }
            }, "Saves the entire log to the provided file.",
            "save <path> - the path can be relative or absolute.",
            "If no path is supplied it saves to a file named con_log.");

            updateView(false);

            Log("Console initialized.", 255);

            base.Initialize();

            if (Initialized != null)
            {
                Initialized(this, new EventArgs());
            }
        }

        /// <summary>
        /// Loads the default font and blank texture for the game console
        /// and sets the default bounds and background texture if necessary.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            defaultFont = content.Load<SpriteFont>("DevConsoleFont");
            blank = new Texture2D(GraphicsDevice, 1, 1);
            blank.SetData<Color>(new Color[] { Color.White });

            font = defaultFont;
            calculateFontSize();

            if (bounds == Rectangle.Empty)
            {
                bounds = new Rectangle(0, 0, Game.GraphicsDevice.Viewport.TitleSafeArea.Width,
                    Game.GraphicsDevice.Viewport.TitleSafeArea.Height);
                Lines = defaultLines;
            }

            if (backgroundTexture == null)
            {
                backgroundTexture = blank;
            }

            base.LoadContent();
        }

        #endregion

        #region Private Methods

        [STAThread]
        void PasteThread()
        {
            if (Clipboard.ContainsText())
            {
                _pasteResult = Clipboard.GetText();
            }
            else
            {
                _pasteResult = "";
            }
        }

        void OnCharEntered(CharacterEventArgs e)
        {
            if (char.IsControl(e.Character))
            {
                //ctrl-v
                if (e.Character == 0x16)
                {
                    //XNA runs in Multiple Thread Apartment state, which cannot receive clipboard
                    Thread thread = new Thread(PasteThread);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    ReceiveTextInput(_pasteResult);
                }
                else
                {
                    ReceiveCommandInput(e.Character);
                }
            }
            else
            {
                ReceiveTextInput(e.Character);
            }
        }

        void OnKeyDown(KeyEventArgs e)
        {
            ReceiveKeyCodeInput(e.KeyCode);
        }

        void ReceiveCommandInput(char inputChar)
        {
            if (isOpen && inputEnabled)
            {
                if (inputChar == '\r' && input.Trim() != "")
                {
                    if (InputEntered != null)
                    {
                        InputEventArgs inputEventArgs = new InputEventArgs(input);
                        InputEntered(this, inputEventArgs);
                        if (inputEventArgs.Execute)
                        {
                            Execute(input, inputEventArgs.AddToLog);
                        }
                    }
                    else
                    {
                        Execute(input);
                    }

                    input = "";
                    cursorPosition = 0;

                    if (InputChanged != null)
                    {
                        InputChanged(this, new InputEventArgs(input));
                    }
                }
                else if (inputChar == '\b' && cursorPosition > 0)
                {
                    input = input.Remove(--cursorPosition, 1);

                    if (InputChanged != null)
                    {
                        InputChanged(this, new InputEventArgs(input));
                    }
                }
                else if (inputChar == '\t') // AutoComplete extension code
                {
                    IEnumerable<string> autoCollection =
                        from c in commands.Keys where c.StartsWith(input) orderby c select c;

                    if (autoCollection.Count() != 0)
                    {
                        int index = autoCollection.FirstOrDefault().Zip(
                            autoCollection.LastOrDefault(), (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
                        cursorPosition += autoCollection.FirstOrDefault().Zip(
                            autoCollection.LastOrDefault(), (c1, c2) => c1 == c2).TakeWhile(b => b).Count() - input.Length;
                        input = autoCollection.FirstOrDefault().Substring(0, index);
                        if (autoCollection.Count() > 1)
                        {
                            Log(Prefix + input);
                            foreach (string s in autoCollection)
                            {
                                Log(" -> " + s);
                            }
                        }
                    }
                }
            }
        }

        void ReceiveTextInput(string inputText)
        {
            if (isOpen && inputEnabled)
            {
                input += inputText;
                cursorPosition += inputText.Length;
            }
        }

        void ReceiveTextInput(char inputChar)
        {
            if (isOpen && inputEnabled)
            {
                if (inputChar == '\0')
                {
                    return;
                }

                if (cursorPosition == input.Length)
                {
                    input += inputChar;
                }
                else
                {
                    input = input.Insert(cursorPosition, inputChar.ToString());
                }

                cursorPosition++;
                inputHistoryPosition = inputHistory.Count;

                if (InputChanged != null)
                {
                    InputChanged(this, new InputEventArgs(input));
                }
            }
        }

        void ReceiveKeyCodeInput(Keys inputKey)
        {
            if (isOpen)
            {
                if (closeKey != null && inputKey == closeKey)
                {
                    Close();
                }
                else if (inputKey == Keys.PageUp && currentLine > 0)
                {
                    currentLine -= maxVisibleLines;
                    if (currentLine < 0)
                    {
                        currentLine = 0;
                    }
                    updateView(false);
                }
                else if (inputKey == Keys.PageDown && currentLine + maxVisibleLines < logSelectedCount)
                {
                    currentLine += maxVisibleLines;
                    if (currentLine > logSelectedCount - maxVisibleLines)
                    {
                        currentLine = logSelectedCount - maxVisibleLines;
                    }
                    updateView(false);
                }
                else if (inputKey == Keys.Delete && cursorPosition < input.Length)
                {
                    input = input.Remove(cursorPosition, 1);

                    if (InputChanged != null)
                    {
                        InputChanged(this, new InputEventArgs(input));
                    }
                }
                else if (inputKey == Keys.Left && cursorPosition > 0)
                {
                    cursorPosition--;
                }
                else if (inputKey == Keys.Right && cursorPosition < input.Length)
                {
                    cursorPosition++;
                }
                else if (inputKey == Keys.Home)
                {
                    cursorPosition = 0;
                }
                else if (inputKey == Keys.End)
                {
                    cursorPosition = input.Length;
                }
                else if (inputKey == Keys.Up && inputHistory.Count > 0 && inputHistoryPosition > 0)
                {
                    input = inputHistory[--inputHistoryPosition];
                    cursorPosition = input.Length;

                    if (InputChanged != null)
                    {
                        InputChanged(this, new InputEventArgs(input));
                    }
                }
                else if (inputKey == Keys.Down && inputHistoryPosition < inputHistory.Count - 1)
                {
                    input = inputHistory[++inputHistoryPosition];
                    cursorPosition = input.Length;

                    if (InputChanged != null)
                    {
                        InputChanged(this, new InputEventArgs(input));
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the the font size for each character. Because the font should be a console
        /// font where all characters have the same width, this is not very complicated.
        /// </summary>
        void calculateFontSize()
        {
            fontSize = new Vector2(font.MeasureString("X").X, font.MeasureString("Xy").Y);
        }

        /// <summary>
        /// Claculates the text area based on the font size and the bounding box.
        /// </summary>
        void calculateTextArea()
        {
            maxVisibleLines = (int)((bounds.Height - padding * 2.0f) / fontSize.Y);
            if (inputEnabled)
            {
                maxVisibleLines--;
            }
            charsPerLine = (int)((bounds.Width - padding * 2.0f) / fontSize.X);
            textOrigin = new Vector2(bounds.X + padding, bounds.Y + padding);

            updateView(autoScroll);
        }

        /// <summary>
        /// Updates the view of the current log entries, what log entries the user will see on the screen.
        /// </summary>
        /// <param name="scrollDown">True if the log should scroll down to the last entry, otherwise false.</param>
        void updateView(bool scrollDown)
        {
            if (logLevelVisibility.Count > 0)
            {
                logSelected = log.Where(entry => logLevelVisibility.ContainsKey(entry.Level) ? logLevelVisibility[entry.Level] : true);
                logSelectedCount = logSelected.Count();
            }
            else
            {
                logSelected = log;
                logSelectedCount = log.Count;
            }

            if (scrollDown)
            {
                currentLine = logSelectedCount - maxVisibleLines;
                if (currentLine < 0)
                {
                    currentLine = 0;
                }
            }

            logSelected = logSelected.Skip(currentLine).Take(maxVisibleLines);
            visibleLines = logSelectedCount > maxVisibleLines ? maxVisibleLines : logSelectedCount;
        }

        /// <summary>
        /// A little helper method to determine if the pressed key was newly pressed in this frame.
        /// </summary>
        /// <param name="key">The pressed key.</param>
        /// <returns>Returns true if the key was newly pressed, otherwise false.</returns>
        bool isNewKeyPress(Keys key)
        {
            return (currentKeyboardState.IsKeyDown(key) && lastKeyboardState.IsKeyUp(key));
        }

        /// <summary>
        /// Initialize the animation, sets the appropriate values depending on the animation type.
        /// </summary>
        /// <param name="animation">The animation type.</param>
        /// <param name="time">How long the animation takes.</param>
        void initializeAnimation(GameConsoleAnimation animation, float time)
        {
            if (animation == GameConsoleAnimation.None)
            {
                animationTime = 0.0f;
                return;
            }

            if ((animation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade)
            {
                //animationBackgroundAlpha = backgroundAlpha;
                //animationTextAlpha = textAlpha;
            }

            if ((animation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop
                || (animation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom)
            {
                animationPosition = bounds.Y;
            }
            else if ((animation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft
                || (animation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight)
            {
                animationPosition = bounds.X;
            }

            animationTime = time;
        }

        /// <summary>
        /// Cleans the animation up, resets the appropriate values depending on the animation type.
        /// </summary>
        /// <param name="animation">The animation type.</param>
        void animationCleanUp(GameConsoleAnimation animation)
        {
            if (animation == GameConsoleAnimation.None)
            {
                return;
            }

            if ((animation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade)
            {
                //backgroundAlpha = animationBackgroundAlpha;
                //textAlpha = animationTextAlpha;
            }

            if ((animation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop
                || (animation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom)
            {
                Bounds = new Rectangle(bounds.X, animationPosition, bounds.Width, bounds.Height);
            }
            else if ((animation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft
                || (animation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight)
            {
                Bounds = new Rectangle(animationPosition, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the game console. Reads user input, executes the commands and handles all animations.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //lastKeyboardState = currentKeyboardState;
            //currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            if (isOpening)
            {
                animationTime -= elapsedTime;
                if (animationTime <= 0.0f)
                {
                    animationCleanUp(openingAnimation);

                    isOpening = false;
                    isOpen = true;
                    if (Opened != null)
                    {
                        Opened(this, new EventArgs());
                    }
                    Log("Console opened.", 255);
                }
                else
                {
                    animationPercentage = animationTime / openingAnimationTime;
                    if ((openingAnimation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade)
                    {
                        //backgroundAlpha = (1.0f - animationPercentage) * animationBackgroundAlpha;
                        //textAlpha = (1.0f - animationPercentage) * animationTextAlpha;
                    }
                    if ((openingAnimation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop)
                    {
                        Bounds = new Rectangle(bounds.X, (int)(animationPosition - animationPercentage * bounds.Height),
                                bounds.Width, bounds.Height);
                    }
                    else if ((openingAnimation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom)
                    {
                        Bounds = new Rectangle(bounds.X, (int)(animationPosition - (1.0f - animationPercentage * bounds.Height)),
                                bounds.Width, bounds.Height);
                    }
                    else if ((openingAnimation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft)
                    {
                        Bounds = new Rectangle((int)(animationPosition - animationPercentage * bounds.Width), bounds.Y,
                                bounds.Width, bounds.Height);
                    }
                    else if ((openingAnimation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight)
                    {
                        Bounds = new Rectangle((int)(animationPosition - (1.0f - animationPercentage * bounds.Width)), bounds.Y,
                                bounds.Width, bounds.Height);
                    }
                }
            }
            else if (isClosing)
            {
                animationTime -= elapsedTime;
                if (animationTime <= 0.0f)
                {
                    animationCleanUp(closingAnimation);

                    Enabled = Visible = false;
                    isOpen = isClosing = false;
                    if (Closed != null)
                    {
                        Closed(this, new EventArgs());
                    }
                    Log("Console closed.", 255);
                }
                else
                {
                    animationPercentage = animationTime / closingAnimationTime;
                    if ((closingAnimation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade)
                    {
                        //backgroundAlpha = animationPercentage * animationBackgroundAlpha;
                        //textAlpha = animationPercentage * animationTextAlpha;
                    }
                    if ((closingAnimation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop)
                    {
                        Bounds = new Rectangle(bounds.X, (int)(animationPosition - bounds.Height - (1.0f - animationPercentage * bounds.Height)),
                            bounds.Width, bounds.Height);
                    }
                    else if ((closingAnimation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom)
                    {
                        Bounds = new Rectangle(bounds.X, (int)(animationPosition + bounds.Height - animationPercentage * bounds.Height),
                            bounds.Width, bounds.Height);
                    }
                    else if ((closingAnimation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft)
                    {
                        Bounds = new Rectangle((int)(animationPosition - bounds.Width - (1.0f - animationPercentage * bounds.Width)), bounds.Y,
                            bounds.Width, bounds.Height);
                    }
                    else if ((closingAnimation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight)
                    {
                        Bounds = new Rectangle((int)(animationPosition + bounds.Width - animationPercentage * bounds.Width), bounds.Y,
                            bounds.Width, bounds.Height);
                    }
                }
            }

            if (InputEnabled && cursorBlinkTime > 0.0f)
            {
                currentCursorBlinkTime += elapsedTime;
                if (currentCursorBlinkTime >= cursorBlinkTime)
                {
                    currentCursorBlinkTime -= cursorBlinkTime;
                }
            }

            if (isOpen)
            {
                if (currentMouseState.ScrollWheelValue > oldScrollWheelValue && currentLine > 0)
                {
                    currentLine -= (int)(maxVisibleLines / 3);
                    if (currentLine < 0)
                    {
                        currentLine = 0;
                    }
                    updateView(false);
                }
                else if (currentMouseState.ScrollWheelValue < oldScrollWheelValue && currentLine + maxVisibleLines < logSelectedCount)
                {
                    currentLine += (int)(maxVisibleLines / 3);
                    if (currentLine > logSelectedCount - maxVisibleLines)
                    {
                        currentLine = logSelectedCount - maxVisibleLines;
                    }
                    updateView(false);
                }
            }
            oldScrollWheelValue = currentMouseState.ScrollWheelValue;

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game console, the log entry view and input field (if enabled).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            //spriteBatch.Draw(backgroundTexture, bounds, new Color(backgroundColor, backgroundAlpha));
            spriteBatch.Draw(backgroundTexture, bounds, backgroundColor);

            int i = 0;
            foreach (LogEntry entry in logSelected)
            {
                string logEntry = entry.ToString(ShowLogTime, ShowLogLevel);
                
                //spriteBatch.DrawString(font, logEntry.Length > charsPerLine ? logEntry.Remove(charsPerLine - 2) + ".." : logEntry,
                //    textOrigin + new Vector2(0.0f, fontSize.Y * i++), logLevelColors.ContainsKey(entry.Level) ?
                //    new Color(logLevelColors[entry.Level], textAlpha) : new Color(logDefaultColor, textAlpha));

                spriteBatch.DrawString(font, logEntry.Length > charsPerLine ? logEntry.Remove(charsPerLine - 2) + ".." : logEntry,
                    textOrigin + new Vector2(0.0f, fontSize.Y * i++), logLevelColors.ContainsKey(entry.Level) ?
                    logLevelColors[entry.Level] : logDefaultColor);
            }

            if (inputEnabled)
            {
                //spriteBatch.DrawString(font, prefix + input, textOrigin + new Vector2(0.0f, visibleLines * fontSize.Y),
                //    new Color(inputColor, textAlpha));

                spriteBatch.DrawString(font, prefix + input, textOrigin + new Vector2(0.0f, visibleLines * fontSize.Y),
                    inputColor);

                if (cursorBlinkTime == 0.0f || currentCursorBlinkTime < cursorBlinkTime / 2.0f)
                {
                    //spriteBatch.Draw(blank, new Rectangle((int)(textOrigin.X + ((prefix.Length + cursorPosition) * fontSize.X)),
                    //    (int)(textOrigin.Y + (visibleLines + 1) * fontSize.Y + cursorBottomMargin),
                    //    (int)fontSize.X, 1), new Color(inputColor, textAlpha));

                    spriteBatch.Draw(blank, new Rectangle((int)(textOrigin.X + ((prefix.Length + cursorPosition) * fontSize.X)),
                        (int)(textOrigin.Y + (visibleLines + 1) * fontSize.Y + cursorBottomMargin),
                        (int)fontSize.X, 1), inputColor);
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// <para>Adds a custom command to the game console.</para>
        /// <para>It is possible to add more then one command with the same name.</para>
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="handler">The method that will be called if the command is executed.</param>
        /// <param name="addToLog">True to add the command to the log, otherwise false.</param>
        /// <param name="addToHistory">True to add the command to the input history, otherwise false.</param>
        /// <param name="manual">The manual of the command, one line per index.</param>
        /// <example>
        /// Add a new command using a delegate (extended example):
        /// <code>
        /// GameConsole console = new GameConsole(this, null);
        /// console.AddCommand("hello", delegate(object sender, CommandEventArgs e)
        /// {
        ///     console.Log("Hello World!");
        /// }, true, false, "hello", "world", "manual");
        /// </code>
        /// </example>
        public void AddCommand(string command, CommandHandler handler, bool addToLog, bool addToHistory, params string[] manual)
        {
            if (handler != null)
            {
                if (commands.ContainsKey(command))
                {
                    commands[command].Handler += handler;
                }
                else
                {
                    if (manual.Length == 0)
                    {
                        manual = new string[] { noDescription };
                    }
                    commands.Add(command, new Command(command, handler, addToLog, addToHistory, manual));
                }
            }
        }

        /// <summary>
        /// <para>Adds a custom command to the game console.</para>
        /// <para>It is possible to add more then one command with the same name.</para>
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="handler">The method that will be called if the command is executed.</param>
        /// <param name="manual">The manual of the command, one line per index.</param>
        /// <example>
        /// Add a new command using a lamba expression:
        /// <code>
        /// GameConsole console = new GameConsole(this, null);
        /// console.AddCommand("hello", (sender, e) => console.Log("Hello World!"));
        /// </code>
        /// </example>
        /// <example>
        /// Add a new command using a delegate:
        /// <code>
        /// GameConsole console = new GameConsole(this, null);
        /// console.AddCommand("hello", delegate(object sender, CommandEventArgs e)
        /// {
        ///     console.Log("Hello World!");
        /// });
        /// </code>
        /// </example>
        /// <example>
        /// Add a new command using a standalone method:
        /// <code>
        /// GameConsole console = new GameConsole(this, null);
        /// console.AddCommand("hello", testMethod);
        /// void testMethod(object sender, CommandEventArgs e)
        /// {
        ///     console.Log("Hello World!");
        /// }
        /// </code>
        /// </example>
        public void AddCommand(string command, CommandHandler handler, params string[] manual)
        {
            AddCommand(command, handler, true, true, manual);
        }

        /// <summary>
        /// Removes the command(s) from the game console.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        public void RemoveCommand(string command)
        {
            if (commands.ContainsKey(command))
            {
                commands.Remove(command);
            }
        }

        /// <summary>
        /// Opens the game console.
        /// </summary>
        /// <param name="closeKey"><para>The key that should close the game console.</para>
        /// <para>If the close key is set to null, the console can only be closed by call the <c>Close</c>-Method
        /// or if the user enters the <c>close</c> command and hits enter.</para></param>
        public void Open(Keys? closeKey)
        {
            if (!isOpen && !isOpening && !isClosing)
            {
                if (Opening != null)
                {
                    CancelEventArgs cancelArgs = new CancelEventArgs();
                    Opening(this, cancelArgs);
                    if (cancelArgs.Cancel)
                    {
                        return;
                    }
                }

                this.closeKey = closeKey;

                initializeAnimation(openingAnimation, openingAnimationTime);

                Enabled = Visible = true;
                isOpening = true;
                isClosing = false;
            }
        }

        /// <summary>
        /// Closes the game console.
        /// </summary>
        public void Close()
        {
            if (isOpen && !isClosing)
            {
                if (Closing != null)
                {
                    CancelEventArgs cancelArgs = new CancelEventArgs();
                    Closing(this, cancelArgs);
                    if (!cancelArgs.Cancel)
                    {
                        initializeAnimation(closingAnimation, closingAnimationTime);
                        isClosing = true;
                    }
                }
                else
                {
                    initializeAnimation(closingAnimation, closingAnimationTime);
                    isClosing = true;
                }
            }
        }

        /// <summary>
        /// <para>Executes a game console command.</para>
        /// <para>This method will also be called if the user enters a command in the input field and hits enter.</para>
        /// </summary>
        /// <param name="input">The command to be executed.</param>
        /// <param name="addToLog">True, if the input should be added to the log.</param>
        /// <returns>Returns true if the execution was successful, otherwise false.</returns>
        public bool Execute(string input, bool addToLog)
        {
            input = input.Trim();
            string[] splitInput = input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (commands.ContainsKey(splitInput[0]))
            {
                try
                {
                    Command command = commands[splitInput[0]];
                    bool addToHistory = command.AddToHistory;

                    if (command.AddToLog && addToLog)
                    {
                        Log(Prefix + input);
                    }

                    string[] args = new string[splitInput.Length - 1];
                    Array.Copy(splitInput, 1, args, 0, args.Length);

                    command.Handler(this, new CommandEventArgs(command, args));

                    if (command.AddToHistory && (inputHistory.Count == 0 || inputHistory.Last() != input))
                    {
                        inputHistory.Add(input);
                    }
                    inputHistoryPosition = inputHistory.Count;

                    command.AddToHistory = addToHistory;

                    return true;
                }
                catch (IndexOutOfRangeException)
                {
                    inputHistory.Add(input);
                    inputHistoryPosition = inputHistory.Count;
                    if (addToLog)
                    {
                        Log(Prefix + input);
                        if (reportOnError)
                        {
                            Log("Error: The argument count doesn't match the command.", 1);
                        }
                    }
                    return false;
                }
                catch (FormatException)
                {
                    inputHistory.Add(input);
                    inputHistoryPosition = inputHistory.Count;
                    if (addToLog)
                    {
                        Log(Prefix + input);
                        if (reportOnError)
                        {
                            Log("Error: One or more of the arguments have the wrong type.", 1);
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    inputHistory.Add(input);
                    inputHistoryPosition = inputHistory.Count;
                    if (addToLog)
                    {
                        Log(Prefix + input);
                        if (reportOnError)
                        {
                            Log("Error: " + ex.Message, 1);
                        }
                    }
                    return false;
                }
            }
            else
            {
                inputHistory.Add(input);
                inputHistoryPosition = inputHistory.Count;
                if (addToLog)
                {
                    Log(Prefix + input);
                    if (reportOnError)
                    {
                        Log("Command '" + splitInput[0] + "' not found.", 1);
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// <para>Executes a game console command.</para>
        /// <para>This method will also be called if the user enters a command in the input field and hits enter.</para>
        /// </summary>
        /// <param name="input">The command to be executed.</param>
        /// <returns>Returns true if the execution was successful, otherwise false.</returns>
        public bool Execute(string input)
        {
            return Execute(input, true);
        }

        /// <summary>
        /// Adds an log entry to the log.
        /// </summary>
        /// <param name="entry">The new log entry.</param>
        public void Log(LogEntry entry)
        {
            if (entry.Level == 255 && !logDebugMessages)
            {
                return;
            }

            if (maxLogEntries > 0 && log.Count >= maxLogEntries)
            {
                log.RemoveAt(0);
            }
            log.Add(entry);

            if (isOpen || isOpening)
            {
                updateView(autoScroll);
            }

            if (LogEntryAdded != null)
            {
                LogEntryAdded(this, new LogEventArgs(entry));
            }
        }

        /// <summary>
        /// Adds a log message to the log.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public void Log(string message, byte level)
        {
            Log(new LogEntry(message, level));
        }

        /// <summary>
        /// Adds a log message with the default log level 0 to the log.
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Log(string message)
        {
            Log(new LogEntry(message));
        }

        /// <summary>
        /// Clears the entire log.
        /// </summary>
        public void Clear()
        {
            int count = log.Count;
            log.Clear();
            currentLine = 0;
            updateView(false);
            Log("Log cleared, " + count.ToString() + " log entries deleted.", 255);
        }

        /// <summary>
        /// Clears the entire input history.
        /// </summary>
        public void ClearHistory()
        {
            int count = inputHistory.Count;
            inputHistory.Clear();
            Log("History cleared, " + count.ToString() + " history entries deleted.", 255);
        }

        /// <summary>
        /// Removes all commands.
        /// </summary>
        public void ClearCommands()
        {
            int count = commands.Count;
            commands.Clear();
            Log("Commands cleared, " + count.ToString() + " commands deleted.", 255);
        }

        /// <summary>
        /// Gets the log level visibility.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>Returns true if the log level is visible, otherwise false.</returns>
        public bool GetLogLevelVisibility(byte level)
        {
            return logLevelVisibility.ContainsKey(level) ? logLevelVisibility[level] : true;
        }

        /// <summary>
        /// Sets the log level visibility.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="visible">True, if the log level should be visible, otherwise false.</param>
        public void SetLogLevelVisibility(byte level, bool visible)
        {
            if (visible)
            {
                logLevelVisibility.Remove(level);
            }
            else if (!logLevelVisibility.ContainsKey(level))
            {
                logLevelVisibility.Add(level, false);
            }
            currentLine = 0;
        }

        /// <summary>
        /// Gets the log level text color.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>Returns the log level text color.</returns>
        public Color GetLogLevelColor(byte level)
        {
            return logLevelColors.ContainsKey(level) ? logLevelColors[level] : logDefaultColor;
        }

        /// <summary>
        /// Sets the log level text color.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="color">The color for the log level text.</param>
        public void SetLogLevelColor(byte level, Color color)
        {
            if (logLevelColors.ContainsKey(level))
            {
                logLevelColors[level] = color;
            }
            else
            {
                logLevelColors.Add(level, color);
            }
        }

        /// <summary>
        /// <para>Updates the view of the log and displays only the last entries.</para>
        /// <para>The view will automatically scroll down, if <c>AutoScroll</c> is set to true.</para>
        /// </summary>
        public void ScrollDown()
        {
            updateView(true);
        }

        /// <summary>
        /// Executes "man" for the string provided.
        /// </summary>
        /// <param name="input"></param>
        public void ExecManual(string input)
        {
            Execute("man " + input, true);
        }
        #endregion
    }

    #region Enum GameConsoleAnimation

    /// <summary>
    /// Defines the game console animation types.
    /// </summary>
    [Flags]
    public enum GameConsoleAnimation
    {
        /// <summary>
        /// No animation.
        /// </summary>
        None = 0x1,

        /// <summary>
        /// Fade in/out animation.
        /// </summary>
        Fade = 0x2,
        /// <summary>
        /// Slide from/to top animation.
        /// </summary>
        SlideTop = 0x4,
        /// <summary>
        /// Slide from/to bottom animation.
        /// </summary>
        SlideBottom = 0x8,
        /// <summary>
        /// Slide from/to left animation.
        /// </summary>
        SlideLeft = 0x10,
        /// <summary>
        /// Slide from/to right animation.
        /// </summary>
        SlideRight = 0x20,

        /// <summary>
        /// Fade in/out and slide from/to top animation.
        /// </summary>
        FadeSlideTop = Fade | SlideTop,
        /// <summary>
        /// Fade in/out and slide from/to bottom animation.
        /// </summary>
        FadeSlideBottom = Fade | SlideBottom,
        /// <summary>
        /// Fade in/out and slide from/to left animation.
        /// </summary>
        FadeSlideLeft = Fade | SlideLeft,
        /// <summary>
        /// Fade in/out and slide from/to right animation.
        /// </summary>
        FadeSlideRight = Fade | SlideRight
    }

    #endregion
}
