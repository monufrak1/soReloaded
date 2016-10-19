using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class Light
    {
        // Light Data
        private Vector3 position;
        private Vector3 direction;
        private Vector4 ambient;
        private Vector4 diffuse;
        private Vector4 specular;
        private float size;
        private BoundingBox aabb;

        private Texture2D lightTexture;

        // Shadow Map Data
        public static int SHADOW_MAP_SIZE = 1024;
        private RenderTarget2D shadowMap;

        private static float PROJECTION_WIDTH_HEIGHT = 500.0f;
        private static float PROJECTION_RANGE = 500.0f;
        private Matrix projectionMatrix;

        private GraphicsDevice device;
        private VertexBuffer vertexBuffer;

        public Light(GraphicsDevice device, Texture2D lightTexture, Vector3 position, Vector3 direction,
                     Vector4 ambient, Vector4 diffuse, Vector4 specular, float size)
        {
            this.device = device;
            this.lightTexture = lightTexture;
            this.position = position;
            this.direction = direction;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.size = size;

            shadowMap = new RenderTarget2D(device, Light.SHADOW_MAP_SIZE, Light.SHADOW_MAP_SIZE, false,
                SurfaceFormat.Single, DepthFormat.Depth24Stencil8);
            projectionMatrix = Matrix.CreateOrthographic(PROJECTION_WIDTH_HEIGHT, 
                PROJECTION_WIDTH_HEIGHT, 1.0f, PROJECTION_RANGE);

            GenerateLight();
        }

        private void GenerateLight()
        {
            // Create light vertices
            VertexPosTex[] vertices = new VertexPosTex[]
            {
                new VertexPosTex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 1.0f)),
                new VertexPosTex(new Vector3(-1.0f,  1.0f, 0.0f), new Vector2(0.0f, 0.0f)),
                new VertexPosTex(new Vector3( 1.0f, -1.0f, 0.0f), new Vector2(1.0f, 1.0f)),
                new VertexPosTex(new Vector3( 1.0f,  1.0f, 0.0f), new Vector2(1.0f, 0.0f))
            };

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTex.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            BuildAABB();
        }

        private void BuildAABB()
        {
            Vector3 maxPos = new Vector3();
            maxPos.X = position.X + (size / 2.0f);
            maxPos.Y = position.Y + (size / 2.0f);
            maxPos.Z = position.Z + (size / 2.0f);

            Vector3 minPos = new Vector3();
            minPos.X = position.X - (size / 2.0f);
            minPos.Y = position.Y - (size / 2.0f);
            minPos.Z = position.Z - (size / 2.0f);

            // Build an AABB to contain the light
            aabb = new BoundingBox(minPos, maxPos);
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set 
            { 
                position = value;

                BuildAABB();
            }
        }

        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public Vector4 Ambient
        {
            get { return ambient; }
            set { ambient = value; }
        }

        public Vector4 Diffuse
        {
            get { return diffuse; }
            set { diffuse = value; }
        }

        public Vector4 Specular
        {
            get { return specular; }
            set { specular = value; }
        }

        public float Size
        {
            get { return size; }
            set 
            { 
                size = value;

                BuildAABB();
            }
        }

        public BoundingBox AABB
        {
            get { return aabb; }
        }
    
        public Texture2D LightTexture
        {
            get { return lightTexture; }
        }

        public RenderTarget2D ShadowMap
        {
            get { return shadowMap; }
        }

        public Matrix ViewMatrix
        {
            get
            {
                // Create a new view matrix for the light
                return Matrix.CreateLookAt(position, position + direction, Vector3.Up);
            }
        }

        public Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
        }

        public BoundingFrustum ViewFrustum
        {
            get
            {
                // Build a frustum and return
                return new BoundingFrustum(ViewMatrix * projectionMatrix);
            }
        }

        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }
    }
}
