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
    public class Gallery : NumberedItem, IPermissibleItem, INestableItem, ICommentableItem
    {

        /// <summary>
        /// Owner of the gallery
        /// </summary>
        protected Primitive owner;

        /// <summary>
        /// Id of the gallery
        /// </summary>
        [DataField("gallery_id", DataFieldKeys.Primary)]
        protected long galleryId;

        /// <summary>
        /// User Id (usergallery)
        /// </summary>
        [DataField("user_id")]
        protected long userId;
        
        /// <summary>
        /// Id of the parent gallery
        /// </summary>
        [DataField("gallery_parent_id")]
        protected long parentId;

        /// <summary>
        /// Gallery title
        /// </summary>
        [DataField("gallery_title", 31)]
        protected string galleryTitle;

        /// <summary>
        /// Gallery parent path
        /// </summary>
        [DataField("gallery_parent_path", MYSQL_TEXT)]
        protected string parentPath;

        /// <summary>
        /// Gallery path (slug)
        /// </summary>
        [DataField("gallery_path", 31)]
        protected string path;

        /// <summary>
        /// Number of gallery comments
        /// </summary>
        [DataField("gallery_comments")]
        protected long galleryComments;

        /// <summary>
        /// Number of gallery items comments
        /// </summary>
        [DataField("gallery_item_comments")]
        protected long galleryItemComments;

        /// <summary>
        /// Number of visits made to the gallery
        /// </summary>
        [DataField("gallery_visits")]
        protected long visits;

        /// <summary>
        /// Number of photos in the gallery
        /// </summary>
        [DataField("gallery_items")]
        protected long items;

        /// <summary>
        /// Number of bytes the the photos in the gallery consume
        /// </summary>
        [DataField("gallery_bytes")]
        protected long bytes;

        /// <summary>
        /// Gallery abstract
        /// </summary>
        [DataField("gallery_abstract", MYSQL_TEXT)]
        protected string galleryAbstract;

        /// <summary>
        /// Gallery Description
        /// </summary>
        [DataField("gallery_description", 255)]
        protected string galleryDescription;

        /// <summary>
        /// Id of the highlighted photo
        /// </summary>
        [DataField("gallery_highlight_id")]
        protected long highlightId;

        /// <summary>
        /// URI of the highlighted photo
        /// </summary>
        protected string highlightUri;

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
        /// Parent Tree
        /// </summary>
        private ParentTree parentTree;

        /// <summary>
        /// Access object for the gallery
        /// </summary>
        Access access;
        List<AccessControlPermission> permissionsList;

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

        /// <summary>
        /// Gets the owner of the gallery
        /// </summary>
        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.Type != owner.Type)
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
        public string HighlightUri
        {
            get
            {
                return highlightUri;
            }
        }

        /// <summary>
        /// Gets the gallery highlighted item thumbnail URI
        /// </summary>
        public string ThumbUri
        {
            get
            {
                if (string.IsNullOrEmpty(highlightUri))
                {
                    return "FALSE";
                }
                else
                {
                    if (owner is User)
                    {
                        return string.Format("/{0}/images/_thumb/{1}/{2}",
                            ((User)owner).UserName, FullPath, highlightUri);
                    }
                    else
                    {
                        return "FALSE";
                    }
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
                    LoadItem(typeof(Gallery), galleryId);
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
                    LoadItem(typeof(Gallery), galleryId);
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

            SelectQuery query = Gallery.GetSelectQueryStub(typeof(Gallery));
            query.AddCondition("gallery_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_path", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_id", owner.Id);
            query.AddCondition("gallery_item_type_id", owner.TypeId);

            DataTable galleryTable = db.Query(query);

            if (galleryTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(Gallery), galleryTable.Rows[0]);
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
        /// <param name="galleryRow">Raw data row of gallery</param>
        /// <param name="hasIcon">True if contains raw data for icon</param>
        public Gallery(Core core, Primitive owner, DataRow galleryRow, bool hasIcon)
            : base(core)
        {
            this.owner = owner;

            try
            {
                loadItemInfo(typeof(Gallery), galleryRow);
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

        /// <summary>
        /// 
        /// </summary>
        private void Gallery_ItemLoad()
        {
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

            foreach (DataRow dr in GetGalleryDataRows(core))
            {
                items.Add(new Gallery(core, owner, dr, false));
            }

            return items;
		}

        /// <summary>
        /// Returns raw data for a list of sub-galleries
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>Raw data for a list of sub-galleries</returns>
        protected DataRowCollection GetGalleryDataRows(Core core)
        {
            long loggedIdUid = User.GetMemberId(core.Session.LoggedInMember);
            //ushort readAccessLevel = owner.GetAccessLevel(core.Session.LoggedInMember);

            SelectQuery query = Gallery.GetSelectQueryStub(typeof(Gallery));
            query.AddFields(GalleryItem.GetFieldsPrefixed(typeof(GalleryItem)));
            query.AddJoin(JoinTypes.Left, new DataField(typeof(Gallery), "gallery_highlight_id"), new DataField(typeof(GalleryItem), "gallery_item_id"));
            query.AddCondition("gallery_parent_id", Id);
            query.AddCondition("`user_galleries`.`gallery_item_id`", owner.Id);
            query.AddCondition("`user_galleries`.`gallery_item_type_id`", owner.TypeId);

            return core.Db.Query(query).Rows;
        }

        /// <summary>
        /// Returns a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>A list of photos</returns>
        public List<GalleryItem> GetItems(Core core)
		{
			List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core))
            {
                items.Add(new GalleryItem(core, owner, dr));
            }

            return items;
		}

        /// <summary>
        /// Returns a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Photos per page</param>
        /// <returns>A list of photos</returns>
        public List<GalleryItem> GetItems(Core core, int currentPage, int perPage)
		{
			List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core, currentPage, perPage))
            {
                items.Add(new GalleryItem(core, owner, dr));
            }

            return items;
		}

        /// <summary>
        /// Returns raw data for a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>Raw data for a list of gallery photos</returns>
        protected DataRowCollection GetItemDataRows(Core core)
        {
            return GetItemDataRows(core, 1, 16);
        }

        /// <summary>
        /// Returns raw data for a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <returns>Raw data for a list of gallery photos</returns>
        protected DataRowCollection GetItemDataRows(Core core, int currentPage, int perPage)
        {
            db = core.Db;

            //ushort readAccessLevel = owner.GetAccessLevel(core.Session.LoggedInMember);
            long loggedIdUid = User.GetMemberId(core.Session.LoggedInMember);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_id", galleryId);
            query.AddCondition("gallery_item_item_id", owner.Id);
            QueryCondition qc1 = query.AddCondition("gallery_item_item_type_id", owner.TypeId);
            query.LimitStart = (currentPage - 1) * perPage;
            query.LimitCount = perPage;

            DataTable photoTable = db.Query(query);

            return photoTable.Rows;
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
            iQuery.AddField("gallery_item_id", owner.Id);
            iQuery.AddField("gallery_item_type_id", owner.TypeId);
            iQuery.AddField("gallery_parent_id", parent.GalleryId);
            iQuery.AddField("gallery_bytes", 0);
            iQuery.AddField("gallery_items", 0);
            iQuery.AddField("gallery_comments", 0);
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
            disallowedNames.Add("comments");
            disallowedNames.Add("page");

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

            if (slug.EndsWith(".aspx"))
            {
                matches++;
            }

            if (slug.EndsWith(".asax"))
            {
                matches++;
            }

            if (slug.EndsWith(".php"))
            {
                matches++;
            }

            if (slug.EndsWith(".html"))
            {
                matches++;
            }

            if (slug.EndsWith(".gif"))
            {
                matches++;
            }

            if (slug.EndsWith(".png"))
            {
                matches++;
            }

            if (slug.EndsWith(".js"))
            {
                matches++;
            }

            if (slug.EndsWith(".bmp"))
            {
                matches++;
            }

            if (slug.EndsWith(".jpg"))
            {
                matches++;
            }

            if (slug.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (slug.EndsWith(".zip"))
            {
                matches++;
            }

            if (slug.EndsWith(".jsp"))
            {
                matches++;
            }

            if (slug.EndsWith(".cfm"))
            {
                matches++;
            }

            if (slug.EndsWith(".exe"))
            {
                matches++;
            }

            if (slug.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (slug.EndsWith(".jpg"))
            {
                matches++;
            }

            if (slug.EndsWith(".mpg"))
            {
                matches++;
            }

            if (slug.EndsWith(".png"))
            {
                matches++;
            }

            if (slug.EndsWith(".gif"))
            {
                matches++;
            }

            if (slug.StartsWith("."))
            {
                matches++;
            }

            if (slug.EndsWith("."))
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
                    normalisedSlug += slug[i];
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
            return core.Uri.AppendSid(string.Format("{0}gallery/{1}/{2}",
                owner.UriStub, galleryPath, photoPath));
        }

        /// <summary>
        /// Generates a URI pointing to a gallery photo from a given photo path
        /// </summary>
        /// <param name="thisGroup">Photo owner</param>
        /// <param name="photoPath">Photo slug</param>
        /// <returns>URI pointing to the photo</returns>
        public static string BuildPhotoUri(Core core, Primitive owner, string photoPath)
        {
            return core.Uri.AppendSid(string.Format("{0}gallery/{1}",
                owner.UriStub, photoPath));
        }

        /// <summary>
        /// Generates a URI pointing to the gallery photo upload form
        /// </summary>
        /// <param name="thisGroup">Group to upload photo to</param>
        /// <returns>URI pointing to the upload form</returns>
        public static string BuildGalleryUpload(Core core, UserGroup thisGroup)
        {
            return core.Uri.AppendSid(string.Format("{0}gallery/?mode=upload",
                thisGroup.UriStub));
        }

        /// <summary>
        /// Generates a URI pointing to the gallery photo upload form
        /// </summary>
        /// <param name="theNetwork">Network to upload photo to</param>
        /// <returns>URI pointing to the upload form</returns>
        public static string BuildGalleryUpload(Core core, Network theNetwork)
        {
            return core.Uri.AppendSid(string.Format("/network/gallery/{0}/?mode=upload",
                theNetwork.NetworkNetwork));
        }

        /// <summary>
        /// Generates a URI to a user gallery
        /// </summary>
        /// <param name="member">Gallery owner</param>
        /// <returns>URI pointing to the gallery</returns>
        public static string BuildGalleryUri(Core core, Primitive owner)
        {
            return core.Uri.AppendSid(string.Format("{0}gallery",
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
                return core.Uri.AppendSid(string.Format("{0}gallery/{1}",
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
            e.Template.SetTemplate("Gallery", "viewgallery");

            int p = 1;
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

                    e.Core.Display.ParsePagination(Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryPath), e.Page.page, (int)Math.Ceiling(gallery.Items / 12.0));
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
                    if (gallery.Access.Can("CREATE_CHILD"))
                    {
                        e.Template.Parse("U_NEW_GALLERY", gallery.NewGalleryUri);
                    }
                }
                catch (InvalidAccessControlPermissionException)
                {
                }
            }

            if (gallery.Id == 0)
            {
                e.Template.Parse("GALLERY_TITLE", gallery.owner.DisplayNameOwnership + " Gallery");
            }
            else
            {
                e.Template.Parse("GALLERY_TITLE", gallery.GalleryTitle);
            }

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "gallery", "Gallery" });

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

            e.Template.Parse("GALLERIES", galleries.Count.ToString());

            foreach (Gallery galleryGallery in galleries)
            {
                VariableCollection galleryVariableCollection = e.Template.CreateChild("gallery_list");

                galleryVariableCollection.Parse("TITLE", galleryGallery.GalleryTitle);
                galleryVariableCollection.Parse("URI", Gallery.BuildGalleryUri(e.Core, e.Page.Owner, galleryGallery.FullPath));
                galleryVariableCollection.Parse("THUMBNAIL", galleryGallery.ThumbUri);
                e.Core.Display.ParseBbcode(galleryVariableCollection, "ABSTRACT", galleryGallery.GalleryAbstract);

                long items = galleryGallery.Items;

                if (items == 1)
                {
                    galleryVariableCollection.Parse("ITEMS", "1 item.");
                }
                else
                {
                    galleryVariableCollection.Parse("ITEMS", string.Format("{0} items.", e.Core.Functions.LargeIntegerToString(items)));
                }
            }

            List<GalleryItem> galleryItems = gallery.GetItems(e.Core, p, 12);

            e.Template.Parse("PHOTOS", galleryItems.Count.ToString());

            long galleryComments = 0;
            int i = 0;
            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = e.Template.CreateChild("photo_list");

                galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(e.Core, e.Page.Owner, galleryItem.ParentPath, galleryItem.Path));
                galleryVariableCollection.Parse("COMMENTS", e.Core.Functions.LargeIntegerToString(galleryItem.ItemComments));
                galleryVariableCollection.Parse("VIEWS", e.Core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                galleryVariableCollection.Parse("INDEX", i.ToString());
                galleryVariableCollection.Parse("ID", galleryItem.Id.ToString());
				galleryVariableCollection.Parse("TYPEID", galleryItem.ItemKey.TypeId.ToString());

                string thumbUri = string.Format("{0}images/_thumb/{1}/{2}",
                    e.Page.Owner.UriStub, galleryPath, galleryItem.Path);
                galleryVariableCollection.Parse("THUMBNAIL", thumbUri);

                Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                galleryComments += galleryItem.ItemComments;
                i++;
            }

            if (galleryItems.Count > 0)
            {
                e.Template.Parse("S_RATEBAR", "TRUE");
            }

            if (gallery.Id > 0)
            {
                e.Template.Parse("ALBUM_COMMENTS", "TRUE");
                if (gallery.Access.Can("COMMENT"))
                {
                    e.Template.Parse("CAN_COMMENT", "TRUE");
                }

                e.Core.Display.DisplayComments(e.Template, e.Page.Owner, gallery);
            }

            e.Template.Parse("COMMENTS", gallery.Comments.ToString());
            e.Template.Parse("L_COMMENTS", string.Format("{0} Comments in gallery", galleryComments));
            e.Template.Parse("U_COMMENTS", e.Core.Uri.BuildGalleryCommentsUri(e.Page.Owner, galleryPath));
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
                return core.Uri.BuildAccountSubModuleUri("galleries", "galleries", "new", galleryId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PhotoUploadUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "upload", galleryId, true);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string AclUri
        {
            get
            {
                return core.Uri.AppendAbsoluteSid(string.Format("/acl.aspx?id={0}&type={1}", Id, ItemKey.TypeId), true);
            }
        }

        /// <summary>
        /// Gets the access object for the gallery
        /// </summary>
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
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public bool GetDefaultCan(string permission)
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

        /// <summary>
        /// 
        /// </summary>
        public long Comments
        {
            get
            {
                return galleryComments;
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
