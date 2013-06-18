/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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
using System.Security.Cryptography;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class captcha : TPage
    {
        public captcha()
            : base()
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long confirmId = 0;
            try
            {
                confirmId = long.Parse(Request.QueryString["secureid"]);
            }
            catch
            {
                core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE (confirm_type = 1 OR confirm_type = 2 OR confirm_type = 3) AND confirm_id = {0} AND session_id = '{1}'",
                confirmId, Mysql.Escape(session.SessionId)));

            if (confirmTable.Rows.Count != 1)
            {
                core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            string confirmString = (string)confirmTable.Rows[0]["confirm_code"];

            Response.Clear();
            Response.ContentType = "image/jpeg";

            int width = 350;
            int height = 120;

            Bitmap captchaImage = GenerateCaptcha(width, height, confirmString);

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

        private Bitmap GenerateCaptcha(int width, int height, string confirmString)
        {
            int chars = confirmString.Length;
            int letterWidth = (int)(width / (double)chars);

            Bitmap captchaImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(captchaImage);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            PaintBackground(g, width, height, chars);

            for (int i = 0; i < chars; i++)
            {
                PaintLetter(g, confirmString[i], i * letterWidth, letterWidth, height);
            }

            PaintForeground(g, width, height, chars);

            return captchaImage;
        }

        private void PaintForeground(Graphics g, int width, int height, int letters)
        {
            Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));

            int specs = rand.Next(100, 300);

            for (int i = 0; i < specs; i++)
            {
                Pen specPen = new Pen(RandomBackgroundColour());

                int x = rand.Next(1, width);
                int y = rand.Next(1, height);

                int left = rand.Next(0, 3);

                if (left == 0)
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x + 3, y + 3));
                    g.DrawLine(specPen, new Point(x + 3, y), new Point(x, y + 3));
                }
                else if (left == 1)
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x + 1, y + 1));
                    g.DrawLine(specPen, new Point(x + 1, y + 1), new Point(x, y + 2));
                }
                else
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x - 1, y + 1));
                    g.DrawLine(specPen, new Point(x - 1, y + 1), new Point(x, y + 2));
                }
            }

            for (int i = 0; i < specs; i++)
            {
                Pen specPen = new Pen(RandomClearColour(), 3F);

                int x = rand.Next(1, width);
                int y = rand.Next(1, height);

                int left = rand.Next(0, 3);

                if (left == 0)
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x + 3, y + 3));
                    g.DrawLine(specPen, new Point(x + 3, y), new Point(x, y + 3));
                }
                else if (left == 1)
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x + 1, y + 1));
                    g.DrawLine(specPen, new Point(x + 1, y + 1), new Point(x, y + 2));
                }
                else
                {
                    g.DrawLine(specPen, new Point(x, y), new Point(x - 1, y + 1));
                    g.DrawLine(specPen, new Point(x - 1, y + 1), new Point(x, y + 2));
                }
            }
        }

        private void PaintBackground(Graphics g, int width, int height, int letters)
        {
            int blockWidth = (int)((width - 10 * letters) / (5.0 * letters));
            int blocksW = width / blockWidth;
            int blocksH = height / blockWidth;
            Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            g.Clear(RandomClearColour());

            int firstOffsetX = (int)(BoxSocial.Internals.SessionState.GetDoubleRNG(rng) * 5);
            int offsetY = 0;

            for (int i = 0; i < (blocksW + blocksH + 2); i++)
            {
                Pen pen = new Pen(RandomBackgroundColour(), 2.5F);
                offsetY += rand.Next(-2, 2);

                for (int j = 0; j < blocksH + 2; j++)
                {
                    g.DrawLine(pen, new Point(i * blockWidth - firstOffsetX, j * blockWidth + offsetY),
                        new Point(i * blockWidth - firstOffsetX, j * blockWidth + offsetY + blockWidth));

                    g.DrawLine(pen, new Point(i * blockWidth - firstOffsetX, j * blockWidth + offsetY + blockWidth),
                        new Point(i * blockWidth - firstOffsetX + blockWidth, j * blockWidth + offsetY + blockWidth));
                }
            }
        }

        private void PaintLetter(Graphics g, char letter, int left, int width, int height)
        {
            int blockHeight = (int)((height * 0.75) / 7.0);
            int blockWidth = (int)((width * 0.75) / 5.0);
            blockWidth = blockHeight = Math.Min(blockWidth, blockHeight);
            int offsetX = (width - 5 * blockWidth) / 2;
            int offsetY = (height - 7 * blockHeight) / 2;
            int halfWidth = (int)(blockWidth * 0.5);
            
            byte[] b = GetLetter(letter);

            Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));

            int upDown = rand.Next(-1 * offsetY / 2, offsetY / 2);

            Pen letterPen = new Pen(RandomColour());

            int chanelChange = rand.Next(0, 3);
            int rleft = rand.Next(0, 2);

            for (int i = 0; i < 7; i++)
            {
                byte blue = letterPen.Color.B;
                byte green = letterPen.Color.G;
                byte red = letterPen.Color.R;

                switch (chanelChange)
                {
                    case 0:
                        blue = (byte)(letterPen.Color.B * rand.NextDouble());
                        break;
                    case 1:

                        green = (byte)(letterPen.Color.G * rand.NextDouble());
                        break;
                    case 2:
                        red = (byte)(letterPen.Color.R * rand.NextDouble());
                        break;
                }

                letterPen = new Pen(Color.FromArgb(red, green, blue));

                for (int j = 0; j < 5; j++)
                {
                    if (b[i * 5 + j] > 0)
                    {
                        int widthVariation = rand.Next(-2, 2);
                        int heightVariation = rand.Next(-2, 2);
                        int leftVariation = rand.Next(-2, 2);
                        int topVariation = rand.Next(-2, 2);
                        //g.DrawRectangle(new Pen(RandomColour()), new Rectangle(offsetX + left + blockWidth * j + leftVariation, offsetY + blockHeight * i + upDown + topVariation, blockWidth + widthVariation, blockHeight + heightVariation));

                        Point[] points = new Point[4];

                        if (i % 2 == rleft)
                        {
                            points[0] = new Point(offsetX + left + blockWidth * j + leftVariation + halfWidth, offsetY + blockHeight * i + upDown + topVariation);
                            points[1] = new Point(offsetX + left + blockWidth * j + leftVariation + halfWidth + blockWidth + widthVariation, offsetY + blockHeight * i + upDown + topVariation);
                            points[2] = new Point(offsetX + left + blockWidth * j + leftVariation + blockWidth + widthVariation, offsetY + blockHeight * i + upDown + topVariation + blockHeight + heightVariation);
                            points[3] = new Point(offsetX + left + blockWidth * j + leftVariation, offsetY + blockHeight * i + upDown + topVariation + blockHeight + heightVariation);
                        }
                        else
                        {
                            points[0] = new Point(offsetX + left + blockWidth * j + leftVariation, offsetY + blockHeight * i + upDown + topVariation);
                            points[1] = new Point(offsetX + left + blockWidth * j + leftVariation + blockWidth + widthVariation, offsetY + blockHeight * i + upDown + topVariation);
                            points[2] = new Point(offsetX + left + blockWidth * j + leftVariation + halfWidth + blockWidth + widthVariation, offsetY + blockHeight * i + upDown + topVariation + blockHeight + heightVariation);
                            points[3] = new Point(offsetX + left + blockWidth * j + leftVariation + halfWidth, offsetY + blockHeight * i + upDown + topVariation + blockHeight + heightVariation);
                        }

                        g.DrawPolygon(letterPen, points);
                    }
                }
            }
        }

        private byte[] GetLetter(char letter)
        {
            byte[] b = null;

            switch (letter)
            {
                case '0':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 1, 1,
                                    1, 0, 1, 0, 1,
                                    1, 1, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case '1':
                    b = new byte[] {0, 0, 1, 0, 0,
                                    0, 1, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 1, 1, 1, 0};
                    break;
                case '2':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 1, 0,
                                    0, 0, 1, 0, 0,
                                    0, 1, 0, 0, 0,
                                    1, 1, 1, 1, 1};
                    break;
                case '3':
                    b = new byte[] {1, 1, 1, 1, 0,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 1, 1, 0,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0};
                    break;
                case '4':
                    b = new byte[] {1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 1, 0, 0,
                                    1, 0, 1, 0, 0,
                                    1, 1, 1, 1, 1,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0};
                    break;
                case '5':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 0,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0};
                    break;
                case '6':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case '7':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    0, 1, 1, 1, 1,
                                    0, 0, 0, 1, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0};
                    break;
                case '8':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case '9':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 1, 0};
                    break;
                case 'A':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1};
                    break;
                case 'B':
                    b = new byte[] {1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0};
                    break;
                case 'C':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    0, 1, 1, 1, 0};
                    break;
                case 'D':
                    b = new byte[] {1, 1, 1, 0, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 1, 0,
                                    1, 1, 1, 0, 0};
                    break;
                case 'E':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 1};
                    break;
                case 'F':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0};
                    break;
                case 'G':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 1, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case 'H':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1};
                    break;
                case 'I':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    1, 1, 1, 1, 1};
                    break;
                case 'J':
                    b = new byte[] {1, 1, 1, 1, 0,
                                    0, 0, 0, 1, 0,
                                    0, 0, 0, 1, 0,
                                    0, 0, 0, 1, 0,
                                    0, 0, 0, 1, 0,
                                    0, 0, 0, 1, 0,
                                    1, 1, 1, 0, 0};
                    break;
                case 'K':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 1, 0,
                                    1, 1, 1, 0, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1};
                    break;
                case 'L':
                    b = new byte[] {1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 1};
                    break;
                case 'M':
                    b = new byte[] {0, 1, 0, 1, 0,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1};
                    break;
                case 'N':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 1, 0, 0, 1,
                                    1, 1, 0, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 0, 1, 1,
                                    1, 0, 0, 1, 1,
                                    1, 0, 0, 0, 1};
                    break;
                case 'O':
                    b = new byte[] {0, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case 'P':
                    b = new byte[] {1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0};
                    break;
                case 'Q':
                    b = new byte[] {0, 1, 1, 0, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 1, 1, 0,
                                    0, 1, 1, 1, 0,
                                    0, 0, 0, 0, 1};
                    break;
                case 'R':
                    b = new byte[] {1, 1, 1, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0,
                                    1, 0, 1, 0, 0,
                                    1, 0, 0, 1, 0,
                                    1, 0, 0, 0, 1};
                    break;
                case 'S':
                    b = new byte[] {0, 1, 1, 1, 1,
                                    1, 0, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    0, 1, 1, 1, 0,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 0, 1,
                                    1, 1, 1, 1, 0};
                    break;
                case 'T':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    1, 0, 1, 0, 1,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0,
                                    0, 0, 1, 0, 0};
                    break;
                case 'U':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 1, 1, 0};
                    break;
                case 'V':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 0, 1, 0,
                                    0, 1, 0, 1, 0,
                                    0, 1, 0, 1, 0,
                                    0, 0, 1, 0, 0};
                    break;
                case 'W':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    1, 0, 1, 0, 1,
                                    0, 1, 0, 1, 0};
                    break;
                case 'X':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1,
                                    0, 1, 0, 1, 0,
                                    0, 0, 1, 0, 0,
                                    0, 1, 0, 1, 0,
                                    1, 0, 0, 0, 1,
                                    1, 0, 0, 0, 1};
                    break;
                case 'Y':
                    b = new byte[] {1, 0, 0, 0, 1,
                                    1, 1, 0, 0, 1,
                                    0, 1, 0, 1, 0,
                                    0, 0, 1, 1, 0,
                                    0, 0, 1, 0, 0,
                                    0, 1, 0, 0, 0,
                                    1, 0, 0, 0, 0};
                    break;
                case 'Z':
                    b = new byte[] {1, 1, 1, 1, 1,
                                    0, 0, 0, 0, 1,
                                    0, 0, 0, 1, 0,
                                    0, 0, 1, 0, 0,
                                    0, 1, 0, 0, 0,
                                    1, 0, 0, 0, 0,
                                    1, 1, 1, 1, 1};
                    break;
            }

            return b;
        }

        private Color RandomColour()
        {
            //Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            double d = BoxSocial.Internals.SessionState.GetDoubleRNG(rng);

            return Display.HlsToRgb(d * 360, 1.0, 0.45);
        }

        private Color RandomBackgroundColour()
        {
            //Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            double d = BoxSocial.Internals.SessionState.GetDoubleRNG(rng);

            return Display.HlsToRgb(d * 360, 0.75, 0.90);
        }

        private Color RandomClearColour()
        {
            //Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            double d = BoxSocial.Internals.SessionState.GetDoubleRNG(rng);

            return Display.HlsToRgb(d * 360, 0.95, 0.98);
        }
    }
}
