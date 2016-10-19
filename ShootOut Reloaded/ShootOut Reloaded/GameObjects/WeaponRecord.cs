using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObjects
{
    class WeaponRecord
    {
        int shotsFired;
        int targetsHit;
        int multishots;
        int longshots;
        int snipershots;
        int bullseyes;
        int headshots;

        public WeaponRecord(int shotsFired, int targetsHit, int multishots, int longshots, int snipershots,
                            int bullseyes, int headshots)
        {
            this.shotsFired = shotsFired;
            this.targetsHit = targetsHit;
            this.multishots = multishots;
            this.longshots = longshots;
            this.snipershots = snipershots;
            this.bullseyes = bullseyes;
            this.headshots = headshots;
        }

        // PROPERTIES
        public int ShotsFired
        {
            get { return shotsFired; }
            set { shotsFired = value; }
        }

        public int TargetsHit
        {
            get { return targetsHit; }
            set { targetsHit = value; }
        }

        public float Accuracy
        {
            get 
            {
                if(shotsFired == 0) 
                    return 0.0f;
                else
                    return (float)targetsHit / (float)shotsFired; 
            }
        }

        public int Multishots
        {
            get { return multishots; }
            set { multishots = value; }
        }

        public int Longshots
        {
            get { return longshots; }
            set { longshots = value; }
        }

        public int Snipershots
        {
            get { return snipershots; }
            set { snipershots = value; }
        }

        public int Bullseyes
        {
            get { return bullseyes; }
            set { bullseyes = value; }
        }

        public int Headshots
        {
            get { return headshots; }
            set { headshots = value; }
        }
    }
}
