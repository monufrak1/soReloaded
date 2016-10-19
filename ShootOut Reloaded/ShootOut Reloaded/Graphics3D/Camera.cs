using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphics3D
{
    class FirstPersonCamera
    {
        private Vector3 position;
        private Vector3 look;
        private Vector3 up;
        private Vector3 right;

        private Vector3 moveDirection;

        private Vector3 initialLook;
        private Vector3 initialUp;
        private Vector3 initialRight;

        private float pitchAngle;
        private float rotationAngle;
        private float twistAngle;

        private float twistSpeed;
        private float twistMaxAngle;

        private bool twistLeft;
        private bool twistRight;

        private float rotationDelta;

        private float moveSpeed;
        private float moveDelta;

        private float upDownSensitivity;
        private float leftRightSensitivity;
        private bool invertY;

        private float drawDistance;   // Used for Frustum computation ONLY
        private float aspectRatio;    // Used for Frustum computation ONLY

        private bool freeFlyEnabled;
        private float pitchMinDegrees;
        private float pitchMaxDegrees;

        private float randomShakedt;
        private float randomShakeTime;  // Time between shakes
        private float randomShakeAmount;
        private bool randomShakeEnabled;

        private Matrix viewMatrix;
        private Matrix projectionMatrix;

        private static int DEFAULT_MOUSE_X = 100;
        private static int DEFAULT_MOUSE_Y = 100;
        private MouseState prevMouseState;

        private GamePadState gamePadState;
        private GamePadState prevGamePadState;
        private float prevLeftStickY;

        private Vector2 aabbSize;
        private BoundingBox aabb;

        private Random random;
        private float randAmount;

        public FirstPersonCamera()
            : this(Vector3.Zero, Vector3.Forward)
        {
        }

        public FirstPersonCamera(Vector3 startPosition)
            : this(startPosition, Vector3.Forward)
        {
        }

        public FirstPersonCamera(Vector3 startPosition, Vector3 startLook)
        {
            initialLook = Vector3.Forward;
            initialRight = Vector3.Right;
            initialUp = Vector3.Up;

            position = startPosition;

            look = initialLook;
            up    = initialUp;
            right = initialRight;

            SetLookDirection(startLook);
            twistAngle = 0.0f;

            twistSpeed = 0.25f;
            twistMaxAngle = MathHelper.ToRadians(2.5f);

            moveSpeed = 1.0f;
            upDownSensitivity = 1.0f;
            leftRightSensitivity = 1.0f;

            freeFlyEnabled = true;
            pitchMinDegrees = -90.0f;
            pitchMaxDegrees = 90.0f;

            randomShakeTime = 0.1f;
            randomShakeAmount = MathHelper.ToRadians(0.5f);
            randomShakeEnabled = false;

            drawDistance = 1000.0f;
            aspectRatio = 1.0f;

            Mouse.SetPosition(DEFAULT_MOUSE_X,
                              DEFAULT_MOUSE_Y);
            prevMouseState = Mouse.GetState();

            aabbSize = Vector2.One;

            random = new Random();

            // Create default matrix values
            viewMatrix = Matrix.CreateLookAt(position, position + look, up);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                aspectRatio, 0.1f, 1000.0f);
        }

        public void Update(float dt)
        {
            Vector3 prevPosition = position;
            randomShakedt += dt;

#if XBOX
            // Get XBOX Input
            prevGamePadState = gamePadState;
            gamePadState = GamePad.GetState(ActivePlayer.PlayerIndex);

            // Adjust position
            Vector3 moveDir = look;

            if(freeFlyEnabled)
            {
                if (gamePadState.IsButtonDown(Buttons.LeftShoulder)) position += up * dt * moveSpeed;
                if (gamePadState.IsButtonDown(Buttons.RightShoulder)) position -= up * dt * moveSpeed;
                moveDelta = 1.0f;
            }
            else
            {
                moveDir.Y = 0;
                moveDir.Normalize();

                // Adjust movement bleedoff speed
                if (gamePadState.ThumbSticks.Left.Length() > 0.0f)
                {
                    moveDelta += dt * 5.0f;
                    prevLeftStickY = 0.0f;
                }
                else
                {
                    moveDelta -= dt * 5.0f;

                    if (prevLeftStickY == 0.0f)
                    {
                        prevLeftStickY = prevGamePadState.ThumbSticks.Left.Y;
                    }

                    if (prevGamePadState.IsButtonDown(Buttons.LeftThumbstickDown) ||
                        prevGamePadState.IsButtonDown(Buttons.LeftThumbstickLeft) ||
                        prevGamePadState.IsButtonDown(Buttons.LeftThumbstickRight))
                    {
                        moveDelta = 0.0f;
                    }

                    if (moveDelta > 0.0f && prevLeftStickY > 0.25f)
                    {
                        position += moveDir * dt * moveSpeed * moveDelta;
                    }
                }

                if (moveDelta > 1.0f) moveDelta = 1.0f;
                if (moveDelta < 0.0f) moveDelta = 0.0f;

                // Update twist angle
                if(gamePadState.IsButtonUp(Buttons.LeftThumbstickLeft) &&
                    gamePadState.IsButtonUp(Buttons.LeftThumbstickRight))
                {
                    if(twistAngle > 0.0f)
                    {
                        twistAngle -= twistSpeed * dt;
                        if(twistAngle < 0.0f)
                        {
                            twistAngle = 0.0f;
                        }
                    }
                    else
                    {
                        twistAngle += twistSpeed * dt;
                        if(twistAngle > 0.0f)
                        {
                            twistAngle = 0.0f;
                        }
                    }
                }
                else
                {
                    twistAngle = gamePadState.ThumbSticks.Left.X * twistMaxAngle;
                }
            }

            position += moveDir * gamePadState.ThumbSticks.Left.Y * dt * moveSpeed * moveDelta *
                (gamePadState.IsButtonDown(Buttons.LeftThumbstickDown) && !freeFlyEnabled ? 0.75f : 1.0f);
            position += right * gamePadState.ThumbSticks.Left.X * dt * moveSpeed * (freeFlyEnabled ? 1.0f : 0.6f);

            // Adjust angle speeds
            if (freeFlyEnabled)
            {
                rotationDelta = 1.0f;
            }
            else
            {
                float increaseSpeed = 3.0f;
                float maxAmount = 2.0f;
                float movementAmount = 0.25f;
                if (gamePadState.ThumbSticks.Right.X >= movementAmount ||
                    gamePadState.ThumbSticks.Right.X <= -movementAmount ||
                    gamePadState.ThumbSticks.Right.Y >= movementAmount ||
                    gamePadState.ThumbSticks.Right.Y <= -movementAmount)
                {
                    rotationDelta += dt * increaseSpeed;
                    if (rotationDelta > maxAmount) rotationDelta = maxAmount;
                }
                else
                {
                    rotationDelta = 1.0f;
                }
            }

            // Adjust look angles
            if(invertY)
            {
                pitchAngle += rotationDelta * gamePadState.ThumbSticks.Right.Y * dt * upDownSensitivity * -1.0f;
            }
            else
            {
                pitchAngle += rotationDelta * gamePadState.ThumbSticks.Right.Y * dt * upDownSensitivity;
            }

            rotationAngle -= rotationDelta * gamePadState.ThumbSticks.Right.X * dt * leftRightSensitivity;
            

#elif WINDOWS
            // Get Keyboard Input
            KeyboardState keyboardState = Keyboard.GetState();
            if(keyboardState.IsKeyDown(Keys.W)) position += look * dt * moveSpeed * moveDelta;
            if(keyboardState.IsKeyDown(Keys.S)) position -= look * dt * moveSpeed * (freeFlyEnabled ? 1.0f : 0.6f);
            if(keyboardState.IsKeyDown(Keys.D)) position += right * dt * moveSpeed * (freeFlyEnabled ? 1.0f : 0.6f);
            if(keyboardState.IsKeyDown(Keys.A)) position -= right * dt * moveSpeed * (freeFlyEnabled ? 1.0f : 0.6f);
            if(keyboardState.IsKeyDown(Keys.I)) pitchAngle += dt;
            if(keyboardState.IsKeyDown(Keys.K)) pitchAngle -= dt;
            if(keyboardState.IsKeyDown(Keys.L)) rotationAngle -= dt;
            if(keyboardState.IsKeyDown(Keys.J)) rotationAngle += dt;

            if(freeFlyEnabled)
            {
                if(keyboardState.IsKeyDown(Keys.E)) position += up * dt * moveSpeed;
                if(keyboardState.IsKeyDown(Keys.Q)) position -= up * dt * moveSpeed;
                moveDelta = 1.0f;
            }
            else
            {
                // Adjust movement bleedoff speed
                if (keyboardState.IsKeyDown(Keys.W))
                {
                    moveDelta += dt * 5.0f;
                }
                else
                {
                    moveDelta -= dt * 7.5f;

                    if (moveDelta > 0.0f)
                    {
                        position += look * dt * moveSpeed * moveDelta;
                    }
                }

                if (moveDelta > 1.0f) moveDelta = 1.0f;
                if (moveDelta < 0.0f) moveDelta = 0.0f;

                // Set twist angle
                if (keyboardState.IsKeyDown(Keys.A))
                {
                    twistLeft = true;
                }
                else
                {
                    twistLeft = false;
                }

                if(keyboardState.IsKeyDown(Keys.D))
                {
                    twistRight = true;
                }
                else
                {
                    twistRight = false;
                }
            } 

            // Update twist angle
            if (twistRight && twistLeft)
            {
                twistRight = false;
                twistLeft = false;
            }

            if (twistRight)
            {
                twistAngle += dt * twistSpeed;
            }
            else if (twistAngle > 0.0f)
            {
                twistAngle -= dt * 2.0f * twistSpeed;
                if (twistAngle < 0.0f)
                {
                    twistAngle = 0.0f;
                }
            }

            if (twistLeft)
            {
                twistAngle -= dt * twistSpeed;
            }
            else if (twistAngle < 0.0f)
            {
                twistAngle += dt * 2.0f * twistSpeed;
                if (twistAngle > 0.0f)
                {
                    twistAngle = 0.0f;
                }
            }

            // Get Mouse Input
            MouseState mouseState = Mouse.GetState();
            if(!prevMouseState.Equals(mouseState))
            {
                pitchAngle -= (mouseState.Y - prevMouseState.Y) * 
                    dt * upDownSensitivity;

                rotationAngle -= (mouseState.X - prevMouseState.X) *
                    dt * leftRightSensitivity;

                Mouse.SetPosition(DEFAULT_MOUSE_X,
                                  DEFAULT_MOUSE_Y);
            }
#endif

            // Lock pitch angle
            if (pitchAngle > MathHelper.ToRadians(pitchMaxDegrees))
            {
                pitchAngle = MathHelper.ToRadians(pitchMaxDegrees);
            }
            else if (pitchAngle < MathHelper.ToRadians(pitchMinDegrees))
            {
                pitchAngle = MathHelper.ToRadians(pitchMinDegrees);
            }

            // Lock twist angle
            if(twistAngle > twistMaxAngle)
            {
                twistAngle = twistMaxAngle;
            }
            else if(twistAngle < -twistMaxAngle)
            {
                twistAngle = -twistMaxAngle;
            }

            // Update move direction
            moveDirection = Vector3.Subtract(position, prevPosition);
            moveDirection.Normalize();

            // Build new AABB
            aabb = new BoundingBox(
                new Vector3(position.X - aabbSize.X, position.Y - aabbSize.Y, position.Z - aabbSize.X),
                new Vector3(position.X + aabbSize.X, position.Y + aabbSize.Y/2, position.Z + aabbSize.X));

            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            // Update rotation
            Matrix rotation = Matrix.CreateRotationY(rotationAngle);
            look = Vector3.TransformNormal(initialLook, rotation);
            right = Vector3.TransformNormal(initialRight, rotation);

            // Update pitch
            rotation = Matrix.CreateFromAxisAngle(right, pitchAngle);
            look = Vector3.TransformNormal(look, rotation);
            up = Vector3.TransformNormal(initialUp, rotation);

            // Update twist
            Matrix lookRotation = Matrix.CreateFromAxisAngle(look, twistAngle);
            up = Vector3.TransformNormal(up, lookRotation);

            // Apply random shaking if needed
            ApplyRandomShaking();

            // Update view matrix
            viewMatrix = Matrix.CreateLookAt(position, position + look, up);
        }

        private void SetLookDirection(Vector3 newLook)
        {
            newLook.Normalize();

            float pitDot = Vector3.Dot(initialLook,
                Vector3.Normalize(new Vector3(initialLook.X, newLook.Y, initialLook.Z)));
            float rotDot = Vector3.Dot(initialLook,
                Vector3.Normalize(new Vector3(newLook.X, 0.0f, newLook.Z)));

            pitchAngle = (float)Math.Acos((double)pitDot);
            rotationAngle = (float)Math.Acos((double)rotDot);

            if (newLook.Y < 0.0f) pitchAngle = -pitchAngle;
            if (newLook.X > 0.0f) rotationAngle = -rotationAngle;

            UpdateViewMatrix();
        }

        private void ApplyRandomShaking()
        {
            if(randomShakeEnabled)
            {
                // Update angle if needed
                if(randomShakedt > randomShakeTime)
                {
                    randAmount = (float)random.NextDouble() * randomShakeAmount
                        * ((random.Next() % 2 == 0) ? -1.0f : 1.0f);

                    randomShakedt = 0.0f;
                }

                // Create random rotation matrices
                Matrix rightRandomMtx = Matrix.CreateFromAxisAngle(right, randAmount);
                Matrix upRandomMtx = Matrix.CreateFromAxisAngle(up, randAmount);

                // Transform the look vector
                look = Vector3.TransformNormal(look, rightRandomMtx);
                look = Vector3.TransformNormal(look, upRandomMtx);
            }
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateViewMatrix();
            }
        }

        public Vector3 Look
        {
            get { return look; }
            set { SetLookDirection(value); }
        }

        public Vector3 Up
        {
            get { return up; }
        }

        public Vector3 Right
        {
            get { return right; }
        }

        public Vector3 MoveDirection
        {
            get { return moveDirection; }
        }

        public float PitchAngle
        {
            get { return pitchAngle; }
        }

        public float RotationAngle
        {
            get { return rotationAngle; }
        }

        public float MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = value; }
        }

        public float VerticalSensitivity
        {
            set { upDownSensitivity = value; }
        }

        public float HorizontalSensitivity
        {
            set { leftRightSensitivity = value; }
        }

        public bool InvertY
        {
            set { invertY = value; }
        }

        public Matrix View
        {
            get { return viewMatrix; }
        }

        public Matrix Projection
        {
            get { return projectionMatrix; }
            set { projectionMatrix = value; }
        }

        public float DrawDistance
        {
            get { return drawDistance; }
            set { drawDistance = value; }
        }

        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; }
        }

        public bool FreeFlyEnabled
        {
            get { return freeFlyEnabled; }
            set { freeFlyEnabled = value; }
        }

        public float PitchMinDegrees
        {
            get { return pitchMinDegrees; }
            set 
            {
                if (value > 0.0f || value < -90.0f)
                    pitchMinDegrees = -90.0f;
                else
                    pitchMinDegrees = value; 
            }
        }

        public float PitchMaxDegrees
        {
            get { return pitchMaxDegrees; }
            set
            {
                if (value < 0.0f || value > 90.0f)
                    pitchMaxDegrees = 90.0f;
                else
                    pitchMaxDegrees = value;
            }
        }

        public float RandomShakeTime
        {
            get { return randomShakeTime; }
            set { randomShakeTime = value; }
        }

        public float RandomShakeAmount
        {
            get { return randomShakeAmount; }
            set { randomShakeAmount = value; }
        }

        public bool RandomShakeEnabled
        {
            get { return randomShakeEnabled; }
            set { randomShakeEnabled = value; }
        }

        public BoundingFrustum ViewFrustum
        {
            get
            { 
                Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
                    aspectRatio, 0.1f, drawDistance);
                return new BoundingFrustum(viewMatrix * projection); 
            }
        }

        public Vector2 AABBSize
        {
            get { return aabbSize; }
            set { aabbSize = value; }
        }

        public BoundingBox AABB
        {
            get { return aabb; }
        }
    }
}
