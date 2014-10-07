using System;
using System.Collections.Generic;

namespace FFTools {
    public class NavigatorGraph {
        public enum Move {NtoS, StoN, EtoW, WtoE};
        private const float DIST_PER_GRID = 1;
        private const int BUFFER_MULTIPLIER = 0;    //BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph
        public GraphNode [][] NavGraph = new GraphNode[1][]; //apparently jagged arrays [][] are faster than multidimensional [,]?

        /*
        NavGraph imagined like this, ex 3x3:
        
        2  minX, maxY    |              |   maxX, maxY
        --------------------------------------------------
        1                |              |
        --------------------------------------------------
        0  minX, minY    |              |   maxX, minY
        --------------------------------------------------
               0         |      1       |        2   
        */
        

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
                    return (costToNode == int.MaxValue) || (costToTarget == int.MaxValue) ? int.MaxValue :  costToNode + costToTarget;
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
                locations.Add(this.NavGraph[0][NavGraph[0].Length-1].location);
                locations.Add(this.NavGraph[NavGraph.Length-1][0].location);
                locations.Add(this.NavGraph[NavGraph.Length-1][NavGraph[0].Length-1].location);
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
            GraphNode[][] newGraph = new GraphNode[graphWidth][];
            for (int x = 0; x < graphWidth; x++) {
                newGraph[x] = new GraphNode[graphHeight];
            }

            //populate new graph 
            //initial "in-game" coordinates stored in [0][0] = most negative X, most negative Y + buffers applied
            float graphX = tmp_minX + DIST_PER_GRID/2;
            float graphY = tmp_minY + DIST_PER_GRID/2;
            float tmp_graphX = graphX;
            //fill in all grids
            for (int x = 0; x < graphWidth; x++) {
                float tmp_graphY = graphY;
                for (int y = 0; y < graphHeight; y++) {
                    int oldX, oldY;
                    Location new_location = new Location(tmp_graphX, tmp_graphY, 0);
                    if(this.findLocation(new_location, out oldX, out oldY)) { 
                        newGraph[x][y] = this.NavGraph[oldX][oldY];
                    }
                    else {
                        newGraph[x][y] = new GraphNode(new_location, true, true, true, true);    //NavGraph Z coordinates aren't used
                    }
                    tmp_graphY = tmp_graphY + DIST_PER_GRID;
                }
                tmp_graphX = tmp_graphX + DIST_PER_GRID;
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

                //min X,Y is at [NavGraph.Length][0]
                //max X,Y is at [0][NavGraph[0].Length]
                float tmp = location.x - this.minX;
                outX = (int) Math.Floor((tmp/DIST_PER_GRID));
                tmp = location.y - this.minY;  //-1 because max is not inclusive
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
                this.NavGraph[x][y].canTravelFrom[(int) direction] = false;
            else
                System.Console.WriteLine("Obstacle unmarkable, does not exist in graph: " + location.ToString());
        }

        public List <int[]> findAdjacent (int x, int y) {
            List <int[]> adjacent = new List <int[]> ();
            //4 possibilities
            //North
            if( (y+1) < NavGraph[0].Length) { //check within graph bounds
                if (NavGraph[x][y+1].canTravelFrom[(int)Move.StoN]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x,y+1});
                }
            }
            //East
            if( (x+1) < NavGraph.Length) { //check within graph bounds
                if (NavGraph[x+1][y].canTravelFrom[(int)Move.WtoE]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x+1,y});
                }
            }
            //South
            if( (y-1) >= 0) { //check within graph bounds
                if (NavGraph[x][y-1].canTravelFrom[(int)Move.NtoS]) { //check if we can reach node going in this direction
                    adjacent.Add(new int[] {x,y-1});
                }
            }
            //West
            if( (x-1) >= 0) { //check within graph bounds
                if (NavGraph[x-1][y].canTravelFrom[(int)Move.EtoW]) { //check if we can reach node going in this direction
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
            for (int x = 0; x < NavGraph.Length; x++) {
                for (int y = 0; y < NavGraph[0].Length; y++) {
                    NavGraph[x][y].costToNode = int.MaxValue;
                    NavGraph[x][y].costToTarget = int.MaxValue;
                    NavGraph[x][y].fromX = -1;
                    NavGraph[x][y].fromY = -1;
                }
            }
            System.Console.WriteLine("done clearing cost and fromX/Y values");

            int startX, startY;
            int endX, endY;
            findLocation(start, out startX, out startY);
            System.Console.WriteLine("startX/Y: " + startX + "," + startY);
            findLocation(end, out endX, out endY);
            System.Console.WriteLine("endX/Y: " + endX + "," + endY);
            System.Console.WriteLine("done finding start/end entries in NavGraph");
            NavGraph[startY][startX].costToNode = 0;
            //heuristic
            //dont use direct distance since we can't currently travel diagonally in graph
            //NavGraph[startY][startX].costToTarget = (int)Math.Ceiling(Math.Sqrt(Math.Pow(startX-endX, 2) + Math.Pow(startY-endY, 2)));
            //use min # of grids needed to traverse to target assuming no obstacles
            NavGraph[startX][startY].costToTarget = Math.Abs(startX-endX) + Math.Abs(startY-endY);
            NavGraph[startX][startY].fromX = startX;
            NavGraph[startX][startY].fromY = startY;
            int currentX = startX;
            int currentY = startY;
            openNodes.addNew(NavGraph[startX][startY]);
            while (openNodes.Count > 0) {
                GraphNode currentNode = openNodes.removeTop();
                System.Console.WriteLine("Finding currentNode");
                findLocation(currentNode.location, out currentX, out currentY);
                if((currentX == endX) && (currentY == endY)) break;
                List <int[]> adjacentList = this.findAdjacent(currentNode);
                System.Console.WriteLine("Current node " + currentX + ", " + currentY);
                System.Console.WriteLine("# valid adjacent: " + adjacentList.Count);
                foreach (int[] adjacent in adjacentList) {
                //Calculate score for adjacent node
                    int costToNode = currentNode.costToNode + 1;
                    //Heuristic is just direct distance as if nothing in the way
                    int adjacentX = adjacent[0];
                    int adjacentY = adjacent[1];
                    int dx = adjacentX - endX;
                    int dy = adjacentY - endY;
                    //int costToTarget = (int)Math.Ceiling(Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)));
                    int costToTarget = Math.Abs(dx) + Math.Abs(dy);
                    int score = costToNode + costToTarget;
                    System.Console.WriteLine("Node " + adjacent[0] + "," + adjacent[1] + " | Calculated Score: " + score + " | Current Score: " + NavGraph[adjacentX][adjacentY].Score);
                    if (score < NavGraph[adjacentX][adjacentY].Score) {
                        System.Console.WriteLine("\tAdding " + adjacentX + "," + adjacentY);
                        NavGraph[adjacentX][adjacentY].costToTarget = costToTarget;
                        NavGraph[adjacentX][adjacentY].costToNode = costToNode;
                        NavGraph[adjacentX][adjacentY].fromX = currentX;
                        NavGraph[adjacentX][adjacentY].fromY = currentY;
                        openNodes.addNew(NavGraph[adjacentX][adjacentY]);
                    }
                }
                this.Print();
                Console.ReadLine();
            }

            //traceback to build path
            System.Console.WriteLine("Building path");
            System.Console.WriteLine("Start x,y: " + startX + "," + startY);
            while (!((currentX == startX) && (currentY == startY))) {
              System.Console.WriteLine("Current x,y: " + currentX + "," + currentY);
              path.Add(NavGraph[currentX][currentY].location);
              System.Console.WriteLine("\t From x,y: " + NavGraph[currentX][currentY].fromX + "," + NavGraph[currentX][currentY].fromY);
              int tmpX = NavGraph[currentX][currentY].fromX;
              int tmpY = NavGraph[currentX][currentY].fromY;
              currentX = tmpX;
              currentY = tmpY;
            }
            path.Reverse();
            return path;
        }

        //print graph contents
        public void Print() {
            System.Console.WriteLine("minY: "+ this.minY + 
                                     " | maxY: " + this.maxY + 
                                     " | minX: " + this.minX + 
                                     " | maxX: " + this.maxX);
            String line0 = "";
            String line1 = "";
            String line2 = "";
            String line3 = "";
            String line4 = "";
            String line5 = "";
            String line6 = "";
            for (int y = this.NavGraph[0].Length-1; y >= 0; y--) {
                for (int x = 0; x < this.NavGraph.Length; x++) {
                    line0 = line0 + "\t";
                    line1 = line1 + "\t";
                    line2 = line2 + "\t";
                    line3 = line3 + "\t";
                    line4 = line4 + "\t";
                    line5 = line5 + "\t";
                    line6 = line6 + "\t";
                    if (!NavGraph[x][y].canTravelFrom[(int)Move.NtoS]) {
                        line0 = line0 + "-------";
                    }
                    if (!NavGraph[x][y].canTravelFrom[(int)Move.WtoE]) {
                        line1 = line1 + "|";
                        line2 = line2 + "|";
                        line3 = line3 + "|";
                        line4 = line4 + "|";
                        line5 = line5 + "|";
                    } else {
                        line1 = line1 + " ";
                        line2 = line2 + " ";
                        line3 = line3 + " ";
                        line4 = line4 + " ";
                        line5 = line5 + " ";
                    }
                    if (!NavGraph[x][y].canTravelFrom[(int)Move.StoN]) {
                        line6 = line6 + "-------";
                    }
                    line1 = line1 + this.NavGraph[x][y].location.x.ToString("n1");
                    line2 = line2 + this.NavGraph[x][y].location.y.ToString("n1");
                    if(this.NavGraph[x][y].costToNode != int.MaxValue)
                        line3 = line3 + this.NavGraph[x][y].costToNode;
                    else
                        line3 = line3 + "Max";
                    if(this.NavGraph[x][y].costToTarget != int.MaxValue)
                        line4 = line4 + this.NavGraph[x][y].costToTarget;
                    else
                        line4 = line4 + "Max";
                    line5 = line5 + this.NavGraph[x][y].fromX+","+this.NavGraph[x][y].fromY;
                    if (!NavGraph[x][y].canTravelFrom[(int)Move.EtoW])
                    {
                        line1 = line1 + "|";
                        line2 = line2 + "|";
                        line3 = line3 + "|";
                        line4 = line4 + "|";
                        line5 = line5 + "|";
                    } else {
                        line1 = line1 + " ";
                        line2 = line2 + " ";
                        line3 = line3 + " ";
                        line4 = line4 + " ";
                        line5 = line5 + " ";
                    }
                }
                System.Console.WriteLine(line0);
                System.Console.WriteLine(line1);
                System.Console.WriteLine(line2);
                System.Console.WriteLine(line3);
                System.Console.WriteLine(line4);
                System.Console.WriteLine(line5);
                System.Console.WriteLine(line6);
                System.Console.WriteLine();
                line0 = "";
                line1 = "";
                line2 = "";
                line3 = "";
                line4 = "";
                line5 = "";
                line6 = "";
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
            List <Location> path = new List <Location> (); 
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
            System.Console.WriteLine("Finding path");
            float startX = (float) 3.999;
            float startY = (float) 3.999;
            int startNodeX, startNodeY;
            float endX = (float) 1;
            float endY = (float) 1;
            int endNodeX, endNodeY;
            Location start = new Location(startX, startY, 0);
            Location end = new Location(endX, endY, 0);
            graph.findLocation(start, out startNodeX, out startNodeY);
            graph.findLocation(end, out endNodeX, out endNodeY);
            Location obstacle = new Location((float)2.5, (float)3.5, 0);
            graph.markObstacle(obstacle, Move.EtoW);
            graph.markObstacle(obstacle, Move.StoN);
            obstacle = new Location ((float)3.5, (float)2.5, 0);
            graph.markObstacle(obstacle, Move.NtoS);
            path = graph.findPath(start,end);
            System.Console.WriteLine("path find done");
            graph.Print();
            System.Console.WriteLine("Start: " + start.ToString() + " | Node: " + startNodeX + "," + startNodeY);
            System.Console.WriteLine("End  : " + end.ToString() + " | Node: " + endNodeX + "," + endNodeY);
            foreach (Location waypoint in path) {
                int wpX, wpY;
                graph.findLocation(waypoint, out wpX, out wpY);
                System.Console.WriteLine(waypoint.ToString() + " | Node: " + wpX + "," + wpY);;
            }
            Console.ReadLine();
        }
    }
}