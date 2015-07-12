using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using VosSoft.Xna.GameConsole;

namespace ExampleGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        GameConsole console;

        KeyboardState currentKey = new KeyboardState(), lastKey;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            IsMouseVisible = true;
            Content.RootDirectory = "Content";

            console = new GameConsole(this);
            //console = new GameConsole(this, new Rectangle(50, 100, 500, 200), Color.DarkGreen, 0.5f, "en-US");
            console.LogDebugMessages = true;
            console.SetLogLevelColor(255, Color.Yellow);
            console.SetLogLevelColor(1, Color.Red); // error messages
            console.OpeningAnimation = GameConsoleAnimation.None;
            console.ClosingAnimation = GameConsoleAnimation.None;
            console.Initialized += new EventHandler(console_Initialized);
            console.InputEntered += new InputHandler(console_InputEntered);
        }

        // add some commands to the game console
        void console_Initialized(object sender, EventArgs e)
        {
            console.AddCommand("hello", (obj, com) => console.Log("Hello World!"));

            console.AddCommand("test", delegate(object obj, CommandEventArgs com)
            {
                console.Log("take a look at the system console output");
                Console.WriteLine(com.Time.ToString() + " => " + com.Command.Name + ": args = " + com.Args.Length);
                foreach (string argument in com.Args)
                {
                    Console.WriteLine(argument);
                }
            });

            console.AddCommand("add", delegate(object obj, CommandEventArgs com)
            {
                if (com.Args.Length >= 2)
                {
                    int sum = 0;
                    for (int i = 0; i < com.Args.Length; i++)
                    {
                        sum += int.Parse(com.Args[i]);
                    }
                    string sumString = sum.ToString();
                    console.Log("sum: " + sumString);   
                }
                else
                {
                    console.ExecManual("add");
                }
            }, "Add two integers together.",
            "add 100 50");

            console.Lines = 15; // set the console to 15 lines height
        }

        void testMethod(object obj, CommandEventArgs com)
        {
            console.Log("test method log entry");
        }

        // handle all input from the game console
        void console_InputEntered(object sender, InputEventArgs e)
        {
            Console.WriteLine(e.Time + ": " + e.Input);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            lastKey = currentKey;
            currentKey = Keyboard.GetState();

            string strKeys = "";
            foreach (Keys key in currentKey.GetPressedKeys())
            {
                strKeys += key.ToString() + " ";
            }
            Window.Title = strKeys;

            // Allows the game to exit
            if (currentKey.IsKeyDown(Keys.Escape))
                this.Exit();

            if (currentKey.IsKeyDown(Keys.F1) && lastKey.IsKeyUp(Keys.F1))
                console.Open(Keys.F1);
            else if (currentKey.IsKeyDown(Keys.F2) && lastKey.IsKeyUp(Keys.F2))
            {
                console.InputEnabled = !console.InputEnabled;
            }
            else if (currentKey.IsKeyDown(Keys.F3) && lastKey.IsKeyUp(Keys.F3))
            {
                console.Bounds = new Rectangle(150, 150, 500, 96);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
