using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics3D
{
    abstract class TerrainGrid
    {
        protected static float HEIGHT_OFFSET = -10.0f;

        protected int numRows;
        protected int numCols;
        protected int numVertices;
        protected int numIndices;

        private float spacing;

        protected float[] heightValues;
        protected Vector3[] normalValues;

        private float textureScale;

        private Texture2D lowLevelTexture;
        private Texture2D highLevelTexture;

        private GraphicsDevice device;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        public TerrainGrid(GraphicsDevice device, Texture2D lowLevelTexture, Texture2D highLevelTexture,
                          float textureScale, float spacing)
        {
            this.device = device;
            this.textureScale = textureScale;
            this.spacing = spacing;

            this.lowLevelTexture = lowLevelTexture;
            this.highLevelTexture = highLevelTexture;

            heightValues = new float[0];
            normalValues = new Vector3[0];
        }

        protected void GenerateGeometry()
        {
            // Create vertices
            numVertices = numRows * numCols;
            numIndices = 3 * ((numRows - 1) * (numCols - 1) * 2);

            VertexPosNormTex[] vertices = new VertexPosNormTex[numVertices];
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
                    float y = heightValues[i * numCols + j];

                    vertices[i * numCols + j].pos = new Vector3(x, y, z);
                    vertices[i * numCols + j].norm = Vector3.Zero;
                    vertices[i * numCols + j].tex.X = du * j;
                    vertices[i * numCols + j].tex.Y = dv * i;
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

            // Calculate vertex normals
            for (int i = 0; i < numIndices; i += 3)
            {
                Vector3 v0 = vertices[indices[i]].pos;
                Vector3 v1 = vertices[indices[i + 1]].pos;
                Vector3 v2 = vertices[indices[i + 2]].pos;

                Vector3 e0 = v1 - v0;
                Vector3 e1 = v2 - v0;

                Vector3 norm = Vector3.Cross(e1, e0);

                norm.Normalize();

                // Update vertex values
                vertices[indices[i]].norm += norm;
                vertices[indices[i + 1]].norm += norm;
                vertices[indices[i + 2]].norm += norm;
            }

            // Normalize all vectors
            normalValues = new Vector3[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                vertices[i].norm.Normalize();
                normalValues[i] = vertices[i].norm;
            }

            // Create Vertex Buffer
            vertexBuffer = new VertexBuffer(device, VertexPosNormTex.VertexLayout,
                numVertices, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create Index Buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                numIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        public float GetHeight(float x, float z)
        {
            // Transform from terrain local space to "cell" space
            float c = (x + 0.5f * (numCols - 1) * spacing) / spacing;
            float d = (z + 0.5f * (numRows - 1) * spacing) / spacing;

            // Get the row and column
            int row = (int)Math.Floor(d);
            int col = (int)Math.Floor(c);

            if (row <= 0 || row >= numRows ||
                col <= 0 || col >= numCols)
            {
                // Out of bounds
                return HEIGHT_OFFSET;
            }

            // Grab the heights of the cell
            // A*--*B
            //  | /|
            //  |/ |
            // C*--*D
            float A, B, C, D;

            try
            {
                A = heightValues[row * numCols + col];
                B = heightValues[row * numCols + col + 1];
                C = heightValues[(row + 1) * numCols + col];
                D = heightValues[(row + 1) * numCols + col + 1];
            }
            catch (IndexOutOfRangeException e)
            {
                return HEIGHT_OFFSET;
            }

            // Relative to the cell
            float s = c - (float)col;
            float t = d - (float)row;

            // If upper triangle ABC
            if (s + t <= 1.0f)
            {
                float uy = B - A;
                float vy = C - A;
                return A + s * uy + t * vy;
            }
            else
            {
                // Lower triangle DCB
                float uy = C - D;
                float vy = B - D;
                return D + (1.0f - s) * uy + (1.0f - t) * vy;
            }
        }

        public bool IsSteepIncline(float x, float z, float angleRatio)
        {
            // Transform from terrain local space to "cell" space
            float c = (x + 0.5f * (numCols - 1) * spacing) / spacing;
            float d = (z + 0.5f * (numRows - 1) * spacing) / spacing;

            // Get the row and column
            int row = (int)Math.Floor(d);
            int col = (int)Math.Floor(c);

            if (row <= 0 || row >= numRows ||
                col <= 0 || col >= numCols)
            {
                // Out of bounds
                return false;
            }

            // Grab the heights of the cell
            // A*--*B
            //  | /|
            //  |/ |
            // C*--*D
            Vector3 normA, normD;

            try
            {
                normA = normalValues[row * numCols + col];
                normD = normalValues[(row + 1) * numCols + col + 1];
            }
            catch (IndexOutOfRangeException e)
            {
                return false;
            }

            // Relative to the cell
            float s = c - (float)col;
            float t = d - (float)row;

            // If upper triangle ABC
            if (s + t <= 1.0f)
            {
                if (normA.Y < angleRatio)
                {
                    return true;
                }
            }
            else
            {
                // Lower triangle DCB
                if (normD.Y < angleRatio)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetSlopeDirection(float x, float z)
        {
            // Transform from terrain local space to "cell" space
            float c = (x + 0.5f * (numCols - 1) * spacing) / spacing;
            float d = (z + 0.5f * (numRows - 1) * spacing) / spacing;

            // Get the row and column
            int row = (int)Math.Floor(d);
            int col = (int)Math.Floor(c);

            if (row <= 0 || row >= numRows ||
                col <= 0 || col >= numCols)
            {
                // Out of bounds
                return Vector3.Zero;
            }

            // Grab the heights of the cell
            // A*--*B
            //  | /|
            //  |/ |
            // C*--*D
            Vector3 normA, normD;

            try
            {
                normA = normalValues[row * numCols + col];
                normD = normalValues[(row + 1) * numCols + col + 1];
            }
            catch (IndexOutOfRangeException e)
            {
                return Vector3.Zero;
            }

            // Relative to the cell
            float s = c - (float)col;
            float t = d - (float)row;

            // If upper triangle ABC
            if (s + t <= 1.0f)
            {
                Vector3 forward = normA;
                forward.Y = 0.0f;
                forward.Normalize();

                Vector3 right = Vector3.Cross(forward, Vector3.Up);
                Matrix rotation = Matrix.CreateFromAxisAngle(right, MathHelper.ToRadians(-90.0f));

                Vector3 direction = Vector3.TransformNormal(normA, rotation);

                // Prevent upward slope
                if(direction.Y > 0.0f)
                {
                    direction = -direction;
                }

                return direction;
            }
            else
            {
                Vector3 forward = normD;
                forward.Y = 0.0f;
                forward.Normalize();

                Vector3 right = Vector3.Cross(forward, Vector3.Up);
                Matrix rotation = Matrix.CreateFromAxisAngle(right, MathHelper.ToRadians(-90.0f));

                Vector3 direction = Vector3.TransformNormal(normA, rotation);

                // Prevent upward slope
                if (direction.Y > 0.0f)
                {
                    direction = -direction;
                }

                return direction;
            }
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

        public float TextureScale
        {
            get { return textureScale; }
        }

        public Texture2D LowLevelTexture
        {
            get { return lowLevelTexture; }
            set { lowLevelTexture = value; }
        }

        public Texture2D HighLevelTexture
        {
            get { return highLevelTexture; }
            set { highLevelTexture = value; }
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
