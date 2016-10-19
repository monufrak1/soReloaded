using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using GameObjects;

namespace GameStateManagement
{
    class WeaponsMenuScreen : MenuScreen
    {
        SpriteFont styleFont;
        SpriteFont bigFont;

        SoundEffect successSFX;
        SoundEffect failedSFX;

        Texture2D background;
        Rectangle backgroundRect;

        public WeaponsMenuScreen()
            : base("Choose Your Weapon!")
        {
            MenuEntry backMenuEntry = new MenuEntry("Back");
            backMenuEntry.Selected += OnCancel;

            // Create menu entries for all weapons          
            foreach (string name in ActivePlayer.Profile.WeaponRecords.Keys)
            {
                MenuEntry weaponMenuEntry = new MenuEntry(name.Split('.')[0]);
                weaponMenuEntry.Selected += WeaponsMenuEntrySelected;

                MenuEntries.Add(weaponMenuEntry);
            }

            MenuEntries.Add(backMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Load font
            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");
            background = ScreenManager.Content.Load<Texture2D>("menu_background");
            if(ActivePlayer.FullHDEnabled)
            {
                bigFont = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                bigFont = ScreenManager.Content.Load<SpriteFont>("smallFont");
            }

            // Load sound effects
            successSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\pistol");
            failedSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\gun_dryfire");

            GraphicsDevice graphicsDevice = ScreenManager.GraphicsDevice;
            int width = (int)(graphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.1f);

            backgroundRect = new Rectangle((int)(graphicsDevice.Viewport.Width * 0.65f),
                (int)(graphicsDevice.Viewport.Height * 0.15f) + (int)((graphicsDevice.Viewport.Height * 0.1f) * 1.5f),
                width,
                ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Height * 0.5f)
                    : (int)(graphicsDevice.Viewport.Height * 0.55f));
        }

        private void WeaponsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Set player weapon if possible
            string weaponName = MenuEntries[SelectedEntry].Text;

            if (ActivePlayer.Profile.IsWeaponUnlocked(weaponName + ".gun"))
            {
                if (Guide.IsTrialMode && SelectedEntry > 1)
                {
                    if (ActivePlayer.Profile.IsXboxLiveEnabled() && !Guide.IsVisible)
                    {
                        // Trial mode players can only use first two weapons. Offer purchase
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
                    ActivePlayer.Profile.SelectedWeaponName = weaponName;

                    // Play equipt sound effect
                    successSFX.Play();
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

            GraphicsDevice device = ScreenManager.GraphicsDevice;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            spriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));

            if (MenuEntries[SelectedEntry].Text != "Back" && 
                ActivePlayer.Profile.IsWeaponUnlocked(MenuEntries[SelectedEntry].Text + ".gun"))
            {
                // Draw weapon record
                WeaponRecord record = ActivePlayer.Profile.WeaponRecords[MenuEntries[SelectedEntry].Text + ".gun"];
                GunStats stats = ActivePlayer.Profile.GetWeaponStats(MenuEntries[SelectedEntry].Text + ".gun");

                string recordString = "Ammo        : " + stats.MaxAmmo + "\n" +
                                      "Precision   : " + (1.0f - stats.Accuracy * 10.0f).ToString("P") + "\n" +
                                      "Fire Mode   : " + (stats.FireType == FireType.SemiAuto ? "Semi-Auto\n" : "Full-Auto\n") +
                                      "Bullet Type : " + (stats.BulletType == BulletType.Penetrative ? "FMJ\n" : "Hollow-Point\n") +
                                      "\n" +
                                      "Targets Hit  : " + record.TargetsHit + "\n" +
                                      "Shots Fired  : " + record.ShotsFired + "\n" +
                                      "Accuracy     : " + record.Accuracy.ToString("P") + "\n" +
                                      "Multi-Shots  : " + record.Multishots.ToString() + "\n" +
                                      "Long Shots   : " + record.Longshots.ToString() + "\n" +
                                      "Sniper Shots : " + record.Snipershots.ToString() + "\n" +
                                      "Bullseyes    : " + record.Bullseyes.ToString() + "\n" +
                                      "Headshots    : " + record.Headshots.ToString();

                Vector2 size = bigFont.MeasureString(recordString);
                Vector2 position = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - size.Y) / 2);

                spriteBatch.DrawString(bigFont, recordString, position, Color.White * TransitionAlpha);
            }
            else
            {
                // Print instructions
                string instructions = "Increase your Total Score" + "\n" +
                                      "to unlock more weapons to" + "\n" +
                                      "use in Score Attack and " + "\n" +
                                      "Weapon Challenges";

                Vector2 size = bigFont.MeasureString(instructions);
                Vector2 position = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                    backgroundRect.Y + (backgroundRect.Height - size.Y) / 2);

                if (MenuEntries[SelectedEntry].Text != "Back")
                {
                    // Print requirements to unlock this weapon
                    string unlockedString = "Locked (" + ActivePlayer.Profile.TotalScore.ToString() + "/" +
                                  ActivePlayer.Profile.GetWeaponScoreRequirement(MenuEntries[SelectedEntry].Text + ".gun") + ")";

                    size = styleFont.MeasureString(unlockedString);
                    Vector2 unlockedPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X) / 2,
                        backgroundRect.Y + (backgroundRect.Height * 0.25f - size.Y / 2));

                    spriteBatch.DrawString(styleFont, unlockedString, unlockedPosition, Color.White * TransitionAlpha);
                }

                spriteBatch.DrawString(bigFont, instructions, position, Color.White * TransitionAlpha);
            }

            spriteBatch.End();
        }
    }
}
