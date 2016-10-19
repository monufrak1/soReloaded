#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
#endregion

namespace GameStateManagement
{

    class OptionsMenuScreen : MenuScreen
    {
        #region Fields

        MenuEntry invertCameraYMenuEntry;
        MenuEntry sensitivityXMenuEntry;
        MenuEntry sensitivityYMenuEntry;
        MenuEntry musicVolumeMenuEntry;

        static string[] settings = { "VERY LOW", "LOW", "MED", "HIGH", "VERY HIGH" };
        public static float[] cameraSensitivitySettings = { 0.15f, 1.25f, 1.75f, 2.25f, 2.75f };

        static bool invertCameraY;
        static int senXCurrentSetting;
        static int senYCurrentSetting;
        static bool musicEnabled;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen()
            : base("Settings")
        {
            // Create our menu entries.
            invertCameraYMenuEntry = new MenuEntry(string.Empty);
            sensitivityXMenuEntry = new MenuEntry(string.Empty);
            sensitivityYMenuEntry = new MenuEntry(string.Empty);
            musicVolumeMenuEntry = new MenuEntry(string.Empty);

            MenuEntry back = new MenuEntry("Back");

            // Hook up menu event handlers
            invertCameraYMenuEntry.Selected += InvertCameraYMenuEntrySelected;
            sensitivityXMenuEntry.Selected += SensitivityXMenuEntrySelected;
            sensitivityYMenuEntry.Selected += SensitivityYMenuEntrySelected;
            musicVolumeMenuEntry.Selected += MusicVolumeMenuEntrySelected;
            back.Selected += OnCancel;
            
            // Add entries to the menu
            MenuEntries.Add(invertCameraYMenuEntry);
            MenuEntries.Add(sensitivityXMenuEntry);
            MenuEntries.Add(sensitivityYMenuEntry);
            MenuEntries.Add(musicVolumeMenuEntry);
            MenuEntries.Add(back);

            // Load current settings from active player profile
            invertCameraY = ActivePlayer.Profile.InvertCameraY;
            senXCurrentSetting =
                Array.IndexOf(cameraSensitivitySettings, ActivePlayer.Profile.CameraSensitivityX);
            senYCurrentSetting =
                Array.IndexOf(cameraSensitivitySettings, ActivePlayer.Profile.CameraSensitivityY);
            musicEnabled = ActivePlayer.Profile.MusicEnabled;

            SetMenuEntryText();
        }


        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            invertCameraYMenuEntry.Text = "Invert Y-Axis: " + (invertCameraY ? "ENABLED" : "DISABLED");
            sensitivityXMenuEntry.Text = "X-Sensitivity: " + settings[senXCurrentSetting];
            sensitivityYMenuEntry.Text = "Y-Sensitivity: " + settings[senYCurrentSetting];
            musicVolumeMenuEntry.Text = "Music Enabled: " + (musicEnabled ? "ON" : "OFF");

            // Update settings in active player profile
            ActivePlayer.Profile.InvertCameraY = invertCameraY;
            ActivePlayer.Profile.CameraSensitivityX = cameraSensitivitySettings[senXCurrentSetting];
            ActivePlayer.Profile.CameraSensitivityY = cameraSensitivitySettings[senYCurrentSetting];
            ActivePlayer.Profile.MusicEnabled = musicEnabled;
        }


        #endregion

        #region Handle Input

        private void InvertCameraYMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Switch current setting for look invert
            invertCameraY = !invertCameraY;

            SetMenuEntryText();
        }

        private void SensitivityXMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Increase current setting
            senXCurrentSetting = (senXCurrentSetting + 1) % settings.Length;

            SetMenuEntryText();
        }

        private void SensitivityYMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Increase current setting
            senYCurrentSetting = (senYCurrentSetting + 1) % settings.Length;

            SetMenuEntryText();
        }

        private void MusicVolumeMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Change music setting
            musicEnabled = !musicEnabled;

            SetMenuEntryText();
        }

        #endregion
    }
}
