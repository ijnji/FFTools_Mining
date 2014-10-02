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
            // Ready singleton MemoryManager.
            TheMemory = new MemoryManager();
            if (TheMemory.initialize() > 0) Environment.Exit(1);
            // Get a first read of Player.
            Player thePlayer = TheMemory.readPlayer();
            System.Console.WriteLine(thePlayer);
            // Ready singleton Navigator.
            TheNavigator = new Navigator(TheMemory);
            // Get a first read of Gen Diag.
            List<string> theGenDiagList = TheMemory.readGeneralDialogueList();
            // Get a first read of Mineral Deposits.
            //List<MineralDeposit> theMinDepList = TheMemory.readMineralDepositList();

            // --- Test case Mineral Deposits ---
            List<MineralDeposit> theMinDepList = new List<MineralDeposit>();
            theMinDepList.Add(new MineralDeposit(false, (float)205.1436, 0, (float)-83.35779));
            theMinDepList.Add(new MineralDeposit(false, (float)216.1513, 0, (float)-87.30682));
            theMinDepList.Add(new MineralDeposit(false, (float)221.7113, 0, (float)-95.18837));
            theMinDepList.Add(new MineralDeposit(false, (float)225.8484, 0, (float)-106.7841));
            theMinDepList.Add(new MineralDeposit(false, (float)256.3488, 0, (float)-215.9667));
            theMinDepList.Add(new MineralDeposit(false, (float)262.8185, 0, (float)-170.5062));
            theMinDepList.Add(new MineralDeposit(false, (float)274.6811, 0, (float)-247.5));
            theMinDepList.Add(new MineralDeposit(false, (float)286.9102, 0, (float)-252.5938));
            theMinDepList.Add(new MineralDeposit(false, (float)317.0013, 0, (float)-178.881));
            theMinDepList.Add(new MineralDeposit(false, (float)323.3648, 0, (float)-182.2007));
            theMinDepList.Add(new MineralDeposit(false, (float)325.8448, 0, (float)-265.9896));
            theMinDepList.Add(new MineralDeposit(false, (float)333.5263, 0, (float)-214.3547));
            theMinDepList.Add(new MineralDeposit(false, (float)334.8407, 0, (float)-242.5161));
            theMinDepList.Add(new MineralDeposit(true , (float)332.7316, 0, (float)-256.8401));
            theMinDepList.Add(new MineralDeposit(true, (float)261.0139, 0, (float)-202.0589));
            theMinDepList.Add(new MineralDeposit(true, (float)291.211, 0, (float)-255.4915));
            // --- Test case Mineral Deposits ---

            foreach (MineralDeposit md in theMinDepList) {
                System.Console.WriteLine(md);
            }
            // Start the UI thread.
            GraphForm theGraphForm = new GraphForm();
            Thread formStartThread = new Thread(new ParameterizedThreadStart(formStart));
            formStartThread.Start(theGraphForm);
            
            theGraphForm.setViewMinDepList(theMinDepList);

            while (true) {
                //List<MineralDeposit> theMinDepList = TheMemory.readMineralDepositList();
                //System.Console.WriteLine("-------");
                //System.Console.WriteLine("Nearest mineral deposit is at...");
                //MineralDeposit md = nearestVisibleMineralDeposit(thePlayer, theMinDepList);
                //System.Console.WriteLine(md);
                //System.Console.WriteLine("With a distance of " + findDistanceBetween(thePlayer, md.x, md.y));
                //System.Console.WriteLine("Need to face " + findOrientationRelativeTo(thePlayer, md.x, md.y));
                //System.Console.WriteLine("Traveling to the node...");
                //travelTo(TheMemory, md.x, md.y);
                //mineFrom(theMemory);
                //System.Console.WriteLine("Done with this node!");                
            }
        }

        private static void formStart(Object theGraphForm) {
            Application.Run((GraphForm)theGraphForm);
        }

        private static void mineFrom(MemoryManager theMemory) {
            theMemory.sendKeyPressMsg(Keys.End, 500);
            Thread.Sleep(500);
            theMemory.sendKeyPressMsg(Keys.F9, 500);
            Thread.Sleep(200);
            theMemory.sendKeyPressMsg(Keys.NumPad1, 500);
            Thread.Sleep(1500);
            for (int i = 0; i < 4; i++) {
                theMemory.sendKeyPressMsg(Keys.NumPad1, 500);
                Thread.Sleep(3600);
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