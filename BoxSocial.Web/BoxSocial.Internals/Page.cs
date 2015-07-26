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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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
    [JsonObject("page")]
    public class Page : NumberedItem, INestableItem, IPermissibleItem, ICommentableItem
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
        [DataField("page_text_cache", MYSQL_MEDIUM_TEXT)]
        private string bodyCache;
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

        public event CommentHandler OnCommentPosted;

        [JsonProperty("id")]
        public long PageId
        {
            get
            {
                return pageId;
            }
        }

        [JsonProperty("application_id")]
        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }

        [JsonProperty("slug")]
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

        [JsonProperty("title")]
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

        [JsonProperty("body")]
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

        [JsonIgnore]
        public string BodyCache
        {
            get
            {
                return bodyCache;
            }
            set
            {
                SetProperty("bodyCache", value);
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
            }
        }

        [JsonProperty("owner")]
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

        [JsonIgnore]
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

        [JsonIgnore]
        public ContentLicense License
        {
            get
            {
                if (license == null && LicenseId > 0)
                {
                    license = new ContentLicense(core, LicenseId);
                }
                return license;
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
        public Classifications Classification
        {
            get
            {
                return classification;
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonProperty("order")]
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

        [JsonProperty("level")]
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

        [JsonIgnore]
        public long ParentTypeId
        {
            get
            {
                return ItemType.GetTypeId(core, typeof(Page));
            }
        }

        [JsonIgnore]
        public string ParentPath
        {
            get
            {
                return parentPath;
            }
        }

        [JsonProperty("path")]
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

        [JsonIgnore]
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

        [JsonProperty("time_created_ut")]
        public long TimeCreatedRaw
        {
            get
            {
                return createdRaw;
            }
        }

        [JsonProperty("time_modified_ut")]
        public long TimeModifiedRaw
        {
            get
            {
                return modifiedRaw;
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

            SelectQuery query = Page.GetSelectQueryStub(core, typeof(Page));
            query.AddCondition("page_slug", Page.GetNameFromPath(pageName));
            query.AddCondition("page_parent_path", Page.GetParentPath(pageName));
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);

            System.Data.Common.DbDataReader pageReader = db.ReaderQuery(query);

            if (pageReader.HasRows)
            {
                pageReader.Read();

                loadItemInfo(pageReader);
                try
                {
                    loadLicenseInfo(pageReader);
                }
                catch (InvalidLicenseException)
                {
                }
                pageReader.Close();
                pageReader.Dispose();
            }
            else
            {
                pageReader.Close();
                pageReader.Dispose();

                throw new PageNotFoundException();
            }
        }

        public Page(Core core, Primitive owner, string pageName, string pageParentPath)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            SelectQuery query = Page.GetSelectQueryStub(core, typeof(Page));
            query.AddCondition("page_slug", pageName);
            query.AddCondition("page_parent_path", pageParentPath);
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadItemInfo(pageTable.Rows[0]);
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

            SelectQuery query = Page.GetSelectQueryStub(core, typeof(Page));
            query.AddCondition("page_id", pageId);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadItemInfo(pageTable.Rows[0]);
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

            SelectQuery query = Page.GetSelectQueryStub(core, typeof(Page));
            query.AddCondition("page_id", pageId);
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);

            DataTable pageTable = db.Query(query);

            if (pageTable.Rows.Count == 1)
            {
                loadItemInfo(pageTable.Rows[0]);
                /*try
                {
                    loadLicenseInfo(pageTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }*/
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

        public Page(Core core, Primitive owner, System.Data.Common.DbDataReader pageRow)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Page_ItemLoad);

            loadItemInfo(pageRow);
        }

        private void Page_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(Page_CommentPosted);
        }

        bool Page_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                ApplicationEntry ae = core.GetApplication("Pages");
                ae.QueueNotifications(core, e.Comment.ItemKey, "notifyPageComment");
                /*ae.SendNotification(core, (User)Owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your page.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                    e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));*/
            }

            return true;
        }

        public static void NotifyPageComment(Core core, Job job)
        {
            Comment comment = new Comment(core, job.ItemId);
            Page ev = new Page(core, comment.CommentedItemKey.Id);
            ApplicationEntry ae = core.GetApplication("Pages");

            if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.OwnerKey)))
            {
                ae.SendNotification(core, comment.User, (User)ev.Owner, ev.OwnerKey, ev.ItemKey, "_COMMENTED_PAGE", comment.BuildUri(ev));
            }

            ae.SendNotification(core, comment.OwnerKey, comment.User, ev.OwnerKey, ev.ItemKey, "_COMMENTED_PAGE", comment.BuildUri(ev));
        }

        private void loadItemInfo(DataRow pageRow)
        {
            loadValue(pageRow, "page_id", out pageId);
            loadValue(pageRow, "user_id", out creatorId);
            loadValue(pageRow, "page_slug", out slug);
            loadValue(pageRow, "page_title", out title);
            loadValue(pageRow, "page_text", out body);
            loadValue(pageRow, "page_text_cache", out bodyCache);
            loadValue(pageRow, "page_license", out licenseId);
            loadValue(pageRow, "page_views", out views);
            loadValue(pageRow, "page_status", out status);
            loadValue(pageRow, "page_ip", out ipRaw);
            loadValue(pageRow, "page_ip_proxy", out ipProxyRaw);
            loadValue(pageRow, "page_parent_path", out parentPath);
            loadValue(pageRow, "page_order", out order);
            loadValue(pageRow, "page_parent_id", out parentId);
            loadValue(pageRow, "page_list_only", out listOnly);
            loadValue(pageRow, "page_application", out applicationId);
            loadValue(pageRow, "page_icon", out pageIcon);
            loadValue(pageRow, "page_date_ut", out createdRaw);
            loadValue(pageRow, "page_modified_ut", out modifiedRaw);
            loadValue(pageRow, "page_classification", out classificationId);
            loadValue(pageRow, "page_level", out pageLevel);
            loadValue(pageRow, "page_hierarchy", out hierarchy);
            loadValue(pageRow, "page_item", out ownerKey);
            loadValue(pageRow, "page_simple_permissions", out simplePermissions);

            itemLoaded(pageRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void loadItemInfo(System.Data.Common.DbDataReader pageRow)
        {
            loadValue(pageRow, "page_id", out pageId);
            loadValue(pageRow, "user_id", out creatorId);
            loadValue(pageRow, "page_slug", out slug);
            loadValue(pageRow, "page_title", out title);
            loadValue(pageRow, "page_text", out body);
            loadValue(pageRow, "page_text_cache", out bodyCache);
            loadValue(pageRow, "page_license", out licenseId);
            loadValue(pageRow, "page_views", out views);
            loadValue(pageRow, "page_status", out status);
            loadValue(pageRow, "page_ip", out ipRaw);
            loadValue(pageRow, "page_ip_proxy", out ipProxyRaw);
            loadValue(pageRow, "page_parent_path", out parentPath);
            loadValue(pageRow, "page_order", out order);
            loadValue(pageRow, "page_parent_id", out parentId);
            loadValue(pageRow, "page_list_only", out listOnly);
            loadValue(pageRow, "page_application", out applicationId);
            loadValue(pageRow, "page_icon", out pageIcon);
            loadValue(pageRow, "page_date_ut", out createdRaw);
            loadValue(pageRow, "page_modified_ut", out modifiedRaw);
            loadValue(pageRow, "page_classification", out classificationId);
            loadValue(pageRow, "page_level", out pageLevel);
            loadValue(pageRow, "page_hierarchy", out hierarchy);
            loadValue(pageRow, "page_item", out ownerKey);
            loadValue(pageRow, "page_simple_permissions", out simplePermissions);

            itemLoaded(pageRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void loadLicenseInfo(DataRow pageRow)
        {
            license = new ContentLicense(core, pageRow);
        }

        private void loadLicenseInfo(System.Data.Common.DbDataReader pageRow)
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

            string pageBodyCache = string.Empty;

            if (!pageBody.Contains("[user") && !pageBody.Contains("sid=true]"))
            {
                pageBodyCache = core.Bbcode.Parse(HttpUtility.HtmlEncode(pageBody), null, core.Session.LoggedInMember, true, string.Empty, string.Empty);
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
            iquery.AddField("page_text_cache", pageBodyCache);
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
            AccessControlGrant.Create(core, User.GetEveryoneGroupKey(core), page.ItemKey, acpView.PermissionId, AccessControlGrants.Allow);

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

            string pageBodyCache = string.Empty;

            if (!pageBody.Contains("[user") && !pageBody.Contains("sid=true]"))
            {
                pageBodyCache = core.Bbcode.Parse(HttpUtility.HtmlEncode(pageBody), null, core.Session.LoggedInMember, true, string.Empty, string.Empty);
            }

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
            uquery.AddField("page_text_cache", pageBodyCache);
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

        [JsonIgnore]
        public override long Id
        {
            get
            {
                return pageId;
            }
        }

        [JsonIgnore]
        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}{1}",
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

        [JsonIgnore]
        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        [JsonIgnore]
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

        [JsonIgnore]
        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            switch (permission)
            {
                case "VIEW":
                    return true;
                default:
                    return false;
            }
        }

        [JsonIgnore]
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

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        [JsonIgnore]
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        [JsonIgnore]
        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        [JsonIgnore]
        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        [JsonIgnore]
        public string Noun
        {
            get
            {
                return "page";
            }
        }

        public bool CanComment
        {
            get
            {
                return Access.Can("COMMENT");
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
