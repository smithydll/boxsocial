﻿/*
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
using BoxSocial.Musician;

namespace BoxSocial.FrontEnd
{
    public partial class musicstyle : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string groupName = Request.QueryString["mn"];
            Musician.Musician profileOwner;

            try
            {

                profileOwner = new Musician.Musician(core, groupName);
            }
            catch
            {
                Functions.Generate404();
                return;
            }

            Response.ContentType = "text/css";
            Response.Clear();

            // don't allow to load up external stylesheets
            // TODO:
            //Response.Write(Regex.Replace(profileOwner.Style, "\\@import(.+?)\\;", "", RegexOptions.IgnoreCase));

            if (db != null)
            {
                db.CloseConnection();
            }

            core.prose.Close();
            core.Dispose();
            core = null;
            Bbcode.Dispose();
            Linker.Core = null;

            Response.End();
        }
    }
}
