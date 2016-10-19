using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Graphics3D;
using GameObjects;

namespace GameStateManagement
{
    class ShooterGameScreen : GameScreen
    {
        public static string originalGunName;

        const int TARGET_HIT_VALUE = 100;
        const float LONGSHOT_DISTANCE = 200.0f;
        const float SNIPERSHOT_DISTANCE = 700.0f;
        const int LONGSHOT_MULTIPLIER = 3;
        const int SNIPERSHOT_MULTIPLIER = 10;
        const int BULLSEYE_MULTIPLIER = 5;
        const int HEADSHOT_MULTIPLIER = 10;
        const float ACTION_TEXT_ANGLE = 15.0f;

        float pauseAlpha;
        bool gameOver;

        GraphicsDevice graphicsDevice;
        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont font;

        int screenWidth;
        int screenHeight;

        // Engine flags
        bool shadowsEnabled;
        bool postProcessingEffectsEnabled;
        bool displayFPS;
        bool hudEnabled;

        RenderTarget2D mainRT;
        RenderTarget2D reflectionRT;
        RenderTarget2D occlusionRT;
        RenderTarget2D bloomRT;
        PostProcessingEffects postEffects;

        Color clearColor;

        FirstPersonCamera camera;
        float moveSpeed;
        float horizontalSensitivity;
        float verticalSensitivity;

        Texture2D bulletTexture;
        Rectangle bulletRect;

        ParticleEmitter sparkParticleEmitter;
        ParticleEmitter waterParticleEmitter;
        Vector2 sparkSize;

        SoundEffect secretFoundSFX;
        SoundEffectInstance secretAmbientSFXInstance;
        bool secretItemCollected;

        // Renderers
        LightRenderer lightRenderer;
        TerrainRenderer terrainRenderer;
        SurfaceRenderer surfaceRenderer;
        WaterRenderer waterRenderer;
        BillboardRenderer billboardRenderer;
        MeshRenderer meshRenderer;

        // Game Objects
        FootstepSFXPlayer footstepPlayer;
        Gun gun;
        string levelName;
        string gunName;

        // Level
        ShooterGameType gameType;
        ShooterLevel level;

        float targetScoreOrTime;
        int targetScore;            // Used in Target Score game types
        float targetTime;           // Used in Time Trial game types
        List<float> meshHitLengths; 

        // Game data
        int targetsHit;

        LevelRecord levelRecord;

        string actionText;
        Vector2 actionTextPosition;
        float actionTextAngle;
        float actionTextTimer;

        string smallActionText;
        float smallActionTextTimer;

        string reloadText;

        int headshotCounter;
        int bullseyeCounter;

        // Input Objects
        KeyboardState keyboardState;
        GamePadState gamepadState;

        public ShooterGameScreen(string levelName, string gunName)
            : this(levelName, gunName, ShooterGameType.ScoreAttack, 0)
        {

        }

        public ShooterGameScreen(string levelName, string gunName, ShooterGameType gameType)
            : this(levelName, gunName, gameType, 0)
        {

        }

        public ShooterGameScreen(string levelName, string gunName, ShooterGameType gameType, 
                                 float targetScoreOrTime)
        {
            base.TransitionOnTime = new TimeSpan(0, 0, 2);

            this.levelName = levelName;
            this.gunName = gunName;
            this.gameType = gameType;
            this.targetScoreOrTime = targetScoreOrTime;
            if (gameType == ShooterGameType.TargetScore)
            {
                targetScore = (int)targetScoreOrTime;
            }
            else if (gameType == ShooterGameType.TimeTrial)
            {
                targetTime = targetScoreOrTime;
            }

            // Set initial game data
            targetsHit = 0;
            meshHitLengths = new List<float>();

            levelRecord = new LevelRecord(0, 0.0f, 0, 0.0f);

            reloadText = "RELOAD!";
            
            // Set graphics flags
            shadowsEnabled = true;
            postProcessingEffectsEnabled = true;
            displayFPS = false;
            hudEnabled = true;
        }
        
        public override void LoadContent()
        {
            Initialize();

            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            if (gameType != ShooterGameType.ScoreAttack)
            {
                // Re-selected original gun
                ActivePlayer.Profile.SelectedWeaponName = originalGunName;
            }

            // Stop music and sound effects
            MediaPlayer.Stop();
            level.AmbientSFX.Stop();
        }

        private void Initialize()
        {
            graphicsDevice = ScreenManager.GraphicsDevice;
            content = ScreenManager.Content;
            spriteBatch = ScreenManager.SpriteBatch;
            font = content.Load<SpriteFont>("outlinedFontTexture2");
            MeshManager.InitializeManager(graphicsDevice, content);

            screenWidth = graphicsDevice.Viewport.Width;
            screenHeight = graphicsDevice.Viewport.Height;

            // Zero out all channels for post processing
            clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            // Create Render Targets
            mainRT = new RenderTarget2D(graphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            reflectionRT = new RenderTarget2D(graphicsDevice, screenWidth / 4, screenHeight / 4, true, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);
            occlusionRT = new RenderTarget2D(graphicsDevice, screenWidth / 4, screenHeight / 4, false, SurfaceFormat.Color,
                DepthFormat.None);
            bloomRT = new RenderTarget2D(graphicsDevice, screenWidth / 8, screenHeight / 8, false, SurfaceFormat.Color,
                DepthFormat.None);

            postEffects = new PostProcessingEffects(graphicsDevice,
                content.Load<Effect>(@"Effects\PostProcessingEffects"));

            // Create renderers
            lightRenderer = new LightRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Light"));
            terrainRenderer = new TerrainRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Terrain"));
            surfaceRenderer = new SurfaceRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Surface"));
            waterRenderer = new WaterRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Water"));
            billboardRenderer = new BillboardRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Billboard"));
            meshRenderer = new MeshRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Mesh"));

            // Create camera
            horizontalSensitivity = ActivePlayer.Profile.CameraSensitivityX;
            verticalSensitivity = ActivePlayer.Profile.CameraSensitivityY * 0.65f;

            camera = new FirstPersonCamera();
            camera.AspectRatio = graphicsDevice.Viewport.AspectRatio;
            camera.AABBSize = new Vector2(2.25f, 8.0f);
            camera.DrawDistance = 10000.0f;
            camera.MoveSpeed = moveSpeed;
            camera.InvertY = ActivePlayer.Profile.InvertCameraY;
            camera.HorizontalSensitivity = horizontalSensitivity;
            camera.VerticalSensitivity = verticalSensitivity;
            camera.FreeFlyEnabled = false;
            camera.PitchMinDegrees = -75.0f;
            camera.PitchMaxDegrees = 60.0f;
            camera.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
                                camera.AspectRatio, 0.1f, 10000.0f);

            // Create spark particle effect
            sparkSize = new Vector2(0.25f, 0.25f);
            sparkParticleEmitter = new ParticleEmitter(graphicsDevice, Vector3.Zero, content.Load<Texture2D>(@"Textures\spark"),
                new SparkParticleUpdater(), billboardRenderer, 200, 0.25f);
            waterParticleEmitter = new ParticleEmitter(graphicsDevice, Vector3.Zero, content.Load<Texture2D>(@"Textures\water_particle"),
                new SparkParticleUpdater(), billboardRenderer, 200, 0.25f);

            // Load HUD data
            int scale = 1;
            bulletTexture = content.Load<Texture2D>("bullet_icon");
            bulletRect = new Rectangle((int)(screenWidth * 0.8f),
                (int)(screenHeight * 0.9f),
                bulletTexture.Width * scale, bulletTexture.Height * scale);


            actionText = string.Empty;
            actionTextPosition = new Vector2(screenWidth * 0.05f, screenHeight * 0.1f);
            actionTextAngle = MathHelper.ToRadians(ACTION_TEXT_ANGLE);
            actionTextTimer = 0.0f;

            // Load sound effect
            secretFoundSFX = content.Load<SoundEffect>(@"SoundEffects\shotgun_pump");
            secretAmbientSFXInstance = (content.Load<SoundEffect>(@"SoundEffects\secret_ambient")).CreateInstance();
            secretAmbientSFXInstance.IsLooped = true;
            secretAmbientSFXInstance.Volume = 0.0f;
            secretAmbientSFXInstance.Pitch = -0.75f;
            secretAmbientSFXInstance.Play();
            
            // Load level data
            ActivePlayer.Profile.SelectedWeaponName = gunName.Split('.')[0];
            string levelDir = (gameType == ShooterGameType.ScoreAttack) ?
                @"Levels\ScoreAttack\" : @"Levels\WeaponChallenges\";
            LoadLevel(levelDir + levelName);
        }

        private void LoadLevel(string levelFileName)
        {
            // Load level
            level = new ShooterLevel(graphicsDevice, content, levelFileName);
            camera.Position = level.CameraStartPosition;
            camera.Look = level.CameraStartDirection;
            camera.DrawDistance = level.FogRange + level.FogStart;

            // Check if secret has been found
            if (gameType == ShooterGameType.ScoreAttack && level.HasSecret() &&
                ActivePlayer.Profile.IsSecretFound(levelName))
            {
                // Remove the secret item
                secretItemCollected = true;
                secretAmbientSFXInstance.Stop();
                level.MeshList.Remove(level.SecretItem);
            }

            if (level.Light.Size <= 0.0f)
            {
                // Disable light scattering
                postProcessingEffectsEnabled = false;
            }

            // Create sound players
            MediaPlayer.Play(level.BackgroundMusic);
            MediaPlayer.IsRepeating = true;
            level.AmbientSFX.Play();
            footstepPlayer = new FootstepSFXPlayer(
                new SoundEffect[] { level.FootstepSFX1, level.FootstepSFX2 },
                0.75f, 0.25f);

            // Load a gun
            gun = new Gun(graphicsDevice, content, meshRenderer,
                billboardRenderer,
                gunName);

            moveSpeed = 35.0f * gun.ReloadSpeed;

            // Set initial effect parameters
            SharedEffectParameters.xProjectionMatrix = camera.Projection;
            SharedEffectParameters.xReflectionProjectionMatrix = camera.Projection;

            SharedEffectParameters.xSkyColor = level.SkyColor;
            SharedEffectParameters.xDarkSkyOffset = level.DarkSkyOffset;
            SharedEffectParameters.xFogStart = level.FogStart;
            SharedEffectParameters.xFogRange = level.FogRange;

            SharedEffectParameters.xWaterColor = level.WaterColor;
            SharedEffectParameters.xWaterHeight = level.WaterHeight;
            SharedEffectParameters.xDeepWaterFogDistance = level.DeepWaterFogDistance;

            SharedEffectParameters.xLightAmbient = level.Light.Ambient;
            SharedEffectParameters.xLightDiffuse = level.Light.Diffuse;
            SharedEffectParameters.xLightSpecular = level.Light.Specular;
            SharedEffectParameters.xLightDirection = level.Light.Direction;
            SharedEffectParameters.xLightProjectionMatrix = level.Light.ProjectionMatrix;
            SharedEffectParameters.xShadowsEnabled = shadowsEnabled;
        }

        float endGameTime;
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                               bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
                pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
            else
                pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

            if (IsActive)
            {
                UpdateGame(gameTime);

                // Update medals
                ActivePlayer.Profile.UpdateStatsAndMedals();

                // Game Conditions
                if (gameType != ShooterGameType.Collection && targetsHit == level.ShootingTargetList.Count)
                {
                    gameOver = true;
                }

                if (gameOver)
                {
                    // Wait a second before ending the game
                    if ((float)gameTime.TotalGameTime.TotalSeconds - endGameTime >= 1.0f)
                    {
                        // Update level record
                        if (levelRecord.ShotsFired > 0)
                        {
                            levelRecord.Accuracy = (float)levelRecord.NumTargetsShot / (float)levelRecord.ShotsFired;
                        }
                        else
                        {
                            levelRecord.Accuracy = 0.0f;
                        }

                        // Level Complete
                        ScreenManager.AddScreen(
                            new LevelCompleteMenuScreen(levelRecord, levelName, gunName, gameType, targetScoreOrTime),
                            ActivePlayer.PlayerIndex);
                    }
                }
                else
                {
                    // Update end game time. Game is not over
                    endGameTime = (float)gameTime.TotalGameTime.TotalSeconds;
                }
            }
            else
            {
                // Ensure Xbox controller is not rumbling
                GamePad.SetVibration(ActivePlayer.PlayerIndex, 0.0f, 0.0f);
            }
        }

        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile
            int playerIndex = (int)ActivePlayer.PlayerIndex;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(levelName, gunName, gameType, targetScoreOrTime), ControllingPlayer);
            }
        }

        private void UpdateGame(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();
            gamepadState = GamePad.GetState(ActivePlayer.PlayerIndex);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            actionTextTimer -= dt;

            // Update level timer
            if (!gameOver)
            {
                levelRecord.Time += dt;
            }

            int scoreThisFrame = 0;
            int prevTargetsHit = targetsHit;
            bool secretCollected = false;

            horizontalSensitivity = ActivePlayer.Profile.CameraSensitivityX;
            verticalSensitivity = ActivePlayer.Profile.CameraSensitivityY * 0.5f;
            camera.InvertY = ActivePlayer.Profile.InvertCameraY;
            camera.MoveSpeed = moveSpeed;
            camera.HorizontalSensitivity = horizontalSensitivity;
            camera.VerticalSensitivity = verticalSensitivity;

            // Allow camera to fly (DEBUG MODE)
            if ((keyboardState.IsKeyDown(Keys.LeftShift) || gamepadState.IsButtonDown(Buttons.A))
                && displayFPS)
            {
                camera.FreeFlyEnabled = true;
                camera.MoveSpeed = moveSpeed * 5.0f;
            }
            else
            {
                camera.FreeFlyEnabled = false;
                camera.MoveSpeed = moveSpeed;
            }
            
            // ADS Speed
            if (gun.AimingDownSights)
            {
                camera.MoveSpeed = moveSpeed * 0.75f;
                camera.HorizontalSensitivity = horizontalSensitivity * 0.3f;
                camera.VerticalSensitivity = verticalSensitivity * 0.5f;
            }

            Vector3 oldCamPos = camera.Position;
            camera.Update(dt);

            if (!camera.FreeFlyEnabled)
            {
                // Collision detection
                foreach (Mesh mesh in level.MeshList)
                {
                    Plane? collisionPlane = mesh.MeshCollision(camera.AABB);
                    if (collisionPlane.HasValue)
                    {
                        if (mesh.Equals(level.SecretItem))
                        {
                            // Set collection flag
                            secretCollected = true;
                        }
                        else
                        {
                            // Reset camera to "safe" position
                            camera.Position = oldCamPos;
                        }
                    }
                }

                // Set camera height
                if (level.Type == LevelType.Outdoor)
                {
                    Vector3 pos = camera.Position;
                    pos.Y = level.Terrain.GetHeight(pos.X, pos.Z) + level.CameraHeightOffset;
                    camera.Position = pos;
                }
                else
                {
                    Vector3 pos = camera.Position;
                    pos.Y = level.Surface.Height + level.CameraHeightOffset;
                    camera.Position = pos;
                }

                // Outdoor level checks
                if (level.Type == LevelType.Outdoor)
                {
                    // Steep terrain or "water walking"
                    if (level.Terrain.IsSteepIncline(camera.Position.X, camera.Position.Z, 0.65f) ||
                                (camera.Position.Y - level.CameraHeightOffset <= level.WaterHeight &&
                                 level.Water.ReflectionAmount > 0.0f))
                    {
                        // Reset camera position
                        camera.Position = oldCamPos;
                    } 
                }

                // Target collision detection
                foreach (ShootingTarget target in level.ShootingTargetList)
                {
                    if (target.IsActive && target.AABBCollision(camera.AABB))
                    {
                        // Knockdown target 
                        target.KnockDown();
                        targetsHit++;

                        // Update Player stats
                        ActivePlayer.Profile.Knockdowns++;
                        levelRecord.NumKnockdowns++;

                        scoreThisFrame += TARGET_HIT_VALUE;

                        UpdateActionText("KNOCKDOWN");

                        if (gameType == ShooterGameType.BullseyeChallenge ||
                           gameType == ShooterGameType.HeadshotChallenge)
                        {
                            // End the game
                            gameOver = true;
                        }
                    }
                }


                gun.Update(dt, camera);
                footstepPlayer.Play(oldCamPos, camera.Position);
            }

            float meshHitDistance = float.MaxValue;
            meshHitLengths.Clear();
            Ray bulletRay = new Ray(gun.Position, gun.AimDirection);
            if (gun.BulletFired)
            {
                // Check for bullet collision with meshes
                foreach (Mesh mesh in level.MeshList)
                {
                    float? length = mesh.MeshCollision(bulletRay);
                    if (length.HasValue)
                    {
                        // Bullet hit a mesh
                        meshHitDistance = (length.Value < meshHitDistance) ?
                            length.Value : meshHitDistance;

                        meshHitLengths.Add(length.Value);
                    }
                }

                meshHitLengths.Sort();

                if (meshHitDistance > 0.0f)
                {
                    // Set spark particle position
                    sparkParticleEmitter.Position = bulletRay.Position + (bulletRay.Direction * meshHitDistance);
                    sparkParticleEmitter.EmitParticles(5, -bulletRay.Direction, sparkSize, 0.05f);
                }

                if (level.Type == LevelType.Indoor)
                {
                    float? dist = bulletRay.Intersects(level.Surface.Plane);
                    if(dist.HasValue)
                    {
                        // Set spark particle position
                        sparkParticleEmitter.Position = bulletRay.Position + (bulletRay.Direction * dist.Value);
                        sparkParticleEmitter.EmitParticles(3, Vector3.Up, sparkSize, 0.05f);
                    }
                }

                if (level.WaterHeight > Level.WATER_DISABLED_HEIGHT && level.Water.ReflectionAmount > 0.0f)
                {
                    float? dist = bulletRay.Intersects(new Plane(Vector3.UnitY, level.WaterHeight));
                    if (dist.HasValue)
                    {
                        // Set spark particle position
                        waterParticleEmitter.Position = bulletRay.Position + (bulletRay.Direction * dist.Value);
                        waterParticleEmitter.EmitParticles(10, Vector3.Up, sparkSize/2, 0.005f);
                    }
                }

                levelRecord.ShotsFired++;

                // Update player stats
                ActivePlayer.Profile.ShotsFired++;
            }

            // Update the targets
            foreach (ShootingTarget target in level.ShootingTargetList)
            {
                // Check if target was shot
                if (gun.BulletFired && target.IsActive)
                {
                    ShootingTargetCollisionResult result = target.HitCheck(bulletRay);
                    if (result.IsTargetHit())
                    {
                        bool penetrativeShotValid = false;
                        if(gun.BulletType == BulletType.Penetrative)
                        {
                            // Check how many meshes the bullet has passed through
                            int maxMeshesHit = 2;
                            int numMeshesHit = 0;
                            for(int i = 0; i < meshHitLengths.Count; i++)
                            {
                                if(result.HitDistance > meshHitLengths[i])
                                {
                                    numMeshesHit++;
                                }
                            }

                            if(numMeshesHit <= maxMeshesHit)
                            {
                                penetrativeShotValid = true;
                            }
                        }

                        if (result.HitDistance < meshHitDistance || penetrativeShotValid)
                        {
                            // Target was shot
                            target.ShootDown(result.CollisionType);
                            targetsHit++;
                            levelRecord.NumTargetsShot++;
                            
                            // Update player stats
                            ActivePlayer.Profile.TargetHits++;

                            string targetActionText = "";

                            // Snipershot/Longshot?
                            if (result.HitDistance >= SNIPERSHOT_DISTANCE)
                            {
                                levelRecord.NumSnipershots++;
                                scoreThisFrame += TARGET_HIT_VALUE * SNIPERSHOT_MULTIPLIER;

                                // Update player stats
                                ActivePlayer.Profile.Snipershots++;
                                levelRecord.NumSnipershots++;
                                targetActionText = "SNIPERSHOT";
                            }
                            else if (result.HitDistance >= LONGSHOT_DISTANCE)
                            {
                                levelRecord.NumLongshots++;
                                scoreThisFrame += TARGET_HIT_VALUE * LONGSHOT_MULTIPLIER;

                                // Update player stats
                                ActivePlayer.Profile.Longshots++;
                                levelRecord.NumLongshots++;
                                targetActionText = "LONGSHOT";
                            }
                            else
                            {
                                // Points for a close range shot (Standard points)
                                scoreThisFrame += TARGET_HIT_VALUE;
                                targetActionText = "TARGET HIT";
                            }

                            // Collision type check
                            if (result.CollisionType == ShootingTargetCollisionType.Headshot)
                            {
                                scoreThisFrame += TARGET_HIT_VALUE * HEADSHOT_MULTIPLIER;
                                ActivePlayer.Profile.Headshots++;
                                levelRecord.NumHeadshots++;
                                headshotCounter++;

                                if (gunName.Split('.')[0] == "Thompson" &&
                                    headshotCounter >= ActivePlayer.Profile.GetMedalRequirement("Chicago Typewriter"))
                                {
                                    ActivePlayer.Profile.UnlockMedal("Chicago Typewriter");
                                }

                                if (targetActionText != "TARGET HIT")
                                {
                                    targetActionText += ActivePlayer.FullHDEnabled ? " HEADSHOT" : "\n -HEADSHOT";
                                }
                                else
                                {
                                    targetActionText = "HEADSHOT";
                                    scoreThisFrame -= TARGET_HIT_VALUE;  // Remove extra points
                                }

                                if (gameType == ShooterGameType.BullseyeChallenge)
                                {
                                    // End the game
                                    gameOver = true;
                                }
                            }
                            else if (result.CollisionType == ShootingTargetCollisionType.Bullseye)
                            {
                                scoreThisFrame += TARGET_HIT_VALUE * BULLSEYE_MULTIPLIER;
                                ActivePlayer.Profile.Bullseyes++;
                                levelRecord.NumBullseyes++;
                                bullseyeCounter++;

                                if (gunName.Split('.')[0] == "M4A1" &&
                                    bullseyeCounter >= ActivePlayer.Profile.GetMedalRequirement("This Is My Rifle"))
                                {
                                    ActivePlayer.Profile.UnlockMedal("This Is My Rifle");
                                }

                                if (targetActionText != "TARGET HIT")
                                {
                                    targetActionText += ActivePlayer.FullHDEnabled ? " BULLSEYE" : "\n -BULLSEYE";
                                }
                                else
                                {
                                    targetActionText = "BULLSEYE";
                                    scoreThisFrame -= TARGET_HIT_VALUE;   // Remove extra points
                                }

                                if (gameType == ShooterGameType.HeadshotChallenge)
                                {
                                    // End the game
                                    gameOver = true;
                                }
                            }
                            else
                            {
                                // Normal target shot
                                headshotCounter = 0;
                                bullseyeCounter = 0;

                                if (gameType == ShooterGameType.BullseyeChallenge ||
                                    gameType == ShooterGameType.HeadshotChallenge)
                                {
                                    // End the game
                                    gameOver = true;
                                }
                            }

                            if (result.HitDistance > meshHitDistance)
                            {
                                ActivePlayer.Profile.UnlockMedal("X-Ray Vision");
                            }

                            // Update the action text
                            UpdateActionText(targetActionText);
                        }
                    }
                }

                target.Update(dt);
            }

            // Update secret item
            if (secretCollected)
            {
                level.MeshList.Remove(level.SecretItem);
                secretItemCollected = true;
                secretFoundSFX.Play();

                if (gameType == ShooterGameType.ScoreAttack)
                {
                    ActivePlayer.Profile.SetSecretFound(levelName);
                }
                else if (gameType == ShooterGameType.Collection)
                {
                    gameOver = true;
                }
            }

            if (!secretItemCollected && level.HasSecret())
            {
                Vector3 pos = level.SecretItem.Position;
                Vector3 rot = level.SecretItem.RotationAngles;

                pos.Y += (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds * 2.0f) * 0.01f;
                rot.Y += dt;

                level.SecretItem.Position = pos;
                level.SecretItem.RotationAngles = rot;

                // Calculate ambient SFX volume
                secretAmbientSFXInstance.Volume = MathHelper.Lerp(1.0f, 0.0f, Math.Min(Vector3.Distance(camera.Position, pos) * 0.01f, 1.0f));
            }
            else
            {
                // Decrease ambient SFX volume
                float newVolume = secretAmbientSFXInstance.Volume - dt;
                if (newVolume >= 0.0f)
                {
                    // Set new volume
                    secretAmbientSFXInstance.Volume = newVolume;
                }
            }
            
            // Update score
            int prevScore = levelRecord.Score;
            int targetsHitThisFrame = targetsHit - prevTargetsHit;
            levelRecord.Score += scoreThisFrame * targetsHitThisFrame;

            if (targetsHitThisFrame > 1)
            {
                // Update player stats
                ActivePlayer.Profile.Multishots++;
                levelRecord.NumMultishots++;

                UpdateActionText("MULTI-SHOT" + " x" + targetsHitThisFrame);

                // Medal check
                if (targetsHitThisFrame >= ActivePlayer.Profile.GetMedalRequirement("Prison Riot") &&
                    levelName == "Prison.lvl")
                {
                    ActivePlayer.Profile.UnlockMedal("Prison Riot");
                }
            }

            int addedScore = levelRecord.Score - prevScore;
            if (addedScore > 0)
            {
                // Add score to action text
                UpdateActionText("+ " + addedScore.ToString());
            }

            // Update light position
            level.Light.Position = camera.Position + (-level.Light.Direction * 250.0f);

            // Update effect parameters
            SharedEffectParameters.xTime = (float)gameTime.TotalGameTime.TotalSeconds;
            SharedEffectParameters.xViewMatrix = camera.View;
            SharedEffectParameters.xEyePosW = camera.Position;
            SharedEffectParameters.xReflectionViewMatrix =
                level.Water.CalculateReflectionMatrix(camera.Position, camera.Look);
            SharedEffectParameters.xLightPosition = level.Light.Position;
            SharedEffectParameters.xLightViewMatrix = level.Light.ViewMatrix;

            meshRenderer.UpdateEffectVariables();
            billboardRenderer.UpdateEffectVariables();

            sparkParticleEmitter.Update(dt);
            waterParticleEmitter.Update(dt);
        }

        private void UpdateActionText(string text)
        {
            // Set small action text
            smallActionTextTimer = 2.0f;
            if (text.Contains('+'))
            {
                smallActionText = text + "\n" + smallActionText;
            }
            else
            {
                smallActionText = text;
            }

            if (actionText == string.Empty)
            {
                // Reset action text timer
                actionTextTimer = 5.0f;
                actionTextAngle = -actionTextAngle;

                actionText = text;
            }
            else
            {
                if (actionTextTimer < 3.0f)
                {
                    actionTextTimer = 3.0f;
                }

                // Add extra line of text
                if (actionText.Contains('+') && text.Contains('+'))
                {
                    // Get both score lines and add together
                    int addScore = 0;
                    string[] actionTextLines = actionText.Split('\n');
                    actionText = string.Empty;
                    foreach (string line in actionTextLines)
                    {
                        if (!line.Contains('+'))
                        {
                            actionText += line + '\n';
                        }
                        else
                        {
                            addScore += int.Parse(line.Split(' ')[1]);
                        }
                    }

                    addScore += int.Parse(text.Split(' ')[1]);
                    actionText += "+ " + addScore;
                }
                else
                {
                    if (text.Contains('+'))
                    {
                        // Add score text to the end
                        actionText += "\n" + text;
                    }
                    else
                    {
                        // Push text onto the beginning
                        actionText = text + "\n" + actionText;
                    }
                }
            }

            // Medal checks
            if (actionTextTimer > 0.0f && levelName == "Flooded.lvl" &&
                actionText.Split('\n').Count((string s) => (s == "TARGET HIT" ||
                            s == "LONGSHOT" ||
                            s == "SNIPERSHOT" ||
                            s == "BULLSEYE" ||
                            s == "HEADSHOT")) == 
                            ActivePlayer.Profile.GetMedalRequirement("Target Flood"))
            {
                ActivePlayer.Profile.UnlockMedal("Target Flood");
            }

            if (actionTextTimer > 0.0f && gunName.Split('.')[0] == "Colt45" &&
                    actionText.Split('\n').Count((string s) => (s == "HEADSHOT")) ==
                        ActivePlayer.Profile.GetMedalRequirement("Pistol Pete"))
            {
                ActivePlayer.Profile.UnlockMedal("Pistol Pete");
            }

            if (actionTextTimer > 0.0f && gunName.Split('.')[0] == "SCARL" &&
                actionText.Split('\n').Count((string s) => (s == "LONGSHOT")) ==
                    ActivePlayer.Profile.GetMedalRequirement("Rifleman"))
            {
                ActivePlayer.Profile.UnlockMedal("Rifleman");
            }

            if (actionTextTimer > 0.0f && gunName.Split('.')[0] == "Kriss" &&
                actionText.Split('\n').Count((string s) => (s == "BULLSEYE")) ==
                    ActivePlayer.Profile.GetMedalRequirement("Krisstastic"))
            {
                ActivePlayer.Profile.UnlockMedal("Krisstastic");
            }

            if (levelName == "Stacks 2.lvl" && actionText.Contains("MULTI-SHOT x2"))
            {
                int index = Array.IndexOf(actionText.Split('\n'), "MULTI-SHOT x2");
                if (actionText.Split('\n')[index + 1] == "HEADSHOT" &&
                    actionText.Split('\n')[index + 2] == "HEADSHOT")
                {
                    ActivePlayer.Profile.UnlockMedal("Two for One");
                }
            }

        }

        public override void Draw(GameTime gameTime)
        {
            BoundingFrustum viewFrustum = camera.ViewFrustum;
            BoundingFrustum occlusionViewFrustum = new BoundingFrustum(camera.View * camera.Projection);
            BoundingFrustum lightViewFrustum = level.Light.ViewFrustum;

            level.SortMeshList(camera.Position, viewFrustum);

            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Draw shadows
            DrawShadows(lightViewFrustum);

            // Draw reflections
            DrawReflections(viewFrustum);

            // Draw main
            DrawMain(viewFrustum);

            // Draw SpriteBatch data
            spriteBatch.Begin();

            if (!postProcessingEffectsEnabled)
            {
                // Draw the frame
                graphicsDevice.SetRenderTarget(null);
                spriteBatch.Draw(mainRT, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            }

            // Draw HUD
            if (IsActive && hudEnabled)
            {
                gun.DrawCrosshairs(spriteBatch);

                // Print action text
                float scale = Math.Min(Math.Max(actionTextTimer / 4, 1.0f), 2.0f);
                if (actionTextTimer > 0.0f)
                {
                    actionTextTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    float alpha = Math.Min(actionTextTimer, 1.0f);
                    spriteBatch.DrawString(font, actionText,
                        actionTextPosition / scale, Color.White * alpha, 0.0f, //actionTextAngle,
                        Vector2.Zero, scale, SpriteEffects.None, 0.0f);
                }
                else
                {
                    // Reset actionText
                    actionText = string.Empty;
                }

                // Print small action text 
                if (smallActionTextTimer > 0.0f)
                {
                    smallActionTextTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    float smallActionTextAlpha = Math.Min(2.0f - smallActionTextTimer, 1.0f);
                    float smallActionTextScale = 0.5f;
                    float textHeight = (font.MeasureString(smallActionText) * smallActionTextScale).Y;
                    Vector2 smallActionTextPos = new Vector2(screenWidth * 0.55f, screenHeight * 0.5f - textHeight * 0.5f);
                    spriteBatch.DrawString(font, smallActionText, smallActionTextPos, (Color.White * smallActionTextAlpha) * TransitionAlpha,
                        0.0f, Vector2.Zero, smallActionTextScale, SpriteEffects.None, 0.0f);
                }
                else
                {
                    // Reset
                    smallActionText = string.Empty;
                }

                // Print reload text
                if (gun.CurrentAmmo == 0)
                {
                    float reloadTextAlpha = Math.Max((float)(Math.Cos(gameTime.TotalGameTime.TotalSeconds * 20.0f)), 0.5f);
                    float reloadTextScale = 0.5f;
                    Vector2 reloadTextSize = font.MeasureString(reloadText) * reloadTextScale;
                    Vector2 reloadTextPosition = new Vector2((float)(screenWidth * 0.45f - reloadTextSize.X),
                        (float)(screenHeight * 0.5f - reloadTextSize.Y * 0.5f));
                    spriteBatch.DrawString(font, reloadText, reloadTextPosition, (Color.White * reloadTextAlpha) * TransitionAlpha,
                        0.0f, Vector2.Zero, reloadTextScale, SpriteEffects.None, 0.0f);
                }

                // Print score
                float scoreScale = Math.Max(scale * 2, 1.5f);
                Vector2 scoreSize = font.MeasureString(levelRecord.Score.ToString()) * scoreScale;
                spriteBatch.DrawString(font, levelRecord.Score.ToString(),
                    new Vector2((float)screenWidth * 0.9f - scoreSize.X, (float)screenHeight * 0.15f - scoreSize.Y/2),
                    (gameType == ShooterGameType.TargetScore && levelRecord.Score < targetScore ? Color.Red : Color.White),
                    0.0f, Vector2.Zero, scoreScale, SpriteEffects.None, 0.0f);

                // Print timer
                string timeStr = levelRecord.Time.ToString("F");
                Vector2 timeStrSize;
                if (levelRecord.Time < 10.0f) timeStrSize = font.MeasureString("X.XX");
                else if (levelRecord.Time < 100.0f) timeStrSize = font.MeasureString("XX.XX");
                else timeStrSize = font.MeasureString("XXX.XX");
                spriteBatch.DrawString(font, timeStr,
                    new Vector2(((float)screenWidth*0.5f - timeStrSize.X*0.5f), (float)screenHeight * 0.1f),
                    (gameType == ShooterGameType.TimeTrial && levelRecord.Time > targetTime ? Color.Red : Color.White));

                // Print target count
                if (gameType != ShooterGameType.Collection)
                {
                    string targetString = targetsHit.ToString() + "/" + level.ShootingTargetList.Count.ToString();
                    float targetScale = scoreScale - 1.0f;
                    Vector2 targetStringSize = font.MeasureString(targetString) * targetScale;
                    spriteBatch.DrawString(font, targetString,
                        new Vector2((float)screenWidth * 0.9f - targetStringSize.X, (float)screenHeight * 0.225f),
                        Color.White, 0.0f, Vector2.Zero, targetScale, SpriteEffects.None, 0.0f);
                }

                // Draw ammo counter
                Point originalPosition = bulletRect.Location;
                for (int i = 1; i <= gun.MaxAmmo; i++)
                {
                    // Draw a bullet icon
                    spriteBatch.Draw(bulletTexture, bulletRect, (i <= gun.CurrentAmmo) ?
                        Color.White : Color.DarkRed);
                    
                    // Set position for next frame
                    if (i % 10 == 0)
                    {
                        bulletRect.X = originalPosition.X;
                        bulletRect.Y -= bulletRect.Height;
                    }
                    else
                    {
                        bulletRect.X += bulletRect.Width/2;
                    }
                }
                bulletRect.Location = originalPosition;
            }

            if (displayFPS)
            {
                spriteBatch.DrawString(ScreenManager.Font, fps.ToString(),
                    new Vector2(screenWidth * 0.05f, screenHeight * 0.05f), Color.Yellow);
            }

            spriteBatch.End();

            CalcFPS(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }

            base.Draw(gameTime);
        }

        private void DrawOcclusions(BoundingFrustum viewFrustum)
        {
            if (postProcessingEffectsEnabled)
            {
                graphicsDevice.SetRenderTarget(occlusionRT);
                graphicsDevice.Clear(Color.Black);
                
                // Only render occlusions if the light is visible by the camera
                if (viewFrustum.Intersects(level.Light.AABB))
                {
                    lightRenderer.Draw(level.Light, camera.Up, camera.Look);

                    gun.DrawOcclusion();
                    if (level.Type == LevelType.Outdoor)
                    {
                        terrainRenderer.DrawOcclusion(level.Terrain);
                        terrainRenderer.DrawOcclusion(level.TerrainPlane);
                    }

                    foreach (Mesh mesh in level.MeshList)
                    {
                        if (viewFrustum.Intersects(mesh.AABB))
                        {
                            meshRenderer.DrawMeshOcclusion(mesh, viewFrustum);
                        }
                    }

                    foreach (ShootingTarget target in level.ShootingTargetList)
                    {
                        target.DrawOcclusion(meshRenderer, viewFrustum);
                    }

                    level.Sky.DrawOcclusion();
                }
            }
        }

        private void DrawShadows(BoundingFrustum lightViewFrustum)
        {
            if (shadowsEnabled)
            {
                graphicsDevice.SetRenderTarget(level.Light.ShadowMap);
                graphicsDevice.Clear(Color.Black);

                foreach (Mesh mesh in level.MeshList)
                {
                    if (lightViewFrustum.Intersects(mesh.AABB))
                    {
                        meshRenderer.DrawMeshShadow(mesh, lightViewFrustum);
                    }
                }

                foreach (ShootingTarget target in level.ShootingTargetList)
                {
                    target.DrawShadow(meshRenderer, lightViewFrustum);
                }

                SharedEffectParameters.xShadowMap = level.Light.ShadowMap;
            }
        }

        private void DrawReflections(BoundingFrustum viewFrustum)
        {
            if (level.WaterHeight != Level.WATER_DISABLED_HEIGHT &&
                level.Water.TransparencyRatio > 0.0f)
            {
                graphicsDevice.SetRenderTarget(reflectionRT);
                graphicsDevice.Clear(Color.Black);

                BoundingFrustum reflViewFrustum = new BoundingFrustum(SharedEffectParameters.xReflectionViewMatrix *
                    SharedEffectParameters.xReflectionProjectionMatrix);

                // Draw terrain reflections
                if (level.Type == LevelType.Outdoor)
                {
                    terrainRenderer.DrawReflection(level.Terrain);
                }

                // Draw mesh reflections
                foreach (Mesh mesh in level.MeshList)
                {
                    if (reflViewFrustum.Intersects(mesh.AABB))
                    {
                        meshRenderer.DrawMeshReflection(mesh, reflViewFrustum, level.WaterHeight);
                    }
                }

                // Draw target reflections
                foreach (ShootingTarget target in level.ShootingTargetList)
                {
                    target.DrawReflection(meshRenderer, reflViewFrustum, level.WaterHeight);
                }
                    
                level.Sky.DrawCloudsReflection();
            }
        }

        private void DrawMain(BoundingFrustum viewFrustum)
        {
            graphicsDevice.SetRenderTarget(mainRT);
            graphicsDevice.Clear(clearColor);

            // Draw Terrain/Surface
            if (level.Type == LevelType.Outdoor)
            {
                terrainRenderer.Draw(level.Terrain);
                terrainRenderer.Draw(level.TerrainPlane);
            }
            else
            {
                surfaceRenderer.DrawSurface(level.Surface);
            }

            // Draw Meshes
            RasterizerState rs = graphicsDevice.RasterizerState;
            foreach (Mesh mesh in level.MeshList)
            {
                if (viewFrustum.Intersects(mesh.AABB))
                {
                    meshRenderer.DrawMesh(mesh, viewFrustum);
                }
            }
            graphicsDevice.RasterizerState = rs;

            // Draw targets
            foreach (ShootingTarget target in level.ShootingTargetList)
            {
                target.Draw(meshRenderer, viewFrustum);
            }

            // Draw billboards
            foreach (Billboard billboard in level.BillboardList)
            {
                if (viewFrustum.Intersects(billboard.AABB))
                {
                    billboardRenderer.DrawLighting(billboard, Vector3.Up, camera.Look);
                }
            }

            // Draw sky box
            level.Sky.DrawClouds();

            // Draw water
            if (level.WaterHeight != Level.WATER_DISABLED_HEIGHT)
            {
                waterRenderer.Draw(level.Water, reflectionRT, mainRT);
            }

            // Draw gun
            gun.Draw(camera.Up, camera.Look);

            // Draw particles
            sparkParticleEmitter.DrawParticles(camera.Up, camera.Look);
            waterParticleEmitter.DrawParticles(camera.Up, camera.Look);

            // Draw post processing
            if (postProcessingEffectsEnabled)
            {
                graphicsDevice.RasterizerState = RasterizerState.CullNone;

                graphicsDevice.SetRenderTarget(occlusionRT);
                graphicsDevice.Clear(Color.Black);

                // Draw light into occlusion map
                lightRenderer.Draw(level.Light, camera.Up, camera.Look);

                // Copy Occlusion data from the Alpha channel of main render target
                postEffects.DrawCopyOcclusion(mainRT);

                graphicsDevice.SetRenderTarget(bloomRT);
                graphicsDevice.Clear(Color.Black);

                // Draw lightscattering into bloom map
                postEffects.DrawLightScattering(occlusionRT);

                // Apply bloom lighting to output the final image
                graphicsDevice.SetRenderTarget(null);
                postEffects.ApplyBloom(mainRT, bloomRT);
            }
        }

        private int fps;
        private int frames;
        private float timer;
        private void CalcFPS(GameTime gameTime)
        {
            frames++;

            if (gameTime.TotalGameTime.TotalSeconds - timer >= 1.0f)
            {
                fps = frames;
                frames = 0;
                timer = (float)gameTime.TotalGameTime.TotalSeconds;
            }
        }
    }

    class SparkParticleUpdater : ParticleEmitterUpdater
    {

        public void UpdateParticle(Particle p, float dt)
        {
            // Update the particle
            p.Age += dt;
            p.Position += p.Direction * p.Speed;
        }
    }
}
