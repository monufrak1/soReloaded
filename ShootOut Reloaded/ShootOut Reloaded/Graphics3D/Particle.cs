using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class Particle
    {
        private Vector3 position;
        private Vector3 direction;
        private Vector2 size;
        private float age;
        private float speed;
        private Billboard particleBillboard;

        public Particle(GraphicsDevice device, Vector3 position, Vector3 direction, 
                        Vector2 size, float speed, Texture2D particleTexture)
        {
            this.position = position;
            this.direction = direction;
            this.size = size;
            this.speed = speed;

            this.age = 0.0f;

            // Create billboard for this particle
            this.particleBillboard = new Billboard(device, this.position, this.size, particleTexture);
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set 
            { 
                position = value;

                // Update billboard position
                particleBillboard.Position = position;
            }
        }

        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public Vector2 Size
        {
            get { return size; }
            set
            {
                size = value;

                // Update billboard size
                particleBillboard.Size = size;
            }
        }

        public float Age
        {
            get { return age; }
            set { age = value; }
        }

        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        public Billboard ParticleBillboard
        {
            get { return particleBillboard; }
        }
    }
}
