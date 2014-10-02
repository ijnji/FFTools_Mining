using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigatorGraph {
		public Location [][] NavGraph; //apparently jagged arrays [][] are faster than multidimensional [,]?
		public

		public NavigatorGraph (int width, int height) {
			this.NavGraph = new Location[height][] ;
			for (int y = 0; y < height; y++) {
				this.NavGraph[y] = new Location[width];
			}
		}

		public void resize (int newWidth, int newHeight) {
			Location[][] newGraph = new Location[newHeight][];
			for (int y = 0; y < newHeight; y++) {
				newGraph[y] = new Location[newWidth];
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
	}
}