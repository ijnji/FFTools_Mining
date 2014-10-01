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
            //mdlist.Add(new MineralDeposit(false, (float)205.1436, 0, (float)-83.35779));
            //mdlist.Add(new MineralDeposit(false, (float)216.1513, 0, (float)-87.30682));
            //mdlist.Add(new MineralDeposit(false, (float)221.7113, 0, (float)-95.18837));
            //mdlist.Add(new MineralDeposit(false, (float)225.8484, 0, (float)-106.7841));
            //mdlist.Add(new MineralDeposit(false, (float)256.3488, 0, (float)-215.9667));
            //mdlist.Add(new MineralDeposit(false, (float)262.8185, 0, (float)-170.5062));
            //mdlist.Add(new MineralDeposit(false, (float)274.6811, 0, (float)-247.5));
            //mdlist.Add(new MineralDeposit(false, (float)286.9102, 0, (float)-252.5938));
            //mdlist.Add(new MineralDeposit(false, (float)317.0013, 0, (float)-178.881));
            //mdlist.Add(new MineralDeposit(false, (float)323.3648, 0, (float)-182.2007));
            //mdlist.Add(new MineralDeposit(false, (float)325.8448, 0, (float)-265.9896));
            //mdlist.Add(new MineralDeposit(false, (float)333.5263, 0, (float)-214.3547));
            //mdlist.Add(new MineralDeposit(false, (float)334.8407, 0, (float)-242.5161));
            //mdlist.Add(new MineralDeposit(true , (float)332.7316, 0, (float)-256.8401));
            //mdlist.Add(new MineralDeposit(true, (float)261.0139, 0, (float)-202.0589));
            //mdlist.Add(new MineralDeposit(true, (float)291.211, 0, (float)-255.4915));
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


        private const int BITMAP_SIZE_IN_PIXELS = 2200;
        private const int BITMAP_OFFSET_TO_ORIGIN = BITMAP_SIZE_IN_PIXELS / 2;
        private const int GRID_PADDING_IN_PIXELS = 100;
        private const int GRID_SPACING_IN_EILMS = 5;
        private const int GRID_PIXELS_PER_ILMS = 2;
        private const int GRID_COLOR_LINES_R = 0x30;
        private const int GRID_COLOR_LINES_G = 0x30;
        private const int GRID_COLOR_LINES_B = 0x30;
        private const int GRID_COLOR_MINDEP_R = 0xF0;
        private const int GRID_COLOR_MINDEP_G = 0xF0;
        private const int GRID_COLOR_MINDEP_B = 0xFF;
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
            this.Size = new Size(500, 500);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
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
        // Draw on a 1000px x 1000px bitmap in memory where the x-axis is at y=500px, and the y-axis is at x=500px.
        // Then the section of the bitmap is shown on the form.
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            Bitmap bmp = new Bitmap(BITMAP_SIZE_IN_PIXELS, BITMAP_SIZE_IN_PIXELS, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics gBmp = Graphics.FromImage(bmp);
            gBmp.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Grab a copy of semaphored view data.
            List<MineralDeposit> tmpViewMinDepList = new List<MineralDeposit>();
            Player tmpViewPlayer;
            lock (DataLock) {
                foreach (MineralDeposit vmd in ViewMinDepList) {
                    tmpViewMinDepList.Add(new MineralDeposit(vmd.vis, vmd.x, vmd.z, vmd.y));
                }
                tmpViewPlayer = new Player(ViewPlayer.x, ViewPlayer.z, ViewPlayer.y, ViewPlayer.rot);
            }

            paintGrid(gBmp);
            paintMineralDeposits(gBmp, tmpViewMinDepList);
            paintPlayer(gBmp, tmpViewPlayer);
            //paintText(gBmp);

            // Find top-left, bottom-right in aboslute ilms of mineral deposits.
            // Convert to absolute pixels for bitmap.
            // Resize form accoridingly and draw.
            float smallestX = BITMAP_OFFSET_TO_ORIGIN;
            float smallestY = BITMAP_OFFSET_TO_ORIGIN;
            float largestX = -BITMAP_OFFSET_TO_ORIGIN;
            float largestY = -BITMAP_OFFSET_TO_ORIGIN;
            foreach (MineralDeposit tvmd in tmpViewMinDepList) {
                if (tvmd.x < smallestX) smallestX = tvmd.x;
                if (tvmd.y < smallestY) smallestY = tvmd.y;
                if (tvmd.x > largestX) largestX = tvmd.x;
                if (tvmd.y > largestY) largestY = tvmd.y;
            }
            int bitmapSmallestX = (int)Math.Round(smallestX * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapSmallestY = (int)Math.Round(smallestY * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapLargestX = (int)Math.Round(largestX * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapLargestY = (int)Math.Round(largestY * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;

            // If there 1 or fewer mineral deposits, default to a size.
            if (tmpViewMinDepList.Count < 2) {
                bitmapSmallestX = BITMAP_OFFSET_TO_ORIGIN - 200;
                bitmapSmallestY = BITMAP_OFFSET_TO_ORIGIN - 200;
                bitmapLargestX = BITMAP_OFFSET_TO_ORIGIN + 200;
                bitmapLargestY = BITMAP_OFFSET_TO_ORIGIN + 200;
            }

            this.Size = new Size(bitmapLargestX - bitmapSmallestX + (2*GRID_PADDING_IN_PIXELS),
                                 bitmapLargestY - bitmapSmallestY + (2*GRID_PADDING_IN_PIXELS));
            Graphics gForm = e.Graphics;
            gForm.FillRectangle(Brushes.Black, 0, 0, 
                                bitmapLargestX - bitmapSmallestX + (2*GRID_PADDING_IN_PIXELS),
                                bitmapLargestY - bitmapSmallestY + (2*GRID_PADDING_IN_PIXELS));
            RectangleF desRect = new RectangleF(0, 0,
                                                bitmapLargestX - bitmapSmallestX + (2*GRID_PADDING_IN_PIXELS),
                                                bitmapLargestY - bitmapSmallestY + (2*GRID_PADDING_IN_PIXELS));
            RectangleF srcRect = new RectangleF(bitmapSmallestX - GRID_PADDING_IN_PIXELS,
                                                bitmapSmallestY - GRID_PADDING_IN_PIXELS,
                                                bitmapLargestX - bitmapSmallestX + (2*GRID_PADDING_IN_PIXELS),
                                                bitmapLargestY - bitmapSmallestY + (2*GRID_PADDING_IN_PIXELS));
            gForm.DrawImage(bmp, desRect, srcRect, GraphicsUnit.Pixel);
        }
        private void paintGrid(Graphics gBmp) {
            int pixels_per_grid_unit = GRID_SPACING_IN_EILMS * GRID_PIXELS_PER_ILMS;
            Pen gridPen = new Pen(Color.FromArgb(GRID_COLOR_LINES_R,
                                                 GRID_COLOR_LINES_G,
                                                 GRID_COLOR_LINES_B));
            // Vertical lines.
            for (int x = 0; x < BITMAP_SIZE_IN_PIXELS; x += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, x, 0, x, BITMAP_SIZE_IN_PIXELS);
            }
            // Horizontal lines.
            for (int y = 0; y < BITMAP_SIZE_IN_PIXELS; y += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, 0, y, BITMAP_SIZE_IN_PIXELS, y);
            }
            gridPen.Dispose();
        }
        private void paintMineralDeposits(Graphics gBmp, List<MineralDeposit> tmpViewMinDepList) {
            SolidBrush mdBrush = new SolidBrush(Color.FromArgb(GRID_COLOR_MINDEP_R,
                                                               GRID_COLOR_MINDEP_G,
                                                               GRID_COLOR_MINDEP_B));
            foreach (MineralDeposit tvmd in tmpViewMinDepList) {
                gBmp.FillEllipse(mdBrush, 
                    (int)Math.Round(tvmd.x * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN, 
                    (int)Math.Round(tvmd.y * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN,
                    6, 6);
            }
        }
        private void paintPlayer(Graphics gBmp, Player tmpViewPlayer) {
            gBmp.FillEllipse(Brushes.Crimson,
                (int)Math.Round(tmpViewPlayer.x * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN,
                (int)Math.Round(tmpViewPlayer.y * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN,
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