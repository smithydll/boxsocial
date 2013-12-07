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
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.FrontEnd
{
    public partial class identicon : TPage
    {
        HttpContext httpContext;
        public identicon()
            : base()
        {
            httpContext = HttpContext.Current;
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string profileUserName = core.Http["un"];
            string groupUserName = core.Http["gn"];
            string mode = core.Http["mode"];
            bool retina = core.Http["retina"] == "true";
            User profileOwner = null;
            UserGroup thisGroup = null;

            int width = 100;

            if (retina)
            {
                switch (mode)
                {
                    case "icon":
                        width = 100;
                        break;
                    case "tile":
                        width = 200;
                        break;
                    case "square":
                    case "high":
                        width = 400;
                        break;
                    case "tiny":
                        width = 160;
                        break;
                    case "thumb":
                        width = 320;
                        break;
                    case "mobile":
                        width = 640;
                        break;
                    case "display":
                        width = 1280;
                        break;
                    case "full":
                    case "ultra":
                        width = 2560;
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case "icon":
                        width = 50;
                        break;
                    case "tile":
                        width = 100;
                        break;
                    case "square":
                        width = 200;
                        break;
                    case "high":
                        width = 400;
                        break;
                    case "tiny":
                        width = 80;
                        break;
                    case "thumb":
                        width = 160;
                        break;
                    case "mobile":
                        width = 320;
                        break;
                    case "display":
                        width = 640;
                        break;
                    case "full":
                        width = 1280;
                        break;
                    case "ultra":
                        width = 2560;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(profileUserName))
            {
                try
                {

                    profileOwner = new User(core, profileUserName);
                }
                catch
                {
                    core.Functions.Generate404();
                    return;
                }

                if (profileOwner != null)
                {
                    if (profileOwner.UserInfo.DisplayPictureId > 0)
                    {
                        httpContext.Response.Redirect(string.Format("/memberpage.aspx?un={0}&path=/images/_{1}/_{0}.png", profileUserName, mode), true);
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(groupUserName))
            {
                try
                {

                    thisGroup = new UserGroup(core, groupUserName);
                }
                catch
                {
                    core.Functions.Generate404();
                    return;
                }

                if (thisGroup != null)
                {
                    if (thisGroup.GroupInfo.DisplayPictureId > 0)
                    {
                        httpContext.Response.Redirect(string.Format("/grouppage.aspx?gn={0}&path=/images/_{1}/_{0}.png", groupUserName, mode), true);
                        return;
                    }
                }
            }

            Response.Cache.SetCacheability(HttpCacheability.Public);
            Response.Cache.SetMaxAge(new TimeSpan(10, 0, 0));
            Response.Cache.SetLastModified(DateTime.Now.Subtract(new TimeSpan(10, 0, 0)));
            Response.ContentType = "image/png";
            Response.Clear();

            Image image = null;

            string imagePath = string.Empty;

            if (!string.IsNullOrEmpty(profileUserName))
            {
                byte[] userBytes = System.Text.Encoding.UTF8.GetBytes(profileUserName);
                MD5 md5 = MD5.Create();
                int hash = BitConverter.ToInt32(md5.ComputeHash(userBytes), 0);

                image = Identicon.CreateIdenticon(hash, width, false);
                if (retina)
                {
                    imagePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Server.MapPath("./"), "images"), "user"), "_" + mode), string.Format("{0}@2x.png",
                        profileUserName));
                }
                else
                {
                    imagePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Server.MapPath("./"), "images"), "user"), "_" + mode), string.Format("{0}.png",
                        profileUserName));
                }
            }

            if (!string.IsNullOrEmpty(groupUserName))
            {
                byte[] userBytes = System.Text.Encoding.UTF8.GetBytes(groupUserName);
                MD5 md5 = MD5.Create();
                int hash = BitConverter.ToInt32(md5.ComputeHash(userBytes), 0);

                char letter = thisGroup.DisplayName.ToUpper()[0];
                image = CreateIcon(letter, width, false);
                if (retina)
                {
                    imagePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Server.MapPath("./"), "images"), "group"), "_" + mode), string.Format("{0}@2x.png",
                        groupUserName));
                }
                else
                {
                    imagePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Server.MapPath("./"), "images"), "group"), "_" + mode), string.Format("{0}.png",
                        groupUserName));
                }
            }

            try
            {
                FileStream newFileStream = new FileStream(imagePath, FileMode.Create);
                image.Save(newFileStream, ImageFormat.Png);
                newFileStream.Close();
            }
            catch { }

            MemoryStream newStream = new MemoryStream();
            image.Save(newStream, ImageFormat.Png);

            core.Http.WriteStream(newStream);

            if (db != null)
            {
                db.CloseConnection();
            }

            core.Prose.Close();
            //core.Dispose();
            //core = null;

            Response.End();
        }

        Image CreateIcon(char letter, int width, bool outline)
        {
            Image image = new Bitmap(width, width);

            Graphics g = Graphics.FromImage(image);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int blockHeight = (int)((width * 0.6381) / 7.0);
            int blockWidth = (int)((width * 0.5522) / 5.0);
            int halfWidth = (int)(blockWidth * 0.5);

            int offsetY = (int)((width * (1 - 0.5522) + blockHeight / 2.0) / 2.0);
            int offsetX = (int)((width * (1 - 0.6381) + blockWidth / 2.0) / 2.0);

            Random rand = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
            int penColor = rand.Next(0, 6);
            SolidBrush letterPen = null;
            Color backgroundColour = Color.White;

            switch (penColor)
            {
                case 0:
                    letterPen = new SolidBrush(Color.FromArgb(255, 85, 85));
                    backgroundColour = Color.FromArgb(255, 205, 205); // 0
                    break;
                case 1:
                    letterPen = new SolidBrush(Color.FromArgb(255, 153, 85));
                    backgroundColour = Color.FromArgb(255, 225, 205); // 17
                    break;
                case 2:
                    letterPen = new SolidBrush(Color.FromArgb(253, 255, 85));
                    backgroundColour = Color.FromArgb(254, 255, 205); // 43
                    break;
                case 3:
                    letterPen = new SolidBrush(Color.FromArgb(85, 255, 93));
                    backgroundColour = Color.FromArgb(205, 255, 207); // 87
                    break;
                case 4:
                    letterPen = new SolidBrush(Color.FromArgb(85, 217, 255));
                    backgroundColour = Color.FromArgb(205, 244, 255); // 137
                    break;
                case 5:
                    letterPen = new SolidBrush(Color.FromArgb(233, 85, 255));
                    backgroundColour = Color.FromArgb(249, 205, 255); // 207
                    break;
            }

            g.Clear(backgroundColour);

            byte[] b = GetLetter(letter);

            int rleft = rand.Next(0, 2);

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (b[i * 5 + j] > 0)
                    {
                        Point[] points = new Point[4];

                        if (i % 2 == rleft)
                        {
                            points[0] = new Point(offsetX + blockWidth * j + halfWidth, offsetY + blockHeight * i);
                            points[1] = new Point(offsetX + blockWidth * j + halfWidth + blockWidth, offsetY + blockHeight * i);
                            points[2] = new Point(offsetX + blockWidth * j + blockWidth, offsetY + blockHeight * i + blockHeight);
                            points[3] = new Point(offsetX + blockWidth * j, offsetY + blockHeight * i + blockHeight);
                        }
                        else
                        {
                            points[0] = new Point(offsetX + blockWidth * j, offsetY + blockHeight * i);
                            points[1] = new Point(offsetX + blockWidth * j + blockWidth, offsetY + blockHeight * i);
                            points[2] = new Point(offsetX + blockWidth * j + halfWidth + blockWidth, offsetY + blockHeight * i + blockHeight);
                            points[3] = new Point(offsetX + blockWidth * j + halfWidth, offsetY + blockHeight * i + blockHeight);
                        }

                        g.FillPolygon(letterPen, points);
                    }
                }
            }

            return image;
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
    }
}
