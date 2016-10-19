using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections;

namespace Graphics3D
{
    class Mesh
    {
        class MaterialData
        {
            public bool useTextures = true;
            public Texture2D diffuseMap;
            public Texture2D specularMap;
            public Texture2D normalMap;

            public Vector4 reflectionMaterial = Vector4.Zero;

            public Vector4 diffuseMaterial = Vector4.Zero;
            public Vector4 specularMaterial = Vector4.Zero;

            public string techniqueName;
        }

        private string meshFileName;

        // Geometry data
        private Vector3 position;
        private Vector3 rotationAngles;
        private float scale;

        private Matrix worldMatrix;
        private BoundingBox aabb; // Bounding Box contains entire mesh

        private List<MeshSubset> subsets;

        public Mesh(GraphicsDevice device, ContentManager content, string meshFileName,
                    Vector3 position, Vector3 rotationAngles, float scale)
        {
            this.meshFileName = meshFileName;
            this.position = position;
            this.rotationAngles = rotationAngles;
            this.scale = scale;

            LoadMeshFromFile(device, content, meshFileName);
            UpdateWorldMatrix();
        }

        public Mesh(Mesh mesh)
        {
            this.meshFileName = mesh.meshFileName;
            this.position = Vector3.Zero;
            this.rotationAngles = Vector3.Zero;
            this.scale = 1.0f;

            // Copy each subset
            this.subsets = new List<MeshSubset>();
            foreach(MeshSubset meshSubset in mesh.subsets)
            {
                this.subsets.Add(new MeshSubset(meshSubset));
            }

            UpdateWorldMatrix();
        }

        private void LoadMeshFromFile(GraphicsDevice device, ContentManager content, string meshFileName)
        {
            subsets = new List<MeshSubset>();

            int numMaterials;
            int numVertices;
            int numTriangles;

            MaterialData[] materials;

            StreamReader fileReader = new StreamReader(meshFileName);
            try
            {
                string buffer;
                char[] separator = { ' ' };
                string[] splitLine;

                // Read in file header information
                fileReader.ReadLine();

                // Num Materials
                buffer = fileReader.ReadLine();
                splitLine = buffer.Split(separator);
                numMaterials = int.Parse(splitLine[1]);

                // Num Vertices
                buffer = fileReader.ReadLine();
                splitLine = buffer.Split(separator);
                numVertices = int.Parse(splitLine[1]);

                // Num Triangles
                buffer = fileReader.ReadLine();
                splitLine = buffer.Split(separator);
                numTriangles = int.Parse(splitLine[1]);

                // Read in the material data
                fileReader.ReadLine();
                fileReader.ReadLine();

                materials = new MaterialData[numMaterials];
                int materialIndex = 0;
                string texturePath = @"Textures\";
                for(int i = 0; i < numMaterials; i++)
                {
                    materials[materialIndex] = new MaterialData();

                    // Diffuse Map
                    buffer = fileReader.ReadLine();
                    if (buffer != "NO_TEXTURE")
                    {
                        buffer = buffer.Remove(buffer.Length - 4); // Remove the file extension
                        materials[materialIndex].diffuseMap = content.Load<Texture2D>(texturePath + buffer);
                    }
                    else
                    {
                        // This material will render using material colors only
                        materials[materialIndex].useTextures = false;
                    }

                    // Specular Map
                    buffer = fileReader.ReadLine();
                    if (buffer != "NO_TEXTURE")
                    {
                        buffer = buffer.Remove(buffer.Length - 4); // Remove the file extension
                        materials[materialIndex].specularMap = content.Load<Texture2D>(texturePath + buffer);
                    }

                    // Normal Map
                    buffer = fileReader.ReadLine();
                    if (buffer != "NO_TEXTURE")
                    {
                        buffer = buffer.Remove(buffer.Length - 4); // Remove the file extension
                        materials[materialIndex].normalMap = content.Load<Texture2D>(texturePath + buffer);
                    }

                    // Reflectivity
                    buffer = fileReader.ReadLine();
                    string[] reflectivity = buffer.Split(' ');
                    materials[materialIndex].reflectionMaterial =
                            new Vector4(float.Parse(reflectivity[1], CultureInfo.InvariantCulture),
                                float.Parse(reflectivity[2], CultureInfo.InvariantCulture),
                                float.Parse(reflectivity[3], CultureInfo.InvariantCulture), 1.0f);

                    // Check for Material data
                    if (fileReader.Peek() == 'D')
                    {
                        // Read Diffuse Material
                        buffer = fileReader.ReadLine();
                        string[] data = buffer.Split(' ');
                        materials[materialIndex].diffuseMaterial =
                            new Vector4(float.Parse(data[1], CultureInfo.InvariantCulture),
                                float.Parse(data[2], CultureInfo.InvariantCulture),
                                float.Parse(data[3], CultureInfo.InvariantCulture), 1.0f);
                        
                    }

                    // Check for Material data
                    if (fileReader.Peek() == 'S')
                    {
                        // Read Specular Material
                        buffer = fileReader.ReadLine();
                        string[] data = buffer.Split(' ');
                        materials[materialIndex].specularMaterial =
                            new Vector4(float.Parse(data[1], CultureInfo.InvariantCulture),
                                float.Parse(data[2], CultureInfo.InvariantCulture),
                                float.Parse(data[3], CultureInfo.InvariantCulture), 1.0f);
                    }

                    // Check for technique name
                    if (fileReader.Peek() == 'T')
                    {
                        // Read in the name of the technique
                        buffer = fileReader.ReadLine();
                        materials[materialIndex].techniqueName = buffer.Split(' ')[1];
                    }
                    else
                    {
                        // Use default technique name
                        materials[materialIndex].techniqueName = "MeshTech";
                    }

                    fileReader.ReadLine();
                    materialIndex++;
                }

                // Read in vertices
                fileReader.ReadLine();

                VertexPosTangNormTex[] vertices = new VertexPosTangNormTex[numVertices];
                for(int i = 0; i < numVertices; i++)
                {
                    // Position
                    buffer = fileReader.ReadLine();
                    splitLine = buffer.Split(separator);

                    vertices[i].pos = new Vector3();
                    vertices[i].pos.X = float.Parse(splitLine[1], CultureInfo.InvariantCulture);
                    vertices[i].pos.Y = float.Parse(splitLine[2], CultureInfo.InvariantCulture);
                    vertices[i].pos.Z = -float.Parse(splitLine[3], CultureInfo.InvariantCulture); // Invert Z-Coord

                    // Tangent
                    buffer = fileReader.ReadLine();
                    splitLine = buffer.Split(separator);

                    vertices[i].tang = new Vector3();
                    vertices[i].tang.X = float.Parse(splitLine[1], CultureInfo.InvariantCulture);
                    vertices[i].tang.Y = float.Parse(splitLine[2], CultureInfo.InvariantCulture);
                    vertices[i].tang.Z = -float.Parse(splitLine[3], CultureInfo.InvariantCulture); // Invert Z-Coord

                    // Normal
                    buffer = fileReader.ReadLine();
                    splitLine = buffer.Split(separator);

                    vertices[i].norm = new Vector3();
                    vertices[i].norm.X = float.Parse(splitLine[1], CultureInfo.InvariantCulture);
                    vertices[i].norm.Y = float.Parse(splitLine[2], CultureInfo.InvariantCulture);
                    vertices[i].norm.Z = -float.Parse(splitLine[3], CultureInfo.InvariantCulture); // Invert Z-Coord

                    // Texture Coordinates
                    buffer = fileReader.ReadLine();
                    splitLine = buffer.Split(separator);

                    vertices[i].tex = new Vector2();
                    vertices[i].tex.X = float.Parse(splitLine[1], CultureInfo.InvariantCulture);
                    vertices[i].tex.Y = float.Parse(splitLine[2], CultureInfo.InvariantCulture);

                    fileReader.ReadLine();
                }

                // Read in the indices
                fileReader.ReadLine();
                Vector4[] indices = new Vector4[numTriangles]; // XYZ- triangle indices, W- Subset index
                for(int i = 0; i < numTriangles; i++)
                {
                    buffer = fileReader.ReadLine();
                    splitLine = buffer.Split(separator);

                    indices[i] = new Vector4();
                    indices[i].X = float.Parse(splitLine[0], CultureInfo.InvariantCulture);
                    indices[i].Y = float.Parse(splitLine[1], CultureInfo.InvariantCulture);
                    indices[i].Z = float.Parse(splitLine[2], CultureInfo.InvariantCulture);
                    indices[i].W = float.Parse(splitLine[3], CultureInfo.InvariantCulture);
                }
                
                // Create the individual subsets
                int subsetID = (int)indices[0].W; // First 
                int index = -1;
                materialIndex = 0;
                for(int i = 0; i < numMaterials; i++)
                {
                    List<VertexPosTangNormTex> vertexList = new List<VertexPosTangNormTex>();

                    // Add all vertices used by this subset to the list
                    try
                    {
                        do
                        {
                            index++;

                            // Get the vertices for this primitive
                            VertexPosTangNormTex v0 = vertices[((int)indices[index].X)];
                            VertexPosTangNormTex v1 = vertices[((int)indices[index].Y)];
                            VertexPosTangNormTex v2 = vertices[((int)indices[index].Z)];
                            subsetID = (int)indices[index].W;

                            // Add the vertices to the list
                            vertexList.Add(v0);
                            vertexList.Add(v1);
                            vertexList.Add(v2);
                        } while(subsetID == (int)indices[index + 1].W);
                    }
                    catch(IndexOutOfRangeException e) {}
                        
                    // Create the subset
                    MeshSubset subset = new MeshSubset(device, vertexList.ToArray());

                    // Set subset textures
                    subset.UseTextures = materials[materialIndex].useTextures;
                    subset.DiffuseMap = materials[materialIndex].diffuseMap;
                    subset.SpecularMap = materials[materialIndex].specularMap;
                    subset.NormalMap = materials[materialIndex].normalMap;
                    subset.ReflectionMaterial = materials[materialIndex].reflectionMaterial;
                    subset.DiffuseMaterial = materials[materialIndex].diffuseMaterial;
                    subset.SpecularMaterial = materials[materialIndex].specularMaterial;
                    subset.TechniqueName = materials[materialIndex].techniqueName;
                    materialIndex++;

                    // Add the subset to the list
                    subsets.Add(subset);
                }
            }
            catch(IOException)
            {

            }
            finally
            {
                fileReader.Close();
            }
        }

        private void UpdateWorldMatrix()
        {
            // Update the world matrix based on position, angles, and scale
            Matrix translationMtx = Matrix.CreateTranslation(position);

            Matrix rotationX = Matrix.CreateRotationX(rotationAngles.X);
            Matrix rotationY = Matrix.CreateRotationY(rotationAngles.Y);
            Matrix rotationZ = Matrix.CreateRotationZ(rotationAngles.Z);

            Matrix rotationMtx = rotationX * rotationY * rotationZ;
            
            Matrix scaleMtx = Matrix.CreateScale(scale);

            worldMatrix = rotationMtx * scaleMtx * translationMtx;

            // Update the aabb for each subset
            bool emptyAABB = true;
            foreach(MeshSubset meshSubset in subsets)
            {
                meshSubset.UpdateAABB(worldMatrix);
                
                if(emptyAABB)
                {
                    // Use first subset AABB
                    aabb = meshSubset.AABB;
                    emptyAABB = false;
                }
                else
                {
                    // Re-box AABB to include other subsets
                    aabb = BoundingBox.CreateMerged(aabb, meshSubset.AABB);
                }
            }
        }

        public bool AABBCollision(BoundingBox otherAABB)
        {
            // Check for collision with entire bounding box
            if(aabb.Intersects(otherAABB))
            {
                // Check for bounding box collision with each subset mesh
                foreach(MeshSubset meshSubset in subsets)
                {
                    if(meshSubset.AABB.Intersects(otherAABB))
                    {
                        return true;
                    }
                }
            }

            // No collision
            return false;
        }

        public float? AABBCollision(Ray ray)
        {
            // Check for collision with entire bounding box
            if (aabb.Intersects(ray).HasValue)
            {
                // Check for bounding box collision with each subset mesh
                foreach (MeshSubset meshSubset in subsets)
                {
                    float? intersect = meshSubset.AABB.Intersects(ray);
                    if (intersect.HasValue)
                    {
                        return intersect;
                    }
                }
            }

            // No collision
            return null;
        }

        /// <summary>
        /// Tests weither the given BoundingBox intersects this Mesh's geometry
        /// </summary>
        /// <param name="otherAABB"></param>
        /// <returns></returns>
        public Plane? MeshCollision(BoundingBox otherAABB)
        {
            // Test collision with the entire Mesh's bounding box
            if(aabb.Intersects(otherAABB))
            {
                // Test collision with each MeshSubset of this Mesh
                foreach(MeshSubset subset in subsets)
                {
                    Plane? p = subset.MeshCollision(otherAABB);
                    if(p.HasValue)
                    {
                        // Collision found
                        return p;
                    }
                }
            }

            // No collision
            return null;
        }

        public float? MeshCollision(Ray ray)
        {
            // Test collision with the entire Mesh's bounding box
            if (ray.Intersects(aabb).HasValue)
            {
                float shortestDist = float.MaxValue;

                // Test collision with each MeshSubset of this Mesh
                foreach (MeshSubset subset in subsets)
                {
                    float? collisionDist = subset.MeshCollision(ray);
                    if (collisionDist.HasValue && collisionDist < shortestDist)
                    {
                        // Update distance
                        shortestDist = collisionDist.Value;
                    }
                }

                return shortestDist;
            }

            // No collision
            return null;
        }
        

        // PROPERTIES
        public string MeshFileName
        {
            get 
            { 
                // Remove file path when returning
                char[] separator = {'\\'};
                string[] split = meshFileName.Split(separator);
                return split[split.Length - 1];
            }
        }

        public Vector3 Position
        {
            get { return position; }
            set 
            { 
                position = value;
 
                UpdateWorldMatrix();
            }
        }

        public Vector3 RotationAngles
        {
            get { return rotationAngles; }
            set 
            { 
                rotationAngles = value;
 
                UpdateWorldMatrix();
            }
        }

        public float Scale
        {
            get { return scale; }
            set
            {
                scale = value;

                UpdateWorldMatrix();
            }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
        }

        public List<MeshSubset> Subsets
        {
            get { return subsets; }
        }

        public BoundingBox AABB
        {
            get { return aabb; }
        }
    }

    class MeshDistanceComparer : System.Collections.Generic.IComparer<Mesh>
    {
        Vector3 position;
        BoundingFrustum frustum;

        public MeshDistanceComparer(Vector3 position, BoundingFrustum frustum)
        {
            this.position = position;
            this.frustum = frustum;
        }

        int IComparer<Mesh>.Compare(Mesh m1, Mesh m2)
        {
            // Distance check
            float dist1 = Vector3.Distance(position, m1.Position);
            float dist2 = Vector3.Distance(position, m2.Position);

            if (dist1 < dist2)
            {
                return -1;
            }
            else if(dist1 == dist2)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}