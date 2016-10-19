using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class ParticleEmitter
    {
        private Vector3 position;
        private Texture2D particleTexture;

        private int maxParticles;
        private float maxParticleAge;
        private int numParticles;
        private List<Particle> activeParticles;

        private ParticleEmitterUpdater particleUpdater;

        private BillboardRenderer billboardRenderer;

        private GraphicsDevice device;
        private Random rand;

        public ParticleEmitter(GraphicsDevice device, Vector3 position, Texture2D particleTexture,
                               ParticleEmitterUpdater particleUpdater, BillboardRenderer billboardRenderer,
                               int maxParticles, float maxParticleAge)
        {
            this.device = device;

            this.position = position;
            this.particleTexture = particleTexture;
            this.particleUpdater = particleUpdater;
            this.billboardRenderer = billboardRenderer;
            this.maxParticles = maxParticles;
            this.maxParticleAge = maxParticleAge;

            rand = new Random();

            // Create particle list
            Reset();
        }

        public void EmitParticle(Vector3 direction, Vector2 size, float speed)
        {
            // Emit a new particle
            if (numParticles < maxParticles)
            {
                // Create new particle
                Particle p = new Particle(device, position, direction,
                    size, speed, particleTexture);

                // Add to the list
                activeParticles.Add(p);
                numParticles++;
            }
        }

        public void EmitParticles(int numParticles, Vector3 direction, Vector2 size, float speed)
        {
            for (int i = 0; i < numParticles; i++)
            {
                // Emit a new particle with random changes
                Vector3 randDirection = direction;
                randDirection.X += (float)(rand.NextDouble()) - 0.5f;
                randDirection.Y += (float)(rand.NextDouble()) - 0.5f;
                randDirection.Z += (float)(rand.NextDouble()) - 0.5f;

                Vector2 randSize = Vector2.One;
                randSize.X = (float)(rand.NextDouble() * size.X) + size.X * 0.25f;
                randSize.Y = (float)(rand.NextDouble() * size.Y) + size.Y * 0.25f;

                float randSpeed = speed;
                randSpeed += (float)(rand.NextDouble()) - (speed * 0.25f);

                EmitParticle(randDirection, randSize, randSpeed);
            }
        }

        public void Reset()
        {
            // Remove all active particles
            activeParticles = new List<Particle>(maxParticles);

            numParticles = 0;
        }

        public void Update(float dt)
        {
            // Create copy list
            List<Particle> copyList = new List<Particle>();
            foreach (Particle p in activeParticles)
            {
                copyList.Add(p);
            }

            // Remove "dead" particles
            foreach (Particle p in copyList)
            {
                if (p.Age > maxParticleAge)
                {
                    activeParticles.Remove(p);
                    numParticles--;
                }
            }

            // Update all particles with particle updater
            foreach (Particle p in activeParticles)
            {
                particleUpdater.UpdateParticle(p, dt);
            }
        }

        public void DrawParticles(Vector3 cameraUp, Vector3 cameraForward)
        {
            // Draw each particle
            foreach (Particle p in activeParticles)
            {
                // Draw the particle's billboard
                billboardRenderer.DrawParticle(p.ParticleBillboard, cameraUp, cameraForward);
            }
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Texture2D ParticleTexture
        {
            get { return particleTexture; }
            set { particleTexture = value; }
        }

        public int MaxParticles
        {
            get { return maxParticles; }
            set { maxParticles = value; }
        }

        public float MaxParticleAge
        {
            get { return maxParticleAge; }
            set { maxParticleAge = value; }
        }

        public int NumParticles
        {
            get { return numParticles; }
        }
    }
}