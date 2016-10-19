using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class WaterGrid
    {
        private int numRows;
        private int numCols;
        private int numVertices;
        private int numIndices;
        private Plane plane;

        private float textureScale;
        private Texture2D waterNormalTexture;
        private float transparencyRatio;
        private float reflectionAmount;
        private float refractionAmount;
        private float waveHeight;
        private float waveSpeed;

        private GraphicsDevice device;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        public WaterGrid(GraphicsDevice device, Texture2D waterNormalTexture,
                         float textureScale, float transparencyRatio)
            : this(device, waterNormalTexture, textureScale, transparencyRatio,
                   0.05f, 0.035f, 0.0f, 1.0f)
        {
        }

        public WaterGrid(GraphicsDevice device, Texture2D waterNormalTexture,
                         float textureScale, float transparencyRatio, float reflectionAmount,
                         float refractionAmount, float waveHeight, float waveSpeed)
        {
            this.device = device;
            this.waterNormalTexture = waterNormalTexture;
            this.textureScale = textureScale;
            this.transparencyRatio = transparencyRatio;
            this.reflectionAmount = reflectionAmount;
            this.refractionAmount = refractionAmount;
            this.waveHeight = waveHeight;
            this.waveSpeed = waveSpeed;

            GenerateGrid();
        }

        private void GenerateGrid()
        {
            // Create vertices
            numRows = 258;
            numCols = 258;
            numVertices = numRows * numCols;
            numIndices = 3 * ((numRows - 1) * (numCols - 1) * 2);

            VertexPosTangNormTex[] vertices = new VertexPosTangNormTex[numVertices];

            float spacing = 100.0f; // Grid is infinitly wide
            float halfWidth = (float)(numRows - 1) * spacing * 0.5f;
            float halfDepth = (float)(numCols - 1) * spacing * 0.5f;
            float du = 1.0f / (numRows - 1);
            float dv = 1.0f / (numCols - 1);
            for(int i = 0; i < numRows; i++)
            {
                float z = (halfDepth - i * spacing) * -1.0f;
                for(int j = 0; j < numCols; j++)
                {
                    float x = -halfWidth + j * spacing;
                    float y = 0.0f;

                    vertices[i * numCols + j].pos = new Vector3(x, y, z);
                    vertices[i * numCols + j].tang = Vector3.UnitX;
                    vertices[i * numCols + j].norm = Vector3.UnitY;
                    vertices[i * numCols + j].tex.X = du * j * textureScale;
                    vertices[i * numCols + j].tex.Y = dv * i * textureScale;
                }
            }

            // Create indices
            int[] indices = new int[numIndices];

            int k = 0;
            for (int i = 0; i < numCols - 1; i++)
            {
                for (int j = 0; j < numRows - 1; j++)
                {
                    indices[k] = i * numRows + j;
                    indices[k + 1] = i * numRows + j + 1;
                    indices[k + 2] = (i + 1) * numRows + j;

                    indices[k + 3] = (i + 1) * numRows + j;
                    indices[k + 4] = i * numRows + j + 1;
                    indices[k + 5] = (i + 1) * numRows + j + 1;

                    k += 6;
                }
            }

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTangNormTex.VertexLayout,
                numVertices, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create index buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                numIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        public Matrix CalculateReflectionMatrix(Vector3 cameraPosition, Vector3 cameraLook)
        {
            // Calculate reflection vectors
            Vector3 reflLook = cameraLook;
            reflLook.Y *= -1.0f;

            Vector3 reflPosition = cameraPosition;
            reflPosition.Y = 2.0f * SharedEffectParameters.xWaterHeight - reflPosition.Y;

            return Matrix.CreateLookAt(reflPosition, reflPosition + reflLook, Vector3.Up);
        }

        // PROPERTIES
        public int NumVertices
        {
            get { return numVertices; }
        }

        public int NumIndices
        {
            get { return numIndices; }
        }

        public float TransparencyRatio
        {
            get { return transparencyRatio; }
            set { transparencyRatio = value; }
        }

        public Texture2D WaterNormalTexture
        {
            get { return waterNormalTexture; }
        }

        public float ReflectionAmount
        {
            get { return reflectionAmount; }
            set { reflectionAmount = value; }
        }

        public float RefractionAmount
        {
            get { return refractionAmount; }
            set { refractionAmount = value; }
        }

        public float WaveHeight
        {
            get { return waveHeight; }
            set { waveHeight = value; }
        }

        public float WaveSpeed
        {
            get { return waveSpeed; }
            set { waveSpeed = value; }
        }

        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }

        public IndexBuffer Indices
        {
            get { return indexBuffer; }
        }
    }
}
