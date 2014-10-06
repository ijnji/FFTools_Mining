using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Navigator {
        // The MemoryManager is used for sending keypresses only.
        private MemoryManager TheMemory = null;
        private enum States {IDLE};
        private States CurrentState = States.IDLE;
        private Location TargetLoc = null;
        public Navigator(MemoryManager newTheMemory) {
            TheMemory = newTheMemory;
        }
        // Main loop is responsible for periodically calling update to refresh data.
        public void update(Player thePlayer) {

        }
    } 
}