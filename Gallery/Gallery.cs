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
    public class Gallery : NumberedItem, IPermissibleItem
    {
        /// <summary>
        /// A list of database fields associated with a user gallery.
        /// </summary>
        //public const string GALLERY_INFO_FIELDS = "ug.gallery_id, ug.gallery_parent_id, ug.gallery_access, ug.gallery_title, ug.gallery_parent_path, ug.gallery_path, ug.gallery_items, ug.gallery_abstract, ug.gallery_visits, ug.gallery_description, ug.gallery_bytes";

        /// <summary>
        /// A list of database fields associated with a user gallery icon.
        /// </summary>
        //public const string GALLERY_ICON_FIELDS = "gi.gallery_item_uri";

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
        /// Access object for the gallery
        /// </summary>
        protected Access galleryAccess;

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
        /// Permissions
        /// </summary>
        [DataField("gallery_access")]
        protected ushort permissions;

        /// <summary>
        /// Hierarchy
        /// </summary>
        [DataField("gallery_hierarchy", MYSQL_TEXT)]
        private string hierarchy;

        /// <summary>
        /// Parent Tree
        /// </summary>
        private ParentTree parentTree;

        Access access;
        List<string> actions = new List<string> { "VIEW", "COMMENT", "CREATE_CHILD", "VIEW_ITEMS", "COMMENT_ITEMS", "RATE_ITEMS", "CREATE_ITEMS", "EDIT_ITEMS", "DELETE_ITEMS"};
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

                if (parentId > 0 && this.GetType() == typeof(UserGallery))
                {
                    Gallery parent = new UserGallery(core, (User)owner, parentId);

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
        /// Gets the raw permissions of the gallery
        /// </summary>
        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        /// <summary>
        /// Gets the access object for the gallery
        /// </summary>
        public Access GalleryAccess
        {
            get
            {
                if (galleryAccess == null)
                {
                    galleryAccess = new Access(core, permissions, Owner);
                }
                return galleryAccess;
            }
        }

        /// <summary>
        /// Gets the owner of the gallery
        /// </summary>
        public Primitive Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.UserProfiles[userId];
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
        public Gallery(Core core, User owner)
            : base(core)
        {
            this.owner = owner;

            galleryId = 0;
            path = "";
            parentPath = "";
            userId = owner.Id;
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        protected Gallery(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;

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
        protected Gallery(Core core, Primitive owner, long galleryId)
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
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="path">Gallery path</param>
        protected Gallery(Core core, Primitive owner, string path)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Gallery_ItemLoad);

            SelectQuery query = Gallery.GetSelectQueryStub(typeof(Gallery));
            query.AddCondition("gallery_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_path", Gallery.GetNameFromPath(path));
            query.AddCondition("user_id", owner.Id);

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
        protected Gallery(Core core, Primitive owner, DataRow galleryRow, bool hasIcon)
            : base(core)
        {
            this.owner = owner;

            loadGalleryInfo(galleryRow);

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
            permissions = (ushort)galleryRow["gallery_access"];
            if (owner is User)
            {
                galleryAccess = new Access(core, (ushort)galleryRow["gallery_access"], owner);
            }
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
        /// <param name="core">Core token</param>
        /// <returns>A list of sub-galleries</returns>
        public List<Gallery> GetGalleries(Core core)
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
            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);

            SelectQuery query = Gallery.GetSelectQueryStub(typeof(Gallery));
            query.AddFields(GalleryItem.GetFieldsPrefixed(typeof(GalleryItem)));
            query.AddJoin(JoinTypes.Left, new DataField(GalleryItem.GetTable(typeof(Gallery)), "gallery_highlight_id"), new DataField(GalleryItem.GetTable(typeof(GalleryItem)), "gallery_item_id"));
            query.AddCondition("gallery_parent_id", Id);
            QueryCondition qc1 = query.AddCondition(new QueryOperation("gallery_access", QueryOperations.BinaryAnd, readAccessLevel).ToString(), ConditionEquality.NotEqual, 0);
            qc1.AddCondition(ConditionRelations.Or, "`user_galleries`.`user_id`", loggedIdUid);
            query.AddCondition("`user_galleries`.`user_id`", ((User)owner).UserId);
            // TODO: permissions

            /*DataTable galleriesTable = core.db.Query(string.Format("SELECT {1}, {2} FROM user_galleries ug LEFT JOIN gallery_items gi ON ug.gallery_highlight_id = gi.gallery_item_id WHERE (ug.gallery_access & {4:0} OR ug.user_id = {5}) AND ug.user_id = {0} AND ug.gallery_parent_path = '{3}';",
                ((User)owner).UserId, Gallery.GALLERY_INFO_FIELDS, Gallery.GALLERY_ICON_FIELDS, Mysql.Escape(FullPath), readAccessLevel, loggedIdUid));*/
            return core.db.Query(query).Rows;
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
            db = core.db;

            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);
            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_id", galleryId);
            query.AddCondition("gallery_item_item_id", owner.Id);
            QueryCondition qc1 = query.AddCondition("gallery_item_item_type_id", owner.TypeId);
            QueryCondition qc2 = qc1.AddCondition(new QueryOperation("gallery_item_access", QueryOperations.BinaryAnd, readAccessLevel), ConditionEquality.NotEqual, false);
            qc2.AddCondition(ConditionRelations.Or, "user_id", loggedIdUid);
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
        public void Update(Core core, string title, string slug, string description, ushort permissions)
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

                if (!Gallery.CheckGallerySlugUnique(core.db, member, parentPath, slug))
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

            db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_title = '{2}', gallery_abstract = '{3}', gallery_path = '{4}', gallery_access = {5} WHERE user_id = {0} AND gallery_id = {1}",
                member.UserId, galleryId, Mysql.Escape(title), Mysql.Escape(description), Mysql.Escape(slug), permissions));
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
                List<Gallery> galleries = ((UserGallery)this).GetGalleries(core);

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
        protected static long create(Core core, Gallery parent, string title, ref string slug, string description, ushort permissions)
        {
            string parents = "";
            // ensure we have generated a valid slug
            slug = Gallery.GetSlugFromTitle(title, slug);

            if (!Gallery.CheckGallerySlugValid(slug))
            {
                throw new GallerySlugNotValidException();
            }

            if (!Gallery.CheckGallerySlugUnique(core.db, (User)parent.owner, parent.FullPath, slug))
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
            iQuery.AddField("gallery_access", permissions);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("gallery_parent_id", parent.GalleryId);
            iQuery.AddField("gallery_bytes", 0);
            iQuery.AddField("gallery_items", 0);
            iQuery.AddField("gallery_visits", 0);
            iQuery.AddField("gallery_hierarchy", parents);

            long galleryId = core.db.Query(iQuery);

            return galleryId;
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
            core.db.UpdateQuery(string.Format("UPDATE user_info SET user_gallery_items = user_gallery_items - {1}, user_bytes = user_bytes - {2} WHERE user_id = {1}",
                core.session.LoggedInMember.UserId, itemsDeleted, bytesDeleted));
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

            List<Gallery> galleries = gallery.GetGalleries(core);

            foreach (Gallery galleryGallery in galleries)
            {
                long[] stuffDeleted = galleryDeleteChildren(core, galleryGallery);
                itemsDeleted += stuffDeleted[0];
                bytesDeleted += stuffDeleted[1];
            }

            object objectsDeleted = core.db.Query(string.Format("SELECT SUM(gallery_item_bytes) AS bytes_deleted FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}';",
                    core.LoggedInMemberId, Mysql.Escape(gallery.FullPath))).Rows[0]["bytes_deleted"];

            if (!(objectsDeleted is DBNull))
            {
                bytesDeleted += (long)(decimal)objectsDeleted;
            }

            core.db.BeginTransaction();
            itemsDeleted += core.db.UpdateQuery(string.Format("DELETE FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}'",
                core.session.LoggedInMember.UserId, Mysql.Escape(gallery.FullPath)));

            core.db.UpdateQuery(string.Format("DELETE FROM user_galleries WHERE user_id = {0} AND gallery_id = {1}",
                core.session.LoggedInMember.UserId, gallery.GalleryId));
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
        public static bool CheckGallerySlugUnique(Mysql db, User owner, string parentFullPath, string slug)
        {
            DataTable galleryGalleryTable = db.Query(string.Format("SELECT gallery_path FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{1}' AND gallery_path = '{2}';",
                        owner.UserId, Mysql.Escape(parentFullPath), Mysql.Escape(slug)));

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
        public static void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            throw new Exception("Use on inherited types only");
        }

        /// <summary>
        /// Generates a URI pointing to a gallery photo from a given photo path
        /// </summary>
        /// <param name="member">Photo owner</param>
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

                    if (!gallery.Access.Can("VIEW"))
                    {
                        e.Core.Functions.Generate403();
                        return;
                    }

                    if (gallery.Access.Can("CREATE_ITEMS"))
                    {
                        e.Template.Parse("U_UPLOAD_PHOTO", e.Core.Uri.BuildPhotoUploadUri(gallery.GalleryId));
                    }
                    if (gallery.Access.Can("CREATE_CHILD"))
                    {
                        e.Template.Parse("U_NEW_GALLERY", e.Core.Uri.BuildNewGalleryUri(gallery.GalleryId));
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

                if (gallery.Access.Can("CREATE_CHILD"))
                {
                    e.Template.Parse("U_NEW_GALLERY", e.Core.Uri.BuildNewGalleryUri(0));
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

            List<Gallery> galleries = gallery.GetGalleries(e.Core);

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

            e.Template.Parse("COMMENTS", galleryComments.ToString());
            e.Template.Parse("L_COMMENTS", string.Format("{0} Comments in gallery", galleryComments));
            e.Template.Parse("U_COMMENTS", e.Core.Uri.BuildGalleryCommentsUri(e.Page.Owner, galleryPath));
        }

        /// <summary>
        /// Show the gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="galleryPath">Path to gallery</param>
        public static void Show(Core core, UPage page, string galleryPath)
        {
            page.template.SetTemplate("Gallery", "viewgallery");

            int p = 1;
            char[] trimStartChars = { '.', '/' };

            if (galleryPath != null)
            {
                galleryPath = galleryPath.TrimEnd('/').TrimStart(trimStartChars);
            }
            else
            {
                galleryPath = "";
            }

            try
            {
                p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
            }
            catch
            {
            }

            page.User.LoadProfileInfo();

            long loggedIdUid = core.LoggedInMemberId;

            UserGallery gallery;
            if (galleryPath != "")
            {
                try
                {
                    gallery = new UserGallery(core, page.User, galleryPath);

                    gallery.GalleryAccess.SetViewer(core.session.LoggedInMember);

                    if (!gallery.GalleryAccess.CanRead)
                    {
                        core.Functions.Generate403();
                        return;
                    }

                    if (gallery.GalleryAccess.CanCreate)
                    {
                        page.template.Parse("U_UPLOAD_PHOTO", core.Uri.BuildPhotoUploadUri(gallery.GalleryId));
                    }
                    if (gallery.Owner.Id == core.LoggedInMemberId)
                    {
                        page.template.Parse("U_NEW_GALLERY", core.Uri.BuildNewGalleryUri(gallery.GalleryId));
                    }

                    core.Display.ParsePagination(Gallery.BuildGalleryUri(core, page.User, galleryPath), p, (int)Math.Ceiling(gallery.Items / 12.0));
                }
                catch (InvalidGalleryException)
                {
                    core.Functions.Generate404();
                    return;
                }
            }
            else
            {
                gallery = new UserGallery(core, page.User);

                if (gallery.Owner.Id == core.LoggedInMemberId)
                {
                    page.template.Parse("U_NEW_GALLERY", core.Uri.BuildNewGalleryUri(0));
                }
            }

            if (gallery.Id == 0)
            {
                page.template.Parse("GALLERY_TITLE", gallery.owner.DisplayNameOwnership + " Gallery");
            }
            else
            {
                page.template.Parse("GALLERY_TITLE", gallery.GalleryTitle);
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

            page.User.ParseBreadCrumbs(breadCrumbParts);

            List<Gallery> galleries = gallery.GetGalleries(core);

            page.template.Parse("GALLERIES", galleries.Count.ToString());

            foreach (Gallery galleryGallery in galleries)
            {
                VariableCollection galleryVariableCollection = page.template.CreateChild("gallery_list");

                galleryVariableCollection.Parse("TITLE", galleryGallery.GalleryTitle);
                galleryVariableCollection.Parse("URI", Gallery.BuildGalleryUri(core, page.User, galleryGallery.FullPath));
                galleryVariableCollection.Parse("THUMBNAIL", galleryGallery.ThumbUri);
                core.Display.ParseBbcode(galleryVariableCollection, "ABSTRACT", galleryGallery.GalleryAbstract);

                long items = galleryGallery.Items;

                if (items == 1)
                {
                    galleryVariableCollection.Parse("ITEMS", "1 item.");
                }
                else
                {
                    galleryVariableCollection.Parse("ITEMS", string.Format("{0} items.", core.Functions.LargeIntegerToString(items)));
                }
            }

            List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

            page.template.Parse("PHOTOS", galleryItems.Count.ToString());

            long galleryComments = 0;
            int i = 0;
            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(core, page.User, galleryItem.ParentPath, galleryItem.Path));
                galleryVariableCollection.Parse("COMMENTS", core.Functions.LargeIntegerToString(galleryItem.ItemComments));
                galleryVariableCollection.Parse("VIEWS", core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                galleryVariableCollection.Parse("INDEX", i.ToString());
                galleryVariableCollection.Parse("ID", galleryItem.Id.ToString());
				galleryVariableCollection.Parse("TYPEID", galleryItem.ItemKey.TypeId.ToString());

                string thumbUri = string.Format("/{0}/images/_thumb/{1}/{2}",
                    page.User.UserName, galleryPath, galleryItem.Path);
                galleryVariableCollection.Parse("THUMBNAIL", thumbUri);

                Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                galleryComments += galleryItem.ItemComments;
                i++;
            }

            if (galleryItems.Count > 0)
            {
                page.template.Parse("S_RATEBAR", "TRUE");
            }

            page.template.Parse("COMMENTS", galleryComments.ToString());
            page.template.Parse("L_COMMENTS", string.Format("{0} Comments in gallery", galleryComments));
            page.template.Parse("U_COMMENTS", core.Uri.BuildGalleryCommentsUri(page.User, galleryPath));
        }

        /// <summary>
        /// Show the gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        public static void Show(Core core, GPage page)
        {
            page.template.SetTemplate("Gallery", "viewgroupgallery");

            string mode = HttpContext.Current.Request.QueryString["mode"];

            if (HttpContext.Current.Request.Form["save"] != null)
            {
                try
                {
                    string title = "";
                    string description = "";
                    byte license = 0;

                    try
                    {
                        license = byte.Parse(HttpContext.Current.Request.Form["license"]);
                        title = HttpContext.Current.Request.Form["title"];
                        description = HttpContext.Current.Request.Form["description"];
                    }
                    catch
                    {
                        core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                        return;
                    }

                    if (!page.Group.IsGroupMember(core.session.LoggedInMember))
                    {
                        core.Functions.Generate403();
                        return;
                    }

                    GroupGallery parent = new GroupGallery(core, page.Group);

                    string slug = HttpContext.Current.Request.Files["photo-file"].FileName;

                    try
                    {
                        string saveFileName = GalleryItem.HashFileUpload(HttpContext.Current.Request.Files["photo-file"].InputStream);
                        if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                        {
                            TPage.EnsureStoragePathExists(saveFileName);
                            HttpContext.Current.Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                        }

                        GroupGalleryItem.Create(core, page.Group, parent, title, ref slug, HttpContext.Current.Request.Files["photo-file"].FileName, saveFileName, HttpContext.Current.Request.Files["photo-file"].ContentType, (ulong)HttpContext.Current.Request.Files["photo-file"].ContentLength, description, 0x0011, license, Classification.RequestClassification());

                        page.template.Parse("REDIRECT_URI", Gallery.BuildPhotoUri(core, page.Group, slug));
                        core.Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                        return;
                    }
                    catch (GalleryItemTooLargeException)
                    {
                        core.Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                        return;
                    }
                    catch (GalleryQuotaExceededException)
                    {
                        core.Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                        return;
                    }
                    catch (InvalidGalleryItemTypeException)
                    {
                        core.Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                        return;
                    }
                    catch (InvalidGalleryFileNameException)
                    {
                        core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                        return;
                    }
                }
                catch (InvalidGalleryException)
                {
                    core.Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                    return;
                }
            }
            else if (mode == "upload")
            {
                page.template.SetTemplate("Gallery", "groupgalleryupload");

                if (!page.Group.IsGroupMember(core.session.LoggedInMember))
                {
                    core.Functions.Generate403();
                    return;
                }

                SelectBox licensesSelectBox = new SelectBox("license");
                DataTable licensesTable = core.db.Query("SELECT license_id, license_title FROM licenses");

                licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
                foreach (DataRow licenseRow in licensesTable.Rows)
                {
                    licensesSelectBox.Add(new SelectBoxItem(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]));
                }

                licensesSelectBox.SelectedKey = "0";
                page.template.Parse("S_GALLERY_LICENSE", licensesSelectBox);

                core.Display.ParseClassification("S_PHOTO_CLASSIFICATION", Classifications.Everyone);
            }
            else
            {
                int p = 1;

                try
                {
                    p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
                }
                catch
                {
                }

                switch (page.Group.GroupType)
                {
                    case "OPEN":
                        // can view the gallery and all it's photos
                        break;
                    case "CLOSED":
                    case "PRIVATE":
                        if (!page.Group.IsGroupMember(core.session.LoggedInMember))
                        {
                            core.Functions.Generate403();
                            return;
                        }
                        break;
                }

                if (page.Group.IsGroupMember(core.session.LoggedInMember))
                {
                    page.template.Parse("U_UPLOAD_PHOTO", Gallery.BuildGalleryUpload(core, page.Group));
                }

                GroupGallery gallery = new GroupGallery(core, page.Group);

                List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

                if (galleryItems.Count > 0)
                {
                    page.template.Parse("PHOTOS", "TRUE");
                }

                long galleryComments = 0;
                int i = 0;
                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                    galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                    galleryVariableCollection.Parse("PHOTO_URI", galleryItem.Uri);
                    galleryVariableCollection.Parse("COMMENTS", core.Functions.LargeIntegerToString(galleryItem.ItemComments));
                    galleryVariableCollection.Parse("VIEWS", core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                    galleryVariableCollection.Parse("INDEX", i.ToString());
                    galleryVariableCollection.Parse("ID", galleryItem.ItemId.ToString());

                    string thumbUri = string.Format("{0}images/_thumb/{1}",
                        page.Group.UriStub, galleryItem.Path);
                    galleryVariableCollection.Parse("THUMBNAIL", thumbUri);

                    Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                    galleryComments += galleryItem.ItemComments;
                    i++;
                }

                core.Display.ParsePagination(string.Format("{0}gallery",
                    page.Group.UriStub), p, (int)Math.Ceiling(page.Group.GalleryItems / 12.0));

            }
        }

        /// <summary>
        /// Show the gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        public static void Show(Core core, NPage page)
        {
            page.template.SetTemplate("Gallery", "viewgroupgallery");

            string mode = HttpContext.Current.Request.QueryString["mode"];

            if (HttpContext.Current.Request.Form["save"] != null)
            {
                try
                {
                    string title = "";
                    string description = "";
                    byte license = 0;

                    try
                    {
                        license = byte.Parse(HttpContext.Current.Request.Form["license"]);
                        title = HttpContext.Current.Request.Form["title"];
                        description = HttpContext.Current.Request.Form["description"];
                    }
                    catch
                    {
                        core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                        return;
                    }

                    if (!page.Network.IsNetworkMember(core.session.LoggedInMember))
                    {
                        core.Functions.Generate403();
                        return;
                    }

                    NetworkGallery parent = new NetworkGallery(core, page.Network);

                    string slug = HttpContext.Current.Request.Files["photo-file"].FileName;

                    try
                    {
                        string saveFileName = GalleryItem.HashFileUpload(HttpContext.Current.Request.Files["photo-file"].InputStream);
                        if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                        {
                            TPage.EnsureStoragePathExists(saveFileName);
                            HttpContext.Current.Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                        }

                        NetworkGalleryItem.Create(core, page.Network, parent, title, ref slug, HttpContext.Current.Request.Files["photo-file"].FileName, saveFileName, HttpContext.Current.Request.Files["photo-file"].ContentType, (ulong)HttpContext.Current.Request.Files["photo-file"].ContentLength, description, 0x0011, license, Classification.RequestClassification());

                        page.template.Parse("REDIRECT_URI", Gallery.BuildPhotoUri(core, page.Network, slug));
                        core.Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                        return;
                    }
                    catch (GalleryItemTooLargeException)
                    {
                        core.Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                        return;
                    }
                    catch (GalleryQuotaExceededException)
                    {
                        core.Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                        return;
                    }
                    catch (InvalidGalleryItemTypeException)
                    {
                        core.Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                        return;
                    }
                    catch (InvalidGalleryFileNameException)
                    {
                        core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                        return;
                    }
                }
                catch (InvalidGalleryException)
                {
                    core.Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                    return;
                }
            }
            else if (mode == "upload")
            {
                page.template.SetTemplate("Gallery", "groupgalleryupload");

                if (!page.Network.IsNetworkMember(core.session.LoggedInMember))
                {
                    core.Functions.Generate403();
                    return;
                }

                SelectBox licensesSelectBox = new SelectBox("license");
                DataTable licensesTable = core.db.Query("SELECT license_id, license_title FROM licenses");

                licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
                foreach (DataRow licenseRow in licensesTable.Rows)
                {
                    licensesSelectBox.Add(new SelectBoxItem(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]));
                }

                licensesSelectBox.SelectedKey = "0";
                page.template.Parse("S_GALLERY_LICENSE", licensesSelectBox);
            }
            else
            {
                int p = 1;

                try
                {
                    p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
                }
                catch
                {
                }

                switch (page.Network.NetworkType)
                {
                    case NetworkTypes.Country:
                    case NetworkTypes.Global:
                        // can view the network and all it's photos
                        break;
                    case NetworkTypes.University:
                    case NetworkTypes.School:
                    case NetworkTypes.Workplace:
                        if (!page.Network.IsNetworkMember(core.session.LoggedInMember))
                        {
                            core.Functions.Generate403();
                            return;
                        }
                        break;
                }

                if (page.Network.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.Parse("U_UPLOAD_PHOTO", Gallery.BuildGalleryUpload(core, page.Network));
                }

                NetworkGallery gallery = new NetworkGallery(core, page.Network);

                List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

                if (galleryItems.Count > 0)
                {
                    page.template.Parse("PHOTOS", "TRUE");
                }

                long galleryComments = 0;
                int i = 0;
                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                    galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                    galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(core, page.Network, galleryItem.Path));
                    galleryVariableCollection.Parse("COMMENTS", core.Functions.LargeIntegerToString(galleryItem.ItemComments));
                    galleryVariableCollection.Parse("VIEWS", core.Functions.LargeIntegerToString(galleryItem.ItemViews));
                    galleryVariableCollection.Parse("INDEX", i.ToString());
                    galleryVariableCollection.Parse("ID", galleryItem.ItemId.ToString());

                    string thumbUri = string.Format("/network/{0}/images/_thumb/{1}",
                        page.Network.NetworkNetwork, galleryItem.Path);
                    galleryVariableCollection.Parse("THUMBNAIL", thumbUri);

                    Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemKey);

                    galleryComments += galleryItem.ItemComments;

                    i++;
                }

                core.Display.ParsePagination(string.Format("/network/{0}/gallery",
                    page.Network.NetworkNetwork), p, (int)Math.Ceiling(page.Network.GalleryItems / 12.0));

            }
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
            get { throw new NotImplementedException(); }
        }

        #region IPermissibleItem Members


        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this, this.Owner);
                }

                return access;
            }
        }

        public List<string> PermissibleActions
        {
            get
            {
                return actions;
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

        #endregion
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
