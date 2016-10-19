using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

using Graphics3D;

namespace GameObjects
{
    enum FireType
    {
        FullAuto,
        SemiAuto
    };

    enum BulletType
    {
        Penetrative,
        NonPenetrative
    };

    class Gun
    {
        public static string gunDirectory = @"Guns\";
        private static float RUMBLE_TIME = 0.2f;

        private bool useAmmoStockpile;
        private int ammoStockpile;
        private int currentAmmo;

        private bool isShooting;
        private bool isReloading;
        private bool bulletFired;

        private bool runAnimation;              // Is true when recoiling
        private bool endAnimation;              // Is true when returning to neutral position
        private float shootAnimationAngle;      // Current amount of shooting animation

        private float reloadDelta;
        private float reloadSpeed;
        private bool runReload;
        private bool endReload;

        private float adsDelta;
        private bool aimingDownSights;

        private float idolAnimationOffsetX;
        private float idolAnimationOffsetY;
        private bool increaseIdolX;
        private bool increaseIdolY;
        private float rotationOffsetAngleX;
        private float rotationOffsetAngleY;
        private float maxRotationOffsetAngle = MathHelper.ToRadians(2.5f);

        private Vector3 aimDirection;
        private float accuracyDelta;
        private float currentAccuracy;          // 0.0f - Perfect accuracy, > 0 less accuracy

        private Vector3 prevNeutralPosition;

        private float rumbleTimer;
        private Random rand;

        // Data loaded from (.gun) file
        private string gunName;
        private string meshFileName;
        private int maxAmmo;
        private float gunAccuracy;
        private FireType fireType;
        private BulletType bulletType;
        private string SFXFileName;

        private float scale;
        private Vector3 positionOffset;
        private Vector3 ADSPositionOffset;
        private Vector3 emitterPositionOffset;
        private float maxShootAnimationAngle;
        private float shootAnimationSpeed;
        private float idolAnimationOffset;
        private float idolAnimationSpeed;
        private float ADSSpeed;
        private float kickBackAmount;

        // Graphics Variables
        private GraphicsDevice device;
        private MeshRenderer meshRenderer;
        private BillboardRenderer billboardRenderer;

        private Mesh gunMesh;
        private ParticleEmitter bulletParticleEmitter;
        private Billboard muzzleFlashBillboard;

        private Texture2D crosshairRight;
        private Texture2D crosshairLeft;
        private Texture2D crosshairTop;
        private Texture2D crosshairBottom;

        // Sound Variables
        private SoundEffect shootSFX;
        private SoundEffect reloadSFX;
        private SoundEffect outOfAmmoSFX;

        private SoundEffectInstance reloadSFXInstance;
        private SoundEffectInstance outOfAmmoSFXInstance;

        // Input States
        private GamePadState gamePadState;
        private GamePadState prevGamePadState;

        private KeyboardState keyboardState;
        private KeyboardState prevKeyboardState;

        private MouseState mouseState;
        private MouseState prevMouseState;

        public Gun(GraphicsDevice device, ContentManager content, MeshRenderer meshRenderer,
                   BillboardRenderer billboardRenderer, string gunFileName)
        {
            this.device = device;
            this.meshRenderer = meshRenderer;
            this.billboardRenderer = billboardRenderer;

            LoadGunFromFile(gunFileName);

            currentAmmo = maxAmmo;
            isShooting = false;
            isReloading = false;

            runAnimation = false;
            endAnimation = false;
            shootAnimationAngle = 0.0f;

            reloadDelta = 0.0f;
            runReload = false;
            endReload = false;

            adsDelta = 0.0f;
            aimingDownSights = false;

            increaseIdolX = true;
            increaseIdolY = true;

            prevNeutralPosition = Vector3.Zero;

            rand = new Random();

            // Create graphics data
            gunMesh = MeshManager.LoadMesh(meshFileName);
            gunMesh.Scale = scale;

            bulletParticleEmitter = new ParticleEmitter(device,
                Vector3.Zero,
                content.Load<Texture2D>(@"Textures\Particle"),
                new BulletParticleUpdater(),
                billboardRenderer,
                maxAmmo,
                2.0f);

            muzzleFlashBillboard = new Billboard(device,
                Vector3.Zero,
                Vector2.One,
                content.Load<Texture2D>(@"Textures\muzzleFlash"));

            // Load crosshair textures
            crosshairRight = content.Load<Texture2D>(@"Textures\crosshair_right");
            crosshairLeft = content.Load<Texture2D>(@"Textures\crosshair_left");
            crosshairTop = content.Load<Texture2D>(@"Textures\crosshair_top");
            crosshairBottom = content.Load<Texture2D>(@"Textures\crosshair_bottom");

            shootSFX = content.Load<SoundEffect>(@"SoundEffects\" + SFXFileName);

            reloadSFX = content.Load<SoundEffect>(@"SoundEffects\" + "gun_reload");
            reloadSFXInstance = reloadSFX.CreateInstance();
            reloadSFXInstance.Pitch = 0.5f;

            outOfAmmoSFX = content.Load<SoundEffect>(@"SoundEffects\" + "gun_dryfire");
            outOfAmmoSFXInstance = outOfAmmoSFX.CreateInstance();
        }

        private void LoadGunFromFile(string gunFileName)
        {
            // Read in gun data from the provided file
            StreamReader fileReader = new StreamReader(gunDirectory + gunFileName);

            string buffer;
            char[] separator = { ' ' };

            while (!fileReader.EndOfStream)
            {
                buffer = fileReader.ReadLine();

                // Tokenize line
                string[] line = buffer.Split(separator);

                if (line[0].Equals("Name"))
                {
                    gunName = line[1];
                }
                else if (line[0].Equals("Mesh"))
                {
                    meshFileName = line[1];
                }
                else if (line[0].Equals("Max_Ammo"))
                {
                    maxAmmo = int.Parse(line[1]);
                }
                else if (line[0].Equals("Accuracy"))
                {
                    gunAccuracy = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Fire_Type"))
                {
                    if (line[1].Equals("SEMI_AUTO"))
                    {
                        fireType = FireType.SemiAuto;
                    }
                    else if (line[1].Equals("FULL_AUTO"))
                    {
                        fireType = FireType.FullAuto;
                    }
                    else
                    {
                        // Error in file. Semi-Auto default
                        fireType = FireType.SemiAuto;
                    }
                }
                else if (line[0].Equals("Bullet_Type"))
                {
                    if (line[1].Equals("PENETRATIVE"))
                    {
                        bulletType = BulletType.Penetrative;
                    }
                    else if (line[1].Equals("NON_PENETRATIVE"))
                    {
                        bulletType = BulletType.NonPenetrative;
                    }
                    else
                    {
                        // Error in file. Non-penetrative default
                        bulletType = BulletType.NonPenetrative;
                    }
                }
                else if (line[0].Equals("Sound_Effect"))
                {
                    SFXFileName = line[1];
                    SFXFileName = SFXFileName.Remove(SFXFileName.IndexOf('.')); // Remove file extension XNA
                }
                else if (line[0].Equals("Scale"))
                {
                    scale = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Position"))
                {
                    float x = float.Parse(line[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(line[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(line[3], CultureInfo.InvariantCulture);

                    positionOffset = new Vector3(x, y, z);
                }
                else if (line[0].Equals("ADS_Position"))
                {
                    float x = float.Parse(line[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(line[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(line[3], CultureInfo.InvariantCulture);

                    ADSPositionOffset = new Vector3(x, y, z);
                }
                else if (line[0].Equals("Emitter_Position"))
                {
                    float x = float.Parse(line[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(line[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(line[3], CultureInfo.InvariantCulture);

                    emitterPositionOffset = new Vector3(x, y, z);
                }
                else if (line[0].Equals("Max_Animation_Angle"))
                {
                    maxShootAnimationAngle = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Animation_Speed"))
                {
                    shootAnimationSpeed = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Idol_Animation_Offset"))
                {
                    idolAnimationOffset = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Idol_Animation_Speed"))
                {
                    idolAnimationSpeed = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("ADS_Speed"))
                {
                    ADSSpeed = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Kick_Back_Amount"))
                {
                    kickBackAmount = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
            }

            // Set other data
            reloadSpeed = ADSSpeed * 0.15f;
        }

        public void Update(float dt, FirstPersonCamera camera)
        {
            // Get data from the camera
            Vector3 position = camera.Position;
            Vector3 forward = camera.Look;
            Vector3 right = camera.Right;
            Vector3 up = camera.Up;
            Vector2 rotationAngles = new Vector2(camera.PitchAngle, camera.RotationAngle);

            float randomIdolOffset = (float)rand.NextDouble() * idolAnimationSpeed * dt;

            // Increase idol animation amount if gun is "moving"
            float dist = Vector3.Distance(prevNeutralPosition, position);
            if (dist > 0)
            {
                randomIdolOffset += dist * idolAnimationSpeed * 0.1f;
            }

            prevNeutralPosition = position;
            bool prevIsShooting = isShooting;

            // Get input
#if XBOX
            prevGamePadState = gamePadState;
            gamePadState = GamePad.GetState(ActivePlayer.PlayerIndex);

            // Shoot gun?
            if ((fireType == FireType.FullAuto && gamePadState.Triggers.Right >= 0.5f) ||
                (fireType == FireType.SemiAuto && gamePadState.Triggers.Right >= 0.5f &&
                    prevGamePadState.Triggers.Right <= 0.5f))
            {
                Shoot();
            }

            // Check for a valid shot this frame
            if (isShooting && !prevIsShooting)
            {
                bulletFired = true;
            }
            else
            {
                bulletFired = false;

                // Set rumble timer
                rumbleTimer -= dt;
                if(rumbleTimer < 0.0f)
                {
                    rumbleTimer = 0.0f;
                }
            }

            // Aim gun?
            if (gamePadState.Triggers.Left >= 0.2f)
            {
                aimingDownSights = true;
            }
            else
            {
                aimingDownSights = false;
            }

            // Reload Gun?
            if (gamePadState.IsButtonDown(Buttons.X))
            {
                if (useAmmoStockpile)
                {
                    Reload(ref ammoStockpile);
                }
                else
                {
                    Reload();
                }
            }

            // Set rotation offsets
            float rotationdt = 0.1f * dt;
            if (gamePadState.ThumbSticks.Right.Y > 0.65f || gamePadState.ThumbSticks.Right.Y < -0.65f)
            {
                rotationOffsetAngleX -= gamePadState.ThumbSticks.Right.Y *  rotationdt;
            }
            else
            {
                if (rotationOffsetAngleX > 0.0f)
                {
                    rotationOffsetAngleX -= rotationdt;
                    if (rotationOffsetAngleX < 0.0f)
                        rotationOffsetAngleX = 0.0f;
                }
                else if(rotationOffsetAngleX < 0.0f)
                {
                    rotationOffsetAngleX += rotationdt;
                    if (rotationOffsetAngleX > 0.0f)
                        rotationOffsetAngleX = 0.0f;
                }
            }

            if (gamePadState.ThumbSticks.Right.X > 0.65f || gamePadState.ThumbSticks.Right.X < -0.65f)
            {
                rotationOffsetAngleY -= gamePadState.ThumbSticks.Right.X * rotationdt;
            }
            else
            {
                if (rotationOffsetAngleY > 0.0f)
                {
                    rotationOffsetAngleY -= rotationdt;
                    if (rotationOffsetAngleY < 0.0f)
                        rotationOffsetAngleY = 0.0f;
                }
                else if(rotationOffsetAngleY < 0.0f)
                {
                    rotationOffsetAngleY += rotationdt;
                    if (rotationOffsetAngleY > 0.0f)
                        rotationOffsetAngleY = 0.0f;
                }
            }

            if (rotationOffsetAngleY > maxRotationOffsetAngle)
                rotationOffsetAngleY = maxRotationOffsetAngle;
            else if (rotationOffsetAngleY < -maxRotationOffsetAngle)
                rotationOffsetAngleY = -maxRotationOffsetAngle;

            float offsetXdelta = 2.0f;
            if (rotationOffsetAngleX > maxRotationOffsetAngle / offsetXdelta)
                rotationOffsetAngleX = maxRotationOffsetAngle / offsetXdelta;
            else if (rotationOffsetAngleX < -maxRotationOffsetAngle / offsetXdelta)
                rotationOffsetAngleX = -maxRotationOffsetAngle / offsetXdelta;

            if (aimingDownSights)
            {
                rotationOffsetAngleX = 0.0f;
                rotationOffsetAngleY = 0.0f;
            }

            // Set rumble levels
            if(rumbleTimer > 0.0f)
            {
                GamePad.SetVibration(ActivePlayer.PlayerIndex, 1.0f, 1.0f);
            }
            else
            {
                GamePad.SetVibration(ActivePlayer.PlayerIndex, 0.0f, 0.0f);
            }
#else // WINDOWS
            prevKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();

            prevMouseState = mouseState;
            mouseState = Mouse.GetState();

            // Shoot gun?
            if ((fireType == FireType.FullAuto && mouseState.LeftButton == ButtonState.Pressed) ||
                (fireType == FireType.SemiAuto && mouseState.LeftButton == ButtonState.Pressed &&
                    prevMouseState.LeftButton == ButtonState.Released))
            {
                Shoot();
            }

            // Check for a valid shot this frame
            if (isShooting && !prevIsShooting)
            {
                bulletFired = true;
            }
            else
            {
                bulletFired = false;
            }

            // Aim gun?
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                aimingDownSights = true;
            }
            else
            {
                aimingDownSights = false;
            }

            // Reload Gun?
            if (keyboardState.IsKeyDown(Keys.R))
            {
                if (useAmmoStockpile)
                {
                    Reload(ref ammoStockpile);
                }
                else
                {
                    Reload();
                }
            }

#endif

            if (isShooting && !runAnimation && !endAnimation)
            {
                // Begin shooting animation
                runAnimation = true;
                endAnimation = false;

                // Update aim direction (random rotation scaled by accuracy)
                Matrix rightRotation = Matrix.CreateFromAxisAngle(right,
                    (float)Math.Sin(MathHelper.ToRadians((float)rand.NextDouble() * 360.0f)) * currentAccuracy);
                Matrix upRotation = Matrix.CreateFromAxisAngle(up,
                    (float)Math.Sin(MathHelper.ToRadians((float)rand.NextDouble() * 360.0f)) * currentAccuracy);

                aimDirection = forward;
                aimDirection = Vector3.TransformNormal(aimDirection, rightRotation);
                aimDirection = Vector3.TransformNormal(aimDirection, upRotation);

                // Set new muzzle flash size
                muzzleFlashBillboard.Size = new Vector2(((float)rand.NextDouble() * 0.05f + 0.025f),
                                                        ((float)rand.NextDouble() * 0.05f + 0.025f));

                // Emit new bullet particle
                Vector2 particleSize = Vector2.One * 0.1f;
                float bulletSpeed = 10.0f;
                bulletParticleEmitter.EmitParticle(aimDirection, particleSize, bulletSpeed);
            }

            // Update shooting animation
            if (runAnimation)
            {
                shootAnimationAngle += dt * shootAnimationSpeed;
            }

            if (endAnimation)
            {
                shootAnimationAngle -= dt * shootAnimationSpeed * 0.5f;
            }

            if (shootAnimationAngle > maxShootAnimationAngle)
            {
                shootAnimationAngle = maxShootAnimationAngle;

                // End animation
                runAnimation = false;
                endAnimation = true;
            }

            if (shootAnimationAngle < 0.0f)
            {
                shootAnimationAngle = 0.0f;

                // Gun is no longer shooting
                endAnimation = false;
                isShooting = false;
            }

            // Update idol animation
            idolAnimationOffsetX += increaseIdolX ? randomIdolOffset : -randomIdolOffset;
            idolAnimationOffsetY += increaseIdolY ? randomIdolOffset : -randomIdolOffset;

            if (idolAnimationOffsetX > idolAnimationOffset)
            {
                idolAnimationOffsetX = idolAnimationOffset;
                increaseIdolX = false;
            }

            if (idolAnimationOffsetX < -idolAnimationOffset)
            {
                idolAnimationOffsetX = -idolAnimationOffset;
                increaseIdolX = true;
            }

            float idolAnimationDelimY = 0.5f;
            if (idolAnimationOffsetY > idolAnimationOffset * idolAnimationDelimY)
            {
                idolAnimationOffsetY = idolAnimationOffset * idolAnimationDelimY;
                increaseIdolY = false;
            }

            if (idolAnimationOffsetY < -idolAnimationOffset * idolAnimationDelimY)
            {
                idolAnimationOffsetY = -idolAnimationOffset * idolAnimationDelimY;
                increaseIdolY = true;
            }

            // Update Reload
            if (isReloading && !runReload && !endReload)
            {
                // Begin the reload animation
                runReload = true;
            }

            if (runReload)
            {
                reloadDelta += dt * reloadSpeed;
            }

            if (endReload)
            {
                reloadDelta -= dt * reloadSpeed;
            }

            if (reloadDelta > 1.0f)
            {
                reloadDelta = 1.0f;

                runReload = false;
                endReload = true;
            }

            if (reloadDelta < 0.0f)
            {
                reloadDelta = 0.0f;

                endReload = false;

                // Finished reloading
                isReloading = false;
            }

            // Update ADS
            adsDelta += aimingDownSights ?
                (dt * ADSSpeed) : -(dt * ADSSpeed);

            if (adsDelta > 1.0f) adsDelta = 1.0f;
            if (adsDelta < 0.0f) adsDelta = 0.0f;

            // Update accuracy
            if (isShooting)
            {
                accuracyDelta += dt;
            }
            else
            {
                accuracyDelta -= dt * 2.0f;
            }

            float worstAccuracy = aimingDownSights ? gunAccuracy * 0.6f : gunAccuracy * 2.0f;
            if (accuracyDelta > worstAccuracy) accuracyDelta = worstAccuracy;
            if (accuracyDelta < gunAccuracy) accuracyDelta = gunAccuracy;

            currentAccuracy = accuracyDelta + (dist * 75.0f);
            if (currentAccuracy > worstAccuracy) currentAccuracy = worstAccuracy;

            // Set gun position and rotation
            Vector3 neutralPosition = position +
                forward * (positionOffset.Z - shootAnimationAngle * kickBackAmount) +
                right * (positionOffset.X + idolAnimationOffsetX) +
                up * (positionOffset.Y + idolAnimationOffsetY);

            Vector3 ADSPosition = position +
                forward * (ADSPositionOffset.Z - shootAnimationAngle * kickBackAmount) +
                right * (ADSPositionOffset.X) +
                up * (ADSPositionOffset.Y);

            // Gun mesh's position is the combination of neutral and ADS positions
            gunMesh.Position = Vector3.Lerp(neutralPosition, ADSPosition, adsDelta);

            if (isReloading)
            {
                Vector3 reloadPosition = gunMesh.Position +
                    up * -2.0f;

                // Gun mesh's position is the combination of the neutral and reload position
                gunMesh.Position = Vector3.Lerp(gunMesh.Position, reloadPosition, reloadDelta);
            }

            gunMesh.RotationAngles = new Vector3(
                rotationAngles.X + rotationOffsetAngleX + shootAnimationAngle + idolAnimationOffsetX * 0.25f,
                rotationAngles.Y + rotationOffsetAngleY + idolAnimationOffsetY * 0.25f,
                idolAnimationOffsetY * 2.5f);

            // Set bullet emitter position
            bulletParticleEmitter.Position = gunMesh.Position +
                forward * emitterPositionOffset.Z +
                right * emitterPositionOffset.X +
                up * emitterPositionOffset.Y;

            // Update bullet emitter
            bulletParticleEmitter.Update(dt);

            // Set muzzle flash position
            muzzleFlashBillboard.Position = bulletParticleEmitter.Position;
        }

        public void Draw(Vector3 cameraUp, Vector3 cameraForward)
        {
            // Draw the gun mesh
            meshRenderer.DrawMesh(gunMesh);

            // Draw the bullet particles
            bulletParticleEmitter.DrawParticles(cameraUp, cameraForward);

            // Draw muzzle flash
            if (runAnimation || rumbleTimer > RUMBLE_TIME * 0.9f)
            {
                billboardRenderer.DrawParticle(muzzleFlashBillboard, cameraUp, cameraForward);
            }
        }

        public void DrawOcclusion()
        {
            // Draw the gun mesh occlusion
            meshRenderer.DrawMeshOcclusion(gunMesh);
        }

        public void DrawCrosshairs(SpriteBatch spriteBatch)
        {
            if (!aimingDownSights)
            {
                Vector2 screenCenter = new Vector2(device.Viewport.Width / 2, device.Viewport.Height / 2);
                float offset = 10.0f + (currentAccuracy + gunAccuracy) * 400.0f;
                int size = 1;

                Rectangle leftRect = new Rectangle((int)(screenCenter.X - crosshairLeft.Width - offset),
                                                    (int)(screenCenter.Y - crosshairLeft.Height / 2),
                                                    crosshairLeft.Width * size, crosshairLeft.Height * size);

                Rectangle rightRect = new Rectangle((int)(2 * screenCenter.X - leftRect.X - crosshairRight.Width),
                                                    (int)(screenCenter.Y - crosshairRight.Height / 2),
                                                    crosshairRight.Width * size, crosshairRight.Height * size);

                Rectangle topRect = new Rectangle((int)(screenCenter.X - crosshairTop.Width / 2),
                                                    (int)(screenCenter.Y - crosshairTop.Height - offset),
                                                    crosshairTop.Width * size, crosshairTop.Height * size);

                Rectangle bottomRect = new Rectangle((int)(screenCenter.X - crosshairBottom.Width / 2),
                                                    (int)(screenCenter.Y + (screenCenter.Y - topRect.Y) - crosshairBottom.Height),
                                                    crosshairBottom.Width * size, crosshairBottom.Height * size);

                // Draw the crosshairs centered on the screen based on gun's current accuracy
                spriteBatch.Draw(crosshairLeft, leftRect, Color.Black);
                spriteBatch.Draw(crosshairRight, rightRect, Color.Black);
                spriteBatch.Draw(crosshairTop, topRect, Color.Black);
                spriteBatch.Draw(crosshairBottom, bottomRect, Color.Black);
            }
        }

        public void Shoot()
        {
            // Out of Ammo?
            if (currentAmmo == 0)
            {
                outOfAmmoSFXInstance.Play();
            }

            // Fire the gun if possible
            if (!isShooting && !isReloading && currentAmmo > 0)
            {
                isShooting = true;
                currentAmmo--;

                // Play shooting sound
                shootSFX.Play(0.5f, 0.0f, 0.0f);

#if XBOX
                // Set rumble timer
                rumbleTimer = RUMBLE_TIME;
#endif
            }
        }

        public void Reload()
        {
            if (!isShooting && !isReloading && currentAmmo < maxAmmo)
            {
                isReloading = true;

                // Reset ammo counter
                currentAmmo = maxAmmo;

                // Play reload sound effect
                reloadSFXInstance.Play();
            }
        }

        public void Reload(ref int ammoStockpile)
        {
            if (!isShooting && !isReloading && currentAmmo < maxAmmo)
            {
                // Only reload if there is enough ammo
                if (ammoStockpile > 0)
                {
                    int exchange = maxAmmo - currentAmmo;

                    if (ammoStockpile >= exchange)
                    {
                        ammoStockpile -= exchange;
                        currentAmmo += exchange;
                    }
                    else
                    {
                        currentAmmo += ammoStockpile;
                        ammoStockpile = 0;
                    }

                    isReloading = true;

                    // Play reload sound effect
                    reloadSFXInstance.Play();
                }
            }
        }

        public void SetUpAmmoStockpile(ref int stockpile)
        {
            ammoStockpile = stockpile;
        }

        // PROPERTIES
        public int CurrentAmmo
        {
            get { return currentAmmo; }
        }

        /// <summary>
        /// Set weither this Gun will replensing current ammo
        /// from a limited supply
        /// </summary>
        public bool UseAmmoStockpile
        {
            get { return useAmmoStockpile; }
            set { useAmmoStockpile = value; }
        }

        /// <summary>
        /// Is this Gun currently in the shooting animation?
        /// </summary>
        public bool IsShooting
        {
            get { return isShooting; }
        }

        /// <summary>
        /// Was a bullet fired during the last call to Update?
        /// Set value to false to "stop" bullet
        /// </summary>
        public bool BulletFired
        {
            get { return bulletFired; }
            set { bulletFired = value; }
        }

        public string Name
        {
            get { return gunName; }
        }

        public int MaxAmmo
        {
            get { return maxAmmo; }
        }

        public float ReloadSpeed
        {
            get { return reloadSpeed; }
        }

        public bool AimingDownSights
        {
            get { return aimingDownSights; }
            set { aimingDownSights = value; }
        }

        public FireType FireType
        {
            get { return fireType; }
        }

        public BulletType BulletType
        {
            get { return bulletType; }
        }

        public Vector3 Position
        {
            get { return gunMesh.Position; }
        }

        public Vector3 AimDirection
        {
            get { return aimDirection; }
        }

        public float CurrentAccuracy
        {
            get { return currentAccuracy; }
        }
    }

    class BulletParticleUpdater : ParticleEmitterUpdater
    {
        public void UpdateParticle(Particle p, float dt)
        {
            // Update the particle
            p.Age += dt;
            p.Position += p.Direction * p.Speed;
        }
    }

    class GunStats
    {
        int maxAmmo;
        float accuracy;
        FireType fireType;
        BulletType bulletType;

        public GunStats(string gunFileName)
        {
            // Read in gun data from the provided file
            StreamReader fileReader = new StreamReader(Gun.gunDirectory + gunFileName);

            string buffer;
            char[] separator = { ' ' };

            while (!fileReader.EndOfStream)
            {
                buffer = fileReader.ReadLine();

                // Tokenize line
                string[] line = buffer.Split(separator);

                if (line[0].Equals("Max_Ammo"))
                {
                    maxAmmo = int.Parse(line[1]);
                }
                else if (line[0].Equals("Accuracy"))
                {
                    accuracy = float.Parse(line[1], CultureInfo.InvariantCulture);
                }
                else if (line[0].Equals("Fire_Type"))
                {
                    if (line[1].Equals("SEMI_AUTO"))
                    {
                        fireType = FireType.SemiAuto;
                    }
                    else if (line[1].Equals("FULL_AUTO"))
                    {
                        fireType = FireType.FullAuto;
                    }
                    else
                    {
                        // Error in file. Semi-Auto default
                        fireType = FireType.SemiAuto;
                    }
                }
                else if (line[0].Equals("Bullet_Type"))
                {
                    if (line[1].Equals("PENETRATIVE"))
                    {
                        bulletType = BulletType.Penetrative;
                    }
                    else if (line[1].Equals("NON_PENETRATIVE"))
                    {
                        bulletType = BulletType.NonPenetrative;
                    }
                    else
                    {
                        // Error in file. Non-penetrative default
                        bulletType = BulletType.NonPenetrative;
                    }
                }
            }
        }

        public int MaxAmmo
        {
            get { return maxAmmo; }
        }

        public float Accuracy
        {
            get { return accuracy; }
        }

        public FireType FireType
        {
            get { return fireType; }
        }

        public BulletType BulletType
        {
            get { return bulletType; }
        }
    }
}
