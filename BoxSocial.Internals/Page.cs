/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum PageStatus : byte
    {
        PageList,
        Publish,
        Draft,
    }

    [DataTable("user_pages")]
    public class Page : NumberedItem
    {
        public const string PAGE_FIELDS = "pa.page_id, pa.user_id, pa.page_slug, pa.page_title, pa.page_text, pa.page_access, pa.page_license, pa.page_views, pa.page_status, pa.page_ip, pa.page_parent_path, pa.page_order, pa.page_parent_id, pa.page_hierarchy, pa.page_date_ut, pa.page_modified_ut, pa.page_classification, pa.page_list_only, pa.page_application, pa.page_icon";

        [DataField("page_id", DataFieldKeys.Primary)]
        private long pageId;
        [DataField("user_id")]
        private long creatorId;
        [DataField("page_slug", 63)]
        private string slug;
        [DataField("page_title", 63)]
        private string title;
        [DataField("page_text", MYSQL_MEDIUM_TEXT)]
        private string body;
        [DataField("page_access")]
        private ushort permissions;
        [DataField("page_license")]
        private byte licenseId;
        [DataField("page_views")]
        private long views;
        [DataField("page_status", 15)]
        private string status;
        [DataField("page_ip", 50)]
        private string ipRaw;
        [DataField("page_ip_proxy", 50)]
        private string ipProxyRaw;
        [DataField("page_parent_path", MYSQL_TEXT)]
        private string parentPath;
        [DataField("page_order")]
        private int order;
        [DataField("page_parent_id")]
        private long parentId;
        [DataField("page_list_only")]
        private bool listOnly;
        [DataField("page_application")]
        private long applicationId;
        [DataField("page_icon", 63)]
        private string icon;
        [DataField("page_date_ut")]
        private long createdRaw;
        [DataField("page_modified_ut")]
        private long modifiedRaw;
        [DataField("page_classification")]
        private byte classificationId;
        [DataField("page_hierarchy", MYSQL_TEXT)]
        private string hierarchy;
        [DataField("page_item_id")]
        private long ownerId;
        [DataField("page_item_type", NAMESPACE)]
        private string ownerType;

        private User creator;
        private Primitive owner;
        private Access pageAccess;
        private ContentLicense license;
        private Classifications classification;
        private ParentTree parentTree;

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
                SetProperty("slug", value);
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
                    string slug = "";
                    Navigation.GenerateSlug(value, ref slug);
                    Slug = slug;
                }

                SetProperty("title", value);
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
                SetProperty("body", value);
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
                SetProperty("icon", value);
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
                SetProperty("permissions", value);
            }
        }

        public Access PageAccess
        {
            get
            {
                if (pageAccess == null)
                {
                    pageAccess = new Access(core, permissions, Owner);
                }
                return pageAccess;
            }
        }

        public User Creator
        {
            get
            {
                if (creator == null || creatorId != creator.Id)
                {
                    core.UserProfiles.LoadUserProfile(creatorId);
                    creator = core.UserProfiles[creatorId];
                    return creator;
                }
                else
                {
                    return creator;
                }
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id || ownerType != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerType, ownerId);
                    owner = core.UserProfiles[ownerType, ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }
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
                SetProperty("licenseId", value);
            }
        }

        public ContentLicense License
        {
            get
            {
                return license;
            }
        }

        public byte ClassificationId
        {
            get
            {
                return classificationId;
            }
            set
            {
                SetProperty("classificationId", value);
            }
        }

        public Classifications Classification
        {
            get
            {
                return classification;
            }
        }

        public long Views
        {
            get
            {
                return views;
            }
            set
            {
                SetProperty("views", value);
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
                SetProperty("status", value);
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
                SetProperty("parentId", value);

                if (parentId > 0)
                {
                    Page parent = new Page(core, owner, parentId);

                    parentTree = new ParentTree();

                    foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                    }

                    if (parent.Id > 0)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(parent.Title, parent.Slug, parent.Id));
                    }

                    XmlSerializer xs = new XmlSerializer(typeof(ParentTreeNode));
                    StringBuilder sb = new StringBuilder();
                    StringWriter stw = new StringWriter(sb);

                    xs.Serialize(stw, parentTree);
                    stw.Flush();
                    stw.Close();

                    SetProperty("hierarchy", sb.ToString());
                }
                else
                {
                    SetProperty("hierarchy", "");
                }
            }
        }

        public ParentTree Parents
        {
            get
            {
                if (parentTree == null)
                {
                    if (string.IsNullOrEmpty(hierarchy))
                    {
                        return null;
                    }
                    else
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(ParentTree)); ;
                        StringReader sr = new StringReader(hierarchy);

                        parentTree = (ParentTree)xs.Deserialize(sr);
                    }
                }

                return parentTree;
            }
        }

        public int Order
        {
            get
            {
                return order;
            }
            set
            {
                SetProperty("order", value);
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
                SetProperty("listOnly", value);
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

        public Page(Core core, Primitive owner, string pageName)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_slug", Page.GetNameFromPath(pageName));
            query.AddCondition("page_parent_path", Page.GetParentPath(pageName));
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type", owner.Type);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadPageInfo(pageTable.Rows[0]);
                try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public Page(Core core, Primitive owner, string pageName, string pageParentPath)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_slug", pageName);
            query.AddCondition("page_parent_path", pageParentPath);
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type", owner.Type);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadPageInfo(pageTable.Rows[0]);
                try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public Page(Core core, Primitive owner, long pageId)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_id", pageId);
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type", owner.Type);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadPageInfo(pageTable.Rows[0]);
                try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public Page(Core core, Primitive owner, DataRow pageRow)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            loadItemInfo(pageRow);
        }

        private void Page_ItemLoad()
        {
        }

        public static SelectQuery Page_GetSelectQueryStub()
        {
            SelectQuery query = Page.GetSelectQueryStub(typeof(Page), false);
            query.AddFields(Page.GetFieldsPrefixed(typeof(ContentLicense)));
            query.AddJoin(JoinTypes.Left, ContentLicense.GetTable(typeof(ContentLicense)), "page_license", "license_id");

            return query;
        }

        private void loadPageInfo(DataRow pageRow)
        {
            pageId = (long)pageRow["page_id"];
            creatorId = (long)pageRow["user_id"];
            ownerId = (long)pageRow["page_item_id"];
            ownerType = (string)pageRow["page_item_type"];
            slug = (string)pageRow["page_slug"];
            title = (string)pageRow["page_title"];
            if (!(pageRow["page_text"] is DBNull))
            {
                body = (string)pageRow["page_text"];
            }
            permissions = (ushort)pageRow["page_access"];
            licenseId = (byte)pageRow["page_license"];
            views = (long)pageRow["page_views"];
            status = (string)pageRow["page_status"];
            ipRaw = (string)pageRow["page_ip"];
            parentPath = (string)pageRow["page_parent_path"];
            order = (int)pageRow["page_order"];
            parentId = (long)pageRow["page_parent_id"];
            createdRaw = (long)pageRow["page_date_ut"];
            modifiedRaw = (long)pageRow["page_modified_ut"];
            classification = (Classifications)(byte)pageRow["page_classification"];
            listOnly = ((byte)pageRow["page_list_only"] > 0) ? true : false;
            if (!(pageRow["page_hierarchy"] is DBNull))
            {
                hierarchy = (string)pageRow["page_hierarchy"];
            }

            pageAccess = new Access(core, permissions, owner);
        }

        private void loadLicenseInfo(DataRow pageRow)
        {
            license = new ContentLicense(core, pageRow);
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

        public static Page Create(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, ushort permissions, byte license, Classifications classification, ApplicationEntry application)
        {
            string parents = "";
            string parentPath = "";
            long pageId = 0;
            int order = 0;
            bool pageListOnly = (status == PageStatus.PageList);

            title = Functions.TrimStringToWord(title);

            if (!pageListOnly)
            {
                Navigation.GenerateSlug(title, ref slug);
            }

            slug = Functions.TrimStringToWord(slug);

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
            squery.AddCondition("page_item_id", owner.Id);
            squery.AddCondition("page_item_type", owner.Type);
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
                parentPage = new Page(core, owner, parent);

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
            squery.AddCondition("page_item_id", owner.Id);
            squery.AddCondition("page_item_type", owner.Type);
            squery.AddSort(SortOrder.Ascending, "page_title");
            squery.LimitCount = 1;

            DataTable orderTable = core.db.Query(squery);

            if (orderTable.Rows.Count == 1)
            {
                order = (int)orderTable.Rows[0]["page_order"];
            }
            else if (parent > 0 && parentPage != null)
            {
                squery = new SelectQuery("user_pages");
                squery.AddFields("page_id", "page_order");
                squery.AddCondition("page_id", ConditionEquality.NotEqual, parentPage.PageId);
                squery.AddCondition("page_title", ConditionEquality.GreaterThan, parentPage.Title);
                squery.AddCondition("page_parent_id", parentPage.ParentId);
                squery.AddCondition("page_item_id", owner.Id);
                squery.AddCondition("page_item_type", owner.Type);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable2 = core.db.Query(squery);

                if (orderTable2.Rows.Count == 1)
                {
                    order = (int)orderTable2.Rows[0]["page_order"];
                }
                else
                {
                    order = (int)(parentPage.Order + 1);
                }
            }
            else
            {
                squery = new SelectQuery("user_pages");
                squery.AddFields("MAX(page_order) + 1 AS max_order");
                squery.AddCondition("page_item_id", owner.Id);
                squery.AddCondition("page_item_type", owner.Type);
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                orderTable = core.db.Query(squery);

                if (orderTable.Rows.Count == 1)
                {
                    if (!(orderTable.Rows[0]["max_order"] is DBNull))
                    {
                        order = (int)(long)orderTable.Rows[0]["max_order"];
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
            uquery.AddCondition("page_item_id", owner.Id);
            uquery.AddCondition("page_item_type", owner.Type);

            if (parentPage != null)
            {
                ParentTree parentTree = new ParentTree();

                if (parentPage.Parents != null)
                {
                    foreach (ParentTreeNode ptn in parentPage.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                    }
                }

                if (parentPage.Id > 0)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(parentPage.Title, parentPage.Slug, parentPage.Id));
                }

                XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
                StringBuilder sb = new StringBuilder();
                StringWriter stw = new StringWriter(sb);

                xs.Serialize(stw, parentTree);
                stw.Flush();
                stw.Close();

                parents = sb.ToString();
            }

            core.db.BeginTransaction();
            core.db.Query(uquery);

            InsertQuery iquery = new InsertQuery("user_pages");
            if (application == null)
            {
                iquery.AddField("user_id", core.LoggedInMemberId);
            }
            else
            {
                iquery.AddField("user_id", owner.Id);
            }
            iquery.AddField("page_item_id", owner.Id);
            iquery.AddField("page_item_type", owner.Type);
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
            iquery.AddField("page_hierarchy", parents);
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

            return new Page(core, owner, pageId);
        }

        public void Update(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, ushort permissions, byte license, Classifications classification)
        {
            string parents = "";
            string parentPath = "";
            long pageId = this.pageId;
            int order = this.order;
            int oldOrder = this.order;
            bool pageListOnly = (status == PageStatus.PageList);
            bool parentChanged = false;
            bool titleChanged = false;

            Page parentPage = null;
            try
            {
                parentPage = new Page(core, owner, parent);

                parentPath = parentPage.FullPath;
                parent = parentPage.PageId;
            }
            catch (PageNotFoundException)
            {
                // we couldn't find a parent so set to zero
                parentPath = "";
                parent = 0;
            }

            if (parentPage != null)
            {
                ParentTree parentTree = new ParentTree();

                if (parentPage.Parents != null)
                {
                    foreach (ParentTreeNode ptn in parentPage.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                    }
                }

                if (parentPage.Id > 0)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(parentPage.Title, parentPage.Slug, parentPage.Id));
                }

                XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
                StringBuilder sb = new StringBuilder();
                StringWriter stw = new StringWriter(sb);

                xs.Serialize(stw, parentTree);
                stw.Flush();
                stw.Close();

                parents = sb.ToString();
            }

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
            squery.AddCondition("page_item_id", owner.Id);
            squery.AddCondition("page_item_type", owner.Type);
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_slug", slug);
            squery.AddCondition("page_parent_id", parent);

            DataTable pagesTable = core.db.Query(squery);

            if (pagesTable.Rows.Count > 0)
            {
                throw new PageSlugNotUniqueException();
            }

            // has the title or parent been changed
            if ((parentChanged || titleChanged))
            {
                squery = new SelectQuery("user_pages");
                squery.AddFields("page_id", "page_order");
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
                squery.AddCondition("page_title", ConditionEquality.GreaterThan, title);
                squery.AddCondition("page_parent_id", parent);
                squery.AddCondition("page_item_id", owner.Id);
                squery.AddCondition("page_item_type", owner.Type);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable = core.db.Query(squery);

                if (orderTable.Rows.Count == 1)
                {
                    order = (int)orderTable.Rows[0]["page_order"];

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
                    squery.AddCondition("page_item_id", owner.Id);
                    squery.AddCondition("page_item_type", owner.Type);
                    squery.AddSort(SortOrder.Ascending, "page_title");
                    squery.LimitCount = 1;

                    DataTable orderTable2 = core.db.Query(squery);

                    if (orderTable2.Rows.Count == 1)
                    {
                        order = (int)orderTable2.Rows[0]["page_order"];

                        if (order == oldOrder + 1 && pageId > 0)
                        {
                            order = oldOrder;
                        }
                    }
                    else
                    {
                        order = (int)(parentPage.Order + 1);
                    }
                }
                else
                {
                    squery = new SelectQuery("user_pages");
                    squery.AddFields("MAX(page_order) + 1 AS max_order");
                    squery.AddCondition("page_item_id", owner.Id);
                    squery.AddCondition("page_item_type", owner.Type);
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                    orderTable = core.db.Query(squery);

                    if (orderTable.Rows.Count == 1)
                    {
                        if (!(orderTable.Rows[0]["max_order"] is DBNull))
                        {
                            order = (int)(long)orderTable.Rows[0]["max_order"];
                        }
                    }
                }
            }

            db.BeginTransaction();
            if (order != oldOrder)
            {
                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND page_item_id = {1} AND page_item_type = `{2}`",
                        oldOrder, owner.Id, owner.Type));

                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND page_item_id = {1} AND page_item_type = `{2}`",
                    order, owner.Id, owner.Type));
            }

            UpdateQuery uquery = new UpdateQuery("user_pages");
            uquery.AddField("page_order", new QueryField("page_order + 1"));
            uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
            uquery.AddCondition("page_item_id", owner.Id);
            uquery.AddCondition("page_item_type", owner.Type);

            db.Query(uquery);

            uquery = new UpdateQuery("user_pages");
            //uquery.AddField("user_id", owner.Id);
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
            uquery.AddField("page_hierarchy", parents);
            uquery.AddCondition("page_id", this.PageId);
            uquery.AddCondition("page_item_id", owner.Id);
            uquery.AddCondition("page_item_type", owner.Type);

            db.Query(uquery);
        }

        public bool Delete(Core core, Primitive owner)
        {
            bool success = false;
            if (this.owner == owner)
            {
                UpdateQuery uquery = new UpdateQuery("user_pages");
                uquery.AddField("page_order", new QueryField("page_order - 1"));
                uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
                uquery.AddCondition("page_item_id", owner.Id);
                uquery.AddCondition("page_item_type", owner.Type);

                db.BeginTransaction();
                if (db.Query(uquery) > 0)
                {
                    DeleteQuery dquery = new DeleteQuery("user_pages");
                    dquery.AddCondition("page_id", pageId);
                    dquery.AddCondition("page_item_id", owner.Id);
                    dquery.AddCondition("page_item_type", owner.Type);

                    db.Query(dquery);

                    success = true;
                }
            }

            return success;
        }

        public static void Show(Core core, Primitive owner, string pageName)
        {
            char[] trimStartChars = { '.', '/' };
            if (pageName != null)
            {
                pageName = pageName.TrimEnd('/').TrimStart(trimStartChars);
            }

            try
            {
                Page thePage = new Page(core, owner, pageName);
                Show(core, owner, thePage);
            }
            catch (PageNotFoundException)
            {
                Functions.Generate404();
            }
        }

        public static void Show(Core core, Primitive owner, long pageId)
        {
            try
            {
                Page thePage = new Page(core, owner, pageId);
                Show(core, owner, thePage);
            }
            catch (PageNotFoundException)
            {
                Functions.Generate404();
            }
        }

        private static void Show(Core core, Primitive owner, Page thePage)
        {
            core.template.SetTemplate("Pages", "viewpage");

            long loggedIdUid = thePage.PageAccess.SetSessionViewer(core.session);

            if (owner is User)
            {
                ((User)owner).LoadProfileInfo();
            }

            Display.ParsePageList(owner, true);

            if (!thePage.PageAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            BoxSocial.Internals.Classification.ApplyRestrictions(core, thePage.Classification);

            core.template.Parse("PAGE_TITLE", thePage.Title);
            if (owner is User)
            {
                Display.ParseBbcode("PAGE_BODY", thePage.Body, (User)owner);
            }
            else
            {
                Display.ParseBbcode("PAGE_BODY", thePage.Body);
            }
            DateTime pageDateTime = thePage.GetModifiedDate(core.tz);
            core.template.Parse("PAGE_LAST_MODIFIED", core.tz.DateTimeToString(pageDateTime));

            if (core.session.LoggedInMember != null)
            {
                if (owner is User && owner.Id != core.session.LoggedInMember.UserId)
                {
                    core.db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE page_item_id = {0} AND page_item_type = '{1}' AND page_id = '{2}';",
                        owner.Id, Mysql.Escape(owner.Type), thePage.PageId));
                }
            }

            if (thePage.License != null)
            {
                if (!string.IsNullOrEmpty(thePage.License.Title))
                {
                    core.template.Parse("PAGE_LICENSE", thePage.License.Title);
                }
                if (!string.IsNullOrEmpty(thePage.License.Icon))
                {
                    core.template.Parse("I_PAGE_LICENSE", thePage.License.Icon);
                }
                if (!string.IsNullOrEmpty(thePage.License.Link))
                {
                    core.template.Parse("U_PAGE_LICENSE", thePage.License.Link);
                }
            }

            switch (thePage.Classification)
            {
                case Classifications.Everyone:
                    core.template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                    core.template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                    break;
                case Classifications.Mature:
                    core.template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                    core.template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                    break;
                case Classifications.Restricted:
                    core.template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                    core.template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                    break;
            }

            core.template.Parse("PAGE_VIEWS", thePage.Views.ToString());

            List<string[]> breadCrumbParts = new List<string[]>();
            if (thePage.Parents != null)
            {
                foreach (ParentTreeNode ptn in thePage.Parents.Nodes)
                {
                    breadCrumbParts.Add(new string[] { ptn.ParentSlug.ToString(), ptn.ParentTitle });
                }
            }

            if (thePage.Id > 0)
            {
                breadCrumbParts.Add(new string[] { thePage.slug, thePage.Title });
            }

            owner.ParseBreadCrumbs(breadCrumbParts);

            if (thePage.PageAccess.CanEdit)
            {
                core.template.Parse("U_EDIT", Linker.BuildAccountSubModuleUri(owner, "pages", "write", "edit", thePage.PageId, true));
            }
        }

        public override long Id
        {
            get
            {
                return pageId;
            }
        }

        public override string Namespace
        {
            get
            {
                return "PAGE";
            }
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(string.Format("{0}{1}",
                    owner.UriStub, FullPath));
            }
        }

        /// <summary>
        /// Extracts the path to the parent of a page given it's full path
        /// </summary>
        /// <param name="path">Path to extract parent path from</param>
        /// <returns>Parent path extracted</returns>
        public static string GetParentPath(string path)
        {
            char[] trimStartChars = { '.', '/' };
            path = path.TrimEnd('/').TrimStart(trimStartChars);

            string[] paths = path.Split('/');

            return path.Remove(path.Length - paths[paths.Length - 1].Length).TrimEnd('/');
        }

        /// <summary>
        /// Extracts the slug of a page given it's full path
        /// </summary>
        /// <param name="path">Path to extract the slug from</param>
        /// <returns>Slug extracted</returns>
        public static string GetNameFromPath(string path)
        {
            char[] trimStartChars = { '.', '/' };
            path = path.TrimEnd('/').TrimStart(trimStartChars);

            string[] paths = path.Split('/');

            return paths[paths.Length - 1];
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
