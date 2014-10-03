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
            String GathType= "Mature Tree"; //set to desired farming type ex: Mineral Deposit, Mature Tree
            byte[] GathTypeByteArray = Encoding.ASCII.GetBytes(GathType);


            //NavigatorGraph test creation
            Location one = new Location(1, 2, 3);
            Location two = new Location(10, 15, 0);
            Location[] array = {one, two};
            NavigatorGraph graph = new NavigatorGraph(array);
            graph.Print();
            Console.ReadLine();

            // Ready singleton MemoryManager.
            MemoryManager theMemory = new MemoryManager();
            if (theMemory.initialize() > 0) Environment.Exit(1);
            // Get a first read of Player.
            Player thePlayer = theMemory.readPlayer();
            System.Console.WriteLine(thePlayer);
            // Ready singleton Navigator.
            Navigator theNavigator = new Navigator(theMemory);
            // Get a first read of Gen Diag.
            List<string> theGenDiagList = theMemory.readGeneralDialogueList();
            // Get a first read of Gather Nodes.
            List<IntPtr> GathTypeAddresses = theMemory.findAddressesOfBytes(GathTypeByteArray);
            List<GatheringNode> theGathNodeList = theMemory.readGatheringNodeList(GathTypeAddresses);
                // --- Test case Mineral Deposits ---
                //List<GatheringNode> theGathNodeList = new List<GatheringNode>();
                //theGathNodeList.Add(new GatheringNode(false, (float)205.1436, 0, (float)-83.35779));
                //theGathNodeList.Add(new GatheringNode(false, (float)216.1513, 0, (float)-87.30682));
                //theGathNodeList.Add(new GatheringNode(false, (float)221.7113, 0, (float)-95.18837));
                //theGathNodeList.Add(new GatheringNode(false, (float)225.8484, 0, (float)-106.7841));
                //theGathNodeList.Add(new GatheringNode(false, (float)256.3488, 0, (float)-215.9667));
                //theGathNodeList.Add(new GatheringNode(false, (float)262.8185, 0, (float)-170.5062));
                //theGathNodeList.Add(new GatheringNode(false, (float)274.6811, 0, (float)-247.5));
                //theGathNodeList.Add(new GatheringNode(false, (float)286.9102, 0, (float)-252.5938));
                //theGathNodeList.Add(new GatheringNode(false, (float)317.0013, 0, (float)-178.881));
                //theGathNodeList.Add(new GatheringNode(false, (float)323.3648, 0, (float)-182.2007));
                //theGathNodeList.Add(new GatheringNode(false, (float)325.8448, 0, (float)-265.9896));
                //theGathNodeList.Add(new GatheringNode(false, (float)333.5263, 0, (float)-214.3547));
                //theGathNodeList.Add(new GatheringNode(false, (float)334.8407, 0, (float)-242.5161));
                //theGathNodeList.Add(new GatheringNode(true , (float)332.7316, 0, (float)-256.8401));
                //theGathNodeList.Add(new GatheringNode(true, (float)261.0139, 0, (float)-202.0589));
                //theGathNodeList.Add(new GatheringNode(true, (float)291.211, 0, (float)-255.4915));
                // --- Test case Mineral Deposits ---
            // Start the UI thread.
            MapForm theMapForm = new MapForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theMapForm);
            // Show working set of Gathering Nodes on MapForm.
            theMapForm.setViewGathNodeList(theGathNodeList);

            GatheringNode gn = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
            Queue<GatheringNode> gnHistory = new Queue<GatheringNode>();
            
            while (true) {
                thePlayer = theMemory.readPlayer();
                GathTypeAddresses = theMemory.findAddressesOfBytes(GathTypeByteArray);
                theGathNodeList = theMemory.readGatheringNodeList(GathTypeAddresses);
                System.Console.WriteLine("-------");
                System.Console.WriteLine("Nearest mineral deposit is at...");
                gn = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
                System.Console.WriteLine(gn);
                if( Location.findDistanceBetween(thePlayer.location, gn.location) < 250) { //TODO: fix 250 hack
                    System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, gn.location));
                    System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(gn.location));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, gn.location);
                    gatherFrom(theMemory);
                    System.Console.WriteLine("Done with this node!");
                    if (gnHistory.Count > 10) gnHistory.Dequeue();
                    gnHistory.Enqueue(gn);
                }
                else {
                    System.Console.WriteLine("No nearby node, moving to previous good node and searching again");
                    gn = gnHistory.Dequeue();
                    System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, gn.location));
                    System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(gn.location));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, gn.location);
                }
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

        private static void travelTo(MemoryManager theMemory, Location tLocation) {
            Player thePlayer = theMemory.readPlayer();
            
            if ( thePlayer.findAngleBetween(tLocation) > 0.1 ) {
                theMemory.sendKeyDownMsg(Keys.A);
                while ( thePlayer.findAngleBetween(tLocation) > 0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.A);
            }
            if ( thePlayer.findAngleBetween(tLocation) < -0.1 ) {
                theMemory.sendKeyDownMsg(Keys.D);
                while ( thePlayer.findAngleBetween(tLocation) < -0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.D);
            }

            bool rotating_left = false;
            bool rotating_right = false;
            thePlayer = theMemory.readPlayer();
            while ( Location.findDistanceBetween(thePlayer.location, tLocation) > 2.0 ) {
                theMemory.sendKeyDownMsg(Keys.W);
                while ( Location.findDistanceBetween(thePlayer.location, tLocation) > 2.0 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                    if ( thePlayer.findAngleBetween(tLocation) > 0.1 ) {
                        theMemory.sendKeyDownMsg(Keys.A);
                        rotating_left = true;
                    } else if ( thePlayer.findAngleBetween(tLocation) < -0.1 ) {
                        theMemory.sendKeyDownMsg(Keys.D);
                        rotating_right = true;
                    } else if ( rotating_left ) {
                        theMemory.sendKeyUpMsg(Keys.A);
                        rotating_left = false;
                    } else if ( rotating_right ) {
                        theMemory.sendKeyUpMsg(Keys.D);
                        rotating_right = false;
                    }
                }
                theMemory.sendKeyUpMsg(Keys.W);
            }

            thePlayer = theMemory.readPlayer();
            if ( thePlayer.findAngleBetween(tLocation) > 0.1 ) {
                theMemory.sendKeyDownMsg(Keys.A);
                while ( thePlayer.findAngleBetween(tLocation) > 0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.A);
            }
            if ( thePlayer.findAngleBetween(tLocation) < -0.1 ) {
                theMemory.sendKeyDownMsg(Keys.D);
                while ( thePlayer.findAngleBetween(tLocation) < -0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.D);
            }
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