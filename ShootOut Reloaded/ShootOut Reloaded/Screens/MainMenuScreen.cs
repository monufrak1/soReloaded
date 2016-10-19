#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
#endregion

namespace GameStateManagement
{

    class MainMenuScreen : MenuScreen
    {
        #region Initialization

        MenuEntry purchaseMenuEntry;
        bool purchased;
        string trialString;

        public MainMenuScreen()
            : base("ShootOut Reloaded")
        {
            trialString = "Trial Mode: Unlock full game to save your progress!";

            // Create menu entries
            MenuEntry playGameMenuEntry = new MenuEntry("Play");
            MenuEntry weaponsMenuEntry = new MenuEntry("Weapon Select");
            MenuEntry medalsMenuEntry = new MenuEntry("Medals");
            MenuEntry extrasMenuEntry = new MenuEntry("Extras");
            MenuEntry optionsMenuEntry = new MenuEntry("Settings");
            MenuEntry logoutMenuEntry = new MenuEntry("Log Out");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            weaponsMenuEntry.Selected += WeaponsMenuEntrySelected;
            medalsMenuEntry.Selected += MedalsMenuEntrySelected;
            extrasMenuEntry.Selected += ExtrasMenuEntrySelected;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            logoutMenuEntry.Selected += LogoutMenuEntrySelected;
            exitMenuEntry.Selected += ExitMenuEntrySelected;

            // Add entries to the menu
            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(weaponsMenuEntry);
            MenuEntries.Add(medalsMenuEntry);
            MenuEntries.Add(extrasMenuEntry);
            MenuEntries.Add(optionsMenuEntry);
            MenuEntries.Add(logoutMenuEntry);
            MenuEntries.Add(exitMenuEntry);

            // Create and add Purchase menu entry
            purchaseMenuEntry = new MenuEntry("Unlock Full Game");
            purchaseMenuEntry.Selected += PurchaseMenuEntrySelected;
            if (Guide.IsTrialMode && ActivePlayer.Profile.IsXboxLiveEnabled())
            {
                MenuEntries.Add(purchaseMenuEntry);
            }

            MenuEntry deleteFileMenuEntry = new MenuEntry("Delete Save File");
            deleteFileMenuEntry.Selected += DeleteFileMenuEntrySelected;
            //MenuEntries.Add(deleteFileMenuEntry);

            // Save active player profile
            ActivePlayer.Profile.SaveDataToFile();
        }

        public override void LoadContent()
        {
            base.LoadContent();

            DirectoryInfo di = new DirectoryInfo(@"Content\Music");
            FileInfo[] files = di.GetFiles();

            Random rand = new Random();

            // Load and play background music
            MediaPlayer.Play(ScreenManager.Content.Load<Song>(@"Music\" + files[rand.Next(files.Length)].Name.Split('.')[0]));
            MediaPlayer.IsRepeating = true;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            // Stop background music
            MediaPlayer.Stop();
        }

        #endregion

        #region Handle Input
         

        private void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new PlayMenuScreen(), ActivePlayer.PlayerIndex);
        }

        private void DeleteFileMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.DeleteSaveFile();
        }

        private void WeaponsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new WeaponsMenuScreen(), ActivePlayer.PlayerIndex);
        }

        private void MedalsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new MedalsMenuScreen(0, MedalsMenuScreen.TRANSITION_ON_TIME), ActivePlayer.PlayerIndex);
        }

        private void ExtrasMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new ExtrasMenuScreen(), ActivePlayer.PlayerIndex);
        }

        void OptionsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen(), e.PlayerIndex);
        }

        private void PurchaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (!Guide.IsVisible && ActivePlayer.Profile.IsXboxLiveEnabled())
            {
                // Purchase the game
                Guide.ShowMarketplace(ActivePlayer.PlayerIndex);
            }
        }

        private void LogoutMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Save player profile
            ActivePlayer.Profile.SaveDataToFile();

            ExitScreen();
            ScreenManager.AddScreen(new StartMenuScreen(), null);
        }

        private void ExitMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            const string message = "Are you sure you want to quit?";

            MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen(message);

            confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmExitMessageBox, ActivePlayer.PlayerIndex);
        }

        void ConfirmExitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            // Logout player than exit game
            ActivePlayer.Profile.Logout();

            ScreenManager.Game.Exit();
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            // Override method so you cant "back out" of Main Menu, only logout or exit the game manually
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Remove purchase menu option if needed
            if (!Guide.IsTrialMode)
            {
                if (!purchased)
                {
                    // Remove the purchase menu option
                    MenuEntries.Remove(purchaseMenuEntry);

                    // Set selected entry to first entry
                    SelectedEntry = 0;
                }

                purchased = true;
            }

#if XBOX
            GamePadState gamePadState = GamePad.GetState(ActivePlayer.PlayerIndex);
            if (gamePadState.IsButtonDown(Buttons.LeftTrigger) && gamePadState.IsButtonDown(Buttons.RightTrigger) &&
               gamePadState.IsButtonDown(Buttons.LeftShoulder) && gamePadState.IsButtonDown(Buttons.RightShoulder) &&
               gamePadState.IsButtonDown(Buttons.X) && gamePadState.IsButtonDown(Buttons.B))
            {
                ActivePlayer.Profile.DeleteSaveFile();
            }
#else
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.LeftShift) && keyboardState.IsKeyDown(Keys.RightShift))
            {
                ActivePlayer.Profile.DeleteSaveFile();
            }
#endif
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw Logged in player profile
            ActivePlayer.Profile.Draw(TransitionAlpha);

            if (Guide.IsTrialMode)
            {
                // Display trial mode info
                ScreenManager.SpriteBatch.Begin();

                Vector2 strPos = new Vector2(ScreenManager.GraphicsDevice.Viewport.Width * 0.1f,
                    ScreenManager.GraphicsDevice.Viewport.Height * 0.05f);
                ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, trialString,
                    strPos + (0.025f * strPos), Color.Black * TransitionAlpha);
                ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, trialString,
                    strPos, Color.OldLace * TransitionAlpha);

                ScreenManager.SpriteBatch.End();
            }
        }


        #endregion
    }
}
