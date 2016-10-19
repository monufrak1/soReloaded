using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

using Graphics3D;

namespace GameObjects
{
    enum ShooterGameType
    {
        ScoreAttack,
        Targets,
        TargetScore,
        TimeTrial,
        Collection,
        BullseyeChallenge,
        HeadshotChallenge
    }

    enum LevelStatus
    {
        Locked,
        Unlocked,
        Completed
    }

    class WeaponChallenge
    {
        LevelStatus status;
        ShooterGameType gameType;
        float targetScoreOrTime;


        public WeaponChallenge(LevelStatus status, ShooterGameType gameType, float targetScoreOrTime)
        {
            this.status = status;
            this.gameType = gameType;
            this.targetScoreOrTime = targetScoreOrTime;
        }

        public LevelStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        public ShooterGameType GameType
        {
            get { return gameType; }
        }

        public float TargetScoreOrTime
        {
            get { return targetScoreOrTime; }
        }
    }

    class ShooterLevel : Level
    {
        const float TARGET_TYPE_SCALE_STATIONARY = 1.0f;
        const float TARGET_TYPE_SCALE_SLIDING = 2.0f;
        const float TARGET_TYPE_SCALE_SHIFTING = 3.0f;

        const string SHOOTING_TARGET_MESH_NAME = "target.m3d";
        const string SECRET_ITEM_MESH_NAME = "grenade.m3d";

        Mesh secretItem;
        List<ShootingTarget> shootingTargetList;
        SoundEffect targetHitSFX;
        SoundEffect targetKnockdownSFX;

        public ShooterLevel(GraphicsDevice device, ContentManager content, string levelFileName)
            : base(device, content, levelFileName)
        {
            shootingTargetList = new List<ShootingTarget>();
            targetHitSFX = content.Load<SoundEffect>(@"SoundEffects\bullseye");
            targetKnockdownSFX = content.Load<SoundEffect>(@"SoundEffects\success");

            ConvertToShooterLevel();
        }

        private void ConvertToShooterLevel()
        {
            // Get all targets in the mesh list and create shooting target objects
            // from them
            Predicate<Mesh> isSecretItem = (Mesh m) => m.MeshFileName == SECRET_ITEM_MESH_NAME;
            Predicate<Mesh> isTarget = (Mesh m) => m.MeshFileName == SHOOTING_TARGET_MESH_NAME;
            List<Mesh> targets = new List<Mesh>();
            foreach (Mesh mesh in meshList)
            {
                if (isTarget(mesh))
                {
                    targets.Add(mesh);
                }
                else if (isSecretItem(mesh))
                {
                    secretItem = mesh;
                }
            }

            // Add shooting targets for each target mesh
            foreach (Mesh target in targets)
            {
                // Target type is determined by mesh size in file
                ShootingTargetType targetType = ShootingTargetType.Stationary;
                if (target.Scale == TARGET_TYPE_SCALE_STATIONARY) targetType = ShootingTargetType.Stationary;
                else if (target.Scale == TARGET_TYPE_SCALE_SLIDING) targetType = ShootingTargetType.Sliding;
                else if (target.Scale == TARGET_TYPE_SCALE_SHIFTING) targetType = ShootingTargetType.Shifting;

                ShootingTarget shootingTarget = new ShootingTarget(target.Position,
                    target.RotationAngles, targetType, targetHitSFX, targetKnockdownSFX);

                // Add the new shooting target
                shootingTargetList.Add(shootingTarget);
            }

            // Remove the targets from the mesh list
            List<Mesh> copyMeshList = new List<Mesh>(meshList);
            foreach (Mesh mesh in copyMeshList)
            {
                if (isTarget(mesh))
                {
                    meshList.Remove(mesh);
                }
            }
        }

        public bool HasSecret()
        {
            return (secretItem != null);
        }

        public List<ShootingTarget> ShootingTargetList
        {
            get { return shootingTargetList; }
        }

        public Mesh SecretItem
        {
            get { return secretItem; }
        }
    }
}
