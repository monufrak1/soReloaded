using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;

namespace GameStateManagement
{
    class ExtrasMenuScreen : MenuScreen
    {
        SpriteFont font;
        SpriteFont infoFont;
        Vector2 position;
        string developerInfo;
        Texture2D background;
        Rectangle rect;

        Vector2 infoPosition;
        string unboundInfo;
        Texture2D unboundBoxArt;

        string shootOutInfo;
        Texture2D shootOutBoxArt;

        Rectangle textureRect;
        Rectangle backgroundRect;
        Rectangle infoRect;

        public ExtrasMenuScreen()
            : base("Extras")
        {
            // Menu Entries
            MenuEntry aboutMeMenuEntry = new MenuEntry("About Me");
            MenuEntry unboundMenuEntry = new MenuEntry("UnBound");
            MenuEntry shootOutMenuEntry = new MenuEntry("ShootOut");
            MenuEntry soundtrackEntry = new MenuEntry("Soundtrack");
            MenuEntry backMenuEntry = new MenuEntry("Back");

            // Set up handlers
            aboutMeMenuEntry.Selected += AboutMeMenuEntrySelected;
            unboundMenuEntry.Selected += UnboundMenuEntrySelected;
            shootOutMenuEntry.Selected += ShootOutMenuEntrySelected;
            soundtrackEntry.Selected += SoundtrackMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            // Add to list
            MenuEntries.Add(aboutMeMenuEntry);
            MenuEntries.Add(unboundMenuEntry);
            MenuEntries.Add(shootOutMenuEntry);
            MenuEntries.Add(soundtrackEntry);
            MenuEntries.Add(backMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (ActivePlayer.FullHDEnabled)
            {
                font = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                font = ScreenManager.Content.Load<SpriteFont>("smallFont");
            }

            // Set message
            developerInfo = "My name is Matt, and I love games!!!\n" +
                            "I'm a Computer Science student at the\n" +
                            "University of Maryland, College Park.\n\n" +
                            "Comments? Suggestions? Fixes?\n" +
                            "Tell me what you Like/Hate/Want!!!\n\n" +
                            "Send feedback to:\n" +
                            "     feedback.monufrak1@gmail.com\n\n" +
                            "Or talk to me on Xbox Live:\n" +
                            "     Matt Dice\n\n" +
                            "I will read ALL suggestions and hopefully\n" +
                            "address them in future updates.\n\n" +
                            "See what I'm working on next at:\n" +
                            "     www.youtube.com/monufrak1.\n";

            // Load background texture
            background = ScreenManager.Content.Load<Texture2D>(@"menu_background");

            int x = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.6f);
            int y = (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.25f);
            int width = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.5f);

            Vector2 size = font.MeasureString(developerInfo);
            position = new Vector2(x + width / 2 - size.X / 2,
                                   y + height / 2 - size.Y / 2);

            rect = new Rectangle((int)(position.X - size.X * 0.05f),
                (int)(position.Y - size.Y * 0.05f),
                (int)(size.X * 1.1f), (int)(size.Y * 1.1f));

            infoFont = ScreenManager.Content.Load<SpriteFont>("smallFont");
            unboundInfo = "UnBound is an open world, Action-Adventure game\n" +
                          "where players can explore various worlds and\n" +
                          "complete difficult challenges to unlock new areas.\n" +
                          "Collect Orbs scattered across the world to level\n" +
                          "up your Skills, or to complete challenges.\n" +
                          "With 3 game modes and 7 different worlds to\n" +
                          "explore, UnBound brings a unique gameplay\n" +
                          "experience to Xbox Live Indie Games.";

            shootOutInfo = "Take up arms and shoot the targets! ShootOut is a" + "\n" +
                           "3D FPS, where players shoot their way through" + "\n" +
                           "firing ranges in attempts to get a new high score," + "\n" +
                           "best time, or better accuracy. Featuring over 15" + "\n" +
                           "levels spanning various locations, 6 guns to master," + "\n" +
                           "weapon challenges, secret items to find, tons of" + "\n" +
                           "medals to earn and unlock, and a great soundtrack" + "\n" +
                           "makes ShootOut a must have for all FPS players.";

            width = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.25f);
            height = (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.5f);

            // Load box art
            unboundBoxArt = ScreenManager.Content.Load<Texture2D>(@"unboundboxart");
            shootOutBoxArt = ScreenManager.Content.Load<Texture2D>(@"shootOutBoxArt");
            textureRect = new Rectangle(ActivePlayer.FullHDEnabled ? (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.65f)
                    : (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.6f),
                (int)(ScreenManager.GraphicsDevice.Viewport.Height * 0.15f),
                width, height);

            backgroundRect = new Rectangle((int)(textureRect.X - width * 0.05f),
                (int)(textureRect.Y - height * 0.05f),
                (int)(width + width * 0.1f), (int)(height + height * 0.1f));

            Vector2 infoSize = infoFont.MeasureString(unboundInfo);
            infoPosition = new Vector2(
                (backgroundRect.Center.X - (infoSize.X / 2)),
                (backgroundRect.Center.Y + (backgroundRect.Height / 2 * 1.1f)));

            infoRect = new Rectangle((int)(infoPosition.X - infoSize.X * 0.05f),
                (int)(infoPosition.Y - infoSize.Y * 0.05f),
                (int)(infoSize.X * 1.1f), (int)(infoSize.Y * 1.1f));
        }


        private void AboutMeMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Unlock a Medal ???
            ActivePlayer.Profile.UnlockMedal("Nice to meet you");
        }

        private void UnboundMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Unlock UnBound medal
            ActivePlayer.Profile.UnlockMedal("UnBound");
        }

        private void ShootOutMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Unlock ShootOut medal
            ActivePlayer.Profile.UnlockMedal("ShootOut");
        }

        private void SoundtrackMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new SoundtrackMenuScreen(), ActivePlayer.PlayerIndex);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            ScreenManager.SpriteBatch.Begin();

            if (MenuEntries[SelectedEntry].Text == "About Me")
            {
                ScreenManager.SpriteBatch.Draw(background, rect, Color.White * (TransitionAlpha - 0.25f));
                ScreenManager.SpriteBatch.DrawString(font, developerInfo, position, Color.White * TransitionAlpha);
            }
            else if (MenuEntries[SelectedEntry].Text == "UnBound")
            {
                ScreenManager.SpriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
                ScreenManager.SpriteBatch.Draw(unboundBoxArt, textureRect, Color.White * TransitionAlpha);

                ScreenManager.SpriteBatch.Draw(background, infoRect, Color.White * (TransitionAlpha - 0.2f));
                ScreenManager.SpriteBatch.DrawString(infoFont, unboundInfo, infoPosition,
                    Color.White * TransitionAlpha);
            }
            else if (MenuEntries[SelectedEntry].Text == "ShootOut")
            {
                // Draw ShootOut info
                ScreenManager.SpriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
                ScreenManager.SpriteBatch.Draw(shootOutBoxArt, textureRect, Color.White * TransitionAlpha);

                ScreenManager.SpriteBatch.Draw(background, infoRect, Color.White * (TransitionAlpha - 0.2f));
                ScreenManager.SpriteBatch.DrawString(infoFont, shootOutInfo, infoPosition,
                    Color.White * TransitionAlpha);
            }

            ScreenManager.SpriteBatch.End();
        }
    }
}
