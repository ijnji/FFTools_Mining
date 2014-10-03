using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigatorGraph {
		private const float DIST_PER_GRID = 1;
		private const int BUFFER_MULTIPLIER = 0;	//BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
		public Waypoint [][] NavGraph = new Waypoint[1][]; //apparently jagged arrays [][] are faster than multidimensional [,]?

		public NavigatorGraph (Location[] locations) {
			//find dimensions in "in-game" units to hold all locations
			float totalX = 0, totalY = 0;

			//most extreme "in-game" X, Y coordinates -- used to fill each grid
			float minX = float.PositiveInfinity;
			float minY = float.PositiveInfinity;
			float maxX = float.NegativeInfinity;
			float maxY = float.NegativeInfinity;

			foreach (Location location in locations) {
				if (location.x < minX) minX = location.x;
				if (location.y < minY) minY = location.y;
				if (location.x > maxX) maxX = location.x;
				if (location.y > maxY) maxY = location.y;
			}
			System.Console.WriteLine("minY: "+minY+" | minX: "+minX+" | maxY: "+maxY+" | maxX: "+maxX);
			//round "in-game" units to nearest DIST_PER_GRID multiple
			totalX = totalX + DIST_PER_GRID - (totalX % DIST_PER_GRID);
			totalY = totalY + DIST_PER_GRID - (totalY % DIST_PER_GRID);
			if(minY > 0)
				minY = minY - (minY % DIST_PER_GRID);
			else
				minY = minY - (DIST_PER_GRID - (minY % DIST_PER_GRID));

			if(minX > 0)
				minX = minX - (minX % DIST_PER_GRID);
			else
				minX = minX - (DIST_PER_GRID - (minX % DIST_PER_GRID));

			if(maxY > 0)
				maxY = maxY - (maxY % DIST_PER_GRID);
			else
				maxY = maxY - (DIST_PER_GRID - (maxY % DIST_PER_GRID));

			if(maxX > 0)
				maxX = maxX - (maxX % DIST_PER_GRID);
			else
				maxX = maxX - (DIST_PER_GRID - (maxX % DIST_PER_GRID));

			System.Console.WriteLine("minY: "+minY+" | minX: "+minX+" | maxY: "+maxY+" | maxX: "+maxX);
			//calculate dimensions in "in-game" units needed and add buffer on all sides
			totalX = maxX - minX + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;
			totalY = maxY - minY + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;

			//# of grid square things
			int graphWidth = (int) (totalX/DIST_PER_GRID);
			int graphHeight = (int) (totalY/DIST_PER_GRID);

			//create NavGraph
			this.NavGraph = new Waypoint[graphHeight][];
			for (int y = 0; y < graphHeight; y++) {
				this.NavGraph[y] = new Waypoint[graphWidth];
			}
			System.Console.WriteLine("NavGraph created");
			System.Console.WriteLine("Height: "+graphHeight+" | Width: "+graphWidth);

			//initial "in-game" coordinates stored in [0][0] = most negative X, most positive Y + buffers applied
			float graphX = minX + DIST_PER_GRID/2;
			float graphY = maxY - DIST_PER_GRID/2;
			System.Console.WriteLine("0,0 = "+graphX+", "+ graphY);
			System.Console.WriteLine("Hit Enter");
			Console.ReadLine();
			//fill in all grids
			for (int y = 0; y < graphHeight; y++) {
				float tmp_graphX = graphX;
				for (int x = 0; x < graphWidth; x++) {
					this.NavGraph[y][x] = new Waypoint(tmp_graphX, graphY, 0, true, true, true, true);	//NavGraph Z coordinates aren't used
					tmp_graphX = tmp_graphX + DIST_PER_GRID;
				}
				graphY = graphY - DIST_PER_GRID;
			}
		}
		//returns true/false if location exists 
		public bool findLocation (Location location) {
			float minX = NavGraph[0][0].location.x - DIST_PER_GRID/2;
			float maxY = NavGraph[0][0].location.y + DIST_PER_GRID/2;
			float maxX = NavGraph[NavGraph.Length][NavGraph[0].Length].location.x + DIST_PER_GRID/2;
			float maxY = NavGraph[NavGraph.Length][NavGraph[0].Length].location.y - DIST_PER_GRID/2;

			if ((location.x >= minX) && (location.x <= maxX)
					&& (location.y >= minY) && (location.y <= maxY)){
				return true;
			}
			return false;
		}
		//overloadded findLocation to also write out X/Y indices if found
		public bool findLocation (Location location, out int x, out int y) {
			int x = -1, y = -1;
			float minX = NavGraph[0][0].location.x - DIST_PER_GRID/2;
			float maxY = NavGraph[0][0].location.y + DIST_PER_GRID/2;
			float maxX = NavGraph[NavGraph.Length][NavGraph[0].Length].location.x + DIST_PER_GRID/2;
			float maxY = NavGraph[NavGraph.Length][NavGraph[0].Length].location.y - DIST_PER_GRID/2;

			if ((location.x >= minX) && (location.x <= maxX)
					&& (location.y >= minY) && (location.y <= maxY)) {
				float tmp = location.x - minX;
				x = (int) tmp/DIST_PER_GRID;
				tmp = maxY - location.y;
				y = (int) tmp/DIST_PER_GRID;
				return true;
			}
			return false;
		}

		public void markObstacle (Location location, MoveDirection direction) {
			int x, y;
			this.NavGraph.findLocation(location, x, y);
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

		//print graph contents
		public void Print() {
			String line1 = "";
			String line2 = "";
			for (int y = 0; y < this.NavGraph.Length; y++) {
				for (int x = 0; x < this.NavGraph[y].Length; x++) {
					line1 = line1 + "\t" + this.NavGraph[y][x].location.x.ToString("n1");
					line2 = line2 + "\t" + this.NavGraph[y][x].location.y.ToString("n1");
				}
				System.Console.WriteLine(line1);
				System.Console.WriteLine(line2);
				System.Console.WriteLine();
				line1 = "";
				line2 = "";
			}
		}
	}
}