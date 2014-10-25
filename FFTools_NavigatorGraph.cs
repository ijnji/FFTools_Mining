using System;
using System.Collections.Generic;
namespace FFTools {
    public class NavigatorGraph {
        public enum Move {NtoS, StoN, EtoW, WtoE, NWtoSE, NEtoSW, SEtoNW, SWtoNE};
        private const float DIST_PER_GRID = 3f;
        // BUFFER_MULTIPLIER * DIST_PER_GRID is buffer space on edges of graph.
        private const int BUFFER_MULTIPLIER = 0;
        // Apparently jagged arrays [][] are faster than multidimensional [,]?
        public GraphNode [][] NavGraph = new GraphNode[1][];
        /*
        NavGraph imagined like this, ex 3x3:
        2  MinX, MaxY    |              |   MaxX, MaxY
        --------------------------------------------------
        1                |              |
        --------------------------------------------------
        0  MinX, MinY    |              |   MaxX, MinY
        --------------------------------------------------
               0         |      1       |        2
        */
        // Min/Max "in game" coordinates represented -- rounded to nearest multiple of DIST_PER_GRID.
        // Inclusive of Min, not inclusive of Max [min, max).
        public float MinX = float.PositiveInfinity;
        public float MinY = float.PositiveInfinity;
        public float MaxX = float.NegativeInfinity;
        public float MaxY = float.NegativeInfinity;
        
        public struct GraphNode : IComparable {
            public Location location;
            // Index into array using Move enumeration.
            public bool[] canTravelFrom;
            // Accumulated cost to reach current node in "in-game" units
            public float costToNode;
            // Heuristic assumes direct travel (no obstacles) and is the "in-game" distance
            // from middle of current GraphNode to middle of ending GraphNode.
            public float costToTarget;
            // Index into NavGraph array indicating from which GraphNode we traveled from to get to current GraphNode.
            public int fromX;
            public int fromY;

            public float Score {
                get {
                    // Score calc + overflow check.
                    return (costToNode == float.MaxValue) ||
                           (costToTarget == float.MaxValue) ? float.MaxValue :  costToNode + costToTarget;
                }
            }

            public GraphNode(Location location, bool ns, bool sn, bool ew, bool we, bool nwse, bool nesw, bool senw, bool swne) {
                this.location = location;
                canTravelFrom = new bool[] {ns, sn, ew, we, nwse, nesw, senw, swne};
                costToNode = float.MaxValue;
                costToTarget = float.MaxValue;
                fromX = -1;
                fromY = -1;
            }

            public GraphNode(Location location) {
                this.location = location;
                canTravelFrom = new bool[] {true, true, true, true, true, true, true, true};
                costToNode = float.MaxValue;
                costToTarget = float.MaxValue;
                fromX = -1;
                fromY = -1;
            }

            // GraphNode implements IComparable.
            public int CompareTo(Object obj) { //-1 inst precedes obj; 0 same; +1 inst follows obj
                if (obj == null) return -1;
                
                GraphNode b = (GraphNode) obj;
                float instScore = ( (costToNode == float.MaxValue) ||
                                    (costToTarget == float.MaxValue) ) ? float.MaxValue : this.costToNode + this.costToTarget;
                float objScore = ( (b.costToNode == float.MaxValue) ||
                                   (b.costToTarget == float.MaxValue) ) ? float.MaxValue : b.costToNode + b.costToTarget;
                
                if (instScore == objScore) return 0;
                else if (instScore > objScore) return 1;
                else return -1;
            }
        }

        public NavigatorGraph() {}

        public int Size {
            get {
                if(this.NavGraph == null)
                    return 0;
                if(this.NavGraph[0] == null)
                    return 0;
                return this.NavGraph.Length * this.NavGraph[0].Length;
            }
        }

        public void addLocations(List<Location> newLocations) {
            List <Location> locations = new List <Location> ();
            foreach (Location location in newLocations) {
                if (!this.findLocation(location))
                    locations.Add(location);
            }

            // All newLocations are already covered.
            if(locations.Count == 0) return;

            // Only need 4 corners of old graph to build encompassing new graph.
            if(this.Size != 0) {
                locations.Add(this.NavGraph[0][0].location);
                locations.Add(this.NavGraph[0][NavGraph[0].Length-1].location);
                locations.Add(this.NavGraph[NavGraph.Length-1][0].location);
                locations.Add(this.NavGraph[NavGraph.Length-1][NavGraph[0].Length-1].location);
            }

            // Find dimensions in "in-game" units to hold all locations.
            float totalX = 0, totalY = 0;
            float tmp_MinX = float.PositiveInfinity;
            float tmp_MinY = float.PositiveInfinity;
            float tmp_MaxX = float.NegativeInfinity;
            float tmp_MaxY = float.NegativeInfinity;

            foreach (Location location in locations) {
                if (location.x < tmp_MinX) tmp_MinX = location.x;
                if (location.y < tmp_MinY) tmp_MinY = location.y;
                if (location.x > tmp_MaxX) tmp_MaxX = location.x;
                if (location.y > tmp_MaxY) tmp_MaxY = location.y;
            }

            // Round "in-game" units to nearest DIST_PER_GRID multiple.
            if(tmp_MinY % DIST_PER_GRID != 0) {
                if(tmp_MinY > 0) tmp_MinY = tmp_MinY - (tmp_MinY % DIST_PER_GRID);
                else tmp_MinY = tmp_MinY - (DIST_PER_GRID - Math.Abs(tmp_MinY % DIST_PER_GRID));
            }

            if(tmp_MaxY % DIST_PER_GRID != 0) {
                if(tmp_MaxY > 0) tmp_MaxY = tmp_MaxY + (DIST_PER_GRID - (tmp_MaxY % DIST_PER_GRID));
                else tmp_MaxY = tmp_MaxY + Math.Abs(tmp_MaxY % DIST_PER_GRID);
            }

            if(tmp_MinX % DIST_PER_GRID != 0) {
                if(tmp_MinX > 0) tmp_MinX = tmp_MinX - (tmp_MinX % DIST_PER_GRID);
                else tmp_MinX = tmp_MinX - (DIST_PER_GRID - Math.Abs(tmp_MinX % DIST_PER_GRID));
            }

            if(tmp_MaxX % DIST_PER_GRID != 0) {
                if(tmp_MaxX > 0) tmp_MaxX = tmp_MaxX + (DIST_PER_GRID - (tmp_MaxX % DIST_PER_GRID));
                else tmp_MaxX = tmp_MaxX + Math.Abs(tmp_MaxX % DIST_PER_GRID);
            }

            // Calculate dimensions in "in-game" units needed and add buffer on all sides.
            totalX = tmp_MaxX - tmp_MinX + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;
            totalY = tmp_MaxY - tmp_MinY + 2*BUFFER_MULTIPLIER*DIST_PER_GRID;

            // Number of GraphNodes in each dimension.
            int graphWidth = (int)Math.Ceiling(totalX/DIST_PER_GRID);
            int graphHeight = (int)Math.Ceiling(totalY/DIST_PER_GRID);

            // Create new graph.
            GraphNode[][] newGraph = new GraphNode[graphWidth][];
            for (int x = 0; x < graphWidth; x++) {
                newGraph[x] = new GraphNode[graphHeight];
            }

            // Populate new graph.
            // Initial "in-game" coordinates stored in [0][0] = most negative X, most negative Y + buffers applied.
            float graphX = tmp_MinX + DIST_PER_GRID/2;
            float graphY = tmp_MinY + DIST_PER_GRID/2;
            float tmp_graphX = graphX;

            // Fill in all GraphNodes.
            for (int x = 0; x < graphWidth; x++) {
                float tmp_graphY = graphY;
                for (int y = 0; y < graphHeight; y++) {
                    int oldX, oldY;
                    //NavGraph Z coordinates aren't used
                    Location new_location = new Location(tmp_graphX, tmp_graphY, 0);
                    if(this.findLocation(new_location, out oldX, out oldY)) {
                        newGraph[x][y] = this.NavGraph[oldX][oldY];
                    }
                    else {
                        newGraph[x][y] = new GraphNode(new_location);
                    }
                    tmp_graphY = tmp_graphY + DIST_PER_GRID;
                }
                tmp_graphX = tmp_graphX + DIST_PER_GRID;
            }
            this.NavGraph = newGraph;
            this.MinX = tmp_MinX;
            this.MinY = tmp_MinY;
            this.MaxX = tmp_MaxX;
            this.MaxY = tmp_MaxY;
            System.Console.WriteLine("MinY: "+ this.MinY +
                                     " | MaxY: " + this.MaxY +
                                     " | MinX: " + this.MinX +
                                     " | MaxX: " + this.MaxX);
        }

        // Returns true/false if location exists.
        public bool findLocation(Location location) {
            if (this.Size == 0) return false;
            if ((location.x >= this.MinX) && (location.x <= this.MaxX) &&
                (location.y >= this.MinY) && (location.y <= this.MaxY) ) {
                return true;
            }
            return false;
        }

        // Oerloaded findLocation to also write out X/Y indices into NavGraph array if found.
        public bool findLocation(Location location, out int outX, out int outY) {
            if (this.Size == 0) {
                outX = -1;
                outY = -1;
                return false;
            }

            if ( (location.x >= this.MinX) && (location.x <= this.MaxX) &&
                 (location.y >= this.MinY) && (location.y <= this.MaxY) ) {
                //System.Console.WriteLine("x: " + location.x +
                //                      " y: " + location.y +
                //                      " MinX: " + MinX +
                //                      " MaxY: " + MaxY);
                //min X,Y is at [NavGraph.Length][0]
                //max X,Y is at [0][NavGraph[0].Length]
                float tmp = location.x - this.MinX;
                outX = (int) Math.Floor((tmp/DIST_PER_GRID));
                tmp = location.y - this.MinY;  //-1 because max is not inclusive
                outY = (int) Math.Floor((tmp/DIST_PER_GRID));
                return true;
            }

            outX = -1;
            outY = -1;
            return false;
        }

        // Returns list of locations in in-game coordinates that're marked as obstacles from all directions.
        public List<Location> getObstacles() {
            List<Location> obstacles = new List<Location>();
            for (int x = 0; x < this.NavGraph.Length; x++) {
                for (int y = 0; y < this.NavGraph[x].Length; y++) {
                    bool blockedFromAllDirections = true;
                    for (int i = 0; i < this.NavGraph[x][y].canTravelFrom.Length; i++) {
                        if (this.NavGraph[x][y].canTravelFrom[i] == true) {
                            blockedFromAllDirections = false;
                        }
                    }
                    if (blockedFromAllDirections == true) {
                        obstacles.Add(this.NavGraph[x][y].location);
                    }
                }
            }
            return obstacles;
        }

        // Marks obstacle given location and all direction.
        public void markObstacle(Location location) {
            int x, y;
            if ( this.findLocation(location, out x, out y) ) {
                for (int i = 0; i < this.NavGraph[x][y].canTravelFrom.Length; i++) {
                    this.NavGraph[x][y].canTravelFrom[i] = false;
                }
            } else {
                System.Console.WriteLine("NAVGRAPH: Obstacle unmarkable, does not exist in graph: " + location.ToString());
            }
        }

        // Marks obstacle given location and direction of travel.
        public void markObstacle(Location location, Move direction) {
            int x, y;
            if ( this.findLocation(location, out x, out y) ) {
                this.NavGraph[x][y].canTravelFrom[(int) direction] = false;
            } else {
                System.Console.WriteLine("NAVGRAPH: Obstacle unmarkable, does not exist in graph: " + location.ToString());
            }
        }

        // Marks all nodes along edges of the given polygon as an obstacle for all directions.
        // The polygon specified must have at least 4 vertices.
        // The last vertice should match first vertice to complete the polygon perimeter.
        public void markObstaclePolygon(List<Location> vertices) {
            if (vertices.Count < 4) {
                System.Console.WriteLine("NAVGRAPH: Must give at least four vertices to mark a polygon obstacle.");
                return;
            }
            for (int i = 0; i < vertices.Count - 1; i++) {
                Location lb = vertices[i + 1];
                Location la = vertices[i];
                if (lb.x != la.x) {
                    float slopeyx = (lb.y - la.y) / (lb.x - la.x);
                    if (lb.x > la.x) {
                        for (float x = la.x; x <= lb.x; x += DIST_PER_GRID) {
                            Location obs = new Location(x, (x - la.x) * slopeyx + la.y, (float)0);
                            markObstacle(obs);
                        }
                    } else {
                        for (float x = la.x; x >= lb.x; x -= DIST_PER_GRID) {
                            Location obs = new Location(x, (x - la.x) * slopeyx + la.y, (float)0);
                            markObstacle(obs);
                        }
                    }
                }
                if (lb.y != la.y) {
                    float slopexy = (lb.x - la.x) / (lb.y - la.y);
                    if (lb.y > la.y) {
                        for (float y = la.y; y <= lb.y; y += DIST_PER_GRID) {
                            Location obs = new Location((y - la.y) * slopexy + la.x, y, (float)0);
                            markObstacle(obs);
                        }
                    } else {
                        for (float y = la.y; y >= lb.y; y -= DIST_PER_GRID) {
                            Location obs = new Location((y - la.y) * slopexy + la.x, y, (float)0);
                            markObstacle(obs);
                        }
                    }
                }
            }
        }

        // Returns list of adjacent GraphNodes that can be reached from GraphNode at [x][y].
        public List <int[]> findAdjacent(int x, int y) {
            List <int[]> adjacent = new List <int[]> ();
            // North.
            if ( (y + 1) < NavGraph[0].Length ) { // Check within graph bounds.
                if (NavGraph[x][y + 1].canTravelFrom[(int)Move.StoN]) adjacent.Add(new int[] {x, y + 1});
            }
            // North West.
            if ( ((x - 1) >= 0) && ((y + 1) < NavGraph[0].Length) ) { // Check within graph bounds.
                if (NavGraph[x - 1][y].canTravelFrom[(int)Move.SEtoNW]) adjacent.Add(new int[] {x - 1, y + 1});
            }
            // North East.
            if ( ((x + 1) < NavGraph.Length) && ((y + 1) < NavGraph[0].Length) ) { // Check within graph bounds.
                if (NavGraph[x - 1][y].canTravelFrom[(int)Move.SWtoNE]) adjacent.Add(new int[] {x + 1, y + 1});
            }
            // East.
            if ( (x + 1) < NavGraph.Length) { // Check within graph bounds.
                if (NavGraph[x + 1][y].canTravelFrom[(int)Move.WtoE]) adjacent.Add(new int[] {x + 1, y});
            }
            // South.
            if ( (y - 1) >= 0) { // Check within graph bounds.
                if (NavGraph[x][y - 1].canTravelFrom[(int)Move.NtoS]) adjacent.Add(new int[] {x, y - 1});
            }
            // South West.
            if ( ((x - 1) >= 0) && ((y - 1) >= 0) ) { // Check within graph bounds.
                if (NavGraph[x - 1][y].canTravelFrom[(int)Move.NEtoSW]) adjacent.Add(new int[] {x - 1, y - 1});
            }
            // South East.
            if ( ((x + 1) < NavGraph.Length) && ((y - 1) >= 0) ) { // Check within graph bounds.
                if (NavGraph[x - 1][y].canTravelFrom[(int)Move.NWtoSE]) adjacent.Add(new int[] {x + 1, y - 1});
            }
            // West.
            if ( (x - 1) >= 0 ) { // Check within graph bounds.
                if (NavGraph[x - 1][y].canTravelFrom[(int)Move.EtoW]) adjacent.Add(new int[] {x - 1,y});
            }
            return adjacent;
        }

        // Overloaded findAdjacent.
        public List <int[]> findAdjacent(GraphNode reference) {
            int x, y;
            findLocation(reference.location, out x, out y);
            return findAdjacent(x, y);
        }

        // A* do it.
        public List <Location> findPath(Location start, Location end) {
            List <Location> path = new List <Location> ();
            PriorityQueue <GraphNode> openNodes = new PriorityQueue <GraphNode> ();
            // Reset all scores to MaxValue, fromX/fromY to -1
            for (int x = 0; x < NavGraph.Length; x++) {
                for (int y = 0; y < NavGraph[0].Length; y++) {
                    NavGraph[x][y].costToNode = float.MaxValue;
                    NavGraph[x][y].costToTarget = float.MaxValue;
                    NavGraph[x][y].fromX = -1;
                    NavGraph[x][y].fromY = -1;
                }
            }
            //System.Console.WriteLine("done clearing cost and fromX/Y values");
            int startX, startY;
            int endX, endY;
            findLocation(start, out startX, out startY);
            //System.Console.WriteLine("startX/Y: " + startX + "," + startY);
            findLocation(end, out endX, out endY);
            //System.Console.WriteLine("endX/Y: " + endX + "," + endY);
            //System.Console.WriteLine("done finding start/end entries in NavGraph");
            NavGraph[startX][startY].costToNode = 0;
            //heuristic assumes direct travel (no obstacles) and is the "in-game" distance from middle of current GraphNode to middle of ending GraphNode
            NavGraph[startX][startY].costToTarget = Location.findDistanceBetween(NavGraph[startX][startY].location, NavGraph[endX][endY].location);
            //heuristic if diagonals not allowed
            //NavGraph[startX][startY].costToTarget = dx + dy;
            NavGraph[startX][startY].fromX = startX;
            NavGraph[startX][startY].fromY = startY;
            int currentX = startX;
            int currentY = startY;
            openNodes.addNew(NavGraph[startX][startY]);
            while (openNodes.Count > 0) {
                GraphNode currentNode = openNodes.removeTop();
                //System.Console.WriteLine("Finding currentNode");
                findLocation(currentNode.location, out currentX, out currentY);
                if ( (currentX == endX) && (currentY == endY) ) break;
                List <int[]> adjacentList = this.findAdjacent(currentNode);
                //System.Console.WriteLine("Current node " + currentX + ", " + currentY);
                //System.Console.WriteLine("# valid adjacent: " + adjacentList.Count);
                foreach (int[] adjacent in adjacentList) {
                    int adjacentX = adjacent[0];
                    int adjacentY = adjacent[1];
                    //Calculate score for adjacent node.
                    float costToNode = currentNode.costToNode + Location.findDistanceBetween(currentNode.location, NavGraph[adjacentX][adjacentY].location);
                    float costToTarget = Location.findDistanceBetween(NavGraph[adjacentX][adjacentY].location, NavGraph[endX][endY].location);
                    // Heuristic if diagonals not allowed.
                    //int costToTarget = dx + dy;
                    float score = costToNode + costToTarget;
                    //System.Console.WriteLine("Node " + adjacent[0] + "," + adjacent[1] + " | Calculated Score: " + score + " | Current Score: " + NavGraph[adjacentX][adjacentY].Score);
                    if (score < NavGraph[adjacentX][adjacentY].Score) {
                        //System.Console.WriteLine("\tAdding " + adjacentX + "," + adjacentY);
                        NavGraph[adjacentX][adjacentY].costToTarget = costToTarget;
                        NavGraph[adjacentX][adjacentY].costToNode = costToNode;
                        NavGraph[adjacentX][adjacentY].fromX = currentX;
                        NavGraph[adjacentX][adjacentY].fromY = currentY;
                        openNodes.addNew(NavGraph[adjacentX][adjacentY]);
                    }
                }
                //this.Print();
                //Console.ReadLine();
            }
            // Traceback to build path.
            //System.Console.WriteLine("Traceback to build path");
            while ( !((currentX == startX) && (currentY == startY)) ) {
              path.Add(NavGraph[currentX][currentY].location);
              int tmpX = NavGraph[currentX][currentY].fromX;
              int tmpY = NavGraph[currentX][currentY].fromY;
              currentX = tmpX;
              currentY = tmpY;
            }
            path.Reverse();
            return path;
        }

        public void Print() {
            System.Console.WriteLine("MinY: "+ this.MinY +
                                     " | MaxY: " + this.MaxY +
                                     " | MinX: " + this.MinX +
                                     " | MaxX: " + this.MaxX);
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
                    if(this.NavGraph[x][y].costToNode != float.MaxValue)
                        line3 = line3 + this.NavGraph[x][y].costToNode.ToString("n1");
                    else
                        line3 = line3 + "Max";
                    if(this.NavGraph[x][y].costToTarget != float.MaxValue)
                        line4 = line4 + this.NavGraph[x][y].costToTarget.ToString("n1");
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
        
        /*
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
            float endX = (float) -1;
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
        */
    }
}