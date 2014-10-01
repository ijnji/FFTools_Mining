using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FFTools {
    public class MemoryManager {
        // Memory handling.
        private const int PERM_PROC_WM_READ = 0x0010;
        private const int MEM_COMMIT = 0x00001000;
        private const int PAGE_READWRITE = 0x04;
        private const int PAGE_READONLY= 0x02;
        private const int PAGE_EXECUTE_READWRITE = 0x40;
        private const int PAGE_EXECUTE_READ = 0x20;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        [DllImport("user32.dll")]
        private static extern void SetForegroundWindow(IntPtr hWnd);
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int plNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // Pointer walk offsets for address bases - v2014.09.11
        private int[] ADDR_PWALK_PLAYX = {0x1037160, 0x31C, 0x504, 0xA0};
        private int[] ADDR_PWALK_FISHBITE = {0xFF7BDC, 0x0, 0x3C, 0x2C};
        // Pointer walk offsets for address bases - earlier.
        // private const int[] ADDR_PWALK_FISHBITE = {0x1034CD8, 0x14, 0x118};
        // private const int[] ADDR_PWALK_GENDIAG = {0xF20F70, 0x18, 0x42C, 0x0};
        // Alt - private const int[] ADDR_PWALK_GENDIAG = {0xF20F70, 0x1C, 0x18, 0x42C, 0x0};

        // Address offsets from address bases.
        private const int ADDR_OFF_PLAYERZ = 0x4;
        private const int ADDR_OFF_PLAYERY = 0x8;
        private const int ADDR_OFF_PLAYERROT = 0x10;
        private const int ADDR_OFF_MINDEPVIS = 0xC8;
        private const int ADDR_OFF_MINDEPX = 0x70;
        private const int ADDR_OFF_MINDEPZ = 0x74;
        private const int ADDR_OFF_MINDEPY = 0x78;

        // Other constants.
        private int[] GENDIAG_STARTPATTERN = {0x02, 0x13, 0x06, 0x100, 0x100, 0x100, 0x100, 0x100, 0x03};
        private int[] GENDIAG_ENDPATTERN = {0x02, 0x13, 0x02, 0xEC, 0x03, 0x0D};
        private int FISHBITE_BITE = 0x1;
        private const int MINDEP_VIS = 0x0;
        private const int MINDEP_INVIS = 0x80;        

        // Address bases.
        private Process Proc = null;
        private IntPtr ProcHandle = IntPtr.Zero;
        private IntPtr ProcAddrBase = IntPtr.Zero;
        private IntPtr AddrPlayerX = IntPtr.Zero;
        private IntPtr AddrGenDiag = IntPtr.Zero;
        private IntPtr AddrFishBite = IntPtr.Zero;

        // Other fields. 
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint lType;
        }
    
        public MemoryManager() {}

        public int initialize() {
            Process[] pList = Process.GetProcessesByName("ffxiv");
            if (pList.Length == 0)
            {
                System.Console.WriteLine("Could not find the FFXIV process.");
                return 1;
            }
            Proc = pList[0];
            ProcHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PERM_PROC_WM_READ, false, Proc.Id);
            ProcAddrBase = Proc.MainModule.BaseAddress;
            System.Console.WriteLine("---");
            System.Console.WriteLine("Found the FFXIV process at 0x" + ProcAddrBase.ToString("X8"));

            System.Console.WriteLine("---");
            System.Console.WriteLine("Pointer walking for the player x location address...");
            AddrPlayerX = pointerWalk(ProcAddrBase, ADDR_PWALK_PLAYX);
            System.Console.WriteLine("Setting fish bite status address as 0x" + AddrPlayerX.ToString("X8"));

             System.Console.WriteLine("---");
              //System.Console.WriteLine("Pointer walking for the general dialog box address...");
              //AddrGenDiag = pointerWalk(ProcAddrBase, ADDROFFS_GENDIAG);
            System.Console.WriteLine("Manually setting the general dialog box address...");
            AddrGenDiag = (IntPtr)0x05AF3680;
            System.Console.WriteLine("Setting general dialog box address as 0x" + AddrGenDiag.ToString("X8"));     

            System.Console.WriteLine("---");
            System.Console.WriteLine("Pointer walking for the fish bite status address...");
            AddrFishBite = pointerWalk(ProcAddrBase, ADDR_PWALK_FISHBITE);
            System.Console.WriteLine("Setting fish bite status address as 0x" + AddrFishBite.ToString("X8"));
         
            System.Console.WriteLine("---");

            return 0;
        }

        private IntPtr pointerWalk(IntPtr addrBase, int[] addrOffs) {
            if (addrOffs.Length == 0) return addrBase;
            IntPtr addrCurrent = addrBase;
            for (int i = 0; i < addrOffs.Length; i++) {
                if (i == addrOffs.Length - 1) {
                    IntPtr addrNew = IntPtr.Add(addrCurrent, addrOffs[i]);
                    System.Console.WriteLine(
                        "0x" + addrCurrent.ToString("X8") + " + " +
                        "0x" + addrOffs[i].ToString("X8") + " = " +
                        "0x" + addrNew.ToString("X8")
                    );
                    return addrNew;
                } else {  
                    IntPtr addrNew = IntPtr.Add(addrCurrent, addrOffs[i]);
                    addrNew = (IntPtr)readProcInt(addrNew);
                    System.Console.WriteLine(
                        "[0x" + addrCurrent.ToString("X8") + " + " +
                        "0x" + addrOffs[i].ToString("X8") + "] -> " +
                        "0x" + addrNew.ToString("X8")
                    );
                    addrCurrent = addrNew;
                }
            }
            return addrCurrent;
        }
        public void sendKeyPressMsg(Keys key, int delay) {
            PostMessage(Proc.MainWindowHandle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
            Thread.Sleep(delay);
            PostMessage(Proc.MainWindowHandle, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
        }

        public void sendKeyDownMsg(Keys key) {
            PostMessage(Proc.MainWindowHandle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
        }

        public void sendKeyUpMsg(Keys key) {
            PostMessage(Proc.MainWindowHandle, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
        }

        private int readProcInt(IntPtr addr) {
            return BitConverter.ToInt32(readProcByteBlock(addr, 4), 0);
        }

        private float readProcFloat(IntPtr addr) {
            return BitConverter.ToSingle(readProcByteBlock(addr, 4), 0);
        }

        private byte[] readProcByteBlock(IntPtr addr, int bytesToRead) {
            byte[] buffer = new byte[bytesToRead];
            int bytesRead = 0;
            ReadProcessMemory(ProcHandle, addr, buffer, buffer.Length, ref bytesRead);
            return buffer;
        }



        public Player readPlayer() {
            float px = readProcFloat(AddrPlayerX);
            float pz = readProcFloat(IntPtr.Add(AddrPlayerX, ADDR_OFF_PLAYERZ));
            float py = readProcFloat(IntPtr.Add(AddrPlayerX, ADDR_OFF_PLAYERY));
            float prot = readProcFloat(IntPtr.Add(AddrPlayerX, ADDR_OFF_PLAYERROT));
            return new Player(px, pz, py, prot);
        }

        public List<string> readGeneralDialogueList() {
            byte[] byteBuffer = readProcByteBlock(AddrGenDiag, 2048);
            Queue<int> startIndices = findPattern(byteBuffer, GENDIAG_STARTPATTERN);
            Queue<int> endIndices = findPattern(byteBuffer, GENDIAG_ENDPATTERN);
            pruneGenDiagIndices(startIndices, endIndices);
            List<string> stringList = new List<string>();
            foreach (int startIndex in startIndices) {
                int endIndex = endIndices.Dequeue();
                List<char> stringBuffer = new List<char>();
                for (int i = startIndex + GENDIAG_STARTPATTERN.Length; i < endIndex; i++) {
                    stringBuffer.Add((char)byteBuffer[i]);
                }
                stringList.Add(new string(stringBuffer.ToArray()));
            }
            return stringList;
        }

        // Returns a List<int> of pattern locations in array.
        // The pattern symbols are in int to allow for wildcards (values > 0xFF).
        // The first symbol in the pattern cannot be a wildcard.
        private Queue<int> findPattern(byte[] array, int[] pattern) {
            Queue<int> startIndices = new Queue<int>();
            Queue<int> possibleStartIndices = new Queue<int>();
            for (int i = 0; i < array.Length - pattern.Length + 1; i++) {
                if (array[i] == pattern[0]) {
                    possibleStartIndices.Enqueue(i);
                }
            }
            foreach (int possibleStartIndex in possibleStartIndices) {
                bool matches = true;
                for (int i = 0; i < pattern.Length; i++) {
                    if (pattern[i] > 0xFF) {
                        continue;
                    }
                    if (array[possibleStartIndex + i] != pattern[i]) {
                        matches = false;
                        break;
                    }
                }
                if (matches) {
                    startIndices.Enqueue(possibleStartIndex);
                }
            }
            return startIndices;
        }

        private void pruneGenDiagIndices(Queue<int> startIndices, Queue<int> endIndices) {
            Queue<int> startIndicesPostPrune = new Queue<int>();
            Queue<int> endIndicesPostPrune = new Queue<int>();
            int startIndex = 0;
            int endIndex = 0;
            bool first = true; // Used to not make a valid start check for the first entry in GenDiag.
            while ( (startIndices.Count > 0) && (endIndices.Count > 0) ) {
                startIndex = startIndices.Dequeue();
                if (first) {
                    first = false;
                } else {
                    if (startIndex != endIndex + GENDIAG_ENDPATTERN.Length) break;
                }
                endIndex = endIndices.Dequeue();
                while ( (startIndices.Count > 0 ) && (startIndices.Peek() < endIndex) ) {
                    startIndices.Dequeue();
                }
                startIndicesPostPrune.Enqueue(startIndex);
                endIndicesPostPrune.Enqueue(endIndex);
            }
            startIndices.Clear();
            endIndices.Clear();
            foreach (int i in startIndicesPostPrune) {
                startIndices.Enqueue(i);
            }
            foreach (int i in endIndicesPostPrune) {
                endIndices.Enqueue(i);
            }
            return;
        }

        public bool readFishBite() {
            if ( readProcInt(AddrFishBite) == FISHBITE_BITE ) return true;
            else return false;
        }

        public List<MineralDeposit> readMineralDepositList(List<IntPtr> Addresses) {
            List<MineralDeposit> mdlist = new List<MineralDeposit>();
            foreach (IntPtr mdaddr in Addresses) {
                bool vis = (readProcInt(IntPtr.Add(mdaddr, ADDR_OFF_MINDEPVIS)) == MINDEP_VIS) ? true : false;
                float mdx = readProcFloat(IntPtr.Add(mdaddr, ADDR_OFF_MINDEPX));
                float mdz = readProcFloat(IntPtr.Add(mdaddr, ADDR_OFF_MINDEPZ));
                float mdy = readProcFloat(IntPtr.Add(mdaddr, ADDR_OFF_MINDEPY));
                mdlist.Add(new MineralDeposit(vis, mdx, mdz, mdy));
            }
            return mdlist;
        }

        public List <IntPtr> findAddresses (byte[] Stuff) {
        //Find visible nodes
            //System.Console.WriteLine("finding addresses of stuff");
            long PROC_VM_SIZE = this.Proc.VirtualMemorySize64;
            //System.Console.WriteLine("process memory size: "+PROC_VM_SIZE);
            long mem_count = 0;
            List <IntPtr> Addresses = new List <IntPtr> ();
            IntPtr address = this.ProcAddrBase;
            IntPtr StuffAddress = IntPtr.Zero;
            
            // this will store any information we get from VirtualQueryEx()
            MEMORY_BASIC_INFORMATION mem_basic_info = new MEMORY_BASIC_INFORMATION();
            int bytesRead = 0;  // number of bytes read with ReadProcessMemory
    
            //search memory
            while (mem_count < PROC_VM_SIZE)
            {
                VirtualQueryEx(this.ProcHandle, address, out mem_basic_info, (uint) Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                // if this memory chunk is readable 
                if ((mem_basic_info.Protect == PAGE_READWRITE
                     || mem_basic_info.Protect == PAGE_READONLY
                     || mem_basic_info.Protect == PAGE_EXECUTE_READWRITE
                     || mem_basic_info.Protect == PAGE_EXECUTE_READ) 
                        && mem_basic_info.State == MEM_COMMIT)
                {
                    byte[] buffer = new byte[(int)mem_basic_info.RegionSize];
                    //read memory and dump into buffer to search 
                    ReadProcessMemory(this.ProcHandle, mem_basic_info.BaseAddress, buffer, (int)mem_basic_info.RegionSize, ref bytesRead);
                    //search buffer
                    int i = 0;
                    while (i < (int) mem_basic_info.RegionSize) {
                        int p = 0;
                        while (buffer[i] == Stuff[p]) {
                            i++; p++;
                            if ((p >= Stuff.Length) || (i >= (int) mem_basic_info.RegionSize)) break;
                        }
                        if (p == Stuff.Length) {
                            StuffAddress = address + i - Stuff.Length;
                            //System.Console.WriteLine("Stuff Found! " + StuffAddress.ToString("X8"));
                            Addresses.Add(StuffAddress);
                        }
                        i++;
                    }
                }
                // move to the next memory chunk
                mem_count = mem_count + (long)mem_basic_info.RegionSize;
                address = new IntPtr((long)address + (long) mem_basic_info.RegionSize);
            }
            //System.Console.WriteLine("memory searched :" + mem_count);
            return Addresses;
        }
    }
}