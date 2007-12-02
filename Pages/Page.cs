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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    public class Page
    {
        public const string PAGE_FIELDS = "pa.page_id, pa.user_id, pa.page_slug, pa.page_title, pa.page_text, pa.page_access, pa.page_license, pa.page_views, pa.page_status, pa.page_ip, pa.page_parent_path, pa.page_order, pa.page_parent_id, pa.page_hierarchy, pa.page_date_ut, pa.page_modified_ut";

        private Mysql db;

        private long pageId;
        private int ownerId;
        private Member owner;
        private string slug;
        private string title;
        private string body;
        private ushort permissions;
        private Access pageAccess;
        private byte licenseId;
        private ulong views;
        private string status;
        private string ipRaw;
        private string parentPath;
        private ushort order;
        private long parentId;
        // TODO: hierarchy
        private long createdRaw;
        private long modifiedRaw;
        private ContentLicense license;

        public long PageId
        {
            get
            {
                return pageId;
            }
        }

        public string Slug
        {
            get
            {
                return slug;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access PageAccess
        {
            get
            {
                return pageAccess;
            }
        }

        public byte LicenseId
        {
            get
            {
                return licenseId;
            }
        }

        public ContentLicense License
        {
            get
            {
                return license;
            }
        }

        public ulong Views
        {
            get
            {
                return views;
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
        }

        public string FullPath
        {
            get
            {
                if (!string.IsNullOrEmpty(parentPath))
                {
                    return string.Format("{0}/{1}", parentPath, slug);
                }
                else
                {
                    return slug;
                }
            }
        }

        public DateTime GetCreatedDate(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public Page(Mysql db, Member owner, string pageName)
        {
            this.db = db;
            this.owner = owner;

            string[] paths = pageName.Split('/');
            DataTable pageTable = db.SelectQuery(string.Format("SELECT {4}, {3} FROM user_pages pa LEFT JOIN licenses li ON li.license_id = pa.page_license WHERE pa.user_id = {0} AND pa.page_slug = '{1}' AND pa.page_parent_path = '{2}';",
                owner.UserId, Mysql.Escape(paths[paths.GetUpperBound(0)]), Mysql.Escape(pageName.Remove(pageName.Length - paths[paths.GetUpperBound(0)].Length).TrimEnd('/')), Page.PAGE_FIELDS, ContentLicense.LICENSE_FIELDS));

            if (pageTable.Rows.Count == 1)
            {
                loadPageInfo(pageTable.Rows[0]);
                try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
                {
                }
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public Page(Mysql db, Member owner, long pageId)
        {
            this.db = db;
            this.owner = owner;

            DataTable pageTable = db.SelectQuery(string.Format("SELECT {3}, {2} FROM user_pages pa LEFT JOIN licenses li ON li.license_id = pa.page_license WHERE pa.user_id = {0} AND pa.page_id = {1};",
                owner.UserId, pageId, Page.PAGE_FIELDS, ContentLicense.LICENSE_FIELDS));

            if (pageTable.Rows.Count == 1)
            {
                loadPageInfo(pageTable.Rows[0]);
                try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
                {
                }
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        private void loadPageInfo(DataRow pageRow)
        {
            pageId = (long)pageRow["page_id"];
            ownerId = (int)pageRow["user_id"];
            slug = (string)pageRow["page_slug"];
            title = (string)pageRow["page_title"];
            if (!(pageRow["page_text"] is DBNull))
            {
                body = (string)pageRow["page_text"];
            }
            permissions = (ushort)pageRow["page_access"];
            licenseId = (byte)pageRow["page_license"];
            views = (ulong)pageRow["page_views"];
            status = (string)pageRow["page_status"];
            ipRaw = (string)pageRow["page_ip"];
            parentPath = (string)pageRow["page_parent_path"];
            order = (ushort)pageRow["page_order"];
            parentId = (long)pageRow["page_parent_id"];
            // TODO: hierarchy
            createdRaw = (long)pageRow["page_date_ut"];
            modifiedRaw = (long)pageRow["page_modified_ut"];

            pageAccess = new Access(db, permissions, owner);
        }

        private void loadLicenseInfo(DataRow pageRow)
        {
            license = new ContentLicense(db, pageRow);
        }

        public static void Show(Core core, PPage page, string pageName)
        {
            char[] trimStartChars = { '.', '/' };
            if (pageName != null)
            {
                pageName = pageName.TrimEnd('/').TrimStart(trimStartChars);
            }

            try
            {
                Page thePage = new Page(core.db, page.ProfileOwner, pageName);
                Show(core, page, thePage);
            }
            catch (PageNotFoundException)
            {
                Functions.Generate404(core);
            }
        }

        public static void Show(Core core, PPage page, long pageId)
        {
            try
            {
                Page thePage = new Page(core.db, page.ProfileOwner, pageId);
                Show(core, page, thePage);
            }
            catch (PageNotFoundException)
            {
                Functions.Generate404(core);
            }
        }

        private static void Show(Core core, PPage page, Page thePage)
        {
            core.template.SetTemplate("viewpage.html");

            long loggedIdUid = thePage.PageAccess.SetSessionViewer(core.session);

            page.ProfileOwner.LoadProfileInfo();

            // TODO: generate page list
            core.template.ParseVariables("PAGE_LIST", Display.GeneratePageList(core.db, page.ProfileOwner, core.session.LoggedInMember, true));

            if (!thePage.PageAccess.CanRead)
            {
                Functions.Generate403(core);
                return;
            }

            core.template.ParseVariables("PAGE_TITLE", HttpUtility.HtmlEncode(thePage.Title));
            core.template.ParseVariables("PAGE_BODY", Bbcode.Parse(HttpUtility.HtmlEncode(thePage.Body), core.session.LoggedInMember, page.ProfileOwner));
            DateTime pageDateTime = thePage.GetModifiedDate(core.tz);
            core.template.ParseVariables("PAGE_LAST_MODIFIED", HttpUtility.HtmlEncode(core.tz.DateTimeToString(pageDateTime)));

            if (core.session.LoggedInMember != null)
            {
                if (page.ProfileOwner.UserId != core.session.LoggedInMember.UserId)
                {
                    core.db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE user_id = {0} AND page_id = '{1}';",
                        page.ProfileOwner.UserId, thePage.PageId));
                }
            }

            if (thePage.License != null)
            {
                if (!string.IsNullOrEmpty(thePage.License.Title))
                {
                    core.template.ParseVariables("PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Title));
                }
                if (!string.IsNullOrEmpty(thePage.License.Icon))
                {
                    core.template.ParseVariables("I_PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Icon));
                }
                if (!string.IsNullOrEmpty(thePage.License.Link))
                {
                    core.template.ParseVariables("U_PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Link));
                }
            }

            core.template.ParseVariables("PAGE_VIEWS", HttpUtility.HtmlEncode(thePage.Views.ToString()));

            core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, thePage.FullPath));

            core.template.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode((ZzUri.BuildProfileUri(page.ProfileOwner))));
            core.template.ParseVariables("U_BLOG", HttpUtility.HtmlEncode((ZzUri.BuildBlogUri(page.ProfileOwner))));
            core.template.ParseVariables("U_GALLERY", HttpUtility.HtmlEncode((ZzUri.BuildGalleryUri(page.ProfileOwner))));
            core.template.ParseVariables("U_FRIENDS", HttpUtility.HtmlEncode((ZzUri.BuildFriendsUri(page.ProfileOwner))));
        }
    }

    public class PageNotFoundException : Exception
    {
    }
}
