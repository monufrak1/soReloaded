using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace GameObjects
{
    class Medal
    {
        const float MEDAL_DRAW_TIME = 6.5f;

        SpriteBatch spriteBatch;
        SpriteFont bigFont;
        SpriteFont smallFont;
        Texture2D background;
        Texture2D medalPicture;

        SoundEffect unlockSFX;

        Rectangle backgroundRect;
        Rectangle medalPictureRect;
        Vector2 titlePosition;
        Vector2 descriptionPosition;

        bool isUnlocked;
        string title;
        string description;
        float drawTimer;

        public Medal(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont bigFont, 
            SpriteFont smallFont, Texture2D background, Texture2D medalPicture, SoundEffect unlockSFX,
            string title, string description)
        {
            this.spriteBatch = spriteBatch;
            this.bigFont = bigFont;
            this.smallFont = smallFont;
            this.medalPicture = medalPicture;
            this.background = background;
            this.unlockSFX = unlockSFX;
            this.title = title;
            this.description = description;

            int width = ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Width * 0.3f)
                : (int)(graphicsDevice.Viewport.Width * 0.4f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.15f);
            int x = (int)(graphicsDevice.Viewport.Width * 0.1f);
            int y = (int)(graphicsDevice.Viewport.Height * 0.95f) - height;

            backgroundRect = new Rectangle(x, y, width, height);
            medalPictureRect = new Rectangle((int)(x + (height * 0.05f)),
                (int)(y + (height * 0.05f)),
                (int)(height - height * 0.1f),
                (int)(height - height * 0.1f));

            Vector2 titleSize = bigFont.MeasureString(title);
            Vector2 descriptionSize = smallFont.MeasureString(description);

            titlePosition = new Vector2(backgroundRect.X + (backgroundRect.Width - titleSize.X) / 2,
                backgroundRect.Y + titleSize.Y/2);
            descriptionPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - descriptionSize.X) / 2,
                titlePosition.Y + (backgroundRect.Height - descriptionSize.Y)/2);
        }

        public bool Unlock()
        {
            // Set Medal to unlocked if necessary
            if (!isUnlocked)
            {
                isUnlocked = true;
                drawTimer = MEDAL_DRAW_TIME;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Draw(float dt)
        {
            if (isUnlocked && drawTimer >= 0.0f)
            {
                if (drawTimer == MEDAL_DRAW_TIME)
                {
                    // Play unlock SFX
                    unlockSFX.Play(1.0f, 0.25f, 0.0f);
                }

                drawTimer -= dt;
                float alpha = ((drawTimer) >= MEDAL_DRAW_TIME * 0.9f) ?
                    Math.Max(-drawTimer + MEDAL_DRAW_TIME, 0.0f) :
                    Math.Min(drawTimer, 0.8f);

                spriteBatch.Draw(background, backgroundRect, Color.White * (alpha - 0.2f));
                spriteBatch.Draw(medalPicture, medalPictureRect, Color.White * (alpha - 0.2f));

                spriteBatch.DrawString(bigFont, title, titlePosition, Color.White * alpha);
                spriteBatch.DrawString(smallFont, description, descriptionPosition, Color.White * alpha);

                // Medal is currently being drawn
                return true;
            }

            return false;
        }

        // PROPERTIES
        public bool IsUnlocked
        {
            get { return isUnlocked; }
            set 
            { 
                // Will not effect draw timer, so medal will not be drawn
                isUnlocked = value; 
            }
        }

        public string Title
        {
            get { return title; }
        }

        public string Description
        {
            get { return description; }
        }
    }
}
