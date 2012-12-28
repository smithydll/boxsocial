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

    [DataTable("user_pages", "PAGE")]
    [Permission("VIEW", "Can view the page", PermissionTypes.View)]
    [Permission("EDIT", "Can edit the page", PermissionTypes.CreateAndEdit)]
    public class Page : NumberedItem, INestableItem, IPermissibleItem
    {
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
        [DataField("page_parent_path", 1023)]
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
        private string pageIcon;
        [DataField("page_date_ut")]
        private long createdRaw;
        [DataField("page_modified_ut")]
        private long modifiedRaw;
        [DataField("page_classification")]
        private byte classificationId;
		[DataField("page_level")]
        private int pageLevel;
        [DataField("page_hierarchy", MYSQL_TEXT)]
        private string hierarchy;
		[DataField("page_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("page_simple_permissions")]
        private bool simplePermissions;

        private User creator;
        private Primitive owner;
        private Access access;
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
                return pageIcon;
            }
            set
            {
                SetProperty("pageIcon", value);
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public User Creator
        {
            get
            {
                if (creator == null || creatorId != creator.Id)
                {
                    core.PrimitiveCache.LoadUserProfile(creatorId);
                    creator = core.PrimitiveCache[creatorId];
                    //creator = (User)core.ItemCache[new ItemKey(creatorId, typeof(User))];
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
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    //owner = (Primitive)core.ItemCache[ownerKey];
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
		
		public ParentTree GetParents()
		{
			return Parents;
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
		
		public int Level
		{
			get
			{
				return pageLevel;
			}
			set
			{
				SetProperty("pageLevel", value);
			}
		}

        public long ParentTypeId
        {
            get
            {
                return ItemType.GetTypeId(typeof(Page));
            }
        }

        public string ParentPath
        {
            get
            {
                return parentPath;
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
            query.AddCondition("page_item_type_id", owner.TypeId);

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
            query.AddCondition("page_item_type_id", owner.TypeId);

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

        public Page(Core core, long pageId)
            : base(core)
        {
            this.db = db;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_id", pageId);

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
            query.AddCondition("page_item_type_id", owner.TypeId);

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
            /*ownerId = (long)pageRow["page_item_id"];
            ownerType = (string)pageRow["page_item_type"];*/
			ownerKey = new ItemKey((long)pageRow["page_item_id"], (long)pageRow["page_item_type_id"]);
            slug = (string)pageRow["page_slug"];
            title = (string)pageRow["page_title"];
            if (!(pageRow["page_text"] is DBNull))
            {
                body = (string)pageRow["page_text"];
            }
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
        }

        private void loadLicenseInfo(DataRow pageRow)
        {
            license = new ContentLicense(core, pageRow);
        }
		
		public List<Item> GetChildren()
		{
			List<Item> children = new List<Item>();
			
			// TODO: fill this method
			
			return children;
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
		
		public static Page Create(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, byte license, Classifications classification)
        {
            return Create(core, false, owner, title, ref slug, parent, pageBody, status, license, classification, null);
        }

        public static Page Create(Core core, bool suppress, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, byte license, Classifications classification)
        {
            return Create(core, suppress, owner, title, ref slug, parent, pageBody, status, license, classification, null);
        }
		
		public static Page Create(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, byte license, Classifications classification, ApplicationEntry application)
        {
			return Create(core, false, owner, title, ref slug, parent, pageBody, status, license, classification, null);
		}

        public static Page Create(Core core, bool suppress, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, byte license, Classifications classification, ApplicationEntry application)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

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
                throw new PageSlugNotUniqueException(slug);
            }

            if (string.IsNullOrEmpty(pageBody) && !pageListOnly)
            {
                throw new PageContentEmptyException();
            }

            // check the page slug for existance

            SelectQuery squery = new SelectQuery("user_pages");
            squery.AddFields("page_title");
            squery.AddCondition("page_item_id", owner.Id);
            squery.AddCondition("page_item_type_id", owner.TypeId);
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_slug", slug);
            squery.AddCondition("page_parent_id", parent);

            DataTable pagesTable = core.Db.Query(squery);

            if (pagesTable.Rows.Count > 0)
            {
                throw new PageSlugNotUniqueException(slug);
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
            squery.AddCondition("page_item_type_id", owner.TypeId);
            squery.AddSort(SortOrder.Ascending, "page_title");
            squery.LimitCount = 1;

            DataTable orderTable = core.Db.Query(squery);

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
                squery.AddCondition("page_item_type_id", owner.TypeId);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable2 = core.Db.Query(squery);

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
                squery.AddCondition("page_item_type_id", owner.TypeId);
                squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                orderTable = core.Db.Query(squery);

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
            uquery.AddCondition("page_item_type_id", owner.TypeId);

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

            core.Db.BeginTransaction();
            core.Db.Query(uquery);

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
            iquery.AddField("page_item_type_id", owner.TypeId);
            iquery.AddField("page_slug", slug);
            iquery.AddField("page_parent_path", parentPath);
            iquery.AddField("page_date_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("page_modified_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("page_title", title);
            iquery.AddField("page_ip", core.Session.IPAddress.ToString());
            iquery.AddField("page_text", pageBody);
            iquery.AddField("page_license", license);
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

            pageId = core.Db.Query(iquery);

            Page page = new Page(core, owner, pageId);

            /* LOAD THE DEFAULT ITEM PERMISSIONS */
            AccessControlPermission acpEdit = new AccessControlPermission(core, page.ItemKey.TypeId, "EDIT");
            AccessControlPermission acpView = new AccessControlPermission(core, page.ItemKey.TypeId, "VIEW");
            AccessControlGrant.Create(core, owner.ItemKey, page.ItemKey, acpEdit.PermissionId, AccessControlGrants.Allow);
            AccessControlGrant.Create(core, User.EveryoneGroupKey, page.ItemKey, acpView.PermissionId, AccessControlGrants.Allow);

            return page;
        }

        public void Update(Core core, Primitive owner, string title, ref string slug, long parent, string pageBody, PageStatus status, byte license, Classifications classification)
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
                throw new PageSlugNotUniqueException(slug);
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
            squery.AddCondition("page_item_type_id", owner.TypeId);
            squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);
            squery.AddCondition("page_slug", slug);
            squery.AddCondition("page_parent_id", parent);

            DataTable pagesTable = core.Db.Query(squery);

            if (pagesTable.Rows.Count > 0)
            {
                throw new PageSlugNotUniqueException(slug);
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
                squery.AddCondition("page_item_type_id", owner.TypeId);
                squery.AddSort(SortOrder.Ascending, "page_title");
                squery.LimitCount = 1;

                DataTable orderTable = core.Db.Query(squery);

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
                    squery.AddCondition("page_item_type_id", owner.TypeId);
                    squery.AddSort(SortOrder.Ascending, "page_title");
                    squery.LimitCount = 1;

                    DataTable orderTable2 = core.Db.Query(squery);

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
                    squery.AddCondition("page_item_type_id", owner.TypeId);
                    squery.AddCondition("page_id", ConditionEquality.NotEqual, pageId);

                    orderTable = core.Db.Query(squery);

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
                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND page_item_id = {1} AND page_item_type_id = {2}",
                        oldOrder, owner.Id, owner.TypeId));

                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND page_item_id = {1} AND page_item_type_id = {2}",
                    order, owner.Id, owner.TypeId));
            }

            UpdateQuery uquery = new UpdateQuery("user_pages");
            uquery.AddField("page_order", new QueryField("page_order + 1"));
            uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
            uquery.AddCondition("page_item_id", owner.Id);
            uquery.AddCondition("page_item_type_id", owner.TypeId);

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
            uquery.AddField("page_ip", core.Session.IPAddress.ToString());
            uquery.AddField("page_text", pageBody);
            uquery.AddField("page_license", license);
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
            uquery.AddCondition("page_item_type_id", owner.TypeId);

            db.Query(uquery);
        }

        public bool Delete(Core core, Primitive owner)
        {
            bool success = false;
            if (this.owner == owner)
            {
                UpdateQuery uquery = new UpdateQuery("user_pages");
                uquery.AddField("page_order", new QueryOperation("page_order", QueryOperations.Subtraction, 1));
                uquery.AddCondition("page_order", ConditionEquality.GreaterThanEqual, order);
                uquery.AddCondition("page_item_id", owner.Id);
                uquery.AddCondition("page_item_type_id", owner.TypeId);

                db.BeginTransaction();
                if (db.Query(uquery) >= 0)
                {
                    DeleteQuery dquery = new DeleteQuery("user_pages");
                    dquery.AddCondition("page_id", pageId);
                    dquery.AddCondition("page_item_id", owner.Id);
                    dquery.AddCondition("page_item_type_id", owner.TypeId);

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
                core.Functions.Generate404();
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
                core.Functions.Generate404();
            }
        }

        private static void Show(Core core, Primitive owner, Page thePage)
        {
            core.Template.SetTemplate("Pages", "viewpage");

            if (owner is User)
            {
                ((User)owner).LoadProfileInfo();
            }

            core.Display.ParsePageList(owner, true, thePage);

            if (!thePage.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            BoxSocial.Internals.Classification.ApplyRestrictions(core, thePage.Classification);

            core.Template.Parse("PAGE_TITLE", thePage.Title);
            if (owner is User)
            {
                core.Display.ParseBbcode("PAGE_BODY", thePage.Body, (User)owner);
            }
            else
            {
                core.Display.ParseBbcode("PAGE_BODY", thePage.Body);
            }
            DateTime pageDateTime = thePage.GetModifiedDate(core.Tz);
            core.Template.Parse("PAGE_LAST_MODIFIED", core.Tz.DateTimeToString(pageDateTime));

            if (core.Session.LoggedInMember != null)
            {
                if (owner is User && owner.Id != core.Session.LoggedInMember.UserId)
                {
                    core.Db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE page_item_id = {0} AND page_item_type_id = {1} AND page_id = '{2}';",
                        owner.Id, owner.TypeId, thePage.PageId));
                }
                else
                {
                    core.Db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE page_item_id = {0} AND page_item_type_id = {1} AND page_id = '{2}';",
                        owner.Id, owner.TypeId, thePage.PageId));
                }
            }

            if (thePage.License != null)
            {
                if (!string.IsNullOrEmpty(thePage.License.Title))
                {
                    core.Template.Parse("PAGE_LICENSE", thePage.License.Title);
                }
                if (!string.IsNullOrEmpty(thePage.License.Icon))
                {
                    core.Template.Parse("I_PAGE_LICENSE", thePage.License.Icon);
                }
                if (!string.IsNullOrEmpty(thePage.License.Link))
                {
                    core.Template.Parse("U_PAGE_LICENSE", thePage.License.Link);
                }
            }

            switch (thePage.Classification)
            {
                case Classifications.Everyone:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                    break;
                case Classifications.Mature:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                    break;
                case Classifications.Restricted:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                    break;
            }

            core.Template.Parse("PAGE_VIEWS", thePage.Views.ToString());

            List<string[]> breadCrumbParts = new List<string[]>();
            if (thePage.Parents != null)
            {
                foreach (ParentTreeNode ptn in thePage.Parents.Nodes)
                {
                    breadCrumbParts.Add(new string[] { ptn.ParentSlug, ptn.ParentTitle });
                }
            }

            if (thePage.Id > 0)
            {
                breadCrumbParts.Add(new string[] { thePage.slug, thePage.Title });
            }

            owner.ParseBreadCrumbs(breadCrumbParts);

            if (thePage.Access.Can("EDIT"))
            {
                core.Template.Parse("U_EDIT", core.Uri.BuildAccountSubModuleUri(owner, "pages", "write", "edit", thePage.PageId, true));
            }
        }

        public override long Id
        {
            get
            {
                return pageId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}{1}",
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

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                if (ParentId == 0)
                {
                    return Owner;
                }
                else
                {
                    return new Page(core, Owner, ParentId);
                }
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public bool GetDefaultCan(string permission)
        {
            switch (permission)
            {
                case "VIEW":
                    return true;
                default:
                    return false;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "Page: " + FullPath;
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
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
        public PageSlugNotUniqueException(string slug)
            : base(slug)
        {
        }
    }

    public class PageContentEmptyException : Exception
    {
    }

    public class PageOwnParentException : Exception
    {
    }
}
