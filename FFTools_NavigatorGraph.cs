using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigatorGraph {
		private const float DIST_PER_GRID = 5;
		private const int BUFFER_MULTIPLIER = 3;	//BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
		public Waypoint [][] NavGraph; //apparently jagged arrays [][] are faster than multidimensional [,]?

		public NavigatorGraph (Location[] locations) {
			//find dimensions in "in-game" units to hold all locations
			float totalX = 0, totalY = 0;

			//most extreme "in-game" X, Y coordinates -- used to fill each grid
			float negX = 0, negY = 0, posX = 0, posY = 0;

			foreach (Location location in locations) {
				if (location.x < negX) negX = location.x;
				if (location.y < negY) negY = location.y;
				if (location.x > posX) posX = location.x;
				if (location.y > posY) posY = location.y;
			}

			//round "in-game" units to nearest DIST_PER_GRID multiple
			totalX = totalX + DIST_PER_GRID - (totalX % DIST_PER_GRID);
			totalY = totalY + DIST_PER_GRID - (totalY % DIST_PER_GRID);
			if(negY > 0)
				negY = negY - (negY % DIST_PER_GRID);
			else
				negY = negY - (DIST_PER_GRID - (negY % DIST_PER_GRID));

			if(negX > 0)
				negX = negX - (negX % DIST_PER_GRID);
			else
				negX = negX - (DIST_PER_GRID - (negX % DIST_PER_GRID));

			if(posY > 0)
				posY = posY - (posY % DIST_PER_GRID);
			else
				posY = posY - (DIST_PER_GRID - (posY % DIST_PER_GRID));

			if(posX > 0)
				posX = posX - (posX % DIST_PER_GRID);
			else
				posX = posX - (DIST_PER_GRID - (posX % DIST_PER_GRID));

			//calculate dimensions in "in-game" units needed and add buffer on all sides
			totalX = posX - negX + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;
			totalY = posY - posY + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;

			//# of grid square things
			int graphWidth = (int) (totalX/DIST_PER_GRID);
			int graphHeight = (int) (totalY/DIST_PER_GRID);

			//create NavGraph
			this.NavGraph = new Waypoint[graphHeight][];
			for (int y = 0; y < graphHeight; y++) {
				this.NavGraph[y] = new Waypoint[graphWidth];
			}

			//initial "in-game" coordinates stored in [0][0] = most negative X, most positive Y + buffers applied
			float graphX = negX - BUFFER_MULTIPLIER*DIST_PER_GRID + DIST_PER_GRID/2;
			float graphY = posY + BUFFER_MULTIPLIER*DIST_PER_GRID - DIST_PER_GRID/2;

			//fill in all grids
			for (int y = 0; y < graphHeight; y++) {
				for (int x = 0; x < graphWidth; x++) {
					this.NavGraph[y][x] = new Waypoint (graphX, graphY, 0, true, true, true, true);	//NavGraph Z coordinates aren't used
					graphX = graphX + DIST_PER_GRID;
				}
				graphY = graphY + DIST_PER_GRID;
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
			this.NavGraph[y][x].canTravelFrom[(int) direction] = false;
		}

		//from and to coordinates should be adjacent
		public bool canNavigate (int fromX, int fromY, int toX, int toY) {
			if (fromY == toY) { //EtoW or WtoE
				int xDir = fromX - toX;
				if(Math.Abs(xDir) > 1) return false; //not adjacent
				if(xDir == 0) return true;			 //same coordinates, technically true
				if(xDir > 0) 	//EtoW
					return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.EtoW];
				else			//WtoE
					return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.WtoE];
			}

			if (fromX == toX) { //NtoS or NtoS
				int yDir = fromY - toY;
				if(Math.Abs(yDir) > 1) return false; //not adjacent
				if(yDir == 0) return true;			 //same coordinates, technically true
				if(yDir > 0) 	//StoN
					return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.StoN];
				else			//NtoS
					return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.NtoS];
			}
			return false; //not adjacent
		}
	}
}