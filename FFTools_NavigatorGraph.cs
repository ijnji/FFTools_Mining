using System;
using System.Collections.Generic;

namespace FFTools {
    public class NavigatorGraph {
        public enum Move {NtoS, StoN, EtoW, WtoE};
        private const float DIST_PER_GRID = 1;
        private const int BUFFER_MULTIPLIER = 0;    //BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
        public GraphNode [][] NavGraph = new GraphNode[1][]; //apparently jagged arrays [][] are faster than multidimensional [,]?
        
        //min/max "in game" coordinates represented -- rounded to nearest multiple of DIST_PER_GRID
        //inclusive of min, not inclusive of max [min, max)
        public float minX = float.PositiveInfinity;
        public float minY = float.PositiveInfinity;
        public float maxX = float.NegativeInfinity;
        public float maxY = float.NegativeInfinity;

        public struct GraphNode : IComparable {
            public Location location;
            public bool[] canTravelFrom;
            //0: NtoS;
            //1: StoN;
            //2: EtoW;
            //3: WtoE;
            public int costToNode;
            public int costToTarget;
            public int fromX;
            public int fromY;
           
            public int Score {
                get {
                    return costToNode + costToTarget;
                }
            } 
            public GraphNode(Location location, bool ns, bool sn, bool ew, bool we) {
                this.location = location;
                canTravelFrom = new bool[] {ns, sn, ew, we};
                costToNode = int.MaxValue;
                costToTarget = int.MaxValue;
                fromX = -1;
                fromY = -1;
            }

            public int CompareTo (Object obj) { //-1 inst precedes obj; 0 same; +1 inst follows obj
                if (obj == null) return -1;

                GraphNode b = (GraphNode) obj;
                int instScore = ((costToNode == int.MaxValue) || (costToTarget == int.MaxValue)) ? int.MaxValue : this.costToNode + this.costToTarget;
                int objScore = ((b.costToNode == int.MaxValue) || (b.costToTarget == int.MaxValue))? int.MaxValue : b.costToNode + b.costToTarget;
                if (instScore == objScore) return 0;
                else if (instScore > objScore) return 1;
                else return -1;
            }
        }

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
            GraphNode[][] newGraph = new GraphNode[graphHeight][];
            for (int y = 0; y < graphHeight; y++) {
                newGraph[y] = new GraphNode[graphWidth];
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
                        //                      " y: " + y +
                        //                      " oldX: " + oldX + 
                        //                      " oldY: " + oldY);
                        newGraph[y][x] = this.NavGraph[oldY][oldX];
                    }
                    else {
                        newGraph[y][x] = new GraphNode(new_location, true, true, true, true);    //NavGraph Z coordinates aren't used
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
                //                      " y: " + location.y +
                //                      " minX: " + minX + 
                //                      " maxY: " + maxY);
                float tmp = location.x - this.minX;
                outX = (int) Math.Floor((tmp/DIST_PER_GRID));
                tmp = this.maxY - location.y;
                outY = (int) Math.Floor((tmp/DIST_PER_GRID));
                return true;
            }
            outX = -1;
            outY = -1;
            return false;
        }

        public void markObstacle (Location location, Move direction) {
            int x, y;
            if (this.findLocation(location, out x, out y))
                this.NavGraph[y][x].canTravelFrom[(int) direction] = false;
            else
                System.Console.WriteLine("Obstacle unmarkable, does not exist in graph: " + location.ToString());
        }

        public List <int[]> findAdjacent (int x, int y) {
            List <int[]> adjacent = new List <int[]> ();
            //4 possibilities
            //North
            if( (y+1) < NavGraph.Length) { //check within graph bounds
                if (NavGraph[y+1][x].canTravelFrom[(int)Move.StoN]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x,y+1});
                }
            }
            //East
            if( (x+1) < NavGraph[0].Length) { //check within graph bounds
                if (NavGraph[y][x+1].canTravelFrom[(int)Move.WtoE]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x+1,y});
                }
            }
            //South
            if( (y-1) >= 0) { //check within graph bounds
                if (NavGraph[y-1][x].canTravelFrom[(int)Move.NtoS]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x,y-1});
                }
            }
            //West
            if( (x-1) >= 0) { //check within graph bounds
                if (NavGraph[y][x-1].canTravelFrom[(int)Move.EtoW]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x-1,y});
                }
            }
            return adjacent;
        }

        public List <int[]> findAdjacent (GraphNode reference) {
            int x, y;
            findLocation(reference.location, out x, out y);
            return findAdjacent(x, y);
        }

        public List <Location> findPath (Location start, Location end) {
            List <Location> path = new List <Location> ();
            PriorityQueue <GraphNode> openNodes = new PriorityQueue <GraphNode> ();
            //reset all scores to MaxValue, fromX/formY to -1
            for (int y = 0; y < NavGraph.Length; y++) {
                for (int x = 0; x < NavGraph[0].Length; x++) {
                    NavGraph[y][x].costToNode = int.MaxValue;
                    NavGraph[y][x].costToTarget = int.MaxValue;
                    NavGraph[y][x].fromX = -1;
                    NavGraph[y][x].fromY = -1;
                }
            }
            System.Console.WriteLine("done clearing cost and from values");

            int startX, startY;
            int endX, endY;
            findLocation(start, out startX, out startY);
            System.Console.WriteLine("startX/y: " + startX + "," + startY);
            findLocation(end, out endX, out endY);
            System.Console.WriteLine("endX/y: " + endX + "," + endY);
            System.Console.WriteLine("done finding start/end entries in NavGraph");
            NavGraph[startY][startX].costToNode = 0;
            int currentX = startX;
            int currentY = startY;
            openNodes.addNew(NavGraph[startY][startX]);
            System.Console.WriteLine("initial openNodes size: " + openNodes.Count);
            while (openNodes.Count > 0) {
                GraphNode currentNode = openNodes.removeTop();
                findLocation(currentNode.location, out currentX, out currentY);
                if((currentX == endX) && (currentY == endY)) break;
                List <int[]> adjacentList = this.findAdjacent(currentNode);
                System.Console.WriteLine("Current node " + currentX + ", " + currentY);
                System.Console.WriteLine("# valid adjacent: " + adjacentList.Count);
                foreach (int[] adjacent in adjacentList) {
                //Calculate score for adjacent node
                    int costToNode = currentNode.costToNode + 1;
                    System.Console.WriteLine("Node " + adjacent[0] + "," + adjacent[1] + " : " + costToNode);
                    //Heuristic is just direct distance as if nothing in the way
                    int adjacentX = adjacent[0];
                    int adjacentY = adjacent[1];
                    int dx = adjacentX - endX;
                    int dy = adjacentY - endY;
                    int costToTarget = (int)Math.Ceiling(Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)));
                    int score = costToNode + costToTarget;
                    if (score < NavGraph[adjacentY][adjacentX].Score) {
                        NavGraph[adjacentY][adjacentX].costToTarget = costToTarget;
                        NavGraph[adjacentY][adjacentX].costToNode = costToNode;
                        NavGraph[adjacentY][adjacentX].fromX = currentX;
                        NavGraph[adjacentY][adjacentX].fromY = currentY;
                        openNodes.addNew(NavGraph[adjacentY][adjacentX]);
                    }
                }
                String line1 = "";
                String line2 = "";
                String line3 = "";
                String line4 = "";
                String line5 = "";
                for (int y = 0; y < this.NavGraph.Length; y++) {
                    for (int x = 0; x < this.NavGraph[y].Length; x++) {
                        line1 = line1 + "\t" + this.NavGraph[y][x].location.x.ToString("n1");
                        line2 = line2 + "\t" + this.NavGraph[y][x].location.y.ToString("n1");
                        if(this.NavGraph[y][x].costToNode != int.MaxValue)
                            line3 = line3 + "\t" + this.NavGraph[y][x].costToNode;
                        else
                            line3 = line3 + "\t" + "Max";
                        if(this.NavGraph[y][x].costToTarget != int.MaxValue)
                            line4 = line4 + "\t" + this.NavGraph[y][x].costToTarget;
                        else
                            line4 = line4 + "\t" + "Max";
                        line5 = line5 + "\t" + this.NavGraph[y][x].fromX+","+this.NavGraph[y][x].fromY;
                    }
                    System.Console.WriteLine(line1);
                    System.Console.WriteLine(line2);
                    System.Console.WriteLine(line3);
                    System.Console.WriteLine(line4);
                    System.Console.WriteLine(line5);
                    System.Console.WriteLine();
                    line1 = "";
                    line2 = "";
                    line3 = "";
                    line4 = "";
                    line5 = "";
                }
                Console.ReadLine();
            }

            //traceback to build path
            while ((currentX != startX) && (currentY != startY)) {
                path.Add(NavGraph[currentY][currentX].location);
                currentX = NavGraph[currentY][currentX].fromX;
                currentY = NavGraph[currentY][currentX].fromY;
            }
            return path;
        }

        //print graph contents
        public void Print() {
            String line1 = "";
            String line2 = "";
            String line3 = "";
            String line4 = "";
            String line5 = "";
            System.Console.WriteLine("minY: "+ this.minY + 
                                     " | maxY: " + this.maxY + 
                                     " | minX: " + this.minX + 
                                     " | maxX: " + this.maxX);

            for (int y = 0; y < this.NavGraph.Length; y++) {
                for (int x = 0; x < this.NavGraph[y].Length; x++) {
                    line1 = line1 + "\t" + this.NavGraph[y][x].location.x.ToString("n1");
                    line2 = line2 + "\t" + this.NavGraph[y][x].location.y.ToString("n1");
                    if(this.NavGraph[y][x].costToNode != int.MaxValue)
                        line3 = line3 + "\t" + this.NavGraph[y][x].costToNode;
                    else
                        line3 = line3 + "\t" + "Max";
                    if(this.NavGraph[y][x].costToTarget != int.MaxValue)
                        line4 = line4 + "\t" + this.NavGraph[y][x].costToTarget;
                    else
                        line4 = line4 + "\t" + "Max";
                    line5 = line5 + "\t" + this.NavGraph[y][x].fromX+","+this.NavGraph[y][x].fromY;
                }
                System.Console.WriteLine(line1);
                System.Console.WriteLine(line2);
                System.Console.WriteLine(line3);
                System.Console.WriteLine(line4);
                System.Console.WriteLine(line5);
                System.Console.WriteLine();
                line1 = "";
                line2 = "";
                line3 = "";
                line4 = "";
                line5 = "";
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
            //graph.Print();
            Console.ReadLine();
            System.Console.WriteLine("adding location that should already be covered");
            graph.addLocations(array2);
            graph.Print();
            //Console.ReadLine();
            //System.Console.WriteLine("adding locations that are not already covered");
            //graph.addLocations(array3);
            //graph.Print();
            Console.ReadLine();
            System.Console.WriteLine("Finding path");
            Location start = new Location((float)3.999,(float)3.999,0);
            Location end = new Location(1,1,0);
            graph.findPath(start,end);
            System.Console.WriteLine("path find done");
            graph.Print();
            Console.ReadLine();
        }
    }
}