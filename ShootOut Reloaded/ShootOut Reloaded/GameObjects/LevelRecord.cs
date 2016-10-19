using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObjects
{
    class LevelRecord
    {
        public static LevelRecord ZeroRecord = new LevelRecord(0, 0, 0, 0);

        // Data for the Level Records
        int score;
        float time;
        int shotsFired;
        float accuracy;

        // Level statistics
        int numTargetsShot;
        int numMultishots;
        int numLongshots;
        int numSnipershots;
        int numBullseyes;
        int numHeadshots;
        int numKnockdowns;

        public LevelRecord(int score, float time, int shotsFired,
            float accuracy)
        {
            this.score = score;
            this.time = time;
            this.shotsFired = shotsFired;
            this.accuracy = accuracy;
        }

        /// <summary>
        /// Returns a new LevelRecord with the best data
        /// </summary>
        /// <param name="newRecord"></param>
        /// <returns></returns>
        public LevelRecord UpdateRecord(LevelRecord newRecord)
        {
            if (this == ZeroRecord)
            {
                return newRecord;
            }
            else
            {
                int bestScore = (score >= newRecord.score) ?
                    score : newRecord.score;
                float bestTime = (time <= newRecord.time) ?
                    time : newRecord.time;
                int bestShotsFired = (shotsFired <= newRecord.shotsFired) ?
                    shotsFired : newRecord.shotsFired;
                float bestAccuracy = (accuracy >= newRecord.accuracy) ?
                    accuracy : newRecord.accuracy;

                return new LevelRecord(bestScore, bestTime, bestShotsFired,
                    bestAccuracy);
            }
        }

        public bool Equals(LevelRecord other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (score == other.score) &&
                       (time == other.time) &&
                       (accuracy == other.accuracy) &&
                       (shotsFired == other.shotsFired);
            }
        }

        // PROPERTIES
        public int Score
        {
            get { return score; }
            set { score = value; }
        }

        public float Time
        {
            get { return time; }
            set { time = value; }
        }

        public int ShotsFired
        {
            get { return shotsFired; }
            set { shotsFired = value; }
        }

        public float Accuracy
        {
            get { return accuracy; }
            set { accuracy = value; }
        }

        public int NumTargetsShot
        {
            get { return numTargetsShot; }
            set { numTargetsShot = value; }
        }

        public int NumMultishots
        {
            get { return numMultishots; }
            set { numMultishots = value; }
        }

        public int NumLongshots
        {
            get { return numLongshots; }
            set { numLongshots = value; }
        }

        public int NumSnipershots
        {
            get { return numSnipershots; }
            set { numSnipershots = value; }
        }

        public int NumBullseyes
        {
            get { return numBullseyes; }
            set { numBullseyes = value; }
        }

        public int NumHeadshots
        {
            get { return numHeadshots; }
            set { numHeadshots = value; }
        }

        public int NumKnockdowns
        {
            get { return numKnockdowns; }
            set { numKnockdowns = value; }
        }
    }
}