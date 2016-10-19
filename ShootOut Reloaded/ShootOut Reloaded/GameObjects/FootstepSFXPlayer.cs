using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace GameObjects
{
    class FootstepSFXPlayer
    {
        SoundEffect[] footstepSFXs;

        int sfxIndex;
        float footstepdt;
        float footstepTimer;
        float sfxMaxVolume;

        Random rand;

        public FootstepSFXPlayer(SoundEffect[] footstepSFXs,
                                 float footstepTimer, float sfxMaxVolume)
        {
            this.footstepSFXs = footstepSFXs;
            this.footstepTimer = footstepTimer;
            this.sfxMaxVolume = sfxMaxVolume;

            if (sfxMaxVolume > 1.0f) sfxMaxVolume = 1.0f;
            if (sfxMaxVolume < 0.0f) sfxMaxVolume = 0.0f;

            rand = new Random();
        }

        public void Play(Vector3 prevPosition, Vector3 position)
        {
            // Play footstep sounds if necessary
            if (!prevPosition.Equals(position))
            {
                float volume = sfxMaxVolume - ((float)rand.NextDouble() * sfxMaxVolume * 0.75f);
                float dist = Vector3.Distance(prevPosition, position) * 0.065f;
                footstepdt += dist;

                if (footstepdt >= footstepTimer)
                {
                    footstepSFXs[sfxIndex].Play(volume, 0.0f, 0.0f);

                    footstepdt = 0.0f;
                    sfxIndex = rand.Next(0, footstepSFXs.Length);
                }
            }
        }
    }
}
