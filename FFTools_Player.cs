using System;

namespace FFTools {
    public class Player {
        public float rot;
        public Location location;

        public Player(Location l, float rot) {
            this.rot = rot;
            this.location = l;
        }

        public Player(float x, float z, float y, float rot) {
            this.rot = rot;
            this.location = new Location(x, y, z);
        }
        public override string ToString() {
            return "[player:" + this.location + "," + rot + "]";
        }
        // Orientation player should face to target location.
        public float findOrientationRelativeTo(Location tLocation) {
            float dx = tLocation.x - this.location.x;
            float dy = tLocation.y - this.location.y;
            if (dy > 0) {
                if (dx > 0) {
                    return (float)Math.Atan(dx/dy);
                } else if (dx < 0) {
                    return 0 - (float)Math.Atan(-dx/dy);
                } else {
                    return 0;
                }
            } else if (dy < 0) {
                if (dx > 0) {
                    return (float)(Math.PI/2) + (float)Math.Atan(-dy/dx); 
                } else if (dx < 0) {
                    return 0 - (float)(Math.PI/2) - (float)Math.Atan(dy/dx);
                } else {
                    return (float)3.140;
                }
            } else {
                if (dx > 0) {
                    return (float)(Math.PI/2);
                } else if (dx < 0) {
                    return 0 - (float)(Math.PI/2);
                } else {
                    return 0;
                }
            }
        }
        // Angle between player and target location.
        public float findAngleBetween(Location tLocation) {
            float prot = this.rot;
            float trot = this.findOrientationRelativeTo(tLocation);
            float drot = Math.Max(prot, trot) - Math.Min(prot, trot);
            if ( drot <= (float)Math.PI ) {
                if (prot >= trot) {
                    return -1 * drot;
                } else {
                    return drot;
                }
            } else {
                if (prot >= trot) {
                    return (float)(2*Math.PI) - drot;
                } else {
                    return -1 * ((float)(2*Math.PI) - drot);
                }
            }
        }
    }
}