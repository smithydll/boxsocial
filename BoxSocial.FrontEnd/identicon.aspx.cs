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
using System.Drawing.Imaging;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.FrontEnd
{
    public partial class identicon : TPage
    {
        public identicon()
            : base()
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string profileUserName = Request.QueryString["un"];
            string mode = Request.QueryString["mode"];
            User profileOwner;

            int width = 100;

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

            try
            {

                profileOwner = new User(core, profileUserName);
            }
            catch
            {
                core.Functions.Generate404();
                return;
            }

            Response.Cache.SetCacheability(HttpCacheability.Public);
            Response.Cache.SetMaxAge(new TimeSpan(180, 0, 0, 0, 0));
            Response.Cache.SetLastModified(new DateTime(2000, 1, 1));
            Response.ContentType = "image/png";
            Response.Clear();

            byte[] userBytes = System.Text.Encoding.UTF8.GetBytes (profileUserName);
            MD5 md5 = MD5.Create();
            int hash = BitConverter.ToInt32(md5.ComputeHash(userBytes),0);
            Image image = Identicon.CreateIdenticon(hash, width, false);

            string imagePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Server.MapPath("./"), "images"), "user"), "_" + mode), string.Format("{0}.png",
                    profileUserName));
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
    }
}
