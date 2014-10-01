using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    // Program settings.
    //private static const Keys K_CAST = Keys.D2;

    public class Mining {
        public static void Main() {
            String MineType= "Mature Tree"; //set to desired farming type ex: Mineral Deposit, Mature Tree
            byte[] MineTypeByteArray = Encoding.ASCII.GetBytes(MineType);

            MemoryManager theMemory = new MemoryManager();
            if (theMemory.initialize() > 0) Environment.Exit(1);
            Player thePlayer = theMemory.readPlayer();
            System.Console.WriteLine(thePlayer);
            List<string> theGenDiagList = theMemory.readGeneralDialogueList();

            //List<MineralDeposit> theMinDepList = theMemory.readMineralDepositList();
            //foreach (MineralDeposit md in theMinDepList) {
                //System.Console.WriteLine(md);
            //}
            List<IntPtr> MineTypeAddresses = theMemory.findAddresses(MineTypeByteArray);
            List<MineralDeposit> theMinDepList = theMemory.readMineralDepositList(MineTypeAddresses);
            MineralDeposit md = nearestVisibleMineralDeposit(thePlayer, theMinDepList);
            Queue<MineralDeposit> mdHistory = new Queue<MineralDeposit>();
            while (true) {
                thePlayer = theMemory.readPlayer();
                MineTypeAddresses = theMemory.findAddresses(MineTypeByteArray);
                theMinDepList = theMemory.readMineralDepositList(MineTypeAddresses);
                System.Console.WriteLine("-------");
                System.Console.WriteLine("Nearest mineral deposit is at...");
                md = nearestVisibleMineralDeposit(thePlayer, theMinDepList);
                System.Console.WriteLine(md);
                if( findDistanceBetween(thePlayer, md.x, md.y) < 250) {
                    System.Console.WriteLine("With a distance of " + findDistanceBetween(thePlayer, md.x, md.y));
                    System.Console.WriteLine("Need to face " + findOrientationRelativeTo(thePlayer, md.x, md.y));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, md.x, md.y);
                    mineFrom(theMemory);
                    System.Console.WriteLine("Done with this node!");
                    if (mdHistory.Count > 10) mdHistory.Dequeue();
                    mdHistory.Enqueue(md);
                }
                else {
                    System.Console.WriteLine("No nearby node, moving to previous good node and searching again");
                    md = mdHistory.Dequeue();
                    System.Console.WriteLine("With a distance of " + findDistanceBetween(thePlayer, md.x, md.y));
                    System.Console.WriteLine("Need to face " + findOrientationRelativeTo(thePlayer, md.x, md.y));
                    System.Console.WriteLine("Traveling to the node...");
                    travelTo(theMemory, md.x, md.y);
                }
            }
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

        // Distance between player and targetx,targety.
        private static float findDistanceBetween(Player thePlayer, float tx, float ty) {
            float dx = thePlayer.x - tx;
            float dy = thePlayer.y - ty;
            return (float)Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }

        // Angle between player and targetx,targety.
        private static float findAngleBetween(Player thePlayer, float tx, float ty) {
            float prot = thePlayer.rot;
            float trot = findOrientationRelativeTo(thePlayer, tx, ty);
            float drot = Math.Max(prot, trot) - Math.Min(prot, trot);
            if ( drot <= (float)Math.PI ) {
                if (prot >= trot) {
                    return -1 * drot;
                } else {
                    return drot;
                }
            } else {
                if (prot >= trot) {
                    return (float)(2*Math.PI) - drot;
                } else {
                    return -1 * ((float)(2*Math.PI) - drot);
                }
            }
        }

        // Orientation player should face to targetx,targety.
        private static float findOrientationRelativeTo(Player thePlayer, float tx, float ty) {
            float dx = tx - thePlayer.x;
            float dy = ty - thePlayer.y;
            if (dy > 0) {
                if (dx > 0) {
                    return (float)Math.Atan(dx/dy);
                } else if (dx < 0) {
                    return 0 - (float)Math.Atan(-dx/dy);
                } else {
                    return 0;
                }
            } else if (dy < 0) {
                if (dx > 0) {
                    return (float)(Math.PI/2) + (float)Math.Atan(-dy/dx); 
                } else if (dx < 0) {
                    return 0 - (float)(Math.PI/2) - (float)Math.Atan(dy/dx);
                } else {
                    return (float)3.140;
                }
            } else {
                if (dx > 0) {
                    return (float)(Math.PI/2);
                } else if (dx < 0) {
                    return 0 - (float)(Math.PI/2);
                } else {
                    return 0;
                }
            }
        }

        private static void travelTo(MemoryManager theMemory, float tx, float ty) {
            Player thePlayer = theMemory.readPlayer();
            
            if ( findAngleBetween(thePlayer, tx, ty) > 0.1 ) {
                theMemory.sendKeyDownMsg(Keys.A);
                while ( findAngleBetween(thePlayer, tx, ty) > 0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.A);
            }
            if ( findAngleBetween(thePlayer, tx, ty) < -0.1 ) {
                theMemory.sendKeyDownMsg(Keys.D);
                while ( findAngleBetween(thePlayer, tx, ty) < -0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.D);
            }

            bool rotating_left = false;
            bool rotating_right = false;
            thePlayer = theMemory.readPlayer();
            while ( findDistanceBetween(thePlayer, tx, ty) > 2.0 ) {
                theMemory.sendKeyDownMsg(Keys.W);
                while ( findDistanceBetween(thePlayer, tx, ty) > 2.0 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                    if ( findAngleBetween(thePlayer, tx, ty) > 0.1 ) {
                        theMemory.sendKeyDownMsg(Keys.A);
                        rotating_left = true;
                    } else if ( findAngleBetween(thePlayer, tx, ty) < -0.1 ) {
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
            if ( findAngleBetween(thePlayer, tx, ty) > 0.1 ) {
                theMemory.sendKeyDownMsg(Keys.A);
                while ( findAngleBetween(thePlayer, tx, ty) > 0.1 ) {
                    Thread.Sleep(50);
                    thePlayer = theMemory.readPlayer();
                }
                theMemory.sendKeyUpMsg(Keys.A);
            }
            if ( findAngleBetween(thePlayer, tx, ty) < -0.1 ) {
                theMemory.sendKeyDownMsg(Keys.D);
                while ( findAngleBetween(thePlayer, tx, ty) < -0.1 ) {
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
                float distance = findDistanceBetween(thePlayer, md.x, md.y);
                if (distance < nearestMineralDepositDistance) {
                    nearestMineralDepositDistance = distance;
                    nearestMineralDeposit = md;
                }
            }
            return nearestMineralDeposit;
        }
    }
}