using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class SurfaceRenderer
    {
        private GraphicsDevice device;
        private Effect effect;

        public SurfaceRenderer(GraphicsDevice device, Effect effect)
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

            // Water data
            effect.Parameters["g_WaterHeight"].SetValue(SharedEffectParameters.xWaterHeight);
            effect.Parameters["g_WaterColor"].SetValue(SharedEffectParameters.xWaterColor);
            effect.Parameters["g_DeepWaterFogDistance"].SetValue(SharedEffectParameters.xDeepWaterFogDistance);

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

        public void DrawSurface(SurfacePlane surface)
        {
            // Set effect variables
            UpdateEffectVariables();
            effect.Parameters["g_TexScale"].SetValue(surface.TextureScale);
            effect.Parameters["g_DiffMap"].SetValue(surface.DiffuseMap);
            effect.Parameters["g_SpecMap"].SetValue(surface.SpecularMap);
            effect.Parameters["g_NormMap"].SetValue(surface.NormalMap);

            // Set geometry data
            device.SetVertexBuffer(surface.Vertices);
            device.Indices = surface.Indices;

            // Draw surface
            if (SharedEffectParameters.xShadowMap != null)
            {
                effect.CurrentTechnique = effect.Techniques["SurfaceTech"];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["SurfaceNoShadowTech"];
            }

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, surface.NumVertices, 0, surface.NumIndices/3);
            }
        }

        public void DrawSurfaceOcclusion(SurfacePlane surface)
        {
            // Set effect variables
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);

            // Set geometry data
            device.SetVertexBuffer(surface.Vertices);
            device.Indices = surface.Indices;

            // Draw surface as occluder
            effect.CurrentTechnique = effect.Techniques["SurfaceOcclusionTech"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, surface.NumVertices, 0, surface.NumIndices / 3);
            }
        }
    }
}
