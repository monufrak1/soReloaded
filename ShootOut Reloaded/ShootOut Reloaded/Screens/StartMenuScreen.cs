using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace GameStateManagement
{
    class StartMenuScreen : MenuScreen
    {
        SpriteFont styleFont;

        Texture2D titleLogo;
        Rectangle titleRect;

        string startMenuMessage;
        string versionNumber;

        bool startPressed;

        public StartMenuScreen()
            : base(string.Empty)
        {
            startMenuMessage = "Press Start";

#if WINDOWS
            startMenuMessage = "Press Enter";
#endif
        }

        public override void LoadContent()
        {
            int logoWidth = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.5f);
            int logoHeight = (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.33f);

            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture2");

            // Load correct version number from file
            StreamReader inFile = new StreamReader("ShootOutReloadedData.txt");
            versionNumber = inFile.ReadLine();
            inFile.Close();

            // Load title logo texture
            titleLogo = ScreenManager.Content.Load<Texture2D>(@"menu_logo");
            titleRect = new Rectangle((ScreenManager.GraphicsDevice.Viewport.Width/2 - logoWidth/2),
                (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.25f - logoHeight/2),
                logoWidth, logoHeight);

            // Create the player profile object located globally in ActivePlayer class
            ActivePlayer.Profile = new GameObjects.PlayerProfile(ScreenManager.GraphicsDevice,
                ScreenManager.SpriteBatch, ScreenManager.Content);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            const string message = "Are you sure you want to quit?";

            MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen(message);

            confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmExitMessageBox, playerIndex);
        }

        void ConfirmExitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.Game.Exit();
        }

        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;
            
            // Exit game
            if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
            {
                OnCancel(playerIndex);
            }

            // Start game
            if (TransitionAlpha == 1.0f)
            {
                GamePadState state1 = GamePad.GetState(PlayerIndex.One);
                GamePadState state2 = GamePad.GetState(PlayerIndex.Two);
                GamePadState state3 = GamePad.GetState(PlayerIndex.Three);
                GamePadState state4 = GamePad.GetState(PlayerIndex.Four);

                if (state1.IsButtonDown(Buttons.Start))
                {
                    ActivePlayer.PlayerIndex = PlayerIndex.One;
                    startPressed = true;
                }
                else if (state2.IsButtonDown(Buttons.Start))
                {
                    ActivePlayer.PlayerIndex = PlayerIndex.Two;
                    startPressed = true;
                }
                else if (state3.IsButtonDown(Buttons.Start))
                {
                    ActivePlayer.PlayerIndex = PlayerIndex.Three;
                    startPressed = true;
                }
                else if (state4.IsButtonDown(Buttons.Start))
                {
                    ActivePlayer.PlayerIndex = PlayerIndex.Four;
                    startPressed = true;
                }

#if WINDOWS

                KeyboardState keyboardState = Keyboard.GetState();
                if (keyboardState.IsKeyDown(Keys.Enter))
                {
                    startPressed = true;
                }

#endif
            }

            if (startPressed)
            {
                StartButtonPressed();
            }
        }

        bool doLogIn;
        int frameCount;
        private void StartButtonPressed()
        {
            if (doLogIn)
            {
                // Reset 
                doLogIn = false;
                frameCount = 0;
#if XBOX
                if (!Guide.IsVisible)
                {
                    // Login gamer
                    if (ActivePlayer.Profile.Login(ActivePlayer.PlayerIndex))
                    {
                        // Add Main Menu Screen
                        ScreenManager.AddScreen(new MainMenuScreen(), ActivePlayer.PlayerIndex);
                    }
                    else
                    {
                        // Allow player to sign in 
                        Guide.ShowSignIn(1, false);
                        startMenuMessage = "Press Start";
                    }
                }
#else
                ActivePlayer.Profile.LoadDataFromFile();

                // No login required
                ScreenManager.AddScreen(new MainMenuScreen(), ActivePlayer.PlayerIndex);
#endif
                startPressed = false;
            }
            else
            {
                startMenuMessage = "Logging In...";

                frameCount++;
                if (frameCount > 10)
                {
                    doLogIn = true;
                }
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 stringSize = styleFont.MeasureString(startMenuMessage);
            Vector2 stringPosition = new Vector2((ScreenManager.GraphicsDevice.Viewport.Width / 2) - stringSize.X * 0.5f,
                (ScreenManager.GraphicsDevice.Viewport.Height * 0.75f) - stringSize.Y / 2);

            ScreenManager.SpriteBatch.Begin();

            // Draw title logo
            ScreenManager.SpriteBatch.Draw(titleLogo, titleRect, Color.White * TransitionAlpha);

            // Draw string
            ScreenManager.SpriteBatch.DrawString(styleFont,
                startMenuMessage, stringPosition, Color.White * TransitionAlpha);

            float versionNumberScale = 0.5f;
            Vector2 versionNumberSize = ScreenManager.Font.MeasureString(versionNumber) * versionNumberScale;
            Vector2 screenSize = new Vector2(ScreenManager.GraphicsDevice.Viewport.Width, 
                                             ScreenManager.GraphicsDevice.Viewport.Height);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, versionNumber, screenSize - versionNumberSize * 3,
                Color.White * (TransitionAlpha - 0.2f), 0.0f, Vector2.Zero, versionNumberScale, SpriteEffects.None, 0.0f);

            ScreenManager.SpriteBatch.End();
        }
    }
}
