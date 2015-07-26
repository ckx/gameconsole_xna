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
using System.Globalization;
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
        private const int DEFAULT_CAPACITY = 1000;
        private const int DEFAULT_LINES = 8;
        private const string NO_DESCRIPTION = "No description found.";

        private readonly ContentManager _content;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font, _defaultFont;
        private Texture2D _blank;

        private Texture2D _backgroundTexture;
        private Color _backgroundColor = new Color(0, 0, 0, 192);

        private Rectangle _bounds, _boundsWindow;

        private Vector2 _fontSize;
        private int _maxVisibleLines, _charsPerLine;

        private float _padding = 4.0f;
        private Vector2 _textOrigin;

        private readonly List<LogEntry> _log;
        private IEnumerable<LogEntry> _logSelected;
        private int _maxLogEntries;

        private readonly Dictionary<byte, bool> _logLevelVisibility;
        private readonly Dictionary<byte, Color> _logLevelColors;

        private bool _autoScroll = true;
        private bool _inputEnabled = true;

        private string _prefix = "> ";
        private string _input = "";

        private readonly List<string> _inputHistory;

        private int _visibleLines, _currentLine;
        private int _cursorPosition, _inputHistoryPosition;

        private bool _isFullscreen;

        //KeyMap keyMap;
        private Keys? _closeKey;
        private KeyboardState _currentKeyboardState = new KeyboardState(), _lastKeyboardState;

        private float _openingAnimationTime = 0.5f;
        private float _closingAnimationTime = 0.5f;
        private float _animationTime, _animationPercentage;
        private int _animationPosition;

        private float _cursorBlinkTime = 1.0f, _currentCursorBlinkTime;

        private MouseState _currentMouseState;
        private int _oldScrollWheelValue;

        private string _pasteResult = "";

        private bool _mashTab;
        #endregion

        #region Properties
        /// <summary>
        /// <para>Gets or sets the SpriteFont of the game console.</para>
        /// <para>This should be a console font where all character have the same width,
        /// otherwise the cursor and some other functions may not work properly.</para>
        /// <para>The default internal font will be used if this is set to null (default).</para>
        /// </summary>
        public SpriteFont Font {
            get { return _font; }
            set {
                if (value != null) {
                    _font = value;
                    CalculateFontSize();
                    CalculateTextArea();
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the background texture of the game console.</para>
        /// <para>The texture will always be sized to the bounds of the game console.</para>
        /// <para>If this is set to null, only the background color will be used (default).</para>
        /// </summary>
        public Texture2D BackgroundTexture {
            get { return _backgroundTexture; }
            set { _backgroundTexture = value ?? _blank; }
        }

        /// <summary>
        /// <para>Gets or sets the background color of the game console.</para>
        /// <para>If the background texture is set, this will be used as an color overlay
        /// for the texture. If the texture is set to null, only this color will be used.</para>
        /// <para>The default background color is black.</para>
        /// </summary>
        public Color BackgroundColor {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }

        /// <summary>
        /// <para>The default text color for the log entries
        /// if no other color is set with the SetLogLevelColor method.</para>
        /// <para>The default color for the log text is light gray.</para>
        /// </summary>
        public Color LogDefaultColor { get; set; }

        /// <summary>
        /// <para>The text color for the input field.</para>
        /// <para>The default input text color is white.</para>
        /// </summary>
        public Color InputColor { get; set; }

        /// <summary>
        /// Gets or sets the bounds of the game console, including the position and size (in pixels).
        /// </summary>
        public Rectangle Bounds {
            get { return _bounds; }
            set {
                if (value != Rectangle.Empty && value.Height - _padding * 2.0f >= _fontSize.Y &&
                    value.Width > _fontSize.X) {
                    _bounds = value;
                    CalculateTextArea();
                    _isFullscreen = false;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the padding between the text inside the game console and the edges of the bounds (in pixels).</para>
        /// <para>The default padding is 4.0 pixels.</para>
        /// </summary>
        public float Padding {
            get { return _padding; }
            set {
                if (value >= 0.0f) {
                    _padding = value;
                    CalculateTextArea();
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the maximum log entries that can be stored to the log.</para>
        /// <para>If there will be more log entries added then this value, the last entry will drop out of the log.</para>
        /// <para>If this is set to 0 (default), there is no limit for the log entry count.</para>
        /// </summary>
        public int MaxLogEntries {
            get { return _maxLogEntries; }
            set {
                _maxLogEntries = value > 0 ? value : 0;
                if (_maxLogEntries == 0) {
                    _log.Capacity = DEFAULT_CAPACITY;
                }
                else {
                    if (_log.Count > _maxLogEntries) {
                        _log.RemoveRange(0, _log.Count - _maxLogEntries);
                        _currentLine = 0;
                        UpdateView(false);
                    }
                    _log.Capacity = _maxLogEntries;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets if the game console should always scroll down if a new entry is added to the log.</para>
        /// <para>The default value is true, so the log will automatically scroll down.</para>
        /// </summary>
        public bool AutoScroll {
            get { return _autoScroll; }
            set {
                _autoScroll = value;
                if (_autoScroll) {
                    UpdateView(true);
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets if the input field is enabled.</para>
        /// <para>The default value is true.</para>
        /// </summary>
        public bool InputEnabled {
            get { return _inputEnabled; }
            set {
                _inputEnabled = value;
                _currentCursorBlinkTime = 0.0f;
                CalculateTextArea();
            }
        }

        /// <summary>
        /// <para>Gets or sets the prefix left to the input field.</para>
        /// <para>The default prefix is <c>"> "</c>.</para>
        /// </summary>
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }

        /// <summary>
        /// Gets or sets the text of the input field.
        /// </summary>
        public string Input {
            get { return _input; }
            set {
                _input = value;
                if (_cursorPosition > _input.Length) {
                    _cursorPosition = _input.Length;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the button margin of the cursor (in pixels).</para>
        /// <para>The default value is 0 pixels.</para>
        /// </summary>
        public float CursorBottomMargin { get; set; }

        /// <summary>
        /// Gets the count of all log entries.
        /// </summary>
        public int Count {
            get { return _log.Count; }
        }

        /// <summary>
        /// Gets the count of all visible log entries.
        /// </summary>
        public int CountVisible { get; private set; }

        /// <summary>
        /// <para>Gets or sets the visible text lines of the game console, including the input field (if enabled).</para>
        /// <para>Default there are 8 lines visible.</para>
        /// </summary>
        public int Lines {
            get { return (int)((_bounds.Height - _padding * 2.0f) / _fontSize.Y); }
            set {
                if (value > 0) {
                    Bounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width,
                        (int)(_fontSize.Y * value + _padding * 2.0f));
                }
            }
        }

        /// <summary>
        /// Gets or sets the cursor position.
        /// </summary>
        public int CursorPosition {
            get { return _cursorPosition; }
            set { _cursorPosition = (int)MathHelper.Clamp(value, 0, _input.Length); }
        }

        /// <summary>
        /// Gets if the game console is open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Gets if the game console is opening.
        /// </summary>
        public bool IsOpening { get; private set; }

        /// <summary>
        /// Gets if the game console is closing.
        /// </summary>
        public bool IsClosing { get; private set; }

        /// <summary>
        /// Gets or sets if the log time is visible (default true).
        /// </summary>
        public bool ShowLogTime { get; set; }

        /// <summary>
        /// Gets or sets if the log level is visible (default true).
        /// </summary>
        public bool ShowLogLevel { get; set; }

        /// <summary>
        /// Gets or sets if the game console is running in fullscreen (default false).
        /// </summary>
        public bool IsFullscreen {
            get { return _isFullscreen; }
            set {
                if (value) {
                    _boundsWindow = _bounds;
                    Bounds = new Rectangle(0, 0, GraphicsDevice.Viewport.TitleSafeArea.Width,
                        GraphicsDevice.Viewport.TitleSafeArea.Height);
                }
                else {
                    Bounds = _boundsWindow;
                }
                _isFullscreen = value;
            }
        }

        /// <summary>
        /// Gets or sets if any error messages should be logged.
        /// </summary>
        public bool ReportOnError { get; set; }

        /// <summary>
        /// Gets or sets if the debug messages (log level 255) should be added to the log (default false).
        /// </summary>
        public bool LogDebugMessages { get; set; }

        /// <summary>
        /// <para>Gets or sets the opening animation of the game console.</para>
        /// <para>The default opening animation is set to fade and slide from the top.</para>
        /// </summary>
        public GameConsoleAnimation OpeningAnimation { get; set; }

        /// <summary>
        /// <para>Gets or sets the closing animation of the game console.</para>
        /// <para>The default closing animation is set to fade and slide to the top.</para>
        /// </summary>
        public GameConsoleAnimation ClosingAnimation { get; set; }

        /// <summary>
        /// <para>Gets or sets the time for the opening animation of the game console (in seconds).</para>
        /// <para>The default opening animation time is set to 0.5 seconds.</para>
        /// </summary>
        public float OpeningAnimationTime {
            get { return _openingAnimationTime; }
            set { _openingAnimationTime = value > 0.0f ? value : 0.5f; }
        }

        /// <summary>
        /// <para>Gets or sets the time for the closing animation (in seconds).</para>
        /// <para>The default closing animation time is set to 0.5 seconds.</para>
        /// </summary>
        public float ClosingAnimationTime {
            get { return _closingAnimationTime; }
            set { _closingAnimationTime = value > 0.0f ? value : 0.5f; }
        }

        /// <summary>
        /// <para>Gets or sets the blinking time period for the cursor (in seconds).</para>
        /// <para>If this value is set to 0.0 seconds the cursor is always visible.</para>
        /// <para>The default blinking time period is set to 1.0 seconds.</para>
        /// </summary>
        public float CursorBlinkTime {
            get { return _cursorBlinkTime; }
            set {
                if (value >= 0.0f) {
                    _cursorBlinkTime = value;
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// The dictionary to store all the commands and command event handlers.
        /// </summary>
        private readonly Dictionary<string, Command> _commands;

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
            LogDefaultColor = Color.LightGray;
            InputColor = Color.White;
            ClosingAnimation = GameConsoleAnimation.FadeSlideTop;
            OpeningAnimation = GameConsoleAnimation.FadeSlideTop;
            ReportOnError = true;
            ShowLogLevel = false;
            ShowLogTime = false;
            Game.Components.Add(this);
            _content = new ResourceContentManager(Game.Services, GameConsoleContent.ResourceManager);

            _log = new List<LogEntry>(DEFAULT_CAPACITY);
            _commands = new Dictionary<string, Command>();
            _inputHistory = new List<string>();

            _logLevelVisibility = new Dictionary<byte, bool>();
            _logLevelColors = new Dictionary<byte, Color>();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

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
            this._bounds = bounds ?? Rectangle.Empty;
            this._backgroundColor = backgroundColor;
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
            this._backgroundTexture = backgroundTexture;
        }

        /// <summary>
        /// Initialize the game console and adds some internal commands to the command dictionary.
        /// </summary>
        public override void Initialize()
        {
            EventInput.Initialize(Game.Window);
            EventInput.CharEntered += OnCharEntered;
            EventInput.KeyDown += OnKeyDown;

            _currentMouseState = new MouseState();

            //AddCommand("con_help", delegate(object sender, CommandEventArgs e)
            //{
            //    Log("help is coming... soon! ;)");
            //}, "Shows the help.");

            AddCommand("con_info", delegate {
                Log(string.Format("Log entries visible: {0}/{1}", CountVisible + 1, _log.Count));
                Log("History entries: " + _inputHistory.Count);
                Log("Commands registered: " + _commands.Count);
            }, "Shows some internal console informations.");

            AddCommand("commands", delegate(object sender, CommandEventArgs e) {
                IEnumerable<Command> commandsSelected;
                if (e.Args.Length > 0) {
                    commandsSelected = from c in _commands.Values
                        where c.Name.Contains(e.Args[0])
                        orderby c.Name
                        select c;
                    if (commandsSelected.Any()) {
                        Log("List of " + commandsSelected.Count() + " appropriate commands for '" +
                            e.Args[0] + "':");
                    }
                    else {
                        Log("No appropriate commands found for '" + e.Args[0] + "'.");
                        //e.Command.AddToHistory = false;
                    }
                }
                else {
                    commandsSelected = from c in _commands.Values orderby c.Name select c;
                    Log("List of all " + _commands.Count + " commands:");
                }
                int i = 0;
                foreach (Command command in commandsSelected) {
                    Log(string.Format("{0:000}: {1} => {2}", ++i, command.Name, command.Manual[0]));
                }
            }, "Lists all currently registered commands with their first description line.",
                "commands <part of the command> - lists only the appropriate commands.");

            AddCommand("man", delegate(object sender, CommandEventArgs e) {
                if (e.Args.Length > 0) {
                    if (_commands.ContainsKey(e.Args[0])) {
                        Log("Manual for the command '" + e.Args[0] + "':");
                        foreach (string descriptionLine in _commands[e.Args[0]].Manual) {
                            Log(descriptionLine);
                        }
                    }
                    else {
                        Log("Command '" + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else {
                    ExecManual("man");
                }
            }, "Displays the manual of the provided command.", "man <command>");

            AddCommand("history", delegate(object sender, CommandEventArgs e) {
                int endIndex = 0;
                if (e.Args.Length > 0) {
                    int count = int.Parse(e.Args[0]);
                    endIndex = count <= _inputHistory.Count ? _inputHistory.Count - count : 0;
                }
                Log("There are " + _inputHistory.Count + " entries in the input history.");
                for (int i = _inputHistory.Count - 1; i >= endIndex; i--) {
                    Log(string.Format("{0:000}: {1}", _inputHistory.Count - i, _inputHistory[i]));
                }
            }, "Lists the input history.",
                "history <number> - lists only the last <number> entries.");

            AddCommand("!", delegate(object sender, CommandEventArgs e) {
                int exeIndex = _inputHistory.Count - 1;
                if (e.Args.Length > 0) {
                    int number = int.Parse(e.Args[0]);
                    exeIndex -= number;
                    if (exeIndex < 0 || exeIndex > _inputHistory.Count - 1) {
                        return;
                    }
                }
                if (_inputHistory.Count > 0) {
                    Execute(_inputHistory[exeIndex]);
                }
            }, false, false, "Executes the last command again.",
                "! <number> - executes the specified command form the input history again.");

            AddCommand("close", delegate { Close(); }, false, false, "Closes the console.");

            AddCommand("exit", delegate { Game.Exit(); }, false, false, "Exit the game.");

            AddCommand("clear", delegate(object sender, CommandEventArgs e) {
                if (e.Args.Length > 0) {
                    if (e.Args[0] == "history") {
                        ClearHistory();
                    }
                    else if (e.Args[0] == "commands") {
                        ClearCommands();
                    }
                    else {
                        Log("Command 'clear " + e.Args[0] + "' not found.", 1);
                    }
                }
                else {
                    Clear();
                }
            }, "Clears the console log, the input history or the command list.",
                "clear - clears the log.", "clear history - clears the input history.",
                "clear commands - clears all registered commands.");

            AddCommand("con_set", delegate(object sender, CommandEventArgs e) {
                if (e.Args.Length > 0) {
                    if (e.Args[0] == "color" && e.Args.Length > 1) {
                        if (e.Args[1] == "background") {
                            BackgroundColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]),
                                byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "log") {
                            LogDefaultColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]),
                                byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "input") {
                            InputColor = new Color(byte.Parse(e.Args[2]), byte.Parse(e.Args[3]),
                                byte.Parse(e.Args[4]));
                        }
                        else if (e.Args[1] == "level") {
                            SetLogLevelColor(byte.Parse(e.Args[2]),
                                new Color(byte.Parse(e.Args[3]), byte.Parse(e.Args[4]),
                                    byte.Parse(e.Args[5])));
                        }
                        else {
                            Log("Command 'con_set color " + e.Args[1] + "' not found.", 1);
                            //e.Command.AddToHistory = false;
                        }
                    }
                    else if (e.Args[0] == "alpha" && e.Args.Length > 1) {
                        if (e.Args[1] == "background") {
                            _backgroundColor.A = byte.Parse(e.Args[2]);
                        }
                        else {
                            Log("Command 'con_set alpha " + e.Args[1] + "' not found.", 1);
                        }
                    }
                    else if (e.Args[0] == "bounds") {
                        Bounds = new Rectangle(int.Parse(e.Args[1]), int.Parse(e.Args[2]),
                            int.Parse(e.Args[3]), int.Parse(e.Args[4]));
                    }
                    else if (e.Args[0] == "padding") {
                        Padding = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "margin") {
                        CursorBottomMargin = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "blink") {
                        CursorBlinkTime = float.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "prefix") {
                        Prefix = e.Args[1] + " ";
                    }
                    else if (e.Args[0] == "lines") {
                        Lines = int.Parse(e.Args[1]);
                    }
                    else if (e.Args[0] == "timeformat") {
                        LogEntry.TimeFormat = e.Args[1];
                    }
                    else {
                        Log("Command 'con_set " + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else {
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

            AddCommand("con_tog", delegate(object sender, CommandEventArgs e) {
                if (e.Args.Length > 0) {
                    if (e.Args[0] == "time") {
                        ShowLogTime = !ShowLogTime;
                        Log("ShowLogTime = " + ShowLogTime, 255);
                    }
                    else if (e.Args[0] == "level") {
                        if (e.Args.Length > 1) {
                            byte logLevel = byte.Parse(e.Args[1]);
                            bool logLevelInvisible = !GetLogLevelVisibility(logLevel);
                            SetLogLevelVisibility(logLevel, logLevelInvisible);
                            Log("LogLevelVisibility[" + logLevel + "] = " + logLevelInvisible, 255);
                        }
                        else {
                            ShowLogLevel = !ShowLogLevel;
                            Log("ShowLogLevel = " + ShowLogLevel, 255);
                        }
                    }
                    else if (e.Args[0] == "autoscroll") {
                        AutoScroll = !AutoScroll;
                        Log("AutoScroll = " + AutoScroll, 255);
                    }
                    else if (e.Args[0] == "fullscreen") {
                        IsFullscreen = !IsFullscreen;
                        Log("IsFullscreen = " + IsFullscreen, 255);
                    }
                    else {
                        Log("Command 'con_toggle " + e.Args[0] + "' not found.", 1);
                        //e.Command.AddToHistory = false;
                    }
                }
                else {
                    ExecManual("con_tog");
                }
            }, "Toggles boolean properties of the console.",
                "con_toggle time - toggles the visibility of the log timestamp.",
                "con_toggle level - toggles the visibility of the log level.",
                "con_toggle level <byte> - toggles the provided log level visibility.",
                "con_toggle autoscroll - toggles the auto scrolling for the log.",
                "con_toggle fullscreen - toggles the fullscreen mode of the console.");

            AddCommand("con_save", delegate(object sender, CommandEventArgs e) {
                if (e.Args.Length > 0) {
                    StreamWriter file = File.CreateText(e.Args[0]);
                    foreach (LogEntry entry in _log) {
                        file.WriteLine(entry.ToString());
                    }
                    file.Close();
                    Log("Log successfully saved: " + e.Args[0]);
                }
                else {
                    StreamWriter file = File.CreateText("con_log");
                    foreach (LogEntry entry in _log) {
                        file.WriteLine(entry.ToString());
                    }
                    file.Close();
                    Log("Log successfully saved: " + "con_log");
                }
            }, "Saves the entire log to the provided file.",
                "con_save <path> - the path can be relative or absolute.",
                "If no path is supplied it saves to a file named con_log.");

            UpdateView(false);

            Log("Console initialized.", 255);

            base.Initialize();

            if (Initialized != null) {
                Initialized(this, new EventArgs());
            }
        }

        /// <summary>
        /// Loads the default font and blank texture for the game console
        /// and sets the default bounds and background texture if necessary.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _defaultFont = _content.Load<SpriteFont>("DevConsoleFont");
            _blank = new Texture2D(GraphicsDevice, 1, 1);
            _blank.SetData(new[] {Color.White});

            _font = _defaultFont;
            CalculateFontSize();

            if (_bounds == Rectangle.Empty) {
                _bounds = new Rectangle(0, 0, Game.GraphicsDevice.Viewport.TitleSafeArea.Width,
                    Game.GraphicsDevice.Viewport.TitleSafeArea.Height);
                Lines = DEFAULT_LINES;
            }

            if (_backgroundTexture == null) {
                _backgroundTexture = _blank;
            }

            base.LoadContent();
        }
        #endregion

        #region Private Methods
        [STAThread]
        private void PasteThread()
        {
            if (Clipboard.ContainsText()) {
                _pasteResult = Clipboard.GetText();
            }
            else {
                _pasteResult = "";
            }
        }

        private void OnCharEntered(CharacterEventArgs e)
        {
            if (char.IsControl(e.Character)) {
                //ctrl-v
                if (e.Character == 0x16) {
                    //XNA runs in Multiple Thread Apartment state, which cannot receive clipboard
                    Thread thread = new Thread(PasteThread);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    ReceiveTextInput(_pasteResult);
                }
                else {
                    ReceiveCommandInput(e.Character);
                }
            }
            else {
                ReceiveTextInput(e.Character);
            }
        }

        private void OnKeyDown(KeyEventArgs e)
        {
            ReceiveKeyCodeInput(e.KeyCode);
        }

        private void ReceiveCommandInput(char inputChar)
        {
            if (IsOpen && _inputEnabled) {
                if (inputChar == '\r') {
                    if (InputEntered != null) {
                        InputEventArgs inputEventArgs = new InputEventArgs(_input);
                        InputEntered(this, inputEventArgs);
                        if (inputEventArgs.Execute) {
                            Execute(_input, inputEventArgs.AddToLog);
                        }
                    }
                    else {
                        Execute(_input);
                    }

                    _input = "";
                    _cursorPosition = 0;

                    if (InputChanged != null) {
                        InputChanged(this, new InputEventArgs(_input));
                    }
                }
                else if (inputChar == '\b' && _cursorPosition > 0) {
                    _input = _input.Remove(--_cursorPosition, 1);

                    if (InputChanged != null) {
                        InputChanged(this, new InputEventArgs(_input));
                    }
                }
                else if (inputChar == '\t') // AutoComplete extension code
                {
                    IEnumerable<string> autoCollection = from c in _commands.Keys
                        where c.StartsWith(_input)
                        orderby c
                        select c;

                    if (autoCollection.Count() != 0) {
                        _cursorPosition +=
                            autoCollection.FirstOrDefault()
                                .Zip(autoCollection.LastOrDefault(), (c1, c2) => c1 == c2)
                                .TakeWhile(b => b)
                                .Count() - _input.Length;
                        int index =
                            autoCollection.FirstOrDefault()
                                .Zip(autoCollection.LastOrDefault(), (c1, c2) => c1 == c2)
                                .TakeWhile(b => b)
                                .Count();
                        _input = autoCollection.FirstOrDefault().Substring(0, index);
                        if (autoCollection.Count() > 1) {
                            if (!_mashTab) {
                                _mashTab = true;
                                Log(Prefix + _input);
                                foreach (string s in autoCollection) {
                                    Log(" -> " + s);
                                }
                            }
                            else if (_mashTab) {
                                if (_input == autoCollection.ElementAt(0)) {
                                    _cursorPosition += autoCollection.ElementAt(1).Length -
                                                      _input.Length;
                                    _input = autoCollection.ElementAt(1);
                                }
                                else {
                                    _cursorPosition += autoCollection.ElementAt(0).Length -
                                                      _input.Length;
                                    _input = autoCollection.ElementAt(0);
                                }
                            }
                        }
                    }
                }
                else {
                    _mashTab = false;
                }
            }
        }

        private void ReceiveTextInput(string inputText)
        {
            if (IsOpen && _inputEnabled) {
                _input += inputText;
                _cursorPosition += inputText.Length;
            }
            _mashTab = false;
        }

        private void ReceiveTextInput(char inputChar)
        {
            if (IsOpen && _inputEnabled) {
                if (inputChar == '\0') {
                    return;
                }

                if (_cursorPosition == _input.Length) {
                    _input += inputChar;
                }
                else {
                    _input = _input.Insert(_cursorPosition, inputChar.ToString());
                }

                _cursorPosition++;
                _inputHistoryPosition = _inputHistory.Count;

                if (InputChanged != null) {
                    InputChanged(this, new InputEventArgs(_input));
                }
            }
            _mashTab = false;
        }

        private void ReceiveKeyCodeInput(Keys inputKey)
        {
            if (IsOpen) {
                if (_closeKey != null && inputKey == _closeKey) {
                    Close();
                }
                else if (inputKey == Keys.PageUp && _currentLine > 0) {
                    _currentLine -= _maxVisibleLines;
                    if (_currentLine < 0) {
                        _currentLine = 0;
                    }
                    UpdateView(false);
                }
                else if (inputKey == Keys.PageDown && _currentLine + _maxVisibleLines < CountVisible) {
                    _currentLine += _maxVisibleLines;
                    if (_currentLine > CountVisible - _maxVisibleLines) {
                        _currentLine = CountVisible - _maxVisibleLines;
                    }
                    UpdateView(false);
                }
                else if (inputKey == Keys.Delete && _cursorPosition < _input.Length) {
                    _input = _input.Remove(_cursorPosition, 1);

                    if (InputChanged != null) {
                        InputChanged(this, new InputEventArgs(_input));
                    }
                }
                else if (inputKey == Keys.Left && _cursorPosition > 0) {
                    _cursorPosition--;
                }
                else if (inputKey == Keys.Right && _cursorPosition < _input.Length) {
                    _cursorPosition++;
                }
                else if (inputKey == Keys.Home) {
                    _cursorPosition = 0;
                }
                else if (inputKey == Keys.End) {
                    _cursorPosition = _input.Length;
                }
                else if (inputKey == Keys.Up && _inputHistory.Count > 0 &&
                         _inputHistoryPosition > 0) {
                    _input = _inputHistory[--_inputHistoryPosition];
                    _cursorPosition = _input.Length;

                    if (InputChanged != null) {
                        InputChanged(this, new InputEventArgs(_input));
                    }
                }
                else if (inputKey == Keys.Down &&
                         _inputHistoryPosition < _inputHistory.Count - 1) {
                    _input = _inputHistory[++_inputHistoryPosition];
                    _cursorPosition = _input.Length;

                    if (InputChanged != null) {
                        InputChanged(this, new InputEventArgs(_input));
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the the font size for each character. Because the font should be a console
        /// font where all characters have the same width, this is not very complicated.
        /// </summary>
        private void CalculateFontSize()
        {
            _fontSize = new Vector2(_font.MeasureString("X").X, _font.MeasureString("Xy").Y);
        }

        /// <summary>
        /// Claculates the text area based on the font size and the bounding box.
        /// </summary>
        private void CalculateTextArea()
        {
            _maxVisibleLines = (int)((_bounds.Height - _padding * 2.0f) / _fontSize.Y);
            if (_inputEnabled) {
                _maxVisibleLines--;
            }
            _charsPerLine = (int)((_bounds.Width - _padding * 2.0f) / _fontSize.X);
            _textOrigin = new Vector2(_bounds.X + _padding, _bounds.Y + _padding);

            UpdateView(_autoScroll);
        }

        /// <summary>
        /// Updates the view of the current log entries, what log entries the user will see on the screen.
        /// </summary>
        /// <param name="scrollDown">True if the log should scroll down to the last entry, otherwise false.</param>
        private void UpdateView(bool scrollDown)
        {
            if (_logLevelVisibility.Count > 0) {
                _logSelected =
                    _log.Where(
                        entry =>
                            _logLevelVisibility.ContainsKey(entry.Level)
                                ? _logLevelVisibility[entry.Level]
                                : true);
                CountVisible = _logSelected.Count();
            }
            else {
                _logSelected = _log;
                CountVisible = _log.Count;
            }

            if (scrollDown) {
                _currentLine = CountVisible - _maxVisibleLines;
                if (_currentLine < 0) {
                    _currentLine = 0;
                }
            }

            _logSelected = _logSelected.Skip(_currentLine).Take(_maxVisibleLines);
            _visibleLines = CountVisible > _maxVisibleLines ? _maxVisibleLines : CountVisible;
        }

        /// <summary>
        /// A little helper method to determine if the pressed key was newly pressed in this frame.
        /// </summary>
        /// <param name="key">The pressed key.</param>
        /// <returns>Returns true if the key was newly pressed, otherwise false.</returns>
        private bool IsNewKeyPress(Keys key)
        {
            return (_currentKeyboardState.IsKeyDown(key) && _lastKeyboardState.IsKeyUp(key));
        }

        /// <summary>
        /// Initialize the animation, sets the appropriate values depending on the animation type.
        /// </summary>
        /// <param name="animation">The animation type.</param>
        /// <param name="time">How long the animation takes.</param>
        private void InitializeAnimation(GameConsoleAnimation animation, float time)
        {
            if (animation == GameConsoleAnimation.None) {
                _animationTime = 0.0f;
                return;
            }

            if ((animation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade) {
                //animationBackgroundAlpha = backgroundAlpha;
                //animationTextAlpha = textAlpha;
            }

            if ((animation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop ||
                (animation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom) {
                _animationPosition = _bounds.Y;
            }
            else if ((animation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft ||
                     (animation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight) {
                _animationPosition = _bounds.X;
            }

            _animationTime = time;
        }

        /// <summary>
        /// Cleans the animation up, resets the appropriate values depending on the animation type.
        /// </summary>
        /// <param name="animation">The animation type.</param>
        private void AnimationCleanUp(GameConsoleAnimation animation)
        {
            if (animation == GameConsoleAnimation.None) {
                return;
            }

            if ((animation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade) {
                //backgroundAlpha = animationBackgroundAlpha;
                //textAlpha = animationTextAlpha;
            }

            if ((animation & GameConsoleAnimation.SlideTop) == GameConsoleAnimation.SlideTop ||
                (animation & GameConsoleAnimation.SlideBottom) == GameConsoleAnimation.SlideBottom) {
                Bounds = new Rectangle(_bounds.X, _animationPosition, _bounds.Width, _bounds.Height);
            }
            else if ((animation & GameConsoleAnimation.SlideLeft) == GameConsoleAnimation.SlideLeft ||
                     (animation & GameConsoleAnimation.SlideRight) == GameConsoleAnimation.SlideRight) {
                Bounds = new Rectangle(_animationPosition, _bounds.Y, _bounds.Width, _bounds.Height);
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
            _currentMouseState = Mouse.GetState();

            if (IsOpening) {
                _animationTime -= elapsedTime;
                if (_animationTime <= 0.0f) {
                    AnimationCleanUp(OpeningAnimation);

                    IsOpening = false;
                    IsOpen = true;
                    if (Opened != null) {
                        Opened(this, new EventArgs());
                    }
                    Log("Console opened.", 255);
                }
                else {
                    _animationPercentage = _animationTime / _openingAnimationTime;
                    if ((OpeningAnimation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade) {
                        //backgroundAlpha = (1.0f - animationPercentage) * animationBackgroundAlpha;
                        //textAlpha = (1.0f - animationPercentage) * animationTextAlpha;
                    }
                    if ((OpeningAnimation & GameConsoleAnimation.SlideTop) ==
                        GameConsoleAnimation.SlideTop) {
                        Bounds = new Rectangle(_bounds.X,
                            (int)(_animationPosition - _animationPercentage * _bounds.Height),
                            _bounds.Width, _bounds.Height);
                    }
                    else if ((OpeningAnimation & GameConsoleAnimation.SlideBottom) ==
                             GameConsoleAnimation.SlideBottom) {
                        Bounds = new Rectangle(_bounds.X,
                            (int)
                                (_animationPosition -
                                 (1.0f - _animationPercentage * _bounds.Height)), _bounds.Width,
                            _bounds.Height);
                    }
                    else if ((OpeningAnimation & GameConsoleAnimation.SlideLeft) ==
                             GameConsoleAnimation.SlideLeft) {
                        Bounds =
                            new Rectangle(
                                (int)
                                    (_animationPosition - _animationPercentage * _bounds.Width),
                                _bounds.Y, _bounds.Width, _bounds.Height);
                    }
                    else if ((OpeningAnimation & GameConsoleAnimation.SlideRight) ==
                             GameConsoleAnimation.SlideRight) {
                        Bounds =
                            new Rectangle(
                                (int)
                                    (_animationPosition -
                                     (1.0f - _animationPercentage * _bounds.Width)),
                                _bounds.Y, _bounds.Width, _bounds.Height);
                    }
                }
            }
            else if (IsClosing) {
                _animationTime -= elapsedTime;
                if (_animationTime <= 0.0f) {
                    AnimationCleanUp(ClosingAnimation);

                    Enabled = Visible = false;
                    IsOpen = IsClosing = false;
                    if (Closed != null) {
                        Closed(this, new EventArgs());
                    }
                    Log("Console closed.", 255);
                }
                else {
                    _animationPercentage = _animationTime / _closingAnimationTime;
                    if ((ClosingAnimation & GameConsoleAnimation.Fade) == GameConsoleAnimation.Fade) {
                        //backgroundAlpha = animationPercentage * animationBackgroundAlpha;
                        //textAlpha = animationPercentage * animationTextAlpha;
                    }
                    if ((ClosingAnimation & GameConsoleAnimation.SlideTop) ==
                        GameConsoleAnimation.SlideTop) {
                        Bounds = new Rectangle(_bounds.X,
                            (int)
                                (_animationPosition - _bounds.Height -
                                 (1.0f - _animationPercentage * _bounds.Height)), _bounds.Width,
                            _bounds.Height);
                    }
                    else if ((ClosingAnimation & GameConsoleAnimation.SlideBottom) ==
                             GameConsoleAnimation.SlideBottom) {
                        Bounds = new Rectangle(_bounds.X,
                            (int)
                                (_animationPosition + _bounds.Height -
                                 _animationPercentage * _bounds.Height), _bounds.Width,
                            _bounds.Height);
                    }
                    else if ((ClosingAnimation & GameConsoleAnimation.SlideLeft) ==
                             GameConsoleAnimation.SlideLeft) {
                        Bounds =
                            new Rectangle(
                                (int)
                                    (_animationPosition - _bounds.Width -
                                     (1.0f - _animationPercentage * _bounds.Width)), _bounds.Y,
                                _bounds.Width, _bounds.Height);
                    }
                    else if ((ClosingAnimation & GameConsoleAnimation.SlideRight) ==
                             GameConsoleAnimation.SlideRight) {
                        Bounds =
                            new Rectangle(
                                (int)
                                    (_animationPosition + _bounds.Width -
                                     _animationPercentage * _bounds.Width), _bounds.Y,
                                _bounds.Width, _bounds.Height);
                    }
                }
            }

            if (InputEnabled && _cursorBlinkTime > 0.0f) {
                _currentCursorBlinkTime += elapsedTime;
                if (_currentCursorBlinkTime >= _cursorBlinkTime) {
                    _currentCursorBlinkTime -= _cursorBlinkTime;
                }
            }

            if (IsOpen) {
                if (_currentMouseState.ScrollWheelValue > _oldScrollWheelValue && _currentLine > 0) {
                    _currentLine -= _maxVisibleLines / 3;
                    if (_currentLine < 0) {
                        _currentLine = 0;
                    }
                    UpdateView(false);
                }
                else if (_currentMouseState.ScrollWheelValue < _oldScrollWheelValue &&
                         _currentLine + _maxVisibleLines < CountVisible) {
                    _currentLine += _maxVisibleLines / 3;
                    if (_currentLine > CountVisible - _maxVisibleLines) {
                        _currentLine = CountVisible - _maxVisibleLines;
                    }
                    UpdateView(false);
                }
            }
            _oldScrollWheelValue = _currentMouseState.ScrollWheelValue;

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game console, the log entry view and input field (if enabled).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            //spriteBatch.Draw(backgroundTexture, bounds, new Color(backgroundColor, backgroundAlpha));
            _spriteBatch.Draw(_backgroundTexture, _bounds, _backgroundColor);

            int i = 0;
            foreach (LogEntry entry in _logSelected) {
                string logEntry = entry.ToString(ShowLogTime, ShowLogLevel);

                //spriteBatch.DrawString(font, logEntry.Length > charsPerLine ? logEntry.Remove(charsPerLine - 2) + ".." : logEntry,
                //    textOrigin + new Vector2(0.0f, fontSize.Y * i++), logLevelColors.ContainsKey(entry.Level) ?
                //    new Color(logLevelColors[entry.Level], textAlpha) : new Color(logDefaultColor, textAlpha));

                _spriteBatch.DrawString(_font,
                    logEntry.Length > _charsPerLine
                        ? logEntry.Remove(_charsPerLine - 2) + ".."
                        : logEntry, _textOrigin + new Vector2(0.0f, _fontSize.Y * i++),
                    _logLevelColors.ContainsKey(entry.Level)
                        ? _logLevelColors[entry.Level]
                        : LogDefaultColor);
            }

            if (_inputEnabled) {
                //spriteBatch.DrawString(font, prefix + input, textOrigin + new Vector2(0.0f, visibleLines * fontSize.Y),
                //    new Color(inputColor, textAlpha));

                _spriteBatch.DrawString(_font, _prefix + _input,
                    _textOrigin + new Vector2(0.0f, _visibleLines * _fontSize.Y), InputColor);

                if (_cursorBlinkTime == 0.0f || _currentCursorBlinkTime < _cursorBlinkTime / 2.0f) {
                    //spriteBatch.Draw(blank, new Rectangle((int)(textOrigin.X + ((prefix.Length + cursorPosition) * fontSize.X)),
                    //    (int)(textOrigin.Y + (visibleLines + 1) * fontSize.Y + cursorBottomMargin),
                    //    (int)fontSize.X, 1), new Color(inputColor, textAlpha));

                    _spriteBatch.Draw(_blank,
                        new Rectangle(
                            (int)(_textOrigin.X + ((_prefix.Length + _cursorPosition) * _fontSize.X)),
                            (int)
                                (_textOrigin.Y + (_visibleLines + 1) * _fontSize.Y + CursorBottomMargin),
                            (int)_fontSize.X, 1), InputColor);
                }
            }

            _spriteBatch.End();

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
        public void AddCommand(string command, CommandHandler handler, bool addToLog,
            bool addToHistory, params string[] manual)
        {
            if (handler != null) {
                if (_commands.ContainsKey(command)) {
                    _commands[command].Handler += handler;
                }
                else {
                    if (manual.Length == 0) {
                        manual = new[] {NO_DESCRIPTION};
                    }
                    _commands.Add(command,
                        new Command(command, handler, addToLog, addToHistory, manual));
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
            if (_commands.ContainsKey(command)) {
                _commands.Remove(command);
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
            if (!IsOpen && !IsOpening && !IsClosing) {
                if (Opening != null) {
                    CancelEventArgs cancelArgs = new CancelEventArgs();
                    Opening(this, cancelArgs);
                    if (cancelArgs.Cancel) {
                        return;
                    }
                }

                this._closeKey = closeKey;

                InitializeAnimation(OpeningAnimation, _openingAnimationTime);

                Enabled = Visible = true;
                IsOpening = true;
                IsClosing = false;
            }
        }

        /// <summary>
        /// Closes the game console.
        /// </summary>
        public void Close()
        {
            if (IsOpen && !IsClosing) {
                if (Closing != null) {
                    CancelEventArgs cancelArgs = new CancelEventArgs();
                    Closing(this, cancelArgs);
                    if (!cancelArgs.Cancel) {
                        InitializeAnimation(ClosingAnimation, _closingAnimationTime);
                        IsClosing = true;
                    }
                }
                else {
                    InitializeAnimation(ClosingAnimation, _closingAnimationTime);
                    IsClosing = true;
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

            if (input == "") {
                Log(Prefix);
                return true;
            }

            string[] splitInput = input.Split(new[] {' ', '\t'},
                StringSplitOptions.RemoveEmptyEntries);

            if (_commands.ContainsKey(splitInput[0])) {
                try {
                    Command command = _commands[splitInput[0]];
                    bool addToHistory = command.AddToHistory;

                    if (command.AddToLog && addToLog) {
                        Log(Prefix + input);
                    }

                    string[] args = new string[splitInput.Length - 1];
                    Array.Copy(splitInput, 1, args, 0, args.Length);

                    command.Handler(this, new CommandEventArgs(command, args));

                    if (command.AddToHistory &&
                        (_inputHistory.Count == 0 || _inputHistory.Last() != input)) {
                        _inputHistory.Add(input);
                    }
                    _inputHistoryPosition = _inputHistory.Count;

                    command.AddToHistory = addToHistory;

                    return true;
                }
                catch (IndexOutOfRangeException) {
                    _inputHistory.Add(input);
                    _inputHistoryPosition = _inputHistory.Count;
                    if (addToLog) {
                        if (ReportOnError) {
                            Log("Error: The argument count doesn't match the command.", 1);
                        }
                    }
                    return false;
                }
                catch (FormatException) {
                    _inputHistory.Add(input);
                    _inputHistoryPosition = _inputHistory.Count;
                    if (addToLog) {
                        if (ReportOnError) {
                            Log("Error: One or more of the arguments have the wrong type.", 1);
                        }
                    }
                    return false;
                }
                catch (Exception ex) {
                    _inputHistory.Add(input);
                    _inputHistoryPosition = _inputHistory.Count;
                    if (addToLog) {
                        if (ReportOnError) {
                            Log("Error: " + ex.Message, 1);
                        }
                    }
                    return false;
                }
            }
            _inputHistory.Add(input);
            _inputHistoryPosition = _inputHistory.Count;
            if (addToLog) {
                Log(Prefix + input);
                if (ReportOnError) {
                    Log("Command '" + splitInput[0] + "' not found.", 1);
                }
            }
            return false;
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
            if (entry.Level == 255 && !LogDebugMessages) {
                return;
            }

            if (_maxLogEntries > 0 && _log.Count >= _maxLogEntries) {
                _log.RemoveAt(0);
            }
            _log.Add(entry);

            if (IsOpen || IsOpening) {
                UpdateView(_autoScroll);
            }

            if (LogEntryAdded != null) {
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
            int count = _log.Count;
            _log.Clear();
            _currentLine = 0;
            UpdateView(false);
            Log("Log cleared, " + count + " log entries deleted.", 255);
        }

        /// <summary>
        /// Clears the entire input history.
        /// </summary>
        public void ClearHistory()
        {
            int count = _inputHistory.Count;
            _inputHistory.Clear();
            Log("History cleared, " + count + " history entries deleted.", 255);
        }

        /// <summary>
        /// Removes all commands.
        /// </summary>
        public void ClearCommands()
        {
            int count = _commands.Count;
            _commands.Clear();
            Log("Commands cleared, " + count + " commands deleted.", 255);
        }

        /// <summary>
        /// Gets the log level visibility.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>Returns true if the log level is visible, otherwise false.</returns>
        public bool GetLogLevelVisibility(byte level)
        {
            return _logLevelVisibility.ContainsKey(level) ? _logLevelVisibility[level] : true;
        }

        /// <summary>
        /// Sets the log level visibility.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="visible">True, if the log level should be visible, otherwise false.</param>
        public void SetLogLevelVisibility(byte level, bool visible)
        {
            if (visible) {
                _logLevelVisibility.Remove(level);
            }
            else if (!_logLevelVisibility.ContainsKey(level)) {
                _logLevelVisibility.Add(level, false);
            }
            _currentLine = 0;
        }

        /// <summary>
        /// Gets the log level text color.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>Returns the log level text color.</returns>
        public Color GetLogLevelColor(byte level)
        {
            return _logLevelColors.ContainsKey(level) ? _logLevelColors[level] : LogDefaultColor;
        }

        /// <summary>
        /// Sets the log level text color.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="color">The color for the log level text.</param>
        public void SetLogLevelColor(byte level, Color color)
        {
            if (_logLevelColors.ContainsKey(level)) {
                _logLevelColors[level] = color;
            }
            else {
                _logLevelColors.Add(level, color);
            }
        }

        /// <summary>
        /// <para>Updates the view of the log and displays only the last entries.</para>
        /// <para>The view will automatically scroll down, if <c>AutoScroll</c> is set to true.</para>
        /// </summary>
        public void ScrollDown()
        {
            UpdateView(true);
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