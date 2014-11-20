using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Navigator {
        private const Keys NAV_KEY_FORWARD = Keys.W;
        private const Keys NAV_KEY_BACKWARD = Keys.S;
        private const Keys NAV_KEY_LEFT = Keys.A;
        private const Keys NAV_KEY_RIGHT = Keys.D;
        private const Keys NAV_KEY_CTRL_LEFT = Keys.NumPad4;
        private const Keys NAV_KEY_TARGET_FRONT = Keys.NumPad7;
        private const Keys NAV_KEY_FACE_TARGET = Keys.NumPad9;
        private const Keys NAV_KEY_OPEN_NODE = Keys.NumPad1;

        public const float NAV_DIS_FROM_TARGET = (float)2;
        private const float NAV_REL_RAD_ALIGN_L1 = (float)0.1;
        private const float NAV_REL_RAD_ALIGN_L2 = (float)Math.PI / 3;
        // The MemoryManager is used for sending keypresses only.
        private MemoryManager TheMemory = null;
        private enum States {STOPPED, MOVE_TO_LOC, ALIGN_TO_NODE, MINE_FROM_NODE};
        private States CurrentState = States.STOPPED;
        private bool NavEnable = false;
        private int Timer = 0;
        private int Command = 0;
        private Location TargetLoc = null;
        private List<Location> TargetLocList = null;
        private GatheringNode TargetGathNode = null;
        private bool KeyPressedForward = false;
        private bool KeyPressedBackward = false;
        private bool KeyPressedLeft = false;
        private bool KeyPressedRight = false;
        
        public Navigator(MemoryManager newTheMemory) {
            TheMemory = newTheMemory;
        }
        // Main loop is responsible for periodically calling update to refresh data.
        // Note, positive findOrientationRelativeTo player means the target location
        // is left from player's POV, and negative means right from POV.
        public void update(Player thePlayer, int timeElapsed) {
            if ( NavEnable == false ) return;
            if ( (TargetLoc == null) && (TargetGathNode == null) ) return;
            Timer += timeElapsed;
            float disTarget = (TargetLoc != null) ? Location.findDistanceBetween(thePlayer.location, TargetLoc) : 0;
            float angTarget = (TargetLoc != null) ? thePlayer.findAngleBetween(TargetLoc) : 0;
            switch (CurrentState) {
                case (States.STOPPED) :
                    if (disTarget <= NAV_DIS_FROM_TARGET) {
                        System.Console.WriteLine("NAV: Target location reached at " + TargetLoc);
                        readyNextTargetFromList();
                        if (TargetLoc != null) {
                            System.Console.WriteLine("NAV: Next target location at " + TargetLoc);
                            nextState(States.STOPPED);
                        } else {
                            System.Console.WriteLine("NAV: No next target location");
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            if (TargetGathNode != null) {
                                System.Console.WriteLine("NAV: Gathering node assigned");
                                nextState(States.ALIGN_TO_NODE);
                            } else {
                                nextState(States.STOPPED);
                            }
                        }
                    } else {
                        if (angTarget > NAV_REL_RAD_ALIGN_L1) setKeyLR(true, false);
                        else if (angTarget < -NAV_REL_RAD_ALIGN_L1) setKeyLR(false, true);
                        else {
                            setKeyFB(true, false);
                            setKeyLR(false, false);
                            nextState(States.MOVE_TO_LOC);
                        }
                    }
                    break;
                case (States.MOVE_TO_LOC) :
                    if (disTarget <= NAV_DIS_FROM_TARGET) {
                        System.Console.WriteLine("NAV: Target location reached at " + TargetLoc);
                        readyNextTargetFromList();
                        if (TargetLoc != null) {
                            System.Console.WriteLine("NAV: Next target location at " + TargetLoc);
                            nextState(States.MOVE_TO_LOC);
                        } else {
                            System.Console.WriteLine("NAV: No next target location");
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            if (TargetGathNode != null) {
                                System.Console.WriteLine("NAV: Gathering node assigned");
                                nextState(States.ALIGN_TO_NODE);
                            } else {
                                nextState(States.STOPPED);
                            }
                        }
                    } else {
                        if ( (angTarget > NAV_REL_RAD_ALIGN_L2) || (angTarget < -NAV_REL_RAD_ALIGN_L2) ) {
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            nextState(States.STOPPED);
                        } else {
                            setKeyFB(true, false);
                            if (angTarget > NAV_REL_RAD_ALIGN_L1) setKeyLR(true, false);
                            else if (angTarget < -NAV_REL_RAD_ALIGN_L1) setKeyLR(false, true);
                            else setKeyLR(false, false);
                        }
                    }
                    break;
                case (States.ALIGN_TO_NODE) :
                    if (angTarget > NAV_REL_RAD_ALIGN_L1) setKeyLR(true, false);
                    else if (angTarget < -NAV_REL_RAD_ALIGN_L1) setKeyLR(false, true);
                    else {
                        nextState(States.MINE_FROM_NODE);
                    }
                    break;
                case (States.MINE_FROM_NODE) :
                    // Sequential commands for the actual mining are tricky with this non-blocking update scheme.
                    // Might have to come back to re-factor this.
                    if ( (Timer >= 500) && (Command == 0) ) {
                        Command++; Timer = 0; 
                        TheMemory.sendKeyDownMsg(NAV_KEY_TARGET_FRONT);
                        System.Console.WriteLine("NAV: Targetting node");
                    }
                    if ( (Timer >= 250) && (Command == 1) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_TARGET_FRONT); 
                    }

                    if ( (Timer >= 250) && (Command == 2) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_FACE_TARGET); 
                        System.Console.WriteLine("NAV: Facing node");
                    }
                    if ( (Timer >= 250) && (Command == 3) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_FACE_TARGET); 
                    }

                    if ( (Timer >= 250) && (Command == 4) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_OPEN_NODE); 
                        System.Console.WriteLine("NAV: Opening node");
                    }
                    if ( (Timer >= 250) && (Command == 5) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_OPEN_NODE); 
                    }

                    if ( (Timer >= 250) && (Command == 6) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_CTRL_LEFT); 
                    }
                    if ( (Timer >= 250) && (Command == 7) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_CTRL_LEFT); 
                    }

                    if ( (Timer >= 2000) && (Command == 8) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_OPEN_NODE); 
                        System.Console.WriteLine("NAV: Mining a 1st time");
                    }
                    if ( (Timer >= 250) && (Command == 9) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_OPEN_NODE); 
                    }

                    if ( (Timer >= 3500) && (Command == 10) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_OPEN_NODE); 
                        System.Console.WriteLine("NAV: Mining a 2nd time");
                    }
                    if ( (Timer >= 250) && (Command == 11) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_OPEN_NODE); 
                    }

                    if ( (Timer >= 3500) && (Command == 12) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_OPEN_NODE); 
                        System.Console.WriteLine("NAV: Mining a 3rd time");
                    }
                    if ( (Timer >= 250) && (Command == 13) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_OPEN_NODE); 
                    }

                    if ( (Timer >= 3500) && (Command == 14) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyDownMsg(NAV_KEY_OPEN_NODE); 
                        System.Console.WriteLine("NAV: Mining a final time");
                        
                    }
                    if ( (Timer >= 250) && (Command == 15) ) {
                        Command++; Timer = 0;
                        TheMemory.sendKeyUpMsg(NAV_KEY_OPEN_NODE); 
                    }

                    if ( (Timer >= 3500) && (Command == 16) ) {
                        Command++; Timer = 0;
                        System.Console.WriteLine("NAV: Waiting for node to close");
                    }

                    if ( (Timer >= 1500) && (Command == 17) ) {
                        Command++; Timer = 0;
                        TargetLoc = null;
                        TargetLocList = null;
                        TargetGathNode = null;
                        nextState(States.STOPPED);
                    }
                    break;
            }
        }
        private void nextState(States s) {
            Timer = 0;
            Command = 0;
            System.Console.WriteLine("NAV: State transition. " + CurrentState + " -> " + s);
            CurrentState = s;
        }
        private void readyNextTargetFromList() {
            TargetLoc = null;
            if (TargetLocList != null) {
                if (TargetLocList.Count > 0) {
                    TargetLoc = TargetLocList[0];
                    TargetLocList.RemoveAt(0);
                } else {
                    TargetLoc = null;
                }
            }
        }
        public void ctrlMoveTo(Location loc) {
            TargetLoc = loc;
            TargetLocList = null;
            CurrentState = States.STOPPED;
        }
        public void ctrlMoveThrough(List<Location> locList) {
            TargetLocList = locList;
            readyNextTargetFromList();
        }
        public void ctrlGatherFrom(GatheringNode gn) {
            TargetLoc = gn.location;
            TargetLocList = null;
            TargetGathNode = gn;
        }
        public void ctrlStop() {
            TargetLoc = null;
            TargetLocList = null;
            CurrentState = States.STOPPED;
        }
        public bool sensArrivedAtTarget() {
            return (TargetLoc == null);
        }
        public bool sensMinedFromTarget() {
            return (TargetGathNode == null);
        }
        private void setKeyFB(bool forward, bool backward) {
            if (forward && !KeyPressedForward) {
                TheMemory.sendKeyDownMsg(NAV_KEY_FORWARD);
                KeyPressedForward = true;
            }
            if (!forward && KeyPressedForward) {
                TheMemory.sendKeyUpMsg(NAV_KEY_FORWARD);
                KeyPressedForward = false;
            }
            if (backward && !KeyPressedBackward) {
                TheMemory.sendKeyDownMsg(NAV_KEY_BACKWARD);
                KeyPressedBackward = true;
            }
            if (!backward && KeyPressedBackward) {
                TheMemory.sendKeyUpMsg(NAV_KEY_BACKWARD);
                KeyPressedBackward = false;
            }
        }
        private void setKeyLR(bool left, bool right) {
            if (left && !KeyPressedLeft) {
                TheMemory.sendKeyDownMsg(NAV_KEY_LEFT);
                KeyPressedLeft = true;
            }
            if (!left && KeyPressedLeft) {
                TheMemory.sendKeyUpMsg(NAV_KEY_LEFT);
                KeyPressedLeft = false;
            }
            if (right && !KeyPressedRight) {
                TheMemory.sendKeyDownMsg(NAV_KEY_RIGHT);
                KeyPressedRight = true;
            }
            if (!right && KeyPressedRight) {
                TheMemory.sendKeyUpMsg(NAV_KEY_RIGHT);
                KeyPressedRight = false;
            }
        }
        public void navEnableToggle() {
            if (NavEnable) NavEnable = false;
            else NavEnable = true;
        }
    } 
}
