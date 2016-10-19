using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace Graphics3D
{
    class MeshManager
    {
        private static string DEFAULT_MESH_DIRECTORY = @"Meshes\";
        public static string MeshDirectory = DEFAULT_MESH_DIRECTORY;

        private static GraphicsDevice device;
        private static ContentManager content;

        private static List<string> meshNames;
        private static List<Mesh> meshList;

        public static void DeleteData()
        {
            // Create new lists
            meshNames = new List<string>();
            meshList = new List<Mesh>();
        }

        public static void InitializeManager(GraphicsDevice device, ContentManager content)
        {
            MeshManager.device = device;
            MeshManager.content = content;

            DeleteData();
        }

        public static Mesh LoadMesh(string meshFileName)
        {
            // Return a copy of the mesh if already opened
            if(meshNames.Contains(meshFileName))
            {
                Mesh meshCopy = 
                    new Mesh(meshList[meshNames.IndexOf(meshFileName)]);

                return meshCopy;
            }

            // Open new mesh, add to the lists and return
            Mesh newMesh = new Mesh(device, 
                content, 
                MeshDirectory + meshFileName,
                Vector3.Zero,
                Vector3.Zero,
                1.0f);

            meshNames.Add(meshFileName);
            meshList.Add(newMesh);

            return newMesh;
        }

        public static List<String> MeshNames
        {
            get { return meshNames; }
        }
    }
}
