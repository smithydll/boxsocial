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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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

        MobileCover = 480,
        Cover = 960,

        Original = 0,
    }

    /// <summary>
    /// Represents a gallery photo
    /// </summary>
    [DataTable("gallery_items", "PHOTO")]
    [JsonObject("gallery_item")]
    public class GalleryItem : NumberedItem, ICommentableItem, ILikeableItem, IPermissibleSubItem, IActionableSubItem, ISearchableItem, IShareableItem, IActionableItem, IEmbeddableItem
    {
        // Square
        public static string IconPrefix = "_icon"; // 50
        public static string TilePrefix = "_tile"; // 100
        public static string SquarePrefix = "_square"; // 200
        public static string HighPrefix = "_high"; // 400

        // Ratio
        public static string TinyPrefix = "_tiny"; // 80
        public static string ThumbnailPrefix = "_thumb"; // 160
        public static string MobilePrefix = "_mobile"; // 320
        public static string DisplayPrefix = "_display"; // 640
        public static string FullPrefix = "_full"; // 1280
        public static string UltraPrefix = "_ultra"; // 2560

        // Cover
        public static string CoverPrefix = "_cover"; // 960
        // Mobile Cover
        public static string MobileCoverPrefix = "_mcover"; // 960

        [DataField("user_id")]
        protected long userId;
        [DataField("gallery_item_id", DataFieldKeys.Primary)]
        protected long itemId;
        [DataField("gallery_item_title", 63)]
        protected string itemTitle;
        [DataField("gallery_item_parent_path", MYSQL_TEXT)]
        protected string parentPath;
        [DataField("gallery_item_uri", 63)]
        protected string path;
        [DataField("gallery_id", typeof(Gallery))]
        protected long parentId;
        [DataField("gallery_item_views")]
        protected long itemViews;
        [DataField("gallery_item_width")]
        protected int itemWidth;
        [DataField("gallery_item_height")]
        protected int itemHeight;
        [DataField("gallery_item_bytes")]
        protected long itemBytes;
        [DataField("gallery_item_rating")]
        protected float itemRating;
        [DataField("gallery_item_content_type", 31)]
        protected string contentType;
        [DataField("gallery_item_storage_path", 128)]
        protected string storagePath;
        [DataField("gallery_item_abstract", MYSQL_TEXT)]
        protected string itemAbstract;
        [DataField("gallery_item_date_ut")]
        private long itemCreatedRaw;
        [DataField("gallery_item_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("gallery_item_classification")]
        protected byte classification;
        [DataField("gallery_item_license")]
        protected byte license;
        [DataField("gallery_item_icon_exists")]
        protected bool iconExists;
        [DataField("gallery_item_tile_exists")]
        protected bool tileExists;
        [DataField("gallery_item_square_exists")]
        protected bool squareExists;
        [DataField("gallery_item_high_exists")]
        protected bool highExists;
        [DataField("gallery_item_tiny_exists")]
        protected bool tinyExists;
        [DataField("gallery_item_thumb_exists")]
        protected bool thumbnailExists;
        [DataField("gallery_item_mobile_exists")]
        protected bool mobileExists;
        [DataField("gallery_item_display_exists")]
        protected bool displayExists;
        [DataField("gallery_item_full_exists")]
        protected bool fullExists;
        [DataField("gallery_item_ultra_exists")]
        protected bool ultraExists;
        [DataField("gallery_item_cover_exists")]
        protected bool coverExists;
        [DataField("gallery_item_mobile_cover_exists")]
        protected bool mobileCoverExists;
        [DataField("gallery_item_vcrop")]
        protected int cropPositionVertical;
        [DataField("gallery_item_hcrop")]
        protected int cropPositionHorizontal;
        [DataField("gallery_item_application_id")]
        private long applicationId;

        /// <summary>
        /// Owner of the photo
        /// </summary>
        protected Primitive owner;

        /// <summary>
        /// 
        /// </summary>
        protected ContentLicense licenseInfo;

        protected Gallery gallery;

        public event CommentHandler OnCommentPosted;

        [JsonProperty("gallery")]
        public Gallery Parent
        {
            get
            {
                if (gallery == null || gallery.Id != parentId)
                {
                    if (parentId > 0)
                    {
                        gallery = new Gallery(core, parentId);
                    }
                    else
                    {
                        gallery = new Gallery(core, Owner);
                    }
                }
                return gallery;
            }
        }

        /// <summary>
        /// Gets the gallery photo Id
        /// </summary>
        [JsonProperty("id")]
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
        [JsonProperty("title")]
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
        [JsonProperty("parent_path")]
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
        [JsonProperty("path")]
        public string Path
        {
            get
            {
                return path;
            }
        }
        
        [JsonIgnore]
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

        [JsonIgnore]
        public string PostPath
        {
            get
            {
                if (string.IsNullOrEmpty(parentPath))
                {
                    return path;
                }
                else
                {
                    return Owner.UriStub + "/gallery/" + parentPath + "/" + path;
                }
            }
        }

        /// <summary>
        /// Gets the number of views
        /// </summary>
        [JsonIgnore]
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
        [JsonProperty("width")]
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
        [JsonProperty("height")]
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
        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonProperty("content_type")]
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
        [JsonIgnore]
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
        [JsonProperty("description")]
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

        [JsonProperty("time_ut")]
        public long CreatedDateRaw
        {
            get
            {
                return itemCreatedRaw;
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
        [JsonProperty("gallery_id")]
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
        [JsonIgnore]
        public ContentLicense License
        {
            get
            {
                if (licenseInfo == null && LicenseId > 0)
                {
                    licenseInfo = new ContentLicense(core, LicenseId);
                }
                return licenseInfo;
            }
        }

        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonProperty("owner")]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
        public bool MobileCoverExists
        {
            get
            {
                return mobileCoverExists;
            }
            internal set
            {
                SetPropertyByRef(new { mobileCoverExists }, value);
            }
        }

        [JsonIgnore]
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
                MobileCoverExists = false;
                core.Storage.DeleteFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_cover"), this.StoragePath);
                core.Storage.DeleteFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_mcover"), this.StoragePath);
            }
        }

        [JsonIgnore]
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

            SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
            query.AddCondition("gallery_item_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_item_uri", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            System.Data.Common.DbDataReader galleryItemReader = db.ReaderQuery(query);

            if (galleryItemReader.HasRows)
            {
                galleryItemReader.Read();

                loadItemInfo(galleryItemReader);
                /*try
                {
                    licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }*/

                galleryItemReader.Close();
                galleryItemReader.Dispose();
            }
            else
            {
                galleryItemReader.Close();
                galleryItemReader.Dispose();

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

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
        }

        public GalleryItem(Core core, User owner, System.Data.Common.DbDataReader itemRow)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
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

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
        }

        public GalleryItem(Core core, Primitive owner, System.Data.Common.DbDataReader itemRow)
            : base(core)
        {
            this.owner = owner;

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
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

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
        }

        public GalleryItem(Core core, System.Data.Common.DbDataReader itemRow)
            : base(core)
        {
            // TODO: owner not set, no big worry

            loadItemInfo(itemRow);
            /*try
            {
                licenseInfo = new ContentLicense(core, itemRow);
            }
            catch (InvalidLicenseException)
            {
            }*/
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

            SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
            query.AddCondition("gallery_item_parent_path", Gallery.GetParentPath(path));
            query.AddCondition("gallery_item_uri", Gallery.GetNameFromPath(path));
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            DataTable galleryItemTable = db.Query(query);

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(galleryItemTable.Rows[0]);
                /*try
                {
                    licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
                }
                catch (InvalidLicenseException)
                {
                }*/
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

            SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
            query.AddCondition("gallery_item_id", itemId);
            query.AddCondition("gallery_item_item_id", owner.Id);
            query.AddCondition("gallery_item_item_type_id", owner.TypeId);

            System.Data.Common.DbDataReader galleryItemReader = db.ReaderQuery(query);

            try
            {
                if (galleryItemReader.HasRows)
                {
                    galleryItemReader.Read();
                    loadItemInfo(galleryItemReader);
                }
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
            finally
            {
                galleryItemReader.Close();
                galleryItemReader.Dispose();
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="itemId">Gallery item Id</param>
        public GalleryItem(Core core, long itemId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(GalleryItem_ItemLoad);

            SelectQuery query = GalleryItem.GetSelectQueryStub(core, typeof(GalleryItem));
            query.AddCondition("gallery_item_id", itemId);

            DataTable galleryItemTable = db.Query(query);

            if (galleryItemTable.Rows.Count < 1)
            {
                throw new GalleryItemNotFoundException();
            }

            try
            {
                loadItemInfo(galleryItemTable.Rows[0]);

                //licenseInfo = new ContentLicense(core, galleryItemTable.Rows[0]);
            }
            catch (InvalidItemException)
            {
                throw new GalleryItemNotFoundException();
            }
            catch (InvalidLicenseException)
            {
            }
        }

        protected override void loadItemInfo(DataRow galleryItemRow)
        {
            loadValue(galleryItemRow, "user_id", out userId);
            loadValue(galleryItemRow, "gallery_item_id", out itemId);
            loadValue(galleryItemRow, "gallery_item_title", out itemTitle);
            loadValue(galleryItemRow, "gallery_item_parent_path", out parentPath);
            loadValue(galleryItemRow, "gallery_item_uri", out path);
            loadValue(galleryItemRow, "gallery_id", out parentId);
            loadValue(galleryItemRow, "gallery_item_views", out itemViews);
            loadValue(galleryItemRow, "gallery_item_width", out itemWidth);
            loadValue(galleryItemRow, "gallery_item_height", out itemHeight);
            loadValue(galleryItemRow, "gallery_item_bytes", out itemBytes);
            loadValue(galleryItemRow, "gallery_item_rating", out itemRating);
            loadValue(galleryItemRow, "gallery_item_content_type", out contentType);
            loadValue(galleryItemRow, "gallery_item_storage_path", out storagePath);
            loadValue(galleryItemRow, "gallery_item_abstract", out itemAbstract);
            loadValue(galleryItemRow, "gallery_item_date_ut", out itemCreatedRaw);
            loadValue(galleryItemRow, "gallery_item_item", out ownerKey);
            loadValue(galleryItemRow, "gallery_item_classification", out classification);
            loadValue(galleryItemRow, "gallery_item_license", out license);
            loadValue(galleryItemRow, "gallery_item_icon_exists", out iconExists);
            loadValue(galleryItemRow, "gallery_item_tile_exists", out tileExists);
            loadValue(galleryItemRow, "gallery_item_square_exists", out squareExists);
            loadValue(galleryItemRow, "gallery_item_high_exists", out highExists);
            loadValue(galleryItemRow, "gallery_item_tiny_exists", out tinyExists);
            loadValue(galleryItemRow, "gallery_item_thumb_exists", out thumbnailExists);
            loadValue(galleryItemRow, "gallery_item_mobile_exists", out mobileExists);
            loadValue(galleryItemRow, "gallery_item_display_exists", out displayExists);
            loadValue(galleryItemRow, "gallery_item_full_exists", out fullExists);
            loadValue(galleryItemRow, "gallery_item_ultra_exists", out ultraExists);
            loadValue(galleryItemRow, "gallery_item_cover_exists", out coverExists);
            loadValue(galleryItemRow, "gallery_item_mobile_cover_exists", out mobileCoverExists);
            loadValue(galleryItemRow, "gallery_item_vcrop", out cropPositionVertical);
            loadValue(galleryItemRow, "gallery_item_hcrop", out cropPositionHorizontal);
            loadValue(galleryItemRow, "gallery_item_application_id", out applicationId);

            itemLoaded(galleryItemRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader galleryItemRow)
        {
            loadValue(galleryItemRow, "user_id", out userId);
            loadValue(galleryItemRow, "gallery_item_id", out itemId);
            loadValue(galleryItemRow, "gallery_item_title", out itemTitle);
            loadValue(galleryItemRow, "gallery_item_parent_path", out parentPath);
            loadValue(galleryItemRow, "gallery_item_uri", out path);
            loadValue(galleryItemRow, "gallery_id", out parentId);
            loadValue(galleryItemRow, "gallery_item_views", out itemViews);
            loadValue(galleryItemRow, "gallery_item_width", out itemWidth);
            loadValue(galleryItemRow, "gallery_item_height", out itemHeight);
            loadValue(galleryItemRow, "gallery_item_bytes", out itemBytes);
            loadValue(galleryItemRow, "gallery_item_rating", out itemRating);
            loadValue(galleryItemRow, "gallery_item_content_type", out contentType);
            loadValue(galleryItemRow, "gallery_item_storage_path", out storagePath);
            loadValue(galleryItemRow, "gallery_item_abstract", out itemAbstract);
            loadValue(galleryItemRow, "gallery_item_date_ut", out itemCreatedRaw);
            loadValue(galleryItemRow, "gallery_item_item", out ownerKey);
            loadValue(galleryItemRow, "gallery_item_classification", out classification);
            loadValue(galleryItemRow, "gallery_item_license", out license);
            loadValue(galleryItemRow, "gallery_item_icon_exists", out iconExists);
            loadValue(galleryItemRow, "gallery_item_tile_exists", out tileExists);
            loadValue(galleryItemRow, "gallery_item_square_exists", out squareExists);
            loadValue(galleryItemRow, "gallery_item_high_exists", out highExists);
            loadValue(galleryItemRow, "gallery_item_tiny_exists", out tinyExists);
            loadValue(galleryItemRow, "gallery_item_thumb_exists", out thumbnailExists);
            loadValue(galleryItemRow, "gallery_item_mobile_exists", out mobileExists);
            loadValue(galleryItemRow, "gallery_item_display_exists", out displayExists);
            loadValue(galleryItemRow, "gallery_item_full_exists", out fullExists);
            loadValue(galleryItemRow, "gallery_item_ultra_exists", out ultraExists);
            loadValue(galleryItemRow, "gallery_item_cover_exists", out coverExists);
            loadValue(galleryItemRow, "gallery_item_mobile_cover_exists", out mobileCoverExists);
            loadValue(galleryItemRow, "gallery_item_vcrop", out cropPositionVertical);
            loadValue(galleryItemRow, "gallery_item_hcrop", out cropPositionHorizontal);
            loadValue(galleryItemRow, "gallery_item_application_id", out applicationId);

            itemLoaded(galleryItemRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void GalleryItem_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(GalleryItem_CommentPosted);
            ItemDeleted += new ItemDeletedEventHandler(GalleryItem_ItemDeleted);
            ItemUpdated += new EventHandler(GalleryItem_ItemUpdated);
        }

        void GalleryItem_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(this);
        }

        void GalleryItem_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            core.Search.DeleteFromIndex(this);
            ActionableItem.CleanUp(core, this);
        }

        bool GalleryItem_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                core.CallingApplication.QueueNotifications(core, e.Comment.ItemKey, "notifyGalleryItemComment");
                /*core.CallingApplication.SendNotification(core, (User)Owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your photo.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                    e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));*/
            }

            return true;
        }

        public static void NotifyGalleryItemComment(Core core, Job job)
        {
            try {
                Comment comment = new Comment(core, job.ItemId);
                GalleryItem ev = new GalleryItem(core, comment.CommentedItemKey.Id);

                if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.OwnerKey)))
                {
                    core.CallingApplication.SendNotification(core, comment.User, (User)ev.Owner, ev.OwnerKey, ev.ItemKey, "_COMMENTED_GALLERY_ITEM", comment.BuildUri(ev));
                }

                core.CallingApplication.SendNotification(core, comment.OwnerKey, comment.User, ev.OwnerKey, ev.ItemKey, "_COMMENTED_GALLERY_ITEM", comment.BuildUri(ev));
            }
            catch (InvalidCommentException)
            {
            }
            catch (GalleryItemNotFoundException)
            {

            }
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
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

        /// <summary>
        /// Delete the gallery item
        /// </summary>
        public override long Delete()
        {
            SelectQuery squery = new SelectQuery("gallery_items gi");
            squery.AddFields("COUNT(*) AS number");
            squery.AddCondition("gallery_item_storage_path", storagePath);

            DataTable results = db.Query(squery);

            /*DeleteQuery dquery = new DeleteQuery("gallery_items");
            dquery.AddCondition("gallery_item_id", itemId);
            dquery.AddCondition("user_id", core.LoggedInMemberId);*/

            long deleted = base.Delete();
            if (deleted > 0)
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

                return deleted;
            }
            else
            {
                //TODO: throw new
                throw new Exception("Unauthorised");
            }
        }

        public static GalleryItem Create(Core core, Primitive owner, Gallery parent, string title, ref string slug, string fileName, string contentType, ulong bytes, string description, byte license, Classifications classification, Stream stream, bool highQuality /*int width, int height*/)
        {
            return Create(core, owner, parent, title, ref slug, fileName, contentType, bytes, description, license, classification, stream, highQuality, 0);
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
        public static GalleryItem Create(Core core, Primitive owner, Gallery parent, string title, ref string slug, string fileName, string contentType, ulong bytes, string description, byte license, Classifications classification, Stream stream, bool highQuality, long applicationId /*int width, int height*/)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (stream == null)
            {
                throw new Exception("The image stream is empty!");
            }

            int width, height;
            stream = OrientImage(stream, out width, out height);

            string storageName = Storage.HashFile(stream);

            /*
             * scale and save
             */
            string storageFilePath = string.Empty;

            if (highQuality || (width <= (int)PictureScale.Ultra && height <= (int)PictureScale.Ultra))
            {
                core.Storage.SaveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), storageName, stream, contentType);
            }
            else
            {
                int originalWidth = width;
                int originalHeight = height;

                Size newSize = GalleryItem.GetSize(new Size(width, height), new Size((int)PictureScale.Ultra, (int)PictureScale.Ultra));
                width = newSize.Width;
                height = newSize.Height;

                if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
                {
                    storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", storageName);

                    if (stream is MemoryStream)
                    {
                        MemoryStream ms = (MemoryStream)stream;
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ms.WriteTo(ds);
                        ds.Close();
                    }
                    else
                    {
                        stream.Position = 0;
                        byte[] b = new byte[stream.Length];
                        stream.Read(b, 0, (int)stream.Length);
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ds.Write(b, 0, b.Length);
                        ds.Close();
                    }
                }

                CreateScaleWithRatioPreserved(core, contentType, stream, storageName, "_storage", (int)PictureScale.Ultra, (int)PictureScale.Ultra);
            }

            /*
             * create thumbnails
             */
            bool tinyExists = false;
            bool thumbExists = false;
            bool mobileExists = false;
            bool displayExists = false;
            bool fullExists = false;
            bool ultraExists = false;

            switch (core.Medium)
            {
                case Forms.DisplayMedium.Desktop:
                    if (width > (int)PictureScale.Display || height > (int)PictureScale.Display)
                    {
                        displayExists = CreateScaleWithRatioPreserved(core, contentType, stream, storageName, DisplayPrefix, (int)PictureScale.Display, (int)PictureScale.Display);
                    }
                    else
                    {
                        // This strips all uploaded images of EXIF data
                        displayExists = CreateScaleWithRatioPreserved(core, contentType, stream, storageName, DisplayPrefix, width, height);
                    }
                    break;
                case Forms.DisplayMedium.Mobile:
                    if (width > (int)PictureScale.Mobile || height > (int)PictureScale.Mobile)
                    {
                        mobileExists = CreateScaleWithRatioPreserved(core, contentType, stream, storageName, MobilePrefix, (int)PictureScale.Mobile, (int)PictureScale.Mobile);
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(storageFilePath))
            {
                if (File.Exists(storageFilePath))
                {
                    try
                    {
                        File.Delete(storageFilePath);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }

            /*
             * save the image
             */

            Mysql db = core.Db;

            if (owner is User)
            {
                if (owner.Id != core.LoggedInMemberId)
                {
                    throw new Exception("Error, user IDs don't match");
                }
            }

            if (bytes > (ulong)core.Settings.MaxFileSize)
            {
                throw new GalleryItemTooLargeException();
            }

            if (core.Session.LoggedInMember.UserInfo.BytesUsed + bytes > (ulong)core.Settings.MaxUserStorage)
            {
                throw new GalleryQuotaExceededException();
            }

            switch (contentType)
            {
                case "image/png":
                case "image/jpeg":
                case "image/pjpeg":
                case "image/gif":
                    break;
                default:
                    throw new InvalidGalleryItemTypeException();
            }

            title = Functions.TrimStringToWord(title);

            slug = GalleryItem.GetSlugFromFileName(fileName, slug);
            slug = Functions.TrimStringWithExtension(slug);

            if (slug != "image.jpg")
            {
                GalleryItem.EnsureGallerySlugUnique(core, parent, owner, ref slug);
            }

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
            iQuery.AddField("gallery_item_tiny_exists", tinyExists);
            iQuery.AddField("gallery_item_thumb_exists", thumbExists);
            iQuery.AddField("gallery_item_mobile_exists", mobileExists);
            iQuery.AddField("gallery_item_display_exists", displayExists);
            iQuery.AddField("gallery_item_full_exists", fullExists);
            iQuery.AddField("gallery_item_ultra_exists", ultraExists);
            iQuery.AddField("gallery_item_cover_exists", false);
            iQuery.AddField("gallery_item_mobile_cover_exists", false);
            iQuery.AddField("gallery_item_width", width);
            iQuery.AddField("gallery_item_height", height);
            iQuery.AddField("gallery_item_vcrop", 0);
            iQuery.AddField("gallery_item_hcrop", 0);
            iQuery.AddField("gallery_item_application_id", applicationId);

            // we want to use transactions
            long itemId = db.Query(iQuery);

            if (itemId >= 0)
            {
                // ios uploads anonymously
                if (slug == "image.jpg")
                {
                    slug = string.Format("image-{0}.jpg", itemId);
                    UpdateQuery iosQuery = new UpdateQuery("gallery_items");
                    iosQuery.AddField("gallery_item_uri", slug);
                    iosQuery.AddCondition("gallery_item_id", itemId);

                    db.Query(iosQuery);
                }

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

                GalleryItem newGalleryItem = new GalleryItem(core, owner, itemId);
                core.Db.CommitTransaction();

                core.Search.Index(newGalleryItem);

                if (core.Queue != null)
                {
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_ultra"));
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_full"));
                    switch (core.Medium)
                    {
                        case Forms.DisplayMedium.Desktop:
                            core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_mobile"));
                            break;
                        case Forms.DisplayMedium.Mobile:
                            core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_display"));
                            break;
                    }
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_thumb"));
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_tiny"));

                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_high"));
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_square"));
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_tile"));
                    core.Queue.PushJob(new Job(core.Settings.QueueDefaultPriority, core.CallingApplication.Id, core.LoggedInMemberId, newGalleryItem.ItemKey.TypeId, newGalleryItem.ItemKey.Id, "create_icon"));
                }

                return newGalleryItem;
                //return itemId;
            }

            throw new Exception("Transaction failed, panic!");
        }

        private static Stream OrientImage(Stream input, out int width, out int height)
        {
            System.Drawing.Image image = System.Drawing.Image.FromStream(input);
            width = image.Width;
            height = image.Height;

            RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone;
            foreach (PropertyItem p in image.PropertyItems)
            {
                if (p.Id == 274)
                {
                    switch ((int)p.Value[0])
                    {
                        case 1:
                            rotate = RotateFlipType.RotateNoneFlipNone;
                            break;
                        case 2:
                            rotate = RotateFlipType.RotateNoneFlipX;
                            break;
                        case 3:
                            rotate = RotateFlipType.Rotate180FlipNone;
                            break;
                        case 4:
                            rotate = RotateFlipType.Rotate180FlipX;
                            break;
                        case 5:
                            rotate = RotateFlipType.Rotate90FlipX;
                            break;
                        case 6:
                            rotate = RotateFlipType.Rotate90FlipNone;
                            break;
                        case 7:
                            rotate = RotateFlipType.Rotate270FlipX;
                            break;
                        case 8:
                            rotate = RotateFlipType.Rotate270FlipNone;
                            break;
                        default:
                            rotate = RotateFlipType.RotateNoneFlipNone;
                            break;
                    }
                }
            }

            if (rotate != RotateFlipType.RotateNoneFlipNone)
            {
                image.RotateFlip(rotate);

                width = image.Width;
                height = image.Height;

                ImageFormat iF = ImageFormat.Jpeg;
                input = new MemoryStream();
                image.Save(input, iF);
                rotate = RotateFlipType.RotateNoneFlipNone;
            }

            return input;
        }

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

            string newFileName = core.Storage.SaveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), stream, ContentType);
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
                uquery.AddField("gallery_item_mobile_cover_exists", false);
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
                return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}/{2}",
                    Owner.UriStub, parentPath, path));
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("{0}gallery/{1}",
                    Owner.UriStub, path));
            }
        }

        public static void Show(Core core)
        {
            long itemId = core.Functions.RequestLong("item_id", 0);
            long itemTypeId = core.Functions.RequestLong("item_type_id", 0);
            string path = core.Http.Query["path"];
            long ownerId = core.Functions.RequestLong("owner_id", 0);
            long ownerTypeId = core.Functions.RequestLong("owner_type_id", 0);
            ItemKey ownerKey = new ItemKey(ownerId, ownerTypeId);

            try
            {
                core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                Primitive owner = core.PrimitiveCache[ownerKey];

                GalleryItem galleryItem = null;
                if (itemId != 0)
                {
                    galleryItem = new GalleryItem(core, itemId);
                }
                else
                {
                    galleryItem = new GalleryItem(core, owner, path);
                }

                Gallery gallery = null;

                if (galleryItem.parentId > 0)
                {
                    gallery = new Gallery(core, galleryItem.parentId);
                }
                else
                {
                    gallery = new Gallery(core, owner);
                }

                if (!gallery.Access.Can("VIEW_ITEMS"))
                {
                    core.Functions.Generate403();
                    return;
                }

                JsonSerializer js;
                StringWriter jstw;
                JsonTextWriter jtw;

                js = new JsonSerializer();
                jstw = new StringWriter();
                jtw = new JsonTextWriter(jstw);

                js.NullValueHandling = NullValueHandling.Ignore;

                core.Http.WriteJson(js, galleryItem);
            }
            catch
            {
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

                if (gallery.Access.Can("EDIT_ITEMS"))
                {
                    e.Template.Parse("S_CAN_EDIT", "TRUE");
                }

                if (gallery.Access.Can("DELETE_ITEMS"))
                {
                    e.Template.Parse("S_CAN_DELETE", "TRUE");
                }

                /* pages */
                e.Core.Display.ParsePageList(e.Page.Owner, true);

                galleryItem.Viewed(e.Core.Session.LoggedInMember);
                ItemView.LogView(e.Core, galleryItem);

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

                e.Core.Meta.Add("og:site_name", e.Core.Settings.SiteTitle);
                e.Core.Meta.Add("twitter:card", "photo");
                if (!string.IsNullOrEmpty(e.Core.Settings.TwitterName))
                {
                    e.Core.Meta.Add("twitter:site", e.Core.Settings.TwitterName);
                }
                if (galleryItem.Owner is User && !string.IsNullOrEmpty(((User)galleryItem.Owner).UserInfo.TwitterUserName))
                {
                    e.Core.Meta.Add("twitter:creator", ((User)galleryItem.Owner).UserInfo.TwitterUserName);
                }
                e.Core.Meta.Add("twitter:image:src", e.Core.Hyperlink.StripSid(e.Core.Hyperlink.AppendCurrentSid(galleryItem.DisplayUri)));
                if (!string.IsNullOrEmpty(galleryItem.ItemTitle))
                {
                    e.Core.Meta.Add("twitter:title", galleryItem.ItemTitle);
                    e.Core.Meta.Add("og:title", galleryItem.ItemTitle);
                }
                else
                {
                    e.Core.Meta.Add("og:title", galleryItem.Path);
                }
                e.Core.Meta.Add("og:type", "boxsocialapp:photo");
                e.Core.Meta.Add("og:url", e.Core.Hyperlink.StripSid(e.Core.Hyperlink.AppendCurrentSid(galleryItem.Uri)));
                e.Core.Meta.Add("og:image", e.Core.Hyperlink.StripSid(e.Core.Hyperlink.AppendCurrentSid(galleryItem.DisplayUri)));

                e.Page.CanonicalUri = galleryItem.Uri;

                e.Template.Parse("PAGE_TITLE", galleryItem.ItemTitle);
                e.Template.Parse("PHOTO_TITLE", galleryItem.ItemTitle);
                e.Template.Parse("PHOTO_ID", galleryItem.ItemId.ToString());
                e.Core.Display.ParseBbcode("PHOTO_DESCRIPTION", e.Core.Bbcode.FromStatusCode(galleryItem.ItemAbstract));
                e.Template.Parse("HD_WIDTH", hdSize.Width);
                e.Template.Parse("HD_HEIGHT", hdSize.Height);
                e.Template.Parse("PHOTO_COMMENTS", e.Core.Functions.LargeIntegerToString(galleryItem.Comments));
                e.Template.Parse("U_MARK_DISPLAY_PIC", galleryItem.MakeDisplayPicUri);
                e.Template.Parse("U_MARK_GALLERY_COVER", galleryItem.SetGalleryCoverUri);
                e.Template.Parse("U_ROTATE_LEFT", galleryItem.RotateLeftUri);
                e.Template.Parse("U_ROTATE_RIGHT", galleryItem.RotateRightUri);
                e.Template.Parse("U_TAG", galleryItem.TagUri);

                e.Template.Parse("PHOTO_MOBILE", galleryItem.MobileUri);
                e.Template.Parse("PHOTO_DISPLAY", galleryItem.DisplayUri);
                e.Template.Parse("PHOTO_FULL", galleryItem.FullUri);
                e.Template.Parse("PHOTO_ULTRA", galleryItem.UltraUri);

                if (gallery.Access.Can("EDIT_ITEMS"))
                {
                    e.Template.Parse("U_EDIT", galleryItem.EditUri);
                }

                if (gallery.Access.Can("DELETE_ITEMS"))
                {
                    e.Template.Parse("U_DELETE", galleryItem.DeleteUri);
                }

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

                if (gallery.Access.IsPublic())
                {
                    e.Template.Parse("IS_PUBLIC", "TRUE");

                    if (galleryItem.Info.SharedTimes > 0)
                    {
                        e.Template.Parse("SHARES", string.Format(" {0:d}", galleryItem.Info.SharedTimes));
                    }
                }
                else
                {
                    e.Template.Parse("IS_PUBLIC", "FALSE");
                }

                if (gallery.Access.Can("COMMENT_ITEMS"))
                {
                    e.Template.Parse("CAN_COMMENT", "TRUE");
                }

                e.Core.Display.DisplayComments(e.Template, e.Page.Owner, galleryItem);

                string pageUri = string.Format("{0}gallery/{1}",
                    HttpUtility.HtmlEncode(e.Page.Owner.UriStub), e.Slug);
                e.Core.Display.ParsePagination("COMMENT_PAGINATION", pageUri, 10, galleryItem.Comments);

                List<string[]> breadCrumbParts = new List<string[]>();

                breadCrumbParts.Add(new string[] { "gallery", e.Core.Prose.GetString("GALLERY") });
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

                if (galleryItem.NextItem != null)
                {
                    e.Template.Parse("U_NEXT_PHOTO", galleryItem.NextItem.Uri);
                }

                if (galleryItem.PreviousItem != null)
                {
                    e.Template.Parse("U_PREVIOUS_PHOTO", galleryItem.PreviousItem.Uri);
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
            Stopwatch timer;
            timer = new Stopwatch();
            timer.Start();

            string photoName = e.Slug;
            string cdnDomain = e.Core.Settings.CdnStorageBucketDomain;

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
            bool mobileCoverRequest = false; // 480

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

            if (photoName.StartsWith(IconPrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(TilePrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(SquarePrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(HighPrefix, StringComparison.Ordinal))
            {
                photoName = photoName.Remove(0, 6);
                highRequest = true;
                storagePrefix = HighPrefix;
                scale = PictureScale.High;
                // There is no retina version of high, it is the retina version
            }
            else if (photoName.StartsWith(TinyPrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(ThumbnailPrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(MobilePrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(DisplayPrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(FullPrefix, StringComparison.Ordinal))
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
            else if (photoName.StartsWith(UltraPrefix, StringComparison.Ordinal))
            {
                photoName = photoName.Remove(0, 7);
                ultraRequest = true;
                storagePrefix = UltraPrefix;
                scale = PictureScale.Ultra;
            }
            else if (photoName.StartsWith(CoverPrefix, StringComparison.Ordinal))
            {
                photoName = photoName.Remove(0, 7);
                coverRequest = true;
                storagePrefix = CoverPrefix;
                scale = PictureScale.Cover;
            }
            else if (photoName.StartsWith(MobileCoverPrefix, StringComparison.Ordinal))
            {
                photoName = photoName.Remove(0, 8);
                if (!retinaModifier)
                {
                    mobileCoverRequest = true;
                    storagePrefix = MobileCoverPrefix;
                    scale = PictureScale.MobileCover;
                }
                else
                {
                    coverRequest = true;
                    storagePrefix = CoverPrefix;
                    scale = PictureScale.Cover;
                }
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

                if (Gallery.GetNameFromPath(photoName) == "_" + e.Page.Owner.Key + ".png" && e.Page.Owner is User)
                {
                    galleryItem = new GalleryItem(e.Core, e.Page.Owner, ((User)e.Page.Owner).UserInfo.DisplayPictureId);
                }
                else if (Gallery.GetNameFromPath(photoName) == "_" + e.Page.Owner.Key + ".png" && e.Page.Owner is UserGroup)
                {
                    galleryItem = new GalleryItem(e.Core, e.Page.Owner, ((UserGroup)e.Page.Owner).GroupInfo.DisplayPictureId);
                }
                else if (Gallery.GetNameFromPath(photoName) == "_" + e.Page.Owner.Key + ".png" && e.Page.Owner is ApplicationEntry)
                {
                    galleryItem = new GalleryItem(e.Core, e.Page.Owner, ((ApplicationEntry)e.Page.Owner).GalleryIcon);
                }
                else
                {
                    galleryItem = new GalleryItem(e.Core, e.Page.Owner, photoName);
                }
                Gallery gallery = null;

                if (galleryItem.ParentId > 0)
                {
                    gallery = new Gallery(e.Core, e.Page.Owner, galleryItem.ParentId);
                }
                else
                {
                    gallery = new Gallery(e.Core, e.Page.Owner);
                }

                // Do not bother with extra queries when it's a public CDN request
                if ((!e.Core.Settings.UseCdn) || originalRequest)
                {
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
                }

                e.Core.Http.SetToImageResponse(galleryItem.ContentType, galleryItem.GetCreatedDate(new UnixTime(e.Core, UnixTime.UTC_CODE)));

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
                            cdnDomain = e.Core.Settings.CdnIconBucketDomain;
                            break;
                        case PictureScale.Tile:
                            scaleExists = galleryItem.TileExists;
                            cdnDomain = e.Core.Settings.CdnTileBucketDomain;
                            break;
                        case PictureScale.Square:
                            scaleExists = galleryItem.SquareExists;
                            cdnDomain = e.Core.Settings.CdnSquareBucketDomain;
                            break;
                        case PictureScale.High:
                            scaleExists = galleryItem.HighExists;
                            cdnDomain = e.Core.Settings.CdnHighBucketDomain;
                            break;
                        case PictureScale.Tiny:
                            scaleExists = galleryItem.TinyExists;
                            cdnDomain = e.Core.Settings.CdnTinyBucketDomain;
                            break;
                        case PictureScale.Thumbnail:
                            scaleExists = galleryItem.ThumbnailExists;
                            cdnDomain = e.Core.Settings.CdnThumbBucketDomain;
                            break;
                        case PictureScale.Mobile:
                            scaleExists = galleryItem.MobileExists;
                            cdnDomain = e.Core.Settings.CdnMobileBucketDomain;
                            break;
                        case PictureScale.Display:
                            scaleExists = galleryItem.DisplayExists;
                            cdnDomain = e.Core.Settings.CdnDisplayBucketDomain;
                            break;
                        case PictureScale.Full:
                            scaleExists = galleryItem.FullExists;
                            cdnDomain = e.Core.Settings.CdnFullBucketDomain;
                            break;
                        case PictureScale.Ultra:
                            scaleExists = galleryItem.UltraExists;
                            cdnDomain = e.Core.Settings.CdnUltraBucketDomain;
                            break;
                        case PictureScale.Cover:
                            scaleExists = galleryItem.CoverExists;
                            cdnDomain = e.Core.Settings.CdnCoverBucketDomain;
                            break;
                        case PictureScale.MobileCover:
                            scaleExists = galleryItem.MobileCoverExists;
                            cdnDomain = e.Core.Settings.CdnMobileCoverBucketDomain;
                            break;
                    }

                    if (!scaleExists)
                    {
                        if (scale == PictureScale.MobileCover && e.Core.Storage.IsCloudStorage && !e.Core.Settings.UseCdn)
                        {
                            //HttpContext.Current.Response.Write("Checking for scale: " + timer.ElapsedMilliseconds.ToString() + "\n");
                        }
                        scaleExists = e.Core.Storage.FileExists(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.StoragePath);

                        if (scaleExists)
                        {
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
                                case PictureScale.Cover:
                                    galleryItem.CoverExists = true;
                                    break;
                                case PictureScale.MobileCover:
                                    galleryItem.MobileCoverExists = true;
                                    break;
                            }

                            galleryItem.Update();
                        }
                    }

                    if (!scaleExists)
                    {
                        bool flag;

                        if (scale == PictureScale.MobileCover && e.Core.Storage.IsCloudStorage && !e.Core.Settings.UseCdn)
                        {
                            //HttpContext.Current.Response.Write("Scale not found: " + timer.ElapsedMilliseconds.ToString() + "\n");
                        }
                        switch (scale)
                        {
                            case PictureScale.Icon:
                            case PictureScale.Tile:
                            case PictureScale.Square:
                            case PictureScale.High:
                                flag = CreateScaleWithSquareRatio(e.Core, galleryItem, galleryItem.StoragePath, storagePrefix, (int)scale, (int)scale);

                                if (flag)
                                {
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
                                }
                                break;
                            case PictureScale.Tiny:
                            case PictureScale.Thumbnail:
                            case PictureScale.Mobile:
                            case PictureScale.Display:
                            case PictureScale.Full:
                            case PictureScale.Ultra:
                                flag = CreateScaleWithRatioPreserved(e.Core, galleryItem, galleryItem.StoragePath, storagePrefix, (int)scale, (int)scale);

                                if (flag)
                                {
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
                                }
                                break;
                            case PictureScale.Cover:
                                CreateCoverPhoto(e.Core, galleryItem, galleryItem.StoragePath, false);
                                galleryItem.CoverExists = true;
                                break;
                            case PictureScale.MobileCover:
                                if (e.Core.Storage.IsCloudStorage && !e.Core.Settings.UseCdn)
                                {
                                    //HttpContext.Current.Response.Write("About to create cover photo: " + timer.ElapsedMilliseconds.ToString() + "\n");
                                }
                                CreateCoverPhoto(e.Core, galleryItem, galleryItem.StoragePath, true);
                                galleryItem.MobileCoverExists = true;
                                break;
                        }

                        galleryItem.Update();
                    }

                    if (e.Core.Storage.IsCloudStorage)
                    {
                        if (e.Core.Settings.UseCdn)
                        {
                            string imageUri = e.Core.Http.DefaultProtocol + cdnDomain + "/" + galleryItem.StoragePath;
                            e.Core.Http.Redirect(imageUri);
                        }
                        else
                        {
                            if (scale == PictureScale.MobileCover)
                            {
                                //HttpContext.Current.Response.Write("About to get file uri: " + timer.ElapsedMilliseconds.ToString() + "\n");
                            }
                            string imageUri = e.Core.Storage.RetrieveFileUri(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, storagePrefix), galleryItem.storagePath, galleryItem.ContentType, "picture" + GetFileExtension(galleryItem.ContentType));
                            //if (scale != PictureScale.MobileCover)
                            {
                                e.Core.Http.Redirect(imageUri);
                            }
                            /*else
                            {
                                HttpContext.Current.Response.ContentType = "text/plain";
                                HttpContext.Current.Response.Write("End: " + timer.ElapsedMilliseconds.ToString() + "\n");
                                HttpContext.Current.Response.Write("I made it here\n");
                            }*/
                        }
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
                        /*if (e.Core.Settings.UseCdn)
                        {
                            string imageUri = "http://" + e.Core.Settings.CdnStorageBucketDomain + "/" + galleryItem.StoragePath;
                            e.Core.Http.Redirect(imageUri);
                        }
                        else
                        {*/
                            string imageUri = e.Core.Storage.RetrieveFileUri(e.Core.Storage.PathCombine(e.Core.Settings.StorageBinUserFilesPrefix, "_storage"), galleryItem.storagePath, galleryItem.ContentType, "picture" + GetFileExtension(galleryItem.ContentType));
                            e.Core.Http.Redirect(imageUri);
                        //}
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

        public static bool CreateScaleWithSquareRatio(Core core, string contentType, Stream stream, string fileName, string bin, int width, int height)
        {
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                string storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", fileName);
                string tempFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, bin, fileName);

                if (!File.Exists(storageFilePath))
                {
                    Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);

                    try
                    {
                        if (fs is MemoryStream)
                        {
                            MemoryStream ms = (MemoryStream)fs;
                            FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                            ms.WriteTo(ds);
                            ds.Close();
                            fs.Close();
                        }
                        else
                        {
                            fs.Position = 0;
                            byte[] bytes = new byte[fs.Length];
                            fs.Read(bytes, 0, (int)fs.Length);
                            FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                            ds.Write(bytes, 0, bytes.Length);
                            ds.Close();
                            fs.Close();
                        }
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }

                Process p1 = new Process();
                p1.StartInfo.FileName = "convert";
                p1.StartInfo.Arguments = string.Format("\"{3}\" -strip -auto-orient -interlace Plane -quality 80 -thumbnail {1}x{2}^ -background none -gravity center -extent {1}x{2} \"{0}\"", tempFilePath, width, height, storageFilePath);
                p1.StartInfo.UseShellExecute = false;
                p1.Start();

                p1.WaitForExit();

                FileStream newStream = new FileStream(tempFilePath, FileMode.Open);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, newStream, contentType);
                stream.Close();

                File.Delete(tempFilePath);
            }
            else
            {
                Stream fs = null;
                if (stream == null)
                {
                    fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                    stream = fs;
                }

                Image image = Image.FromStream(stream);
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

                MemoryStream newStream = new MemoryStream();
                if (image.RawFormat == ImageFormat.Jpeg)
                {
                    ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(2);
                    EncoderParameter myEncoderParameter = new EncoderParameter(encoder, 90L);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    myEncoderParameters.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.RenderMethod, (int)EncoderValue.RenderProgressive);

                    displayImage.Save(newStream, codecInfo, myEncoderParameters);
                }
                else
                {
                    displayImage.Save(newStream, image.RawFormat);
                }
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, newStream, contentType);

                if (fs != null)
                {
                    fs.Close();
                }
            }

            return true;
        }


        /// <summary>
        /// Thumbnail size fits into a x * y display area. Icons are trimmed to an exact x * y pixel display size.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        public static bool CreateScaleWithSquareRatio(Core core, GalleryItem gi, string fileName, string bin, int width, int height)
        {
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);

                string storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", fileName);
                string tempFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, bin, fileName);

                try
                {
                    if (fs is MemoryStream)
                    {
                        MemoryStream ms = (MemoryStream)fs;
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ms.WriteTo(ds);
                        ds.Close();
                        fs.Close();
                    }
                    else
                    {
                        fs.Position = 0;
                        byte[] bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, (int)fs.Length);
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ds.Write(bytes, 0, bytes.Length);
                        ds.Close();
                        fs.Close();
                    }
                }
                catch (IOException)
                {
                    return false;
                }

                Process p1 = new Process();
                p1.StartInfo.FileName = "convert";
                p1.StartInfo.Arguments = string.Format("\"{3}\" -strip -auto-orient -interlace Plane -quality 80 -thumbnail {1}x{2}^ -background none -gravity center -extent {1}x{2} \"{0}\"", tempFilePath, width, height, storageFilePath);
                p1.StartInfo.UseShellExecute = false;
                p1.Start();

                p1.WaitForExit();

                FileStream stream = new FileStream(tempFilePath, FileMode.Open);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
                stream.Close();
                fs.Close();

                File.Delete(storageFilePath);
                File.Delete(tempFilePath);
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
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
                stream.Close();
                fs.Close();
            }

            return true;
        }

        public static bool CreateScaleWithRatioPreserved(Core core, string contentType, Stream stream, string fileName, string bin, int width, int height)
        {
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                string storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", fileName);
                string tempFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, bin, fileName);

                if (!File.Exists(storageFilePath))
                {
                    Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);

                    try
                    {
                        if (fs is MemoryStream)
                        {
                            MemoryStream ms = (MemoryStream)fs;
                            FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                            ms.WriteTo(ds);
                            ds.Close();
                            fs.Close();
                        }
                        else
                        {
                            fs.Position = 0;
                            byte[] bytes = new byte[fs.Length];
                            fs.Read(bytes, 0, (int)fs.Length);
                            FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                            ds.Write(bytes, 0, bytes.Length);
                            ds.Close();
                            fs.Close();
                        }
                    }
                    catch (IOException)
                    {
                        return false; // FAIL
                    }
                }

                Process p1 = new Process();
                p1.StartInfo.FileName = "convert";
                p1.StartInfo.Arguments = string.Format("\"{3}\" -strip -auto-orient -interlace Plane -quality 80 -thumbnail {1}x{2} \"{0}\"", tempFilePath, width, height, storageFilePath);
                p1.StartInfo.UseShellExecute = false;
                p1.Start();

                p1.WaitForExit();

                FileStream newStream = new FileStream(tempFilePath, FileMode.Open);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, newStream, contentType);
                stream.Close();

                File.Delete(tempFilePath);
            }
            else
            {
                Stream fs = null;
                if (stream == null)
                {
                    fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                    stream = fs;
                }

                Image image = Image.FromStream(stream);
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

                    MemoryStream newStream = new MemoryStream();
                    thumbImage.Save(newStream, image.RawFormat);
                    core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, newStream, contentType);
                    newStream.Close();
                    fs.Close();
                }
                else
                {
                    core.Storage.CopyFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName);
                }

                if (fs != null)
                {
                    fs.Close();
                }
            }

            return true;
        }

        /// <summary>
        /// Thumbnail size fits into a x * y display area. The aspect ratio is preserved.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        public static bool CreateScaleWithRatioPreserved(Core core, GalleryItem gi, string fileName, string bin, int width, int height)
        {
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);

                string storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", fileName);
                string tempFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, bin, fileName);

                try
                {
                    if (fs is MemoryStream)
                    {
                        MemoryStream ms = (MemoryStream)fs;
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ms.WriteTo(ds);
                        ds.Close();
                        fs.Close();
                    }
                    else
                    {
                        fs.Position = 0;
                        byte[] bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, (int)fs.Length);
                        FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                        ds.Write(bytes, 0, bytes.Length);
                        ds.Close();
                        fs.Close();
                    }
                }
                catch (IOException)
                {
                    return false;
                }

                Process p1 = new Process();
                p1.StartInfo.FileName = "convert";
                p1.StartInfo.Arguments = string.Format("\"{3}\" -strip -auto-orient -interlace Plane -quality 80 -thumbnail {1}x{2} \"{0}\"", tempFilePath, width, height, storageFilePath);
                p1.StartInfo.UseShellExecute = false;
                p1.Start();

                p1.WaitForExit();

                FileStream stream = new FileStream(tempFilePath, FileMode.Open);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
                stream.Close();
                fs.Close();

                File.Delete(storageFilePath);
                File.Delete(tempFilePath);
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
                    core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
                    stream.Close();
                    fs.Close();
                }
                else
                {
                    fs.Close();
                    core.Storage.CopyFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName);
                }
            }

            return true;
        }

        /// <summary>
        /// Thumbnail size fits into a x * y display area. Icons are trimmed to an exact x * y pixel display size.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="fileName"></param>
        public static void CreateCoverPhoto(Core core, GalleryItem gi, string fileName, bool isMobile)
        {
            string bin = "_cover";
            int width = 960;
            int height = 200;

            if (isMobile)
            {
                bin = "_mcover";
                width = 480;
                height = 100;
            }

            int crop = gi.CropPositionVertical;
            // Imagemagick is only supported under mono which doesn't have very good implementation of GDI for image resizing
            if (Core.IsUnix && WebConfigurationManager.AppSettings["image-method"] == "imagemagick")
            {
                Stream fs = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), fileName);
                string storageFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, "_storage", fileName);
                string tempFilePath = System.IO.Path.Combine(core.Settings.ImagemagickTempPath, bin, fileName);

                if (fs is MemoryStream)
                {
                    MemoryStream ms = (MemoryStream)fs;
                    FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                    ms.WriteTo(ds);
                    ds.Close();
                    fs.Close();
                }
                else
                {
                    fs.Position = 0;
                    byte[] bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, (int)fs.Length);
                    FileStream ds = new FileStream(storageFilePath, FileMode.Create);
                    ds.Write(bytes, 0, bytes.Length);
                    ds.Close();
                    fs.Close();
                }

                Size imageSize = new Size(gi.ItemWidth, gi.ItemHeight);
                double scale = (double)width / imageSize.Width;
                int newHeight = (int)(imageSize.Height * scale);
                int cropY = (int)(crop * scale);

                Process p1 = new Process();
                p1.StartInfo.FileName = "convert";
                p1.StartInfo.Arguments = string.Format("\"{3}\" -strip -auto-orient -interlace Plane -quality 80 -resize {1}x{1} -extent {1}x{2}+0+{4} \"{0}\"", tempFilePath, width, height, storageFilePath, cropY);
                p1.StartInfo.UseShellExecute = false;
                p1.Start();

                p1.WaitForExit();

                FileStream stream = new FileStream(tempFilePath, FileMode.Open);
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
                stream.Close();
                fs.Close();

                File.Delete(storageFilePath);
                File.Delete(tempFilePath);
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
                core.Storage.SaveFileWithReducedRedundancy(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, bin), fileName, stream, gi.ContentType);
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
        [JsonIgnore]
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
        [JsonProperty("uri")]
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
        [JsonIgnore]
        public string EditUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "edit-photo", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string MakeDisplayPicUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "display-pic", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string MakeProfileCoverPhotoUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "profile-cover", itemId, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string SetGalleryCoverUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "gallery-cover", itemId, true);
            }
        }

        [JsonIgnore]
        public string RotateLeftUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "rotate-photo", true, new string[] { "id=" + itemId.ToString(), "rotation=left" });
            }
        }

        [JsonIgnore]
        public string RotateRightUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "rotate-photo", true, new string[] { "id=" + itemId.ToString(), "rotation=right" });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string DeleteUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "delete", itemId, true);
            }
        }

        [JsonIgnore]
        public string TagUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri(Owner, "galleries", "tag", itemId, true);
            }
        }

        /// <summary>
        /// Returns the gallery item icon uri
        /// </summary>
        [JsonProperty("icon_uri")]
        public string IconUri
        {
            get
            {
                if (core.Settings.UseCdn && IconExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnIconBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_icon/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_icon/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("tile_uri")]
        public string TileUri
        {
            get
            {
                if (core.Settings.UseCdn && TileExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnTileBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_tile/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_tile/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("square_uri")]
        public string SquareUri
        {
            get
            {
                if (core.Settings.UseCdn && SquareExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnSquareBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_square/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_square/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("high_uri")]
        public string HighUri
        {
            get
            {
                if (core.Settings.UseCdn && HighExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnHighBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_high/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_high/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("tiny_uri")]
        public string TinyUri
        {
            get
            {
                string size = "_tiny";
                if (ItemWidth <= 80 && ItemHeight <= 80)
                {
                    return UltraUri;
                }

                if (core.Settings.UseCdn && TinyExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnTinyBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("thumbnail_uri")]
        public string ThumbnailUri
        {
            get
            {
                string size = "_thumb";
                if (ItemWidth <= 160 && ItemHeight <= 160)
                {
                    return UltraUri;
                }

                if (core.Settings.UseCdn && ThumbnailExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnThumbBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("mobile_uri")]
        public string MobileUri
        {
            get
            {
                string size = "_mobile";
                if (ItemWidth <= 320 && ItemHeight <= 320)
                {
                    return UltraUri;
                }

                if (core.Settings.UseCdn && MobileExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnMobileBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("display_uri")]
        public string DisplayUri
        {
            get
            {
                string size = "_display";
                /*if (ItemWidth <= 640 && ItemHeight <= 640)
                {
                    return UltraUri;
                }*/

                if (core.Settings.UseCdn && DisplayExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnDisplayBucketDomain, StoragePath);
                }
                

                if (parentId > 0)
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, FullPath, size));
                    }
                    else
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                            Owner.UriStub, FullPath, size));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, path, size));
                    }
                    else
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                            Owner.UriStub, path, size));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("full_uri")]
        public string FullUri
        {
            get
            {
                if (ItemWidth <= 1280 && ItemHeight <= 1280)
                {
                    return UltraUri;
                }

                if (core.Settings.UseCdn && FullExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnFullBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/_full/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, FullPath));
                    }
                    else
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/_full/{1}",
                            Owner.UriStub, FullPath));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(core.Http["reload"]))
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/_full/{1}?reload=" + UnixTime.UnixTimeStamp(),
                            Owner.UriStub, path));
                    }
                    else
                    {
                        return core.Hyperlink.AppendSid(string.Format("{0}images/_full/{1}",
                            Owner.UriStub, path));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonProperty("ultra_uri")]
        public string UltraUri
        {
            get
            {
                string size = "_ultra";
                /*if (ItemWidth <= 2560 && ItemHeight <= 2560)
                {
                    return OriginalUri;
                }*/

                if (core.Settings.UseCdn && UltraExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnUltraBucketDomain, StoragePath);
                }

                if (core.Settings.UseCdn &&  ItemWidth <= 2560 && ItemHeight <= 2560)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnStorageBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, FullPath, size));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{2}/{1}",
                        Owner.UriStub, path, size));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item thumbnail uri
        /// </summary>
        [JsonIgnore]
        public string OriginalUri
        {
            get
            {
                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns the gallery item cover uri
        /// </summary>
        [JsonProperty("cover_uri")]
        public string CoverUri
        {
            get
            {
                if (core.Settings.UseCdn && CoverExists)
                {
                    return string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnCoverBucketDomain, StoragePath);
                }

                if (parentId > 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_cover/{1}",
                        Owner.UriStub, FullPath));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}images/_cover/{1}",
                        Owner.UriStub, path));
                }
            }
        }

        /// <summary>
        /// Returns gallery item comment count
        /// </summary>
        [JsonIgnore]
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        #region ICommentableItem Members

        /// <summary>
        /// Returns the gallery item comment sort order
        /// </summary>
        [JsonIgnore]
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
                return "photo";
            }
        }

        #endregion

        [JsonIgnore]
        public long Likes
        {
            get
            {
                return Info.Likes;
            }
        }

        [JsonIgnore]
        public long Dislikes
        {
            get
            {
                return Info.Dislikes;
            }
        }

        [JsonIgnore]
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Parent;
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

        [JsonIgnore]
        public string ShareString
        {
            get
            {
                return string.Format("[iurl=\"{0}#hd\"][inline cdn-object=\"{2}\" width=\"{3}\" height=\"{4}\"]{1}[/inline][/iurl]",
                            Uri, FullPath, StoragePath, ItemWidth, ItemHeight);
            }
        }

        [JsonProperty("share_uri")]
        public string ShareUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/share?item={0}&type={1}", ItemKey.Id, ItemKey.TypeId), true);
            }
        }

        [JsonIgnore]
        public string Action
        {
            get
            {
                return "uploaded a photo";
            }
        }

        public string GetActionBody(List<ItemKey> subItems)
        {
            string returnValue = string.Empty;

            if (!string.IsNullOrEmpty(ItemAbstract))
            {
                returnValue += ItemAbstract + "\r\n\r\n";
            }

            returnValue += string.Format("[iurl=\"{0}#hd\"][inline cdn-object=\"{2}\" width=\"{3}\" height=\"{4}\"]{1}[/inline][/iurl]",
                        Uri, FullPath, StoragePath, ItemWidth, ItemHeight);

            return returnValue;
        }

        [JsonIgnore]
        public string IndexingString
        {
            get
            {
                return ItemAbstract;
            }
        }

        [JsonIgnore]
        public string IndexingTitle
        {
            get
            {
                return ItemTitle;
            }
        }

        [JsonIgnore]
        public string IndexingTags
        {
            get
            {
                return string.Empty;
            }
        }

        public Template RenderPreview()
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "search_result_galleryitem");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            VariableCollection statusMessageVariableCollection = template.CreateChild("status_messages");

            //statusMessageVariableCollection.Parse("STATUS_MESSAGE", item.Message);
            core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(GetActionBody(new List<ItemKey>())), owner, true, string.Empty, string.Empty);
            statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(GetCreatedDate(core.Tz)));

            statusMessageVariableCollection.Parse("ID", Id.ToString());
            statusMessageVariableCollection.Parse("TYPE_ID", ItemKey.TypeId.ToString());
            statusMessageVariableCollection.Parse("USERNAME", Owner.DisplayName);
            statusMessageVariableCollection.Parse("USER_ID", Owner.Id);
            statusMessageVariableCollection.Parse("U_PROFILE", Owner.Uri); // TODO: ProfileUri for primitive
            statusMessageVariableCollection.Parse("U_PERMISSIONS", Parent.Access.AclUri);
            statusMessageVariableCollection.Parse("USER_TILE", Owner.Tile);
            statusMessageVariableCollection.Parse("USER_ICON", Owner.Icon);
            statusMessageVariableCollection.Parse("URI", Uri);

            if (core.Session.IsLoggedIn)
            {
                if (Owner is User && Owner.Id == core.Session.LoggedInMember.Id)
                {
                    statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");
                }
            }

            if (Info.Likes > 0)
            {
                statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", Info.Likes));
                statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", Info.Dislikes));
            }

            if (Info.Comments > 0)
            {
                statusMessageVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", Info.Comments));
            }

            return template;
        }

        [JsonIgnore]
        public ActionableItemType PostType
        {
            get
            {
                return ActionableItemType.Photo;
            }
        }

        [JsonIgnore]
        public byte[] Data
        {
            get
            {
                if (this.ItemBytes < 3145728)
                {
                    Stream image = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), StoragePath);

                    byte[] data = new byte[image.Length];
                    image.Read(data, 0, data.Length);

                    image.Close();

                    return data;
                }
                else if (this.ItemWidth > (int)PictureScale.Full || this.ItemHeight > (int)PictureScale.Full)
                {
                    if (!FullExists)
                    {
                        Stream stream = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), StoragePath);

                        if (CreateScaleWithRatioPreserved(core, contentType, stream, StoragePath, FullPrefix, (int)PictureScale.Full, (int)PictureScale.Full))
                        {
                            FullExists = true;
                            Update();
                        }
                    }

                    Stream image = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, FullPrefix), StoragePath);

                    byte[] data = new byte[image.Length];
                    image.Read(data, 0, data.Length);

                    image.Close();

                    return data;

                }
                else if (this.ItemWidth > (int)PictureScale.Display || this.ItemHeight > (int)PictureScale.Display)
                {
                    if (!DisplayExists)
                    {
                        Stream stream = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), StoragePath);

                        if (CreateScaleWithRatioPreserved(core, contentType, stream, StoragePath, DisplayPrefix, (int)PictureScale.Display, (int)PictureScale.Display))
                        {
                            DisplayExists = true;
                            Update();
                        }
                    }

                    Stream image = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, DisplayPrefix), StoragePath);

                    byte[] data = new byte[image.Length];
                    image.Read(data, 0, data.Length);

                    image.Close();

                    return data;
                }
                else if (this.ItemWidth > (int)PictureScale.Mobile || this.ItemHeight > (int)PictureScale.Mobile)
                {
                    if (!MobileExists)
                    {
                        Stream stream = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), StoragePath);

                        if (CreateScaleWithRatioPreserved(core, contentType, stream, StoragePath, MobilePrefix, (int)PictureScale.Mobile, (int)PictureScale.Mobile))
                        {
                            MobileExists = true;
                            Update();
                        }
                    }

                    Stream image = core.Storage.RetrieveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, MobilePrefix), StoragePath);

                    byte[] data = new byte[image.Length];
                    image.Read(data, 0, data.Length);

                    image.Close();

                    return data;
                }

                return null;
            }
        }

        [JsonIgnore]
        public string DataContentType
        {
            get
            {
                return ContentType;
            }
        }

        [JsonIgnore]
        public string Caption
        {
            get
            {
                return ItemAbstract;
            }
        }

        public Embed GetEmbedObject(int maxWidth, int maxHeight)
        {
            Embed embed = new Embed(EmbedType.Photo);

            Size newSize = new Size();
            if (maxWidth <= (int)PictureScale.Tiny && maxHeight <= (int)PictureScale.Tiny)
            {
                newSize = GetSize(new Size(ItemWidth, ItemHeight), new Size((int)PictureScale.Tiny, (int)PictureScale.Tiny));

                // We do not want an embed consumer caching sizes that are queued for creation in the CDN
                bool flag = tinyExists;
                tinyExists = true;

                embed.Url = core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(TinyUri));

                tinyExists = flag;
            }
            else if (maxWidth <= (int)PictureScale.Thumbnail && maxHeight <= (int)PictureScale.Thumbnail)
            {
                newSize = GetSize(new Size(ItemWidth, ItemHeight), new Size((int)PictureScale.Thumbnail, (int)PictureScale.Thumbnail));

                // We do not want an embed consumer caching sizes that are queued for creation in the CDN
                bool flag = thumbnailExists;
                thumbnailExists = true;

                embed.Url = core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(ThumbnailUri));

                thumbnailExists = flag;
            }
            else if (maxWidth <= (int)PictureScale.Mobile && maxHeight <= (int)PictureScale.Mobile)
            {
                newSize = GetSize(new Size(ItemWidth, ItemHeight), new Size((int)PictureScale.Mobile, (int)PictureScale.Mobile));

                // We do not want an embed consumer caching sizes that are queued for creation in the CDN
                bool flag = mobileExists;
                mobileExists = true;

                embed.Url = core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(MobileUri));

                mobileExists = flag;
            }
            else
            {
                newSize = GetSize(new Size(ItemWidth, ItemHeight), new Size((int)PictureScale.Display, (int)PictureScale.Display));

                // We do not want an embed consumer caching sizes that are queued for creation in the CDN
                bool flag = displayExists;
                displayExists = true;

                embed.Url = core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(DisplayUri));

                displayExists = flag;
            }

            embed.Width = newSize.Width.ToString();
            embed.Height = newSize.Height.ToString();
            embed.Title = Functions.SingleLine(core.Bbcode.Flatten(ItemAbstract));
            embed.AuthorName = Owner.DisplayName;
            embed.AuthorUrl = core.Hyperlink.StripSid(Owner.UriStubAbsolute);


            return embed;
        }

        [JsonIgnore]
        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }

        private GalleryItem previousItem = null;
        private GalleryItem nextItem = null;
        private bool previousLoaded = false;
        private bool nextLoaded = false;

        [JsonIgnore]
        public GalleryItem PreviousItem
        {
            get
            {
                if (!previousLoaded)
                {
                    previousLoaded = true;

                    SelectQuery prevQuery = GetSelectQueryStub(core, typeof(GalleryItem));
                    prevQuery.AddCondition("gallery_id", Parent.Id);
                    prevQuery.AddCondition("gallery_item_item_id", Parent.Owner.Id);
                    prevQuery.AddCondition("gallery_item_item_type_id", Parent.Owner.TypeId);
                    prevQuery.AddCondition("gallery_item_id", ConditionEquality.LessThan, Id);
                    prevQuery.AddSort(SortOrder.Descending, "gallery_item_id");
                    prevQuery.LimitCount = 1;

                    System.Data.Common.DbDataReader prevDataReader = core.Db.ReaderQuery(prevQuery);

                    if (prevDataReader.HasRows)
                    {
                        prevDataReader.Read();

                        previousItem = new GalleryItem(core, prevDataReader);
                    }

                    prevDataReader.Close();
                    prevDataReader.Dispose();
                }
                return previousItem;
            }
        }

        [JsonIgnore]
        public GalleryItem NextItem
        {
            get
            {
                if (!nextLoaded)
                {
                    nextLoaded = true;
                    SelectQuery nextQuery = GetSelectQueryStub(core, typeof(GalleryItem));
                    nextQuery.AddCondition("gallery_id", Parent.Id);
                    nextQuery.AddCondition("gallery_item_item_id", Parent.Owner.Id);
                    nextQuery.AddCondition("gallery_item_item_type_id", Parent.Owner.TypeId);
                    nextQuery.AddCondition("gallery_item_id", ConditionEquality.GreaterThan, Id);
                    nextQuery.AddSort(SortOrder.Ascending, "gallery_item_id");
                    nextQuery.LimitCount = 1;

                    System.Data.Common.DbDataReader nextDataReader = core.Db.ReaderQuery(nextQuery);

                    if (nextDataReader.HasRows)
                    {
                        nextDataReader.Read();

                        nextItem = new GalleryItem(core, nextDataReader);
                    }

                    nextDataReader.Close();
                    nextDataReader.Dispose();
                }
                return nextItem;
            }
        }

        [JsonProperty("previous_item_key")]
        public ItemKey PreviousItemKey
        {
            get
            {
                if (PreviousItem != null)
                {
                    return PreviousItem.ItemKey;
                }
                return null;
            }
        }

        [JsonProperty("next_item_key")]
        public ItemKey NextItemKey
        {
            get
            {
                if (NextItem != null)
                {
                    return NextItem.ItemKey;
                }
                return null;
            }
        }

        public bool CanComment
        {
            get
            {
                return Parent.Access.Can("COMMENT");
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
