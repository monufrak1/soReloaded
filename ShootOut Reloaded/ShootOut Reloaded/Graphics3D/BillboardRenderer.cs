using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class BillboardRenderer
    {
        private GraphicsDevice device;
        private Effect effect;

        public BillboardRenderer(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;
        }

        public void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_ReflectionView"].SetValue(SharedEffectParameters.xReflectionViewMatrix);
            effect.Parameters["g_ReflectionProjection"].SetValue(SharedEffectParameters.xReflectionProjectionMatrix);
            effect.Parameters["g_EyePosW"].SetValue(SharedEffectParameters.xEyePosW);

            // Light data
            effect.Parameters["g_LightDir"].SetValue(SharedEffectParameters.xLightDirection);
            effect.Parameters["g_LightAmbient"].SetValue(SharedEffectParameters.xLightAmbient);
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
            effect.Parameters["g_LightSpecular"].SetValue(SharedEffectParameters.xLightSpecular);

            // Fog data
            effect.Parameters["g_FogStart"].SetValue(SharedEffectParameters.xFogStart);
            effect.Parameters["g_FogRange"].SetValue(SharedEffectParameters.xFogRange);
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        private void Draw(Billboard billboard, Vector3 cameraUp, Vector3 cameraForward, string techniqueName)
        {
            Matrix worldMtx = Matrix.CreateBillboard(billboard.Position,
                SharedEffectParameters.xEyePosW, cameraUp, cameraForward);

            // Set effect variables
            effect.Parameters["g_World"].SetValue(worldMtx);
            effect.Parameters["g_BillboardTexture"].SetValue(billboard.BillboardTexture);

            device.RasterizerState = RasterizerState.CullNone;

            device.SetVertexBuffer(billboard.Vertices);

            // Draw billboard
            effect.CurrentTechnique = effect.Techniques[techniqueName];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void Draw(Billboard billboard, Vector3 cameraUp, Vector3 cameraForward)
        {
            Draw(billboard, cameraUp, cameraForward, "BillboardTech");
        }

        public void DrawParticle(Billboard billboard, Vector3 cameraUp, Vector3 cameraForward)
        {
            Draw(billboard, cameraUp, cameraForward, "ParticleTech");
        }

        public void DrawLighting(Billboard billboard, Vector3 cameraUp, Vector3 cameraForward)
        {
            Draw(billboard, cameraUp, cameraForward, "BillboardLightingTech");
        }

        public void DrawOcclusion(Billboard billboard, Vector3 cameraUp, Vector3 cameraForward)
        {
            Draw(billboard, cameraUp, cameraForward, "BillboardOcclusionTech");
        }
    }
}