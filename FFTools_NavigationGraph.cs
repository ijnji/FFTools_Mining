using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigationGraph {
		public Location [][] NavGraph; //apparently jagged arrays [][] are faster than multidimensional [,]?

		public NavigationGraph (int width, int height) {
			NavGraph = new Location[height][] ;
			for (int y = 0; y < height; y++) {
				NavGraph[y] = new Location[width];
			}
		}

		public void resize (int newWidth, int newHeight) {
			Location[][] newGraph = new Location[newHeight][];
			for (int y = 0; y < newHeight; y++) {
				newGraph[y] = new Location[newWidth];
			}
			int oldWidth = NavGraph[0].Length;
			int oldHeight = NavGraph.Length;

			int yDestination = (newHeight - oldHeight)/2;
			int xDestination = (newWidth - oldWidth)/2;

			for (int y = 0; y < NavGraph.Length; y++) {
				Array.Copy(NavGraph[y], 0, newGraph[yDestination], xDestination, oldWidth);
				yDestination++;
			}
			NavGraph = newGraph;
		}
	}
}