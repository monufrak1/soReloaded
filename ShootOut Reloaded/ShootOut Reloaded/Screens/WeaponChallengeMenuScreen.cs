using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using GameObjects;

namespace GameStateManagement
{
    class WeaponChallengeMenuScreen : MenuScreen
    {
        SpriteFont font;
        SpriteFont styleFont;
        Texture2D background;
        Rectangle backgroundRect;

        Texture2D medalIcon;
        Rectangle medalIconRect;

        SoundEffect failedSFX;

        public WeaponChallengeMenuScreen()
            : base("Select Challenge")
        {
            MenuEntry backMenuEntry = new MenuEntry("Back");
            backMenuEntry.Selected += OnCancel;

            // Create menu entries for all levels
            foreach (string file in ActivePlayer.Profile.WeaponChallengeRecords.Keys)
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

            if (ActivePlayer.FullHDEnabled)
            {
                font = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                font = ScreenManager.Content.Load<SpriteFont>("smallFont");
            }
            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");
            background = ScreenManager.Content.Load<Texture2D>("menu_background");
            medalIcon = ScreenManager.Content.Load<Texture2D>("medal_icon");

            failedSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\gun_dryfire");

            GraphicsDevice graphicsDevice = ScreenManager.GraphicsDevice;
            int width = (int)(graphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.1f);

            backgroundRect = new Rectangle((int)(graphicsDevice.Viewport.Width * 0.65f),
                (int)(graphicsDevice.Viewport.Height * 0.15f) + (int)((graphicsDevice.Viewport.Height * 0.1f) * 1.5f),
                width,
                (int)(graphicsDevice.Viewport.Height * 0.5f));

            int medalWidth = backgroundRect.Width / 4;
            int medalHeight = backgroundRect.Height / 4;
            medalIconRect = new Rectangle(backgroundRect.X + (backgroundRect.Width - medalWidth) / 2,
                    (int)(backgroundRect.Bottom - (1.25f * medalHeight)),
                    medalWidth, medalHeight);
        }

        private void LevelMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            string levelFileName = MenuEntries[SelectedEntry].Text + ".lvl";
            string gunFileName = MenuEntries[SelectedEntry].Text.Split(' ')[0] + ".gun";
            WeaponChallenge challenge = ActivePlayer.Profile.GetWeaponChallenge(levelFileName);

            // Load the level selected
            if (challenge.Status != GameObjects.LevelStatus.Locked)
            {
                if (Guide.IsTrialMode && SelectedEntry > 1)
                {
                    if (ActivePlayer.Profile.IsXboxLiveEnabled() && !Guide.IsVisible)
                    {
                        // Trial mode players can only play first two levels. Offer purchase
                        Guide.ShowMarketplace(ActivePlayer.PlayerIndex);
                    }
                    else
                    {
                        // Play failed sound effect
                        failedSFX.Play();
                    }
                }
                else
                {
                    ShooterGameType gameType = challenge.GameType;
                    float targetScoreOrTime = challenge.TargetScoreOrTime;

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

                    ShooterGameScreen.originalGunName = ActivePlayer.Profile.SelectedWeaponName;
                    LoadingScreen.Load(ScreenManager, true, message,
                        ActivePlayer.PlayerIndex,
                        new ShooterGameScreen(levelFileName, gunFileName,
                            gameType, targetScoreOrTime));
                }
            }
            else
            {
                // Play failed sound effect
                failedSFX.Play();
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Draw(gameTime);

            ActivePlayer.Profile.DrawGamerTag(TransitionAlpha);

            if (SelectedEntry != MenuEntries.Count - 1)
            {
                string levelFileName = MenuEntries[SelectedEntry].Text + ".lvl";
                WeaponChallenge weaponChallenge = ActivePlayer.Profile.GetWeaponChallenge(levelFileName);
                ShooterGameType gameType = weaponChallenge.GameType;

                string challengeInfo = string.Empty;
                if(gameType == ShooterGameType.BullseyeChallenge)
                {
                    challengeInfo = "BULLSEYE Challenge";
                }
                else if(gameType == ShooterGameType.HeadshotChallenge)
                {
                    challengeInfo = "HEADSHOT Challenge";
                }
                else if(gameType == ShooterGameType.TargetScore)
                {
                    challengeInfo = "Score Challenge";
                }
                else if(gameType == ShooterGameType.TimeTrial)
                {
                    challengeInfo = "Time Trial";
                }
                else if(gameType == ShooterGameType.Collection)
                {
                    challengeInfo = "Collection Challenge";
                }


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

                float challengeInfoScale = 0.75f;
                Vector2 challengeInfoSize = styleFont.MeasureString(challengeInfo) * challengeInfoScale;
                Vector2 challengeInfoPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - challengeInfoSize.X) / 2,
                    stringPosition.Y + (stringSize.Y * 1.5f));

                string statusString = "LOCKED (" + ActivePlayer.Profile.TotalScore + "/" +
                    ActivePlayer.Profile.GetWeaponScoreRequirement(MenuEntries[SelectedEntry].Text.Split(' ')[0] + ".gun") + ")";
                GameObjects.LevelStatus status = weaponChallenge.Status;
                
                if (status == GameObjects.LevelStatus.Unlocked)
                {
                    statusString = "UNLOCKED";
                }
                else if (status == GameObjects.LevelStatus.Completed)
                {
                    statusString = "";
                }
                Vector2 stringSize2 = styleFont.MeasureString(statusString);
                Vector2 stringPosition2 = new Vector2(backgroundRect.X + (backgroundRect.Width - stringSize2.X) / 2,
                    backgroundRect.Bottom - (1.5f * stringSize2.Y));

                Vector2 size = font.MeasureString(recordData);
                Vector2 strPos = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - size.Y) / 2);

                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

                spriteBatch.Begin();

                spriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
                spriteBatch.DrawString(styleFont, recordString, stringPosition, Color.White * TransitionAlpha);
                spriteBatch.DrawString(styleFont, statusString, stringPosition2, Color.White * TransitionAlpha);
                spriteBatch.DrawString(styleFont, challengeInfo, challengeInfoPosition, Color.White * TransitionAlpha,
                    0.0f, Vector2.Zero, challengeInfoScale, SpriteEffects.None, 0.0f);
                spriteBatch.DrawString(font, recordData, strPos, Color.White * TransitionAlpha);

                // Draw medal icon
                if (status == LevelStatus.Completed)
                {
                    spriteBatch.Draw(medalIcon, medalIconRect, Color.White * TransitionAlpha);
                }

                spriteBatch.End();
            }
        }
    }
}
