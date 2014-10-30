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
        private const int GRID_SPACING_IN_EILMS = 3;
        private const int GRID_PIXELS_PER_ILMS = 5;
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
        private const int GRID_COLOR_GRAPHOBS_R = 0x30;
        private const int GRID_COLOR_GRAPHOBS_G = 0x30;
        private const int GRID_COLOR_GRAPHOBS_B = 0x30;

        private System.Windows.Forms.Timer RefreshTimer;
        private IContainer Components;
        // Lock for all View objects.
        private Object MapFormLock = new Object();
        private float ViewEilmMinX;
        private float ViewEilmMinY;
        private List<GatheringNode> ViewGathNodeList;
        private List<Location> ViewGraphObs;
        private List<Location> ViewPath;
        private Player ViewPlayer;

        private NavigatorGraph TheNavigatorGraph = null;

        public MapForm(NavigatorGraph theNavigatorGraph) {
            InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(500, 500);
            this.MouseDown += new MouseEventHandler(MapForm_MouseDown);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            ViewEilmMinX = 0;
            ViewEilmMinY = 0;
            ViewGathNodeList = new List<GatheringNode>();
            ViewGraphObs = new List<Location>();
            ViewPath = new List<Location>();
            ViewPlayer = new Player(0, 0, 0, 0);
            TheNavigatorGraph = theNavigatorGraph;
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

        private void MapForm_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                float tmpViewEilmMinX = 0;
                float tmpViewEilmMinY = 0;
                lock (MapFormLock) {
                    tmpViewEilmMinX = ViewEilmMinX;
                    tmpViewEilmMinY = ViewEilmMinY;
                }
                float obsX = tmpViewEilmMinX + e.X / GRID_PIXELS_PER_ILMS;
                float obsY = tmpViewEilmMinY + e.Y / GRID_PIXELS_PER_ILMS;
                TheNavigatorGraph.toggleObstacle(new Location(obsX, obsY));
            }
            if (e.Button == MouseButtons.Left) {
                float tmpViewEilmMinX = 0;
                float tmpViewEilmMinY = 0;
                lock (MapFormLock) {
                    tmpViewEilmMinX = ViewEilmMinX;
                    tmpViewEilmMinY = ViewEilmMinY;
                }
                float obsX = tmpViewEilmMinX + e.X / GRID_PIXELS_PER_ILMS;
                float obsY = tmpViewEilmMinY + e.Y / GRID_PIXELS_PER_ILMS;
                List<Location> path = TheNavigatorGraph.findPath(new Location(-10f, 130f), new Location(obsX, obsY));
                this.setViewPath(path);
            }
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
            List<Location> tmpViewGraphObs = new List<Location>();
            List<Location> tmpViewPath = new List<Location>();
            Player tmpViewPlayer;
            lock (MapFormLock) {
                foreach (GatheringNode vgn in ViewGathNodeList) {
                    tmpViewGathNodeList.Add(new GatheringNode(vgn.vis, vgn.location));
                }
            }
            lock (MapFormLock) {
                foreach (Location gol in ViewGraphObs) {
                    tmpViewGraphObs.Add(new Location(gol.x, gol.y));
                }
            }
            lock (MapFormLock) {
                foreach (Location pl in ViewPath) {
                    tmpViewPath.Add(new Location(pl.x, pl.y));
                }
            }
            lock (MapFormLock) {
                tmpViewPlayer = new Player(ViewPlayer.location, ViewPlayer.rot);
            }

            paintGraphObs(gBmp, tmpViewGraphObs);
            paintGatheringNodes(gBmp, tmpViewGathNodeList);
            paintPath(gBmp, tmpViewPath);
            paintPlayer(gBmp, tmpViewPlayer);
            paintGrid(gBmp);

            // Find top-left, bottom-right in aboslute ilms of mineral deposits.
            // Convert to absolute pixels for bitmap.
            // Resize form accoridingly and draw.
            float topleftx = BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float toplefty = BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float botrighx = -BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;
            float botrighy = -BITMAP_OFFSET_TO_ORIGIN / GRID_PIXELS_PER_ILMS;

            foreach (GatheringNode tvgn in tmpViewGathNodeList) {
                if (tvgn.location.x < topleftx) topleftx = tvgn.location.x;
                if (tvgn.location.y < toplefty) toplefty = tvgn.location.y;
                if (tvgn.location.x > botrighx) botrighx = tvgn.location.x;
                if (tvgn.location.y > botrighy) botrighy = tvgn.location.y;
            }

            int bitmaptopleftx = (int)Math.Round(topleftx * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmaptoplefty = (int)Math.Round(toplefty * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapbotrighx = (int)Math.Round(botrighx * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapbotrighy = (int)Math.Round(botrighy * GRID_PIXELS_PER_ILMS) + BITMAP_OFFSET_TO_ORIGIN;
            int bitmapwidth = bitmapbotrighx - bitmaptopleftx + GRID_LEFT_PADDING_IN_PIXELS + GRID_RIGHT_PADDING_IN_PIXELS;
            int bitmapheigh = bitmapbotrighy - bitmaptoplefty + GRID_TOP_PADDING_IN_PIXELS + GRID_BOTTOM_PADDING_IN_PIXELS;

            this.Size = new Size(bitmapwidth, bitmapheigh);
            Graphics gForm = e.Graphics;
            gForm.FillRectangle(Brushes.Black, 0, 0, bitmapwidth, bitmapheigh);
            RectangleF desRect = new RectangleF(0, 0, bitmapwidth, bitmapheigh);
            RectangleF srcRect = new RectangleF(bitmaptopleftx - GRID_LEFT_PADDING_IN_PIXELS,
                                                bitmaptoplefty - GRID_TOP_PADDING_IN_PIXELS,
                                                bitmapwidth, bitmapheigh);
            gForm.DrawImage(bmp, desRect, srcRect, GraphicsUnit.Pixel);

            lock (MapFormLock) {
                ViewEilmMinX = topleftx - GRID_LEFT_PADDING_IN_PIXELS / GRID_PIXELS_PER_ILMS;
                ViewEilmMinY = toplefty - GRID_TOP_PADDING_IN_PIXELS / GRID_PIXELS_PER_ILMS;
            }
        }

        private void paintGrid(Graphics gBmp) {
            int pixels_per_grid_unit = GRID_SPACING_IN_EILMS * GRID_PIXELS_PER_ILMS;
            Pen gridPen = new Pen(Color.FromArgb(0xFF,
                                                 GRID_COLOR_LINES_R,
                                                 GRID_COLOR_LINES_G,
                                                 GRID_COLOR_LINES_B));
            // Vertical lines.
            for (int x = BITMAP_OFFSET_TO_ORIGIN; x < BITMAP_SIZE_IN_PIXELS; x += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, x, 0, x, BITMAP_SIZE_IN_PIXELS);
            }
            for (int x = BITMAP_OFFSET_TO_ORIGIN; x > 0; x -= pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, x, 0, x, BITMAP_SIZE_IN_PIXELS);
            }
            // Horizontal lines.
            for (int y = BITMAP_OFFSET_TO_ORIGIN; y < BITMAP_SIZE_IN_PIXELS; y += pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, 0, y, BITMAP_SIZE_IN_PIXELS, y);
            }
            for (int y = BITMAP_OFFSET_TO_ORIGIN; y > 0; y -= pixels_per_grid_unit) {
                gBmp.DrawLine(gridPen, 0, y, BITMAP_SIZE_IN_PIXELS, y);
            }
            gridPen.Dispose();
        }

        private void paintGraphObs(Graphics gBmp, List<Location> tmpViewGraphObs) {
            SolidBrush graphobsBrush = new SolidBrush(Color.FromArgb(0xFF,
                                                                     GRID_COLOR_GRAPHOBS_R,
                                                                     GRID_COLOR_GRAPHOBS_G,
                                                                     GRID_COLOR_GRAPHOBS_B));
            foreach (Location gol in tmpViewGraphObs) {
                gBmp.FillRectangle(graphobsBrush,
                    (int)Math.Round((gol.x - (float)GRID_SPACING_IN_EILMS / 2) * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN),
                    (int)Math.Round((gol.y - (float)GRID_SPACING_IN_EILMS / 2) * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN),
                    GRID_SPACING_IN_EILMS * GRID_PIXELS_PER_ILMS,
                    GRID_SPACING_IN_EILMS * GRID_PIXELS_PER_ILMS);
            }
            graphobsBrush.Dispose();
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
                        (int)Math.Round(tvgn.location.x * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 2), 
                        (int)Math.Round(tvgn.location.y * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 2),
                        4, 4);
                } else {
                    gBmp.FillEllipse(gninvBrush, 
                        (int)Math.Round(tvgn.location.x * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 2), 
                        (int)Math.Round(tvgn.location.y * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 2),
                        4, 4);
                }
            }
        }

        private void paintPath(Graphics gBmp, List<Location> tmpViewPath) {
            foreach (Location pl in tmpViewPath) {
                gBmp.FillEllipse(Brushes.Crimson,
                    (int)Math.Round(pl.x * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 1),
                    (int)Math.Round(pl.y * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 1),
                    2, 2);
            }
        }

        private void paintPlayer(Graphics gBmp, Player tmpViewPlayer) {
            gBmp.FillEllipse(Brushes.Crimson,
                (int)Math.Round(tmpViewPlayer.location.x * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 3),
                (int)Math.Round(tmpViewPlayer.location.y * GRID_PIXELS_PER_ILMS + BITMAP_OFFSET_TO_ORIGIN - 3),
                6, 6);
        }

        private void paintText(Graphics gBmp) {
            Font labelFont = new Font("Century Gothic", 9);
            SolidBrush labelBrush = new SolidBrush(Color.White);

            gBmp.DrawString("Test", labelFont, labelBrush, new PointF(80F, 80F));
        }

        public void setViewGathNodeList(List<GatheringNode> newGathNodeList) {
            lock (MapFormLock) {
                ViewGathNodeList.Clear();
                foreach (GatheringNode gn in newGathNodeList) {
                    ViewGathNodeList.Add(new GatheringNode(gn.vis, gn.location));
                }
            }
        }

        public void setViewGraphObstacles(List<Location> newObstacles) {
            lock (MapFormLock) {
                ViewGraphObs.Clear();
                foreach (Location gol in newObstacles) {
                    ViewGraphObs.Add(new Location(gol.x, gol.y));
                }
            }
        }

        public void setViewPath(List<Location> newPath) {
            lock (MapFormLock) {
                ViewPath.Clear();
                foreach (Location pl in newPath) {
                    ViewPath.Add(new Location(pl.x, pl.y));
                }
            }
        }

        public void setViewPlayer(Player newPlayer) {
            lock (MapFormLock) {
                ViewPlayer = new Player(newPlayer.location, newPlayer.rot);
            }
        }
    }
}