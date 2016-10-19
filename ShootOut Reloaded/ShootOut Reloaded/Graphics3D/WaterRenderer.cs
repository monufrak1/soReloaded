using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class WaterRenderer
    {
        private GraphicsDevice device;
        private Effect effect;
        private SpriteBatch spriteBatch;

        private RasterizerState waterRS;

        private RenderTarget2D refractionMap;
        private Rectangle screenRect;

        public WaterRenderer(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;

            spriteBatch = new SpriteBatch(device);

            waterRS = new RasterizerState();
            waterRS.CullMode = CullMode.None;

            // Create refraction map and data in memory
            refractionMap = new RenderTarget2D(device, device.Viewport.Width, device.Viewport.Height,
                false, SurfaceFormat.Color, DepthFormat.None);

            screenRect = new Rectangle(0, 0, refractionMap.Width, refractionMap.Height);
        }

        private void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_Time"].SetValue(SharedEffectParameters.xTime);
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

            // Water data
            effect.Parameters["g_WaterHeight"].SetValue(SharedEffectParameters.xWaterHeight);
            effect.Parameters["g_WaterColor"].SetValue(SharedEffectParameters.xWaterColor);

            // Fog data
            effect.Parameters["g_FogStart"].SetValue(SharedEffectParameters.xFogStart);
            effect.Parameters["g_FogRange"].SetValue(SharedEffectParameters.xFogRange);
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void Draw(WaterGrid water, Texture2D reflectionMap, RenderTarget2D refractionRT)
        {
            // Copy data from refractionRT to a texture
            device.SetRenderTarget(refractionMap);
            spriteBatch.Begin();

            spriteBatch.Draw(refractionRT, screenRect, Color.White);

            spriteBatch.End();
            device.SetRenderTarget(refractionRT);
            device.DepthStencilState = DepthStencilState.Default;

            // Set new rasterizer state
            RasterizerState originalRS = device.RasterizerState;
            device.RasterizerState = waterRS;

            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TransparencyRatio"].SetValue(water.TransparencyRatio);
            effect.Parameters["g_ReflAmount"].SetValue(water.ReflectionAmount);
            effect.Parameters["g_RefrAmount"].SetValue(water.RefractionAmount);
            effect.Parameters["g_WaveHeight"].SetValue(water.WaveHeight);
            effect.Parameters["g_WaveSpeed"].SetValue(water.WaveSpeed);
            effect.Parameters["g_WaterNormalMap"].SetValue(water.WaterNormalTexture);
            effect.Parameters["g_ReflectionMap"].SetValue(reflectionMap);
            effect.Parameters["g_RefractionMap"].SetValue(refractionMap);

            // Draw water
            device.SetVertexBuffer(water.Vertices);
            device.Indices = water.Indices;

            effect.CurrentTechnique = effect.Techniques["WaterTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, water.NumVertices, 0, water.NumIndices / 3);
            }

            // Reset original rasterizer state
            device.RasterizerState = originalRS;
        }
    }
}
