using System;

namespace FFTools {
    public class MineralDeposit {
        public bool vis;
        public float x;
        public float z;
        public float y;

        public MineralDeposit(bool vis, float x, float z, float y) {
            this.vis = vis;
            this.x = x;
            this.z = z;
            this.y = y;
        }
        public override string ToString() {
            return "MD | " +
                "vis: " + vis + " | " +
                "mx: " + x + " | " +
                "my: " + y;
        }
    }
}