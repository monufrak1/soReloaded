using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

using Graphics3D;

namespace GameObjects
{
    enum ShootingTargetType
    {
        Stationary,         // Target does not move
        Sliding,            // Target moves from side to side
        Shifting,           // Target moves forward and backward
        FlipUp
    }

    enum ShootingTargetCollisionType
    {
        Miss,
        Hit,
        Bullseye,
        Headshot
    }

    class ShootingTargetCollisionResult
    {
        float hitDistance;
        ShootingTargetCollisionType collisionType;

        public ShootingTargetCollisionResult(float hitDistance, ShootingTargetCollisionType collisionType)
        {
            this.hitDistance = hitDistance;
            this.collisionType = collisionType;
        }
        
        public bool IsTargetHit()
        {
            return hitDistance > 0.0f;
        }

        public float HitDistance
        {
            get { return hitDistance; }
        }

        public ShootingTargetCollisionType CollisionType
        {
            get { return collisionType; }
        }
    }

    class ShootingTarget
    {
        private static Random rand = new Random();

        private bool isActive;
        private ShootingTargetType targetType;

        private SoundEffect shotSFX;
        private SoundEffect knockdownSFX;
        private float sfxVolume;

        private Mesh targetMesh;

        private Vector3 initialPosition;
        private Vector3 initialRotation;
        private float animationSpeed;
        private bool runFallAnimation;
        private float totalTime;

        private Vector3 slidingDirection;
        private Vector3 shiftingDirection;
        private float maxMovement = 10.0f;

        public ShootingTarget(Vector3 position, Vector3 rotation, ShootingTargetType type,
                              SoundEffect shotSFX, SoundEffect knockdownSFX)
        {
            isActive = true;
            targetType = type;

            this.shotSFX = shotSFX;
            this.knockdownSFX = knockdownSFX;
            sfxVolume = 0.75f;

            slidingDirection = new Vector3(maxMovement, 0.0f, 0.0f);
            shiftingDirection = new Vector3(0.0f, 0.0f, maxMovement);

            initialPosition = position;
            initialRotation = rotation;
            animationSpeed = 3.0f;
            runFallAnimation = false;

            // Skew starting time for randomized animations
            totalTime = (float)rand.NextDouble() * 10.0f;

            // Load the target mesh
            targetMesh = MeshManager.LoadMesh("target.m3d");
            targetMesh.Position = position;
            targetMesh.RotationAngles = rotation;
            targetMesh.Scale = 3.0f;
        }

        public bool AABBCollision(BoundingBox otherAABB)
        {
            // Test collision with the target mesh if the target is active
            if(isActive)
            {
                return targetMesh.AABBCollision(otherAABB);
            }

            return false;
        }

        public void Reset()
        {
            isActive = true;
            runFallAnimation = false;

            // Restore mesh rotation angles
            targetMesh.RotationAngles = initialRotation;
        }

        public void ShootDown(ShootingTargetCollisionType collisionType)
        {
            if (isActive && collisionType != ShootingTargetCollisionType.Miss)
            {
                isActive = false;
                runFallAnimation = true;

                // Assign SFX pitch based on the collision type
                float pitch = -0.75f;
                if (collisionType == ShootingTargetCollisionType.Bullseye ||
                    collisionType == ShootingTargetCollisionType.Headshot)
                {
                    // Raise pitch
                    pitch = 0.25f;
                }

                // Play SFX
                shotSFX.Play(sfxVolume, pitch, 0.0f);
            }
        }

        public void KnockDown()
        {
            if (isActive)
            {
                isActive = false;
                runFallAnimation = true;

                // Play SFX
                knockdownSFX.Play(sfxVolume, 0.0f, 0.0f);
            }
        }

        public ShootingTargetCollisionResult HitCheck(Ray bulletDirection)
        {
            float? result = bulletDirection.Intersects(targetMesh.AABB);
            float hitDist;
            ShootingTargetCollisionType collisionType;

            if (!result.HasValue)
            {
                hitDist = 0.0f;
                collisionType = ShootingTargetCollisionType.Miss;
            }
            else
            {
                hitDist = result.Value; 

                float dy = targetMesh.AABB.Max.Y - targetMesh.AABB.Min.Y;
                float bullseyeOffset = 0.05f;
                float headshotOffset = 0.4f;

                Vector3 bullseyeMin = Vector3.Lerp(targetMesh.AABB.Min, targetMesh.AABB.Max, 0.35f);
                Vector3 bullseyeMax = Vector3.Lerp(targetMesh.AABB.Min, targetMesh.AABB.Max, 0.65f);
                bullseyeMin.Y += dy * bullseyeOffset;
                bullseyeMax.Y += dy * bullseyeOffset;

                Vector3 headshotMin = Vector3.Lerp(targetMesh.AABB.Min, targetMesh.AABB.Max, 0.4f);
                Vector3 headshotMax = Vector3.Lerp(targetMesh.AABB.Min, targetMesh.AABB.Max, 0.625f);
                headshotMin.Y += dy * headshotOffset;
                headshotMax.Y += dy * headshotOffset;

                // Determine if there is a headshot or a bullseye also
                BoundingBox bullseyeBoundingBox = new BoundingBox(bullseyeMin, bullseyeMax);
                BoundingBox headshotBoundingBox = new BoundingBox(headshotMin, headshotMax);

                if (bulletDirection.Intersects(bullseyeBoundingBox).HasValue)
                {
                    collisionType = ShootingTargetCollisionType.Bullseye;
                }
                else if (bulletDirection.Intersects(headshotBoundingBox).HasValue)
                {
                    collisionType = ShootingTargetCollisionType.Headshot;
                }
                else
                {
                    collisionType = ShootingTargetCollisionType.Hit;
                }
            }

            return new ShootingTargetCollisionResult(hitDist, collisionType);
        }

        public void Update(float dt)
        {
            // Only update if the target is active
            if (isActive)
            {
                totalTime += dt;

                // Update mesh position based on target type
                if (targetType == ShootingTargetType.Sliding)
                {
                    UpdatePositionSliding(dt);
                }
                else if (targetType == ShootingTargetType.Shifting)
                {
                    UpdatePositionShifting(dt);
                }
                else if (targetType == ShootingTargetType.FlipUp)
                {
                    UpdatePositionFlipUp(dt);
                }
            }

            // Update fall animation
            if (runFallAnimation)
            {
                Vector3 rotAngles = targetMesh.RotationAngles;
                rotAngles.X -= dt * animationSpeed;

                if (rotAngles.X < MathHelper.ToRadians(-90.0f))
                {
                    rotAngles.X = MathHelper.ToRadians(-90.0f);
                }

                targetMesh.RotationAngles = rotAngles;
            }
        }

        public void Draw(MeshRenderer meshRenderer, BoundingFrustum viewFrustum)
        {
            // Draw the target mesh
            meshRenderer.DrawMesh(targetMesh, viewFrustum);
        }

        public void DrawOcclusion(MeshRenderer meshRenderer, BoundingFrustum viewFrustum)
        {
            // Draw the target mesh occlusion
            meshRenderer.DrawMeshOcclusion(targetMesh, viewFrustum);
        }

        public void DrawShadow(MeshRenderer meshRenderer, BoundingFrustum lightViewFrustum)
        {
            // Draw the target mesh shadow
            meshRenderer.DrawMeshShadow(targetMesh, lightViewFrustum);
        }

        public void DrawReflection(MeshRenderer meshRenderer, BoundingFrustum lightViewFrustum,
            float reflectionPlaneHeight)
        {
            // Draw the target mesh shadow
            meshRenderer.DrawMeshReflection(targetMesh, lightViewFrustum, reflectionPlaneHeight);
        }

        private void UpdatePositionSliding(float dt)
        {
            // Move target side to side
            targetMesh.Position = initialPosition +
                slidingDirection * (float)Math.Sin(totalTime);
        }

        private void UpdatePositionShifting(float dt)
        {
            // Move target foward and back
            targetMesh.Position = initialPosition +
                shiftingDirection * (float)Math.Sin(totalTime);
        }

        private void UpdatePositionFlipUp(float dt)
        {

        }

        // PROPERTIES
        public bool IsActive
        {
            get { return isActive; }
        }

        public ShootingTargetType TargetType
        {
            get { return targetType; }
        }
    }
}
