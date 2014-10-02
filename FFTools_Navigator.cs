using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class Navigator {
        private static MemoryManager TheMemory;
        public Navigator(MemoryManager newTheMemory) {
            TheMemory = newTheMemory;
        }
    } 
}