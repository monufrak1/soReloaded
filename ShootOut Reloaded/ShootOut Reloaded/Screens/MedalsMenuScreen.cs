using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

using GameObjects;

namespace GameStateManagement
{
    class MedalsMenuScreen : MenuScreen
    {
        public static int TRANSITION_ON_TIME = 1500000;
        static int MEDALS_PER_SCREEN = ActivePlayer.FullHDEnabled ? 5 : 4;

        float instructionScale;

        int medalStartIndex;
        Texture2D background;
        Texture2D medalIcon;
        SpriteFont font;
        SpriteFont descriptionFont;

        Rectangle infoRect;
        Rectangle medalIconRect;

        SpriteFont styleFont;
        string title;
        string instructions;

        Color medalLockedColor;

        Vector2 titlePosition;
        Vector2 instructionsPosition;

        SoundEffect screenChangeSFX;

        public MedalsMenuScreen(int medalStartIndex, int transitionOnTimeInTicks)
            : base("")
        {
            base.TransitionOnTime = new TimeSpan(transitionOnTimeInTicks);
            base.TransitionOffTime = new TimeSpan(100);

            title = "Medals: " + ActivePlayer.Profile.MedalsUnlocked + "/" +
                   ActivePlayer.Profile.MedalList.Count;

#if XBOX
            instructions = "RB = Next Page" + "\n\n" +
                           "LB = Prev Page" + "\n\n" +
                           "B   = Exit";

#else
            instructions = "-> = Next Page" + "\n\n" +
                           "<- = Prev Page" + "\n\n" +
                           "Esc = Exit";
#endif

            this.medalStartIndex = medalStartIndex;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            styleFont = ScreenManager.Content.Load<SpriteFont>("outlinedFontTexture");
            background = ScreenManager.Content.Load<Texture2D>("menu_background");
            medalIcon = ScreenManager.Content.Load<Texture2D>("medal_icon");
            font = ScreenManager.Content.Load<SpriteFont>("bigFont");
            descriptionFont = ScreenManager.Content.Load<SpriteFont>("smallFont");
            screenChangeSFX = ScreenManager.Content.Load<SoundEffect>(@"SoundEffects\swoosh_1");

            int width = ScreenManager.GraphicsDevice.Viewport.Width;
            int height = ScreenManager.GraphicsDevice.Viewport.Height;
            instructionScale = ActivePlayer.FullHDEnabled ? 1.0f : 0.75f;
            Vector2 instructionsSize = styleFont.MeasureString(instructions) * instructionScale;
            float offset = 0.075f;
            titlePosition = new Vector2(width * offset, height * offset);
            instructionsPosition = new Vector2(width - instructionsSize.X - (width * offset),
                (height * offset));

            medalLockedColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (TransitionPosition == 0)
            {
                // Reset this screen's transition on time
                TransitionOnTime = TimeSpan.Zero;
            }
        }

        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;
            if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
            {
                // Remove all MedalsMenuScreens open
                GameScreen[] screens = ScreenManager.GetScreens();
                for (int i = screens.Length - 1; i >= 0; i--)
                {
                    if (screens[i] is MedalsMenuScreen)
                    {
                        screens[i].ExitScreen();
                    }
                }
            }
            else if (input.IsNewButtonPress(Buttons.LeftShoulder, ControllingPlayer, out playerIndex) ||
                    input.IsNewKeyPress(Keys.Left, ControllingPlayer, out playerIndex))
            {
                if (medalStartIndex != 0)
                {
                    screenChangeSFX.Play(0.5f, -0.1f, 0.0f);
                    // Remove this page of medals
                    OnCancel(playerIndex);
                }
            }
            else if (input.IsNewButtonPress(Buttons.RightShoulder, ControllingPlayer, out playerIndex) ||
                     input.IsNewKeyPress(Keys.Right, ControllingPlayer, out playerIndex))
            {
                // Add a new page of medals if needed
                if (medalStartIndex + MEDALS_PER_SCREEN < ActivePlayer.Profile.MedalList.Count)
                {
                    screenChangeSFX.Play(0.5f, 0.1f, 0.0f);
                    ScreenManager.AddScreen(new MedalsMenuScreen(medalStartIndex + MEDALS_PER_SCREEN, 0),
                        ActivePlayer.PlayerIndex);
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            int width = (int)(ScreenManager.GraphicsDevice.Viewport.Width / 2);
            int height = (int)(ScreenManager.GraphicsDevice.Viewport.Height / (MEDALS_PER_SCREEN + 1));
            int x = (int)(ScreenManager.GraphicsDevice.Viewport.Width * 0.25f);
            int y = (int)((ScreenManager.GraphicsDevice.Viewport.Height / (MEDALS_PER_SCREEN + 1)) - (height / 2));
            int yOffset = height;

            infoRect = new Rectangle(x, y, width, height);
            medalIconRect = new Rectangle(x, y, 0, (int)(height - (height * 0.1f)));
            medalIconRect.Width = medalIconRect.Height;
            medalIconRect.X += (int)(medalIconRect.Height * 0.05f);
            medalIconRect.Y += (int)(medalIconRect.Height * 0.05f);

            // Draw the medals for this page
            ScreenManager.SpriteBatch.Begin();

            Dictionary<string, Medal> medalList = ActivePlayer.Profile.MedalList;
            for (int i = medalStartIndex; i < medalStartIndex + MEDALS_PER_SCREEN && i < medalList.Count; i++)
            {
                // Draw current medal
                Medal medal = medalList[medalList.Keys.ElementAt(i)];

                Vector2 descSize = descriptionFont.MeasureString(medal.Description);

                Color medalColor = medal.IsUnlocked ? Color.White : medalLockedColor;

                ScreenManager.SpriteBatch.Draw(background, infoRect, Color.White * (TransitionAlpha - 0.2f));
                ScreenManager.SpriteBatch.Draw(medalIcon, medalIconRect, medalColor * (TransitionAlpha));
                ScreenManager.SpriteBatch.DrawString(font, medal.Title, new Vector2(infoRect.X + medalIconRect.Width, infoRect.Y + infoRect.Height/6),
                    Color.White * TransitionAlpha);
                ScreenManager.SpriteBatch.DrawString(descriptionFont, medal.Description,
                    new Vector2(infoRect.X + medalIconRect.Width, infoRect.Y + (infoRect.Height - descSize.Y) * 2/3), Color.White * TransitionAlpha);

                infoRect.Y += yOffset;
                medalIconRect.Y += yOffset;
            }

            ScreenManager.SpriteBatch.DrawString(styleFont, title, titlePosition, Color.White * TransitionAlpha,
                0.0f, Vector2.Zero, instructionScale, SpriteEffects.None, 0.0f);
            ScreenManager.SpriteBatch.DrawString(styleFont, instructions, instructionsPosition, Color.White * TransitionAlpha,
                0.0f, Vector2.Zero, instructionScale, SpriteEffects.None, 0.0f);

            ScreenManager.SpriteBatch.End();
        }
    }
}
