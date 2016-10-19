#region File Description
//-----------------------------------------------------------------------------
// MenuScreen.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// Base class for screens that contain a menu of options. The user can
    /// move up and down to select an entry, or cancel to back out of the screen.
    /// </summary>
    abstract class MenuScreen : GameScreen
    {
        #region Fields

        List<MenuEntry> menuEntries = new List<MenuEntry>();
        int selectedEntry = 0;
        string menuTitle;

        Rectangle rect;

        Texture2D selectedBackground;
        Texture2D menuBackground;

        SpriteFont styleFont;
        SoundEffect menuSFX;

        #endregion

        #region Properties


        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> MenuEntries
        {
            get { return menuEntries; }
        }

        protected int SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        protected string MenuTitle
        {
            get { return menuTitle; }
            set { menuTitle = value; }
        }

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuScreen(string menuTitle)
        {
            this.menuTitle = menuTitle;

            rect = new Rectangle();

            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            // Load textures
            selectedBackground = ScreenManager.Content.Load<Texture2D>(@"menu_image");
            menuBackground = ScreenManager.Content.Load<Texture2D>(@"menu_image_black");
            menuSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\menu_select");//swoosh_1");

            if (ActivePlayer.FullHDEnabled)
            {
                styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture2");
            }
            else
            {
                styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");
            }
        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or cancelling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            // Move to the previous menu entry?
            if (input.IsMenuUp(ControllingPlayer))
            {
                selectedEntry--;
                menuSFX.Play();

                if (selectedEntry < 0)
                    selectedEntry = menuEntries.Count - 1;
            }

            // Move to the next menu entry?
            if (input.IsMenuDown(ControllingPlayer))
            {
                selectedEntry++;
                menuSFX.Play();

                if (selectedEntry >= menuEntries.Count)
                    selectedEntry = 0;
            }

            // Accept or cancel the menu? We pass in our ControllingPlayer, which may
            // either be null (to accept input from any player) or a specific index.
            // If we pass a null controlling player, the InputState helper returns to
            // us which player actually provided the input. We pass that through to
            // OnSelectEntry and OnCancel, so they can tell which player triggered them.
            PlayerIndex playerIndex;

            if (input.IsMenuSelect(ControllingPlayer, out playerIndex))
            {
                OnSelectEntry(selectedEntry, playerIndex);
            }
            else if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
            {
                OnCancel(playerIndex);
            }
        }


        /// <summary>
        /// Handler for when the user has chosen a menu entry.
        /// </summary>
        protected virtual void OnSelectEntry(int entryIndex, PlayerIndex playerIndex)
        {
            menuEntries[entryIndex].OnSelectEntry(playerIndex);
        }


        /// <summary>
        /// Handler for when the user has cancelled the menu.
        /// </summary>
        protected virtual void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }


        /// <summary>
        /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
        /// </summary>
        protected void OnCancel(object sender, PlayerIndexEventArgs e)
        {
            OnCancel(e.PlayerIndex);
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Allows the screen the chance to position the menu entries. By default
        /// all menu entries are lined up in a vertical list, centered on the screen.
        /// </summary>
        protected virtual void UpdateMenuEntryLocations()
        {
            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // start at Y = 175; each X value is generated per entry
            Vector2 position = new Vector2(0f, 
                ScreenManager.GraphicsDevice.Viewport.Height * 0.25f);

            // update each menu entry's location in turn
            for (int i = 0; i < menuEntries.Count; i++)
            {
                MenuEntry menuEntry = menuEntries[i];
                
                // each entry is to be centered horizontally
                float offset = 0;// (i == selectedEntry) ? ScreenManager.GraphicsDevice.Viewport.Width * 0.01f : 0;
                position.X = ScreenManager.GraphicsDevice.Viewport.Width * 0.1f + offset;
                
                if (ScreenState == ScreenState.TransitionOn)
                    position.X -= transitionOffset * 256;
                else
                    position.X += transitionOffset * 512;

                // set the entry's position
                menuEntry.Position = position;

                // move down for the next entry the size of this entry
                position.Y += menuEntry.GetHeight(this) * 1.1f;
            }
        }


        /// <summary>
        /// Updates the menu.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Update each nested MenuEntry object.
            for (int i = 0; i < menuEntries.Count; i++)
            {
                bool isSelected = IsActive && (i == selectedEntry);

                menuEntries[i].Update(this, isSelected, gameTime);
            }
        }


        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // make sure our entries are in the right place before we draw them
            UpdateMenuEntryLocations();

            GraphicsDevice graphics = ScreenManager.GraphicsDevice;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = styleFont;

            spriteBatch.Begin();

            // Draw each menu entry in turn
            Vector2 firstEntryPosition = Vector2.Zero;
            int width = graphics.Viewport.Width / 3;
            int offset = (int)(graphics.Viewport.Width * 0.025f);
            for (int i = 0; i < menuEntries.Count; i++)
            {
                MenuEntry menuEntry = menuEntries[i];

                bool isSelected = IsActive && (i == selectedEntry);

                int height = (int)menuEntry.GetHeight(this); 
                rect.X = (int)menuEntry.Position.X - offset; 
                rect.Y = (int) menuEntry.Position.Y - height/2;
                rect.Width = width + ((ActivePlayer.FullHDEnabled) ? 0 : 
                                            (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.025f));
                rect.Height = height;

                if (isSelected)
                {
                    spriteBatch.Draw(selectedBackground, rect, 
                        Color.White * (TransitionAlpha - 0.2f));
                }
                else
                {
                    spriteBatch.Draw(menuBackground, rect, 
                        Color.White * (TransitionAlpha - 0.2f));
                }

                menuEntry.Draw(this, isSelected, gameTime);

                if (i == 0)
                {
                    firstEntryPosition = menuEntry.Position;
                }
            }

            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // Draw the menu title centered on the screen
            float titleScale = 1.25f;
            Vector2 titleSize = font.MeasureString(menuTitle) * titleScale;
            Vector2 titlePosition = new Vector2(rect.X + ((width - titleSize.X) / 2),
                firstEntryPosition.Y - (titleSize.Y * 1.5f));

            titlePosition.Y -= transitionOffset * 100;
            Vector2 titlePosition2 = titlePosition + new Vector2(graphics.Viewport.Width * 0.0025f,
                graphics.Viewport.Height * 0.0025f);

            // Print menu title
            spriteBatch.DrawString(font, menuTitle, titlePosition, Color.White * TransitionAlpha,
                0.0f, Vector2.Zero,
                titleScale, SpriteEffects.None, 0.0f);


            spriteBatch.End();

            base.Draw(gameTime);
        }


        #endregion
    }
}
