using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Mining {
        private static MemoryManager TheMemory;
        private static Navigator TheNavigator;
        public static void Main() {
            String MineType= "Mature Tree"; //set to desired farming type ex: Mineral Deposit, Mature Tree
            byte[] MineTypeByteArray = Encoding.ASCII.GetBytes(MineType);

            // Ready singleton MemoryManager.
            MemoryManager theMemory = new MemoryManager();
            if (theMemory.initialize() > 0) Environment.Exit(1);
            // Get a first read of Player.
            Player thePlayer = TheMemory.readPlayer();
            System.Console.WriteLine(thePlayer);
            // Ready singleton Navigator.
            TheNavigator = new Navigator(TheMemory);
            // Get a first read of Gen Diag.
            List<string> theGenDiagList = theMemory.readGeneralDialogueList();

            List<IntPtr> MineTypeAddresses = theMemory.findAddresses(MineTypeByteArray);
            List<MineralDeposit> theMinDepList = theMemory.readMineralDepositList(MineTypeAddresses);
            MineralDeposit md = nearestVisibleMineralDeposit(thePlayer, theMinDepList);
            Queue<MineralDeposit> mdHistory = new Queue<MineralDeposit>();

            // Start the UI thread.
            GraphForm theGraphForm = new GraphForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theGraphForm);
            
            theGraphForm.setViewMinDepList(theMinDepList);
            
            while (true) {
                thePlayer = theMemory.readPlayer();
                MineTypeAddresses = theMemory.findAddresses(MineTypeByteArray);
                theMinDepList = theMemory.readMineralDepositList(MineTypeAddresses);
                System.Console.WriteLine("-------");
                System.Console.WriteLine("Nearest mineral deposit is at...");
                md = nearestVisibleMineralDeposit(thePlayer, theMinDepList);
                System.Console.WriteLine(md);
                if( Location.findDistanceBetween(thePlayer.location, md.location) < 250) { //TODO: fix 250 hack
                    System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, md.location));
                    System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(md.location));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, md.location);
                    mineFrom(theMemory);
                    System.Console.WriteLine("Done with this node!");
                    if (mdHistory.Count > 10) mdHistory.Dequeue();
                    mdHistory.Enqueue(md);
                }
                else {
                    System.Console.WriteLine("No nearby node, moving to previous good node and searching again");
                    md = mdHistory.Dequeue();
                    System.Console.WriteLine("With a distance of " + Location.findDistanceBetween(thePlayer.location, md.location));
                    System.Console.WriteLine("Need to face " + thePlayer.findOrientationRelativeTo(md.location));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, md.location);
                }
            }
        }

        private static void formStart(Object theGraphForm) {
            Application.Run((GraphForm)theGraphForm);
        }

        private static void mineFrom(MemoryManager theMemory) {
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

        private static MineralDeposit nearestVisibleMineralDeposit(Player thePlayer, List<MineralDeposit> theMinDepList) {
            List<MineralDeposit> visibleList = new List<MineralDeposit>();
            foreach (MineralDeposit md in theMinDepList) {
                if (md.vis) visibleList.Add(md);
            }
            MineralDeposit nearestMineralDeposit = null;
            float nearestMineralDepositDistance = Single.MaxValue;
            foreach (MineralDeposit md in visibleList) {
                float distance = Location.findDistanceBetween(thePlayer.location, md.location);
                if (distance < nearestMineralDepositDistance) {
                    nearestMineralDepositDistance = distance;
                    nearestMineralDeposit = md;
                }
            }
            return nearestMineralDeposit;
        }
    }
}