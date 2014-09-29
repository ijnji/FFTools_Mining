using System;

namespace FFTools {
    public class Player {
        public float x;
        public float z;
        public float y;
        public float rot;

        public Player(float x, float z, float y, float rot) {
            this.x = x;
            this.z = z;
            this.y = y;
            this.rot = rot;
        }
        public override string ToString() {
            return "P | " +
                "px: " + x + "| " +
                "py: " + y + "| " +
                "prot: " + rot;
        }
    }
}