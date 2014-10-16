using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Navigator {
        private const bool VERBOSE = true;
        private const Keys NAV_KEY_FORWARD = Keys.W;
        private const Keys NAV_KEY_BACKWARD = Keys.S;
        private const Keys NAV_KEY_LEFT = Keys.A;
        private const Keys NAV_KEY_RIGHT = Keys.D;
        private const float NAV_DIS_FROM_TARGET = (float)2.5;
        private const float NAV_REL_RAD_ALIGN_L1 = (float)0.1;
        private const float NAV_REL_RAD_ALIGN_L2 = (float)Math.PI / 4;
        // The MemoryManager is used for sending keypresses only.
        private MemoryManager TheMemory = null;
        private enum States {STOPPED, MOVING};
        private States CurrentState = States.STOPPED;
        private Location TargetLoc = null;
        private List<Location> TargetLocList = null;
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
        public void update(Player thePlayer) {
            if (TargetLoc == null) return;
            float disTarget = Location.findDistanceBetween(thePlayer.location, TargetLoc);
            float angTarget = thePlayer.findAngleBetween(TargetLoc);
            switch (CurrentState) {
                case (States.STOPPED) :
                    if (disTarget <= NAV_DIS_FROM_TARGET) {
                        if (VERBOSE) {
                            System.Console.WriteLine("NAV: Target reached. " + TargetLoc);
                        }
                        readyNextTargetFromList();
                        if (TargetLoc != null) CurrentState = States.MOVING;
                        else {
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            CurrentState = States.STOPPED;
                        }
                    } else {
                        if (angTarget > NAV_REL_RAD_ALIGN_L1) setKeyLR(true, false);
                        else if (angTarget < -NAV_REL_RAD_ALIGN_L1) setKeyLR(false, true);
                        else {
                            setKeyFB(true, false);
                            setKeyLR(false, false);
                            CurrentState = States.MOVING;
                        }
                    }
                    break;
                case (States.MOVING) :
                    if (disTarget <= NAV_DIS_FROM_TARGET) {
                        if (VERBOSE) {
                            System.Console.WriteLine("NAV: Target reached. " + TargetLoc);
                        }
                        readyNextTargetFromList();
                        if (TargetLoc != null) CurrentState = States.MOVING;
                        else {
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            CurrentState = States.STOPPED;
                        }
                    } else {
                        if ( (angTarget > NAV_REL_RAD_ALIGN_L2) || (angTarget < -NAV_REL_RAD_ALIGN_L2) ) {
                            setKeyFB(false, false);
                            setKeyLR(false, false);
                            CurrentState = States.STOPPED;
                        } else {
                            setKeyFB(true, false);
                            if (angTarget > NAV_REL_RAD_ALIGN_L1) setKeyLR(true, false);
                            else if (angTarget < -NAV_REL_RAD_ALIGN_L1) setKeyLR(false, true);
                            else setKeyLR(false, false);
                        }
                    }
                    break;
            }
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
        public void moveTo(Location loc) {
            TargetLoc = loc;
        }
        public void moveThrough(List<Location> locList) {
            TargetLocList = locList;
            readyNextTargetFromList();
        }
    } 
}
