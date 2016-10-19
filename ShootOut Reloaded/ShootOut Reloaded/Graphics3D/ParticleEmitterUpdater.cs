using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Graphics3D
{
    interface ParticleEmitterUpdater
    {
        void UpdateParticle(Particle particle, float dt);
    }
}
