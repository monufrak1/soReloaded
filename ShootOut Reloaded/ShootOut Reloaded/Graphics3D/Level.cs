using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Collections;

namespace Graphics3D
{
    enum LevelType
    {
        Outdoor,
        Indoor
    }

    class Level
    {
        protected GraphicsDevice device;
        protected ContentManager content;

        // Level data
        protected string title;
        protected LevelType type;

        protected Song backgroundMusic;
        protected SoundEffect ambientSFX;
        protected SoundEffectInstance ambientSFXInstance;
        protected SoundEffect footstepSFX1;
        protected SoundEffect footstepSFX2;

        protected Vector3 cameraStartPosition;
        protected Vector3 cameraStartDirection;
        protected float cameraHeightOffset;

        protected Vector4 skyColor;
        protected float darkSkyOffset;
        protected float fogStart;
        protected float fogRange;

        public static float WATER_DISABLED_HEIGHT = -10000.0f;
        protected float waterHeight;
        protected Vector4 waterColor;
        protected float deepWaterFogDistance;

        protected Light light;
        protected TexturedSkyDome sky;
        protected TerrainGrid terrain;
        protected TerrainGrid terrainPlane;
        protected SurfacePlane surface;
        protected WaterGrid water;
        protected List<Billboard> billboardList;
        protected List<Mesh> meshList;

        public Level(GraphicsDevice device, ContentManager content, string levelFileName)
        {
            this.device = device;
            this.content = content;

            billboardList = new List<Billboard>();
            meshList = new List<Mesh>();

            LoadLevel(levelFileName);
        }

        private void LoadLevel(string levelFileName)
        {
            // Load the data from the file
            StreamReader inFile = new StreamReader(levelFileName);

            while (!inFile.EndOfStream)
            {
                string line = inFile.ReadLine();
                string command = line.Split(' ')[0];
                string section = (command == "#") ? line.Split(' ')[1] : "";

                // HEADER
                if (command == "LEVEL_TITLE")
                {
                    title = line.Substring(command.Length + 1, line.Length - (command.Length + 1)); 
                }
                else if (command == "LEVEL_TYPE")
                {
                    string levelType = line.Split(' ')[1];
                    if (levelType == "INDOOR")
                    {
                        type = LevelType.Indoor;
                    }
                    else if (levelType == "OUTDOOR")
                    {
                        type = LevelType.Outdoor;
                    }
                }

                // SOUND EFFECTS
                else if (command == "BACKGROUND_MUSIC")
                {
                    string musicFileName = line.Remove(0, ("BACKGROUND_MUSIC").Length + 1);
                    musicFileName = musicFileName.Remove(musicFileName.IndexOf('.')); // Remove file extension XNA

                    backgroundMusic = content.Load<Song>(@"Music\" + musicFileName);
                }
                else if (command == "AMBIENT_SFX")
                {
                    string ambientFileName = line.Split(' ')[1];
                    ambientFileName = ambientFileName.Remove(ambientFileName.IndexOf('.')); // Remove file extension XNA

                    float volume = float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture);
                    float pitch = float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture);

                    ambientSFX = content.Load<SoundEffect>(@"SoundEffects\" + ambientFileName);
                    ambientSFXInstance = ambientSFX.CreateInstance();
                    ambientSFXInstance.IsLooped = true;
                    ambientSFXInstance.Volume = volume;
                    ambientSFXInstance.Pitch = pitch;
                }
                else if (command == "FOOTSTEP_SFXs")
                {
                    string fileName1 = line.Split(' ')[1];
                    string fileName2 = line.Split(' ')[2];

                    fileName1 = fileName1.Remove(fileName1.IndexOf('.')); // Remove file extensions XNA
                    fileName2 = fileName2.Remove(fileName2.IndexOf('.'));

                    footstepSFX1 = content.Load<SoundEffect>(@"SoundEffects\" + fileName1);
                    footstepSFX2 = content.Load<SoundEffect>(@"SoundEffects\" + fileName2);
                }

                // CAMERA
                else if (command == "CAMERA_START_POSITION")
                {
                    cameraStartPosition = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                    float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                    float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));
                }
                else if (command == "CAMERA_START_DIRECTION")
                {
                    cameraStartDirection = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));
                }
                else if (command == "CAMERA_HEIGHT_OFFSET")
                {
                    cameraHeightOffset = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);
                }

                // LIGHTING
                else if (command == "NUM_LIGHTS")
                {
                    int numLights = int.Parse(line.Split(' ')[1]);

                    // Create lights  (ONLY ONE LIGHT FOR NOW!!!)
                    for (int i = 0; i < numLights; i++)
                    {
                        // Light type
                        line = inFile.ReadLine();

                        // Light position
                        line = inFile.ReadLine();
                        Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                        // Light direction
                        line = inFile.ReadLine();
                        Vector3 direction = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                        // Ambient
                        line = inFile.ReadLine();
                        Vector4 ambient = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Diffuse
                        line = inFile.ReadLine();
                        Vector4 diffuse = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Specular
                        line = inFile.ReadLine();
                        Vector4 specular = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Size
                        line = inFile.ReadLine();
                        float lightSize = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                        // Create the light
                        light = new Light(device,
                            content.Load<Texture2D>(@"Textures\sunFlare"),
                            position,
                            direction,
                            ambient,
                            diffuse,
                            specular,
                            lightSize);
                    }
                }
                    
                // SKY and FOG
                if (section == "SKY_FOG")
                {
                    string skyTexture;
                    float skyTextureScale;

                    line = inFile.ReadLine();
                    skyTexture = (line.Split(' ')[1]).Split('.')[0];   // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    skyTextureScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    skyColor = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                    line = inFile.ReadLine();
                    darkSkyOffset = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    fogStart = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    fogRange = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create sky
                    sky = new TexturedSkyDome(device,
                        content.Load<Effect>(@"Effects\SkyDome"),
                        content.Load<Texture2D>(@"Textures\" + skyTexture),
                        skyTextureScale,
                        8000.0f);
                }

                // WATER
                if (section == "WATER")
                {
                    line = inFile.ReadLine();
                    waterHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    waterColor = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                    line = inFile.ReadLine();
                    string waterNormalTexture = line.Split(' ')[1].Split('.')[0];  // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    float texScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float ratio = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    deepWaterFogDistance = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float reflectionAmount = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float refractionAmount = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float waveHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float waveSpeed = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create water
                    water = new WaterGrid(device,
                        content.Load<Texture2D>(@"Textures\" + waterNormalTexture),
                        texScale, ratio, reflectionAmount, refractionAmount,
                        waveHeight, waveSpeed);
                }

                // TERRAIN
                if (section == "TERRAIN")
                {
                    line = inFile.ReadLine();
                    string heightMapFileName = line.Split(' ')[1];

                    line = inFile.ReadLine();
                    string lowTex = line.Split(' ')[1].Split('.')[0];

                    line = inFile.ReadLine();
                    string highTex = line.Split(' ')[1].Split('.')[0];   // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    float texScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float spacing = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float heightScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create terrain
                    terrain = new TerrainFromHeightMap(device,
                        content.Load<Texture2D>(@"Textures\" + lowTex),
                        content.Load<Texture2D>(@"Textures\" + highTex),
                        @"Terrain\" + heightMapFileName,
                        texScale,
                        spacing,
                        heightScale);

                    // Create terrain plane
                    terrainPlane = new TerrainPlane(device,
                        content.Load<Texture2D>(@"Textures\" + lowTex),
                        content.Load<Texture2D>(@"Textures\" + highTex),
                        texScale);
                }

                // SURFACE
                if (section == "SURFACE")
                {
                    // Surface height
                    line = inFile.ReadLine();
                    float surfaceHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Diffuse
                    line = inFile.ReadLine();
                    string diffTexFileName = line.Split(' ')[1];
                    diffTexFileName = diffTexFileName.Remove(diffTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Specular
                    line = inFile.ReadLine();
                    string specTexFileName = line.Split(' ')[1];
                    specTexFileName = specTexFileName.Remove(specTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Normal
                    line = inFile.ReadLine();
                    string normTexFileName = line.Split(' ')[1];
                    normTexFileName = normTexFileName.Remove(normTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Texture Scale
                    line = inFile.ReadLine();
                    float scale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create surface
                    surface = new SurfacePlane(device,
                        surfaceHeight,
                        content.Load<Texture2D>(@"Textures\" + diffTexFileName),
                        content.Load<Texture2D>(@"Textures\" + specTexFileName),
                        content.Load<Texture2D>(@"Textures\" + normTexFileName),
                        scale);
                }

                // Billboard
                if (command == "NEW_BILLBOARD")
                {
                    // Get the texture name
                    line = inFile.ReadLine();
                    string billboardTex = line.Split(' ')[1];
 
                    // Get the position
                    line = inFile.ReadLine();
                    Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                               float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                               float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // Get the size
                    line = inFile.ReadLine();
                    Vector2 size = new Vector2(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture));

                    // Load the billboard
                    Billboard newBillboard = new Billboard(device, position, size,
                        content.Load<Texture2D>(@"Textures\" + billboardTex));

                    // Add the billboard to the list
                    billboardList.Add(newBillboard);
                }

                // MESHES
                if (command == "MESH_NAME")
                {
                    // Preload the Mesh
                    MeshManager.LoadMesh(line.Split(' ')[1]);
                }
                else if (command == "NEW_MESH")
                {
                    // Get new mesh copy from the manager
                    Mesh newMesh = MeshManager.LoadMesh(line.Split(' ')[1]);

                    // POSITION
                    line = inFile.ReadLine();
                    Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // ROTATION
                    line = inFile.ReadLine();
                    Vector3 rotation = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // SCALE
                    line = inFile.ReadLine();
                    float scale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    newMesh.Position = position;
                    newMesh.RotationAngles = rotation;
                    newMesh.Scale = scale;

                    // Add the mesh to the list
                    meshList.Add(newMesh);
                }
            }

            inFile.Close();
        }

        public void SortMeshList(Vector3 cameraPosition, BoundingFrustum viewFrustum)
        {
            // Sort meses in a front to back order from the camera
            IComparer<Mesh> comp = new MeshDistanceComparer(cameraPosition, viewFrustum);
            MeshList.Sort(comp);
        }

        // PROPERTIES
        public string Title
        {
            get { return title; }
        }

        public LevelType Type
        {
            get { return type; }
        }

        public Song BackgroundMusic
        {
            get { return backgroundMusic; }
        }

        public SoundEffectInstance AmbientSFX
        {
            get { return ambientSFXInstance; }
        }

        public SoundEffect FootstepSFX1
        {
            get { return footstepSFX1; }
        }

        public SoundEffect FootstepSFX2
        {
            get { return footstepSFX2; }
        }

        public Vector3 CameraStartPosition
        {
            get { return cameraStartPosition; }
        }

        public Vector3 CameraStartDirection
        {
            get { return cameraStartDirection; }
        }

        public float CameraHeightOffset
        {
            get { return cameraHeightOffset; }
        }

        public Vector4 SkyColor
        {
            get { return skyColor; }
        }

        public float DarkSkyOffset
        {
            get { return darkSkyOffset; }
        }

        public float FogStart
        {
            get { return fogStart; }
        }

        public float FogRange
        {
            get { return fogRange; }
        }

        public float WaterHeight
        {
            get { return waterHeight; }
        }

        public Vector4 WaterColor
        {
            get { return waterColor; }
        }

        public float DeepWaterFogDistance
        {
            get { return deepWaterFogDistance; }
        }

        public Light Light
        {
            get { return light; }
        }

        public TexturedSkyDome Sky
        {
            get { return sky; }
        }

        public TerrainGrid Terrain
        {
            get { return terrain; }
        }

        public TerrainGrid TerrainPlane
        {
            get { return terrainPlane; }
        }

        public SurfacePlane Surface
        {
            get { return surface; }
        }

        public WaterGrid Water
        {
            get { return water; }
        }

        public List<Billboard> BillboardList
        {
            get { return billboardList; }
        }

        public List<Mesh> MeshList
        {
            get { return meshList; }
        }
    }
}
