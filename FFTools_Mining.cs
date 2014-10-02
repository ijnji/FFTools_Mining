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

            // Ready singleton MemoryManager.
            MemoryManager theMemory = new MemoryManager();
            if (theMemory.initialize() > 0) Environment.Exit(1);
            // Get a first read of Player.
            Player thePlayer = theMemory.readPlayer();
            System.Console.WriteLine(thePlayer);
            // Get a first read of Gen Diag.
            List<string> theGenDiagList = theMemory.readGeneralDialogueList();

            List<IntPtr> GathTypeAddresses = theMemory.findAddressesOfBytes(GathTypeByteArray);
            List<GatheringNode> theGathNodeList = theMemory.readGatheringNodeList(GathTypeAddresses);
            GatheringNode gn = nearestVisibleGatheringNode(thePlayer, theGathNodeList);
            Queue<GatheringNode> gnHistory = new Queue<GatheringNode>();

            // Start the UI thread.
            MapForm theMapForm = new MapForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theMapForm);

            theMapForm.setViewGathNodeList(theGathNodeList);
            
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