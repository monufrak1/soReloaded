using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using GameObjects;

namespace GameStateManagement
{
    class LevelCompleteMenuScreen : MenuScreen
    {
        SpriteFont styleFont;
        SpriteFont textFont;
        Texture2D background;
        Rectangle backgroundRect;

        LevelRecord levelRecord;
        string levelName;
        string gunName;
        ShooterGameType gameType;
        float targetScoreOrTime;

        bool levelComplete;
        bool newRecord;
        LevelRecord oldLevelRecord;

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public LevelCompleteMenuScreen(LevelRecord levelRecord, string levelName, string gunName,
                                       ShooterGameType gameType, float targetScoreOrTime)
            : base("Level Complete")
        {
            string doneMenuEntryText = "Done";

            this.levelName = levelName;
            this.gunName = gunName;
            this.levelRecord = levelRecord;
            this.gameType = gameType;
            this.targetScoreOrTime = targetScoreOrTime;

            levelComplete = true;

            if (gameType != ShooterGameType.ScoreAttack)
            {
                MenuTitle = "Challenge Complete";
            }

            // Check if level was completed
            if ((gameType == ShooterGameType.TargetScore && levelRecord.Score < (int)targetScoreOrTime) ||
                (gameType == ShooterGameType.TimeTrial && levelRecord.Time > targetScoreOrTime) ||
                (gameType == ShooterGameType.BullseyeChallenge && levelRecord.NumBullseyes != levelRecord.NumTargetsShot + 
                                                                                              levelRecord.NumKnockdowns) ||
                (gameType == ShooterGameType.HeadshotChallenge && levelRecord.NumHeadshots != levelRecord.NumTargetsShot +
                                                                                              levelRecord.NumKnockdowns))
            {
                levelComplete = false;
                MenuTitle = "Challenge Failed";
                doneMenuEntryText = "Quit";
            }

            if (levelComplete)
            {
                // Update player record
                oldLevelRecord = ActivePlayer.Profile.GetLevelRecord(levelName);
                newRecord = ActivePlayer.Profile.UpdateLevelRecord(levelName, levelRecord);
            }

            // Create our menu entries
            MenuEntry restartGameMenuEntry = new MenuEntry("Restart");
            MenuEntry doneGameMenuEntry = new MenuEntry(doneMenuEntryText);
            
            // Hook up menu event handlers.
            restartGameMenuEntry.Selected += RestartLevelMenuEntrySelected;
            doneGameMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(doneGameMenuEntry);
            MenuEntries.Add(restartGameMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            textFont = ScreenManager.Content.Load<SpriteFont>("bigFont");
            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture2");
            background = ScreenManager.Content.
                Load<Texture2D>("menu_background");

            int x = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.08f);
            int y = (int)(ScreenManager.GraphicsDevice.Viewport.Height * (ActivePlayer.FullHDEnabled ? 0.55f : 0.5f));
            int width = (int)(ScreenManager.GraphicsDevice.Viewport.Width / 2);
            int height = (int)(ScreenManager.GraphicsDevice.Viewport.Height / 4);

            backgroundRect = new Rectangle(x, y, width, height);
        }

        #endregion

        #region Handle Input

        private void RestartLevelMenuEntrySelected(object sender, PlayerIndexEventArgs e)
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

            // Re-load the level again
            LoadingScreen.Load(ScreenManager, true, message, ActivePlayer.PlayerIndex,
                new ShooterGameScreen(levelName, gunName, gameType, targetScoreOrTime));
        }

        protected override void OnCancel(PlayerIndex p)
        {
            // Return to the correct level select menu
            if (gameType == ShooterGameType.ScoreAttack)
            {
                LoadingScreen.Load(ScreenManager, false, ActivePlayer.PlayerIndex,
                    new BackgroundScreen(),
                    new MainMenuScreen(),
                    new PlayMenuScreen(),
                    new ScoreAttackMenuScreen());
            }
            else
            {
                LoadingScreen.Load(ScreenManager, false, ActivePlayer.PlayerIndex,
                    new BackgroundScreen(),
                    new MainMenuScreen(),
                    new PlayMenuScreen(),
                    new WeaponChallengeMenuScreen());
            }
        }


        #endregion

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw player profile
            ActivePlayer.Profile.Draw(TransitionAlpha);

            ScreenManager.SpriteBatch.Begin();

            if (levelComplete)
            {
                // Draw the level record
                ScreenManager.SpriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));

                if (newRecord)
                {
                    string newRecordString = "NEW RECORD!";
                    Vector2 size = styleFont.MeasureString(newRecordString);
                    Vector2 strPos = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                        ActivePlayer.FullHDEnabled ? backgroundRect.Y + 0.25f * size.Y
                            : backgroundRect.Y - size.Y);
                    ScreenManager.SpriteBatch.DrawString(styleFont, newRecordString,
                        strPos, Color.White * TransitionAlpha);
                }

                // Print out record
                string recordString = "Score       : " + levelRecord.Score + "\n" +
                                      "Time        : " + levelRecord.Time.ToString("F") + " Secs\n" +
                                      "Shots Fired : " + levelRecord.ShotsFired + "\n" +
                                      "Accuracy    : " + levelRecord.Accuracy.ToString("P");
                Vector2 strSize = textFont.MeasureString(recordString);
                Vector2 recordStrPos = new Vector2(backgroundRect.X + (backgroundRect.Width - strSize.X) / 2,
                        backgroundRect.Y + (backgroundRect.Height - strSize.Y) / 2);
                ScreenManager.SpriteBatch.DrawString(textFont,
                    recordString, recordStrPos, Color.White * TransitionAlpha);

                // Draw indicator next to updated stats
                if (newRecord)
                {
                    string newStatString = "New\n";
                    int heightOffset = 0;
                    float newStatScale = 0.5f;
                    Vector2 newStatStringSize = styleFont.MeasureString(newStatString) * newStatScale;
                    if (levelRecord.Score > oldLevelRecord.Score)
                    {
                        ScreenManager.SpriteBatch.DrawString(styleFont, newStatString,
                            new Vector2(recordStrPos.X - 1.5f * newStatStringSize.X, recordStrPos.Y + heightOffset), Color.White * TransitionAlpha,
                            0.0f, Vector2.Zero, newStatScale, SpriteEffects.None, 0.0f);
                    }
                    heightOffset += (int)newStatStringSize.Y;

                    if (levelRecord.Time < oldLevelRecord.Time || oldLevelRecord.Time == 0.0f)
                    {
                        ScreenManager.SpriteBatch.DrawString(styleFont, newStatString,
                            new Vector2(recordStrPos.X - 1.5f * newStatStringSize.X, recordStrPos.Y + heightOffset), Color.White * TransitionAlpha,
                            0.0f, Vector2.Zero, newStatScale, SpriteEffects.None, 0.0f);
                    }
                    heightOffset += (int)newStatStringSize.Y;

                    if ((levelRecord.ShotsFired < oldLevelRecord.ShotsFired) ||
                        (oldLevelRecord == LevelRecord.ZeroRecord))
                    {
                        ScreenManager.SpriteBatch.DrawString(styleFont, newStatString,
                            new Vector2(recordStrPos.X - 1.5f * newStatStringSize.X, recordStrPos.Y + heightOffset), Color.White * TransitionAlpha,
                            0.0f, Vector2.Zero, newStatScale, SpriteEffects.None, 0.0f);
                    }
                    heightOffset += (int)newStatStringSize.Y;

                    if (levelRecord.Accuracy > oldLevelRecord.Accuracy)
                    {
                        ScreenManager.SpriteBatch.DrawString(styleFont, newStatString,
                            new Vector2(recordStrPos.X - 1.5f * newStatStringSize.X, recordStrPos.Y + heightOffset), Color.White * TransitionAlpha,
                            0.0f, Vector2.Zero, newStatScale, SpriteEffects.None, 0.0f);
                    }
                    heightOffset += (int)newStatStringSize.Y;
                }
            }
            else 
            {
                // Draw background
                ScreenManager.SpriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));

                // Draw the reason for level failure
                string failureMessage = string.Empty;
                if (gameType == ShooterGameType.BullseyeChallenge)
                {
                    failureMessage = "You must get only BULLSEYES!";
                }
                else if (gameType == ShooterGameType.HeadshotChallenge)
                {
                    failureMessage = "You must get only HEADSHOTS!";
                }
                else if (gameType == ShooterGameType.TargetScore)
                {
                    failureMessage = "You must Score at least " + ((int)targetScoreOrTime).ToString() + "!";
                }
                else if (gameType == ShooterGameType.TimeTrial)
                {
                    failureMessage = "You must get a Time under " + targetScoreOrTime.ToString() + " secs!";
                }

                // Draw the message
                Vector2 failureMessageSize = textFont.MeasureString(failureMessage);
                Vector2 failureMessagePosition = new Vector2(backgroundRect.X + (backgroundRect.Width - failureMessageSize.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - failureMessageSize.Y) / 2);
                ScreenManager.SpriteBatch.DrawString(textFont, failureMessage, failureMessagePosition,
                    Color.White * TransitionAlpha);
            }

            ScreenManager.SpriteBatch.End();
        }
    }
}
