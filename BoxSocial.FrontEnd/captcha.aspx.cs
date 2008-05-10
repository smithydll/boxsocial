/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class captcha : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long confirmId = 0;
            try
            {
                confirmId = long.Parse(Request.QueryString["secureid"]);
            }
            catch
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE (confirm_type = 1 OR confirm_type = 2) AND confirm_id = {0} AND session_id = '{1}'",
                confirmId, Mysql.Escape(session.SessionId)));

            if (confirmTable.Rows.Count != 1)
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            string confirmString = (string)confirmTable.Rows[0]["confirm_code"];

            Response.Clear();
            Response.ContentType = "image/jpeg";

            int width = 350;
            int height = 120;

            Bitmap captchaImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(captchaImage);
            g.Clear(Color.LightYellow);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            Random rand = new Random();
            Image img;
            if (rand.NextDouble() > 0.5)
            {
                img = Image.FromFile(Server.MapPath(@"\images\captcha_1.jpg"));
            }
            else
            {
                img = Image.FromFile(Server.MapPath(@"\images\captcha_2.jpg"));
            }

            TextureBrush tbt = new TextureBrush(img, new Rectangle(new Point((int)(500 * rand.NextDouble()), (int)(500 * rand.NextDouble())), new Size(400, 200)));
            g.FillRectangle(tbt, new Rectangle(new Point(0, 0), new Size(width, height)));

            //char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            for (int i = 0; i < confirmString.Length; i++)
            {
                Matrix transformMatrix = new Matrix(1F, 0F, 0F, 1F, i * 30F + (float)(10 * rand.NextDouble()), 10F + (float)(10 * rand.NextDouble()));
                transformMatrix.Shear((float)(0.15 * rand.NextDouble()), (float)(0.15 * rand.NextDouble()));
                transformMatrix.Rotate(10F - (float)(5 * rand.NextDouble()));
                g.Transform = transformMatrix;
                //g.TransformPoints(CoordinateSpace.World, CoordinateSpace.World, new Point[] { new Point(1, 1), new Point(1, 50), new Point(50,50), new Point(50,1) });

                GraphicsPath gp = new GraphicsPath();
                //int j = (int)(rand.NextDouble() * chars.Length);
                if (rand.NextDouble() > 0.5)
                {
                    gp.AddString(confirmString[i].ToString(), new FontFamily("Times New Roman"), (int)FontStyle.Bold, 35F + (float)(20 * rand.NextDouble()), new PointF(2F, 2F), StringFormat.GenericTypographic);
                }
                else
                {
                    gp.AddString(confirmString[i].ToString(), new FontFamily("Arial"), (int)FontStyle.Bold, 35F + (float)(20 * rand.NextDouble()), new PointF(2F, 2F), StringFormat.GenericTypographic);
                }
                gp.Transform(transformMatrix);
                PointF[] pts = gp.PathPoints;

                Color[] colours = new Color[pts.Length];
                for (int j = 0; j < pts.Length; j++)
                {
                    colours[j] = captchaImage.GetPixel((int)pts[j].X, (int)pts[j].Y);
                }

                int[] mean = new int[3];
                int[] geomean = new int[3];
                for (int j = 0; j < colours.Length; j++)
                {
                    mean[0] += colours[j].R;
                    mean[1] += colours[j].G;
                    mean[2] += colours[j].B;

                    geomean[0] += (int)Math.Pow(colours[j].R, 2);
                    geomean[1] += (int)Math.Pow(colours[j].G, 2);
                    geomean[2] += (int)Math.Pow(colours[j].B, 2);
                }

                Color meanColour = Color.FromArgb(mean[0] / colours.Length,
                    mean[1] / colours.Length,
                    mean[2] / colours.Length);

                Color geomeanColour = Color.FromArgb((int)Math.Pow(geomean[0] / colours.Length, 0.5),
                    (int)Math.Pow(geomean[1] / colours.Length, 0.5),
                    (int)Math.Pow(geomean[2] / colours.Length, 0.5));

                Color inverseMean = Color.FromArgb(255 - meanColour.R,
                    255 - meanColour.G,
                    255 - meanColour.B);

                Color inverseGeomean = Color.FromArgb(255 - geomeanColour.R,
                    255 - geomeanColour.G,
                    255 - geomeanColour.B);

                TextureBrush tb = new TextureBrush(img, new Rectangle(new Point((int)(500 * rand.NextDouble()), (int)(500 * rand.NextDouble())), new Size(200, 200)));
                g.FillPolygon(tb, pts);
                Color pen = captchaImage.GetPixel((int)pts[0].X, (int)pts[0].Y);
                Color pen2 = captchaImage.GetPixel((int)pts[pts.Length / 4].X, (int)pts[pts.Length / 4].Y);
                Color pen3 = captchaImage.GetPixel((int)pts[pts.Length / 4 * 2].X, (int)pts[pts.Length / 4 * 2].Y);
                Color pen4 = captchaImage.GetPixel((int)pts[pts.Length / 4 * 3].X, (int)pts[pts.Length / 4 * 3].Y);
                Color npen = Color.FromArgb(255 - (int)Math.Pow((double)pen.R * pen2.R * pen3.R * pen4.R, 1 / 4.0), 255 - (int)Math.Pow((double)pen.G * pen2.G * pen3.G * pen4.G, 1 / 4), 255 - (int)Math.Pow((double)pen.B * pen2.B * pen3.B * pen4.B, 1 / 4.0));
                Color npen2 = Color.FromArgb(255 - (pen.R + pen2.R + pen3.R + pen4.R) / 4, 255 - (pen.G + pen2.G + pen3.G + pen4.G) / 4, 255 - (pen.B + pen2.B + pen3.B + pen4.B) / 4);
                //Color npen = Color.FromArgb((int)((255 - npen2.R) * rand.NextDouble() * 0.5), (int)((255 - npen2.G) * rand.NextDouble() * 0.5), (int)((255 - npen2.B) * rand.NextDouble() * 0.5));

                HatchBrush hb = new HatchBrush((HatchStyle)((int)(50 * rand.NextDouble())), inverseMean, meanColour);

                g.DrawPolygon(new Pen(npen2, 0.5F), pts);
                g.FillPolygon(hb, pts);

                g.TranslateTransform(2, 2);
                hb = new HatchBrush((HatchStyle)((int)(50 * rand.NextDouble())), geomeanColour, inverseGeomean);

                g.DrawPolygon(new Pen(npen, 1.5F), pts);
                g.FillPolygon(hb, pts);

                g.DrawLine(new Pen(npen, 0.5F), new PointF(pts[pts.Length / 3].X + 20 * (float)rand.NextDouble(), pts[pts.Length / 3].Y - 20 * (float)rand.NextDouble()),
                    new PointF(pts[pts.Length / 3 * 2].X - 20 * (float)rand.NextDouble(), pts[pts.Length / 3 * 2].Y + 20 * (float)rand.NextDouble()));

                g.DrawLine(new Pen(npen2, 0.5F), new PointF(pts[pts.Length / 3].X + 20 * (float)rand.NextDouble(), pts[pts.Length / 3].Y + 20 * (float)rand.NextDouble()),
                    new PointF(pts[pts.Length / 3 * 2].X - 20 * (float)rand.NextDouble(), pts[pts.Length / 3 * 2].Y - 20 * (float)rand.NextDouble()));

            }


            ImageCodecInfo encoderInfo = GetEncoderInfo("image/jpeg");
            EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = encoderParam;
            captchaImage.Save(Response.OutputStream, encoderInfo, encoderParams);
        }

        /// <summary>
        /// http://www.c-sharpcorner.com/UploadFile/scottlysle/WatermarkCS05072007024947AM/WatermarkCS.aspx
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
