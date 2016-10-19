using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameStateManagement
{
    class PlayMenuScreen : MenuScreen
    {
        SpriteFont bigFont;

        string scoreAttackDescription = "Find and destroy all of" + "\n" +
                                        "the targets. Improve your" + "\n" +
                                        "skills by racing against" + "\n" +
                                        "the clock, or try to go" + "\n" +
                                        "for a new top score.";

        string weaponChallengeDescription = "Challenging puzzle levels" + "\n" +
                                            "for each weapon. Increase" + "\n" +
                                            "your Total Score to play" + "\n" +
                                            "more Weapon Challenges" + "\n" +
                                            "by unlocking additional" + "\n" +
                                            "weapons!";

        Texture2D background;
        Rectangle backgroundRect;

        public PlayMenuScreen()
            : base("Choose Game")
        {
            MenuEntry scoreAttackMenuEntry = new MenuEntry("Score Attack");
            MenuEntry weaponChallengesMenuEntry = new MenuEntry("Weapon Challenges");
            MenuEntry backMenuEntry = new MenuEntry("Back");

            scoreAttackMenuEntry.Selected += ScoreAttackMenuEntrySelected;
            weaponChallengesMenuEntry.Selected += WeaponChallengesMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(scoreAttackMenuEntry);
            MenuEntries.Add(weaponChallengesMenuEntry);
            MenuEntries.Add(backMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (ActivePlayer.FullHDEnabled)
            {
                bigFont = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                bigFont = ScreenManager.Content.Load<SpriteFont>("smallFont");
            }
            background = ScreenManager.Content.Load<Texture2D>("menu_background");

            GraphicsDevice graphicsDevice = ScreenManager.GraphicsDevice;
            int width = (int)(graphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.1f);

            backgroundRect = new Rectangle((int)(graphicsDevice.Viewport.Width * 0.65f),
                (int)(graphicsDevice.Viewport.Height * 0.15f) + (int)((graphicsDevice.Viewport.Height * 0.1f) * 1.5f),
                width,
                (int)(graphicsDevice.Viewport.Height * 0.5f));
        }

        private void ScoreAttackMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new ScoreAttackMenuScreen(), ActivePlayer.PlayerIndex);
        }

        private void WeaponChallengesMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new WeaponChallengeMenuScreen(), ActivePlayer.PlayerIndex);
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Draw(gameTime);

            ActivePlayer.Profile.DrawGamerTag(TransitionAlpha);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            // Draw information about each game type
            if (SelectedEntry != MenuEntries.Count - 1)
            {
                spriteBatch.Begin();

                string message = MenuEntries[SelectedEntry].Text == "Score Attack" ?
                    scoreAttackDescription : weaponChallengeDescription;

                Vector2 stringSize = bigFont.MeasureString(message);
                Vector2 stringPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - stringSize.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - stringSize.Y)/2);

                spriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
                spriteBatch.DrawString(bigFont, message, stringPosition, Color.White * TransitionAlpha);

                spriteBatch.End();
            }

        }
    }
}
