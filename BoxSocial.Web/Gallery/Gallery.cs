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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a gallery
    /// </summary>
    [DataTable("user_galleries")]
    [Permission("VIEW", "Can view the gallery", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on the gallery", PermissionTypes.Interact)]
    [Permission("CREATE_CHILD", "Can create child galleries", PermissionTypes.CreateAndEdit)]
    [Permission("VIEW_ITEMS", "Can view gallery photos", PermissionTypes.View)]
    [Permission("COMMENT_ITEMS", "Can comment on the photos", PermissionTypes.Interact)]
    [Permission("RATE_ITEMS", "Can rate the photos", PermissionTypes.Interact)]
    [Permission("CREATE_ITEMS", "Can upload photos to gallery", PermissionTypes.CreateAndEdit)]
    [Permission("EDIT_ITEMS", "Can edit photos", PermissionTypes.CreateAndEdit)]
    [Permission("DELETE_ITEMS", "Can delete photos", PermissionTypes.Delete)]
    [Permission("DOWNLOAD_ORIGINAL", "Can download the original photo. This will include EXIF data which may include personally identifiable information.", PermissionTypes.View)]
    public class Gallery : NumberedItem, IPermissibleItem, INestableItem, ICommentableItem, ILikeableItem, IActionableItem
    {

        /// <summary>
        /// Owner of the gallery
        /// </summary>
        private Primitive owner;

        /// <summary>
        /// Id of the gallery
        /// </summary>
        [DataField("gallery_id", DataFieldKeys.Primary)]
        private long galleryId;

        /// <summary>
        /// User Id (usergallery)
        /// </summary>
        [DataField("user_id")]
        private long userId;

        [DataField("settings_id")]
        private long settingsId;
        
        /// <summary>
        /// Id of the parent gallery
        /// </summary>
        [DataField("gallery_parent_id")]
        private long parentId;

        /// <summary>
        /// Gallery title
        /// </summary>
        [DataField("gallery_title", 63)]
        private string galleryTitle;

        /// <summary>
        /// Gallery parent path
        /// </summary>
        [DataField("gallery_parent_path", MYSQL_TEXT)]
        private string parentPath;

        /// <summary>
        /// Gallery path (slug)
        /// </summary>
        [DataField("gallery_path", 31)]
        private string path;

        /// <summary>
        /// Number of gallery items comments
        /// </summary>
        [DataField("gallery_item_comments")]
        private long galleryItemComments;

        /// <summary>
        /// Number of visits made to the gallery
        /// </summary>
        [DataField("gallery_visits")]
        private long visits;

        /// <summary>
        /// Number of photos in the gallery
        /// </summary>
        [DataField("gallery_items")]
        private long items;

        /// <summary>
        /// Number of bytes the the photos in the gallery consume
        /// </summary>
        [DataField("gallery_bytes")]
        private long bytes;

        /// <summary>
        /// Gallery abstract
        /// </summary>
        [DataField("gallery_abstract", MYSQL_TEXT)]
        private string galleryAbstract;

        /// <summary>
        /// Gallery Description
        /// </summary>
        [DataField("gallery_description", 255)]
        private string galleryDescription;

        /// <summary>
        /// Id of the highlighted photo
        /// </summary>
        [DataField("gallery_highlight_id")]
        private long highlightId;

        /// <summary>
        /// URI of the highlighted photo
        /// </summary>
        private string highlightUri;

        /// <summary>
        /// Hierarchy
        /// </summary>
        [DataField("gallery_hierarchy", MYSQL_TEXT)]
        private string hierarchy;
        
        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_level")]
        private int galleryLevel;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_order")]
        private int galleryOrder;

        /// <summary>
        /// 
        /// </summary>
        [DataField("app_simple_permissions")]
        private bool simplePermissions;

        /// <summary>
        /// Mark the gallery as a system gallery, it cannot be deleted as long as an application is attached to it
        /// </summary>
        [DataField("gallery_application")]
        private long galleryApplication;

        /// <summary>
        /// Parent Tree
        /// </summary>
        private ParentTree parentTree;

        /// <summary>
        /// Access object for the gallery
        /// </summary>
        Access access;
        List<AccessControlPermission> permissionsList;

        private GallerySettings settings;
        private GalleryItem highlightItem;

        public event CommentHandler OnCommentPosted;

        /// <summary>
        /// Gets the gallery Id
        /// </summary>
        public long GalleryId
        {
            get
            {
                return galleryId;
            }
        }

        /// <summary>
        /// Gets the gallery parent Id
        /// </summary>
        public long ParentId
        {
            get
            {
                return parentId;
            }
            set
            {
                SetProperty("parentId", value);

                if (parentId > 0 && this.GetType() == typeof(Gallery))
                {
                    Gallery parent = new Gallery(core, (User)owner, parentId);

                    parentTree = new ParentTree();

                    foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                    }

                    if (parent.Id > 0)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(parent.GalleryTitle, parent.Path, parent.Id));
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

        /// <summary>
        /// Get the parents
        /// </summary>
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

        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
            }
        }

        /// <summary>
        /// Gets the owner of the gallery
        /// </summary>
        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        /// <summary>
        /// Gets the gallery title
        /// </summary>
        public string GalleryTitle
        {
            get
            {
                return galleryTitle;
            }
        }


        /// <summary>
        /// Gets the gallery parent path
        /// </summary>
        public string ParentPath
        {
            get
            {
                return parentPath;
            }
            set
            {
                SetProperty("parentPath", value);
            }
        }

        /// <summary>
        /// Gets the gallery path (slug)
        /// </summary>
        public string Path
        {
            get
            {
                return path;
            }
        }

        /// <summary>
        /// Gets the gallery full path
        /// </summary>
        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(parentPath))
                {
                    return path;
                }
                else
                {
                    return parentPath + "/" + path;
                }
            }
        }

        /// <summary>
        /// Gets the number of visits made to the gallery
        /// </summary>
        public long Visits
        {
            get
            {
                return visits;
            }
        }

        /// <summary>
        /// Gets the number of items in the gallery
        /// </summary>
        public long Items
        {
            get
            {
                return items;
            }
            /*set
            {
                SetProperty("items", value);
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        public long ItemComment
        {
            get
            {
                return galleryItemComments;
            }
        }

        /// <summary>
        /// Gets the number of bytes consumed by the items in the gallery
        /// </summary>
        public long Bytes
        {
            get
            {
                return bytes;
            }
            set
            {
                SetProperty("bytes", value);
            }
        }

        /// <summary>
        /// Gets the gallery abstract
        /// </summary>
        public string GalleryAbstract
        {
            get
            {
                return galleryAbstract;
            }
        }

        /// <summary>
        /// Gets the gallery description
        /// </summary>
        public string GalleryDescription
        {
            get
            {
                return galleryDescription;
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item URI
        /// </summary>
        [Obsolete("The preferred method is to use the highlight ID to load the item and check it's permissions.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string HighlightUri
        {
            get
            {
                return highlightUri;
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item ID
        /// </summary>
        public long HighlightId
        {
            get
            {
                return highlightId;
            }
            set
            {
                SetProperty("highlightId", value);
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string TinyUri
        {
            get
            {
                if (HighlightId > 0)
                {
                    if (this.Access.Can("VIEW_ITEMS"))
                    {
                        if (Items == 0)
                        {
                            return "FALSE";
                        }
                        else if (HighlightItem != null)
                        {
                            try
                            {
                                if (HighlightItem.TinyUri != null)
                                {
                                    return HighlightItem.TinyUri;
                                }
                                else
                                {
                                    return "FALSE";
                                }
                            }
                            catch (GalleryItemNotFoundException)
                            {
                                return "FALSE";
                            }
                        }
                        else
                        {
                            return "FALSE";
                        }
                    }
                    else
                    {
                        return "FALSE";
                    }
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string ThumbnailUri
        {
            get
            {
                if (HighlightId > 0)
                {
                    if (this.Access.Can("VIEW_ITEMS"))
                    {
                        if (Items == 0)
                        {
                            return "FALSE";
                        }
                        else if (HighlightItem != null)
                        {
                            try
                            {
                                if (HighlightItem.ThumbnailUri != null)
                                {
                                    return HighlightItem.ThumbnailUri;
                                }
                                else
                                {
                                    return "FALSE";
                                }
                            }
                            catch (GalleryItemNotFoundException)
                            {
                                return "FALSE";
                            }
                        }
                        else
                        {
                            return "FALSE";
                        }
                    }
                    else
                    {
                        return "FALSE";
                    }
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string IconUri
        {
            get
            {
                if (this.Access.Can("VIEW_ITEMS"))
                {
                    if (HighlightItem != null)
                    {
                        try
                        {
                            if (HighlightItem.IconUri != null)
                            {
                                return HighlightItem.IconUri;
                            }
                            else
                            {
                                return "FALSE";
                            }
                        }
                        catch (GalleryItemNotFoundException)
                        {
                            return "FALSE";
                        }
                    }
                    else
                    {
                        return "FALSE";
                    }
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string TileUri
        {
            get
            {
                if (this.Access.Can("VIEW_ITEMS"))
                {
                    if (HighlightItem != null)
                    {
                        try
                        {
                            if (HighlightItem.TileUri != null)
                            {
                                return HighlightItem.TileUri;
                            }
                            else
                            {
                                return "FALSE";
                            }
                        }
                        catch (GalleryItemNotFoundException)
                        {
                            return "FALSE";
                        }
                    }
                    else
                    {
                        return "FALSE";
                    }
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string SquareUri
        {
            get
            {
                if (this.Access.Can("VIEW_ITEMS"))
                {
                    if (HighlightItem != null)
                    {
                        try
                        {
                            if (HighlightItem.SquareUri != null)
                            {
                                return HighlightItem.SquareUri;
                            }
                            else
                            {
                                return "FALSE";
                            }
                        }
                        catch (GalleryItemNotFoundException)
                        {
                            return "FALSE";
                        }
                    }
                    else
                    {
                        return "FALSE";
                    }
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        public Gallery(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;
            this.ownerKey = owner.ItemKey;

            galleryId = 0;
            path = "";
            parentPath = "";

            if (owner is User)
            {
                userId = owner.Id;
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="settings">Gallery Settings</param>
        public Gallery(Core core, GallerySettings settings)
            : base(core)
        {
            this.settings = settings;
            this.owner = settings.Owner;
            this.ownerKey = owner.ItemKey;

            galleryId = 0;
            path = "";
            parentPath = "";

            if (owner is User)
            {
                userId = owner.Id;
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryId">Gallery Id</param>
        public Gallery(Core core, Primitive owner, long galleryId)
            : base(core)
        {
            this.owner = owner;

            if (galleryId > 0)
            {
                ItemLoad += new ItemLoadHandler(Gallery_ItemLoad);

                try
                {
                    LoadItem(galleryId);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidGalleryException();
                }
            }
            else
            {
                this.galleryId = 0;
                this.path = "";
                this.parentPath = "";
                this.userId = owner.Id;
                this.ownerKey = owner.ItemKey;
            }
        }
        
                /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="galleryId">Gallery Id</param>
        public Gallery(Core core, long galleryId)
            : base(core)
        {
            if (galleryId > 0)
            {
                ItemLoad += new ItemLoadHandler(Gallery_ItemLoad);

                try
                {
                    LoadItem(galleryId);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidGalleryException();
                }
            }
            else
            {
                throw new InvalidGalleryException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="path">Gallery path</param>
        public Gallery(Core core, Primitive owner, string path)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Gallery_ItemLoad);

            SelectQuery query = Gallery.GetSelectQueryStub(core, typeof(Gallery));
            query.AddCondition("gallery_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_path", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_id", owner.Id);
            query.AddCondition("gallery_item_type_id", owner.TypeId);

            System.Data.Common.DbDataReader galleryReader = db.ReaderQuery(query);

            if (galleryReader.HasRows)
            {
                galleryReader.Read();

                loadItemInfo(galleryReader);

                galleryReader.Close();
                galleryReader.Dispose();
            }
            else
            {
                galleryReader.Close();
                galleryReader.Dispose();

                throw new InvalidGalleryException();
            }
        }

        public Gallery(Core core, DataRow galleryRow)
            : base(core)
        {
            try
            {
                loadItemInfo(galleryRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGalleryException();
            }
        }

        public Gallery(Core core, System.Data.Common.DbDataReader galleryRow)
            : base(core)
        {
            try
            {
                loadItemInfo(galleryRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGalleryException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryRow">Raw data row of gallery</param>
        /// <param name="hasIcon">True if contains raw data for icon</param>
        public Gallery(Core core, Primitive owner, DataRow galleryRow, bool hasIcon)
            : base(core)
        {
            this.owner = owner;

            try
            {
                loadItemInfo(galleryRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGalleryException();
            }

            if (hasIcon)
            {
                loadGalleryIcon(galleryRow);
            }
        }

        public Gallery(Core core, Primitive owner, System.Data.Common.DbDataReader galleryRow, bool hasIcon)
            : base(core)
        {
            this.owner = owner;

            try
            {
                loadItemInfo(galleryRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGalleryException();
            }

            if (hasIcon)
            {
                loadGalleryIcon(galleryRow);
            }
        }

        protected override void loadItemInfo(DataRow galleryRow)
        {
            loadValue(galleryRow, "gallery_id", out galleryId);
            loadValue(galleryRow, "user_id", out userId);
            loadValue(galleryRow, "settings_id", out settingsId);
            loadValue(galleryRow, "gallery_parent_id", out parentId);
            loadValue(galleryRow, "gallery_title", out galleryTitle);
            loadValue(galleryRow, "gallery_parent_path", out parentPath);
            loadValue(galleryRow, "gallery_path", out path);
            loadValue(galleryRow, "gallery_item_comments", out galleryItemComments);
            loadValue(galleryRow, "gallery_visits", out visits);
            loadValue(galleryRow, "gallery_items", out items);
            loadValue(galleryRow, "gallery_bytes", out bytes);
            loadValue(galleryRow, "gallery_abstract", out galleryAbstract);
            loadValue(galleryRow, "gallery_description", out galleryDescription);
            loadValue(galleryRow, "gallery_highlight_id", out highlightId);
            loadValue(galleryRow, "gallery_hierarchy", out hierarchy);
            loadValue(galleryRow, "gallery_item", out ownerKey);
            loadValue(galleryRow, "gallery_level", out galleryLevel);
            loadValue(galleryRow, "gallery_order", out galleryOrder);
            loadValue(galleryRow, "app_simple_permissions", out simplePermissions);
            loadValue(galleryRow, "gallery_application", out galleryApplication);

            itemLoaded(galleryRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader galleryRow)
        {
            loadValue(galleryRow, "gallery_id", out galleryId);
            loadValue(galleryRow, "user_id", out userId);
            loadValue(galleryRow, "settings_id", out settingsId);
            loadValue(galleryRow, "gallery_parent_id", out parentId);
            loadValue(galleryRow, "gallery_title", out galleryTitle);
            loadValue(galleryRow, "gallery_parent_path", out parentPath);
            loadValue(galleryRow, "gallery_path", out path);
            loadValue(galleryRow, "gallery_item_comments", out galleryItemComments);
            loadValue(galleryRow, "gallery_visits", out visits);
            loadValue(galleryRow, "gallery_items", out items);
            loadValue(galleryRow, "gallery_bytes", out bytes);
            loadValue(galleryRow, "gallery_abstract", out galleryAbstract);
            loadValue(galleryRow, "gallery_description", out galleryDescription);
            loadValue(galleryRow, "gallery_highlight_id", out highlightId);
            loadValue(galleryRow, "gallery_hierarchy", out hierarchy);
            loadValue(galleryRow, "gallery_item", out ownerKey);
            loadValue(galleryRow, "gallery_level", out galleryLevel);
            loadValue(galleryRow, "gallery_order", out galleryOrder);
            loadValue(galleryRow, "app_simple_permissions", out simplePermissions);
            loadValue(galleryRow, "gallery_application", out galleryApplication);

            itemLoaded(galleryRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Gallery_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(Gallery_CommentPosted);
            ItemDeleted += new ItemDeletedEventHandler(Gallery_ItemDeleted);
        }

        void Gallery_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            ActionableItem.CleanUp(core, this);
        }

        bool Gallery_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                core.CallingApplication.QueueNotifications(core, e.Comment.ItemKey, "notifyGalleryComment");
                /*core.CallingApplication.SendNotification(core, (User)Owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your gallery.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                    e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));*/
            }

            return true;
        }

        public static void NotifyGalleryComment(Core core, Job job)
        {
            Comment comment = new Comment(core, job.ItemId);
            Gallery ev = new Gallery(core, comment.CommentedItemKey.Id);

            if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.OwnerKey)))
            {
                core.CallingApplication.SendNotification(core, comment.User, (User)ev.Owner, ev.OwnerKey, ev.ItemKey, "_COMMENTED_GALLERY", comment.BuildUri(ev));
            }

            core.CallingApplication.SendNotification(core, comment.OwnerKey, comment.User, ev.OwnerKey, ev.ItemKey, "_COMMENTED_GALLERY", comment.BuildUri(ev));
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        /// <summary>
        /// Loads the database information into the Gallery class object.
        /// </summary>
        /// <param name="galleryRow">Raw data row of blog entry</param>
        protected void loadGalleryInfo(DataRow galleryRow)
        {
            galleryId = (long)galleryRow["gallery_id"];
            parentId = (long)galleryRow["gallery_parent_id"];
            galleryTitle = (string)galleryRow["gallery_title"];
            if (!(galleryRow["gallery_abstract"] is System.DBNull))
            {
                galleryAbstract = (string)galleryRow["gallery_abstract"];
            }
            galleryDescription = (string)galleryRow["gallery_description"];
            items = (long)galleryRow["gallery_items"];
            bytes = (long)galleryRow["gallery_bytes"];
            visits = (long)galleryRow["gallery_visits"];
            path = (string)galleryRow["gallery_path"];
            parentPath = (string)galleryRow["gallery_parent_path"];
            userId = (long)galleryRow["user_id"];
        }

        /// <summary>
        /// Loads the icon information into the Gallery class object.
        /// </summary>
        /// <param name="galleryRow">Raw data row of blog entry</param>
        protected void loadGalleryIcon(DataRow galleryRow)
        {
            if (!(galleryRow["gallery_item_parent_path"] is DBNull))
            {
                highlightItem = new GalleryItem(core, galleryRow);
            }
            if (!(galleryRow["gallery_item_uri"] is DBNull))
            {
                highlightUri = (string)galleryRow["gallery_item_uri"];
            }
            else
            {
                highlightUri = null;
            }
        }

        protected void loadGalleryIcon(System.Data.Common.DbDataReader galleryRow)
        {
            if (!(galleryRow["gallery_item_parent_path"] is DBNull))
            {
                highlightItem = new GalleryItem(core, galleryRow);
            }
            if (!(galleryRow["gallery_item_uri"] is DBNull))
            {
                highlightUri = (string)galleryRow["gallery_item_uri"];
            }
            else
            {
                highlightUri = null;
            }
        }

        /// <summary>
        /// Returns a list of sub-galleries
        /// </summary>
        /// <returns>A list of sub-galleries</returns>
        public List<Gallery> GetGalleries()
		{
			List<Gallery> items = new List<Gallery>();

            System.Data.Common.DbDataReader galleryReader = core.Db.ReaderQuery(GetGalleryQuery(core));

            while (galleryReader.Read())
            {
                items.Add(new Gallery(core, owner, galleryReader, true));
            }

            galleryReader.Close();
            galleryReader.Dispose();

            return items;
		}

        /// <summary>
        /// Returns raw data for a list of sub-galleries
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>Raw data for a list of sub-galleries</returns>
        protected SelectQuery GetGalleryQuery(Core core)
        {
            long loggedIdUid = User.GetMemberId(core.Session.LoggedInMember);
            //ushort readAccessLevel = owner.GetAccessLevel(core.Session.LoggedInMember);

            SelectQuery query = Gallery.GetSelectQueryStub(core, typeof(Gallery));
            query.AddFields(GalleryItem.GetFieldsPrefixed(core, typeof(GalleryItem)));
            query.AddJoin(JoinTypes.Left, new DataField(typeof(Gallery), "gallery_highlight_id"), new DataField(typeof(GalleryItem), "gallery_item_id"));
            /*query.AddFields(GalleryItem.GetFieldsPrefixed(typeof(ContentLicense)));
            query.AddJoin(JoinTypes.Left, new DataField(typeof(GalleryItem), "gallery_item_license"), new DataField(typeof(ContentLicense), "license_id"));*/
            query.AddCondition("gallery_parent_id", Id);
            query.AddCondition("`user_galleries`.`gallery_item_id`", owner.Id);
            query.AddCondition("`user_galleries`.`gallery_item_type_id`", owner.TypeId);

            return query;
        }

        /// <summary>
        /// Returns a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>A list of photos</returns>
        public List<GalleryItem> GetItems(Core core)
		{
			List<GalleryItem> items = new List<GalleryItem>();

            SelectQuery query = GetItemQuery(core);

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);

            while (reader.Read())
            {
                items.Add(new GalleryItem(core, owner, reader));
            }

            reader.Close();
            reader.Dispose();

            return items;
		}

        /// <summary>
        /// Returns a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Photos per page</param>
        /// <returns>A list of photos</returns>
        public List<GalleryItem> GetItems(Core core, int currentPage, int perPage, long currentOffset)
		{
			List<GalleryItem> items = new List<GalleryItem>();

            SelectQuery query = GetItemQuery(core, currentPage, perPage, currentOffset);

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);

            while (reader.Read())
            {
                items.Add(new GalleryItem(core, owner, reader));
            }

            reader.Close();
            reader.Dispose();

            return items;
		}

        /// <summary>
        /// Returns raw data for a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>Raw data for a list of gallery photos</returns>
        protected SelectQuery GetItemQuery(Core core)
        {
            return GetItemQuery(core, 1, 12, 0);
        }

        /// <summary>
        /// Returns raw data for a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <returns>Raw data for a list of gallery photos</returns>
        protected SelectQuery GetItemQuery(Core core, int currentPage, int perPage, long currentOffset)
        {
            db = core.Db;

            //ushort readAccessLevel = owner.GetAccessLevel(core.Session.LoggedInMember);
            long loggedIdUid = User.GetMemberId(core.Session.LoggedInMember);

            SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
            query.AddCondition("gallery_id", galleryId);
            query.AddCondition("gallery_item_item_id", owner.Id);
            QueryCondition qc1 = query.AddCondition("gallery_item_item_type_id", owner.TypeId);
            if (currentOffset > 0)
            {
                // Ascending order
                query.AddCondition("gallery_item_id", ConditionEquality.GreaterThan, currentOffset);
            }
            else if (currentPage > 1)
            {
                query.LimitStart = (currentPage - 1) * perPage;
            }
            query.LimitCount = perPage;

            return query;
        }

        /// <summary>
        /// Updates data for the gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="title">New gallery title</param>
        /// <param name="slug">New gallery slug</param>
        /// <param name="description">New gallery description</param>
        /// <param name="permissions">New gallery permission mask</param>
        public void Update(Core core, string title, string slug, string description)
        {
            if (GalleryId == 0)
            {
                throw new GalleryCannotUpdateRootGalleryException();
            }

            if (owner is User)
            {
                if (core.LoggedInMemberId != ((User)owner).UserId)
                {
                    throw new GalleryPermissionException();
                }
            }
            else
            {
                throw new GalleryNotAMemberObjectException();
            }

            User member = (User)this.owner;

            // do we have to generate a new slug
            if (slug != path) // || parent.ParentPath != ParentPath) // we can't move galleries between parents at the moment
            {
                // ensure we have generated a valid slug
                if (!Gallery.CheckGallerySlugValid(slug))
                {
                    throw new GallerySlugNotValidException();
                }

                if (!Gallery.CheckGallerySlugUnique(core.Db, member, parentPath, slug))
                {
                    throw new GallerySlugNotUniqueException();
                }

                string oldPath = "";
                string newPath = "";
                if (string.IsNullOrEmpty(parentPath))
                {
                    oldPath = path;
                    newPath = slug;
                }
                else
                {
                    oldPath = FullPath;
                    newPath = parentPath + "/" + slug;
                }

                // update the children
                updateParentPathChildren(oldPath, slug);
            }

            db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_title = '{2}', gallery_abstract = '{3}', gallery_path = '{4}'WHERE user_id = {0} AND gallery_id = {1}",
                member.UserId, galleryId, Mysql.Escape(title), Mysql.Escape(description), Mysql.Escape(slug)));
        }

        /// <summary>
        /// Updates the parent path for children galleries and photos
        /// </summary>
        /// <param name="oldPath">Old parent path</param>
        /// <param name="newPath">New parent path</param>
        private void updateParentPathChildren(string oldPath, string newPath)
        {
            if (owner is User)
            {
                List<Gallery> galleries = ((Gallery)this).GetGalleries();

                foreach (Gallery gallery in galleries)
                {
                    ParentTree parentTree = new ParentTree();

                    if (this.Parents != null)
                    {
                        foreach (ParentTreeNode ptn in this.Parents.Nodes)
                        {
                            parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                        }
                    }

                    if (this.Id > 0)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(this.GalleryTitle, this.Path, this.Id));
                    }

                    XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
                    StringBuilder sb = new StringBuilder();
                    StringWriter stw = new StringWriter(sb);

                    xs.Serialize(stw, parentTree);
                    stw.Flush();
                    stw.Close();

                    db.BeginTransaction();
                    UpdateQuery uQuery = new UpdateQuery("user_galleries");
                    uQuery.AddField("gallery_hierarchy", sb.ToString());
                    uQuery.AddField("gallery_parent_path", newPath);
                    uQuery.AddCondition("gallery_id", gallery.Id);

                    db.Query(uQuery);

                    gallery.updateParentPathChildren(gallery.FullPath, newPath + "/" + gallery.Path);
                }

                {
                    UpdateQuery uQuery = new UpdateQuery("gallery_items");
                    uQuery.AddField("gallery_item_parent_path", newPath);
                    uQuery.AddCondition("gallery_id", Id);
                    uQuery.AddCondition("user_id", owner.Id);

                    db.Query(uQuery);
                }
            }
            else
            {
                throw new GalleryCannotUpdateChildrenException();
            }
        }

        /// <summary>
        /// Creates a new gallery for the logged in user.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="parent">Parent gallery</param>
        /// <param name="title">Gallery title</param>
        /// <param name="slug">Gallery slug</param>
        /// <param name="description">Gallery description</param>
        /// <param name="permissions">Gallery permission mask</param>
        /// <returns>An instance of the newly created gallery</returns>
        public static Gallery Create(Core core, Primitive owner, Gallery parent, string title, ref string slug, string description)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            GallerySettings settings = null;
            try
            {
                settings = new GallerySettings(core, owner);
            }
            catch (InvalidGallerySettingsException)
            {
                settings = GallerySettings.Create(core, owner);
            }

            string parents = "";
            // ensure we have generated a valid slug
            slug = Gallery.GetSlugFromTitle(title, slug);

            if (!Gallery.CheckGallerySlugValid(slug))
            {
                throw new GallerySlugNotValidException();
            }

            if (!Gallery.CheckGallerySlugUnique(core.Db, parent.owner, parent.FullPath, slug))
            {
                throw new GallerySlugNotUniqueException();
            }

            if (parent != null)
            {
                ParentTree parentTree = new ParentTree();

                if (parent.Parents != null)
                {
                    foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentSlug, ptn.ParentId));
                    }
                }

                if (parent.Id > 0)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(parent.GalleryTitle, parent.Path, parent.Id));
                }

                XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
                StringBuilder sb = new StringBuilder();
                StringWriter stw = new StringWriter(sb);

                xs.Serialize(stw, parentTree);
                stw.Flush();
                stw.Close();

                parents = sb.ToString();
            }

            InsertQuery iQuery = new InsertQuery("user_galleries");
            iQuery.AddField("gallery_title", title);
            iQuery.AddField("gallery_abstract", description);
            iQuery.AddField("gallery_path", slug);
            iQuery.AddField("gallery_parent_path", parent.FullPath);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("settings_id", settings.Id);
            iQuery.AddField("gallery_item_id", owner.Id);
            iQuery.AddField("gallery_item_type_id", owner.TypeId);
            iQuery.AddField("gallery_parent_id", parent.GalleryId);
            iQuery.AddField("gallery_bytes", 0);
            iQuery.AddField("gallery_items", 0);
            iQuery.AddField("gallery_item_comments", 0);
            iQuery.AddField("gallery_visits", 0);
            iQuery.AddField("gallery_hierarchy", parents);

            long galleryId = core.Db.Query(iQuery);

            Gallery gallery = new Gallery(core, owner, galleryId);

            /* LOAD THE DEFAULT ITEM PERMISSIONS */
            Access.CreateAllGrantsForOwner(core, gallery);
            Access.CreateGrantForPrimitive(core, gallery, User.EveryoneGroupKey, "VIEW");
            Access.CreateGrantForPrimitive(core, gallery, Friend.FriendsGroupKey, "COMMENT");
            Access.CreateGrantForPrimitive(core, gallery, User.EveryoneGroupKey, "VIEW_ITEMS");
            Access.CreateGrantForPrimitive(core, gallery, Friend.FriendsGroupKey, "COMMENT_ITEMS");
            Access.CreateGrantForPrimitive(core, gallery, Friend.FriendsGroupKey, "RATE_ITEMS");

            return gallery;
        }

        /// <summary>
        /// Deletes a gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="gallery">Gallery to delete</param>
        public static void Delete(Core core, Gallery gallery)
        {
            long[] stuffDeleted = galleryDeleteChildren(core, gallery);
            long itemsDeleted = stuffDeleted[0];
            long bytesDeleted = stuffDeleted[1];

            // comitt the transaction
            core.Db.UpdateQuery(string.Format("UPDATE user_info SET user_gallery_items = user_gallery_items - {1}, user_bytes = user_bytes - {2} WHERE user_id = {1}",
                core.Session.LoggedInMember.UserId, itemsDeleted, bytesDeleted));
        }

        /// <summary>
        /// Delete all children of a gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="gallery">Gallery being deleted</param>
        /// <returns>An array containing the number of gallery photos deleted
        /// (index 0), and the number of bytes consumed by said photos
        /// (index 1).</returns>
        private static long[] galleryDeleteChildren(Core core, Gallery gallery)
        {
            long itemsDeleted = 0; // index 0
            long bytesDeleted = 0; // index 1

            List<Gallery> galleries = gallery.GetGalleries();

            foreach (Gallery galleryGallery in galleries)
            {
                long[] stuffDeleted = galleryDeleteChildren(core, galleryGallery);
                itemsDeleted += stuffDeleted[0];
                bytesDeleted += stuffDeleted[1];
            }

            object objectsDeleted = core.Db.Query(string.Format("SELECT SUM(gallery_item_bytes) AS bytes_deleted FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}';",
                    core.LoggedInMemberId, Mysql.Escape(gallery.FullPath))).Rows[0]["bytes_deleted"];

            if (!(objectsDeleted is DBNull))
            {
                bytesDeleted += (long)(decimal)objectsDeleted;
            }

            core.Db.BeginTransaction();
            itemsDeleted += core.Db.UpdateQuery(string.Format("DELETE FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}'",
                core.Session.LoggedInMember.UserId, Mysql.Escape(gallery.FullPath)));

            core.Db.UpdateQuery(string.Format("DELETE FROM user_galleries WHERE user_id = {0} AND gallery_id = {1}",
                core.Session.LoggedInMember.UserId, gallery.GalleryId));
            return new long[] { itemsDeleted, bytesDeleted };
        }

        /// <summary>
        /// Extracts the path to the parent of a gallery given it's full path
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
        /// Extracts the slug of a gallery given it's full path
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

        /// <summary>
        /// Checks a given gallery slug to ensure uniqueness
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="parentFullPath">Parent path</param>
        /// <param name="slug">Slug to check for uniqueness</param>
        /// <returns>True if slug is unique given owner and parent</returns>
        public static bool CheckGallerySlugUnique(Mysql db, Primitive owner, string parentFullPath, string slug)
        {
            DataTable galleryGalleryTable = db.Query(string.Format("SELECT gallery_path FROM user_galleries WHERE gallery_item_id = {0} AND gallery_item_type_id = {1} AND gallery_parent_path = '{2}' AND gallery_path = '{3}';",
                        owner.Id, owner.TypeId, Mysql.Escape(parentFullPath), Mysql.Escape(slug)));

            if (galleryGalleryTable.Rows.Count > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks a given gallery slug to ensure validity
        /// </summary>
        /// <param name="slug">Slug to check for validity</param>
        /// <returns>True is slug is value</returns>
        public static bool CheckGallerySlugValid(string slug)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            disallowedNames.Add("upload");
            disallowedNames.Add("comments");
            disallowedNames.Add("page");
            disallowedNames.Add("favourites");
            disallowedNames.Add("from-posts");

            if (disallowedNames.BinarySearch(slug.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(slug, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            slug = slug.Normalize().ToLower();

            if (slug.Length < 2)
            {
                matches++;
            }

            if (slug.Length > 64)
            {
                matches++;
            }

            if (slug.EndsWith(".aspx", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".asax", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".php", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".html", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".gif", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".png", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".js", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".bmp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".jpg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".jpeg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".zip", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".jsp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".cfm", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".exe", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".jpeg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".jpg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".mpg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".png", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".gif", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.StartsWith(".", StringComparison.Ordinal))
            {
                matches++;
            }

            if (slug.EndsWith(".", StringComparison.Ordinal))
            {
                matches++;
            }

            if (matches > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Generates a slug for a gallery from it's title
        /// </summary>
        /// <param name="title">Title to generate slug for</param>
        /// <param name="slug">Pre-existing slug (can be null)</param>
        /// <returns>New slug</returns>
        public static string GetSlugFromTitle(string title, string slug)
        {
            // normalise slug if it has been fiddeled with
            if (string.IsNullOrEmpty(slug))
            {
                slug = title.ToLower().Normalize(NormalizationForm.FormD);
            }
            else
            {
                slug = slug.ToLower().Normalize(NormalizationForm.FormD);
            }
            string normalisedSlug = "";

            for (int i = 0; i < slug.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                {
                    normalisedSlug += slug[i].ToString();
                }
            }
            slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");

            return slug;
        }

        /// <summary>
        /// Updates gallery information
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="parent">Parent gallery</param>
        /// <param name="itemId">If greater than 0, the index of new gallery cover photo</param>
        /// <param name="items">Number of items added to the gallery</param>
        /// <param name="bytes">Number of bytes added to the gallery</param>
        public static void UpdateGalleryInfo(Core core, Gallery parent, long itemId, int items, long bytes)
        {
            UpdateQuery uQuery = new UpdateQuery("user_galleries");
            uQuery.AddField("gallery_items", new QueryOperation("gallery_items", QueryOperations.Addition, items));
            uQuery.AddField("gallery_bytes", new QueryOperation("gallery_bytes", QueryOperations.Addition, bytes));
            uQuery.AddCondition("gallery_id", parent.GalleryId);

            core.Db.Query(uQuery);
        }

        /// <summary>
        /// Generates a URI pointing to a gallery photo from a given photo path
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Photo owner</param>
        /// <param name="galleryPath">Gallery path</param>
        /// <param name="photoPath">Photo slug</param>
        /// <returns>URI pointing to the photo</returns>
        public static string BuildPhotoUri(Core core, Primitive owner, string galleryPath, string photoPath)
        {
            return BuildPhotoUri(core, owner, galleryPath, photoPath, false);
        }

        /// <summary>
        /// Generates a URI pointing to a gallery photo from a given photo path
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Photo owner</param>
        /// <param name="galleryPath">Gallery path</param>
        /// <param name="photoPath">Photo slug</param>
        /// <param name="reload">Tell the page to append a unique identifier to the end of pictures as a cache buster to force reload</param>
        /// <returns>URI pointing to the photo</returns>
        public static string BuildPhotoUri(Core core, Primitive owner, string galleryPath, string photoPath, bool reload)
        {
            if (reload)
            {
                return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}/{2}?reload=true",
                    owner.UriStub, galleryPath, photoPath));
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}/{2}",
                    owner.UriStub, galleryPath, photoPath));
            }
        }

        /// <summary>
        /// Generates a URI pointing to a gallery photo from a given photo path
        /// </summary>
        /// <param name="thisGroup">Photo owner</param>
        /// <param name="photoPath">Photo slug</param>
        /// <returns>URI pointing to the photo</returns>
        public static string BuildPhotoUri(Core core, Primitive owner, string photoPath)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}",
                owner.UriStub, photoPath));
        }

        /// <summary>
        /// Generates a URI pointing to the gallery photo upload form
        /// </summary>
        /// <param name="thisGroup">Group to upload photo to</param>
        /// <returns>URI pointing to the upload form</returns>
        public static string BuildGalleryUpload(Core core, UserGroup thisGroup)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}gallery/?mode=upload",
                thisGroup.UriStub));
        }

        /// <summary>
        /// Generates a URI pointing to the gallery photo upload form
        /// </summary>
        /// <param name="theNetwork">Network to upload photo to</param>
        /// <returns>URI pointing to the upload form</returns>
        public static string BuildGalleryUpload(Core core, Network theNetwork)
        {
            return core.Hyperlink.AppendSid(string.Format("/network/gallery/{0}/?mode=upload",
                theNetwork.NetworkNetwork));
        }

        /// <summary>
        /// Generates a URI to a user gallery
        /// </summary>
        /// <param name="member">Gallery owner</param>
        /// <returns>URI pointing to the gallery</returns>
        public static string BuildGalleryUri(Core core, Primitive owner)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}gallery",
                owner.UriStub));
        }

        /// <summary>
        /// Generates a URI to a user sub-gallery
        /// </summary>
        /// <param name="member">Gallery owner</param>
        /// <param name="path">sub-gallery path</param>
        /// <returns>URI pointing to the gallery</returns>
        public static string BuildGalleryUri(Core core, Primitive owner, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BuildGalleryUri(core, owner);
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}",
                    owner.UriStub, path));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Show(object sender, ShowPPageEventArgs e)
        {
            if (e.Core.IsAjax)
            {
                ShowMore(sender, e);
                return;
            }

            e.Template.SetTemplate("Gallery", "viewgallery");

            /*GallerySettings settings;
            try
            {
                settings = new GallerySettings(e.Core, e.Page.Owner);
            }
            catch (InvalidGallerySettingsException)
            {
                GallerySettings.Create(e.Core, e.Page.Owner);
                settings = new GallerySettings(e.Core, e.Page.Owner);
            }*/

            char[] trimStartChars = { '.', '/' };

            string galleryPath = e.Slug;

            if (galleryPath != null)
            {
                galleryPath = galleryPath.TrimEnd('/').TrimStart(trimStartChars);
            }
            else
            {
                galleryPath = "";
            }

            Gallery gallery;
            if (galleryPath != "")
            {
                try
                {
                    gallery = new Gallery(e.Core, e.Page.Owner, galleryPath);

                    try
                    {
                        if (!gallery.Access.Can("VIEW"))
                        {
                            e.Core.Functions.Generate403();
                            return;
                        }
                    }
                    catch (InvalidAccessControlPermissionException)
                    {
                    }

                    try
                    {
                        if (gallery.Access.Can("CREATE_ITEMS"))
                        {
                            e.Template.Parse("U_UPLOAD_PHOTO", gallery.PhotoUploadUri);
                        }
                        if (gallery.Access.Can("CREATE_CHILD"))
                        {
                            e.Template.Parse("U_NEW_GALLERY", gallery.NewGalleryUri);
                        }
                    }
                    catch (InvalidAccessControlPermissionException)
                    {
                    }
                }
                catch (InvalidGalleryException)
                {
                    e.Core.Functions.Generate404();
                    return;
                }
            }
            else
            {
                gallery = new Gallery(e.Core, e.Page.Owner);

                try
                {
                    if (gallery.Settings.AllowItemsAtRoot && gallery.access.Can("CREATE_ITEMS"))
                    {
                        e.Template.Parse("U_UPLOAD_PHOTO", gallery.PhotoUploadUri);
                    }
                    if (gallery.Access.Can("CREATE_CHILD"))
                    {
                        e.Template.Parse("U_NEW_GALLERY", gallery.NewGalleryUri);
                    }
                }
                catch (InvalidAccessControlPermissionException)
                {
                }
            }

            /* pages */
            e.Core.Display.ParsePageList(e.Page.Owner, true);

            if (gallery.Id == 0)
            {
                e.Template.Parse("PAGE_TITLE", gallery.owner.DisplayNameOwnership + " Gallery");
                e.Template.Parse("GALLERY_TITLE", gallery.owner.DisplayNameOwnership + " Gallery");
            }
            else
            {
                e.Template.Parse("PAGE_TITLE", gallery.GalleryTitle);
                e.Template.Parse("GALLERY_TITLE", gallery.GalleryTitle);
            }

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "gallery", e.Core.Prose.GetString("GALLERY") });

            if (gallery.Parents != null)
            {
                foreach (ParentTreeNode ptn in gallery.Parents.Nodes)
                {
                    breadCrumbParts.Add(new string[] { ptn.ParentSlug.ToString(), ptn.ParentTitle });
                }
            }

            if (gallery.Id > 0)
            {
                breadCrumbParts.Add(new string[] { gallery.Path, gallery.GalleryTitle });
            }

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);

            List<Gallery> galleries = gallery.GetGalleries();
            List<IPermissibleItem> iPermissibleItems = new List<IPermissibleItem>();

            if (galleries.Count > 0)
            {
                foreach (Gallery galleryGallery in galleries)
                {
                    iPermissibleItems.Add(galleryGallery);
                }
                e.Core.AcessControlCache.CacheGrants(iPermissibleItems);

                e.Template.Parse("GALLERIES", galleries.Count.ToString());

                foreach (Gallery galleryGallery in galleries)
                {
                    if (!galleryGallery.Access.Can("VIEW"))
                    {
                        continue;
                    }
                    VariableCollection galleryVariableCollection = e.Template.CreateChild("gallery_list");

                    galleryVariableCollection.Parse("TITLE", galleryGallery.GalleryTitle);
                    galleryVariableCollection.Parse("URI", Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryGallery.FullPath));
                    galleryVariableCollection.Parse("ID", galleryGallery.Id.ToString());
                    galleryVariableCollection.Parse("TYPE_ID", galleryGallery.ItemKey.TypeId.ToString());

                    galleryVariableCollection.Parse("ICON", galleryGallery.IconUri);
                    galleryVariableCollection.Parse("TILE", galleryGallery.TileUri);
                    galleryVariableCollection.Parse("SQUARE", galleryGallery.SquareUri);

                    galleryVariableCollection.Parse("TINY", galleryGallery.TinyUri);
                    galleryVariableCollection.Parse("THUMBNAIL", galleryGallery.ThumbnailUri);

                    //galleryVariableCollection.Parse("U_EDIT", );

                    //e.Core.Display.ParseBbcode(galleryVariableCollection, "ABSTRACT", galleryGallery.GalleryAbstract);

                    if (galleryGallery.Info.Likes > 0)
                    {
                        galleryVariableCollection.Parse("LIKES", string.Format(" {0:d}", galleryGallery.Info.Likes));
                        galleryVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", galleryGallery.Info.Dislikes));
                    }

                    long items = galleryGallery.Items;

                    if (items == 1)
                    {
                        galleryVariableCollection.Parse("ITEMS", string.Format(e.Core.Prose.GetString("_ITEM"), e.Core.Functions.LargeIntegerToString(items)));
                    }
                    else
                    {
                        galleryVariableCollection.Parse("ITEMS", string.Format(e.Core.Prose.GetString("_ITEMS"), e.Core.Functions.LargeIntegerToString(items)));
                    }
                }
            }

            bool moreContent = false;
            long lastId = 0;
            bool first = true;

            long galleryComments = 0;
            if (gallery.Items > 0)
            {
                List<GalleryItem> galleryItems = gallery.GetItems(e.Core, e.Page.TopLevelPageNumber, 12, e.Page.TopLevelPageOffset);

                e.Template.Parse("PHOTOS", galleryItems.Count.ToString());

                int i = 0;
                foreach (GalleryItem galleryItem in galleryItems)
                {
                    if (first)
                    {
                        first = false;
                        e.Template.Parse("NEWEST_ID", galleryItem.Id.ToString());
                    }

                    VariableCollection galleryVariableCollection = e.Template.CreateChild("photo_list");

                    galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                    galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(e.Core, e.Page.Owner, galleryItem.ParentPath, galleryItem.Path));
                    galleryVariableCollection.Parse("COMMENTS", e.Core.Functions.LargeIntegerToString(galleryItem.Comments));
                    galleryVariableCollection.Parse("VIEWS", e.Core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                    galleryVariableCollection.Parse("INDEX", i.ToString());
                    galleryVariableCollection.Parse("ID", galleryItem.Id.ToString());
                    galleryVariableCollection.Parse("TYPE_ID", galleryItem.ItemKey.TypeId.ToString());

                    galleryVariableCollection.Parse("ICON", galleryItem.IconUri);
                    galleryVariableCollection.Parse("TILE", galleryItem.TileUri);
                    galleryVariableCollection.Parse("SQUARE", galleryItem.SquareUri);

                    galleryVariableCollection.Parse("TINY", galleryItem.TinyUri);
                    galleryVariableCollection.Parse("THUMBNAIL", galleryItem.ThumbnailUri);

                    Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                    if (galleryItem.Info.Likes > 0)
                    {
                        galleryVariableCollection.Parse("LIKES", string.Format(" {0:d}", galleryItem.Info.Likes));
                        galleryVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", galleryItem.Info.Dislikes));
                    }

                    switch (i % 3)
                    {
                        case 0:
                            galleryVariableCollection.Parse("ABC", "a");
                            break;
                        case 1:
                            galleryVariableCollection.Parse("ABC", "b");
                            break;
                        case 2:
                            galleryVariableCollection.Parse("ABC", "c");
                            break;
                    }

                    switch (i % 4)
                    {
                        case 0:
                            galleryVariableCollection.Parse("ABCD", "a");
                            break;
                        case 1:
                            galleryVariableCollection.Parse("ABCD", "b");
                            break;
                        case 2:
                            galleryVariableCollection.Parse("ABCD", "c");
                            break;
                        case 3:
                            galleryVariableCollection.Parse("ABCD", "d");
                            break;
                    }

                    lastId = galleryItem.Id;
                    galleryComments += galleryItem.Comments;
                    i++;
                }

                if (galleryItems.Count > 0)
                {
                    e.Template.Parse("S_RATEBAR", "TRUE");
                }

                e.Core.Display.ParsePagination(Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryPath), 0, 12, gallery.Items);

                if (e.Core.TopLevelPageNumber * 12 < gallery.Items)
                {
                    moreContent = true;
                }

                e.Template.Parse("U_NEXT_PAGE", Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryPath) + "?p=" + (e.Core.TopLevelPageNumber + 1) + "&o=" + lastId);
            }

            if (gallery.Id > 0)
            {
                e.Template.Parse("ALBUM_COMMENTS", "TRUE");
                if (gallery.Access.Can("COMMENT"))
                {
                    e.Template.Parse("CAN_COMMENT", "TRUE");
                }

                e.Core.Display.DisplayComments(e.Template, e.Page.Owner, e.Page.CommentPageNumber, gallery);

                e.Core.Display.ParsePagination("COMMENT_PAGINATION", Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryPath), 1, 10, gallery.Info.Comments);
            }

            e.Template.Parse("COMMENTS", gallery.Comments.ToString());
            e.Template.Parse("L_COMMENTS", string.Format("{0} Comments in gallery", galleryComments));
            e.Template.Parse("U_COMMENTS", e.Core.Hyperlink.BuildGalleryCommentsUri(e.Page.Owner, galleryPath));
        }

        public static void ShowMore(object sender, ShowPPageEventArgs e)
        {
            char[] trimStartChars = { '.', '/' };

            string galleryPath = e.Slug;

            if (galleryPath != null)
            {
                galleryPath = galleryPath.TrimEnd('/').TrimStart(trimStartChars);
            }
            else
            {
                galleryPath = "";
            }

            Gallery gallery;
            if (galleryPath != "")
            {
                try
                {
                    gallery = new Gallery(e.Core, e.Page.Owner, galleryPath);

                    if (!gallery.Access.Can("VIEW"))
                    {
                        e.Core.Functions.Generate403();
                        return;
                    }
                }
                catch (InvalidGalleryException)
                {
                    return;
                }
            }
            else
            {
                gallery = new Gallery(e.Core, e.Page.Owner);
            }

            Template template = new Template(Assembly.GetExecutingAssembly(), "pane_photo");
            template.Medium = e.Core.Template.Medium;
            template.SetProse(e.Core.Prose);

            bool moreContent = false;
            long lastId = 0;

            List<GalleryItem> galleryItems = gallery.GetItems(e.Core, e.Page.TopLevelPageNumber, 12, e.Page.TopLevelPageOffset);

            int i = 0;
            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(e.Core, e.Page.Owner, galleryItem.ParentPath, galleryItem.Path));
                galleryVariableCollection.Parse("COMMENTS", e.Core.Functions.LargeIntegerToString(galleryItem.Comments));
                galleryVariableCollection.Parse("VIEWS", e.Core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                galleryVariableCollection.Parse("INDEX", i.ToString());
                galleryVariableCollection.Parse("ID", galleryItem.Id.ToString());
                galleryVariableCollection.Parse("TYPE_ID", galleryItem.ItemKey.TypeId.ToString());

                galleryVariableCollection.Parse("ICON", galleryItem.IconUri);
                galleryVariableCollection.Parse("TILE", galleryItem.TileUri);
                galleryVariableCollection.Parse("SQUARE", galleryItem.SquareUri);

                galleryVariableCollection.Parse("TINY", galleryItem.TinyUri);
                galleryVariableCollection.Parse("THUMBNAIL", galleryItem.ThumbnailUri);

                Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                if (galleryItem.Info.Likes > 0)
                {
                    galleryVariableCollection.Parse("LIKES", string.Format(" {0:d}", galleryItem.Info.Likes));
                    galleryVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", galleryItem.Info.Dislikes));
                }

                switch (i % 3)
                {
                    case 0:
                        galleryVariableCollection.Parse("ABC", "a");
                        break;
                    case 1:
                        galleryVariableCollection.Parse("ABC", "b");
                        break;
                    case 2:
                        galleryVariableCollection.Parse("ABC", "c");
                        break;
                }

                switch (i % 4)
                {
                    case 0:
                        galleryVariableCollection.Parse("ABCD", "a");
                        break;
                    case 1:
                        galleryVariableCollection.Parse("ABCD", "b");
                        break;
                    case 2:
                        galleryVariableCollection.Parse("ABCD", "c");
                        break;
                    case 3:
                        galleryVariableCollection.Parse("ABCD", "d");
                        break;
                }

                lastId = galleryItem.Id;
                i++;
            }

            if (e.Core.TopLevelPageNumber * 12 < gallery.Items)
            {
                moreContent = true;
            }

            string loadMoreUri = Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryPath) + "?p=" + (e.Core.TopLevelPageNumber + 1) + "&o=" + lastId;
            e.Core.Ajax.SendRawText(moreContent ? loadMoreUri : "noMoreContent", template.ToString());
        }

        /// <summary>
        /// Returns the gallery id
        /// </summary>
        public override long Id
        {
            get
            {
                return galleryId;
            }
        }

        /// <summary>
        /// Returns gallery URI
        /// </summary>
        public override string Uri
        {
            get
            { 
                if (string.IsNullOrEmpty(this.FullPath))
                {
                    return Owner.UriStub + "gallery/";
                }
                else
                {
                    return Owner.UriStub + "gallery/" + this.FullPath + "/";
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string NewGalleryUri
        {
            get
            {
                //return core.Uri.BuildAccountSubModuleUri("galleries", "galleries", "new", galleryId, true);
                return core.Hyperlink.AppendSid(Owner.AccountUriStub + "galleries/galleries/?mode=new&id=" + Id.ToString(), true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PhotoUploadUri
        {
            get
            {
                //return core.Uri.BuildAccountSubModuleUri("galleries", "upload", galleryId, true);
                return core.Hyperlink.AppendSid(Owner.UriStub + "gallery/upload?gallery-id=" + Id.ToString(), true);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string AclUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", Id, ItemKey.TypeId), true);
            }
        }

        /// <summary>
        /// Gets the access object for the gallery
        /// </summary>
        public Access Access
        {
            get
            {
                if (Id == 0)
                {
                    return Settings.Access;
                }
                else
                {
                    if (access == null)
                    {
                        access = new Access(core, this);
                    }
                    return access;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
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

        public GallerySettings Settings
        {
            get
            {
                if (settings == null)
                {
                    if (settingsId > 0)
                    {
                        ItemKey gik = new ItemKey(settingsId, typeof(GallerySettings));
                        core.ItemCache.RequestItem(gik);
                        settings = (GallerySettings)core.ItemCache[gik];
                        //settings = (GallerySettings)NumberedItem.Reflect(core, new ItemKey(settingsId, typeof(GallerySettings)));
                    }
                    else
                    {
                        try
                        {
                            settings = new GallerySettings(core, Owner);
                        }
                        catch (InvalidGallerySettingsException)
                        {
                            settings = GallerySettings.Create(core, Owner);
                        }
                    }
                    return settings;
                }
                else
                {
                    return settings;
                }
            }
        }

        internal GalleryItem HighlightItem
        {
            get
            {
                if (highlightItem == null)
                {
                    if (HighlightId > 0)
                    {
                        try
                        {
                            highlightItem = new GalleryItem(core, Owner, HighlightId);
                        }
                        catch (InvalidGalleryException)
                        {
                            highlightItem = null;
                        }
                    }
                    
                    if (highlightItem == null)
                    {
                        if (Items > 0)
                        {
                            List<GalleryItem> items = GetItems(core, 1, 1, 0);
                            if (items.Count > 0)
                            {
                                highlightItem = items[0];

                                UpdateQuery uQuery = new UpdateQuery(typeof(Gallery));
                                uQuery.AddCondition("gallery_id", Id);
                                uQuery.AddField("gallery_highlight_id", highlightItem.Id);

                                db.Query(uQuery);

                                return highlightItem;
                            }
                        }
                        List<Gallery> galleries = GetGalleries();
                        for (int i = 0; i < galleries.Count; i++)
                        {
                            if (galleries[i].Access.Can("VIEW_ITEMS"))
                            {
                                highlightItem = galleries[i].HighlightItem;
                                break;
                            }
                        }
                        return highlightItem;
                    }
                    return highlightItem;
                }
                else
                {
                    return highlightItem;
                }
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                if (permissionsList == null)
                {
                    permissionsList = AccessControlLists.GetPermissions(core, this);
                }

                return permissionsList;
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                if (parentId == 0)
                {
                    return Settings;
                }
                else
                {
                    return new Gallery(core, ParentId);
                }
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                if (parentId == 0)
                {
                    return Settings.ItemKey;
                }
                else
                {
                    return new ItemKey(parentId, typeof(Gallery));
                }
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public int Order
        {
            get
            {
                return galleryOrder;
            }
        }

        public int Level
        {
            get
            {
                return galleryLevel;
            }
        }

        public long ParentTypeId
        {
            get
            {
                return ItemType.GetTypeId(typeof(Gallery));
            }
        }

        public ParentTree GetParents()
        {
            return Parents;
        }

        public List<Item> GetChildren()
        {
            List<Item> ret = new List<Item>();

            foreach (Item i in GetGalleries())
            {
                ret.Add(i);
            }

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayTitle
        {
            get
            {
                return "Gallery: " + GalleryTitle + " (" + FullPath + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        /// <summary>
        /// 
        /// </summary>
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public long Likes
        {
            get
            {
                return Info.Likes;
            }
        }

        public long Dislikes
        {
            get
            {
                return Info.Dislikes;
            }
        }


        public string Action
        {
            get
            {
                return string.Format(core.Prose.GetString("_UPLOADED_A_PHOTO_TO"), GalleryTitle);
            }
        }

        public string GetActionBody(List<ItemKey> subItems)
        {
            string returnValue = string.Empty;

            if (subItems.Count > 0)
            {

                long galleryItemTypeId = ItemType.GetTypeId(typeof(GalleryItem));
                List<long> itemIds = new List<long>();

                foreach (ItemKey il in subItems)
                {
                    if (il.TypeId == galleryItemTypeId)
                    {
                        itemIds.Add(il.Id);
                    }
                }

                SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
                query.AddCondition("gallery_item_id", ConditionEquality.In, itemIds);
                query.AddCondition("gallery_id", Id);

                DataTable itemDataTable = db.Query(query);

                if (itemDataTable.Rows.Count == 1)
                {
                    GalleryItem item = new GalleryItem(core, Owner, itemDataTable.Rows[0]);

                    if (!string.IsNullOrEmpty(item.ItemAbstract))
                    {
                        returnValue += item.ItemAbstract + "\r\n\r\n";
                    }

                    returnValue += string.Format("[iurl=\"{0}#hd\"][inline cdn-object=\"{2}\" width=\"{3}\" height=\"{4}\"]{1}[/inline][/iurl]",
                            item.Uri, item.FullPath, item.StoragePath, item.ItemWidth, item.ItemHeight);
                }
                else
                {
                    foreach (DataRow row in itemDataTable.Rows)
                    {
                        GalleryItem item = new GalleryItem(core, Owner, row);

                        returnValue += string.Format("[iurl=\"{0}#hd\"][thumb cdn-object=\"{2}\" width=\"{3}\" height=\"{4}\"]{1}[/thumb][/iurl]",
                                item.Uri, item.FullPath, item.StoragePath, item.ItemWidth, item.ItemHeight);
                    }
                }
            }

            return returnValue;
        }

        public string Noun
        {
            get
            {
                return core.Prose.GetString("_PHOTO_GALLERY");
            }
        }


        public ActionableItemType PostType
        {
            get
            {
                return ActionableItemType.Text;
            }
        }

        public byte[] Data
        {
            get
            {
                return null;
            }
        }

        public string DataContentType
        {
            get
            {
                return null;
            }
        }

        public string Caption
        {
            get
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The exception that is thrown when the gallery slug given is not unique.
    /// </summary>
    public class GallerySlugNotUniqueException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when the gallery slug given is not valid.
    /// </summary>
    public class GallerySlugNotValidException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when permission to modify a gallery has not been granted.
    /// </summary>
    public class GalleryPermissionException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when a requested gallery does not exist.
    /// </summary>
    public class InvalidGalleryException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when an owner object given is not a user.
    /// </summary>
    public class GalleryNotAMemberObjectException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when the root gallery cannot be updated.
    /// </summary>
    public class GalleryCannotUpdateRootGalleryException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when a child gallery cannot be updated.
    /// </summary>
    public class GalleryCannotUpdateChildrenException : Exception
    {
    }
}
