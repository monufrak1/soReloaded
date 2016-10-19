using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class SurfacePlane
    {
        private GraphicsDevice device;

        // Geometry data
        private float height;
        private Plane plane;
        private int numRows;
        private int numCols;
        private int numVertices;
        private int numIndices;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        // Texture data
        private Texture2D diffuseMap;
        private Texture2D specularMap;
        private Texture2D normalMap;
        private float textureScale;

        public SurfacePlane(GraphicsDevice device, float height,
            Texture2D diffuseMap, Texture2D specularMap, Texture2D normalMap, float textureScale)
        {
            this.device = device;
            this.height = height;

            plane = new Plane(Vector3.UnitY, height);

            this.diffuseMap = diffuseMap;
            this.specularMap = specularMap;
            this.normalMap = normalMap;
            this.textureScale = textureScale;

            numRows = 3;
            numCols = 3;

            GenerateGeometry();
        }

        private void GenerateGeometry()
        {
            float spacing = 1000.0f;

            // Create vertices
            numVertices = numRows * numCols;
            numIndices = 3 * ((numRows - 1) * (numCols - 1) * 2);

            VertexPosTex[] vertices = new VertexPosTex[numVertices];
            float halfWidth = (float)(numRows - 1) * spacing * 0.5f;
            float halfDepth = (float)(numCols - 1) * spacing * 0.5f;
            float du = 1.0f / (numRows - 1);
            float dv = 1.0f / (numCols - 1);
            for (int i = 0; i < numRows; i++)
            {
                float z = (halfDepth - i * spacing) * -1.0f;
                for (int j = 0; j < numCols; j++)
                {
                    float x = -halfWidth + j * spacing;
                    float y = height;

                    vertices[i * numCols + j].pos = new Vector3(x, y, z);
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

            // Create Vertex Buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTex.VertexLayout,
                numVertices, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create Index Buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                numIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }


        // PROPERTIES
        public float Height
        {
            get { return height; }
        }

        public Plane Plane
        {
            get { return plane; }
        }

        public int NumVertices
        {
            get { return numVertices; }
        }

        public int NumIndices
        {
            get { return numIndices; }
        }

        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }

        public IndexBuffer Indices
        {
            get { return indexBuffer; }
        }

        public Texture2D DiffuseMap
        {
            get { return diffuseMap; }
        }

        public Texture2D SpecularMap
        {
            get { return specularMap; }
        }

        public Texture2D NormalMap
        {
            get { return normalMap; }
        }

        public float TextureScale
        {
            get { return textureScale; }
        }
    }
}
