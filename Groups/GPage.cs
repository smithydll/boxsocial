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
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public abstract partial class GPage : PPage
    {
        protected string groupSlug;

        public UserGroup Group
        {
            get
            {
                return (UserGroup)primitive;
            }
        }

        public GPage()
            : base()
        {
            //page = 1;
        }

        public GPage(string templateFile)
            : base(templateFile)
        {
            //page = 1;
        }

        protected void BeginGroupPage()
        {
            groupSlug = core.Http["gn"];

            try
            {
                primitive = new UserGroup(core, groupSlug);
            }
            catch (InvalidGroupException)
            {
                core.Functions.Generate404();
                return;
            }

            if (string.IsNullOrEmpty(Group.Domain) || Linker.Domain == core.Http.Domain)
            {
                core.PagePath = core.PagePath.Substring(Group.Slug.Length + 1 + 6);
            }
            if (core.PagePath.Trim(new char[] { '/' }) == string.Empty)
            {
                core.PagePath = Group.Info.GroupHomepage;
            }
            if (core.PagePath.Trim(new char[] { '/' }) == string.Empty)
            {
                core.PagePath = "/profile";
            }

            if (Group.IsGroupMemberBanned(core.Session.LoggedInMember))
            {
                core.Functions.Generate403();
                return;
            }

            if (!Group.IsGroupMember(core.Session.LoggedInMember) && Group.GroupType == "PRIVATE")
            {
                core.Functions.Generate403();
                return;
            }

            if (loggedInMember != null)
            {
                if (loggedInMember.Info.ShowCustomStyles)
                {
                    template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", Group.Key));
                }
            }
            else
            {
                template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", Group.Key));
            }

            if (!core.PagePath.StartsWith("/account"))
            {
                BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Group, core.PagePath, BoxSocial.Internals.Application.GetApplications(core, Group));

                core.FootHooks += new Core.HookHandler(core_FootHooks);
                HookEventArgs e = new HookEventArgs(core, AppPrimitives.Group, Group);
                core.InvokeHeadHooks(e);
                core.InvokeFootHooks(e);
            }

            PageTitle = Group.DisplayName;
        }

        void core_FootHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "group_footer");

                if (e.Owner.Type == "GROUP")
                {
                    if (((UserGroup)e.Owner).IsGroupOperator(core.Session.LoggedInMember))
                    {
                        template.Parse("U_GROUP_ACCOUNT", core.Uri.AppendSid(e.Owner.AccountUriStub));
                    }
                }

                e.core.AddFootPanel(template);
            }
        }
    }

    public class ShowGPageEventArgs : ShowPPageEventArgs
    {
        public new GPage Page
        {
            get
            {
                return (GPage)page;
            }
        }

        public ShowGPageEventArgs(GPage page, long itemId)
            : base(page, itemId)
        {
        }

        public ShowGPageEventArgs(GPage page)
            : base(page)
        {
        }
    }
}
