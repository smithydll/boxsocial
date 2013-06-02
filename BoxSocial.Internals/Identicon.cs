//
//  @(#) identicon.cs - Identicon generator
//
//  Copyright (c) 2007 Daniel Schick. 
// http://www.puls200.de/?p=316

#region GPL

// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace BoxSocial.Internals
{
    /// <summary>
    /// This class generates identicons in a given size using the idea of Don Park, found at:
    /// http://www.docuverse.com/blog/donpark/2007/01/18/visual-security-9-block-ip-identification
    /// Algorithm is explained here
    /// http://www.docuverse.com/blog/donpark/2007/01/19/identicon-updated-and-source-released
    /// This is a new implementation, I didn't even look at the java source code :-\
    /// </summary>
    public class Identicon
    {
        private static List<GraphicsPath> shapes = new List<GraphicsPath>(16);
        private static List<GraphicsPath> invshapes = new List<GraphicsPath>(16);
        private static List<GraphicsPath> symshapes = new List<GraphicsPath>(4);
        private static bool initialized = false;

        private static void Initialize()
        {
            for (int i = 0; i < 16; i++)
            {
                GraphicsPath gp = new GraphicsPath();
                GraphicsPath ip = new GraphicsPath();
                shapes.Add(gp);
                invshapes.Add(gp);
            }

            // initializing the 16 shapes

            #region obere Reihe

            PointF[] p0 = new PointF[4];
            p0[0].X = 0; p0[0].Y = 0;
            p0[1].X = 1; p0[1].Y = 0;
            p0[2].X = 0; p0[2].Y = 1;
            p0[3].X = 1; p0[3].Y = 1;
            shapes[0].AddPolygon(p0);

            PointF[] p1 = new PointF[3];
            p1[0].X = 0; p1[0].Y = 0;
            p1[1].X = 1; p1[1].Y = 0;
            p1[2].X = 0; p1[2].Y = 1;
            shapes[1].AddPolygon(p1);

            PointF[] p2 = new PointF[3];
            p2[0].X = 0; p2[0].Y = 1;
            p2[1].X = 0.5F; p2[1].Y = 0;
            p2[2].X = 1; p2[2].Y = 1;
            shapes[2].AddPolygon(p2);

            PointF[] p3 = new PointF[4];
            p3[0].X = 0; p3[0].Y = 0;
            p3[1].X = 0.5F; p3[1].Y = 0;
            p3[2].X = 0.5F; p3[2].Y = 1;
            p3[3].X = 0; p3[3].Y = 1;
            shapes[3].AddPolygon(p3);

            PointF[] p4 = new PointF[4];
            p4[0].X = 0.5F; p4[0].Y = 0;
            p4[1].X = 1; p4[1].Y = 0.5F;
            p4[2].X = 0.5F; p4[2].Y = 1;
            p4[3].X = 0; p4[3].Y = 0.5F;
            shapes[4].AddPolygon(p4);

            PointF[] p5 = new PointF[4];
            p5[0].X = 0; p5[0].Y = 0;
            p5[1].X = 1; p5[1].Y = 0.5F;
            p5[2].X = 1; p5[2].Y = 1;
            p5[3].X = 0.5F; p5[3].Y = 1;
            shapes[5].AddPolygon(p5);

            PointF[] p61 = new PointF[3];
            p61[0].X = 0.5F; p61[0].Y = 0;
            p61[1].X = 0.75F; p61[1].Y = 0.5F;
            p61[2].X = 0.25F; p61[2].Y = 0.5F;
            shapes[6].AddPolygon(p61);
            PointF[] p62 = new PointF[3];
            p62[0].X = 0.75F; p62[0].Y = 0.5F;
            p62[1].X = 1; p62[1].Y = 1;
            p62[2].X = 0.5F; p62[2].Y = 1;
            shapes[6].AddPolygon(p62);
            PointF[] p63 = new PointF[3];
            p63[0].X = 0.25F; p63[0].Y = 0.5F;
            p63[1].X = 0.5F; p63[1].Y = 1;
            p63[2].X = 0; p63[2].Y = 1;
            shapes[6].AddPolygon(p63);

            PointF[] p7 = new PointF[3];
            p7[0].X = 0; p7[0].Y = 0;
            p7[1].X = 1; p7[1].Y = 0.5F;
            p7[2].X = 0.5F; p7[2].Y = 1;

            #endregion

            #region untere Reihe

            PointF[] p8 = new PointF[4];
            p8[0].X = 0.25F; p8[0].Y = 0.25F;
            p8[1].X = 0.75F; p8[1].Y = 0.25F;
            p8[2].X = 0.75F; p8[2].Y = 0.75F;
            p8[3].X = 0.25F; p8[3].Y = 0.75F;
            shapes[8].AddPolygon(p8);

            PointF[] p91 = new PointF[3];
            p91[0].X = 0.5F; p91[0].Y = 0;
            p91[1].X = 1; p91[1].Y = 0;
            p91[2].X = 0.5F; p91[2].Y = 0.5F;
            shapes[9].AddPolygon(p91);
            PointF[] p92 = new PointF[3];
            p92[0].X = 0; p92[0].Y = 0.5F;
            p92[1].X = 0.5F; p92[1].Y = 0.5F;
            p92[2].X = 0; p92[2].Y = 1;
            shapes[9].AddPolygon(p92);

            PointF[] p10 = new PointF[4];
            p10[0].X = 0; p10[0].Y = 0;
            p10[1].X = 0.5F; p10[1].Y = 0;
            p10[2].X = 0.5F; p10[2].Y = 0.5F;
            p10[3].X = 0; p10[3].Y = 0.5F;
            shapes[10].AddPolygon(p10);

            PointF[] p11 = new PointF[3];
            p11[0].X = 0; p11[0].Y = 0.5F;
            p11[1].X = 1; p11[1].Y = 0.5F;
            p11[2].X = 0.5F; p11[2].Y = 1;
            shapes[11].AddPolygon(p11);

            PointF[] p12 = new PointF[3];
            p12[0].X = 0; p12[0].Y = 1;
            p12[1].X = 0.5F; p12[1].Y = 0.5F;
            p12[2].X = 1; p12[2].Y = 1;
            shapes[12].AddPolygon(p12);

            PointF[] p13 = new PointF[3];
            p13[0].X = 0.5F; p13[0].Y = 0;
            p13[1].X = 0.5F; p13[1].Y = 0.5F;
            p13[2].X = 0; p13[2].Y = 0.5F;
            shapes[13].AddPolygon(p13);

            PointF[] p14 = new PointF[3];
            p14[0].X = 0; p14[0].Y = 0;
            p14[1].X = 0.5F; p14[1].Y = 0;
            p14[2].X = 0; p14[2].Y = 0.5F;
            shapes[14].AddPolygon(p14);

            #endregion

            symshapes.Add(shapes[0]);
            symshapes.Add(shapes[4]);
            symshapes.Add(shapes[8]);
            symshapes.Add(shapes[15]);

            initialized = true;
        }

        /// <summary>
        /// creates a new Identicon
        /// </summary>
        /// <param name="source">source value, e.g. a hash created from a username, an ip address,..</param>
        /// <param name="size">side length</param>
        /// <returns>a 24 bpp bitmap</returns>
        public static Image CreateIdenticon(int source, int size, bool outline)
        {
            if (size == 0) return null;
            if (!initialized) Initialize();
            Bitmap bi = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bi);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.HighQuality; // try different settings

            int centerindex = source & 3; // 2 lowest bits
            int sideindex = (source >> 2) & 15; // next 4 bits for side shapes
            int cornerindex = (source >> 6) & 15; // next 4 for corners
            int siderot = (source >> 10) & 3; // 2 bits for side offset rotation
            int cornerrot = (source >> 12) & 3; // 2 bits for corner offset rotation

            float shapesize = (float)size / 3.0F; // adjust the shape size to the target bitmap size
            Matrix sizeMatrix = new Matrix();
            sizeMatrix.Scale(shapesize, shapesize);
            Matrix tlrMatrix = new Matrix(); // translates 1 unit to the right
            tlrMatrix.Translate(shapesize, 0);
            Matrix tldMatrix = new Matrix(); // translates 1 unit down
            tldMatrix.Translate(0, shapesize);

            // inversion per shape

            // calculate color
            int red = (source >> 14) & 31;
            int green = (source >> 19) & 31;
            int blue = (source >> 24) & 31;
            Color shapecolor = Color.FromArgb(red * 8, green * 8, blue * 8);

            // remaining bits to decide shape flipping
            bool flipcenter = ((source >> 29) & 1) == 1;
            bool flipcorner = ((source >> 30) & 1) == 1;
            bool flipsides = ((source >> 31) & 1) == 1;

            using (SolidBrush sb = new SolidBrush(shapecolor), wb = new SolidBrush(Color.White))
            {
                using (Pen p = new Pen(Color.Black))
                {

                    #region Transform and move shapes into position

                    // comment out the "DrawPath" statements if you don't want an outline

                    // center
                    GraphicsPath g5 = symshapes[centerindex].Clone() as GraphicsPath;
                    g5.Transform(sizeMatrix);
                    g5.Transform(tlrMatrix);
                    g5.Transform(tldMatrix);
                    if (flipcenter)
                    {
                        g.FillRectangle(sb, shapesize, shapesize, shapesize, shapesize);
                        g.FillPath(wb, g5);
                    }
                    else g.FillPath(sb, g5);
                    if (outline) g.DrawPath(p, g5);

                    // corner top left
                    GraphicsPath g1 = shapes[cornerindex].Clone() as GraphicsPath;
                    RotatePath90(g1, cornerrot);
                    g1.Transform(sizeMatrix);
                    if (flipcorner)
                    {
                        g.FillRectangle(sb, 0, 0, shapesize, shapesize);
                        g.FillPath(wb, g1);
                    }
                    else g.FillPath(sb, g1);
                    if (outline) g.DrawPath(p, g1);

                    // corner bottom left
                    GraphicsPath g7 = shapes[cornerindex].Clone() as GraphicsPath;
                    RotatePath90(g7, cornerrot + 1);
                    g7.Transform(sizeMatrix);
                    g7.Transform(tldMatrix);
                    g7.Transform(tldMatrix);
                    if (flipcorner)
                    {
                        g.FillRectangle(sb, 0, 2 * shapesize, shapesize, shapesize);
                        g.FillPath(wb, g7);
                    }
                    else g.FillPath(sb, g7);
                    if (outline) g.DrawPath(p, g7);

                    // corner bottom right
                    GraphicsPath g9 = shapes[cornerindex].Clone() as GraphicsPath;
                    RotatePath90(g9, cornerrot + 2);
                    g9.Transform(sizeMatrix);
                    g9.Transform(tldMatrix);
                    g9.Transform(tldMatrix);
                    g9.Transform(tlrMatrix);
                    g9.Transform(tlrMatrix);
                    if (flipcorner)
                    {
                        g.FillRectangle(sb, 2 * shapesize, 2 * shapesize, shapesize, shapesize);
                        g.FillPath(wb, g9);
                    }
                    else g.FillPath(sb, g9);
                    if (outline) g.DrawPath(p, g9);

                    // corner top right
                    GraphicsPath g3 = shapes[cornerindex].Clone() as GraphicsPath;
                    RotatePath90(g3, cornerrot + 3);
                    g3.Transform(sizeMatrix);
                    g3.Transform(tlrMatrix);
                    g3.Transform(tlrMatrix);
                    if (flipcorner)
                    {
                        g.FillRectangle(sb, 2 * shapesize, 0, shapesize, shapesize);
                        g.FillPath(wb, g3);
                    }
                    else g.FillPath(sb, g3);
                    if (outline) g.DrawPath(p, g3);

                    // top side
                    GraphicsPath g2 = shapes[sideindex].Clone() as GraphicsPath;
                    RotatePath90(g2, siderot);
                    g2.Transform(sizeMatrix);
                    g2.Transform(tlrMatrix);
                    if (flipsides)
                    {
                        g.FillRectangle(sb, shapesize, 0, shapesize, shapesize);
                        g.FillPath(wb, g2);
                    }
                    else g.FillPath(sb, g2);
                    if (outline) g.DrawPath(p, g2);

                    // left side
                    GraphicsPath g4 = shapes[sideindex].Clone() as GraphicsPath;
                    RotatePath90(g4, siderot + 1);
                    g4.Transform(sizeMatrix);
                    g4.Transform(tldMatrix);
                    if (flipsides)
                    {
                        g.FillRectangle(sb, 0, shapesize, shapesize, shapesize);
                        g.FillPath(wb, g4);
                    }
                    else g.FillPath(sb, g4);
                    if (outline) g.DrawPath(p, g4);

                    // bottom side
                    GraphicsPath g8 = shapes[sideindex].Clone() as GraphicsPath;
                    RotatePath90(g8, siderot + 2);
                    g8.Transform(sizeMatrix);
                    g8.Transform(tlrMatrix);
                    g8.Transform(tldMatrix);
                    g8.Transform(tldMatrix);
                    if (flipsides)
                    {
                        g.FillRectangle(sb, shapesize, 2 * shapesize, shapesize, shapesize);
                        g.FillPath(wb, g8);
                    }
                    else g.FillPath(sb, g8);
                    if (outline) g.DrawPath(p, g8);

                    // right side
                    GraphicsPath g6 = shapes[sideindex].Clone() as GraphicsPath;
                    RotatePath90(g6, siderot + 3);
                    g6.Transform(sizeMatrix);
                    g6.Transform(tlrMatrix);
                    g6.Transform(tlrMatrix);
                    g6.Transform(tldMatrix);
                    if (flipsides)
                    {
                        g.FillRectangle(sb, 2 * shapesize, shapesize, shapesize, shapesize);
                        g.FillPath(wb, g6);
                    }
                    else g.FillPath(sb, g6);
                    if (outline) g.DrawPath(p, g6);

                    #endregion

                }
            }
            return bi;
        }

        /// <summary>
        /// helper func to rotate input matrix
        /// </summary>
        private static void RotatePath90(GraphicsPath gp, int times)
        {
            Matrix rotMatrix = new Matrix();
            rotMatrix.RotateAt(90.0F, new PointF(0.5F, 0.5f));

            times = times % 4;
            for (int i = 0; i < times; i++)
                gp.Transform(rotMatrix);
        }

    }
}

// eof "identicon.cs"
