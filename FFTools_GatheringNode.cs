using System;

namespace FFTools {
    public class GatheringNode {
        public bool vis;
        public Location location;

        public GatheringNode(bool vis, Location l) {
            this.vis = vis;
            this.location = l;
        }
        public GatheringNode(bool vis, float x, float z, float y) {
            this.vis = vis;
            this.location = new Location(x, y, z);
        }
        public override string ToString() {
            return "[gathnode:" + vis + "," + this.location + "]";
        }
    }
}