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
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract partial class PPage : TPage
    {
        protected string profileUserName;
        protected Member profileOwner;

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

        public Member ProfileOwner
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
                profileOwner = new Member(db, profileUserName);
            }
            catch (InvalidUserException)
            {
                Functions.Generate404(Core);
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
                    template.ParseVariables("USER_STYLE_SHEET", HttpUtility.HtmlEncode(string.Format("{0}.css", profileOwner.UserName)));
                }
            }
            else
            {
                template.ParseVariables("USER_STYLE_SHEET", HttpUtility.HtmlEncode(string.Format("{0}.css", profileOwner.UserName)));
            }
            template.ParseVariables("USER_NAME", HttpUtility.HtmlEncode(profileOwner.UserName));
            template.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(profileOwner.DisplayName));
            template.ParseVariables("USER_DISPLAY_NAME_OWNERSHIP", HttpUtility.HtmlEncode(profileOwner.UserNameOwnership));

            if (loggedInMember != null)
            {
                if (loggedInMember.UserId == profileOwner.UserId)
                {
                    template.ParseVariables("OWNER", "TRUE");
                    template.ParseVariables("SELF", "TRUE");
                }
                else
                {
                    template.ParseVariables("OWNER", "FALSE");
                    template.ParseVariables("SELF", "FALSE");
                }
            }
            else
            {
                template.ParseVariables("OWNER", "FALSE");
                template.ParseVariables("SELF", "FALSE");
            }
        }
    }
}
