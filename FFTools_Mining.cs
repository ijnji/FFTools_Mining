using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Mining {
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
            List<Location> gnlocl = new List<Location>();
            gnlocl.Add(thePlayer.location);
            foreach (GatheringNode gn in theGathNodeList) gnlocl.Add(gn.location);
            theNavigatorGraph.addLocations(gnlocl);    
            // Start the UI thread.
            MapForm theMapForm = new MapForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theMapForm);
            theMapForm.setViewPlayer(thePlayer);
            theMapForm.setViewGathNodeList(theGathNodeList);

            System.Console.WriteLine("MAIN: Player is at " + thePlayer.location);
            GatheringNode nearestGathNode = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
            System.Console.WriteLine("MAIN: Nearest gathering node is at " + nearestGathNode.location);
            List<Location> path = theNavigatorGraph.findPath(thePlayer.location, nearestGathNode.location);
            System.Console.WriteLine("MAIN: The path to this node is: ");
            foreach (Location l in path) {
                System.Console.WriteLine(l);
            }

            // Begin main loop.
            theNavigator.moveThrough(path);
            while (true) {
                // Read one copy of each data from memory only.
                thePlayer = theMemory.readPlayer();
                theGathNodeList = theMemory.readGatheringNodeList(gathNodeAddrList);
                // Update with new data.
                theMapForm.setViewPlayer(thePlayer);
                theMapForm.setViewGathNodeList(theGathNodeList);
                theNavigator.update(thePlayer);

                // Update each 50ms.
                Thread.Sleep(50);
            }

            //GatheringNode gn = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
            //Queue<GatheringNode> gnHistory = new Queue<GatheringNode>();
            //while (true) {
            //    thePlayer = theMemory.readPlayer();
            //    GathTypeAddresses = theMemory.findAddressesOfBytes(GathTypeByteArray);
            //    theGathNodeList = theMemory.readGatheringNodeList(GathTypeAddresses);
            //    System.Console.WriteLine("-------");
            //    System.Console.WriteLine("Nearest mineral deposit is at...");
            //    gn = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
            //    System.Console.WriteLine(gn);
            //    if( Location.findDistanceBetween(thePlayer.location, gn.location) < 250) { //TODO: fix 250 hack
            //        System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, gn.location));
            //        System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(gn.location));
            //        System.Console.WriteLine("Traveling to the node...");
            //        travelTo(theMemory, gn.location);
            //        gatherFrom(theMemory);
            //        System.Console.WriteLine("Done with this node!");
            //        if (gnHistory.Count > 10) gnHistory.Dequeue();
            //        gnHistory.Enqueue(gn);
            //    }
            //    else {
            //        System.Console.WriteLine("No nearby node, moving to previous good node and searching again");
            //        gn = gnHistory.Dequeue();
            //        System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, gn.location));
            //        System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(gn.location));
            //        System.Console.WriteLine("Traveling to the node...");
            //        travelTo(theMemory, gn.location);
            //    }
            //}
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