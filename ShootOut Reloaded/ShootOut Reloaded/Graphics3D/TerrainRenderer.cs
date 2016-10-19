using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class TerrainRenderer
    {
        private GraphicsDevice device;
        private Effect effect;

        public TerrainRenderer(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;
        }

        private void UpdateEffectVariables()
        {
            // Common data
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_ReflectionView"].SetValue(SharedEffectParameters.xReflectionViewMatrix);
            effect.Parameters["g_ReflectionProjection"].SetValue(SharedEffectParameters.xReflectionProjectionMatrix);
            effect.Parameters["g_EyePosW"].SetValue(SharedEffectParameters.xEyePosW);

            // Water data
            effect.Parameters["g_WaterHeight"].SetValue(SharedEffectParameters.xWaterHeight);
            effect.Parameters["g_DeepWaterFogDistance"].SetValue(SharedEffectParameters.xDeepWaterFogDistance);
            effect.Parameters["g_WaterColor"].SetValue(SharedEffectParameters.xWaterColor);

            // Light data
            effect.Parameters["g_LightDir"].SetValue(SharedEffectParameters.xLightDirection);
            effect.Parameters["g_LightAmbient"].SetValue(SharedEffectParameters.xLightAmbient);
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
            effect.Parameters["g_LightSpecular"].SetValue(SharedEffectParameters.xLightSpecular);
            effect.Parameters["g_LightView"].SetValue(SharedEffectParameters.xLightViewMatrix);
            effect.Parameters["g_LightProjection"].SetValue(SharedEffectParameters.xLightProjectionMatrix);
            effect.Parameters["g_ShadowMap"].SetValue(SharedEffectParameters.xShadowMap);

            // Fog data
            effect.Parameters["g_FogStart"].SetValue(SharedEffectParameters.xFogStart);
            effect.Parameters["g_FogRange"].SetValue(SharedEffectParameters.xFogRange);
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void Draw(TerrainGrid terrain)
        {
            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_LowLevelTexture"].SetValue(terrain.LowLevelTexture);
            effect.Parameters["g_HighLevelTexture"].SetValue(terrain.HighLevelTexture);
            effect.Parameters["g_TextureScale"].SetValue(terrain.TextureScale);

            // Draw terrain
            device.SetVertexBuffer(terrain.Vertices);
            device.Indices = terrain.Indices;

            if (SharedEffectParameters.xShadowMap != null)
            {
                effect.CurrentTechnique = effect.Techniques["TerrainTech"];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["TerrainNoShadowTech"];
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, terrain.NumVertices, 0, terrain.NumIndices / 3);
            }
        }

        public void DrawReflection(TerrainGrid terrain)
        {
            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_LowLevelTexture"].SetValue(terrain.LowLevelTexture);
            effect.Parameters["g_HighLevelTexture"].SetValue(terrain.HighLevelTexture);
            effect.Parameters["g_TextureScale"].SetValue(terrain.TextureScale);

            // Draw terrain reflection
            device.SetVertexBuffer(terrain.Vertices);
            device.Indices = terrain.Indices;

            effect.CurrentTechnique = effect.Techniques["TerrainReflectionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, terrain.NumVertices, 0, terrain.NumIndices / 3);
            }
        }

        public void DrawOcclusion(TerrainGrid terrain)
        {
            // Set effect variables
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);

            // Draw terrain occlusion
            device.SetVertexBuffer(terrain.Vertices);
            device.Indices = terrain.Indices;

            effect.CurrentTechnique = effect.Techniques["TerrainOcclusionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, terrain.NumVertices, 0, terrain.NumIndices / 3);
            }
        }
    }
}
