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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;

namespace BoxSocial.FrontEnd
{
    public partial class userstyle : TPage
    {
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
                Functions.Generate404();
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
            Response.End();
        }
    }
}
