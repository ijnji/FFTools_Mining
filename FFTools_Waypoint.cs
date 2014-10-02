using System;
using System.Collections.Generic;

namespace FFTools {
	public enum MoveDirection {NtoS, StoN, EtoW, WtoE};
	public class Waypoint {
		//XYZ coordinates
		public Location location;

		//indicates whether it is possible to travel to location from direction
		public bool[] canTravelFrom = new bool[4];
		//0: public bool NtoS;
		//1: public bool StoN;
		//2: public bool EtoW;
		//3: public bool WtoE;

		public Waypoint() {
			this.location = new Location();

			this.canTravelFrom = new bool[] {true, true, true, true};
			//this.NtoS = true;
			//this.StoN = true;
			//this.EtoW = true;
			//this.WtoE = true;
		}

		public Waypoint(float x, float y, float z, bool ns, bool sn, bool ew, bool we) {
			this.location.x = x;
			this.location.y = y;
			this.location.z = z;

			this.canTravelFrom = new bool[] {ns, sn, ew, we};
			//this.NtoS = ns;
			//this.StoN = sn;
			//this.EtoW = ew;
			//this.WtoE = we;
		}

		public Waypoint(Location location) {
			this.location = location;

			this.canTravelFrom = new bool[] {true, true, true, true};
			//this.NtoS = true;
			//this.StoN = true;
			//this.EtoW = true;
			//this.WtoE = true;
		}

		public Waypoint(Location location, bool ns, bool sn, bool ew, bool we) {
			this.location = location;

			this.canTravelFrom = new bool[] {ns, sn, ew, we};
			//this.NtoS = ns;
			//this.StoN = sn;
			//this.EtoW = ew;
			//this.WtoE = we;
		}

		public override string ToString() {
			return "X: " + location.x + " | " +
				   "Y: " + location.y + " | " +	
				   "Z: " + location.z + " | " +
				   "NtoS: " + canTravelFrom[(int) MoveDirection.NtoS] + " | " +
				   "StoN: " + canTravelFrom[(int) MoveDirection.StoN] + " | " +
				   "EtoW: " + canTravelFrom[(int) MoveDirection.EtoW] + " | " +
				   "WtoE: " + canTravelFrom[(int) MoveDirection.WtoE] ;
		}
	}
}