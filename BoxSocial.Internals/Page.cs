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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum PageStatus : byte
    {
        PageList,
        Publish,
        Draft,
    }

    public class Page
    {
        public const string PAGE_FIELDS = "pa.page_id, pa.user_id, pa.page_slug, pa.page_title, pa.page_text, pa.page_access, pa.page_license, pa.page_views, pa.page_status, pa.page_ip, pa.page_parent_path, pa.page_order, pa.page_parent_id, pa.page_hierarchy, pa.page_date_ut, pa.page_modified_ut, pa.page_classification, pa.page_list_only, pa.page_application, pa.page_icon";

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
        private bool listOnly;
        private long applicationId;
        private string icon;
        // TODO: hierarchy
        private long createdRaw;
        private long modifiedRaw;
        private ContentLicense license;
        private Classifications classification;

        private bool slugChanged;
        private bool titleChanged;
        private bool bodyChanged;
        private bool permissionsChanged;
        private bool licenseChanged;
        private bool viewsChanged;
        private bool statusChanged;
        private bool parentChanged;
        private bool orderChanged;
        private bool listOnlyChanged;
        private bool applicationChanged;
        private bool iconChanged;

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
            set
            {
                slug = value;
                slugChanged = true;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                if (value != title)
                {
                    Navigation.GenerateSlug(value, ref slug);
                    slugChanged = true;
                }

                title = value;
                titleChanged = true;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
            set
            {
                body = value;
                bodyChanged = true;
            }
        }

        public string Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = value;
                iconChanged = true;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
            set
            {
                permissions = value;
                permissionsChanged = true;
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
            set
            {
                licenseId = value;
                licenseChanged = true;
            }
        }

        public ContentLicense License
        {
            get
            {
                return license;
            }
        }

        public Classifications Classification
        {
            get
            {
                return classification;
            }
        }

        public ulong Views
        {
            get
            {
                return views;
            }
            set
            {
                views = value;
                viewsChanged = true;
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                statusChanged = true;
            }
        }

        public long ParentId
        {
            get
            {
                return parentId;
            }
            set
            {
                parentId = value;
                parentChanged = true;
            }
        }

        public ushort Order
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                orderChanged = true;
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

        public bool ListOnly
        {
            get
            {
                return listOnly;
            }
            set
            {
                listOnly = value;
                listOnlyChanged = true;
            }
        }

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public Page(Mysql db, Member owner, string pageName)
        {
            this.db = db;
            this.owner = owner;

            string[] paths = pageName.Split('/');
            DataTable pageTable = db.Query(string.Format("SELECT {4}, {3} FROM user_pages pa LEFT JOIN licenses li ON li.license_id = pa.page_license WHERE pa.user_id = {0} AND pa.page_slug = '{1}' AND pa.page_parent_path = '{2}';",
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

        public Page(Mysql db, Member owner, string pageName, string pageParentPath)
        {
            this.db = db;
            this.owner = owner;

            DataTable pageTable = db.Query(string.Format("SELECT {4}, {3} FROM user_pages pa LEFT JOIN licenses li ON li.license_id = pa.page_license WHERE pa.user_id = {0} AND pa.page_slug = '{1}' AND pa.page_parent_path = '{2}';",
                owner.UserId, Mysql.Escape(pageName), Mysql.Escape(pageParentPath), Page.PAGE_FIELDS, ContentLicense.LICENSE_FIELDS));

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

            DataTable pageTable = db.Query(string.Format("SELECT {3}, {2} FROM user_pages pa LEFT JOIN licenses li ON li.license_id = pa.page_license WHERE pa.user_id = {0} AND pa.page_id = {1};",
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
            classification = (Classifications)(byte)pageRow["page_classification"];
            listOnly = ((byte)pageRow["page_list_only"] > 0) ? true : false;

            pageAccess = new Access(db, permissions, owner);
        }

        private void loadLicenseInfo(DataRow pageRow)
        {
            license = new ContentLicense(db, pageRow);
        }

        public static string PageStatusToString(PageStatus status)
        {
            switch (status)
            {
                case PageStatus.PageList:
                    return "PUBLISH";
                case PageStatus.Publish:
                    return "PUBLISH";
                case PageStatus.Draft:
                    return "DRAFT";
                default:
                    return "PUBLISH";
            }
        }

        public static PageStatus StringToPageStatus(string status)
        {
            switch (status)
            {
                case "PUBLISH":
                    return PageStatus.Publish;
                case "DRAFT":
                    return PageStatus.Draft;
                default:
                    return PageStatus.Publish;
            }
        }

        public static Page Create(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, ushort permissions, byte license, Classifications classification)
        {
            return Create(core, owner, title, ref slug, parent, pageBody, status, permissions, license, classification, null);
        }

        internal static Page Create(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, ushort permissions, byte license, Classifications classification, ApplicationEntry application)
        {
            string parentPath = "";
            long pageId = 0;
            ushort order = 0;
            bool pageListOnly = (status == PageStatus.PageList);

            if (!pageListOnly)
            {
                Navigation.GenerateSlug(title, ref slug);
            }

            // validate title;

            if (string.IsNullOrEmpty(title))
            {
                throw new PageTitleNotValidException();
            }

            if (string.IsNullOrEmpty(slug))
            {
                throw new PageSlugNotValidException();
            }

            if ((!Functions.CheckPageNameValid(slug)) && parent == 0)
            {
                throw new PageSlugNotUniqueException();
            }

            if (string.IsNullOrEmpty(pageBody) && !pageListOnly)
            {
                throw new PageContentEmptyException();
            }

            // check the page slug for existance

            SelectQuery squery = new SelectQuery("user_pages");
            squery.AddFields("page_title");
            squery.AddCondition("user_id", owner.Id);
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_slug", slug);
            squery.AddCondition("page_parent_id", parent);

            DataTable pagesTable = core.db.Query(squery);

            if (pagesTable.Rows.Count > 0)
            {
                throw new PageSlugNotUniqueException();
            }

            Page parentPage = null;
            try
            {
                parentPage = new Page(core.db, (Member)owner, parent);

                parentPath = parentPage.FullPath;
                parent = parentPage.PageId;
            }
            catch (PageNotFoundException)
            {
                // we couldn't find a parent so set to zero
                parentPath = "";
                parent = 0;
            }

            squery = new SelectQuery("user_pages");
            squery.AddFields("page_id", "page_order");
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_title", ConditionEquality.GreaterThan, title);
            squery.AddCondition("page_parent_id", parent);
            squery.AddCondition("user_id", owner.Id);
            squery.AddSort(SortOrder.Ascending, "page_title");
            squery.LimitCount = 1;

            DataTable orderTable = core.db.Query(squery);

            if (orderTable.Rows.Count == 1)
            {
                order = (ushort)orderTable.Rows[0]["page_order"];
            }
            else if (parent > 0 && parentPage != null)
            {
                squery = new SelectQuery("user_pages");
                squery.AddFields("page_id", "page_order");
                squery.AddCondition("page_id", ConditionEquality.NotEqual, parentPage.PageId);
                squery.AddCondition("page_title", ConditionEquality.GreaterThan, parentPage.Title);
                squery.AddCondition("page_parent_id", parentPage.ParentId);
                squery.AddCondition("user_id", owner.Id);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable2 = core.db.Query(squery);

                if (orderTable2.Rows.Count == 1)
                {
                    order = (ushort)orderTable2.Rows[0]["page_order"];
                }
                else
                {
                    order = (ushort)(parentPage.Order + 1);
                }
            }
            else
            {
                squery = new SelectQuery("user_pages");
                squery.AddFields("MAX(page_order) + 1 AS max_order");
                squery.AddCondition("user_id", owner.Id);
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                orderTable = core.db.Query(squery);

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

            UpdateQuery uquery = new UpdateQuery("user_pages");
            uquery.AddField("page_order", new QueryOperation("page_order", QueryOperations.Addition, 1));
            uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
            uquery.AddCondition("user_id", owner.Id);

            core.db.BeginTransaction();
            core.db.Query(uquery);

            InsertQuery iquery = new InsertQuery("user_pages");
            iquery.AddField("user_id", owner.Id);
            iquery.AddField("page_slug", slug);
            iquery.AddField("page_parent_path", parentPath);
            iquery.AddField("page_date_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("page_modified_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("page_title", title);
            iquery.AddField("page_ip", core.session.IPAddress.ToString());
            iquery.AddField("page_text", pageBody);
            iquery.AddField("page_license", license);
            iquery.AddField("page_access", permissions);
            iquery.AddField("page_order", order);
            iquery.AddField("page_parent_id", parent);
            iquery.AddField("page_status", PageStatusToString(status));
            iquery.AddField("page_classification", (byte)classification);
            iquery.AddField("page_list_only", ((pageListOnly) ? 1 : 0));
            if (application != null)
            {
                if (application.HasIcon)
                {
                    iquery.AddField("page_icon", string.Format(@"/images/{0}/icon.png", application.Key));
                }
                iquery.AddField("page_application", application.Id);
            }
            else
            {
                iquery.AddField("page_icon", "");
                iquery.AddField("page_application", 0);
            }

            pageId = core.db.Query(iquery);

            return new Page(core.db, (Member)owner, pageId);
        }

        internal void Update()
        {
            ushort order = 0;
            ushort oldOrder = 0;

            UpdateQuery uQuery = new UpdateQuery("user_pages");
            uQuery.AddCondition("user_id", ownerId);
            uQuery.AddCondition("page_id", pageId);

            if (titleChanged)
            {
                // validate title;
                if (string.IsNullOrEmpty(title))
                {
                    throw new PageTitleNotValidException();
                }

                uQuery.AddField("page_title", title);
            }
            if (slugChanged)
            {
                // check the page slug for existance

                SelectQuery squery = new SelectQuery("user_pages");
                squery.AddFields("page_title");
                squery.AddCondition("user_id", owner.Id);
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
                squery.AddCondition("page_slug", slug);
                squery.AddCondition("page_parent_id", parentId);

                DataTable pagesTable = db.Query(squery);

                if (pagesTable.Rows.Count > 0)
                {
                    throw new PageSlugNotUniqueException();
                }

                uQuery.AddField("page_slug", slug);
            }
            if (bodyChanged)
            {
                uQuery.AddField("page_text", body);
            }
            if (iconChanged)
            {
                uQuery.AddField("page_icon", icon);
            }
            if (listOnlyChanged)
            {
                uQuery.AddField("page_list_only", listOnly);
            }
            if (permissionsChanged)
            {
                uQuery.AddField("page_access", permissions);
            }
            if (licenseChanged)
            {
                uQuery.AddField("page_license", licenseId);
            }
            if (viewsChanged)
            {
                uQuery.AddField("page_views", views);
            }
            if (statusChanged)
            {
                uQuery.AddField("page_status", status);
            }
            if (orderChanged)
            {
                uQuery.AddField("page_order", order);
            }
            if (applicationChanged)
            {
                uQuery.AddField("page_application", applicationId);
            }
            if (parentChanged)
            {
                if (this.PageId == parentId)
                {
                    throw new PageOwnParentException();
                }

                uQuery.AddField("page_parent_id", parentId);
            }

            if (!uQuery.HasFields)
            {
                return;
            }

            if ((parentChanged || titleChanged))
            {
                Page parentPage = null;
                try
                {
                    parentPage = new Page(db, (Member)owner, parentId);

                    parentPath = parentPage.FullPath;
                    parentId = parentPage.PageId;
                }
                catch (PageNotFoundException)
                {
                    // we couldn't find a parent so set to zero
                    parentPath = "";
                    parentId = 0;
                }

                SelectQuery squery = new SelectQuery("user_pages");
                squery.AddFields("page_id", "page_order");
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
                squery.AddCondition("page_title", ConditionEquality.GreaterThan, title);
                squery.AddCondition("page_parent_id", parentId);
                squery.AddCondition("user_id", owner.Id);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable = db.Query(squery);

                if (orderTable.Rows.Count == 1)
                {
                    order = (ushort)orderTable.Rows[0]["page_order"];

                    if (order == oldOrder + 1 && pageId > 0)
                    {
                        order = oldOrder;
                    }
                }
                else if (parentId > 0 && parentPage != null)
                {
                    squery = new SelectQuery("user_pages");
                    squery.AddFields("page_id", "page_order");
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, parentPage.PageId);
                    squery.AddCondition("page_title", ConditionEquality.GreaterThan, parentPage.Title);
                    squery.AddCondition("page_parent_id", parentPage.ParentId);
                    squery.AddCondition("user_id", owner.Id);
                    squery.AddSort(SortOrder.Ascending, "page_title");
                    squery.LimitCount = 1;

                    DataTable orderTable2 = db.Query(squery);

                    if (orderTable2.Rows.Count == 1)
                    {
                        order = (ushort)orderTable2.Rows[0]["page_order"];

                        if (order == oldOrder + 1 && pageId > 0)
                        {
                            order = oldOrder;
                        }
                    }
                    else
                    {
                        order = (ushort)(parentPage.Order + 1);
                    }
                }
                else
                {
                    squery = new SelectQuery("user_pages");
                    squery.AddFields("MAX(page_order) + 1 AS max_order");
                    squery.AddCondition("user_id", owner.Id);
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                    orderTable = db.Query(squery);

                    if (orderTable.Rows.Count == 1)
                    {
                        if (!(orderTable.Rows[0]["max_order"] is DBNull))
                        {
                            order = (ushort)(ulong)orderTable.Rows[0]["max_order"];
                        }
                    }
                }
            }

            if (orderChanged) // order != oldOrder
            {
                db.BeginTransaction();
                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND user_id = {1}",
                        oldOrder, owner.Id));

                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND user_id = {1}",
                    order, owner.Id));
            }

            UpdateQuery uQuerys = new UpdateQuery("user_pages");
            uQuerys.AddField("page_order", new QueryOperation("page_order", QueryOperations.Addition, 1));
            uQuerys.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
            uQuerys.AddCondition("user_id", owner.Id);

            //Display.ShowMessage("Query", uQuerys.ToString());

            db.BeginTransaction();
            db.Query(uQuerys);

            db.Query(uQuery);
        }

        public void Update(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, ushort permissions, byte license, Classifications classification)
        {
            string parentPath = "";
            long pageId = this.pageId;
            ushort order = 0;
            ushort oldOrder = 0;
            bool pageListOnly = (status == PageStatus.PageList);
            bool parentChanged = false;
            bool titleChanged = false;

            if (this.parentPath != parentPath)
            {
                parentChanged = true;
            }

            if (this.Title != title)
            {
                titleChanged = true;
            }

            if (!pageListOnly)
            {
                Navigation.GenerateSlug(title, ref slug);
            }

            // validate title;

            if (string.IsNullOrEmpty(title))
            {
                throw new PageTitleNotValidException();
            }

            if (string.IsNullOrEmpty(slug))
            {
                throw new PageSlugNotValidException();
            }

            if ((!Functions.CheckPageNameValid(slug)) && parent == 0)
            {
                throw new PageSlugNotUniqueException();
            }

            if (string.IsNullOrEmpty(pageBody) && !pageListOnly)
            {
                throw new PageContentEmptyException();
            }

            if (this.PageId == parent)
            {
                throw new PageOwnParentException();
            }

            // check the page slug for existance

            SelectQuery squery = new SelectQuery("user_pages");
            squery.AddFields("page_title");
            squery.AddCondition("user_id", owner.Id);
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_slug", slug);
            squery.AddCondition("page_parent_id", parent);

            DataTable pagesTable = core.db.Query(squery);

            if (pagesTable.Rows.Count > 0)
            {
                throw new PageSlugNotUniqueException();
            }

            if ((parentChanged || titleChanged))
            {
                Page parentPage = null;
                try
                {
                    parentPage = new Page(core.db, (Member)owner, parent);

                    parentPath = parentPage.FullPath;
                    parent = parentPage.PageId;
                }
                catch (PageNotFoundException)
                {
                    // we couldn't find a parent so set to zero
                    parentPath = "";
                    parent = 0;
                }

                squery = new SelectQuery("user_pages");
                squery.AddFields("page_id", "page_order");
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
                squery.AddCondition("page_title", ConditionEquality.GreaterThan, title);
                squery.AddCondition("page_parent_id", parent);
                squery.AddCondition("user_id", owner.Id);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable = core.db.Query(squery);

                if (orderTable.Rows.Count == 1)
                {
                    order = (ushort)orderTable.Rows[0]["page_order"];

                    if (order == oldOrder + 1 && pageId > 0)
                    {
                        order = oldOrder;
                    }
                }
                else if (parent > 0 && parentPage != null)
                {
                    squery = new SelectQuery("user_pages");
                    squery.AddFields("page_id", "page_order");
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, parentPage.PageId);
                    squery.AddCondition("page_title", ConditionEquality.GreaterThan, parentPage.Title);
                    squery.AddCondition("page_parent_id", parentPage.ParentId);
                    squery.AddCondition("user_id", owner.Id);
                    squery.AddSort(SortOrder.Ascending, "page_title");
                    squery.LimitCount = 1;

                    DataTable orderTable2 = core.db.Query(squery);

                    if (orderTable2.Rows.Count == 1)
                    {
                        order = (ushort)orderTable2.Rows[0]["page_order"];

                        if (order == oldOrder + 1 && pageId > 0)
                        {
                            order = oldOrder;
                        }
                    }
                    else
                    {
                        order = (ushort)(parentPage.Order + 1);
                    }
                }
                else
                {
                    squery = new SelectQuery("user_pages");
                    squery.AddFields("MAX(page_order) + 1 AS max_order");
                    squery.AddCondition("user_id", owner.Id);
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                    orderTable = core.db.Query(squery);

                    if (orderTable.Rows.Count == 1)
                    {
                        if (!(orderTable.Rows[0]["max_order"] is DBNull))
                        {
                            order = (ushort)(ulong)orderTable.Rows[0]["max_order"];
                        }
                    }
                }
            }

            if (order != oldOrder)
            {
                db.BeginTransaction();
                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND user_id = {1}",
                        oldOrder, owner.Id));

                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND user_id = {1}",
                    order, owner.Id));
            }

            UpdateQuery uquery = new UpdateQuery("user_pages");
            uquery.AddField("page_order", new QueryField("page_order + 1"));
            uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
            uquery.AddCondition("user_id", owner.Id);

            core.db.BeginTransaction();
            core.db.Query(uquery);

            uquery = new UpdateQuery("user_pages");
            uquery.AddField("user_id", owner.Id);
            uquery.AddField("page_slug", slug);
            if (parentChanged)
            {
                uquery.AddField("page_parent_path", parentPath);
            }
            uquery.AddField("page_modified_ut", UnixTime.UnixTimeStamp());
            if (titleChanged)
            {
                uquery.AddField("page_title", title);
            }
            uquery.AddField("page_ip", core.session.IPAddress.ToString());
            uquery.AddField("page_text", pageBody);
            uquery.AddField("page_license", license);
            uquery.AddField("page_access", permissions);
            if (parentChanged)
            {
                uquery.AddField("page_parent_id", parent);
            }
            uquery.AddField("page_order", order);
            uquery.AddField("page_status", PageStatusToString(status));
            uquery.AddField("page_list_only", ((pageListOnly) ? 1 : 0));
            uquery.AddField("page_classification", (byte)classification);
            uquery.AddCondition("page_id", this.PageId);
            uquery.AddCondition("user_id", owner.Id);

            core.db.Query(uquery);
        }

        public bool Delete(Core core, Primitive owner)
        {
            bool success = false;
            if (this.owner == owner)
            {
                UpdateQuery uquery = new UpdateQuery("user_pages");
                uquery.AddField("page_order", new QueryField("page_order - 1"));
                uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
                uquery.AddCondition("user_id", owner.Id);

                db.BeginTransaction();
                if (db.Query(uquery) > 0)
                {
                    DeleteQuery dquery = new DeleteQuery("user_pages");
                    dquery.AddCondition("page_id", pageId);
                    dquery.AddCondition("user_id", owner.Id);

                    db.Query(dquery);

                    success = true;
                }
            }

            return success;
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
                Functions.Generate404();
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
                Functions.Generate404();
            }
        }

        private static void Show(Core core, PPage page, Page thePage)
        {
            page.template.SetTemplate("Pages", "viewpage");

            long loggedIdUid = thePage.PageAccess.SetSessionViewer(core.session);

            page.ProfileOwner.LoadProfileInfo();

            // TODO: generate page list
            page.template.ParseVariables("PAGE_LIST", Display.GeneratePageList(page.ProfileOwner, core.session.LoggedInMember, true));

            if (!thePage.PageAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            BoxSocial.Internals.Classification.ApplyRestrictions(core, thePage.Classification);

            page.template.ParseVariables("PAGE_TITLE", HttpUtility.HtmlEncode(thePage.Title));
            page.template.ParseVariables("PAGE_BODY", Bbcode.Parse(HttpUtility.HtmlEncode(thePage.Body), core.session.LoggedInMember, page.ProfileOwner));
            DateTime pageDateTime = thePage.GetModifiedDate(core.tz);
            page.template.ParseVariables("PAGE_LAST_MODIFIED", HttpUtility.HtmlEncode(core.tz.DateTimeToString(pageDateTime)));

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
                    page.template.ParseVariables("PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Title));
                }
                if (!string.IsNullOrEmpty(thePage.License.Icon))
                {
                    page.template.ParseVariables("I_PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Icon));
                }
                if (!string.IsNullOrEmpty(thePage.License.Link))
                {
                    page.template.ParseVariables("U_PAGE_LICENSE", HttpUtility.HtmlEncode(thePage.License.Link));
                }
            }

            switch (thePage.Classification)
            {
                case Classifications.Everyone:
                    page.template.ParseVariables("PAGE_CLASSIFICATION", "Suitable for Everyone");
                    page.template.ParseVariables("I_PAGE_CLASSIFICATION", "rating_e.png");
                    break;
                case Classifications.Mature:
                    page.template.ParseVariables("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                    page.template.ParseVariables("I_PAGE_CLASSIFICATION", "rating_15.png");
                    break;
                case Classifications.Restricted:
                    page.template.ParseVariables("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                    page.template.ParseVariables("I_PAGE_CLASSIFICATION", "rating_18.png");
                    break;
            }

            page.template.ParseVariables("PAGE_VIEWS", HttpUtility.HtmlEncode(thePage.Views.ToString()));

            page.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, thePage.FullPath));

            page.template.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode((Linker.BuildProfileUri(page.ProfileOwner))));
            page.template.ParseVariables("U_BLOG", HttpUtility.HtmlEncode((Linker.BuildBlogUri(page.ProfileOwner))));
            page.template.ParseVariables("U_GALLERY", HttpUtility.HtmlEncode((Linker.BuildGalleryUri(page.ProfileOwner))));
            page.template.ParseVariables("U_FRIENDS", HttpUtility.HtmlEncode((Linker.BuildFriendsUri(page.ProfileOwner))));

            if (page.ProfileOwner.UserId == core.LoggedInMemberId)
            {
                page.template.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("pages", "write", "action=edit", string.Format("id={0}", thePage.PageId))));
            }
        }
    }

    public class PageNotFoundException : Exception
    {
    }

    public class PageTitleNotValidException : Exception
    {
    }

    public class PageSlugNotValidException : Exception
    {
    }

    public class PageSlugNotUniqueException : Exception
    {
    }

    public class PageContentEmptyException : Exception
    {
    }

    public class PageOwnParentException : Exception
    {
    }
}
