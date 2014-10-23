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
            MemoryManager theMemory = new MemoryManager();
            if (theMemory.initialize() > 0) Environment.Exit(1);
            // Ready singleton Navigator.
            Navigator theNavigator = new Navigator(theMemory);
            NavigatorGraph theNavigatorGraph = new NavigatorGraph();

            // Get a first read of all data.
            Player thePlayer = theMemory.readPlayer();
            //List<string> theGenDiagList = theMemory.readGeneralDialogueList();
            List<IntPtr> gathNodeAddrList = theMemory.findAddressesOfBytes(gathTypeByteArray);
            List<GatheringNode> theGathNodeList = theMemory.readGatheringNodeList(gathNodeAddrList);
 

            // Start the UI thread.
            MapForm theMapForm = new MapForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theMapForm);
            theMapForm.setViewPlayer(thePlayer);
            theMapForm.setViewGathNodeList(theGathNodeList);

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
                        // Find path and begin navigation.
                        TargetGathNode = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
                        System.Console.WriteLine("MAIN: Nearest gathering node at " + TargetGathNode.location);
                        List<Location> navPath = theNavigatorGraph.findPath(thePlayer.location, TargetGathNode.location);
                        System.Console.WriteLine("MAIN: Beginning navigation");
                        theNavigator.ctrlMoveThrough(navPath);
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
        }

        private static void formStart(Object theMapForm) {
            Application.Run((MapForm)theMapForm);
        }

        private static void gatherFrom(MemoryManager theMemory) {
            theMemory.sendKeyPressMsg(Keys.End, 100);
            Thread.Sleep(2500);
            theMemory.sendKeyPressMsg(Keys.Enter, 100);
            Thread.Sleep(2500);
            theMemory.sendKeyPressMsg(Keys.Enter, 100);
            Thread.Sleep(2500);
            for (int i = 0; i < 4; i++) {
                theMemory.sendKeyPressMsg(Keys.Enter, 100);
                Thread.Sleep(5000);
            }
            Thread.Sleep(3000);
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
    }
}