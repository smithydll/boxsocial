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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;

namespace BoxSocial.FrontEnd
{
    public partial class userstyle : TPage
    {
        public userstyle()
            : base()
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string profileUserName = Request.QueryString["un"];
            User profileOwner;

            try
            {

                profileOwner = new User(core, profileUserName);
            }
            catch
            {
                core.Functions.Generate404();
                return;
            }

            Response.ContentType = "text/css";
            Response.Clear();

            // don't allow to load up external stylesheets
            if (profileOwner.Style != null)
            {
                Response.Write(Regex.Replace(profileOwner.Style.RawCss, "\\@import(.+?)\\;", "", RegexOptions.IgnoreCase));
            }

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
