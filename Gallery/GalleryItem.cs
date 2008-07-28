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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a gallery photo
    /// </summary>
    public abstract class GalleryItem : NumberedItem, ICommentableItem
    {
        /// <summary>
        /// A list of database fields associated with a gallery photo.
        /// </summary>
        public const string GALLERY_ITEM_INFO_FIELDS = "gi.gallery_item_id, gi.gallery_item_title, gi.gallery_item_parent_path, gi.gallery_item_uri, gi.gallery_item_comments, gi.gallery_item_views, gi.gallery_item_rating, gi.user_id, gi.gallery_id, gi.gallery_item_item_id, gi.gallery_item_item_type, gi.gallery_item_access, gi.gallery_item_storage_path, gi.gallery_item_content_type, gi.gallery_item_abstract, gi.gallery_item_classification";

        /// <summary>
        /// Owner of the photo
        /// </summary>
        protected Primitive owner;

        /// <summary>
        /// Owner of the photo's user Id
        /// </summary>
        protected long userId;

        /// <summary>
        /// Gallery photo Id
        /// </summary>
        protected long itemId;

        /// <summary>
        /// Gallery photo title
        /// </summary>
        protected string itemTitle;

        /// <summary>
        /// Gallery photo parent path
        /// </summary>
        protected string parentPath;

        /// <summary>
        /// Gallery photo path (slug)
        /// </summary>
        protected string path;

        /// <summary>
        /// Gallery photo (parent) gallery Id
        /// </summary>
        protected long parentId;

        /// <summary>
        /// Gallery photo comments
        /// </summary>
        protected long itemComments;

        /// <summary>
        /// Gallery photo views
        /// </summary>
        protected long itemViews;

        /// <summary>
        /// Gallery photo rating
        /// </summary>
        protected float itemRating;

        /// <summary>
        /// Gallery photo permission mask
        /// </summary>
        protected ushort permissions;

        /// <summary>
        /// Gallery photo access information
        /// </summary>
        protected Access itemAccess;

        /// <summary>
        /// Gallery photo content (MIME) type
        /// </summary>
        protected string contentType;

        /// <summary>
        /// Gallery photo storage file name
        /// </summary>
        /// <remarks>
        /// The storage file name for a photo is the hash of the photo file
        /// without a file extension.
        /// </remarks>
        protected string storagePath;

        /// <summary>
        /// Gallery photo abstract (description)
        /// </summary>
        protected string itemAbstract;

        /// <summary>
        /// Gallery photo license
        /// </summary>
        private ContentLicense license;

        /// <summary>
        /// Gallery photo classification
        /// </summary>
        private Classifications classification;

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
        /// Gets the rating
        /// </summary>
        public float ItemRating
        {
            get
            {
                return itemRating;
            }
        }

        /// <summary>
        /// Gets the permission mask for the gallery photo
        /// </summary>
        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        /// <summary>
        /// Gets the access information (permissions) for the gallery photo
        /// </summary>
        public Access ItemAccess
        {
            get
            {
                return itemAccess;
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
                return license;
            }
        }

        /// <summary>
        /// Returns the gallery item classification
        /// </summary>
        public Classifications Classification
        {
            get
            {
                return classification;
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="path">Gallery item path</param>
        protected GalleryItem(Core core, Primitive owner, string path) : base(core)
        {
            this.owner = owner;

            DataTable galleryItemTable = db.Query(string.Format("SELECT {1}, {5} FROM gallery_items gi LEFT JOIN licenses li ON li.license_id = gi.gallery_item_license WHERE gi.gallery_item_parent_path = '{2}' AND gi.gallery_item_uri = '{3}' AND gi.gallery_item_item_id = {0} AND gi.gallery_item_item_type = '{4}';",
                owner.Id, GalleryItem.GALLERY_ITEM_INFO_FIELDS, Mysql.Escape(Gallery.GetParentPath(path)), Mysql.Escape(Gallery.GetNameFromPath(path)), Mysql.Escape(owner.Type), ContentLicense.LICENSE_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(galleryItemTable.Rows[0]);
                try
                {
                    loadLicenseInfo(galleryItemTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
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
        public GalleryItem(Core core, User owner, DataRow itemRow) : base(core)
        {
            this.db = db;
            this.owner = owner;

            loadItemInfo(itemRow);
            try
            {
                loadLicenseInfo(itemRow);
            }
            catch (NonexistantLicenseException)
            {
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        protected GalleryItem(Core core, Primitive owner, DataRow itemRow) : base(core)
        {
            this.owner = owner;

            loadItemInfo(itemRow);
            try
            {
                loadLicenseInfo(itemRow);
            }
            catch (NonexistantLicenseException)
            {
            }
        }

        /// <summary>
        /// Initialises a new instance of the GalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public GalleryItem(Core core, DataRow itemRow) : base(core)
        {
            // TODO: owner not set, no big worry

            loadItemInfo(itemRow);
            try
            {
                loadLicenseInfo(itemRow);
            }
            catch (NonexistantLicenseException)
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
        public GalleryItem(Core core, User owner, Gallery parent, string path) : base(core)
        {
            this.db = db;
            this.owner = owner;

            DataTable galleryItemTable = db.Query(string.Format("SELECT {1}, {4} FROM gallery_items gi LEFT JOIN licenses li ON li.license_id = gi.gallery_item_license WHERE gi.gallery_item_parent_path = '{2}' AND gi.gallery_item_uri = {3} AND gi.user_id = {0}",
                owner.UserId, GalleryItem.GALLERY_ITEM_INFO_FIELDS, Mysql.Escape(parent.FullPath), Mysql.Escape(path), ContentLicense.LICENSE_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(galleryItemTable.Rows[0]);
                try
                {
                    loadLicenseInfo(galleryItemTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
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
        protected GalleryItem(Core core, Primitive owner, long itemId) : base(core)
        {
            this.owner = owner;

            DataTable galleryItemTable = db.Query(string.Format("SELECT {1}, {4} FROM gallery_items gi LEFT JOIN licenses li ON li.license_id = gi.gallery_item_license WHERE gi.gallery_item_id = {2} AND gi.gallery_item_item_id = {0} AND gi.gallery_item_item_type = '{3}'",
                owner.Id, GalleryItem.GALLERY_ITEM_INFO_FIELDS, itemId, Mysql.Escape(owner.Type), ContentLicense.LICENSE_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(galleryItemTable.Rows[0]);
                try
                {
                    loadLicenseInfo(galleryItemTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
                {
                }
            }
            else
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
        public GalleryItem(Core core, long itemId) : base(core)
        {
            // TODO: owner not set, no big worry

            DataTable galleryItemTable = db.Query(string.Format("SELECT {0}, {2} FROM gallery_items gi LEFT JOIN licenses li ON li.license_id = gi.gallery_item_license WHERE gi.gallery_item_id = {1};",
                GalleryItem.GALLERY_ITEM_INFO_FIELDS, itemId, ContentLicense.LICENSE_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                loadItemInfo(galleryItemTable.Rows[0]);
                try
                {
                    loadLicenseInfo(galleryItemTable.Rows[0]);
                }
                catch (NonexistantLicenseException)
                {
                }
            }
            else
            {
                throw new GalleryItemNotFoundException();
            }
        }

        /// <summary>
        /// Loads the database information into the GalleryItem class object.
        /// </summary>
        /// <param name="itemRow"></param>
        protected void loadItemInfo(DataRow itemRow)
        {
            itemId = (long)itemRow["gallery_item_id"];
            itemTitle = (string)itemRow["gallery_item_title"];
            parentPath = (string)itemRow["gallery_item_parent_path"];
            path = (string)itemRow["gallery_item_uri"];
            itemComments = (long)itemRow["gallery_item_comments"];
            itemViews = (long)itemRow["gallery_item_views"];
            itemRating = (float)itemRow["gallery_item_rating"];
            permissions = (ushort)itemRow["gallery_item_access"];
            itemAccess = new Access(core, permissions, owner);
            contentType = (string)itemRow["gallery_item_content_type"];
            storagePath = (string)itemRow["gallery_item_storage_path"];
            if (!(itemRow["gallery_item_abstract"] is System.DBNull))
            {
                itemAbstract = (string)itemRow["gallery_item_abstract"];
            }
            parentId = (long)itemRow["gallery_id"];
            classification = (Classifications)(byte)itemRow["gallery_item_classification"];
        }

        /// <summary>
        /// Loads the licensing information into the GalleryItem class object.
        /// </summary>
        /// <param name="itemRow"></param>
        private void loadLicenseInfo(DataRow itemRow)
        {
            license = new ContentLicense(core, itemRow);
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

                FileInfo fi = new FileInfo(TPage.GetStorageFilePath(storagePath));

                if (owner is User)
                {
                    UserGallery parent = new UserGallery(core, (User)owner, parentId);
                    UserGallery.UpdateGalleryInfo(db, (Primitive)owner, parent, (long)itemId, -1, -fi.Length);
                }

                UpdateQuery uQuery = new UpdateQuery("user_info");
                uQuery.AddField("user_gallery_items", new QueryOperation("user_gallery_items", QueryOperations.Subtraction, 1));
                uQuery.AddField("user_bytes", new QueryOperation("user_bytes", QueryOperations.Subtraction, fi.Length));
                uQuery.AddCondition("user_id", userId);

                db.Query(uQuery);

                if ((long)results.Rows[0]["number"] > 1)
                {
                    // do not delete the storage file, still in use
                }
                else
                {
                    // delete the storage file
                    File.Delete(TPage.GetStorageFilePath(storagePath));
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
        protected static long create(Core core, Primitive owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            Mysql db = core.db;

            if (owner is User)
            {
                if (owner.Id != core.LoggedInMemberId)
                {
                    throw new Exception("Error, user IDs don't match");
                }
            }

            if (bytes > (ulong)2 * 1024 * 1024)
            {
                throw new GalleryItemTooLargeException();
            }

            if (core.session.LoggedInMember.BytesUsed + bytes > (ulong)150 * 1024 * 1024)
            {
                throw new GalleryQuotaExceededException();
            }

            switch (contentType)
            {
                case "image/png":
                case "image/jpeg":
                case "image/pjpeg":
                    //case "image/gif": // not accepting gif at the moment
                    break;
                default:
                    throw new InvalidGalleryItemTypeException();
            }

            slug = GalleryItem.GetSlugFromFileName(fileName, slug);

            GalleryItem.EnsureGallerySlugUnique(core, parent, owner, ref slug);

            InsertQuery iQuery = new InsertQuery("gallery_items");
            iQuery.AddField("gallery_item_uri", slug);
            iQuery.AddField("gallery_item_title", title);
            iQuery.AddField("gallery_item_abstract", description);
            iQuery.AddField("gallery_item_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("gallery_item_storage_path", storageName);
            iQuery.AddField("gallery_item_parent_path", parent.FullPath);
            iQuery.AddField("gallery_item_access", permissions);
            iQuery.AddField("gallery_item_content_type", contentType);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("gallery_item_bytes", bytes);
            iQuery.AddField("gallery_item_license", license);
            iQuery.AddField("gallery_id", parent.GalleryId);
            iQuery.AddField("gallery_item_item_id", owner.Id);
            iQuery.AddField("gallery_item_item_type", owner.Type);
            iQuery.AddField("gallery_item_classification", (byte)classification);

            // we want to use transactions
            long itemId = db.Query(iQuery);

            if (itemId >= 0)
            {
                //owner.UpdateGalleryInfo(parent, itemId, 1, (long)bytes);
                if (owner is User)
                {
                    UserGallery.UpdateGalleryInfo(db, owner, parent, itemId, 1, (long)bytes);
                }

                UpdateQuery uQuery = new UpdateQuery("user_info");
                uQuery.AddField("user_gallery_items", new QueryOperation("user_gallery_items", QueryOperations.Addition, 1));
                uQuery.AddField("user_bytes", new QueryOperation("user_bytes", QueryOperations.Addition, bytes));
                uQuery.AddCondition("user_id", core.LoggedInMemberId);

                if (db.Query(uQuery) < 0)
                {
                    throw new Exception("Transaction failed, panic!");
                }

                //return new GalleryItem(db, owner, itemId);
                return itemId;
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
        public void Update(string title, string description, ushort permissions, byte license, Classifications classification)
        {
            long rowsChanged = db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_title = '{2}', gallery_item_abstract = '{3}', gallery_item_access = {4}, gallery_item_license = {5}, gallery_item_classification = {8} WHERE user_id = {0} AND gallery_item_id = {1} AND gallery_item_item_id = {6} AND gallery_item_item_type = '{7}';",
                core.LoggedInMemberId, itemId, Mysql.Escape(title), Mysql.Escape(description), permissions, license, owner.Id, owner.Type, (byte)classification));

            if (rowsChanged == 0)
            {
                throw new GalleryItemNotFoundException();
            }
        }

        /// <summary>
        /// Rotate the gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="rotation">Rotation</param>
        public void Rotate(Core core, RotateFlipType rotation)
        {
            ImageFormat iF = ImageFormat.Jpeg;

            Image image = Image.FromFile(TPage.GetStorageFilePath(StoragePath, StorageFileType.Original));
            iF = image.RawFormat;

            image.RotateFlip(rotation);

            MemoryStream ms = new MemoryStream();

            image.Save(ms, iF);

            string newFileName = GalleryItem.HashFileUpload(ms);

            if (!File.Exists(TPage.GetStorageFilePath(newFileName, StorageFileType.Original)))
            {
                TPage.EnsureStoragePathExists(newFileName, StorageFileType.Original);

                FileStream fs = new FileStream(TPage.GetStorageFilePath(newFileName, StorageFileType.Original), FileMode.CreateNew);
                ms.WriteTo(fs);

                fs.Close();
                ms.Close();
            }

            UpdateQuery uquery = new UpdateQuery("gallery_items");
            uquery.AddField("gallery_item_storage_path", newFileName);
            uquery.AddCondition("gallery_item_id", itemId);

            db.Query(uquery);

        }

        /// <summary>
        /// Hashses the contents of a file
        /// </summary>
        /// <param name="fileStream">File stream to hash</param>
        /// <returns>File hash string</returns>
        public static string HashFileUpload(Stream fileStream)
        {
            HashAlgorithm hash = new SHA512Managed();

            byte[] fileBytes = new byte[fileStream.Length];
            fileStream.Read(fileBytes, 0, (int)fileStream.Length);

            byte[] fileHash = hash.ComputeHash(fileBytes);

            string fileHashString = "";
            foreach (byte fileHashByte in fileHash)
            {
                fileHashString += string.Format("{0:x2}", fileHashByte);
            }

            return fileHashString;
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
                    normalisedSlug += saveFileUri[i];
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
                DataTable galleryItemTable = core.db.Query(string.Format("SELECT gallery_item_uri FROM gallery_items WHERE gallery_item_uri = '{0}' AND gallery_id = {1} AND gallery_item_item_id = {2} AND gallery_item_item_type = '{3}';",
                    Mysql.Escape(slug), gallery.GalleryId, owner.Id, owner.Type));

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
        public abstract string BuildUri();

        /// <summary>
        /// Shows a user gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoPath">Photo parent path</param>
        /// <param name="photoName">Photo slug</param>
        public static void Show(Core core, PPage page, string photoPath, string photoName)
        {
            page.template.SetTemplate("Gallery", "viewphoto");

            char[] trimStartChars = { '.', '/' };
            if (photoPath != null)
            {
                photoPath = photoPath.TrimEnd('/').TrimStart(trimStartChars);
            }
            else
            {
                photoPath = "";
            }

            page.ProfileOwner.LoadProfileInfo();

            try
            {
                UserGalleryItem photo = new UserGalleryItem(core, page.ProfileOwner, photoPath + "/" + photoName);

                photo.ItemAccess.SetViewer(core.session.LoggedInMember);

                if (!photo.ItemAccess.CanRead)
                {
                    Functions.Generate403();
                    return;
                }

                photo.Viewed(core.session.LoggedInMember);

                string displayUri = string.Format("/{0}/images/_display/{1}/{2}",
                    page.ProfileOwner.UserName, photoPath, photo.Path);
                page.template.Parse("PHOTO_DISPLAY", displayUri);
                page.template.Parse("PHOTO_TITLE", photo.ItemTitle);
                page.template.Parse("PHOTO_ID", photo.ItemId.ToString());
                page.template.Parse("U_UPLOAD_PHOTO", Linker.BuildPhotoUploadUri(photo.ParentId));

                if (!string.IsNullOrEmpty(photo.ItemAbstract))
                {
                    //page.template.ParseRaw("PHOTO_DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(photo.ItemAbstract), core.session.LoggedInMember));
                    Display.ParseBbcode("PHOTO_DESCRIPTION", photo.ItemAbstract);
                }
                else
                {
                    page.template.Parse("PHOTO_DESCRIPTION", "FALSE");
                }

                page.template.Parse("PHOTO_COMMENTS", Functions.LargeIntegerToString(photo.ItemComments));

                Display.RatingBlock(photo.ItemRating, page.template, photo.ItemId, "PHOTO");

                page.template.Parse("ID", photo.ItemId.ToString());
                page.template.Parse("U_MARK_DISPLAY_PIC", Linker.BuildMarkDisplayPictureUri(photo.ItemId));
                page.template.Parse("U_MARK_GALLERY_COVER", Linker.BuildMarkGalleryCoverUri(photo.ItemId));
                page.template.Parse("U_EDIT", Linker.BuildPhotoEditUri(photo.ItemId));
                page.template.Parse("U_ROTATE_LEFT", Linker.BuildPhotoRotateLeftUri(photo.ItemId));
                page.template.Parse("U_ROTATE_RIGHT", Linker.BuildPhotoRotateRightUri(photo.ItemId));
                page.template.Parse("U_DELETE", photo.BuildDeleteUri());

                switch (photo.Classification)
                {
                    case Classifications.Everyone:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                        break;
                    case Classifications.Mature:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                        break;
                    case Classifications.Restricted:
                        page.template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                        break;
                }

                if (photo.License != null)
                {
                    if (!string.IsNullOrEmpty(photo.License.Title))
                    {
                        page.template.Parse("PAGE_LICENSE", photo.License.Title);
                    }
                    if (!string.IsNullOrEmpty(photo.License.Icon))
                    {
                        page.template.Parse("I_PAGE_LICENSE", photo.License.Icon);
                    }
                    if (!string.IsNullOrEmpty(photo.License.Link))
                    {
                        page.template.Parse("U_PAGE_LICENSE", photo.License.Link);
                    }
                }

                List<UserTag> tags = UserTag.GetTags(core, photo);

                if (tags.Count > 0)
                {
                    page.template.Parse("HAS_USER_TAGS", "TRUE");
                }

                int i = 0;
                foreach (UserTag tag in tags)
                {
                    VariableCollection tagsVariableCollection = page.template.CreateChild("user_tags");

                    tagsVariableCollection.Parse("INDEX", i.ToString());
                    tagsVariableCollection.Parse("TAG_ID", tag.TagId.ToString());
                    tagsVariableCollection.Parse("TAG_X", (tag.TagLocation.X / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("TAG_Y", (tag.TagLocation.Y / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("DISPLAY_NAME", tag.TaggedMember.DisplayName);
                    tagsVariableCollection.Parse("U_MEMBER", tag.TaggedMember.Uri);

                    i++;
                }

                int p = 1;

                try
                {
                    p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
                }
                catch
                {
                }

                if (photo.ItemAccess.CanComment)
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
                Display.DisplayComments(page.template, page.ProfileOwner, photo);

                string pageUri = string.Format("/{0}/gallery/{1}/{2}",
                    HttpUtility.HtmlEncode(page.ProfileOwner.UserName), photoPath, photoName);
                //page.template.Parse("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(photo.ItemComments / 10.0)));
                //page.template.Parse("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, "gallery/" + photo.ParentPath + "/" + photo.Path));
                Display.ParsePagination(pageUri, p, (int)Math.Ceiling(photo.ItemComments / 10.0));
                page.ProfileOwner.ParseBreadCrumbs("gallery/" + photo.ParentPath + "/" + photo.Path);

            }
            catch (GalleryItemNotFoundException)
            {
                Functions.Generate404();
                return;
            }
        }

        /// <summary>
        /// Shows a group gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoName">Photo slug</param>
        public static void Show(Core core, GPage page, string photoName)
        {
            page.template.SetTemplate("Gallery", "viewphoto");

            char[] trimStartChars = { '.', '/' };

            try
            {
                GroupGalleryItem galleryItem = new GroupGalleryItem(core, page.ThisGroup, photoName);

                switch (page.ThisGroup.GroupType)
                {
                    case "OPEN":
                        // can view the gallery and all it's photos
                        break;
                    case "CLOSED":
                    case "PRIVATE":
                        if (!page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                        {
                            Functions.Generate403();
                            return;
                        }
                        break;
                }

                galleryItem.Viewed(core.session.LoggedInMember);

                string displayUri = string.Format("/group/{0}/images/_display/{1}",
                        page.ThisGroup.Slug, galleryItem.Path);
                page.template.Parse("PHOTO_DISPLAY", displayUri);
                page.template.Parse("PHOTO_TITLE", galleryItem.ItemTitle);
                page.template.Parse("PHOTO_ID", galleryItem.ItemId.ToString());
                //page.template.ParseRaw("PHOTO_DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(galleryItem.ItemAbstract), core.session.LoggedInMember));
                Display.ParseBbcode("PHOTO_DESCRIPTION", galleryItem.ItemAbstract);
                page.template.Parse("PHOTO_COMMENTS", Functions.LargeIntegerToString(galleryItem.ItemComments));
                page.template.Parse("U_UPLOAD_PHOTO", Linker.BuildPhotoUploadUri(galleryItem.ParentId));

                switch (galleryItem.Classification)
                {
                    case Classifications.Everyone:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                        break;
                    case Classifications.Mature:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                        break;
                    case Classifications.Restricted:
                        page.template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                        break;
                }

                if (galleryItem.License != null)
                {
                    if (!string.IsNullOrEmpty(galleryItem.License.Title))
                    {
                        page.template.Parse("PAGE_LICENSE", galleryItem.License.Title);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Icon))
                    {
                        page.template.Parse("I_PAGE_LICENSE", galleryItem.License.Icon);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Link))
                    {
                        page.template.Parse("U_PAGE_LICENSE", galleryItem.License.Link);
                    }
                }

                Display.RatingBlock(galleryItem.ItemRating, page.template, galleryItem.ItemId, "PHOTO");

                page.template.Parse("ID", galleryItem.ItemId.ToString());
                //template.Parse("U_EDIT", ZzUri.BuildPhotoEditUri((long)photoTable.Rows[0]["gallery_item_id"])));

                int p = 1;

                try
                {
                    p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
                }
                catch
                {
                }

                if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
                Display.DisplayComments(page.template, page.ThisGroup, galleryItem);

                string pageUri = string.Format("/group/{0}/gallery/{1}",
                    HttpUtility.HtmlEncode(page.ThisGroup.Slug), photoName);
                //page.template.Parse("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(galleryItem.ItemComments / 10.0)));
                //page.template.Parse("BREADCRUMBS", page.ThisGroup.GenerateBreadCrumbs("gallery/" + galleryItem.Path));
                Display.ParsePagination(pageUri, p, (int)Math.Ceiling(galleryItem.ItemComments / 10.0));
                page.ThisGroup.ParseBreadCrumbs("gallery/" + galleryItem.Path);

            }
            catch (GalleryItemNotFoundException)
            {
                Functions.Generate404();
                return;
            }
        }

        /// <summary>
        /// Shows a network gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoName">Photo slug</param>
        public static void Show(Core core, NPage page, string photoName)
        {
            page.template.SetTemplate("Gallery", "viewphoto");

            char[] trimStartChars = { '.', '/' };

            try
            {
                NetworkGalleryItem galleryItem = new NetworkGalleryItem(core, page.TheNetwork, photoName);

                switch (page.TheNetwork.NetworkType)
                {
                    case NetworkTypes.Country:
                    case NetworkTypes.Global:
                        // can view the network and all it's photos
                        break;
                    case NetworkTypes.University:
                    case NetworkTypes.School:
                    case NetworkTypes.Workplace:
                        if (!page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                        {
                            Functions.Generate403();
                            return;
                        }
                        break;
                }

                galleryItem.Viewed(core.session.LoggedInMember);

                string displayUri = string.Format("/network/{0}/images/_display/{1}",
                        page.TheNetwork.NetworkNetwork, galleryItem.Path);
                page.template.Parse("PHOTO_DISPLAY", displayUri);
                page.template.Parse("PHOTO_TITLE", galleryItem.ItemTitle);
                page.template.Parse("PHOTO_ID", galleryItem.ItemId.ToString());
                //page.template.ParseRaw("PHOTO_DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(galleryItem.ItemAbstract), core.session.LoggedInMember));
                Display.ParseBbcode("PHOTO_DESCRIPTION", galleryItem.ItemAbstract);
                page.template.Parse("PHOTO_COMMENTS", Functions.LargeIntegerToString(galleryItem.ItemComments));
                page.template.Parse("U_UPLOAD_PHOTO", Linker.BuildPhotoUploadUri(galleryItem.ParentId));

                switch (galleryItem.Classification)
                {
                    case Classifications.Everyone:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                        break;
                    case Classifications.Mature:
                        page.template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                        break;
                    case Classifications.Restricted:
                        page.template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                        page.template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                        break;
                }

                if (galleryItem.License != null)
                {
                    if (!string.IsNullOrEmpty(galleryItem.License.Title))
                    {
                        page.template.Parse("PAGE_LICENSE", galleryItem.License.Title);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Icon))
                    {
                        page.template.Parse("I_PAGE_LICENSE", galleryItem.License.Icon);
                    }
                    if (!string.IsNullOrEmpty(galleryItem.License.Link))
                    {
                        page.template.Parse("U_PAGE_LICENSE", galleryItem.License.Link);
                    }
                }

                Display.RatingBlock(galleryItem.ItemRating, page.template, galleryItem.ItemId, "PHOTO");

                page.template.Parse("ID", galleryItem.ItemId.ToString());
                //template.Parse("U_EDIT", ZzUri.BuildPhotoEditUri((long)photoTable.Rows[0]["gallery_item_id"])));

                int p = 1;

                try
                {
                    p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
                }
                catch
                {
                }

                if (page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
                Display.DisplayComments(page.template, page.TheNetwork, galleryItem);

                string pageUri = string.Format("/network/{0}/gallery/{1}",
                    HttpUtility.HtmlEncode(page.TheNetwork.NetworkNetwork), photoName);
                //page.template.Parse("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(galleryItem.ItemComments / 10.0)));
                //page.template.Parse("BREADCRUMBS", page.TheNetwork.GenerateBreadCrumbs("gallery/" + galleryItem.Path));
                Display.ParsePagination(pageUri, p, (int)Math.Ceiling(galleryItem.ItemComments / 10.0));
                page.TheNetwork.ParseBreadCrumbs("gallery/" + galleryItem.Path);

            }
            catch (GalleryItemNotFoundException)
            {
                Functions.Generate404();
                return;
            }
        }

        /// <summary>
        /// Shows a user image
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoName">Photo slug</param>
        public static void ShowImage(Core core, PPage page, string photoName)
        {
            ShowImage(core, page.ProfileOwner, photoName);
        }

        /// <summary>
        /// Shows a group image
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoName">Photo slug</param>
        public static void ShowImage(Core core, GPage page, string photoName)
        {
            ShowImage(core, page.ThisGroup, photoName);
        }

        /// <summary>
        /// Shows a network image
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page token</param>
        /// <param name="photoName">Photo slug</param>
        public static void ShowImage(Core core, NPage page, string photoName)
        {
            ShowImage(core, page.TheNetwork, photoName);
        }

        /// <summary>
        /// Shows an image
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Image owner</param>
        /// <param name="photoName">Photo slug</param>
        private static void ShowImage(Core core, Primitive owner, string photoName)
        {
            bool thumbnailRequest = false; // large 160px thumbnail
            bool displayRequest = false;
            bool iconRequest = false;
            bool tileRequest = false;
            bool tinyRequest = false; // small 80px thumbnail

            if (photoName.StartsWith("_thumb"))
            {
                photoName = photoName.Remove(0, 7);
                thumbnailRequest = true;
            }
            else if (photoName.StartsWith("_display"))
            {
                photoName = photoName.Remove(0, 9);
                displayRequest = true;
            }
            else if (photoName.StartsWith("_icon"))
            {
                photoName = photoName.Remove(0, 6);
                iconRequest = true;
            }
            else if (photoName.StartsWith("_tile"))
            {
                photoName = photoName.Remove(0, 6);
                tileRequest = true;
            }
            else if (photoName.StartsWith("_tiny"))
            {
                photoName = photoName.Remove(0, 6);
                tinyRequest = true;
            }

            string[] paths = photoName.Split('/');

            try
            {
                GalleryItem galleryItem;// = new GalleryItem(db, owner, imagePath);

                if (owner is User)
                {
                    galleryItem = new UserGalleryItem(core, (User)owner, photoName);
                    galleryItem.ItemAccess.SetViewer(core.session.LoggedInMember);

                    if (!galleryItem.ItemAccess.CanRead)
                    {
                        Functions.Generate403();
                        return;
                    }
                }
                else if (owner is UserGroup)
                {
                    galleryItem = new GroupGalleryItem(core, (UserGroup)owner, photoName);
                    switch (((UserGroup)owner).GroupType)
                    {
                        case "OPEN":
                            // can view the gallery and all it's photos
                            break;
                        case "CLOSED":
                        case "PRIVATE":
                            if (!((UserGroup)owner).IsGroupMember(core.session.LoggedInMember))
                            {
                                Functions.Generate403();
                                return;
                            }
                            break;
                    }
                }
                else if (owner is Network)
                {
                    galleryItem = new NetworkGalleryItem(core, (Network)owner, photoName);
                    switch (((Network)owner).NetworkType)
                    {
                        case NetworkTypes.Country:
                        case NetworkTypes.Global:
                            // can view the network and all it's photos
                            break;
                        case NetworkTypes.University:
                        case NetworkTypes.School:
                        case NetworkTypes.Workplace:
                            if (!((Network)owner).IsNetworkMember(core.session.LoggedInMember))
                            {
                                Functions.Generate403();
                                return;
                            }
                            break;
                    }
                }
                else
                {
                    Functions.Generate404();
                    return;
                }

                /* we assume exists */

                FileInfo fi = new FileInfo(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Original));

                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ContentType = galleryItem.ContentType;
                HttpContext.Current.Response.Cache.SetLastModified(fi.LastWriteTimeUtc);
                HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);

                if (thumbnailRequest)
                {
                    if (!File.Exists(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Thumbnail)))
                    {
                        CreateThumbnail(TPage.GetStorageFilePath(galleryItem.StoragePath));
                    }

                    if (HttpContext.Current.Response.ContentType == "image/png")
                    {
                        MemoryStream newStream = new MemoryStream();

                        Image hoi = Image.FromFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Thumbnail));

                        hoi.Save(newStream, hoi.RawFormat);

                        newStream.WriteTo(HttpContext.Current.Response.OutputStream);
                    }
                    else
                    {
                        HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Thumbnail));
                    }
                }
                else if (displayRequest)
                {
                    if (!File.Exists(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Display)))
                    {
                        CreateDisplay(TPage.GetStorageFilePath(galleryItem.StoragePath));
                    }

                    if (HttpContext.Current.Response.ContentType == "image/png")
                    {
                        MemoryStream newStream = new MemoryStream();

                        Image hoi = Image.FromFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Display));

                        hoi.Save(newStream, hoi.RawFormat);

                        newStream.WriteTo(HttpContext.Current.Response.OutputStream);
                    }
                    else
                    {
                        HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Display));
                    }
                }
                else if (iconRequest)
                {
                    if (!File.Exists(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Icon)))
                    {
                        CreateIcon(TPage.GetStorageFilePath(galleryItem.StoragePath));
                    }

                    if (HttpContext.Current.Response.ContentType == "image/png")
                    {
                        MemoryStream newStream = new MemoryStream();

                        Image hoi = Image.FromFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Icon));

                        hoi.Save(newStream, hoi.RawFormat);

                        newStream.WriteTo(HttpContext.Current.Response.OutputStream);
                    }
                    else
                    {
                        HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Icon));
                    }
                }
                else if (tileRequest)
                {
                    if (!File.Exists(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tile)))
                    {
                        CreateTile(TPage.GetStorageFilePath(galleryItem.StoragePath));
                    }

                    if (HttpContext.Current.Response.ContentType == "image/png")
                    {
                        MemoryStream newStream = new MemoryStream();

                        Image hoi = Image.FromFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tile));

                        hoi.Save(newStream, hoi.RawFormat);

                        newStream.WriteTo(HttpContext.Current.Response.OutputStream);
                    }
                    else
                    {
                        HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tile));
                    }
                }
                else if (tinyRequest)
                {
                    if (!File.Exists(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tiny)))
                    {
                        CreateTiny(TPage.GetStorageFilePath(galleryItem.StoragePath));
                    }

                    if (HttpContext.Current.Response.ContentType == "image/png")
                    {
                        MemoryStream newStream = new MemoryStream();

                        Image hoi = Image.FromFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tiny));

                        hoi.Save(newStream, hoi.RawFormat);

                        newStream.WriteTo(HttpContext.Current.Response.OutputStream);
                    }
                    else
                    {
                        HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath, StorageFileType.Tiny));
                    }
                }
                else
                {
                    HttpContext.Current.Response.TransmitFile(TPage.GetStorageFilePath(galleryItem.StoragePath));
                }
            }
            catch (GalleryItemNotFoundException)
            {
                Functions.Generate404();
                return;
            }

            if (core.db != null)
            {
                core.db.CloseConnection();
            }
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// Tile size fits into a 50 x 50 display area. Icons are trimmed to an exact 50 x 50 pixel display size.
        /// </summary>
        /// <param name="fileName"></param>
        public static void CreateTile(string fileName)
        {
            Image image = Image.FromFile(fileName);
            Bitmap displayImage;
            int width = image.Width;
            int height = image.Height;
            double ratio = (double)width / height;

            if (width < height)
            {
                width = 50;
                height = (int)(50 / ratio);
            }
            else
            {
                height = 50;
                width = (int)(50 * ratio);
            }

            displayImage = new Bitmap(50, 50, image.PixelFormat);
            displayImage.Palette = image.Palette;

            Graphics g = Graphics.FromImage(displayImage);
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.AssumeLinear;

            int square = Math.Min(image.Width, image.Height);
            g.DrawImage(image, new Rectangle(0, 0, 50, 50), new Rectangle((image.Width - square) / 2, (image.Height - square) / 2, square, square), GraphicsUnit.Pixel);

            FileInfo imageFile = new FileInfo(fileName);
            TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Tile);
            displayImage.Save(TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Tile), image.RawFormat);
        }

        /// <summary>
        /// Icon size fits into a 100 x 100 display area. Icons are trimmed to an exact 100 x 100 pixel display size.
        /// </summary>
        /// <param name="fileName"></param>
        public static void CreateIcon(string fileName)
        {
            Image image = Image.FromFile(fileName);
            Bitmap displayImage;
            int width = image.Width;
            int height = image.Height;
            double ratio = (double)width / height;

            if (width < height)
            {
                width = 100;
                height = (int)(100 / ratio);
            }
            else
            {
                height = 100;
                width = (int)(100 * ratio);
            }

            displayImage = new Bitmap(100, 100, image.PixelFormat);
            displayImage.Palette = image.Palette;

            Graphics g = Graphics.FromImage(displayImage);
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.AssumeLinear;

            int square = Math.Min(image.Width, image.Height);
            g.DrawImage(image, new Rectangle(0, 0, 100, 100), new Rectangle((image.Width - square) / 2, (image.Height - square) / 2, square, square), GraphicsUnit.Pixel);

            FileInfo imageFile = new FileInfo(fileName);
            TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Icon);
            displayImage.Save(TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Icon), image.RawFormat);
        }

        /// <summary>
        /// Thumbnail size fits into a 160 x 160 display area. The aspect ratio is preserved.
        /// </summary>
        /// <param name="fileName"></param>
        private static void CreateThumbnail(string fileName)
        {
            Image image = Image.FromFile(fileName);
            Image thumbImage;
            int width = image.Width;
            int height = image.Height;
            double ratio = (double)width / height;

            if (width > 160 || height > 160)
            {
                if (width >= height)
                {
                    width = 160;
                    height = (int)(160 / ratio);
                }
                else
                {
                    height = 160;
                    width = (int)(160 * ratio);
                }

                Image.GetThumbnailImageAbort abortCallBack = new Image.GetThumbnailImageAbort(abortResize);
                thumbImage = image.GetThumbnailImage(width, height, abortCallBack, IntPtr.Zero);
                thumbImage.Palette = image.Palette;

                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Thumbnail);
                thumbImage.Save(TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Thumbnail), image.RawFormat);
            }
            else
            {
                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Thumbnail);
                File.Copy(fileName, TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Thumbnail));
            }
        }

        /// <summary>
        /// Thumbnail size fits into a 80 x 80 display area. The aspect ratio is preserved.
        /// </summary>
        /// <param name="fileName"></param>
        private static void CreateTiny(string fileName)
        {
            Image image = Image.FromFile(fileName);
            Image tinyImage;
            int width = image.Width;
            int height = image.Height;
            double ratio = (double)width / height;

            if (width > 80 || height > 80)
            {
                if (width >= height)
                {
                    width = 80;
                    height = (int)(80 / ratio);
                }
                else
                {
                    height = 80;
                    width = (int)(80 * ratio);
                }

                Image.GetThumbnailImageAbort abortCallBack = new Image.GetThumbnailImageAbort(abortResize);
                tinyImage = image.GetThumbnailImage(width, height, abortCallBack, IntPtr.Zero);
                tinyImage.Palette = image.Palette;

                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Tiny);
                tinyImage.Save(TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Tiny), image.RawFormat);
            }
            else
            {
                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Tiny);
                File.Copy(fileName, TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Tiny));
            }
        }

        /// <summary>
        /// Display size fits into a 640 x 640 display area. The aspect ratio is preserved.
        /// </summary>
        /// <param name="fileName"></param>
        public static void CreateDisplay(string fileName)
        {
            Image image = Image.FromFile(fileName);
            Bitmap displayImage;
            int width = image.Width;
            int height = image.Height;
            double ratio = (double)width / height;

            if (width > 640 || height > 640)
            {
                if (width >= height)
                {
                    width = 640;
                    height = (int)(640 / ratio);
                }
                else
                {
                    height = 640;
                    width = (int)(640 * ratio);
                }

                displayImage = new Bitmap(width, height, image.PixelFormat);
                displayImage.Palette = image.Palette;
                Graphics g = Graphics.FromImage(displayImage);
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.AssumeLinear;

                g.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Display);
                displayImage.Save(TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Display), image.RawFormat);
            }
            else
            {
                FileInfo imageFile = new FileInfo(fileName);
                TPage.EnsureStoragePathExists(imageFile.Name, StorageFileType.Display);
                File.Copy(fileName, TPage.GetStorageFilePath(imageFile.Name, StorageFileType.Display));
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
        /// Returns gallery item namespace
        /// </summary>
        public override string Namespace
        {
            get
            {
                return "PHOTO";
            }
        }

        /// <summary>
        /// Returns gallery item Uri
        /// </summary>
        public abstract override string Uri
        {
            get;
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
