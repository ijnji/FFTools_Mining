using System;
using System.Collections.Generic;

namespace FFTools {
	public class NavigatorGraph {
		private const float DIST_PER_GRID = 1;
		private const int BUFFER_MULTIPLIER = 0;	//BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
		public Waypoint [][] NavGraph = new Waypoint[1][]; //apparently jagged arrays [][] are faster than multidimensional [,]?
		
		//min/max "in game" coordinates represented -- rounded to nearest multiple of DIST_PER_GRID
		public float minX = float.PositiveInfinity;
		public float minY = float.PositiveInfinity;
		public float maxX = float.NegativeInfinity;
		public float maxY = float.NegativeInfinity;

		public NavigatorGraph (Location[] locations) {
			//find dimensions in "in-game" units to hold all locations
			float totalX = 0, totalY = 0;

			foreach (Location location in locations) {
				if (location.x < this.minX) this.minX = location.x;
				if (location.y < this.minY) this.minY = location.y;
				if (location.x > this.maxX) this.maxX = location.x;
				if (location.y > this.maxY) this.maxY = location.y;
			}
			System.Console.WriteLine("minY: "+ this.minY +" | minX: "+ this.minX + " | maxY: "+ this.maxY + " | maxX: " + this.maxX);
			//round "in-game" units to nearest DIST_PER_GRID multiple
			totalX = totalX + DIST_PER_GRID - (totalX % DIST_PER_GRID);
			totalY = totalY + DIST_PER_GRID - (totalY % DIST_PER_GRID);
			if(this.minY > 0)
				this.minY = this.minY - (this.minY % DIST_PER_GRID);
			else
				this.minY = this.minY - (DIST_PER_GRID - (this.minY % DIST_PER_GRID));

			if(this.minX > 0)
				this.minX = this.minX - (this.minX % DIST_PER_GRID);
			else
				this.minX = this.minX - (DIST_PER_GRID - (this.minX % DIST_PER_GRID));

			if(this.maxY > 0)
				this.maxY = this.maxY - (this.maxY % DIST_PER_GRID);
			else
				this.maxY = this.maxY - (DIST_PER_GRID - (this.maxY % DIST_PER_GRID));

			if(this.maxX > 0)
				this.maxX = this.maxX - (this.maxX % DIST_PER_GRID);
			else
				this.maxX = this.maxX - (DIST_PER_GRID - (this.maxX % DIST_PER_GRID));

			System.Console.WriteLine("minY: "+ this.minY + " | minX: " + this.minX + " | maxY: " + this.maxY + " | maxX: " + this.maxX);
			//calculate dimensions in "in-game" units needed and add buffer on all sides
			totalX = this.maxX - this.minX + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;
			totalY = this.maxY - this.minY + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;

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
			float graphX = this.minX + DIST_PER_GRID/2;
			float graphY = this.maxY - DIST_PER_GRID/2;
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
			if ((location.x >= this.minX) && (location.x <= this.maxX)
					&& (location.y >= this.minY) && (location.y <= this.maxY)){
				return true;
			}
			return false;
		}
		//overloaded findLocation to also write out X/Y indices if found
		public bool findLocation (Location location, out int x, out int y) {
			int x = -1, y = -1;
			if ((location.x >= this.minX) && (location.x <= this.maxX)
					&& (location.y >= this.minY) && (location.y <= this.maxY)) {
				float tmp = location.x - this.minX;
				x = (int) tmp/DIST_PER_GRID;
				tmp = this.maxY - location.y;
				y = (int) tmp/DIST_PER_GRID;
				return true;
			}
			return false;
		}

		public void markObstacle (Location location, MoveDirection direction) {
			int x, y;
			if (this.NavGraph.findLocation(location, x, y))
				this.NavGraph[y][x].canTravelFrom[(int) direction] = false;
			else
				System.Console.WriteLine("Obstacle unmarkable, does not exist in graph: " + location.ToString());
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