using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class SkyDome
    {
        private int numVertices;
        private int numIndices;
        private float radius;

        private Effect effect;

        private GraphicsDevice device;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        private RasterizerState skyRS;

        public SkyDome(GraphicsDevice device, Effect effect, float radius)
        {
            this.device = device;
            this.effect = effect;
            this.radius = radius;

            skyRS = new RasterizerState();
            skyRS.CullMode = CullMode.CullCounterClockwiseFace;

            GenerateDome();
        }

        private void GenerateDome()
        {
            float X = 0.525731f;
            float Z = 0.850651f;

            VertexPos[] vertices = new VertexPos[]
            {
                new VertexPos(new Vector3(-X, 0.0f, Z)),  new VertexPos(new Vector3(X, 0.0f, Z)),  
                new VertexPos(new Vector3(-X, 0.0f, -Z)), new VertexPos(new Vector3(X, 0.0f, -Z)),    
                new VertexPos(new Vector3(0.0f, Z, X)),   new VertexPos(new Vector3(0.0f, Z, -X)), 
                new VertexPos(new Vector3(0.0f, -Z, X)),  new VertexPos(new Vector3(0.0f, -Z, -X)),    
                new VertexPos(new Vector3(Z, X, 0.0f)),   new VertexPos(new Vector3(-Z, X, 0.0f)), 
                new VertexPos(new Vector3(Z, -X, 0.0f)),  new VertexPos(new Vector3(-Z, -X, 0.0f))
            };

            int[] indices = new int[] 
            {
                1,4,0,  4,9,0,  4,5,9,  8,5,4,  1,8,4,    
                1,10,8, 10,3,8, 8,3,5,  3,2,5,  3,7,2,    
                3,10,7, 10,6,7, 6,11,7, 6,0,11, 6,1,0, 
                10,1,6, 11,0,9, 2,11,9, 5,2,9,  11,2,7 
            };

            SubdivideGeometry(ref vertices, ref indices);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].pos.Normalize();
                vertices[i].pos *= radius;
            }

            numVertices = vertices.Length;
            numIndices = indices.Length;

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPos.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create index buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, 
                indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        private void SubdivideGeometry(ref VertexPos[] vertices, ref int[] indices)
        {
            List<VertexPos> newVertices = new List<VertexPos>();
            List<int> newIndices = new List<int>();

            for (int subdivide = 0; subdivide < 3; subdivide++)
            {
                List<VertexPos> vin = new List<VertexPos>(vertices);
                List<int> iin = new List<int>(indices);

                //       v1
                //       *
                //      / \
                //     /   \
                //  m0*-----*m1
                //   / \   / \
                //  /   \ /   \
                // *-----*-----*
                // v0    m2     v2

                int numTris = iin.ToArray().Length / 3;
                for (int i = 0; i < numTris; ++i)
                {
                    Vector3 v0 = vin[iin[i * 3 + 0]].pos;
                    Vector3 v1 = vin[iin[i * 3 + 1]].pos;
                    Vector3 v2 = vin[iin[i * 3 + 2]].pos;

                    Vector3 m0 = 0.5f * (v0 + v1);
                    Vector3 m1 = 0.5f * (v1 + v2);
                    Vector3 m2 = 0.5f * (v0 + v2);

                    newVertices.Add(new VertexPos(v0)); // 0
                    newVertices.Add(new VertexPos(v1)); // 1
                    newVertices.Add(new VertexPos(v2)); // 2
                    newVertices.Add(new VertexPos(m0)); // 3
                    newVertices.Add(new VertexPos(m1)); // 4
                    newVertices.Add(new VertexPos(m2)); // 5

                    newIndices.Add(i * 6 + 0);
                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 5);

                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 4);
                    newIndices.Add(i * 6 + 5);

                    newIndices.Add(i * 6 + 5);
                    newIndices.Add(i * 6 + 4);
                    newIndices.Add(i * 6 + 2);

                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 1);
                    newIndices.Add(i * 6 + 4);
                }

                vertices = newVertices.ToArray();
                indices = newIndices.ToArray();
            }
        }

        private void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_EyePosW"].SetValue(SharedEffectParameters.xEyePosW);
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
           
            // Sky color data
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void Draw()
        {
            // Set new RasterizerState
            RasterizerState originalRS = device.RasterizerState;
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            
            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyTech"];

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices/3);
            }

            // Reset original RasterizerState
            device.RasterizerState = originalRS;
        }

        public void DrawReflection()
        {
            // Set new RasterizerState
            RasterizerState originalRS = device.RasterizerState;
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyReflectionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices / 3);
            }

            // Reset original RasterizerState
            device.RasterizerState = originalRS;
        }
    }

    public class TexturedSkyDome
    {
        private GraphicsDevice device;
        private Effect effect;

        private RasterizerState skyRS;

        private Texture2D skyTexture;
        private float radius;
        private float textureScale;

        private int numVertices;
        private int numIndices;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        public TexturedSkyDome(GraphicsDevice device, Effect effect, Texture2D skyTexture,
                                float textureScale, float radius)
        {
            this.device = device;
            this.effect = effect;
            this.skyTexture = skyTexture;
            this.textureScale = textureScale;
            this.radius = radius;

            skyRS = new RasterizerState();
            skyRS.CullMode = CullMode.None;

            effect.Parameters["g_SkyTexture"].SetValue(skyTexture);

            GenerateGeometry();
        }

        private void GenerateGeometry()
        {
            int numStacks = 30;
            int numSlices = 30;

            List<VertexPosTex> vertexList = new List<VertexPosTex>();
            List<int> indexList = new List<int>();

            float phiStep = (float)(MathHelper.Pi/numStacks);

            // do not count the poles as rings
            int numRings = numStacks - 1;

            // Compute vertices for each stack ring.
            for (int i = 1; i <= numRings; ++i)
            {
                float phi = i * phiStep;

                // vertices of ring
                float thetaStep = 2.0f * (float)(MathHelper.Pi / numSlices);
                for (int j = 0; j <= numSlices; ++j)
                {
                    float theta = j * thetaStep;

                    VertexPosTex v = new VertexPosTex();

                    // spherical to cartesian
                    v.pos.X = radius * (float)Math.Sin(phi) * (float)Math.Cos(theta);
                    v.pos.Y = radius * (float)Math.Cos(phi);
                    v.pos.Z = radius * (float)Math.Sin(phi) * (float)Math.Sin(theta);

                    v.tex.X = theta / (2.0f * MathHelper.Pi);
                    v.tex.Y = phi / MathHelper.Pi;

                    vertexList.Add(v);
                }
            }

            // poles: note that there will be texture coordinate distortion
            vertexList.Add(new VertexPosTex(new Vector3(0.0f, -radius, 0.0f),
                new Vector2(0.0f, 1.0f)));
            vertexList.Add(new VertexPosTex(new Vector3(0.0f, radius, 0.0f),
                new Vector2(0.0f, 0.0f)));

            int northPoleIndex = vertexList.ToArray().Length - 1;
            int southPoleIndex = vertexList.ToArray().Length - 2;

            int numRingVertices = numSlices + 1;

            // Compute indices for inner stacks (not connected to poles).
            for (int i = 0; i < numStacks - 2; ++i)
            {
                for (int j = 0; j < numSlices; ++j)
                {
                    indexList.Add(i * numRingVertices + j);
                    indexList.Add(i * numRingVertices + j + 1);
                    indexList.Add((i + 1) * numRingVertices + j);

                    indexList.Add((i + 1) * numRingVertices + j);
                    indexList.Add(i * numRingVertices + j + 1);
                    indexList.Add((i + 1) * numRingVertices + j + 1);
                }
            }

            // Compute indices for top stack.  The top stack was written 
            // first to the vertex buffer.
            for (int i = 0; i < numSlices; ++i)
            {
                indexList.Add(northPoleIndex);
                indexList.Add(i + 1);
                indexList.Add(i);
            }

            // Compute indices for bottom stack.  The bottom stack was written
            // last to the vertex buffer, so we need to offset to the index
            // of first vertex in the last ring.
            int baseIndex = (numRings - 1) * numRingVertices;
            for (int i = 0; i < numSlices; ++i)
            {
                indexList.Add(southPoleIndex);
                indexList.Add(baseIndex + i);
                indexList.Add(baseIndex + i + 1);
            }

            VertexPosTex[] vertices = vertexList.ToArray();
            int[] indices = indexList.ToArray();

            numVertices = vertices.Length;
            numIndices = indices.Length;

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTex.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create index buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        private void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_Time"].SetValue(SharedEffectParameters.xTime);
            effect.Parameters["g_EyePosW"].SetValue(SharedEffectParameters.xEyePosW);
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);

            // Light data
            effect.Parameters["g_LightDir"].SetValue(SharedEffectParameters.xLightDirection);
            effect.Parameters["g_LightAmbient"].SetValue(SharedEffectParameters.xLightAmbient);
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
            effect.Parameters["g_LightSpecular"].SetValue(SharedEffectParameters.xLightSpecular);

            // Sky color data
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void Draw()
        {
            // Set new RasterizerState
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TextureScale"].SetValue(textureScale);

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyTexturedTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices / 3);
            }

            // Reset original RasterizerState
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void DrawClouds()
        {
            // Set new RasterizerState
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TextureScale"].SetValue(textureScale);
            effect.Parameters["g_ReflView"].SetValue(SharedEffectParameters.xReflectionViewMatrix);
            effect.Parameters["g_ReflProjection"].SetValue(SharedEffectParameters.xReflectionProjectionMatrix);

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyTexturedCloudsTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices / 3);
            }

            // Reset original RasterizerState
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void DrawCloudsReflection()
        {
            // Set new RasterizerState
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TextureScale"].SetValue(textureScale);

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            
            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyTexturedCloudsReflectionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices / 3);
            }

            // Reset original RasterizerState
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void DrawOcclusion()
        {
            // Set new RasterizerState
            device.RasterizerState = skyRS;

            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TextureScale"].SetValue(textureScale);

            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Draw SkyDome
            effect.CurrentTechnique = effect.Techniques["SkyTexturedOcclusionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numVertices, 0, numIndices / 3);
            }

            // Reset original RasterizerState
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }
    }
}
