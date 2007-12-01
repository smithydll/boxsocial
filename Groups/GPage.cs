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
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public abstract partial class GPage : TPage
    {

        protected string groupSlug;
        protected UserGroup thisGroup;
        protected bool isGroupMember;
        protected bool isGroupOperator;
        protected bool isGroupMemberPending;
        protected bool isGroupMemberAbsolute;

        public UserGroup ThisGroup
        {
            get
            {
                return thisGroup;
            }
        }

        public bool IsGroupMember
        {
            get
            {
                return isGroupMember;
            }
        }

        public bool IsGroupOperator
        {
            get
            {
                return isGroupOperator;
            }
        }

        public bool IsGroupMemberPending
        {
            get
            {
                return isGroupMemberPending;
            }
        }

        public bool IsGroupMemberAbsolute
        {
            get
            {
                return isGroupMemberAbsolute;
            }
        }

        public GPage()
            : base()
        {
            page = 1;
            isGroupMember = false;
            isGroupOperator = false;
            isGroupMemberPending = false;
            isGroupMemberAbsolute = false;
        }

        public GPage(string templateFile)
            : base(templateFile)
        {
            page = 1;
            isGroupMember = false;
            isGroupOperator = false;
            isGroupMemberPending = false;
            isGroupMemberAbsolute = false;
        }

        protected void BeginGroupPage()
        {
            groupSlug = HttpContext.Current.Request["gn"];

            try
            {
                thisGroup = new UserGroup(db, groupSlug);
            }
            catch (InvalidGroupException)
            {
                Functions.Generate404(Core);
                return;
            }

            core.PagePath = core.PagePath.Substring(thisGroup.Slug.Length + 1 + 6);
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = "/profile";
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Group, core.PagePath, BoxSocial.Internals.Application.GetApplications(Core, thisGroup));

            PageTitle = thisGroup.DisplayName;

            if (loggedInMember != null)
            {
                isGroupMember = thisGroup.IsGroupMember(loggedInMember);
                isGroupMemberPending = thisGroup.IsGroupMemberPending(loggedInMember);
                isGroupMemberAbsolute = (isGroupMember || isGroupMemberPending);
                isGroupOperator = thisGroup.IsGroupOperator(loggedInMember);
            }

            if (!isGroupMember && thisGroup.GroupType == "PRIVATE")
            {
                Functions.Generate403(Core);
                return;
            }

        }
    }
}