using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class Billboard
    {
        private Vector3 position;
        private Vector2 size;
        private BoundingBox aabb;

        private Texture2D billboardTexture;

        private GraphicsDevice device;
        private VertexBuffer vertexBuffer;

        public Billboard(GraphicsDevice device, Vector3 position, Vector2 size, 
            Texture2D billboardTexture)
        {
            this.device = device;
            this.position = position;
            this.size = size;
            this.billboardTexture = billboardTexture;

            GenerateGeometry();
        }

        private void GenerateGeometry()
        {
            // Create vertices
            VertexPosTex[] vertices = new VertexPosTex[4];
            vertices[0] = new VertexPosTex(new Vector3(-size.X, -size.Y, 0.0f), new Vector2(0.0f, 1.0f));
            vertices[1] = new VertexPosTex(new Vector3(-size.X, +size.Y, 0.0f), new Vector2(0.0f, 0.0f));
            vertices[2] = new VertexPosTex(new Vector3(+size.X, -size.Y, 0.0f), new Vector2(1.0f, 1.0f));
            vertices[3] = new VertexPosTex(new Vector3(+size.X, +size.Y, 0.0f), new Vector2(1.0f, 0.0f));

            vertexBuffer = new VertexBuffer(device, VertexPosTex.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create AABB
            aabb = new BoundingBox(new Vector3(position.X - size.X, position.Y - size.Y, position.Z - size.X),
                       new Vector3(position.X + size.X, position.Y + size.Y, position.Z + size.X));
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector2 Size
        {
            get { return size; }
            set 
            { 
                size = value;
 
                // Generate new geometry
                GenerateGeometry();
            }
        }

        public BoundingBox AABB
        {
            get { return aabb; }
        }

        public Texture2D BillboardTexture
        {
            get { return billboardTexture; }
            set { billboardTexture = value; }
        }

        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }
    }
}
