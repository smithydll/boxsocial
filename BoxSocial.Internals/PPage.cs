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

        public static long CreatePage(Core core, Member owner, string title, string slug, string parentPath, string pageBody, long parent, string status, bool pageListOnly, ushort permissions, byte license)
        {
            byte listOnly = 0;
            long pageId = 0;
            ushort order = 0;
            ushort oldOrder = 0;

            if (pageListOnly)
            {
                listOnly = 1;
            }

            if (!pageListOnly)
            {
                if (string.IsNullOrEmpty(slug))
                {
                    slug = title;
                }

                // normalise slug if it has been fiddeled with
                slug = slug.ToLower().Normalize(NormalizationForm.FormD);
                string normalisedSlug = "";

                for (int i = 0; i < slug.Length; i++)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                    {
                        normalisedSlug += slug[i];
                    }
                }
                slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");
            }

            // validate title;

            if (string.IsNullOrEmpty(title))
            {
                //template.ParseVariables("ERROR", "You must give the page a title.");
                return -1;
            }

            if (string.IsNullOrEmpty(slug))
            {
                //template.ParseVariables("ERROR", "You must specify a page slug.");
                return -1;
            }

            if ((!Functions.CheckPageNameValid(slug)) && parent == 0)
            {
                //template.ParseVariables("ERROR", "You must give your page a different name.");
                return -1;
            }

            if (string.IsNullOrEmpty(pageBody) && !pageListOnly)
            {
                //template.ParseVariables("ERROR", "You cannot save empty pages. You must post some content.");
                return -1;
            }

            DataTable pagesTable = core.db.SelectQuery(string.Format("SELECT page_title FROM user_pages WHERE user_id = {0} AND page_id <> {1} AND page_slug = '{2}' AND page_parent_id = {3}",
                owner.UserId, pageId, Mysql.Escape(slug), parent));

            if (pagesTable.Rows.Count > 0)
            {
                //template.ParseVariables("ERROR", "You must give your page a different name, a page already has that name.");
                return -1;
            }


            DataTable parentTable = core.db.SelectQuery(string.Format("SELECT page_id, page_slug, page_parent_path, page_order FROM user_pages WHERE user_id = {0} AND page_id = {1}",
                core.session.LoggedInMember.UserId, parent));

            if (parentTable.Rows.Count == 1)
            {
                if (string.IsNullOrEmpty((string)parentTable.Rows[0]["page_parent_path"]))
                {
                    parentPath = (string)parentTable.Rows[0]["page_slug"];
                }
                else
                {
                    parentPath = (string)parentTable.Rows[0]["page_parent_path"] + "/" + (string)parentTable.Rows[0]["page_slug"];
                }
            }
            else
            {
                // we couldn't find a parent so set to zero
                parent = 0;
            }

            DataTable orderTable = core.db.SelectQuery(string.Format("SELECT page_id, page_order FROM user_pages WHERE page_id <> {3} AND page_title > '{0}' AND page_parent_path = '{1}' AND user_id = {2} ORDER BY page_title ASC LIMIT 1",
                Mysql.Escape(title), Mysql.Escape(parentPath), owner.UserId, pageId));

            if (orderTable.Rows.Count == 1)
            {
                order = (ushort)orderTable.Rows[0]["page_order"];

                if (order == oldOrder + 1 && pageId > 0)
                {
                    order = oldOrder;
                }
            }
            else if (parent > 0 && parentTable.Rows.Count == 1)
            {
                order = (ushort)((ushort)parentTable.Rows[0]["page_order"] + 1);
            }
            else
            {
                orderTable = core.db.SelectQuery(string.Format("SELECT MAX(page_order) + 1 as max_order FROM user_pages WHERE user_id = {0} AND page_id <> {1}",
                    owner.UserId, pageId));

                if (orderTable.Rows.Count == 1)
                {
                    if (!(orderTable.Rows[0]["max_order"] is DBNull))
                    {
                        order = (ushort)(ulong)orderTable.Rows[0]["max_order"];
                    }
                }
            }

            if (order < 0)
            {
                order = 0;
            }

            core.db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND user_id = {1}",
                order, owner.UserId), true);

            pageId = core.db.UpdateQuery(string.Format("INSERT INTO user_pages (user_id, page_slug, page_parent_path, page_date_ut, page_title, page_modified_ut, page_ip, page_text, page_license, page_access, page_order, page_parent_id, page_status, page_list_only) VALUES ({0}, '{1}', '{2}', UNIX_TIMESTAMP(), '{3}', UNIX_TIMESTAMP(), '{4}', '{5}', {6}, {7}, {8}, {9}, '{10}', {11})",
                owner.UserId, Mysql.Escape(slug), Mysql.Escape(parentPath), Mysql.Escape(title), Mysql.Escape(core.session.IPAddress.ToString()), Mysql.Escape(pageBody), license, permissions, order, parent, Mysql.Escape(status), listOnly), false);

            return pageId;
        }

        public static void DeletePage(Core core, Member owner, string title, string slug, string parentPath)
        {
            //DeleteQuery query = new DeleteQuery("user_pages");

            core.db.UpdateQuery(string.Format("DELETE FROM user_pages WHERE user_id = {0} AND page_title = '{1}' AND page_slug = '{2}' AND page_parent_path = '{3}'",
                owner.UserId, Mysql.Escape(title), Mysql.Escape(slug), Mysql.Escape(parentPath)));
        }
    }
}
