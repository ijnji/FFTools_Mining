using System;
using System.Collections.Generic;

namespace FFTools {
	public class Waypoint : Location {
		//XYZ coordinates
		//inherited public float x;	
		//inherited public float y;	
		//inherited public float z;	

		//indicates whether it is possible to travel to location from direction
		public bool NtoS;
		public bool StoN;
		public bool EtoW;
		public bool WtoE;

		public Waypoint() {
			this.x = 0;
			this.y = 0;
			this.z = 0;

			this.NtoS = true;
			this.StoN = true;
			this.EtoW = true;
			this.WtoE = true;
		}

		public Waypoint(float x, float y, float z, bool ns, bool sn, bool ew, bool we) {
			this.x = x;
			this.y = y;
			this.z = z;

			this.NtoS = ns;
			this.StoN = sn;
			this.EtoW = ew;
			this.WtoE = we;
		}

		public Waypoint(Location location) {
			this.x = location.x;
			this.y = location.y;
			this.z = location.z;

			this.NtoS = true;
			this.StoN = true;
			this.EtoW = true;
			this.WtoE = true;
		}

		public Waypoint(Location location, bool ns, bool sn, bool ew, bool we) {
			this.x = location.x;
			this.y = location.y;
			this.z = location.z;

			this.NtoS = ns;
			this.StoN = sn;
			this.EtoW = ew;
			this.WtoE = we;
		}

		public override string ToString() {
			return "X: " + x + " | " +
				   "Y: " + y + " | " +	
				   "Z: " + z + " | " +
				   "NtoS: " + NtoS + " | " +
				   "StoN: " + StoN + " | " +
				   "EtoW: " + EtoW + " | " +
				   "WtoE: " + WtoE ;
		}
	}
}