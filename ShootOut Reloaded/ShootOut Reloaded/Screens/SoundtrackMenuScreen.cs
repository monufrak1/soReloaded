using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace GameStateManagement
{
    class SoundtrackMenuScreen : MenuScreen
    {
        List<Song> soundtrack;

        SpriteFont font;
        string musicInfo;
        Vector2 musicInfoPosition;
        Texture2D background;
        Rectangle backgroundRect;

        public SoundtrackMenuScreen()
            :base("Soundtrack")
        {
            soundtrack = new List<Song>();
 
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Load the soundtrack songs
            DirectoryInfo di = new DirectoryInfo(@"Content\Music\");
            FileInfo[] files = di.GetFiles();
            List<string> songList = new List<string>();
            foreach (FileInfo songFile in files)
            {
                string name = songFile.Name.Split('.')[0];

                if (!songList.Contains(name))
                {
                    songList.Add(name);
                }
            }

            foreach (string songName in songList)
            {
                soundtrack.Add(ScreenManager.Content.Load<Song>(@"Music\" + songName));

                MenuEntry entry = new MenuEntry(songName);
                entry.Selected += SongMenuEntrySelected;
                MenuEntries.Add(entry);
            }

            MenuEntry backMenuEntry = new MenuEntry("Back");
            backMenuEntry.Selected += OnCancel;
            MenuEntries.Add(backMenuEntry);

            if (ActivePlayer.FullHDEnabled)
            {
                font = ScreenManager.Content.Load<SpriteFont>("bigFont");
            }
            else
            {
                font = ScreenManager.Content.Load<SpriteFont>("smallFont");
            }

            // Set message
            musicInfo = "   Music by Devin McAfee" + "\n" +
                        "Special thanks to Devin for" + "\n" +
                        "providing all of the music" + "\n" +
                        "in ShootOut Reloaded. Visit" + "\n" +
                        "his YouTube channel for" + "\n" +
                        "more:" + "\n\n" +
                        "www.youtube.com/devinmcafee";

            // Load background texture
            background = ScreenManager.Content.Load<Texture2D>(@"menu_background");

            GraphicsDevice graphicsDevice = ScreenManager.GraphicsDevice;
            int width = (int)(graphicsDevice.Viewport.Width * 0.25f);
            int height = (int)(graphicsDevice.Viewport.Height * 0.1f);

            backgroundRect = new Rectangle((int)(graphicsDevice.Viewport.Width * 0.65f),
                (int)(graphicsDevice.Viewport.Height * 0.15f) + (int)((graphicsDevice.Viewport.Height * 0.1f) * 1.5f),
                width,
                (int)(graphicsDevice.Viewport.Height * 0.5f));

            Vector2 size = font.MeasureString(musicInfo);
            musicInfoPosition = new Vector2(backgroundRect.X + (backgroundRect.Width - size.X)/2,
                                   backgroundRect.Y + (backgroundRect.Height - size.Y) / 2);

        }

        private void SongMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Unlock UnBound medal
            ActivePlayer.Profile.UnlockMedal("Master Orchestrator");

            // Play selected song
            MediaPlayer.Play(soundtrack[SelectedEntry]);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw music info
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            spriteBatch.Draw(background, backgroundRect, Color.White * (TransitionAlpha - 0.2f));
            spriteBatch.DrawString(font, musicInfo, musicInfoPosition, Color.White * TransitionAlpha);

            spriteBatch.End();
        }
    }
}
