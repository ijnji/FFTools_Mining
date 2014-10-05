using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace FFTools {
    public class MapForm : Form {
        // Unit conversion constants.
        private const int BITMAP_SIZE_IN_PIXELS = 2000;
        private const int BITMAP_OFFSET_TO_ORIGIN = BITMAP_SIZE_IN_PIXELS / 2;
        // Grid appearance constants.
        private const int GRID_TOP_PADDING_IN_PIXELS = 20;
        private const int GRID_LEFT_PADDING_IN_PIXELS = 20;
        private const int GRID_RIGHT_PADDING_IN_PIXELS = 45;
        private const int GRID_BOTTOM_PADDING_IN_PIXELS = 65;
        private const int GRID_SPACING_IN_EILMS = 5;
        private const int GRID_PIXELS_PER_ILMS = 2;
        // Grid color constants.
        private const int GRID_COLOR_LINES_R = 0x30;
        private const int GRID_COLOR_LINES_G = 0x30;
        private const int GRID_COLOR_LINES_B = 0x30;
        private const int GRID_COLOR_GATHNODE_VIS_R = 0xFF;
        private const int GRID_COLOR_GATHNODE_VIS_G = 0xFF;
        private const int GRID_COLOR_GATHNODE_VIS_B = 0x40;
        private const int GRID_COLOR_GATHNODE_INV_R = 0x00;
        private const int GRID_COLOR_GATHNODE_INV_G = 0x80;
        private const int GRID_COLOR_GATHNODE_INV_B = 0x80;

        private System.Windows.Forms.Timer RefreshTimer;
        private IContainer Components;
        private Object DataLock = new Object();
        private Player ViewPlayer;
        private List<GatheringNode> ViewGathNodeList;
        public MapForm() {
            InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(500, 500);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            ViewGathNodeList = new List<GatheringNode>();
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
            List<GatheringNode> tmpViewGathNodeList = new List<GatheringNode>();
            Player tmpViewPlayer;
            lock (DataLock) {
                foreach (GatheringNode vgn in ViewGathNodeList) {
                    tmpViewGathNodeList.Add(new GatheringNode(vgn.vis, vgn.location));
                }
                tmpViewPlayer = new Player(ViewPlayer.location, ViewPlayer.rot);
            }

            paintGrid(gBmp);
            paintGatheringNodes(gBmp, tmpViewGathNodeList);
            paintPlayer(gBmp, tmpViewPlayer);
            //paintText(gBmp);

            // Find top-left, bottom-right in aboslute ilms of mineral deposits.
            // Convert to absolute pixels for bitmap.
            // Resize form accoridingly and draw.
            float topleftx = BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float toplefty = -BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float botrighx = -BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float botrighy = BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;

            foreach (GatheringNode tvgn in tmpViewGathNodeList) {
                if (tvgn.location.x < topleftx) topleftx = tvgn.location.x;
                if (tvgn.location.y > toplefty) toplefty = tvgn.location.y;
                if (tvgn.location.x > botrighx) botrighx = tvgn.location.x;
                if (tvgn.location.y < botrighy) botrighy = tvgn.location.y;
            }

            int bitmaptopleftx = (int)Math.Round(topleftx * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmaptoplefty = BITMAP_OFFSET_TO_ORIGIN - (int)Math.Round(toplefty * GRID_PIXELS_PER_ILMS);
            int bitmapbotrighx = (int)Math.Round(botrighx * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapbotrighy = BITMAP_OFFSET_TO_ORIGIN - (int)Math.Round(botrighy * GRID_PIXELS_PER_ILMS);

            this.Size = new Size(bitmapbotrighx - bitmaptopleftx + 
                                 GRID_LEFT_PADDING_IN_PIXELS + GRID_RIGHT_PADDING_IN_PIXELS,
                                 bitmapbotrighy - bitmaptoplefty +
                                 GRID_TOP_PADDING_IN_PIXELS + GRID_BOTTOM_PADDING_IN_PIXELS);
            Graphics gForm = e.Graphics;
            gForm.FillRectangle(Brushes.Black, 0, 0, 
                                bitmapbotrighx - bitmaptopleftx +
                                GRID_LEFT_PADDING_IN_PIXELS + GRID_RIGHT_PADDING_IN_PIXELS,
                                bitmapbotrighy - bitmaptoplefty +
                                GRID_TOP_PADDING_IN_PIXELS + GRID_BOTTOM_PADDING_IN_PIXELS);
            RectangleF desRect = new RectangleF(0, 0,
                                                bitmapbotrighx - bitmaptopleftx +
                                                GRID_LEFT_PADDING_IN_PIXELS + GRID_RIGHT_PADDING_IN_PIXELS,
                                                bitmapbotrighy - bitmaptoplefty +
                                                GRID_TOP_PADDING_IN_PIXELS + GRID_BOTTOM_PADDING_IN_PIXELS);
            RectangleF srcRect = new RectangleF(bitmaptopleftx - GRID_LEFT_PADDING_IN_PIXELS,
                                                bitmaptoplefty - GRID_TOP_PADDING_IN_PIXELS,
                                                bitmapbotrighx - bitmaptopleftx +
                                                GRID_LEFT_PADDING_IN_PIXELS + GRID_RIGHT_PADDING_IN_PIXELS,
                                                bitmapbotrighy - bitmaptoplefty +
                                                GRID_TOP_PADDING_IN_PIXELS + GRID_BOTTOM_PADDING_IN_PIXELS);
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
        private void paintGatheringNodes(Graphics gBmp, List<GatheringNode> tmpViewGathNodeList) {
            SolidBrush gnvisBrush = new SolidBrush(Color.FromArgb(0xFF,
                                                                            GRID_COLOR_GATHNODE_VIS_R,
                                                                            GRID_COLOR_GATHNODE_VIS_G,
                                                                            GRID_COLOR_GATHNODE_VIS_B));
            SolidBrush gninvBrush = new SolidBrush(Color.FromArgb(0xFF,
                                                                            GRID_COLOR_GATHNODE_INV_R,
                                                                            GRID_COLOR_GATHNODE_INV_G,
                                                                            GRID_COLOR_GATHNODE_INV_B));
            foreach (GatheringNode tvgn in tmpViewGathNodeList) {
                if (tvgn.vis) {
                    gBmp.FillEllipse(gnvisBrush, 
                        (int)Math.Round(tvgn.location.x * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN, 
                         BITMAP_OFFSET_TO_ORIGIN - (int)Math.Round(tvgn.location.y * GRID_PIXELS_PER_ILMS),
                        4, 4);
                } else {
                    gBmp.FillEllipse(gninvBrush, 
                        (int)Math.Round(tvgn.location.x * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN, 
                         BITMAP_OFFSET_TO_ORIGIN - (int)Math.Round(tvgn.location.y * GRID_PIXELS_PER_ILMS),
                        4, 4);
                }
            }
        }
        private void paintPlayer(Graphics gBmp, Player tmpViewPlayer) {
            gBmp.FillEllipse(Brushes.Crimson,
                (int)Math.Round(tmpViewPlayer.location.x * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN,
                BITMAP_OFFSET_TO_ORIGIN - (int)Math.Round(tmpViewPlayer.location.y * GRID_PIXELS_PER_ILMS),
                6, 6);
        }
        private void paintText(Graphics gBmp) {
            Font labelFont = new Font("Century Gothic", 9);
            SolidBrush labelBrush = new SolidBrush(Color.White);

            gBmp.DrawString("Test", labelFont, labelBrush, new PointF(80F, 80F));
        }
        public void setViewGathNodeList(List<GatheringNode> newGathNodeList) {
            lock (DataLock) {
                ViewGathNodeList.Clear();
                foreach (GatheringNode gn in newGathNodeList) {
                    ViewGathNodeList.Add(new GatheringNode(gn.vis, gn.location));
                }
            }
        }
        public void setViewPlayer(Player newPlayer) {
            lock (DataLock) {
                ViewPlayer = new Player(newPlayer.location, newPlayer.rot);
            }
        }
    }
}