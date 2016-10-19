#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using GameObjects;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// The pause menu comes up over the top of the game,
    /// giving the player options to resume or quit.
    /// </summary>
    class PauseMenuScreen : MenuScreen
    {
        string levelName;
        string gunName;
        ShooterGameType gameType;
        float targetScoreOrTime;

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public PauseMenuScreen(string levelName, string gunName, ShooterGameType gameType, float targetScoreOrTime)
            : base("Paused")
        {
            this.levelName = levelName;
            this.gunName = gunName;
            this.gameType = gameType;
            this.targetScoreOrTime = targetScoreOrTime;

            // Create our menu entries.
            MenuEntry resumeGameMenuEntry = new MenuEntry("Resume");
            MenuEntry restartGameMenuEntry = new MenuEntry("Restart");
            MenuEntry medalsMenuEntry = new MenuEntry("Medals");
            MenuEntry settingsGameMenuEntry = new MenuEntry("Settings");
            MenuEntry quitGameMenuEntry = new MenuEntry("Quit");
            
            // Hook up menu event handlers.
            resumeGameMenuEntry.Selected += OnCancel;
            restartGameMenuEntry.Selected += RestartGameMenuEntrySelected;
            medalsMenuEntry.Selected += MedalsMenuEntrySelected;
            settingsGameMenuEntry.Selected += SettingsGameMenuEntrySelected;
            quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;

            // Add entries to the menu.
            MenuEntries.Add(resumeGameMenuEntry);
            MenuEntries.Add(restartGameMenuEntry);
            MenuEntries.Add(medalsMenuEntry);
            MenuEntries.Add(settingsGameMenuEntry);
            MenuEntries.Add(quitGameMenuEntry);

            // Decrease music volume
            MediaPlayer.Volume = MediaPlayer.Volume / 2;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            // Reset music volume
            MediaPlayer.Volume = ActivePlayer.Profile.MusicEnabled ? ActivePlayer.Profile.MusicVolume : 0.0f;
        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            const string message = "Are you sure you want to quit?";

            MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen(message);

            confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmQuitMessageBox, ControllingPlayer);
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to quit" message box. This uses the loading screen to
        /// transition from the game back to the main menu screen.
        /// </summary>
        void ConfirmQuitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, false, ActivePlayer.PlayerIndex,
                new BackgroundScreen(),
                new MainMenuScreen());
        }

        private void RestartGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Create message string
            string message = string.Empty;
            if (gameType == ShooterGameType.Targets)
            {
                message = "Shoot all of the targets.";
            }
            else if (gameType == ShooterGameType.TargetScore)
            {
                message = "Get a High Score over " + (int)targetScoreOrTime + ".";
            }
            else if (gameType == ShooterGameType.TimeTrial)
            {
                message = "Complete level in under " + targetScoreOrTime.ToString("F") + " secs.";
            }
            else if (gameType == ShooterGameType.Collection)
            {
                message = "Find and collect the Grenade.";
            }
            else if (gameType == ShooterGameType.BullseyeChallenge)
            {
                message = "Get a Bullseye on every target.";
            }
            else if (gameType == ShooterGameType.HeadshotChallenge)
            {
                message = "Get a Headshot on every target.";
            }

            // Re-load the level
            LoadingScreen.Load(ScreenManager, true, message, ActivePlayer.PlayerIndex,
                new ShooterGameScreen(levelName, gunName, gameType, targetScoreOrTime));
        }

        private void MedalsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new MedalsMenuScreen(0, MedalsMenuScreen.TRANSITION_ON_TIME),
                ActivePlayer.PlayerIndex);
        }

        private void SettingsGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Add options screen
            ScreenManager.AddScreen(new OptionsMenuScreen(), ActivePlayer.PlayerIndex);
        }

        #endregion

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw player profile
            ActivePlayer.Profile.Draw(TransitionAlpha);
        }
    }
}
