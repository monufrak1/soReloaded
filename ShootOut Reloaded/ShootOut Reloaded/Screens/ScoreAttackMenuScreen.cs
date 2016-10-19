using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;

namespace GameStateManagement
{
    class ScoreAttackMenuScreen : MenuScreen
    {
        SpriteFont font;
        SpriteFont styleFont;
        Texture2D background;
        Rectangle backgroundRect;
        SoundEffect failedSFX;

        public ScoreAttackMenuScreen()
            : base("Select Level")
        {
            MenuEntry backMenuEntry = new MenuEntry("Back");
            backMenuEntry.Selected += OnCancel;

            // Create menu entries for all levels
            foreach (string file in ActivePlayer.Profile.ScoreAttackRecords.Keys)
            {
                MenuEntry levelMenuEntry = new MenuEntry(file.Split('.')[0]);
                levelMenuEntry.Selected += LevelMenuEntrySelected;

                MenuEntries.Add(levelMenuEntry);
            }

            MenuEntries.Add(backMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            GraphicsDevice graphicsDevice = ScreenManager.GraphicsDevice;
            int width = (int)(graphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.1f);

            if (ActivePlayer.FullHDEnabled)
            {
                font = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                font = ScreenManager.Content.Load<SpriteFont>("smallFont");
                width = (int)(graphicsDevice.Viewport.Width * 0.28f);
            }
            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");
            background = ScreenManager.Content.Load<Texture2D>("menu_background");
            failedSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\gun_dryfire");


            backgroundRect = new Rectangle((int)(graphicsDevice.Viewport.Width * 0.65f),
                (int)(graphicsDevice.Viewport.Height * 0.15f) + (int)((graphicsDevice.Viewport.Height * 0.1f) * 1.5f),
                width,
                (int)(graphicsDevice.Viewport.Height * 0.5f));
        }

        private void LevelMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            bool levelUnlocked = SelectedEntry == 0 ||
                ActivePlayer.Profile.GetLevelRecord(MenuEntries[SelectedEntry - 1].Text + ".lvl")
                    != GameObjects.LevelRecord.ZeroRecord;
            levelUnlocked = true;

            if (Guide.IsTrialMode && SelectedEntry > 2 && levelUnlocked)
            {
                if (!Guide.IsVisible && ActivePlayer.Profile.IsXboxLiveEnabled())
                {
                    // Trial mode players can only play first set of levels. Offer purchase
                    Guide.ShowMarketplace(ActivePlayer.PlayerIndex);
                }
                else
                {
                    failedSFX.Play();
                }
            }
            else
            {
                if (levelUnlocked)
                {
                    // Load the level selected
                    LoadingScreen.Load(ScreenManager, true, ActivePlayer.PlayerIndex,
                        new ShooterGameScreen(MenuEntries[SelectedEntry].Text + ".lvl",
                            ActivePlayer.Profile.SelectedWeaponName + ".gun"));
                }
                else
                {
                    failedSFX.Play();
                }
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Draw(gameTime);

            ActivePlayer.Profile.DrawGamerTag(TransitionAlpha);

            if (SelectedEntry != MenuEntries.Count - 1)
            {
                // Draw selected level record
                GameObjects.LevelRecord levelRecord = ActivePlayer.Profile.GetLevelRecord(MenuEntries[SelectedEntry].Text + ".lvl");
                string recordData = "High Score    : " + levelRecord.Score + "\n" +
                                    "Best Time     : " + levelRecord.Time.ToString("F") + " Secs" + "\n" +
                                    "Least Shots   : " + levelRecord.ShotsFired + "\n" +
                                    "Best Accuracy : " + levelRecord.Accuracy.ToString("P");

                string recordString = (levelRecord.Equals(GameObjects.LevelRecord.ZeroRecord) ?
                    "No Record" : "Best Records");
                Vector2 stringSize = styleFont.MeasureString(recordString);
                Vector2 stringPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - stringSize.X) / 2,
                    backgroundRect.Y + stringSize.Y);

                string secretFound = ActivePlayer.Profile.IsSecretFound(MenuEntries[SelectedEntry].Text + ".lvl") ?
                    "Grenade Found" : "Grenade Not Found";
                Vector2 stringSize2 = styleFont.MeasureString(secretFound);
                Vector2 stringPosition2 = new Vector2(backgroundRect.X + (backgroundRect.Width - stringSize2.X) / 2,
                    backgroundRect.Bottom - (1.5f * stringSize2.Y));

                Vector2 size = font.MeasureString(recordData);
                Vector2 strPos = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - size.Y) / 2);

                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

                spriteBatch.Begin();

                spriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
                spriteBatch.DrawString(styleFont, recordString, stringPosition, Color.White * TransitionAlpha);
                spriteBatch.DrawString(styleFont, secretFound, stringPosition2, Color.White * TransitionAlpha);
                spriteBatch.DrawString(font, recordData, strPos, Color.White * TransitionAlpha);

                spriteBatch.End();
            }
        }
    }
}