using System;
using System.Collections.Generic;

namespace FFTools {
    public class NavigatorGraph {
        private const float DIST_PER_GRID = 1;
        private const int BUFFER_MULTIPLIER = 0;    //BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
        public Waypoint [][] NavGraph = new Waypoint[1][]; //apparently jagged arrays [][] are faster than multidimensional [,]?
        
        //min/max "in game" coordinates represented -- rounded to nearest multiple of DIST_PER_GRID
        public float minX = float.PositiveInfinity;
        public float minY = float.PositiveInfinity;
        public float maxX = float.NegativeInfinity;
        public float maxY = float.NegativeInfinity;

        public NavigatorGraph() {
        }

        public int Size {
            get {
            	if(this.NavGraph == null)
            		return 0;
            	if(this.NavGraph[0] == null)
            		return 0;
                return this.NavGraph.Length * this.NavGraph[0].Length;
            }
        }

        public void addLocations (Location[] newLocations) {
            List <Location> locations = new List <Location> ();
            foreach (Location location in newLocations) {
                if (!this.findLocation(location))
                    locations.Add(location);
            }
            if(locations.Count == 0) //all newLocations are already covered
            	return;

            //only need 4 corners of old graph to build encompassing new graph
            if(this.Size != 0) {
            	locations.Add(this.NavGraph[0][0].location);
            	locations.Add(this.NavGraph[0][NavGraph.Length-1].location);
            	locations.Add(this.NavGraph[NavGraph[0].Length-1][0].location);
            	locations.Add(this.NavGraph[NavGraph[0].Length-1][NavGraph.Length-1].location);
            }

            //find dimensions in "in-game" units to hold all locations
            float totalX = 0, totalY = 0;
        	float tmp_minX = float.PositiveInfinity;
        	float tmp_minY = float.PositiveInfinity;
        	float tmp_maxX = float.NegativeInfinity;
        	float tmp_maxY = float.NegativeInfinity;

            foreach (Location location in locations) {
                if (location.x < tmp_minX) tmp_minX = location.x;
                if (location.y < tmp_minY) tmp_minY = location.y;
                if (location.x > tmp_maxX) tmp_maxX = location.x;
                if (location.y > tmp_maxY) tmp_maxY = location.y;
            }
            //round "in-game" units to nearest DIST_PER_GRID multiple
            if(tmp_minY % DIST_PER_GRID != 0) {
            	if(tmp_minY > 0)
	                tmp_minY = tmp_minY - (tmp_minY % DIST_PER_GRID);
            	else
	                tmp_minY = tmp_minY - (DIST_PER_GRID - (tmp_minY % DIST_PER_GRID));
	        }
	        if(tmp_maxY % DIST_PER_GRID != 0) {        
            	if(tmp_maxY > 0)
	                tmp_maxY = tmp_maxY - (tmp_maxY % DIST_PER_GRID);
            	else
	                tmp_maxY = tmp_maxY - (DIST_PER_GRID - (tmp_maxY % DIST_PER_GRID));
	        }
	        if(tmp_minX % DIST_PER_GRID != 0) {
            	if(tmp_minX > 0)
	                tmp_minX = tmp_minX - (tmp_minX % DIST_PER_GRID);
    	        else
                	tmp_minX = tmp_minX - (DIST_PER_GRID - (tmp_minX % DIST_PER_GRID));
            }
            if(tmp_maxX % DIST_PER_GRID != 0) {
            	if(tmp_maxX > 0)
	                tmp_maxX = tmp_maxX - (tmp_maxX % DIST_PER_GRID);
            	else
	                tmp_maxX = tmp_maxX - (DIST_PER_GRID - (tmp_maxX % DIST_PER_GRID));
	        }

            //calculate dimensions in "in-game" units needed and add buffer on all sides
            totalX = tmp_maxX - tmp_minX + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;
            totalY = tmp_maxY - tmp_minY + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;

            //# of grid square things
            int graphWidth = (int) (totalX/DIST_PER_GRID);
            int graphHeight = (int) (totalY/DIST_PER_GRID);

            //create new graph 
            Waypoint[][] newGraph = new Waypoint[graphHeight][];
            for (int y = 0; y < graphHeight; y++) {
                newGraph[y] = new Waypoint[graphWidth];
            }

            //populate new graph 
            //initial "in-game" coordinates stored in [0][0] = most negative X, most positive Y + buffers applied
            float graphX = tmp_minX + DIST_PER_GRID/2;
            float graphY = tmp_maxY - DIST_PER_GRID/2;
            //fill in all grids
            for (int y = 0; y < graphHeight; y++) {
                float tmp_graphX = graphX;
                for (int x = 0; x < graphWidth; x++) {
                	int oldX, oldY;
                	Location new_location = new Location(tmp_graphX, graphY, 0);
                	if(this.findLocation(new_location, out oldX, out oldY)) { 
                	//if location was in old graph, copy old graph properties
                		//System.Console.WriteLine("x: " + x +
                		//						" y: " + y +
                		//						" oldX: " + oldX + 
                		//						" oldY: " + oldY);
	                    newGraph[y][x] = this.NavGraph[oldY][oldX];
                	}
                	else {
                    	newGraph[y][x] = new Waypoint(new_location, true, true, true, true);    //NavGraph Z coordinates aren't used
                    }
                    tmp_graphX = tmp_graphX + DIST_PER_GRID;
                }
                graphY = graphY - DIST_PER_GRID;
            }
            this.NavGraph = newGraph;
            this.minX = tmp_minX;
            this.minY = tmp_minY;
            this.maxX = tmp_maxX;
            this.maxY = tmp_maxY;
        }
        //returns true/false if location exists 
        public bool findLocation (Location location) {
        	if (this.Size == 0) return false;
            if ((location.x >= this.minX) && (location.x <= this.maxX)
                    && (location.y >= this.minY) && (location.y <= this.maxY)){
                return true;
            }
            return false;
        }
        //overloaded findLocation to also write out X/Y indices if found
        public bool findLocation (Location location, out int outX, out int outY) {
        	if (this.Size == 0) {
        		outX = -1;
        		outY = -1;
        		return false;
        	}
            if ((location.x >= this.minX) && (location.x <= this.maxX)
                    && (location.y >= this.minY) && (location.y <= this.maxY)) {
                //System.Console.WriteLine("x: " + location.x +
                //						" y: " + location.y +
                //						" minX: " + minX + 
                //						" maxY: " + maxY);
                float tmp = location.x - this.minX;
                outX = (int) Math.Round((tmp/DIST_PER_GRID));
                tmp = this.maxY - location.y;
                outY = (int) Math.Round((tmp/DIST_PER_GRID));
                return true;
            }
            outX = -1;
            outY = -1;
            return false;
        }

        public void markObstacle (Location location, MoveDirection direction) {
            int x, y;
            if (this.findLocation(location, out x, out y))
                this.NavGraph[y][x].canTravelFrom[(int) direction] = false;
            else
                System.Console.WriteLine("Obstacle unmarkable, does not exist in graph: " + location.ToString());
        }

        //from and to coordinates should be adjacent
        public bool canNavigate (int fromX, int fromY, int toX, int toY) {
            if (fromY == toY) { //EtoW or WtoE
                int xDir = fromX - toX;
                if(Math.Abs(xDir) > 1) return false; //not adjacent
                if(xDir == 0) return true;           //same coordinates, technically true
                if(xDir > 0)     //EtoW
                    return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.EtoW];
                else             //WtoE
                    return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.WtoE];
            }

            if (fromX == toX) { //NtoS or StoN
                int yDir = fromY - toY;
                if(Math.Abs(yDir) > 1) return false; //not adjacent
                if(yDir == 0) return true;           //same coordinates, technically true
                if(yDir > 0)     //StoN
                    return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.StoN];
                else             //NtoS
                    return this.NavGraph[toY][toX].canTravelFrom[(int)MoveDirection.NtoS];
            }
            return false; //not adjacent
        }

        public List <Locations> findPath (Location start, Location end) {

        }

        //print graph contents
        public void Print() {
            String line1 = "";
            String line2 = "";
            System.Console.WriteLine("minY: "+ this.minY + 
            						 " | maxY: " + this.maxY + 
									 " | minX: " + this.minX + 
            						 " | maxX: " + this.maxX);
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

        public static void Main() {
        	
            //NavigatorGraph test creation
            Location one = new Location(1, 1, 0);
            Location two = new Location(4, 4, 0);
            Location three = new Location(3, 2, 0);
            Location four = new Location(4, 8, 0);
            Location five = new Location(-2, -2, 0);
            Location[] array = {one, two};
            Location[] array2 = {three};
            Location[] array3 = {four, five};
            NavigatorGraph graph = new NavigatorGraph();
            graph.addLocations(array);
            graph.Print();
            Console.ReadLine();
            System.Console.WriteLine("adding location that should already be covered");
            graph.addLocations(array2);
            graph.Print();
            Console.ReadLine();
            System.Console.WriteLine("adding locations that are not already covered");
            graph.addLocations(array3);
            graph.Print();
            Console.ReadLine();

        }
    }
}