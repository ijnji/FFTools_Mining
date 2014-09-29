using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class GraphForm : Form {
        private static GraphForm TheGraphForm;
        public static void Main() {
            TheGraphForm = new GraphForm();
            Thread formStartThread = new Thread(new ThreadStart(formStart));
            formStartThread.Start();

            List<MineralDeposit> mdlist = new List<MineralDeposit>();
            mdlist.Add(new MineralDeposit(false, (float)-111.6122, (float)0, (float)164.0807));
            mdlist.Add(new MineralDeposit(true, (float)-99.16898, (float)0, (float)158.4617));
            mdlist.Add(new MineralDeposit(false, (float)-117.896, (float)0, (float)168.2807));
            mdlist.Add(new MineralDeposit(false, (float)-89.49954, (float)0, (float)13.95296));
            mdlist.Add(new MineralDeposit(false, (float)-88.62637, (float)0, (float)24.27498));
            mdlist.Add(new MineralDeposit(false, (float)-30.22703, (float)0, (float)19.45272));
            mdlist.Add(new MineralDeposit(true, (float)-86.37704, (float)0, (float)32.77007));
            mdlist.Add(new MineralDeposit(false, (float)-23.19106, (float)0, (float)29.00829));
            mdlist.Add(new MineralDeposit(true, (float)-24.8432, (float)0, (float)39.98703));
            mdlist.Add(new MineralDeposit(false, (float)-46.58563, (float)0, (float)68.71722));
            mdlist.Add(new MineralDeposit(false, (float)-101.0807, (float)0, (float)79.47763));
            mdlist.Add(new MineralDeposit(false, (float)-46.45711, (float)0, (float)84.50039));
            mdlist.Add(new MineralDeposit(true, (float)-45.5108, (float)0, (float)78.73927));
            mdlist.Add(new MineralDeposit(false, (float)-97.96165, (float)0, (float)92.54741));
            mdlist.Add(new MineralDeposit(true, (float)-98.66853, (float)0, (float)87.56786));
            TheGraphForm.setViewMinDepList(mdlist);

            while (true) {
                for (int i = 0; i < 100; i++) {
                    TheGraphForm.setViewPlayer(new Player(i, 0, i, 0));
                    Thread.Sleep(50);
                }
            }
        }

        private static void formStart() {
            Application.Run(TheGraphForm);
        }


        private const int WIN_WIDTH = 650;
        private const int WIN_HEIGHT = 600;
        private const int GRID_ILMS_UNITS = 5;
        private const int ILMS_PADDING = 10;
        private const int PIXELS_PER_ILMS = 5;
        private System.Windows.Forms.Timer RefreshTimer;
        private IContainer Components;
        private Object DataLock = new Object();
        private Player ViewPlayer;
        private List<MineralDeposit> ViewMinDepList;
        public GraphForm() {
            InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(WIN_WIDTH, WIN_HEIGHT);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            ViewMinDepList = new List<MineralDeposit>();
            ViewPlayer = new Player(0, 0, 0, 0);
        }
        public void InitializeComponent() {
            Components = new System.ComponentModel.Container();
            RefreshTimer = new System.Windows.Forms.Timer(Components);
            RefreshTimer.Interval = 50;
            RefreshTimer.Tick += new EventHandler(timerTick);
        }
        protected override void Dispose(bool disposing) {
            if (disposing && (Components != null)) Components.Dispose();
            base.Dispose(disposing);
        }
        private void timerTick(object sender, System.EventArgs e) {
            Refresh();
        }
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            RefreshTimer.Start();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            Bitmap bmp = new Bitmap(this.ClientRectangle.Width, this.ClientRectangle.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics gBmp = Graphics.FromImage(bmp);
            gBmp.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            paintGrid(gBmp);
            paintMineralDeposits(gBmp);
            paintPlayer(gBmp);
            //paintText(gBmp);

            Graphics gForm = e.Graphics;
            gForm.FillRectangle(Brushes.Black, this.ClientRectangle);
            gForm.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
        }
        private void paintGrid(Graphics gBmp) {
            int pixels_per_grid_unit = GRID_ILMS_UNITS * PIXELS_PER_ILMS;
            Pen gridPen = new Pen(Color.FromArgb(0x30, 0x30, 0x30));
            // Vertical lines.
            for (int x = 0; x < this.ClientRectangle.Width; x += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, x, 0, x, this.ClientRectangle.Height);
            }
            // Horizontal lines.
            for (int y = 0; y < this.ClientRectangle.Height; y += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, 0, y, this.ClientRectangle.Width, y);
            }
            gridPen.Dispose();
        }
        private void paintMineralDeposits(Graphics gBmp) {
            List<MineralDeposit> tmpViewMinDepList = new List<MineralDeposit>();
            lock (DataLock) {
                foreach (MineralDeposit vmd in ViewMinDepList) {
                    tmpViewMinDepList.Add(new MineralDeposit(vmd.vis, vmd.x, vmd.z, vmd.y));
                }
            }
//            // Find shift to orgin offset.
//            float shiftx = Single.MaxValue;
//            float shifty = Single.MaxValue;
//            foreach (MineralDeposit tvmd in tmpViewMinDepList) {
//                if (Math.Abs(tvmd.x) < Math.Abs(shiftx)) shiftx = tvmd.x;
//                if (Math.Abs(tvmd.y) < Math.Abs(shifty)) shifty = tvmd.y;
//            }
//            // Pad shift offset.
//            if (shiftx < 0) shiftx += ILMS_PADDING;
//            else shiftx -= ILMS_PADDING;
//            if (shifty < 0) shifty += ILMS_PADDING;
//            else shifty -= ILMS_PADDING;
            // Draw mineral deposits.
            foreach (MineralDeposit tvmd in tmpViewMinDepList) {
                gBmp.FillEllipse(Brushes.AliceBlue, 
                    Math.Abs(tvmd.x) * PIXELS_PER_ILMS, 
                    Math.Abs(tvmd.y) * PIXELS_PER_ILMS,
                    6, 6);
            }
        }
        private void paintPlayer(Graphics gBmp) {
            Player tvp;
            lock (DataLock) {
                tvp = new Player(ViewPlayer.x,
                    ViewPlayer.z,
                    ViewPlayer.y,
                    ViewPlayer.rot);
            }
            gBmp.FillEllipse(Brushes.Crimson,
                Math.Abs(tvp.x) * PIXELS_PER_ILMS,
                Math.Abs(tvp.y) * PIXELS_PER_ILMS,
                10, 10);
        }
        private void paintText(Graphics gBmp) {
            Font labelFont = new Font("Century Gothic", 9);
            SolidBrush labelBrush = new SolidBrush(Color.White);

            gBmp.DrawString("Test", labelFont, labelBrush, new PointF(80F, 80F));
        }
        public void setViewMinDepList(List<MineralDeposit> newMinDepList) {
            lock (DataLock) {
                ViewMinDepList.Clear();
                foreach (MineralDeposit md in newMinDepList) {
                    ViewMinDepList.Add(new MineralDeposit(md.vis, md.x, md.z, md.y));
                }
            }
        }
        public void setViewPlayer(Player newPlayer) {
            lock (DataLock) {
                ViewPlayer = new Player(newPlayer.x,
                    newPlayer.z,
                    newPlayer.y,
                    newPlayer.rot);
            }
        }
    }
}