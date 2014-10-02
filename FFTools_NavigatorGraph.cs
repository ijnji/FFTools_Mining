using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigatorGraph {
		enum MoveDirection {NTOS, STON, ETOW, WTOE};

		public Waypoint [][] NavGraph; //apparently jagged arrays [][] are faster than multidimensional [,]?

		public NavigatorGraph (int width, int height) {
			this.NavGraph = new Waypoint[height][] ;
			for (int y = 0; y < height; y++) {
				this.NavGraph[y] = new Waypoint[width];
			}
		}

		public void resize (int newWidth, int newHeight) {
			Waypoint[][] newGraph = new Waypoint[newHeight][];
			for (int y = 0; y < newHeight; y++) {
				newGraph[y] = new Waypoint[newWidth];
			}
			int oldWidth = this.NavGraph[0].Length;
			int oldHeight = this.NavGraph.Length;

			int yDestination = (newHeight - oldHeight)/2;
			int xDestination = (newWidth - oldWidth)/2;

			for (int y = 0; y < this.NavGraph.Length; y++) {
				Array.Copy(NavGraph[y], 0, newGraph[yDestination], xDestination, oldWidth);
				yDestination++;
			}
			this.NavGraph = newGraph;
		}

		public void markObstacle (int x, int y, MoveDirection direction) {
			this.NavGraph[y][x].
		}
	}
}