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
    public abstract partial class UPage : PPage
    {
        protected string profileUserName;
        //protected User profileOwner;

        public UPage()
            : base()
        {
        }

        public UPage(string templateFile)
            : base(templateFile)
        {
        }

        public new User User
        {
            get
            {
                return ((User)primitive);
            }
        }

        protected void BeginProfile()
        {
            profileUserName = core.Http["un"];

            try
            {
                long userId = core.PrimitiveCache.LoadUserProfile(profileUserName);
                primitive = core.PrimitiveCache[userId];
            }
            catch
            {
                core.Functions.Generate404();
                return;
            }

            if (core.PagePath.StartsWith("/memberpage.aspx"))
            {
                core.PagePath = core.PagePath.Substring(20 + User.UserName.Length + 1 + 5);
            }
            else
            {
                if (string.IsNullOrEmpty(User.Domain) || Hyperlink.Domain == core.Http.Domain)
                {
                    core.PagePath = core.PagePath.Substring(User.UserName.Length + 1 + 5);
                }
                if (core.PagePath.ToLower().StartsWith("/" + User.UserName.ToLower()))
                {
                    core.PagePath = core.PagePath.Substring(User.UserName.Length + 1);
                }
                if (core.PagePath.Trim(new char[] { '/' }) == "")
                {
                    core.PagePath = User.UserInfo.ProfileHomepage;
                }
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Member, core.PagePath, BoxSocial.Internals.Application.GetApplications(Core, User));

            HookEventArgs e = new HookEventArgs(core, AppPrimitives.Member, User);
            core.InvokeHeadHooks(e);
            core.InvokePrimitiveHeadHooks(e);

            PageTitle = User.DisplayName;

            if (!string.IsNullOrEmpty(User.UserInfo.AnalyticsCode))
            {
                template.Parse("ANALYTICS_CODE", User.UserInfo.AnalyticsCode);
            }

            if (loggedInMember != null)
            {
                if (loggedInMember.UserInfo.ShowCustomStyles)
                {
                    template.Parse("USER_STYLE_SHEET", string.Format("user/{0}.css", User.UserName));
                }
            }
            else
            {
                template.Parse("USER_STYLE_SHEET", string.Format("user/{0}.css", User.UserName));
            }
            template.Parse("USER_NAME", User.UserName);
            template.Parse("USER_DISPLAY_NAME", User.DisplayName);
            template.Parse("USER_DISPLAY_NAME_OWNERSHIP", User.DisplayNameOwnership);

            /*if (!string.IsNullOrEmpty(User.UserDomain))
            {
                template.Parse("U_HOME", User.ProfileUri);
            }*/

            if (loggedInMember != null)
            {
                if (loggedInMember.UserId == User.UserId)
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

            template.Parse("USER_ID", User.Id.ToString());
            template.Parse("USER_TYPE_ID", User.TypeId.ToString());

            core.Template.Parse("PRIMITIVE_DISPLAY_NAME", Owner.DisplayName);
            core.Template.Parse("PRIMITIVE_THUMB", Owner.Thumbnail);
            core.Template.Parse("PRIMITIVE_ICON", Owner.Icon);
            core.Template.Parse("PRIMITIVE_TILE", Owner.Tile);
            core.Template.Parse("PRIMITIVE_SQUARE", Owner.Square);
            core.Template.Parse("PRIMITIVE_COVER_PHOTO", Owner.CoverPhoto);
            core.Template.Parse("PRIMITIVE_MOBILE_COVER_PHOTO", Owner.MobileCoverPhoto);
        }
    }

    public class ShowUPageEventArgs : ShowPPageEventArgs
    {
        public new UPage Page
        {
            get
            {
                return (UPage)page;
            }
        }

        public ShowUPageEventArgs(UPage page, long itemId)
            : base(page, itemId)
        {
        }

        public ShowUPageEventArgs(UPage page)
            : base(page)
        {
        }
    }
}
