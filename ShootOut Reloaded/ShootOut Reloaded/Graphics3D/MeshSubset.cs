using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    class MeshSubset
    {
        // Geometry Data
        private int numVertices;
        private VertexBuffer vertexBuffer;
        private Vector3[] vertexPositions;
        private Matrix worldMatrix;             // Copy of entire Mesh's world Matrix

        // Texture Data
        private bool useTextures;
        private Texture2D diffuseMap;
        private Texture2D normalMap;
        private Texture2D specularMap;

        // Material Data
        private Vector4 reflectionMaterial;
        private Vector4 diffuseMaterial;
        private Vector4 specularMaterial;

        private string techniqueName;

        // Axis-Aligned Bounding Box
        private BoundingBox aabbModelSpace;
        private BoundingBox aabb;

        public MeshSubset(GraphicsDevice device, VertexPosTangNormTex[] vertices)
        {
            numVertices = vertices.Length;
            worldMatrix = Matrix.Identity;

            // Build vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosTangNormTex.VertexLayout,
                vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // Create vertex positions
            vertexPositions = new Vector3[numVertices];
            for(int i = 0; i < numVertices; i++)
            {
                vertexPositions[i] = vertices[i].pos;
            }

            // Build aabb
            Vector3 min = new Vector3(999999.0f, 999999.0f, 999999.0f);
            Vector3 max = new Vector3(-999999.0f, -999999.0f, -999999.0f);

            foreach (VertexPosTangNormTex vertex in vertices)
            {
                min = Vector3.Min(vertex.pos, min);
                max = Vector3.Max(vertex.pos, max);
            }

            aabb = new BoundingBox(min, max);
            aabbModelSpace = aabb;
        }

        public MeshSubset(MeshSubset meshSubset)
        {
            this.numVertices = meshSubset.numVertices;
            this.vertexBuffer = meshSubset.vertexBuffer;
            this.vertexPositions = meshSubset.vertexPositions;
            this.worldMatrix = Matrix.Identity; // Do not copy world matrix

            this.useTextures = meshSubset.useTextures;
            this.diffuseMap = meshSubset.diffuseMap;
            this.normalMap = meshSubset.normalMap;
            this.specularMap = meshSubset.specularMap;

            this.reflectionMaterial = meshSubset.reflectionMaterial;
            this.diffuseMaterial = meshSubset.diffuseMaterial;
            this.specularMaterial = meshSubset.specularMaterial;

            this.techniqueName = meshSubset.techniqueName;

            this.aabb = meshSubset.aabbModelSpace;
            this.aabbModelSpace = new BoundingBox(this.aabb.Min, this.aabb.Max);
        }

        public void UpdateAABB(Matrix worldMatrix)
        {
            // Update the world matrix for this subset
            this.worldMatrix = worldMatrix;

            // Get original AABB points
            Vector3[] corners = aabbModelSpace.GetCorners();
            Vector3[] transformedCorners = new Vector3[corners.Length];
            for(int i = 0; i < corners.Length; i++)
            {
                transformedCorners[i] = Vector3.Transform(corners[i], worldMatrix);
            }

            // Build new AABB
            aabb = BoundingBox.CreateFromPoints(transformedCorners);
        }

        public Plane? MeshCollision(BoundingBox otherAABB)
        {
            // Check if otherAABB intersects subset's AABB
            if (aabb.Intersects(otherAABB))
            {
                // Build array of planes to check collision against
                int length = vertexPositions.Length / 3;
                Plane[] planes = new Plane[length];
                BoundingBox[] triangleAABBs = new BoundingBox[length];

                // Create each plane
                Vector3[] worldSpacePositions = new Vector3[3];
                int triIndex = 0;
                for (int i = 0; i < planes.Length; i++)
                {
                    worldSpacePositions[0] = Vector3.Transform(vertexPositions[triIndex], worldMatrix);
                    worldSpacePositions[1] = Vector3.Transform(vertexPositions[triIndex + 1], worldMatrix);
                    worldSpacePositions[2] = Vector3.Transform(vertexPositions[triIndex + 2], worldMatrix);

                    // Create a plane from this triangle's world-space positions
                    planes[i] = new Plane(worldSpacePositions[0], worldSpacePositions[1], worldSpacePositions[2]);

                    // Create an AABB for each triangle
                    triangleAABBs[i] = BoundingBox.CreateFromPoints(worldSpacePositions);

                    triIndex += 3;
                }

                // Test collision on each triangle
                int index = 0;
                List<int> collisionIndices = new List<int>();
                foreach (BoundingBox b in triangleAABBs)
                {
                    if (b.Intersects(otherAABB))
                    {
                        collisionIndices.Add(index);
                    }

                    index++;
                }

                // Test collision on the planes
                foreach (int triangleIndex in collisionIndices)
                {
                    // Get this triangle's plane to test
                    if (planes[triangleIndex].Intersects(otherAABB) == PlaneIntersectionType.Intersecting)
                    {
                        return planes[triangleIndex];
                    }
                }

                return null;
            }

            // No collision
            return null;
        }

        public float? MeshCollision(Ray ray)
        {
            // Check if otherAABB intersects subset's AABB
            if (ray.Intersects(aabb).HasValue)
            {
                // Build array of planes to check collision against
                int length = vertexPositions.Length / 3;
                Plane[] planes = new Plane[length];
                BoundingBox[] triangleAABBs = new BoundingBox[length];

                // Create each plane
                Vector3[] worldSpacePositions = new Vector3[3];
                int triIndex = 0;
                for (int i = 0; i < planes.Length; i++)
                {
                    worldSpacePositions[0] = Vector3.Transform(vertexPositions[triIndex], worldMatrix);
                    worldSpacePositions[1] = Vector3.Transform(vertexPositions[triIndex + 1], worldMatrix);
                    worldSpacePositions[2] = Vector3.Transform(vertexPositions[triIndex + 2], worldMatrix);

                    // Create a plane from this triangle's world-space positions
                    planes[i] = new Plane(worldSpacePositions[0], worldSpacePositions[1], worldSpacePositions[2]);

                    // Create an AABB for each triangle
                    triangleAABBs[i] = BoundingBox.CreateFromPoints(worldSpacePositions);

                    triIndex += 3;
                }

                float shortestDist = float.MaxValue;

                // Test collision on each triangle
                int index = 0;
                List<int> collisionIndices = new List<int>();
                foreach (BoundingBox b in triangleAABBs)
                {
                    if (ray.Intersects(b).HasValue)
                    {
                        collisionIndices.Add(index);
                    }

                    index++;
                }

                // Test collision on the planes
                foreach (int triangleIndex in collisionIndices)
                {
                    // Test collision against this triangle's plane
                    if (ray.Intersects(planes[triangleIndex]).HasValue)
                    {
                        Vector3 center = Vector3.Subtract(triangleAABBs[triangleIndex].Max,
                                                            Vector3.Subtract(triangleAABBs[triangleIndex].Max,
                                                            triangleAABBs[triangleIndex].Min) * 0.5f);
                        float collisionDist = Vector3.Distance(center, ray.Position);
                        if (collisionDist < shortestDist)
                        {
                            shortestDist = collisionDist;
                        }
                    }
                }

                return shortestDist;
            }

            // No collision
            return null;
        }

        // PROPERTIES
        public int NumVertices
        {
            get { return numVertices; }
        }

        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }

        public bool UseTextures
        {
            get { return useTextures; }
            set { useTextures = value; }
        }

        public Texture2D DiffuseMap
        {
            get { return diffuseMap; }
            set { diffuseMap = value; }
        }

        public Texture2D NormalMap
        {
            get { return normalMap; }
            set { normalMap = value; }
        }

        public Texture2D SpecularMap
        {
            get { return specularMap; }
            set { specularMap = value; }
        }

        public Vector4 ReflectionMaterial
        {
            get { return reflectionMaterial; }
            set { reflectionMaterial = value; }
        }

        public Vector4 DiffuseMaterial
        {
            get { return diffuseMaterial; }
            set { diffuseMaterial = value; }
        }

        public Vector4 SpecularMaterial
        {
            get { return specularMaterial; }
            set { specularMaterial = value; }
        }

        public string TechniqueName
        {
            get { return techniqueName; }
            set { techniqueName = value; }
        }

        public BoundingBox AABB
        {
            get { return aabb; }
        }
    }
}
