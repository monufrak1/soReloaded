using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics3D
{
    class MeshRenderer
    {
        private GraphicsDevice device;
        private Effect effect;

        private Texture2D currentlySetDiffuseMap;
        private Texture2D currentlySetNormalMap;
        private Texture2D currentlySetSpecularMap;

        public MeshRenderer(GraphicsDevice device, Effect effect)
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
            effect.Parameters["g_DeepWaterFogDistance"].SetValue(SharedEffectParameters.xDeepWaterFogDistance);
            effect.Parameters["g_WaterColor"].SetValue(SharedEffectParameters.xWaterColor);

            // Light data
            effect.Parameters["g_LightDir"].SetValue(SharedEffectParameters.xLightDirection);
            effect.Parameters["g_LightAmbient"].SetValue(SharedEffectParameters.xLightAmbient);
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
            effect.Parameters["g_LightSpecular"].SetValue(SharedEffectParameters.xLightSpecular);

            if (SharedEffectParameters.xShadowsEnabled)
            {
                effect.Parameters["g_ShadowMap"].SetValue(SharedEffectParameters.xShadowMap);
                effect.Parameters["g_LightView"].SetValue(SharedEffectParameters.xLightViewMatrix);
                effect.Parameters["g_LightProjection"].SetValue(SharedEffectParameters.xLightProjectionMatrix);
            }
            else
            {
                effect.Parameters["g_LightView"].SetValue(Matrix.Identity);
                effect.Parameters["g_LightProjection"].SetValue(Matrix.Identity);
            }

            // Fog data
            effect.Parameters["g_FogStart"].SetValue(SharedEffectParameters.xFogStart);
            effect.Parameters["g_FogRange"].SetValue(SharedEffectParameters.xFogRange);
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void DrawMesh(Mesh mesh, BoundingFrustum viewFrustum)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);

            // Draw each individual mesh subset
            foreach (MeshSubset meshSubset in mesh.Subsets)
            {
                // Draw subset if it is contained within view frustum
                if(viewFrustum.Intersects(meshSubset.AABB))
                {
                    // Set the technique
                    effect.CurrentTechnique = effect.Techniques[meshSubset.TechniqueName];
                    
                    DrawMeshSubset(meshSubset);
                }
            }
        }

        public void DrawMesh(Mesh mesh)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);
            
            // Draw each individual mesh subset
            foreach(MeshSubset meshSubset in mesh.Subsets)
            {
                // Set the technique
                effect.CurrentTechnique = effect.Techniques[meshSubset.TechniqueName];

                DrawMeshSubset(meshSubset);
            }
        }

        public void DrawMeshOcclusion(Mesh mesh, BoundingFrustum viewFrustum)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);

            // Set the technique
            effect.CurrentTechnique = effect.Techniques["MeshOcclusionTech"];

            // Draw each individual mesh subset
            foreach (MeshSubset meshSubset in mesh.Subsets)
            {
                // Draw subset if it is contained within view frustum
                if (viewFrustum.Intersects(meshSubset.AABB))
                {
                    DrawMeshSubsetOcclusion(meshSubset);
                }
            }
        }

        public void DrawMeshOcclusion(Mesh mesh)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);

            // Set the technique
            effect.CurrentTechnique = effect.Techniques["MeshOcclusionTech"];

            // Draw each individual mesh subset
            foreach (MeshSubset meshSubset in mesh.Subsets)
            {
                // Draw subset
                DrawMeshSubsetOcclusion(meshSubset);
            }
        }

        public void DrawMeshShadow(Mesh mesh, BoundingFrustum lightViewFrustum)
        {
            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = BlendState.Opaque;

            // Set effect variables
            effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);

            // Set the technique
            effect.CurrentTechnique = effect.Techniques["MeshShadowTech"];

            // Draw each individual mesh subset
            foreach (MeshSubset meshSubset in mesh.Subsets)
            {
                if (lightViewFrustum.Intersects(meshSubset.AABB))
                {
                    // Draw subset
                    DrawMeshSubsetShadow(meshSubset);
                }
            }

            device.BlendState = BlendState.AlphaBlend;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void DrawMeshReflection(Mesh mesh, BoundingFrustum viewFrustum, float reflectionPlaneHeight)
        {
            // Only draw reflection if the mesh is above the given height
            if (mesh.AABB.Max.Y > reflectionPlaneHeight)
            {
                // Set effect variables
                effect.Parameters["g_World"].SetValue(mesh.WorldMatrix);

                // Set the technique
                effect.CurrentTechnique = effect.Techniques["MeshReflectionTech"];

                // Draw each individual mesh subset
                foreach (MeshSubset meshSubset in mesh.Subsets)
                {
                    // Draw subset if it is contained within view frustum
                    if (viewFrustum.Intersects(meshSubset.AABB))
                    {
                        DrawMeshSubsetReflection(meshSubset);
                    }
                }
            }
        }

        private void DrawMeshSubset(MeshSubset meshSubset)
        {
            // Set effect variables for this subset
            effect.Parameters["g_UseTextures"].SetValue(meshSubset.UseTextures);
            if (meshSubset.UseTextures)
            {
                // Set textures if needed
                if (!meshSubset.DiffuseMap.Equals(currentlySetDiffuseMap))
                {
                    effect.Parameters["g_DiffMap"].SetValue(meshSubset.DiffuseMap);
                    currentlySetDiffuseMap = meshSubset.DiffuseMap;
                }

                if (!meshSubset.DiffuseMap.Equals(currentlySetNormalMap))
                {
                    effect.Parameters["g_NormMap"].SetValue(meshSubset.NormalMap);
                    currentlySetNormalMap = meshSubset.NormalMap;
                }

                if (!meshSubset.DiffuseMap.Equals(currentlySetSpecularMap))
                {
                    effect.Parameters["g_SpecMap"].SetValue(meshSubset.SpecularMap);
                    currentlySetSpecularMap = meshSubset.SpecularMap;
                }
            }
            effect.Parameters["g_DiffuseMaterial"].SetValue(meshSubset.DiffuseMaterial);
            effect.Parameters["g_SpecularMaterial"].SetValue(meshSubset.SpecularMaterial);

            // Set vertex buffer
            device.SetVertexBuffer(meshSubset.Vertices);

            // Draw
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, meshSubset.NumVertices/3);
            }
        }

        private void DrawMeshSubsetOcclusion(MeshSubset meshSubset)
        {
            // Set effect variables for this subset
            effect.Parameters["g_UseTextures"].SetValue(meshSubset.UseTextures);
            if (meshSubset.UseTextures)
            {
                // Set texture if needed
                if (!meshSubset.DiffuseMap.Equals(currentlySetDiffuseMap))
                {
                    effect.Parameters["g_DiffMap"].SetValue(meshSubset.DiffuseMap);
                    currentlySetDiffuseMap = meshSubset.DiffuseMap;
                }
            }

            // Set vertex buffer
            device.SetVertexBuffer(meshSubset.Vertices);

            // Draw
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, meshSubset.NumVertices / 3);
            }
        }

        private void DrawMeshSubsetShadow(MeshSubset meshSubset)
        {
            // Set effect variables for this subset
            effect.Parameters["g_UseTextures"].SetValue(meshSubset.UseTextures);
            if (meshSubset.UseTextures)
            {
                // Set texture if needed
                if (!meshSubset.DiffuseMap.Equals(currentlySetDiffuseMap))
                {
                    effect.Parameters["g_DiffMap"].SetValue(meshSubset.DiffuseMap);
                    currentlySetDiffuseMap = meshSubset.DiffuseMap;
                }
            }

            // Set vertex buffer
            device.SetVertexBuffer(meshSubset.Vertices);

            // Draw
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, meshSubset.NumVertices / 3);
            }
        }

        private void DrawMeshSubsetReflection(MeshSubset meshSubset)
        {
            // Set effect variables for this subset
            effect.Parameters["g_UseTextures"].SetValue(meshSubset.UseTextures);
            if (meshSubset.UseTextures)
            {
                // Set texture if needed
                if (!meshSubset.DiffuseMap.Equals(currentlySetDiffuseMap))
                {
                    effect.Parameters["g_DiffMap"].SetValue(meshSubset.DiffuseMap);
                    currentlySetDiffuseMap = meshSubset.DiffuseMap;
                }
            }
            effect.Parameters["g_DiffuseMaterial"].SetValue(meshSubset.DiffuseMaterial);
            effect.Parameters["g_SpecularMaterial"].SetValue(meshSubset.SpecularMaterial);

            // Set vertex buffer
            device.SetVertexBuffer(meshSubset.Vertices);

            // Draw
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, meshSubset.NumVertices / 3);
            }
        }
    }
}
