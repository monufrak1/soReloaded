#region File Description
//-----------------------------------------------------------------------------
// LoadingScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// The loading screen coordinates transitions between the menu system and the
    /// game itself. Normally one screen will transition off at the same time as
    /// the next screen is transitioning on, but for larger transitions that can
    /// take a longer time to load their data, we want the menu system to be entirely
    /// gone before we start loading the game. This is done as follows:
    /// 
    /// - Tell all the existing screens to transition off.
    /// - Activate a loading screen, which will transition on at the same time.
    /// - The loading screen watches the state of the previous screens.
    /// - When it sees they have finished transitioning off, it activates the real
    ///   next screen, which may take a long time to load its data. The loading
    ///   screen will be the only thing displayed while this load is taking place.
    /// </summary>
    class LoadingScreen : GameScreen
    {
        #region Fields

        Vector2 messagePosition;
        string message;
        string[] tipStrings = {"FMJ bullets can pass through" + "\n" +
                               "objects, to hit targets on" + "\n" +
                               "the other side.",

                               "Shoot targets at a medium" + "\n" +
                               "range for a Longshot.",

                               "Shoot targets at a very long" + "\n" +
                               "range for a Snipershot.",
 
                               "Shoot multiple targets with" + "\n" +
                               "one bullet for a Multi-Shot.",

                               "Run into a target to" + "\n" +
                               "get a Knockdown.",

                               "Getting Multi-Shots can push" + "\n" +
                               "your Accuracy over 100%.",

                               "Target Hit    - 100" + "\n" +
                               "Knockdown    - 100" + "\n" +
                               "Long Shot       - 300" + "\n" +
                               "Sniper Shot   - 1000" + "\n" +
                               "Bullseye        - 500" + "\n" +
                               "Headshot       - 1000" + "\n" +
                               "Multi-Shot     - Score Multiplier",

                               "Accuracy will decrease" + "\n" +
                               "as you move or shoot.",

                               "Increase your Total Score by" + "\n" +
                               "completing Score Attack levels," + "\n" +
                               "and Weapon Challenges.",

                               "You cannot walk over" + "\n" +
                               "steep terrain.",

                               "Completing a Weapon Challenge" + "\n" +
                               "again will only increase your" + "\n" +
                               "Total Score by the amount of" + "\n" +
                               "points received over your" + "\n" +
                               "previous High Score.",

                               "Unlock new Weapons and" + "\n" +
                               "Challenges by increasing" + "\n" +
                               "your Total Score.",

                               "All Score Attack levels contain" + "\n" +
                               "a hidden Golden Grenade. FIND IT!",

                               "In Score Attack, try to get the" + "\n" +
                               "best level record, by improving" + "\n" +
                               "each stat individually. Go for" + "\n" +
                               "a Best Accuracy run separate" + "\n" +
                               "from a Best Time run.",

                               "You can run faster by using" + "\n" +
                               "light-weight weapons like" + "\n" +
                               "the Colt45 or the Kriss.",
                              
                               "You will run slower while" + "\n" +
                               "using heavier weapons like" + "\n" +
                               "the M4A1 or the Thompson."};

        Vector2 controlsPosition;
        string controls = "CONTROLS" + "\n\n" +
                          "Move      = Left Thumbstick" + "\n" +
                          "Look       = Right Thumbstick" + "\n" +
                          "Aim         = Left Trigger" + "\n" +
                          "Shoot     = Right Trigger" + "\n" +
                          "Reload  = X" + "\n" +
                          "Accept  = A" + "\n" +
                          "Cancel  = B";

        SpriteFont font;
        SpriteFont font2;

        bool loadingIsSlow;
        bool otherScreensAreGone;

        GameScreen[] screensToLoad;

        #endregion

        #region Initialization


        /// <summary>
        /// The constructor is private: loading screens should
        /// be activated via the static Load method instead.
        /// </summary>
        private LoadingScreen(ScreenManager screenManager, bool loadingIsSlow,
                              GameScreen[] screensToLoad)
        {
            this.loadingIsSlow = loadingIsSlow;
            this.screensToLoad = screensToLoad;
            message = string.Empty;

            TransitionOnTime = TimeSpan.FromSeconds(0.5);
        }

        private LoadingScreen(ScreenManager screenManager, bool loadingIsSlow,
                      string message, GameScreen[] screensToLoad)
        {
            this.loadingIsSlow = loadingIsSlow;
            this.screensToLoad = screensToLoad;

            this.message = message;

            TransitionOnTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            int screenWidth = ScreenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = ScreenManager.GraphicsDevice.Viewport.Height;

            font = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture2");
            font2 = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");

            Vector2 controlsSize = font2.MeasureString(controls);
            controlsPosition = new Vector2((screenWidth / 4) - controlsSize.X / 2,
                (screenHeight / 2) - controlsSize.Y / 2);

            if (message == string.Empty)
            {
                // Load a random tip message
                message = tipStrings[(new Random()).Next() % tipStrings.Length];
            }
            Vector2 tipSize = font2.MeasureString(message);
            messagePosition = new Vector2((screenWidth * 3 / 4) - tipSize.X / 2,
                (screenHeight / 2) - tipSize.Y / 2);
                                 
        }

        /// <summary>
        /// Activates the loading screen.
        /// </summary>
        public static void Load(ScreenManager screenManager, bool loadingIsSlow,
                                PlayerIndex? controllingPlayer,
                                params GameScreen[] screensToLoad)
        {
            // Tell all the current screens to transition off.
            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            // Create and activate the loading screen.
            LoadingScreen loadingScreen = new LoadingScreen(screenManager,
                                                            loadingIsSlow,
                                                            screensToLoad);

            screenManager.AddScreen(loadingScreen, controllingPlayer);
        }

        public static void Load(ScreenManager screenManager, bool loadingIsSlow, string message,
                        PlayerIndex? controllingPlayer,
                        params GameScreen[] screensToLoad)
        {
            // Tell all the current screens to transition off.
            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            // Create and activate the loading screen.
            LoadingScreen loadingScreen = new LoadingScreen(screenManager,
                                                            loadingIsSlow,
                                                            message,
                                                            screensToLoad);

            screenManager.AddScreen(loadingScreen, controllingPlayer);
        }

        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the loading screen.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // If all the previous screens have finished transitioning
            // off, it is time to actually perform the load.
            if (otherScreensAreGone)
            {
                ScreenManager.RemoveScreen(this);

                foreach (GameScreen screen in screensToLoad)
                {
                    if (screen != null)
                    {
                        ScreenManager.AddScreen(screen, ControllingPlayer);
                    }
                }

                // Once the load has finished, we use ResetElapsedTime to tell
                // the  game timing mechanism that we have just finished a very
                // long frame, and that it should not try to catch up.
                ScreenManager.Game.ResetElapsedTime();
            }
        }


        /// <summary>
        /// Draws the loading screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // If we are the only active screen, that means all the previous screens
            // must have finished transitioning off. We check for this in the Draw
            // method, rather than in Update, because it isn't enough just for the
            // screens to be gone: in order for the transition to look good we must
            // have actually drawn a frame without them before we perform the load.
            if ((ScreenState == ScreenState.Active) &&
                (ScreenManager.GetScreens().Length == 1))
            {
                otherScreensAreGone = true;
            }

            // The gameplay screen takes a while to load, so we display a loading
            // message while that is going on, but the menus load very quickly, and
            // it would look silly if we flashed this up for just a fraction of a
            // second while returning from the game to the menus. This parameter
            // tells us how long the loading is going to take, so we know whether
            // to bother drawing the message.
            if (loadingIsSlow)
            {
                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

                const string loadingString = "Loading...";

                // Center the text in the viewport.
                Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
                Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
                Vector2 textSize = font.MeasureString(loadingString);
                Vector2 textPosition = new Vector2((viewportSize.X - textSize.X) / 2,
                    (viewportSize.Y * 0.2f) - textSize.Y / 2);

                Color color = Color.White * TransitionAlpha;

                // Draw the text.
                spriteBatch.Begin();
                spriteBatch.DrawString(font, loadingString, textPosition, color);
                spriteBatch.DrawString(font2, controls, controlsPosition, color);
                spriteBatch.DrawString(font2, message, messagePosition, color);
                spriteBatch.End();
            }
        }


        #endregion
    }
}
