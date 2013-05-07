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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{
    public enum PictureScale : int
    {
        Icon = 50,
        Tile = 100,
        Square = 200,
        High = 400,

        Tiny = 80,
        Thumbnail = 160,
        Mobile = 320,
        Display = 640,
        Full = 1280,
        Ultra = 2560,

        Cover = 960,

        Original = 0,
    }

    /// <summary>
    /// Represents a gallery photo
    /// </summary>
    [DataTable("gallery_items", "PHOTO")]
    public class GalleryItem : NumberedItem, ICommentableItem, IActionableItem, ILikeableItem
    {
        // Square
        static string IconPrefix = "_icon"; // 50
        static string TilePrefix = "_tile"; // 100
        static string SquarePrefix = "_square"; // 200
        static string HighPrefix = "_high"; // 400

        // Ratio
        static string TinyPrefix = "_tiny"; // 80
        static string ThumbnailPrefix = "_thumb"; // 160
        static string MobilePrefix = "_mobile"; // 320
        static string DisplayPrefix = "_display"; // 640
        static string FullPrefix = "_full"; // 1280
        static string UltraPrefix = "_ultra"; // 2560

        // Cover
        static string CoverPrefix = "_cover"; // 960

        /// <summary>
        /// Owner of the photo's user Id
        /// </summary>
        [DataField("user_id")]
        protected long userId;

        /// <summary>
        /// Gallery photo Id
        /// </summary>
        [DataField("gallery_item_id", DataFieldKeys.Primary)]
        protected long itemId;

        /// <summary>
        /// Gallery photo title
        /// </summary>
        [DataField("gallery_item_title", 63)]
        protected string itemTitle;

        /// <summary>
        /// Gallery photo parent path
        /// </summary>
        [DataField("gallery_item_parent_path", MYSQL_TEXT)]
        protected string parentPath;

        /// <summary>
        /// Gallery photo path (slug)
        /// </summary>
        [DataField("gallery_item_uri", 63)]
        protected string path;

        /// <summary>
        /// Gallery photo (parent) gallery Id
        /// </summary>
        [DataField("gallery_id")]
        protected long parentId;

        /// <summary>
        /// Gallery photo comments
        /// </summary>
        [DataField("gallery_item_comments")]
        protected long itemComments;

        /// <summary>
        /// Gallery photo views
        /// </summary>
        [DataField("gallery_item_views")]
        protected long itemViews;

        /// <summary>
        /// Gallery photo width in pixels
        /// </summary>
        [DataField("gallery_item_width")]
        protected int itemWidth;

        /// <summary>
        /// Gallery photo height in pixels
        /// </summary>
        [DataField("gallery_item_height")]
        protected int itemHeight;

        /// <summary>
        /// Gallery photo size in bytes
        /// </summary>
        [DataField("gallery_item_bytes")]
        protected long itemBytes;

        /// <summary>
        /// Gallery photo rating
        /// </summary>
        [DataField("gallery_item_rating")]
        protected float itemRating;

        /// <summary>
        /// Gallery photo content (MIME) type
        /// </summary>
        [DataField("gallery_item_content_type", 31)]
        protected string contentType;

        /// <summary>
        /// Gallery photo storage file name
        /// </summary>
        /// <remarks>
        /// The storage file name for a photo is the hash of the photo file
        /// without a file extension.
        /// </remarks>
        [DataField("gallery_item_storage_path", 128)]
        protected string storagePath;

        /// <summary>
        /// Gallery photo abstract (description)
        /// </summary>
        [DataField("gallery_item_abstract", MYSQL_TEXT)]
        protected string itemAbstract;

        [DataField("gallery_item_date_ut")]
        private long itemCreatedRaw;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_classification")]
        protected byte classification;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_license")]
        protected byte license;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_icon_exists")]
        protected bool iconExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_tile_exists")]
        protected bool tileExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_square_exists")]
        protected bool squareExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_high_exists")]
        protected bool highExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_tiny_exists")]
        protected bool tinyExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_thumb_exists")]
        protected bool thumbnailExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_mobile_exists")]
        protected bool mobileExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_display_exists")]
        protected bool displayExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_full_exists")]
        protected bool fullExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_ultra_exists")]
        protected bool ultraExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_cover_exists")]
        protected bool coverExists;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_vcrop")]
        protected int cropPositionVertical;

        /// <summary>
        /// 
        /// </summary>
        [DataField("gallery_item_hcrop")]
        protected int cropPositionHorizontal;

        /// <summary>
        /// Owner of the photo
        /// </summary>
        protected Primitive owner;

        /// <summary>
        /// 
        /// </summary>
        protected ContentLicense licenseInfo;

        /// <summary>
        /// Gets the gallery photo Id
        /// </summary>
        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        /// <summary>
        /// Gets the gallery photo title
        /// </summary>
        public string ItemTitle
        {
            get
            {
                return itemTitle;
            }
            set
            {
                SetProperty("itemTitle", value);
            }
        }

        /// <summary>
        /// Gets the gallery photo gallery path
        /// </summary>
        public string ParentPath
        {
            get
            {
                return parentPath;
            }
        }

        /// <summary>
        /// Gets the gallery photo path (slug)
        /// </summary>
        public string Path
        {
            get
            {
                return path;
            }
        }
        
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
        /// Gets the number of comments
        /// </summary>
        public long ItemComments
        {
            get
            {
                return itemComments;
            }
        }

        /// <summary>
        /// Gets the number of views
        /// </summary>
        public long ItemViews
        {
            get
            {
                return itemViews;
            }
        }

        /// <summary>
        /// Gets the width of the gallery item
        /// </summary>
        public int ItemWidth
        {
            get
            {
                return itemWidth;
            }
            internal set
            {
                SetProperty("itemWidth", value);
            }
        }

        /// <summary>
        /// Gets the height of the gallery item
        /// </summary>
        public int ItemHeight
        {
            get
            {
                return itemHeight;
            }
            internal set
            {
                SetProperty("itemHeight", value);
            }
        }

        /// <summary>
        /// Gets the number of bytes consumed by the gallery item
        /// </summary>
        public long ItemBytes
        {
            get
            {
                return itemBytes;
            }
        }

        /// <summary>
        /// Gets the rating
        /// </summary>
        public float ItemRating
        {
            get
            {
                return Info.Rating;
            }
        }

        /// <summary>
        /// Returns the content type (MIME type) of the gallery photo
        /// </summary>
        public string ContentType
        {
            get
            {
                return contentType;
            }
        }

        /// <summary>
        /// Returns the path where the photo is stored
        /// </summary>
        public string StoragePath
        {
            get
            {
                return storagePath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ItemAbstract
        {
            get
            {
                return itemAbstract;
            }
            set
            {
                SetProperty("itemAbstract", value);
            }
        }

        /// <summary>
        /// Gets the date the gallery item was uploaded.
        /// </summary>
        /// <param name="tz">Timezone</param>
        /// <returns>DateTime object</returns>
        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(itemCreatedRaw);
        }

        /// <summary>
        /// Returns the gallery item parent gallery Id
        /// </summary>
        public long ParentId
        {
            get
            {
                return parentId;
            }
        }

        /// <summary>
        /// Returns the gallery item license
        /// </summary>
        public ContentLicense License
        {
            get
            {
                return licenseInfo;
            }
        }

        public byte LicenseId
        {
            get
            {
                return license;
            }
            set
            {
                SetProperty("license", value);
            }
        }

        /// <summary>
        /// Returns the gallery item classification
        /// </summary>
        public Classifications Classification
        {
            get
            {
                return (Classifications)classification;
            }
            set
            {
                SetProperty("classification", value);
            }
        }

        /// <summary>
        /// Returns the gallery item owner
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

        public bool IconExists
        {
            get
            {
                return iconExists;
            }
            internal set
            {
                SetPropertyByRef(new { iconExists }, value);
            }
        }

        public bool TileExists
        {
            get
            {
                return tileExists;
            }
            internal set
            {
                SetPropertyByRef(new { tileExists }, value);
            }
        }

        public bool SquareExists
        {
            get
            {
                return squareExists;
            }
            internal set
            {
                SetPropertyByRef(new { squareExists }, value);
            }
        }

        public bool HighExists
        {
            get
            {
                return highExists;
            }
            internal set
            {
                SetPropertyByRef(new { highExists }, value);
            }
        }

        public bool TinyExists
        {
            get
            {
                return tinyExists;
            }
            internal set
            {
                SetPropertyByRef(new { tinyExists }, value);
            }
        }

        public bool ThumbnailExists
        {
            get
            {
                return thumbnailExists;
            }
            internal set
            {
                SetPropertyByRef(new { thumbnailExists }, value);
            }
        }

        public bool MobileExists
        {
            get
            {
                return mobileExists;
            }
            internal set
            {
                SetPropertyByRef(new { mobileExists }, value);
            }
        }

        public bool DisplayExists
        {
            get
            {
                return displayExists;
            }
            internal set
            {
                SetPropertyByRef(new { displayExists }, value);
            }
        }

        public bool FullExists
        {
            get
            {
                return fullExists;
            }
            internal set
            {
                SetPropertyByRef(new { fullExists }, value);
            }
        }

        public bool UltraExists
        {
            get
            {
                return ultraExists;
            }
            internal set
            {
                SetPropertyByRef(new { ultraExists }, value);
            }
        }

        public bool CoverExists
        {
            get
            {
                return coverExists;
            }
            internal set
            {
                SetPropertyByRef(new { coverExists }, value);
            }
        }

        public int CropPositionVertical
        {
            get
            {
                return cropPositionVertical;
            }
            set
            {
                SetPropertyByRef(new { cropPositionVertical }, value);
                CoverExists = false;
                core.Storage.DeleteFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_cover"), this.StoragePath);
            }
        }

        public int CropPositionHorizontal
        {
            get
            {
                return cropPositionHorizontal;
            }
            set
            {
                SetPropertyByRef(new { cropPositionHorizontal }, value);
            }
        }

        Access access;
        List<string> actions = new List<string> { "VIEW", "RATE", "COMMENT" };
        List<AccessControlPermission> permissionsList;

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="path">Gallery item path</param>
        public GalleryItem(Core core, Primitive owner, string path)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(GalleryItem_ItemLoad);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_item_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_item_uri", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            DataTable galleryItemTable = db.Query(query);

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(GalleryItem), galleryItemTable.Rows[0]);
                try
                {
                    licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }
            }
            else
            {
                throw new GalleryItemNotFoundException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public GalleryItem(Core core, User owner, DataRow itemRow)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            loadItemInfo(typeof(GalleryItem), itemRow);
            try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public GalleryItem(Core core, Primitive owner, DataRow itemRow)
            : base(core)
        {
            this.owner = owner;

            loadItemInfo(typeof(GalleryItem), itemRow);
            try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public GalleryItem(Core core, DataRow itemRow)
            : base(core)
        {
            // TODO: owner not set, no big worry

            loadItemInfo(typeof(GalleryItem), itemRow);
            try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="parent">Gallery item parent</param>
        /// <param name="path">Gallery item path</param>
        public GalleryItem(Core core, User owner, Gallery parent, string path)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(GalleryItem_ItemLoad);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_item_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_item_uri", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            DataTable galleryItemTable = db.Query(query);

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(GalleryItem), galleryItemTable.Rows[0]);
                try
                {
                    licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }
            }
            else
            {
                throw new GalleryItemNotFoundException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemId">Gallery item Id</param>
        public GalleryItem(Core core, Primitive owner, long itemId)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(GalleryItem_ItemLoad);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_item_id", itemId);
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            DataTable galleryItemTable = db.Query(query);

            try
            {
                loadItemInfo(typeof(GalleryItem), galleryItemTable.Rows[0]);
                licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
            }
            catch (InvalidItemException)
            {
                throw new GalleryItemNotFoundException();
            }
            catch (InvalidLicenseException)
            {
            }
            catch (IndexOutOfRangeException)
            {
                throw new GalleryItemNotFoundException();
            }
        }

        /*public GalleryItem(Mysql db, Member owner, long itemId)
            : this(db, (Primitive)owner, itemId)
        {
        }*/

        /*public GalleryItem(Mysql db, Group owner, long itemId)
            : this(db, (Primitive)owner, itemId)
        {
        }*/

        /*public GalleryItem(Mysql db, Network owner, long itemId)
            : this(db, (Primitive)owner, itemId)
        {
        }*/

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="itemId">Gallery item Id</param>
        public GalleryItem(Core core, long itemId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(GalleryItem_ItemLoad);

            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem));
            query.AddCondition("gallery_item_id", itemId);

            DataTable galleryItemTable = db.Query(query);

            try
            {
                loadItemInfo(typeof(GalleryItem), galleryItemTable.Rows[0]);

                licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
            }
            catch (InvalidItemException)
            {
                throw new GalleryItemNotFoundException();
            }
            catch (InvalidLicenseException)
            {
            }
        }

        void GalleryItem_ItemLoad()
        {
        }

        /// <summary>
        /// Generates a query stub for generating more complex queries for the
        /// gallery item data type.
        /// </summary>
        /// <returns>A query stub for the gallery item data type</returns>
        public static SelectQuery GalleryItem_GetSelectQueryStub()
        {
            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem), false);
            query.AddFields(GalleryItem.GetFieldsPrefixed(typeof(ContentLicense)));
            query.AddJoin(JoinTypes.Left, ContentLicense.GetTable(typeof(ContentLicense)), "gallery_item_license", "license_id");

            return query;
        }

        /// <summary>
        /// Increment the number of views
        /// </summary>
        /// <param name="viewer">Person viewing the gallery item</param>
        public void Viewed(User viewer)
        {
            if (viewer != null)
            {
                if (owner is User)
                {
                    if (viewer.UserId == ((User)owner).UserId)
                    {
                        return;
                    }
                }
                db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_views = gallery_item_views + 1 WHERE gallery_item_id = {0};",
                    itemId));
                // otherwise just update the view count
            }
            return;
        }

        /*public static GalleryItem Create(TPage page, Member owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license)
        {
            return GalleryItem.Create(page, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license);
        }*/

        /// <summary>
        /// Delete the gallery item
        /// </summary>
        /// <param name="core"></param>
        public void Delete(Core core)
        {
            SelectQuery squery = new SelectQuery("gallery_items gi");
            squery.AddFields("COUNT(*) AS number");
            squery.AddCondition("gallery_item_storage_path", storagePath);

            DataTable results = db.Query(squery);

            DeleteQuery dquery = new DeleteQuery("gallery_items");
            dquery.AddCondition("gallery_item_id", itemId);
            dquery.AddCondition("user_id", core.LoggedInMemberId);

            db.BeginTransaction();
            if (db.Query(dquery) > 0)
            {
                // TODO, determine if the gallery icon and act appropriately
                /*if (parentId > 0)
                {

                }*/

                if (owner is User)
                {
                    Gallery parent = new Gallery(core, (User)owner, parentId);
                    Gallery.UpdateGalleryInfo(core, parent, (long)itemId, -1, -ItemBytes);
                }

                UpdateQuery uQuery = new UpdateQuery("user_info");
                uQuery.AddField("user_gallery_items", new QueryOperation("user_gallery_items", QueryOperations.Subtraction, 1));
                uQuery.AddField("user_bytes", new QueryOperation("user_bytes", QueryOperations.Subtraction, ItemBytes));
                uQuery.AddCondition("user_id", userId);

                db.Query(uQuery);

                if ((long)results.Rows[0]["number"] > 1)
                {
                    // do not delete the storage file, still in use
                }
                else
                {
                    // delete the storage file
                    core.Storage.DeleteFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), storagePath);
                }
            }
            else
            {
                //TODO: throw new
                throw new Exception("Unauthorised");
            }
        }

        /// <summary>
        /// Creates a new gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Owner</param>
        /// <param name="parent">Gallery</param>
        /// <param name="title">Title</param>
        /// <param name="slug">Slug</param>
        /// <param name="fileName">File name</param>
        /// <param name="storageName">Storage name</param>
        /// <param name="contentType">Content type</param>
        /// <param name="bytes">Bytes</param>
        /// <param name="description">Description</param>
        /// <param name="permissions">Permissions mask</param>
        /// <param name="license">License</param>
        /// <param name="classification">Classification</param>
        /// <remarks>Slug is a reference</remarks>
        /// <returns>New gallery item</returns>
        public static GalleryItem Create(Core core, Primitive owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, byte license, Classifications classification, int width, int height)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Mysql db = core.Db;

            if (owner is User)
            {
                if (owner.Id != core.LoggedInMemberId)
                {
                    throw new Exception("Error, user IDs don't match");
                }
            }

            // 10 MiB
            if (bytes > (ulong)10 * 1024 * 1024)
            {
                throw new GalleryItemTooLargeException();
            }

            // 5 giB
            if (core.Session.LoggedInMember.UserInfo.BytesUsed + bytes > (ulong)5 * 1024 * 1024 * 1024)
            {
                throw new GalleryQuotaExceededException();
            }

            switch (contentType)
            {
                case "image/png":
                case "image/jpeg":
                case "image/pjpeg":
                case "image/gif": // not accepting gif at the moment
                    break;
                default:
                    throw new InvalidGalleryItemTypeException();
            }

            title = Functions.TrimStringToWord(title);

            slug = GalleryItem.GetSlugFromFileName(fileName, slug);
            slug = Functions.TrimStringWithExtension(slug);

            GalleryItem.EnsureGallerySlugUnique(core, parent, owner, ref slug);

            InsertQuery iQuery = new InsertQuery("gallery_items");
            iQuery.AddField("gallery_item_uri", slug);
            iQuery.AddField("gallery_item_title", title);
            iQuery.AddField("gallery_item_abstract", description);
            iQuery.AddField("gallery_item_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("gallery_item_storage_path", storageName);
            iQuery.AddField("gallery_item_parent_path", parent.FullPath);
            iQuery.AddField("gallery_item_content_type", contentType);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("gallery_item_bytes", bytes);
            iQuery.AddField("gallery_item_license", license);
            iQuery.AddField("gallery_id", parent.GalleryId);
            iQuery.AddField("gallery_item_item_id", owner.Id);
            iQuery.AddField("gallery_item_item_type_id", owner.TypeId);
            iQuery.AddField("gallery_item_classification", (byte)classification);
            iQuery.AddField("gallery_item_icon_exists", false);
            iQuery.AddField("gallery_item_tile_exists", false);
            iQuery.AddField("gallery_item_square_exists", false);
            iQuery.AddField("gallery_item_high_exists", false);
            iQuery.AddField("gallery_item_tiny_exists", false);
            iQuery.AddField("gallery_item_thumb_exists", false);
            iQuery.AddField("gallery_item_mobile_exists", false);
            iQuery.AddField("gallery_item_display_exists", false);
            iQuery.AddField("gallery_item_full_exists", false);
            iQuery.AddField("gallery_item_ultra_exists", false);
            iQuery.AddField("gallery_item_cover_exists", false);
            iQuery.AddField("gallery_item_width", width);
            iQuery.AddField("gallery_item_height", height);
            iQuery.AddField("gallery_item_vcrop", 0);
            iQuery.AddField("gallery_item_hcrop", 0);

            // we want to use transactions
            long itemId = db.Query(iQuery);

            if (itemId >= 0)
            {
                //owner.UpdateGalleryInfo(parent, itemId, 1, (long)bytes);
                //if (owner is User)
                {
                    Gallery.UpdateGalleryInfo(core, parent, itemId, 1, (long)bytes);
                }
                /*parent.Bytes += (long)bytes;
                parent.Items += 1;
                parent.Update();*/

                UpdateQuery uQuery = new UpdateQuery("user_info");
                uQuery.AddField("user_gallery_items", new QueryOperation("user_gallery_items", QueryOperations.Addition, 1));
                uQuery.AddField("user_bytes", new QueryOperation("user_bytes", QueryOperations.Addition, bytes));
                uQuery.AddCondition("user_id", core.LoggedInMemberId);

                if (db.Query(uQuery) < 0)
                {
                    throw new Exception("Transaction failed, panic!");
                }

                return new GalleryItem(core, owner, itemId);
                //return itemId;
            }

            throw new Exception("Transaction failed, panic!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="permissions"></param>
        /// <param name="license"></param>
        /// <param name="classification"></param>
        /*public void Update(string title, string description, byte license, Classifications classification)
        {
            long rowsChanged = db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_title = '{2}', gallery_item_abstract = '{3}', gallery_item_license = {4}, gallery_item_classification = {7} WHERE user_id = {0} AND gallery_item_id = {1} AND gallery_item_item_id = {5} AND gallery_item_item_type_id = {6};",
                core.LoggedInMemberId, itemId, Mysql.Escape(title), Mysql.Escape(description), license, owner.Id, owner.TypeId, (byte)classification));

            if (rowsChanged == 0)
            {
                throw new GalleryItemNotFoundException();
            }
        }*/

        /// <summary>
        /// Rotate the gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="rotation">Rotation</param>
        public void Rotate(Core core, RotateFlipType rotation)
        {
            ImageFormat iF = ImageFormat.Jpeg;

            Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), StoragePath);
            Image image = Image.FromStream(fs);

            iF = image.RawFormat;

            image.RotateFlip(rotation);

            MemoryStream stream = new MemoryStream();
            image.Save(stream, iF);

            string newFileName = core.Storage.SaveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), stream);
            stream.Close();
            fs.Close();

            if (core.Storage.FileExists(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), newFileName))
            {
                UpdateQuery uquery = new UpdateQuery("gallery_items");
                uquery.AddField("gallery_item_storage_path", newFileName);
                uquery.AddField("gallery_item_icon_exists", false);
                uquery.AddField("gallery_item_tile_exists", false);
                uquery.AddField("gallery_item_square_exists", false);
                uquery.AddField("gallery_item_high_exists", false);
                uquery.AddField("gallery_item_tiny_exists", false);
                uquery.AddField("gallery_item_thumb_exists", false);
                uquery.AddField("gallery_item_mobile_exists", false);
                uquery.AddField("gallery_item_display_exists", false);
                uquery.AddField("gallery_item_full_exists", false);
                uquery.AddField("gallery_item_ultra_exists", false);
                uquery.AddField("gallery_item_cover_exists", false);
                uquery.AddField("gallery_item_width", ItemHeight);
                uquery.AddField("gallery_item_height", ItemWidth);
                uquery.AddCondition("gallery_item_id", itemId);

                db.Query(uquery);
            }
        }

        /// <summary>
        /// Generates a slug from the file name
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="slug">Existing slug</param>
        /// <returns>New slug</returns>
        public static string GetSlugFromFileName(string filename, string slug)
        {
            string[] saveFileUriParts = filename.Split(new char[] { '\\', '/' });
            string saveFileUri = saveFileUriParts[saveFileUriParts.GetUpperBound(0)];
            string saveFileExt = saveFileUri.Substring(saveFileUri.LastIndexOf('.'));
            saveFileUri = saveFileUri.Substring(0, saveFileUri.LastIndexOf('.'));

            saveFileUri = saveFileUri.ToLower().Normalize(NormalizationForm.FormD);
            string normalisedSlug = "";
            for (int i = 0; i < saveFileUri.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(saveFileUri[i]) != UnicodeCategory.NonSpacingMark)
                {
                    normalisedSlug += saveFileUri[i].ToString();
                }
            }
            saveFileUri = Regex.Replace(normalisedSlug, @"([\W]+)", "-") + saveFileExt;

            return saveFileUri;
        }

        /// <summary>
        /// Checks the slug for uniqueness, and updates it to maintain
        /// uniqueness if necessary
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="gallery">Parent gallery</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="slug">Slug</param>
        /// <remarks>Slug is a reference argument</remarks>
        public static void EnsureGallerySlugUnique(Core core, Gallery gallery, Primitive owner, ref string slug)
        {
            int nameCount = 1;
            bool copyFound = false;

            string originalSlug = slug;

            // keep going until we find a name that does not already exist in the database
            do
            {
                DataTable galleryItemTable = core.Db.Query(string.Format("SELECT gallery_item_uri FROM gallery_items WHERE gallery_item_uri = '{0}' AND gallery_id = {1} AND gallery_item_item_id = {2} AND gallery_item_item_type_id = {3};",
                    Mysql.Escape(slug), gallery.GalleryId, owner.Id, owner.TypeId));

                if (galleryItemTable.Rows.Count > 0)
                {
                    nameCount++;
                    slug = originalSlug;
                    int pointIndex = slug.LastIndexOf('.');
                    copyFound = true;
                    slug = slug.Remove(pointIndex) + "--" + nameCount.ToString() + slug.Substring(pointIndex);
                }
                else
                {
                    copyFound = false;
                }

                // limit the number of tries to stop abuse, very very very unlikely
                // this allows for 6 files with the same name in the same gallery
                if (nameCount > 5)
                {
                    throw new InvalidGalleryFileNameException();
                }
            }
            while (copyFound);
        }

        /// <summary>
        /// Returns gallery item URI
        /// </summary>
        /// <returns></returns>
        public string BuildUri()
        {
            if (parentId > 0)
            {
                return core.Uri.AppendSid(string.Format("{0}gallery/{1}/{2}",
                    Owner.UriStub, parentPath, path));
            }
            else
            {
                return core.Uri.AppendSid(string.Format("{0}gallery/{1}",
                    Owner.UriStub, path));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Gallery", "viewphoto");

            char[] trimStartChars = { '.', '/' };

            try
            {
                GalleryItem galleryItem = new GalleryItem(e.Core, e.Page.Owner, e.Slug);
                Gallery gallery = null;

                if (galleryItem.parentId > 0)
                {
                    gallery = new Gallery(e.Core, galleryItem.parentId);
                }
                else
                {
                    gallery = new Gallery(e.Core, e.Page.Owner);
                }

                if (!gallery.Access.Can("VIEW_ITEMS"))
                {
                    e.Core.Functions.Generate403();
                    return;
                }

                /* pages */
                e.Core.Display.ParsePageList(e.Page.Owner, true);

                if (e.Page.Owner is User)
                {
                    e.Template.Parse("USER_THUMB", ((User)e.Page.Owner).UserThumbnail);
                    e.Template.Parse("USER_COVER_PHOTO", ((User)e.Page.Owner).CoverPhoto);
                }

                galleryItem.Viewed(e.Core.Session.LoggedInMember);

                /* check gallery item has width and height information saved */

                if (galleryItem.ItemWidth <= 0 || galleryItem.ItemHeight <= 0)
                {
                    Stream fs = e.Core.Storage.RetrieveFile(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.StoragePath);
                    Image image = Image.FromStream(fs);
                    int width = image.Width;
                    int height = image.Height;

                    galleryItem.ItemWidth = width;
                    galleryItem.ItemHeight = height;
                    galleryItem.Update();
                }

                Size hdSize = galleryItem.GetSize(new Size(1920, 1080));

                e.Template.Parse("PHOTO_TITLE", galleryItem.ItemTitle);
                e.Template.Parse("PHOTO_ID", galleryItem.ItemId.ToString());
                e.Core.Display.ParseBbcode("PHOTO_DESCRIPTION", galleryItem.ItemAbstract);
                e.Template.Parse("HD_WIDTH", hdSize.Width);
                e.Template.Parse("HD_HEIGHT", hdSize.Height);
                e.Template.Parse("PHOTO_COMMENTS", e.Core.Functions.LargeIntegerToString(galleryItem.ItemComments));
                e.Template.Parse("U_EDIT", galleryItem.EditUri);
                e.Template.Parse("U_MARK_DISPLAY_PIC", galleryItem.MakeDisplayPicUri);
                e.Template.Parse("U_MARK_GALLERY_COVER", galleryItem.SetGalleryCoverUri);
                e.Template.Parse("U_ROTATE_LEFT", galleryItem.RotateLeftUri);
                e.Template.Parse("U_ROTATE_RIGHT", galleryItem.RotateRightUri);
                e.Template.Parse("U_DELETE", galleryItem.DeleteUri);
                e.Template.Parse("U_TAG", galleryItem.TagUri);

                e.Template.Parse("PHOTO_MOBILE", galleryItem.MobileUri);
                e.Template.Parse("PHOTO_DISPLAY", galleryItem.DisplayUri);
                e.Template.Parse("PHOTO_FULL", galleryItem.FullUri);
                e.Template.Parse("PHOTO_ULTRA", galleryItem.UltraUri);

                if (gallery.Access.Can("CREATE_ITEMS"))
                {
                    e.Template.Parse("U_UPLOAD_PHOTO", gallery.PhotoUploadUri);
                }

                if (gallery.Access.Can("DOWNLOAD_ORIGINAL"))
                {
                    e.Template.Parse("U_VIEW_FULL", galleryItem.OriginalUri);
                }

                switch (galleryItem.Classification)
                {
                    case Classifications.Everyone:
                        e.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                        e.Template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                        break;
                    case Classifications.Mature:
                        e.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                        e.Template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                        break;
                    case Classifications.Restricted:
                        e.Template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                        e.Template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                        break;
                }

                if (galleryItem.License != null)
                {
                    if (!string.IsNullOrEmpty(galleryItem.License.Title))
                    {
                        e.Template.Parse("PAGE_LICENSE", galleryItem.License.Title);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Icon))
                    {
                        e.Template.Parse("I_PAGE_LICENSE", galleryItem.License.Icon);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Link))
                    {
                        e.Template.Parse("U_PAGE_LICENSE", galleryItem.License.Link);
                    }
                }

                Display.RatingBlock(galleryItem.ItemRating, e.Template, galleryItem.ItemKey);

                e.Template.Parse("ID", galleryItem.ItemId.ToString());
                e.Template.Parse("TYPEID", galleryItem.ItemKey.TypeId.ToString());
                //template.Parse("U_EDIT", ZzUri.BuildPhotoEditUri((long)photoTable.Rows[0]["gallery_item_id"])));

                if (gallery.Access.Can("COMMENT_ITEMS"))
                {
                    e.Template.Parse("CAN_COMMENT", "TRUE");
                }

                e.Core.Display.DisplayComments(e.Template, e.Page.Owner, galleryItem);

                string pageUri = string.Format("{0}gallery/{1}",
                    HttpUtility.HtmlEncode(e.Page.Owner.UriStub), e.Slug);
                e.Core.Display.ParsePagination("COMMENT_PAGINATION", pageUri, e.Page.TopLevelPageNumber, (int)Math.Ceiling(galleryItem.ItemComments / 10.0));

                List<string[]> breadCrumbParts = new List<string[]>();

                breadCrumbParts.Add(new string[] { "gallery", "Gallery" });
                if (gallery.Parents != null)
                {
                    foreach (ParentTreeNode node in gallery.Parents.Nodes)
                    {
                        breadCrumbParts.Add(new string[] { node.ParentSlug, node.ParentTitle });
                    }
                }
                breadCrumbParts.Add(new string[] { gallery.Path, gallery.GalleryTitle });
                if (!string.IsNullOrEmpty(galleryItem.ItemTitle))
                {
                    breadCrumbParts.Add(new string[] { galleryItem.Path, galleryItem.ItemTitle });
                }
                else
                {
                    breadCrumbParts.Add(new string[] { galleryItem.Path, galleryItem.Path });
                }

                e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);

                List<UserTag> tags = UserTag.GetTags(e.Core, galleryItem);

                if (tags.Count > 0)
                {
                    e.Template.Parse("HAS_USER_TAGS", "TRUE");
                }

                e.Template.Parse("TAG_COUNT", tags.Count.ToString());

                int i = 0;

                foreach (UserTag tag in tags)
                {
                    VariableCollection tagsVariableCollection = e.Template.CreateChild("user_tags");

                    tagsVariableCollection.Parse("INDEX", i.ToString());
                    tagsVariableCollection.Parse("TAG_ID", tag.TagId);
                    tagsVariableCollection.Parse("TAG_X", (tag.TagLocation.X / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("TAG_Y", (tag.TagLocation.Y / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("DISPLAY_NAME", tag.TaggedMember.DisplayName);
                    tagsVariableCollection.Parse("U_MEMBER", tag.TaggedMember.Uri);
                    tagsVariableCollection.Parse("TAG_USER_ID", tag.TaggedMember.Id.ToString());
                }

                GalleryItem nextItem = null;
                GalleryItem prevItem = null;

                SelectQuery nextQuery = GetSelectQueryStub(typeof(GalleryItem));
                nextQuery.AddCondition("gallery_id", gallery.Id);
                nextQuery.AddCondition("gallery_item_item_id", gallery.Owner.Id);
                nextQuery.AddCondition("gallery_item_item_type_id", gallery.Owner.TypeId);
                nextQuery.AddCondition("gallery_item_id", ConditionEquality.GreaterThan, galleryItem.Id);
                nextQuery.AddSort(SortOrder.Ascending, "gallery_item_id");
                nextQuery.LimitCount = 1;

                SelectQuery prevQuery = GetSelectQueryStub(typeof(GalleryItem));
                prevQuery.AddCondition("gallery_id", gallery.Id);
                prevQuery.AddCondition("gallery_item_item_id", gallery.Owner.Id);
                prevQuery.AddCondition("gallery_item_item_type_id", gallery.Owner.TypeId);
                prevQuery.AddCondition("gallery_item_id", ConditionEquality.LessThan, galleryItem.Id);
                prevQuery.AddSort(SortOrder.Descending, "gallery_item_id");
                prevQuery.LimitCount = 1;

                DataTable nextDataTable = e.Db.Query(nextQuery);

                if (nextDataTable.Rows.Count == 1)
                {
                    nextItem = new GalleryItem(e.Core, nextDataTable.Rows[0]);
                }

                DataTable prevDataTable = e.Db.Query(prevQuery);

                if (prevDataTable.Rows.Count == 1)
                {
                    prevItem = new GalleryItem(e.Core, prevDataTable.Rows[0]);
                }

                if (nextItem != null)
                {
                    e.Template.Parse("U_NEXT_PHOTO", nextItem.Uri);
                }

                if (prevItem != null)
                {
                    e.Template.Parse("U_PREVIOUS_PHOTO", prevItem.Uri);
                }

                /*string path1 = TPage.GetStorageFilePath(galleryItem.StoragePath);
                string path2 = e.Core.Storage.RetrieveFilePath(string.Empty, galleryItem.StoragePath);
                string path3 = e.Core.Storage.RetrieveFilePath("_thumb", galleryItem.StoragePath);

                HttpContext.Current.Response.Write(path1 + "<br />" + path2 + "<br />" + path3);*/

            }
            catch (GalleryItemNotFoundException)
            {
                e.Core.Functions.Generate404();
                return;
            }
        }

        public static string GetFileExtension(string contentType)
        {
            switch (contentType)
            {
                case "image/jpeg":
                case "image/pjpeg":
                    return ".jpg";
                case "image/png":
                    return ".png";
                case "image/svg+xml":
                    return ".svg";
                case "image/vnd.wap.wbmp":
                    return ".wbmp";
                case "image/gif":
                    return ".gif";
                case "image/bmp":
                    return ".bmp";
            }
            return string.Empty;
        }

        /// <summary>
        /// Shows an image
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        public static void ShowImage(object sender, ShowPPageEventArgs e)
        {
            string photoName = e.Slug;

            // Square
            bool iconRequest = false; // 50
            bool tileRequest = false; // 100
            bool squareRequest = false; // 200
            bool highRequest = false; // 400

            // Ratio
            bool tinyRequest = false; // 80
            bool thumbnailRequest = false; // 160
            bool mobileRequest = false; // 320
            bool displayRequest = false; // 640
            bool fullRequest = false; // 1280
            bool ultraRequest = false; // 2560

            // Cover
            bool coverRequest = false; // 960

            bool originalRequest = false;
   
            bool retinaModifier = false;
            string storagePrefix = string.Empty;
            PictureScale scale = PictureScale.Original;

            int extensionIndex = photoName.LastIndexOf('.');

            //HttpContext.Current.Response.Write("Here " + photoName.Substring(extensionIndex - 3, 3));
            //HttpContext.Current.Response.End();
            if (photoName.Substring(extensionIndex - 3, 3).Equals("@2x"))
            {
                retinaModifier = true;
                photoName = photoName.Remove(extensionIndex - 3, 3);
            }

            //photoName.

            if (photoName.StartsWith(IconPrefix))
            {
                photoName = photoName.Remove(0, 6);
                if (!retinaModifier)
                {
                    iconRequest = true;
                    storagePrefix = IconPrefix;
                    scale = PictureScale.Icon;
                }
                else
                {
                    tileRequest = true;
                    storagePrefix = TilePrefix;
                    scale = PictureScale.Tile;
                }
            }
            else if (photoName.StartsWith(TilePrefix))
            {
                photoName = photoName.Remove(0, 6);
                if (!retinaModifier)
                {
                    tileRequest = true;
                    storagePrefix = TilePrefix;
                    scale = PictureScale.Tile;
                }
                else
                {
                    squareRequest = true;
                    storagePrefix = SquarePrefix;
                    scale = PictureScale.Square;
                }
            }
            else if (photoName.StartsWith(SquarePrefix))
            {
                photoName = photoName.Remove(0, 8);
                if (!retinaModifier)
                {
                    squareRequest = true;
                    storagePrefix = SquarePrefix;
                    scale = PictureScale.Square;
                }
                else
                {
                    highRequest = true;
                    storagePrefix = HighPrefix;
                    scale = PictureScale.High;
                }
            }
            else if (photoName.StartsWith(HighPrefix))
            {
                photoName = photoName.Remove(0, 6);
                highRequest = true;
                storagePrefix = HighPrefix;
                scale = PictureScale.High;
                // There is no retina version of high, it is the retina version
            }
            else if (photoName.StartsWith(TinyPrefix))
            {
                photoName = photoName.Remove(0, 6);
                if (!retinaModifier)
                {
                    tinyRequest = true;
                    storagePrefix = TinyPrefix;
                    scale = PictureScale.Tiny;
                }
                else
                {
                    thumbnailRequest = true;
                    storagePrefix = ThumbnailPrefix;
                    scale = PictureScale.Thumbnail;
                }
            }
            else if (photoName.StartsWith(ThumbnailPrefix))
            {
                photoName = photoName.Remove(0, 7);
                if (!retinaModifier)
                {
                    thumbnailRequest = true;
                    storagePrefix = ThumbnailPrefix;
                    scale = PictureScale.Thumbnail;
                }
                else
                {
                    mobileRequest = true;
                    storagePrefix = MobilePrefix;
                    scale = PictureScale.Mobile;
                }
            }
            else if (photoName.StartsWith(MobilePrefix))
            {
                photoName = photoName.Remove(0, 8);
                if (!retinaModifier)
                {
                    mobileRequest = true;
                    storagePrefix = MobilePrefix;
                    scale = PictureScale.Mobile;
                }
                else
                {
                    displayRequest = true;
                    storagePrefix = DisplayPrefix;
                    scale = PictureScale.Display;
                }
            }
            else if (photoName.StartsWith(DisplayPrefix))
            {
                photoName = photoName.Remove(0, 9);
                if (!retinaModifier)
                {
                    displayRequest = true;
                    storagePrefix = DisplayPrefix;
                    scale = PictureScale.Display;
                }
                else
                {
                    fullRequest = true;
                    storagePrefix = FullPrefix;
                    scale = PictureScale.Full;
                }
            }
            else if (photoName.StartsWith(FullPrefix))
            {
                photoName = photoName.Remove(0, 6);
                if (!retinaModifier)
                {
                    fullRequest = true;
                    storagePrefix = FullPrefix;
                    scale = PictureScale.Full;
                }
                else
                {
                    ultraRequest = true;
                    storagePrefix = UltraPrefix;
                    scale = PictureScale.Ultra;
                }
            }
            else if (photoName.StartsWith(UltraPrefix))
            {
                photoName = photoName.Remove(0, 7);
                ultraRequest = true;
                storagePrefix = UltraPrefix;
                scale = PictureScale.Ultra;
            }
            else if (photoName.StartsWith(CoverPrefix))
            {
                photoName = photoName.Remove(0, 7);
                coverRequest = true;
                storagePrefix = CoverPrefix;
                scale = PictureScale.Cover;
            }
            else
            {
                originalRequest = true;
                scale = PictureScale.Original;
            }

            string[] paths = photoName.Split('/');


            try
            {
                GalleryItem galleryItem;


                galleryItem = new GalleryItem(e.Core, e.Page.Owner, photoName);
                Gallery gallery = null;

                if (galleryItem.ParentId > 0)
                {
                    gallery = new Gallery(e.Core, e.Page.Owner, galleryItem.ParentId);
                }
                else
                {
                    gallery = new Gallery(e.Core, e.Page.Owner);
                }

                if (!gallery.Access.Can("VIEW_ITEMS"))
                {
                    e.Core.Functions.Generate403();
                    return;
                }

                if (originalRequest)
                {
                    if (!gallery.Access.Can("DOWNLOAD_ORIGINAL"))
                    {
                        e.Core.Functions.Generate403();
                        return;
                    }
                }

                e.Core.Http.SetToImageResponse(galleryItem.ContentType, galleryItem.GetCreatedDate(e.Core.Tz));

                /* we assume exists */

                /* process */

                if (!e.Core.Storage.IsCloudStorage)
                {
                    FileInfo fi = new FileInfo(e.Core.Storage.RetrieveFilePath(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.StoragePath));

                    e.Core.Http.SetToImageResponse(galleryItem.ContentType, fi.LastWriteTimeUtc);
                }

                bool scaleExists = false;

                if (scale != PictureScale.Original)
                {
                    switch (scale)
                    {
                        case PictureScale.Icon:
                            scaleExists = galleryItem.IconExists;
                            break;
                        case PictureScale.Tile:
                            scaleExists = galleryItem.TileExists;
                            break;
                        case PictureScale.Square:
                            scaleExists = galleryItem.SquareExists;
                            break;
                        case PictureScale.High:
                            scaleExists = galleryItem.HighExists;
                            break;
                        case PictureScale.Tiny:
                            scaleExists = galleryItem.TinyExists;
                            break;
                        case PictureScale.Thumbnail:
                            scaleExists = galleryItem.ThumbnailExists;
                            break;
                        case PictureScale.Mobile:
                            scaleExists = galleryItem.MobileExists;
                            break;
                        case PictureScale.Display:
                            scaleExists = galleryItem.DisplayExists;
                            break;
                        case PictureScale.Full:
                            scaleExists = galleryItem.FullExists;
                            break;
                        case PictureScale.Ultra:
                            scaleExists = galleryItem.UltraExists;
                            break;
                        case PictureScale.Cover:
                            scaleExists = galleryItem.CoverExists;
                            break;
                    }

                    if (!scaleExists)
                    {
                        scaleExists = e.Core.Storage.FileExists(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.StoragePath);
                    }

                    if (!scaleExists)
                    {
                        switch (scale)
                        {
                            case PictureScale.Icon:
                            case PictureScale.Tile:
                            case PictureScale.Square:
                            case PictureScale.High:
                                CreateScaleWithSquareRatio(e.Core, galleryItem, galleryItem.StoragePath, storagePrefix, (int)scale, (int)scale);

                                switch (scale)
                                {
                                    case PictureScale.Icon:
                                        galleryItem.IconExists = true;
                                        break;
                                    case PictureScale.Tile:
                                        galleryItem.TileExists = true;
                                        break;
                                    case PictureScale.Square:
                                        galleryItem.SquareExists = true;
                                        break;
                                    case PictureScale.High:
                                        galleryItem.HighExists = true;
                                        break;
                                }
                                break;
                            case PictureScale.Tiny:
                            case PictureScale.Thumbnail:
                            case PictureScale.Mobile:
                            case PictureScale.Display:
                            case PictureScale.Full:
                            case PictureScale.Ultra:
                                CreateScaleWithRatioPreserved(e.Core, galleryItem.StoragePath, storagePrefix, (int)scale, (int)scale);

                                switch (scale)
                                {
                                    case PictureScale.Tiny:
                                        galleryItem.TinyExists = true;
                                        break;
                                    case PictureScale.Thumbnail:
                                        galleryItem.ThumbnailExists = true;
                                        break;
                                    case PictureScale.Mobile:
                                        galleryItem.MobileExists = true;
                                        break;
                                    case PictureScale.Display:
                                        galleryItem.DisplayExists = true;
                                        break;
                                    case PictureScale.Full:
                                        galleryItem.FullExists = true;
                                        break;
                                    case PictureScale.Ultra:
                                        galleryItem.UltraExists = true;
                                        break;
                                }
                                break;
                            case PictureScale.Cover:
                                CreateCoverPhoto(e.Core, galleryItem, galleryItem.StoragePath);
                                galleryItem.CoverExists = true;
                                break;
                        }

                        galleryItem.Update();
                    }

                    if (e.Core.Storage.IsCloudStorage)
                    {
                        string imageUri = e.Core.Storage.RetrieveFileUri(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.storagePath, galleryItem.ContentType, "picture" + GetFileExtension(galleryItem.ContentType));
                        e.Core.Http.Redirect(imageUri);
                    }
                    else
                    {
                        if (galleryItem.ContentType == "image/png")
                        {
                            MemoryStream newStream = new MemoryStream();

                            Stream image = e.Core.Storage.RetrieveFile(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.StoragePath);
                            Image hoi = Image.FromStream(image);

                            hoi.Save(newStream, hoi.RawFormat);

                            e.Core.Http.WriteStream(newStream);
                        }
                        else
                        {
                            string imagePath = e.Core.Storage.RetrieveFilePath(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.storagePath);
                            e.Core.Http.TransmitFile(imagePath);
                        }
                    }
                }
                else
                {
                    if (e.Core.Storage.IsCloudStorage)
                    {
                        string imageUri = e.Core.Storage.RetrieveFileUri(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.storagePath, galleryItem.ContentType, "picture" + GetFileExtension(galleryItem.ContentType));
                        e.Core.Http.Redirect(imageUri);
                    }
                    else
                    {
                        if (galleryItem.ContentType == "image/png")
                        {
                            MemoryStream newStream = new MemoryStream();

                            Stream image = e.Core.Storage.RetrieveFile(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.StoragePath);
                            Image hoi = Image.FromStream(image);

                            hoi.Save(newStream, hoi.RawFormat);

                            e.Core.Http.WriteStream(newStream);
                        }
                        else
                        {
                            string imagePath = e.Core.Storage.RetrieveFilePath(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.storagePath);
                            e.Core.Http.TransmitFile(imagePath);
                        }
                    }
                }
            }
            catch (GalleryItemNotFoundException)
            {
                e.Core.Functions.Generate404();
                return;
            }

            if (e.Db != null)
            {
                e.Db.CloseConnection();
            }
            e.Core.Http.End();
        }

        public Size GetSize(Size bounds)
        {
            return GetSize(new Size(this.ItemWidth, this.ItemHeight), bounds);
        }

        public static Size GetSize(Size source, Size bounds)
        {
            int width = source.Width;
            int height = source.Height;
            double ratio = (double)width / height;

            if (source.Width > bounds.Height || source.Height > bounds.Height)
            {
                if (width >= height)
                {
                    width = bounds.Width;
                    height = (int)(bounds.Width / ratio);
                }
                else
                {
                    height = bounds.Height;
                    width = (int)(bounds.Height * ratio);
                }
            }

            return new Size(width, height);
        }

        public Size GetTileSize(Size bounds)
        {
            return GetTileSize(new Size(this.ItemWidth, this.ItemHeight), bounds);
        }

        public static Size GetTileSize(Size source, Size bounds)
        {
            int width = source.Width;
            int height = source.Height;
            double ratio = (double)width / height;

            if (width < height)
            {
                width = bounds.Width;
                height = (int)(bounds.Width / ratio);
            }
            else
            {
                height = bounds.Height;
                width = (int)(bounds.Height * ratio);
            }

            return new Size(width, height);
        }

        public static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        /// <summary>
        /// Thumbnail size fits into a x * y display area. Icons are trimmed to an exact x * y pixel display size.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        public static void CreateScaleWithSquareRatio(Core core, GalleryItem gi, string fileName, string bin, int width, int height)
        {
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                fs.Position = 0;
                long newLength = fs.Length;
                byte[] data = new byte[newLength];

                int bytesRead = 0;
                int totalBytesRead = 0;
                while ((bytesRead = fs.Read(data, totalBytesRead, (int)newLength - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }

                ImageMagick.WandGenesis();
                IntPtr wand = ImageMagick.NewWand();

                ImageMagick.ReadImageBlob(wand, data);

                Size imageSize = new Size(ImageMagick.GetWidth(wand).ToInt32(), ImageMagick.GetHeight(wand).ToInt32());
                Size newSize = GetTileSize(imageSize, new Size(width, height));

                int square = Math.Min(imageSize.Width, imageSize.Height);
                int cropX = imageSize.Width - square;
                int cropY = imageSize.Height - square;

                ImageMagick.CropImage(wand, (IntPtr)square, (IntPtr)square, (IntPtr)(cropX / 2), (IntPtr)(cropY / 2));
                ImageMagick.ResizeImage(wand, (IntPtr)(width), (IntPtr)(height), ImageMagick.Filter.Lanczos, 1.0);
                ImageMagick.SetImagePage(wand, (IntPtr)0, (IntPtr)0, (IntPtr)0, (IntPtr)0);

                byte[] newdata = ImageMagick.GetImageBlob(wand);

                ImageMagick.DestroyWand(wand);
                ImageMagick.WandTerminus();

                MemoryStream stream = new MemoryStream();
                stream.Write(newdata, 0, newdata.Length);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                stream.Close();
                fs.Close();
            }
            else
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                Image image = Image.FromStream(fs);
                Bitmap displayImage;

                displayImage = new Bitmap(width, height, image.PixelFormat);
                displayImage.Palette = image.Palette;

                Graphics g = Graphics.FromImage(displayImage);
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                int square = Math.Min(image.Width, image.Height);
                g.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle((image.Width - square) / 2, (image.Height - square) / 2, square, square), GraphicsUnit.Pixel);

                MemoryStream stream = new MemoryStream();
                if (image.RawFormat == ImageFormat.Jpeg)
                {
                    ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(2);
                    EncoderParameter myEncoderParameter = new EncoderParameter(encoder, 90L);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    myEncoderParameters.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (int)EncoderValue.RenderProgressive);

                    displayImage.Save(stream, codecInfo, myEncoderParameters);
                }
                else
                {
                    displayImage.Save(stream, image.RawFormat);
                }
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                stream.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// Thumbnail size fits into a x * y display area. The aspect ratio is preserved.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        private static void CreateScaleWithRatioPreserved(Core core, string fileName, string bin, int width, int height)
        {
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                fs.Position = 0;
                long newLength = fs.Length;
                byte[] data = new byte[newLength];

                int bytesRead = 0;
                int totalBytesRead = 0;
                while ((bytesRead = fs.Read(data, totalBytesRead, (int)newLength - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }

                ImageMagick.WandGenesis();
                IntPtr wand = ImageMagick.NewWand();

                ImageMagick.ReadImageBlob(wand, data);

                Size imageSize = new Size(ImageMagick.GetWidth(wand).ToInt32(), ImageMagick.GetHeight(wand).ToInt32());

                if (imageSize.Width > width || imageSize.Height > height)
                {
                    Size newSize = GetSize(imageSize, new Size(width, height));
                    int newWidth = newSize.Width;
                    int newHeight = newSize.Height;

                    ImageMagick.ResizeImage(wand, (IntPtr)newWidth, (IntPtr)newHeight, ImageMagick.Filter.Lanczos, 1.0);

                    byte[] newdata = ImageMagick.GetImageBlob(wand);

                    ImageMagick.DestroyWand(wand);
                    ImageMagick.WandTerminus();

                    MemoryStream stream = new MemoryStream();
                    stream.Write(newdata, 0, newdata.Length);
                    core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                    stream.Close();
                    fs.Close();
                }
                else
                {
                    fs.Close();
                    core.Storage.CopyFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName);
                }
            }
            else
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                Image image = Image.FromStream(fs);
                Image thumbImage;

                if (image.Width > width || image.Height > height)
                {
                    Size newSize = GetSize(image.Size, new Size(width, height));

                    thumbImage = new Bitmap(newSize.Width, newSize.Height, image.PixelFormat);
                    thumbImage.Palette = image.Palette;
                    Graphics g = Graphics.FromImage(thumbImage);
                    g.Clear(Color.Transparent);
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    g.DrawImage(image, new Rectangle(0, 0, newSize.Width, newSize.Height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

                    MemoryStream stream = new MemoryStream();
                    thumbImage.Save(stream, image.RawFormat);
                    core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                    stream.Close();
                    fs.Close();
                }
                else
                {
                    fs.Close();
                    core.Storage.CopyFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName);
                }
            }
        }

        /// <summary>
        /// Thumbnail size fits into a x * y display area. Icons are trimmed to an exact x * y pixel display size.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        public static void CreateCoverPhoto(Core core, GalleryItem gi, string fileName)
        {
            string bin = "_cover";
            int width = 960;
            int height = 200;
            int crop = gi.CropPositionVertical;
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                fs.Position = 0;
                long newLength = fs.Length;
                byte[] data = new byte[newLength];

                int bytesRead = 0;
                int totalBytesRead = 0;
                while ((bytesRead = fs.Read(data, totalBytesRead, (int)newLength - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }

                ImageMagick.WandGenesis();
                IntPtr wand = ImageMagick.NewWand();

                ImageMagick.ReadImageBlob(wand, data);

                Size imageSize = new Size(ImageMagick.GetWidth(wand).ToInt32(), ImageMagick.GetHeight(wand).ToInt32());
                double scale = (double)width / imageSize.Width;
                int newHeight = (int)(imageSize.Height * scale);
                int cropY = (int)(crop * scale);

                ImageMagick.ResizeImage(wand, (IntPtr)(width), (IntPtr)(newHeight), ImageMagick.Filter.Lanczos, 1.0);
                ImageMagick.CropImage(wand, (IntPtr)width, (IntPtr)height, (IntPtr)(0), (IntPtr)(cropY));

                byte[] newdata = ImageMagick.GetImageBlob(wand);

                ImageMagick.DestroyWand(wand);
                ImageMagick.WandTerminus();

                MemoryStream stream = new MemoryStream();
                stream.Write(newdata, 0, newdata.Length);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                stream.Close();
                fs.Close();
            }
            else
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                Image image = Image.FromStream(fs);
                Bitmap displayImage;

                displayImage = new Bitmap(width, height, image.PixelFormat);
                displayImage.Palette = image.Palette;

                Graphics g = Graphics.FromImage(displayImage);
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                double scale = (double)width / image.Width;
                int newHeight = (int)(image.Height * scale);
                int oldHeight = (int)(height / scale);

                g.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(0, crop, image.Width, oldHeight), GraphicsUnit.Pixel);

                MemoryStream stream = new MemoryStream();
                if (image.RawFormat == ImageFormat.Jpeg)
                {
                    ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(2);
                    EncoderParameter myEncoderParameter = new EncoderParameter(encoder, 90L);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    myEncoderParameters.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (int)EncoderValue.RenderProgressive);

                    displayImage.Save(stream, codecInfo, myEncoderParameters);
                }
                else
                {
                    displayImage.Save(stream, image.RawFormat);
                }
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream);
                stream.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// Abort image resize handler
        /// </summary>
        /// <returns></returns>
        private static bool abortResize()
        {
            return false;
        }

        /// <summary>
        /// Returns gallery item Id
        /// </summary>
        public override long Id
        {
            get
            {
                return itemId;
            }
        }

        /// <summary>
        /// Returns gallery item uri
        /// </summary>
        public override string Uri
        {
            get
            {
                return BuildUri();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string EditUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "edit-photo", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MakeDisplayPicUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "display-pic", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MakeProfileCoverPhotoUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "profile-cover", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SetGalleryCoverUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "gallery-cover", itemId, true);
            }
        }

        public string RotateLeftUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "rotate-photo", true, new string[] { "id=" + itemId.ToString() , "rotation=left" });
            }
        }

        public string RotateRightUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "rotate-photo", true, new string[] { "id=" + itemId.ToString(), "rotation=right" });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DeleteUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "delete", itemId, true);
            }
        }

        public string TagUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri("galleries", "tag", itemId, true);
            }
        }

        /// <summary>
        /// Returns the gallery item icon uri
        /// </summary>
        public string IconUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_icon/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_icon/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string TileUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_tile/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_tile/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string SquareUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_square/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_square/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string HighUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_high/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_high/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string TinyUri
        {
            get
            {
                string size = "_tiny";
                if (ItemWidth <= 80 && ItemHeight <= 80)
                {
                    return UltraUri;
                }

                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string ThumbnailUri
        {
            get
            {
                string size = "_thumb";
                if (ItemWidth <= 160 && ItemHeight <= 160)
                {
                    return UltraUri;
                }

                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string MobileUri
        {
            get
            {
                string size = "_mobile";
                if (ItemWidth <= 320 && ItemHeight <= 320)
                {
                    return UltraUri;
                }

                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string DisplayUri
        {
            get
            {
                string size = "_display";
                if (ItemWidth <= 640 && ItemHeight <= 640)
                {
                    return UltraUri;
                }

                if (parentId > 0)
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, FullPath, size));
                    }
                    else
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                            Owner.UriStub, FullPath, size));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, path, size));
                    }
                    else
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                            Owner.UriStub, path, size));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string FullUri
        {
            get
            {
                if (ItemWidth <= 1280 && ItemHeight <= 1280)
                {
                    return UltraUri;
                }

                if (parentId > 0)
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/_full/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, FullPath));
                    }
                    else
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/_full/{1}",
                            Owner.UriStub, FullPath));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/_full/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, path));
                    }
                    else
                    {
                        return core.Uri.AppendSid(string.Format("{0}images/_full/{1}",
                            Owner.UriStub, path));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string UltraUri
        {
            get
            {
                string size = "_ultra";
                /*if (ItemWidth <= 2560 && ItemHeight <= 2560)
                {
                    return OriginalUri;
                }*/

                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        public string OriginalUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item cover uri
        /// </summary>
        public string CoverUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_cover/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}images/_cover/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns gallery item comment count
        /// </summary>
        public long Comments
        {
            get
            {
                return itemComments;
            }
        }

        #region ICommentableItem Members

        /// <summary>
        /// Returns the gallery item comment sort order
        /// </summary>
        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        /// <summary>
        /// Returns the gallery item comment count per page
        /// </summary>
        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion

        #region IActionableItem Members

        /// <summary>
        /// Returns Feed Action body
        /// </summary>
        /// <returns></returns>
        public string GetActionBody()
        {
            return string.Format("[iurl={0}][thumb]{1}/{2}[/thumb][/iurl]",
                BuildUri(), ParentPath, Path);
        }

        public string RebuildAction(BoxSocial.Internals.Action action)
        {
            throw new NotImplementedException();
        }

        #endregion

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
    }

    /// <summary>
    /// The exception that is thrown when a gallery item has not been found.
    /// </summary>
    public class GalleryItemNotFoundException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when a photo is too large to accept.
    /// </summary>
    public class GalleryItemTooLargeException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when the gallery file size quote has been exceeded.
    /// </summary>
    public class GalleryQuotaExceededException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when a gallery item is not owned by a user, group, or network.
    /// </summary>
    public class InvalidGalleryItemTypeException : Exception
    {
    }

    /// <summary>
    /// The exception that is thrown when a gallery item has an invalid file name.
    /// </summary>
    public class InvalidGalleryFileNameException : Exception
    {
    }
}
