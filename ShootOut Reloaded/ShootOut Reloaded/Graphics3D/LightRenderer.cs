using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class LightRenderer
    {
        private GraphicsDevice device;
        private Effect effect;

        private RasterizerState lightRS;
        private DepthStencilState lightDSS;

        public LightRenderer(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;

            lightRS = new RasterizerState();
            lightRS.CullMode = CullMode.None;

            lightDSS = new DepthStencilState();
            lightDSS.DepthBufferEnable = false;
        }

        private void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);

            // Light data
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
        }

        public void Draw(Light light, Vector3 cameraUp, Vector3 cameraLook)
        {
            Matrix world = Matrix.CreateBillboard(light.Position, SharedEffectParameters.xEyePosW,
                cameraUp, cameraLook);

            // Update effect variables
            UpdateEffectVariables();
            effect.Parameters["g_World"].SetValue(world);
            effect.Parameters["g_Size"].SetValue(light.Size);
            effect.Parameters["g_LightTexture"].SetValue(light.LightTexture);

            // Disable rasterizer and depth buffer
            RasterizerState originalRS = device.RasterizerState;
            DepthStencilState originalDSS = device.DepthStencilState;

            device.RasterizerState = lightRS;
            device.DepthStencilState = lightDSS;

            // Draw the light
            device.SetVertexBuffer(light.Vertices);

            effect.CurrentTechnique = effect.Techniques["LightTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            // Reset original RasterizerState and DepthStencilState
            device.RasterizerState = originalRS;
            device.DepthStencilState = originalDSS;
        }
    }
}
