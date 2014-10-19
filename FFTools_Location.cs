using System;
using System.Collections.Generic;

namespace FFTools {
    public class Location {
        //XYZ coordinates
        public float x;
        public float y;
        public float z;
        //indicates whether it is possible to travel to location from direction
        //public bool NtoS;
        //public bool StoN;
        //public bool EtoW;
        //public bool WtoE;
        public Location() {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }
        public Location(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public override string ToString() {
            return "[loc:" + x + "," + y + "]";
        }
        public static float findDistanceBetween (Location A, Location B) {
            float dx = A.x - B.x;
            float dy = A.y - B.y;
            return (float)Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }
    }
}