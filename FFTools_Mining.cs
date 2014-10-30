using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Mining {
        private enum States {IDLE, MOVING, MINING};
        private static States CurrentState = States.IDLE;
        private static GatheringNode TargetGathNode = null;

        public static void Main() {
            String gathType= "Mineral Deposit"; //set to desired farming type ex: Mineral Deposit, Mature Tree
            byte[] gathTypeByteArray = Encoding.ASCII.GetBytes(gathType);

            // Ready singleton MemoryManager.
            //UNCOMMENT MemoryManager theMemory = new MemoryManager();
            //UNCOMMENT if (theMemory.initialize() > 0) Environment.Exit(1);
            // Ready singleton Navigator.
            //UNCOMMENT Navigator theNavigator = new Navigator(theMemory);
            NavigatorGraph theNavigatorGraph = new NavigatorGraph();

            // Get a first read of all data.
            //UNCOMMENT Player thePlayer = theMemory.readPlayer();
            Player thePlayer = new Player(-10f, 0f, 130f, 0);//DELETEME
            //List<string> theGenDiagList = theMemory.readGeneralDialogueList();
            //UNCOMMENT List<IntPtr> gathNodeAddrList = theMemory.findAddressesOfBytes(gathTypeByteArray);
            //UNCOMMENT List<GatheringNode> theGathNodeList = theMemory.readGatheringNodeList(gathNodeAddrList);
            List<GatheringNode> theGathNodeList = new List<GatheringNode>();//DELETEME
            theGathNodeList.Add(new GatheringNode(false, -60f, 0f, 80f));//DELETEME
            theGathNodeList.Add(new GatheringNode(false, 40f, 0f, 80f));//DELETEME
            theGathNodeList.Add(new GatheringNode(false, 40f, 0f, 180f));//DELETEME
            theGathNodeList.Add(new GatheringNode(false, -60f, 0f, 180f));//DELETEME
            List<Location> gnlocl = new List<Location>();
            gnlocl.Add(thePlayer.location);
            foreach (GatheringNode gn in theGathNodeList) gnlocl.Add(gn.location);
            theNavigatorGraph.addLocations(gnlocl);

            theNavigatorGraph.markObstacle(new Location(-19f, 150f));
            theNavigatorGraph.markObstacle(new Location(-16f, 150f));
            theNavigatorGraph.markObstacle(new Location(-13f, 150f));
            theNavigatorGraph.markObstacle(new Location(-10f, 150f));
            theNavigatorGraph.markObstacle(new Location(-7f, 150f));
            theNavigatorGraph.markObstacle(new Location(-4f, 150f));
            theNavigatorGraph.markObstacle(new Location(-1f, 150f));
            

            // Start the UI thread.
            MapForm theMapForm = new MapForm(theNavigatorGraph);
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theMapForm);
            theMapForm.setViewPlayer(thePlayer);
            theMapForm.setViewGathNodeList(theGathNodeList);

            while (true) {
                List<Location> obstacles = theNavigatorGraph.getObstacles();
                theMapForm.setViewGraphObstacles(obstacles);
                Thread.Sleep(59);
            }

            /*
            // Begin main loop.
            while (true) {
                // Read one copy of each data from memory only.
                thePlayer = theMemory.readPlayer();
                theGathNodeList = theMemory.readGatheringNodeList(gathNodeAddrList);
                
                // Update every loop.
                theMapForm.setViewPlayer(thePlayer);
                theMapForm.setViewGathNodeList(theGathNodeList);
                
                switch (CurrentState) {
                    case (States.IDLE) :
                        // Update the navgraph graph with current location in case we're at a completely new location.
                        // NavGraph will ignore duplicate locations.
                        List<Location> gnlocl = new List<Location>();
                        gnlocl.Add(thePlayer.location);
                        foreach (GatheringNode gn in theGathNodeList) gnlocl.Add(gn.location);
                        theNavigatorGraph.addLocations(gnlocl);

                        // Mark obstacles manually for now.
                        manualObstacleMark(theNavigatorGraph);
                        List<Location> obstacles = theNavigatorGraph.getObstacles();
                        theMapForm.setViewGraphObstacles(obstacles);

                        // Find path and begin navigation.
                        TargetGathNode = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
                        List<Location> path = theNavigatorGraph.findPath(thePlayer.location, TargetGathNode.location);
                        // Remove last elements in path to make navigation slightly cleaner.
                        path.RemoveAt(path.Count - 1);
                        theMapForm.setViewPath(path);
                        theNavigator.ctrlMoveThrough(path);

                        System.Console.WriteLine("MAIN: Nearest gathering node at " + TargetGathNode.location);
                        System.Console.WriteLine("MAIN: Beginning navigation");
                        CurrentState = States.MOVING;
                        break;
                    case (States.MOVING) :
                        if (theNavigator.sensArrivedAtTarget()) {
                            theNavigator.ctrlGatherFrom(TargetGathNode);
                            CurrentState = States.MINING;
                        }
                        break;
                    case (States.MINING) :
                        if (theNavigator.sensMinedFromTarget()) {
                            CurrentState = States.IDLE;
                        }
                        break;
                }

                // Navigator's update must be called consistently.
                theNavigator.update(thePlayer, 50);

                // Update each 50ms.
                Thread.Sleep(50);
            }
            */
        }

        private static void formStart(Object theMapForm) {
            Application.Run((MapForm)theMapForm);
        }

        private static GatheringNode nearestVisibleGatheringNode(Player thePlayer, List<GatheringNode> theGathNodeList) {
            List<GatheringNode> visibleList = new List<GatheringNode>();
            foreach (GatheringNode gn in theGathNodeList) {
                if (gn.vis) visibleList.Add(gn);
            }
            GatheringNode nearestGatheringNode = null;
            float nearestGatheringNodeDistance = Single.MaxValue;
            foreach (GatheringNode gn in visibleList) {
                float distance = Location.findDistanceBetween(thePlayer.location, gn.location);
                if (distance < nearestGatheringNodeDistance) {
                    nearestGatheringNodeDistance = distance;
                    nearestGatheringNode = gn;
                }
            }
            return nearestGatheringNode;
        }

        private static void manualObstacleMark(NavigatorGraph theNavigatorGraph) {
            // Lv10 Mineral Deposit Central Thanalan.
            //Location obs = new Location(-79.5f, 13.5f);
            //theNavigatorGraph.markObstacle(obs);
        }
    }
}