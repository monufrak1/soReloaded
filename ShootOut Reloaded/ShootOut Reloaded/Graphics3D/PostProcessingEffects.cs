using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class PostProcessingEffects
    {
        private GraphicsDevice device;
        private Effect effect;

        private VertexBuffer vertexBuffer;

        public PostProcessingEffects(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;

            GenerateVertices();
        }

        private void GenerateVertices()
        {
            // Create full screen quad vertices in screen space
            VertexPosTex[] vertices = new VertexPosTex[]
            {
                new VertexPosTex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 1.0f)),
                new VertexPosTex(new Vector3(-1.0f,  1.0f, 0.0f), new Vector2(0.0f, 0.0f)),
                new VertexPosTex(new Vector3( 1.0f, -1.0f, 0.0f), new Vector2(1.0f, 1.0f)),

                new VertexPosTex(new Vector3( 1.0f, -1.0f, 0.0f), new Vector2(1.0f, 1.0f)),
                new VertexPosTex(new Vector3(-1.0f,  1.0f, 0.0f), new Vector2(0.0f, 0.0f)),
                new VertexPosTex(new Vector3( 1.0f,  1.0f, 0.0f), new Vector2(1.0f, 0.0f))
            };

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTex.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);
        }

        public void DrawLightScattering(Texture2D frameBuffer, Texture2D lightOcclusionMap)
        {
            // Set effect variables
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_LightPosition"].SetValue(SharedEffectParameters.xLightPosition);
            effect.Parameters["g_FrameBuffer"].SetValue(frameBuffer);
            effect.Parameters["g_LightOcclusionMap"].SetValue(lightOcclusionMap);

            // Draw full screen quad
            device.SetVertexBuffer(vertexBuffer);

            effect.CurrentTechnique = effect.Techniques["LightScatteringTech"];
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }

        public void DrawLightScattering(Texture2D lightOcclusionMap)
        {
            // Set effect variables
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_LightPosition"].SetValue(SharedEffectParameters.xLightPosition);
            effect.Parameters["g_LightOcclusionMap"].SetValue(lightOcclusionMap);

            // Draw full screen quad
            device.SetVertexBuffer(vertexBuffer);

            effect.CurrentTechnique = effect.Techniques["LightScatteringTech3"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }

        public void ApplyBloom(Texture2D frameBuffer, Texture2D bloomMap)
        {
            // Set effect variables
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_LightPosition"].SetValue(SharedEffectParameters.xLightPosition);
            effect.Parameters["g_FrameBuffer"].SetValue(frameBuffer);
            effect.Parameters["g_BloomMap"].SetValue(bloomMap);

            // Draw full screen quad
            device.SetVertexBuffer(vertexBuffer);

            effect.CurrentTechnique = effect.Techniques["BloomTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }

        public void DrawCopy(Texture2D texToCopy)
        {
            effect.Parameters["g_FrameBuffer"].SetValue(texToCopy);

            // Draw full screen quad
            device.SetVertexBuffer(vertexBuffer);

            effect.CurrentTechnique = effect.Techniques["CopyUseAlphaTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }

        public void DrawCopyOcclusion(Texture2D texToCopy)
        {
            effect.Parameters["g_FrameBuffer"].SetValue(texToCopy);

            // Draw full screen quad
            device.SetVertexBuffer(vertexBuffer);

            effect.CurrentTechnique = effect.Techniques["CopyOcclusionTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }
    }
}
