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
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract partial class PPage : TPage
    {
        protected string profileUserName;
        protected User profileOwner;

        public PPage()
            : base()
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }

        }

        public PPage(string templateFile)
            : base(templateFile)
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }
        }

        public User ProfileOwner
        {
            get
            {
                return profileOwner;
            }
        }

        protected void BeginProfile()
        {
            profileUserName = HttpContext.Current.Request["un"];

            try
            {
                profileOwner = new User(core, profileUserName);
            }
            catch (InvalidUserException)
            {
                Functions.Generate404();
                return;
            }

            core.PagePath = core.PagePath.Substring(profileOwner.UserName.Length + 1);
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = profileOwner.ProfileHomepage;
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Member, core.PagePath, BoxSocial.Internals.Application.GetApplications(Core, profileOwner));

            PageTitle = profileOwner.DisplayName;

            if (loggedInMember != null)
            {
                if (loggedInMember.ShowCustomStyles)
                {
                    template.Parse("USER_STYLE_SHEET", string.Format("{0}.css", profileOwner.UserName));
                }
            }
            else
            {
                template.Parse("USER_STYLE_SHEET", string.Format("{0}.css", profileOwner.UserName));
            }
            template.Parse("USER_NAME", profileOwner.UserName);
            template.Parse("USER_DISPLAY_NAME", profileOwner.DisplayName);
            template.Parse("USER_DISPLAY_NAME_OWNERSHIP", profileOwner.DisplayNameOwnership);

            if (loggedInMember != null)
            {
                if (loggedInMember.UserId == profileOwner.UserId)
                {
                    template.Parse("OWNER", "TRUE");
                    template.Parse("SELF", "TRUE");
                }
                else
                {
                    template.Parse("OWNER", "FALSE");
                    template.Parse("SELF", "FALSE");
                }
            }
            else
            {
                template.Parse("OWNER", "FALSE");
                template.Parse("SELF", "FALSE");
            }
        }
    }
}
