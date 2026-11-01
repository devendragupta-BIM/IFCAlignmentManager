using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace IFCAlignmentManager.UI
{
    /// <summary>
    /// Factory for generating crisp, modern, vector-styled UI icons and bitmaps
    /// for the Revit ribbon and Windows Forms dialog controls.
    /// Uses anti-aliased GDI+ rendering with premium architectural/BIM color palettes.
    /// </summary>
    public static class AppIcons
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        // ─── CONVERSION HELPERS ──────────────────────────────────────────────────

        /// <summary>
        /// Converts a System.Drawing.Bitmap into a WPF BitmapSource for Revit's PushButtonData.
        /// </summary>
        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap into a safe, leak-free System.Drawing.Icon for Form titlebars.
        /// </summary>
        public static Icon ToIcon(Bitmap bitmap)
        {
            if (bitmap == null) return null;
            IntPtr hIcon = bitmap.GetHicon();
            try
            {
                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    return (Icon)temp.Clone();
                }
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        // ─── ICON 1: ALIGNMENT WIZARD ────────────────────────────────────────────

        /// <summary>
        /// Premium 3D coordinate alignment icon:
        /// Intersecting coordinate planes & BIM model bounding cubes snapping into alignment
        /// with precision magnetic target crosshairs and cyan/azure gradients.
        /// </summary>
        public static Bitmap CreateWizardIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);

                float s = size;
                float r = s * 0.18f;

                // 1. Dark glass rounded squircle base tile
                using (var path = CreateRoundedRect(1, 1, s - 2, s - 2, r))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s),
                    Color.FromArgb(245, 15, 23, 42),   // Dark slate
                    Color.FromArgb(245, 30, 41, 59),   // Rich navy
                    135f))
                using (var pen = new Pen(Color.FromArgb(160, 56, 189, 248), Math.Max(1f, s * 0.04f)))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }

                if (size >= 24)
                {
                    // 2. Host Coordinate Grid / Model Box (Soft steel blue)
                    float bx = s * 0.18f, by = s * 0.22f, bw = s * 0.42f, bh = s * 0.42f;
                    using (var fill = new SolidBrush(Color.FromArgb(110, 71, 85, 105)))
                    using (var border = new Pen(Color.FromArgb(180, 148, 163, 184), 1.2f))
                    {
                        border.DashStyle = DashStyle.Solid;
                        g.FillRectangle(fill, bx, by, bw, bh);
                        g.DrawRectangle(border, bx, by, bw, bh);
                    }

                    // 3. Consultant Model Box (Vibrant Electric Cyan with gradient)
                    float cx = s * 0.38f, cy = s * 0.36f, cw = s * 0.44f, ch = s * 0.44f;
                    using (var fill = new LinearGradientBrush(
                        new RectangleF(cx, cy, cw, ch),
                        Color.FromArgb(210, 2, 132, 199),
                        Color.FromArgb(210, 14, 165, 233), 45f))
                    using (var border = new Pen(Color.FromArgb(255, 224, 242, 254), Math.Max(1.2f, s * 0.045f)))
                    {
                        g.FillRectangle(fill, cx, cy, cw, ch);
                        g.DrawRectangle(border, cx, cy, cw, ch);
                    }

                    // 4. Precision Snapping Crosshairs / Target Reticle
                    float tx = cx, ty = cy; // Snap corner
                    float tr = s * 0.15f;
                    using (var ringPen = new Pen(Color.FromArgb(255, 56, 189, 248), 1.6f))
                    using (var glowBrush = new SolidBrush(Color.FromArgb(70, 0, 229, 255)))
                    {
                        g.FillEllipse(glowBrush, tx - tr, ty - tr, tr * 2, tr * 2);
                        g.DrawEllipse(ringPen, tx - tr, ty - tr, tr * 2, tr * 2);

                        // Crosshair hairs
                        using (var crossPen = new Pen(Color.White, 1.3f))
                        {
                            g.DrawLine(crossPen, tx - tr * 1.3f, ty, tx + tr * 1.3f, ty);
                            g.DrawLine(crossPen, tx, ty - tr * 1.3f, tx, ty + tr * 1.3f);
                        }

                        // Center glowing dot
                        using (var dotBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                        {
                            g.FillEllipse(dotBrush, tx - 1.8f, ty - 1.8f, 3.6f, 3.6f);
                        }
                    }

                    // 5. Dynamic alignment motion arrows (Gold/Cyan)
                    using (var arrowPen = new Pen(Color.FromArgb(255, 251, 191, 36), 1.5f))
                    {
                        arrowPen.EndCap = LineCap.ArrowAnchor;
                        g.DrawLine(arrowPen, s * 0.72f, s * 0.22f, s * 0.58f, s * 0.34f);
                    }
                }
                else
                {
                    // High-contrast 16x16 icon: Clean overlapping geometric axis + snap reticle
                    using (var pen1 = new Pen(Color.FromArgb(180, 148, 163, 184), 1.2f))
                    {
                        g.DrawRectangle(pen1, 3, 3, 7, 7);
                    }
                    using (var fill2 = new SolidBrush(Color.FromArgb(230, 14, 165, 233)))
                    using (var pen2 = new Pen(Color.FromArgb(255, 224, 242, 254), 1.2f))
                    {
                        g.FillRectangle(fill2, 6, 6, 7, 7);
                        g.DrawRectangle(pen2, 6, 6, 7, 7);
                    }
                    // Precision center pip
                    using (var dot = new SolidBrush(Color.FromArgb(255, 251, 191, 36)))
                    {
                        g.FillEllipse(dot, 5, 5, 3, 3);
                    }
                }
            }
            return bmp;
        }

        // ─── ICON 2: DIAGNOSTICS VIEW ────────────────────────────────────────────

        /// <summary>
        /// Coordinate Diagnostics & Inspection Icon:
        /// Circular radar scanner with 3D XYZ coordinate vectors, precision reticle,
        /// and pulsing diagnostic audit sweep.
        /// </summary>
        public static Bitmap CreateDiagnosticsIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);

                float s = size;
                float r = s * 0.18f;

                // 1. Dark emerald/teal tech tile
                using (var path = CreateRoundedRect(1, 1, s - 2, s - 2, r))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s),
                    Color.FromArgb(245, 6, 78, 59),    // Deep emerald
                    Color.FromArgb(245, 15, 23, 42),   // Dark slate
                    135f))
                using (var pen = new Pen(Color.FromArgb(170, 52, 211, 153), Math.Max(1f, s * 0.04f)))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }

                float cx = s * 0.5f, cy = s * 0.5f;

                if (size >= 24)
                {
                    // 2. Concentric radar audit rings
                    float r1 = s * 0.35f, r2 = s * 0.22f;
                    using (var ringPen = new Pen(Color.FromArgb(70, 16, 185, 129), 1.0f))
                    {
                        g.DrawEllipse(ringPen, cx - r1, cy - r1, r1 * 2, r1 * 2);
                        g.DrawEllipse(ringPen, cx - r2, cy - r2, r2 * 2, r2 * 2);
                    }

                    // 3. Coordinate axes (X = Crimson, Y = Green, Z = Blue)
                    using (var xPen = new Pen(Color.FromArgb(250, 248, 113, 113), 1.5f)) // X-axis
                    using (var yPen = new Pen(Color.FromArgb(250, 52, 211, 153), 1.5f))  // Y-axis
                    using (var zPen = new Pen(Color.FromArgb(250, 96, 165, 250), 1.5f))  // Z-axis
                    {
                        xPen.EndCap = LineCap.ArrowAnchor;
                        yPen.EndCap = LineCap.ArrowAnchor;
                        zPen.EndCap = LineCap.ArrowAnchor;

                        g.DrawLine(xPen, cx, cy, cx + s * 0.34f, cy);                  // Right (+X)
                        g.DrawLine(yPen, cx, cy, cx, cy - s * 0.34f);                  // Up (+Y)
                        g.DrawLine(zPen, cx, cy, cx - s * 0.24f, cy + s * 0.24f);      // Diagonal (+Z)
                    }

                    // 4. Center pulse / reticle
                    using (var centerBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                    using (var pulseBrush = new SolidBrush(Color.FromArgb(100, 52, 211, 153)))
                    {
                        g.FillEllipse(pulseBrush, cx - 4f, cy - 4f, 8f, 8f);
                        g.FillEllipse(centerBrush, cx - 1.8f, cy - 1.8f, 3.6f, 3.6f);
                    }

                    // 5. Diagnostic Delta Symbol "Δ" in top right corner
                    using (var deltaPen = new Pen(Color.FromArgb(255, 251, 191, 36), 1.4f))
                    {
                        PointF[] deltaPts = new PointF[]
                        {
                            new PointF(s * 0.76f, s * 0.20f),
                            new PointF(s * 0.86f, s * 0.36f),
                            new PointF(s * 0.66f, s * 0.36f)
                        };
                        g.DrawPolygon(deltaPen, deltaPts);
                    }
                }
                else
                {
                    // 16x16 icon: Crisp radar circle + 2 axes + center dot
                    using (var ring = new Pen(Color.FromArgb(150, 52, 211, 153), 1.0f))
                    {
                        g.DrawEllipse(ring, 2, 2, 11, 11);
                    }
                    using (var xP = new Pen(Color.FromArgb(250, 248, 113, 113), 1.2f))
                    using (var yP = new Pen(Color.FromArgb(250, 52, 211, 153), 1.2f))
                    {
                        g.DrawLine(xP, 7, 7, 12, 7);
                        g.DrawLine(yP, 7, 7, 7, 2);
                    }
                    using (var dot = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(dot, 6, 6, 3, 3);
                    }
                }
            }
            return bmp;
        }

        // ─── ICON 3: PROFILE MANAGER ─────────────────────────────────────────────

        /// <summary>
        /// Consultant Profiles Manager Icon:
        /// Architectural portfolio card stack with golden gear badge and coordinate bookmark.
        /// </summary>
        public static Bitmap CreateProfileIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);

                float s = size;
                float r = s * 0.18f;

                // 1. Dark obsidian / amber slate tile
                using (var path = CreateRoundedRect(1, 1, s - 2, s - 2, r))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s),
                    Color.FromArgb(245, 45, 30, 20),   // Warm dark umber
                    Color.FromArgb(245, 23, 23, 35),   // Deep slate
                    135f))
                using (var pen = new Pen(Color.FromArgb(180, 245, 158, 11), Math.Max(1f, s * 0.04f)))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }

                if (size >= 24)
                {
                    // 2. Blueprint document cards (back card + front card)
                    float cardX = s * 0.20f, cardY = s * 0.20f, cardW = s * 0.45f, cardH = s * 0.55f;

                    // Back card
                    using (var backBrush = new SolidBrush(Color.FromArgb(120, 71, 85, 105)))
                    using (var backPen = new Pen(Color.FromArgb(180, 100, 116, 139), 1.0f))
                    {
                        g.FillRectangle(backBrush, cardX + 4, cardY - 2, cardW, cardH);
                        g.DrawRectangle(backPen, cardX + 4, cardY - 2, cardW, cardH);
                    }

                    // Front card (blueprint blue)
                    using (var frontBrush = new LinearGradientBrush(
                        new RectangleF(cardX, cardY, cardW, cardH),
                        Color.FromArgb(235, 30, 58, 138),
                        Color.FromArgb(235, 29, 78, 216), 90f))
                    using (var frontPen = new Pen(Color.FromArgb(255, 191, 219, 254), 1.2f))
                    {
                        g.FillRectangle(frontBrush, cardX, cardY, cardW, cardH);
                        g.DrawRectangle(frontPen, cardX, cardY, cardW, cardH);

                        // Architectural schematic lines on front card
                        using (var linePen = new Pen(Color.FromArgb(180, 224, 242, 254), 1.0f))
                        {
                            g.DrawLine(linePen, cardX + 4, cardY + 7, cardX + cardW - 4, cardY + 7);
                            g.DrawLine(linePen, cardX + 4, cardY + 12, cardX + cardW - 7, cardY + 12);
                            g.DrawLine(linePen, cardX + 4, cardY + 17, cardX + cardW - 5, cardY + 17);
                        }
                    }

                    // 3. Golden Precision Cog / Medal Badge on bottom right
                    float gx = s * 0.64f, gy = s * 0.64f, gr = s * 0.22f;
                    using (var cogFill = new LinearGradientBrush(
                        new RectangleF(gx - gr, gy - gr, gr * 2, gr * 2),
                        Color.FromArgb(255, 251, 191, 36),
                        Color.FromArgb(255, 217, 119, 6), 45f))
                    using (var cogBorder = new Pen(Color.FromArgb(255, 254, 243, 199), 1.2f))
                    {
                        // Draw cog teeth
                        for (int i = 0; i < 8; i++)
                        {
                            double angle = i * Math.PI / 4.0;
                            float tx = gx + (float)Math.Cos(angle) * (gr + 1.5f);
                            float ty = gy + (float)Math.Sin(angle) * (gr + 1.5f);
                            g.FillEllipse(cogFill, tx - 2f, ty - 2f, 4f, 4f);
                        }
                        g.FillEllipse(cogFill, gx - gr, gy - gr, gr * 2, gr * 2);
                        g.DrawEllipse(cogBorder, gx - gr, gy - gr, gr * 2, gr * 2);

                        // Cog center core
                        using (var coreBrush = new SolidBrush(Color.FromArgb(255, 120, 53, 15)))
                        {
                            g.FillEllipse(coreBrush, gx - gr * 0.38f, gy - gr * 0.38f, gr * 0.76f, gr * 0.76f);
                        }
                    }
                }
                else
                {
                    // 16x16 icon: Blueprint sheet + golden badge
                    using (var docFill = new SolidBrush(Color.FromArgb(220, 29, 78, 216)))
                    using (var docPen = new Pen(Color.FromArgb(255, 191, 219, 254), 1.0f))
                    {
                        g.FillRectangle(docFill, 2, 2, 8, 11);
                        g.DrawRectangle(docPen, 2, 2, 8, 11);
                    }
                    using (var badgeFill = new SolidBrush(Color.FromArgb(255, 245, 158, 11)))
                    using (var badgeBorder = new Pen(Color.White, 1.0f))
                    {
                        g.FillEllipse(badgeFill, 7, 7, 7, 7);
                        g.DrawEllipse(badgeBorder, 7, 7, 7, 7);
                    }
                }
            }
            return bmp;
        }

        // ─── ICON 4: RUN SELF TESTS ──────────────────────────────────────────────

        /// <summary>
        /// Automated Self-Test Suite & Validation Icon:
        /// High-tech quality assurance shield with radiant checkmark and coordinate calibration marks.
        /// </summary>
        public static Bitmap CreateTestsIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);

                float s = size;
                float r = s * 0.18f;

                // 1. Dark indigo/violet tech tile
                using (var path = CreateRoundedRect(1, 1, s - 2, s - 2, r))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s),
                    Color.FromArgb(245, 24, 24, 45),   // Deep indigo
                    Color.FromArgb(245, 15, 23, 42),   // Slate
                    135f))
                using (var pen = new Pen(Color.FromArgb(170, 99, 102, 241), Math.Max(1f, s * 0.04f)))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }

                if (size >= 24)
                {
                    // 2. Verified Shield Graphic
                    float sx = s * 0.22f, sy = s * 0.18f, sw = s * 0.56f, sh = s * 0.62f;
                    using (var shieldPath = CreateShieldPath(sx, sy, sw, sh))
                    using (var shieldFill = new LinearGradientBrush(
                        new RectangleF(sx, sy, sw, sh),
                        Color.FromArgb(220, 49, 46, 129),
                        Color.FromArgb(220, 67, 56, 202), 45f))
                    using (var shieldPen = new Pen(Color.FromArgb(255, 165, 180, 252), 1.4f))
                    {
                        g.FillPath(shieldFill, shieldPath);
                        g.DrawPath(shieldPen, shieldPath);
                    }

                    // 3. Bold Radiant Emerald Checkmark inside shield
                    using (var checkPen = new Pen(Color.FromArgb(255, 52, 211, 153), Math.Max(2.2f, s * 0.08f)))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        checkPen.LineJoin = LineJoin.Round;

                        PointF[] checkPts = new PointF[]
                        {
                            new PointF(s * 0.36f, s * 0.48f),
                            new PointF(s * 0.47f, s * 0.59f),
                            new PointF(s * 0.65f, s * 0.36f)
                        };
                        g.DrawLines(checkPen, checkPts);
                    }

                    // 4. Verification dot in white
                    using (var dot = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(dot, s * 0.47f - 1.5f, s * 0.59f - 1.5f, 3f, 3f);
                    }
                }
                else
                {
                    // 16x16 icon: Clean shield + checkmark
                    using (var path = CreateShieldPath(2, 1, 12, 13))
                    using (var fill = new SolidBrush(Color.FromArgb(220, 49, 46, 129)))
                    using (var border = new Pen(Color.FromArgb(255, 165, 180, 252), 1.0f))
                    {
                        g.FillPath(fill, path);
                        g.DrawPath(border, path);
                    }
                    using (var checkPen = new Pen(Color.FromArgb(255, 52, 211, 153), 1.6f))
                    {
                        g.DrawLines(checkPen, new PointF[]
                        {
                            new PointF(4, 7),
                            new PointF(7, 10),
                            new PointF(11, 4)
                        });
                    }
                }
            }
            return bmp;
        }

        // ─── ACTION ICONS FOR BUTTONS & DIALOGS ──────────────────────────────────

        /// <summary>
        /// Precision survey crosshair icon (for Pick Point buttons: Pick H1, Pick H2, etc.).
        /// </summary>
        public static Bitmap CreateTargetIcon(int size, Color accentColor)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                float c = s * 0.5f;
                float r = s * 0.38f;

                using (var ringPen = new Pen(accentColor, Math.Max(1.2f, s * 0.08f)))
                using (var crossPen = new Pen(Color.FromArgb(240, 240, 255), Math.Max(1.0f, s * 0.07f)))
                using (var dotBrush = new SolidBrush(accentColor))
                {
                    g.DrawEllipse(ringPen, c - r, c - r, r * 2, r * 2);
                    g.DrawLine(crossPen, c - r * 1.35f, c, c + r * 1.35f, c);
                    g.DrawLine(crossPen, c, c - r * 1.35f, c, c + r * 1.35f);
                    g.FillEllipse(dotBrush, c - 2f, c - 2f, 4f, 4f);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Prominent "Apply Alignment" icon with electric lock & lightning bolt symbol.
        /// </summary>
        public static Bitmap CreateApplyIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                // Lightning bolt geometry
                PointF[] bolt = new PointF[]
                {
                    new PointF(s * 0.56f, s * 0.12f),
                    new PointF(s * 0.28f, s * 0.52f),
                    new PointF(s * 0.48f, s * 0.52f),
                    new PointF(s * 0.42f, s * 0.88f),
                    new PointF(s * 0.76f, s * 0.44f),
                    new PointF(s * 0.54f, s * 0.44f)
                };
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, s, s),
                    Color.FromArgb(255, 251, 191, 36),  // Radiant gold
                    Color.FromArgb(255, 245, 158, 11),  // Amber
                    45f))
                using (var pen = new Pen(Color.White, 1.2f))
                {
                    g.FillPolygon(brush, bolt);
                    g.DrawPolygon(pen, bolt);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Interactive HTML Report document icon.
        /// </summary>
        public static Bitmap CreateReportIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                float x = s * 0.20f, y = s * 0.14f, w = s * 0.60f, h = s * 0.72f;

                // Document sheet
                using (var brush = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
                using (var pen = new Pen(Color.FromArgb(200, 14, 165, 233), 1.2f))
                {
                    g.FillRectangle(brush, x, y, w, h);
                    g.DrawRectangle(pen, x, y, w, h);
                }

                // Bar chart bars on sheet
                using (var b1 = new SolidBrush(Color.FromArgb(230, 2, 132, 199)))
                using (var b2 = new SolidBrush(Color.FromArgb(230, 16, 185, 129)))
                using (var b3 = new SolidBrush(Color.FromArgb(230, 245, 158, 11)))
                {
                    float bw = w * 0.20f;
                    g.FillRectangle(b1, x + w * 0.15f, y + h * 0.45f, bw, h * 0.42f);
                    g.FillRectangle(b2, x + w * 0.42f, y + h * 0.25f, bw, h * 0.62f);
                    g.FillRectangle(b3, x + w * 0.69f, y + h * 0.35f, bw, h * 0.52f);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Undo Alignment icon: circular counter-clockwise arrow with coral/amber hue.
        /// </summary>
        public static Bitmap CreateUndoIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                float cx = s * 0.5f, cy = s * 0.5f;
                float r = s * 0.32f;

                // Circular arc
                using (var pen = new Pen(Color.FromArgb(255, 255, 255, 255), Math.Max(2f, s * 0.11f)))
                {
                    pen.StartCap = LineCap.Round;
                    g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, 45, 240);
                }

                // Arrowhead at top left of arc
                float ax = cx - r * 0.7f, ay = cy - r * 0.7f;
                PointF[] arrow = new PointF[]
                {
                    new PointF(ax - s * 0.16f, ay - s * 0.05f),
                    new PointF(ax + s * 0.08f, ay - s * 0.16f),
                    new PointF(ax + s * 0.02f, ay + s * 0.10f)
                };
                using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                {
                    g.FillPolygon(brush, arrow);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Left navigation chevron arrow.
        /// </summary>
        public static Bitmap CreateBackIcon(int size, Color color)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                using (var pen = new Pen(color, Math.Max(2f, s * 0.13f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new PointF[]
                    {
                        new PointF(s * 0.62f, s * 0.22f),
                        new PointF(s * 0.36f, s * 0.50f),
                        new PointF(s * 0.62f, s * 0.78f)
                    });
                }
            }
            return bmp;
        }

        /// <summary>
        /// Right navigation chevron arrow.
        /// </summary>
        public static Bitmap CreateNextIcon(int size, Color color)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                using (var pen = new Pen(color, Math.Max(2f, s * 0.13f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new PointF[]
                    {
                        new PointF(s * 0.38f, s * 0.22f),
                        new PointF(s * 0.64f, s * 0.50f),
                        new PointF(s * 0.38f, s * 0.78f)
                    });
                }
            }
            return bmp;
        }

        /// <summary>
        /// Checkmark icon for finish / select / apply.
        /// </summary>
        public static Bitmap CreateCheckIcon(int size, Color color)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                using (var pen = new Pen(color, Math.Max(2f, s * 0.13f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new PointF[]
                    {
                        new PointF(s * 0.22f, s * 0.52f),
                        new PointF(s * 0.44f, s * 0.74f),
                        new PointF(s * 0.78f, s * 0.28f)
                    });
                }
            }
            return bmp;
        }

        /// <summary>
        /// Trash/delete icon in coral red.
        /// </summary>
        public static Bitmap CreateDeleteIcon(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                ConfigureGraphics(g);
                float s = size;
                float x = s * 0.26f, y = s * 0.32f, w = s * 0.48f, h = s * 0.52f;
                using (var pen = new Pen(Color.FromArgb(255, 239, 68, 68), Math.Max(1.4f, s * 0.08f)))
                {
                    // Can body
                    g.DrawRectangle(pen, x, y, w, h);
                    // Lid
                    g.DrawLine(pen, x - 2, y, x + w + 2, y);
                    // Handle
                    g.DrawLine(pen, x + w * 0.35f, y - 2, x + w * 0.65f, y - 2);
                    // Ribs
                    g.DrawLine(pen, x + w * 0.35f, y + 3, x + w * 0.35f, y + h - 3);
                    g.DrawLine(pen, x + w * 0.65f, y + 3, x + w * 0.65f, y + h - 3);
                }
            }
            return bmp;
        }

        // ─── PRIVATE DRAWING UTILITIES ───────────────────────────────────────────

        private static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(Color.Transparent);
        }

        private static GraphicsPath CreateRoundedRect(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateShieldPath(float x, float y, float width, float height)
        {
            var path = new GraphicsPath();
            float midX = x + width * 0.5f;
            float topY = y;
            float shoulderY = y + height * 0.45f;
            float bottomY = y + height;

            path.AddLine(midX, topY, x + width, topY);
            path.AddLine(x + width, topY, x + width, shoulderY);
            path.AddBezier(
                x + width, shoulderY,
                x + width, bottomY * 0.85f,
                midX + width * 0.15f, bottomY,
                midX, bottomY);
            path.AddBezier(
                midX, bottomY,
                midX - width * 0.15f, bottomY,
                x, bottomY * 0.85f,
                x, shoulderY);
            path.AddLine(x, shoulderY, x, topY);
            path.CloseFigure();
            return path;
        }
    }
}
