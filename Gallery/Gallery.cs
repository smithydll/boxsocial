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
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a gallery
    /// </summary>
    public abstract class Gallery
    {
        /// <summary>
        /// A list of database fields associated with a user gallery.
        /// </summary>
        public const string GALLERY_INFO_FIELDS = "ug.gallery_id, ug.gallery_parent_id, ug.gallery_access, ug.gallery_title, ug.gallery_parent_path, ug.gallery_path, ug.gallery_items, ug.gallery_abstract, ug.gallery_visits, ug.gallery_description, ug.gallery_bytes";

        /// <summary>
        /// A list of database fields associated with a user gallery icon.
        /// </summary>
        public const string GALLERY_ICON_FIELDS = "gi.gallery_item_uri";

        /// <summary>
        /// Database object
        /// </summary>
        protected Mysql db;

        /// <summary>
        /// Owner of the gallery
        /// </summary>
        protected Primitive owner;

        /// <summary>
        /// Id of the gallery
        /// </summary>
        protected long galleryId;
        
        /// <summary>
        /// Id of the parent gallery
        /// </summary>
        protected long parentId;

        /// <summary>
        /// Access object for the gallery
        /// </summary>
        protected Access galleryAccess;

        /// <summary>
        /// Gallery title
        /// </summary>
        protected string galleryTitle;

        /// <summary>
        /// Gallery parent path
        /// </summary>
        protected string parentPath;

        /// <summary>
        /// Gallery path (slug)
        /// </summary>
        protected string path;

        /// <summary>
        /// Number of visits made to the gallery
        /// </summary>
        protected long visits;

        /// <summary>
        /// Number of photos in the gallery
        /// </summary>
        protected long items;

        /// <summary>
        /// Number of bytes the the photos in the gallery consume
        /// </summary>
        protected long bytes;

        /// <summary>
        /// Gallery abstract
        /// </summary>
        protected string galleryAbstract;

        /// <summary>
        /// Gallery Description
        /// </summary>
        protected string galleryDescription;

        /// <summary>
        /// URI of the highlighted photo
        /// </summary>
        protected string highlightUri;

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
        }

        /// <summary>
        /// Gets the access object for the gallery
        /// </summary>
        public Access GalleryAccess
        {
            get
            {
                return galleryAccess;
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
                    if (owner is Member)
                    {
                        return string.Format("/{0}/images/_thumb/{1}/{2}",
                            ((Member)owner).UserName, FullPath, highlightUri);
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
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        public Gallery(Mysql db, Member owner)
        {
            this.db = db;
            this.owner = owner;

            galleryId = 0;
            path = "";
            parentPath = "";
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        protected Gallery(Mysql db, Primitive owner)
        {
            this.db = db;
            this.owner = owner;

            galleryId = 0;
            path = "";
            parentPath = "";
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryId">Gallery Id</param>
        protected Gallery(Mysql db, Primitive owner, long galleryId)
        {
            this.db = db;
            this.owner = owner;

            if (galleryId > 0)
            {
                DataTable galleryTable = db.SelectQuery(string.Format("SELECT {1} FROM user_galleries ug WHERE ug.gallery_id = {2} AND ug.user_id = {0}",
                        owner.Id, Gallery.GALLERY_INFO_FIELDS, galleryId));

                if (galleryTable.Rows.Count == 1)
                {
                    loadGalleryInfo(galleryTable.Rows[0]);
                }
                else
                {
                    throw new GalleryNotFoundException();
                }
            }
            else
            {
                this.galleryId = 0;
                this.path = "";
                this.parentPath = "";
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="path">Gallery path</param>
        protected Gallery(Mysql db, Primitive owner, string path)
        {
            this.db = db;
            this.owner = owner;

            DataTable galleryTable = db.SelectQuery(string.Format("SELECT {1} FROM user_galleries ug WHERE ug.gallery_parent_path = '{3}' AND ug.gallery_path = '{2}' AND ug.user_id = {0}",
                    owner.Id, Gallery.GALLERY_INFO_FIELDS, Mysql.Escape(Gallery.GetNameFromPath(path)), Mysql.Escape(Gallery.GetParentPath(path))));

            if (galleryTable.Rows.Count == 1)
            {
                loadGalleryInfo(galleryTable.Rows[0]);
            }
            else
            {
                throw new GalleryNotFoundException();
            }
        }

        /// <summary>
        /// Initialises a new instance of the Gallery class.
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryRow">Raw data row of gallery</param>
        /// <param name="hasIcon">True if contains raw data for icon</param>
        protected Gallery(Mysql db, Primitive owner, DataRow galleryRow, bool hasIcon)
        {
            this.db = db;
            this.owner = owner;

            loadGalleryInfo(galleryRow);

            if (hasIcon)
            {
                loadGalleryIcon(galleryRow);
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
            if (owner is Member)
            {
                galleryAccess = new Access(db, (ushort)galleryRow["gallery_access"], owner);
            }
            items = (long)(int)galleryRow["gallery_items"];
            bytes = (long)galleryRow["gallery_bytes"];
            visits = (long)galleryRow["gallery_visits"];
            path = (string)galleryRow["gallery_path"];
            parentPath = (string)galleryRow["gallery_parent_path"];
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

        public abstract List<Gallery> GetGalleries(TPage page);

        protected DataRowCollection GetGalleryDataRows(TPage page)
        {
            long loggedIdUid = Member.GetMemberId(page.loggedInMember);
            ushort readAccessLevel = owner.GetAccessLevel(page.loggedInMember);

            DataTable galleriesTable = db.SelectQuery(string.Format("SELECT {1}, {2} FROM user_galleries ug LEFT JOIN gallery_items gi ON ug.gallery_highlight_id = gi.gallery_item_id WHERE (ug.gallery_access & {4:0} OR ug.user_id = {5}) AND ug.user_id = {0} AND ug.gallery_parent_path = '{3}';",
                ((Member)owner).UserId, Gallery.GALLERY_INFO_FIELDS, Gallery.GALLERY_ICON_FIELDS, Mysql.Escape(FullPath), readAccessLevel, loggedIdUid));

            return galleriesTable.Rows;
        }

        public abstract List<GalleryItem> GetItems(Core core);
        public abstract List<GalleryItem> GetItems(Core core, int currentPage, int perPage);

        protected DataRowCollection GetItemDataRows(Core core)
        {
            return GetItemDataRows(core, 1, 16);
        }

        protected DataRowCollection GetItemDataRows(Core core, int currentPage, int perPage)
        {
            db = core.db;

            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);
            long loggedIdUid = Member.GetMemberId(core.session.LoggedInMember);

            DataTable photoTable = db.SelectQuery(string.Format(
                @"SELECT {2}, {8}
                    FROM gallery_items gi
                    LEFT JOIN licenses li ON li.license_id = gi.gallery_item_license
                    WHERE (gi.gallery_item_access & {3:0} OR gi.user_id = {1}) AND gi.gallery_id = {0} AND gi.gallery_item_item_id = {6} AND gi.gallery_item_item_type = '{7}'
                    LIMIT {4}, {5};",
                galleryId, loggedIdUid, GalleryItem.GALLERY_ITEM_INFO_FIELDS, readAccessLevel, (currentPage - 1) * perPage, perPage, owner.Id, Mysql.Escape(owner.Type), ContentLicense.LICENSE_FIELDS));

            return photoTable.Rows;
        }

        public void Update(TPage page, string title, string slug, string description, ushort permissions)
        {
            if (GalleryId == 0)
            {
                throw new GalleryCannotUpdateRootGalleryException();
            }

            if (owner is Member)
            {
                if (page.loggedInMember.UserId != ((Member)owner).UserId)
                {
                    throw new GalleryPermissionException();
                }
            }
            else
            {
                throw new GalleryNotAMemberObjectException();
            }

            Member member = (Member)this.owner;

            // do we have to generate a new slug
            if (slug != path) // || parent.ParentPath != ParentPath) // we can't move galleries between parents at the moment
            {
                // ensure we have generated a valid slug
                if (!Gallery.CheckGallerySlugValid(slug))
                {
                    throw new GallerySlugNotValidException();
                }

                if (!Gallery.CheckGallerySlugUnique(page.db, member, parentPath, slug))
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
                member.UserId, galleryId, Mysql.Escape(title), Mysql.Escape(description), Mysql.Escape(slug), permissions), false);
        }

        private void updateParentPathChildren(string oldPath, string newPath)
        {
            if (owner is Member)
            {
                DataTable galleriesTable = db.SelectQuery(string.Format("SELECT gallery_id, gallery_path, gallery_parent_path FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{1}'",
                    ((Member)owner).UserId, oldPath));

                for (int i = 0; i < galleriesTable.Rows.Count; i++)
                {
                    string oldPath2 = oldPath + "/" + (string)galleriesTable.Rows[i]["gallery_path"];
                    string newPath2 = newPath + "/" + (string)galleriesTable.Rows[i]["gallery_path"];
                    updateParentPathChildren(oldPath2, newPath2);

                    db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_parent_path = '{2}' WHERE user_id = {0} AND gallery_id = {1};",
                        ((Member)owner).UserId, (long)galleriesTable.Rows[i]["gallery_id"], Mysql.Escape(newPath)), true);
                }
            }
            else
            {
                throw new GalleryCannotUpdateChildrenException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parent"></param>
        /// <param name="title"></param>
        /// <param name="slug"></param>
        /// <param name="description"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        protected static long create(TPage page, Gallery parent, string title, ref string slug, string description, ushort permissions)
        {
            // ensure we have generated a valid slug
            slug = Gallery.GetSlugFromTitle(title, slug);

            if (!Gallery.CheckGallerySlugValid(slug))
            {
                throw new GallerySlugNotValidException();
            }

            if (!Gallery.CheckGallerySlugUnique(page.db, (Member)parent.owner, parent.FullPath, slug))
            {
                throw new GallerySlugNotUniqueException();
            }

            long galleryId = page.db.UpdateQuery(string.Format("INSERT INTO user_galleries (gallery_title, gallery_abstract, gallery_path, gallery_parent_path, gallery_access, user_id, gallery_parent_id) VALUES ('{0}', '{1}', '{2}', '{3}', {4}, {5}, {6})",
                Mysql.Escape(title), Mysql.Escape(description), Mysql.Escape(slug), Mysql.Escape(parent.FullPath), permissions, page.loggedInMember.UserId, parent.GalleryId));

            return galleryId;
        }

        public static void Delete(TPage page, Gallery gallery)
        {
            long[] stuffDeleted = galleryDeleteChildren(page, gallery);
            long itemsDeleted = stuffDeleted[0];
            long bytesDeleted = stuffDeleted[1];

            // comitt the transaction
            page.db.UpdateQuery(string.Format("UPDATE user_info SET user_gallery_items = user_gallery_items - {1}, user_bytes = user_bytes - {2} WHERE user_id = {1}",
                page.loggedInMember.UserId, itemsDeleted, bytesDeleted), false);
        }

        private static long[] galleryDeleteChildren(TPage page, Gallery gallery)
        {
            long itemsDeleted = 0; // index 0
            long bytesDeleted = 0; // index 1

            List<Gallery> galleries = gallery.GetGalleries(page);

            foreach (Gallery galleryGallery in galleries)
            {
                long[] stuffDeleted = galleryDeleteChildren(page, galleryGallery);
                itemsDeleted += stuffDeleted[0];
                bytesDeleted += stuffDeleted[1];
            }

            object objectsDeleted = page.db.SelectQuery(string.Format("SELECT SUM(gallery_item_bytes) AS bytes_deleted FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}';",
                    page.loggedInMember.UserId, Mysql.Escape(gallery.FullPath))).Rows[0]["bytes_deleted"];

            if (!(objectsDeleted is DBNull))
            {
                bytesDeleted += (long)(decimal)objectsDeleted;
            }

            itemsDeleted += page.db.UpdateQuery(string.Format("DELETE FROM gallery_items WHERE user_id = {0} AND gallery_item_parent_path = '{1}'",
                page.loggedInMember.UserId, Mysql.Escape(gallery.FullPath)), true);

            page.db.UpdateQuery(string.Format("DELETE FROM user_galleries WHERE user_id = {0} AND gallery_id = {1}",
                page.loggedInMember.UserId, gallery.GalleryId), true);
            return new long[] { itemsDeleted, bytesDeleted };
        }

        public static string GetParentPath(string path)
        {
            char[] trimStartChars = { '.', '/' };
            path = path.TrimEnd('/').TrimStart(trimStartChars);

            string[] paths = path.Split('/');

            return path.Remove(path.Length - paths[paths.Length - 1].Length).TrimEnd('/');
        }

        public static string GetNameFromPath(string path)
        {
            char[] trimStartChars = { '.', '/' };
            path = path.TrimEnd('/').TrimStart(trimStartChars);

            string[] paths = path.Split('/');

            return paths[paths.Length - 1];
        }

        public static bool CheckGallerySlugUnique(Mysql db, Member owner, string parentFullPath, string slug)
        {
            DataTable galleryGalleryTable = db.SelectQuery(string.Format("SELECT gallery_path FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{1}' AND gallery_path = '{2}';",
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

        public static void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            throw new Exception("Use on inherited types only");
        }

        public static string BuildPhotoUri(Member member, string galleryPath, string photoPath)
        {
            return Linker.AppendSid(string.Format("/{0}/gallery/{1}/{2}",
                member.UserName, galleryPath, photoPath));
        }

        public static string BuildPhotoUri(UserGroup thisGroup, string photoPath)
        {
            return Linker.AppendSid(string.Format("/group/{0}/gallery/{1}",
                thisGroup.Slug, photoPath));
        }

        public static string BuildPhotoUri(Network theNetwork, string photoPath)
        {
            return Linker.AppendSid(string.Format("/network/{0}/gallery/{1}",
                theNetwork.NetworkNetwork, photoPath));
        }

        public static string BuildGalleryUpload(UserGroup thisGroup)
        {
            return Linker.AppendSid(string.Format("/group/gallery/{0}/?mode=upload",
                thisGroup.Slug));
        }

        public static string BuildGalleryUpload(Network theNetwork)
        {
            return Linker.AppendSid(string.Format("/network/gallery/{0}/?mode=upload",
                theNetwork.NetworkNetwork));
        }

        public static string BuildGalleryUri(Member member)
        {
            return Linker.AppendSid(string.Format("/{0}/gallery",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryUri(Member member, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BuildGalleryUri(member);
            }
            else
            {
                return Linker.AppendSid(string.Format("/{0}/gallery/{1}",
                    member.UserName.ToLower(), path));
            }
        }

        public static string BuildGalleryUri(UserGroup thisGroup)
        {
            return Linker.AppendSid(string.Format("/group/{0}/gallery",
                thisGroup.Slug));
        }

        public static string BuildGalleryUri(Network theNetwork)
        {
            return Linker.AppendSid(string.Format("/network/{0}/gallery",
                theNetwork.NetworkNetwork));
        }

        public static void Show(Core core, PPage page, string galleryPath)
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

            page.ProfileOwner.LoadProfileInfo();

            long loggedIdUid = 0;
            if (core.session.LoggedInMember != null)
            {
                loggedIdUid = core.session.LoggedInMember.UserId;
            }

            UserGallery gallery;
            if (galleryPath != "")
            {
                try
                {
                    gallery = new UserGallery(core.db, page.ProfileOwner, galleryPath);

                    gallery.GalleryAccess.SetViewer(core.session.LoggedInMember);

                    if (!gallery.GalleryAccess.CanRead)
                    {
                        Functions.Generate403();
                        return;
                    }

                    /*List<string[]> breadCrumbParts = new List<string[]>();
                    breadCrumbParts.Add(new string[] { "gallery", "Gallery" });

                    page.template.ParseVariables("BREADCRUMBS", page.ProfileOwner.GenerateBreadCrumbs(breadCrumbParts));*/

                    page.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, "gallery/" + gallery.FullPath));
                    page.template.ParseVariables("U_UPLOAD_PHOTO", HttpUtility.HtmlEncode(Linker.BuildPhotoUploadUri(gallery.GalleryId)));
                    page.template.ParseVariables("U_NEW_GALLERY", HttpUtility.HtmlEncode(Linker.BuildNewGalleryUri(gallery.GalleryId)));

                    page.template.ParseVariables("PAGINATION", Display.GeneratePagination(Gallery.BuildGalleryUri(page.ProfileOwner, galleryPath), p, (int)Math.Ceiling(gallery.Items / 12.0)));
                }
                catch (GalleryNotFoundException)
                {
                    Functions.Generate404();
                    return;
                }
            }
            else
            {
                gallery = new UserGallery(core.db, page.ProfileOwner);
                page.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, "gallery"));
                page.template.ParseVariables("U_NEW_GALLERY", HttpUtility.HtmlEncode(Linker.BuildNewGalleryUri(0)));
            }

            List<Gallery> galleries = gallery.GetGalleries(page);

            page.template.ParseVariables("GALLERIES", HttpUtility.HtmlEncode(galleries.Count.ToString()));

            foreach (Gallery galleryGallery in galleries)
            {
                VariableCollection galleryVariableCollection = page.template.CreateChild("gallery_list");

                galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryGallery.GalleryTitle));
                galleryVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Gallery.BuildGalleryUri(page.ProfileOwner, galleryGallery.FullPath)));
                galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(galleryGallery.ThumbUri));
                galleryVariableCollection.ParseVariables("ABSTRACT", Bbcode.Parse(HttpUtility.HtmlEncode(galleryGallery.GalleryAbstract), core.session.LoggedInMember));

                long items = galleryGallery.Items;

                if (items == 1)
                {
                    galleryVariableCollection.ParseVariables("ITEMS", "1 item.");
                }
                else
                {
                    galleryVariableCollection.ParseVariables("ITEMS", HttpUtility.HtmlEncode(string.Format("{0} items.", Functions.LargeIntegerToString(items))));
                }
            }

            List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

            page.template.ParseVariables("PHOTOS", HttpUtility.HtmlEncode(galleryItems.Count.ToString()));

            long galleryComments = 0;
            /*for (int i = 0; i < photoTable.Rows.Count; i++)*/
            int i = 0;
            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryItem.ItemTitle));
                galleryVariableCollection.ParseVariables("PHOTO_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(page.ProfileOwner, galleryItem.ParentPath, galleryItem.Path)));
                galleryVariableCollection.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemComments)));
                galleryVariableCollection.ParseVariables("VIEWS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemViews)));
                galleryVariableCollection.ParseVariables("INDEX", HttpUtility.HtmlEncode(i.ToString()));
                galleryVariableCollection.ParseVariables("ID", HttpUtility.HtmlEncode(galleryItem.ItemId.ToString()));

                string thumbUri = string.Format("/{0}/images/_thumb/{1}/{2}",
                    page.ProfileOwner.UserName, galleryPath, galleryItem.Path);
                galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(thumbUri));

                Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemId, "PHOTO");

                galleryComments += galleryItem.ItemComments;
                i++;
            }

            if (galleryItems.Count > 0)
            {
                page.template.ParseVariables("S_RATEBAR", "TRUE");
            }

            page.template.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(galleryComments.ToString()));
            page.template.ParseVariables("L_COMMENTS", HttpUtility.HtmlEncode(string.Format("{0} Comments in gallery", galleryComments)));
            page.template.ParseVariables("U_COMMENTS", HttpUtility.HtmlEncode(Linker.BuildGalleryCommentsUri(page.ProfileOwner, galleryPath)));
        }

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
                        Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                        return;
                    }

                    if (!page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                    {
                        Functions.Generate403();
                        return;
                    }

                    GroupGallery parent = new GroupGallery(core.db, page.ThisGroup);

                    string slug = HttpContext.Current.Request.Files["photo-file"].FileName;

                    try
                    {
                        string saveFileName = GalleryItem.HashFileUpload(HttpContext.Current.Request.Files["photo-file"].InputStream);
                        if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                        {
                            TPage.EnsureStoragePathExists(saveFileName);
                            HttpContext.Current.Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                        }

                        GroupGalleryItem.Create(page, page.ThisGroup, parent, title, ref slug, HttpContext.Current.Request.Files["photo-file"].FileName, saveFileName, HttpContext.Current.Request.Files["photo-file"].ContentType, (ulong)HttpContext.Current.Request.Files["photo-file"].ContentLength, description, 0x0011, license, Classification.RequestClassification());

                        page.template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(page.ThisGroup, slug)));
                        Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                        return;
                    }
                    catch (GalleryItemTooLargeException)
                    {
                        Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                        return;
                    }
                    catch (GalleryQuotaExceededException)
                    {
                        Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                        return;
                    }
                    catch (InvalidGalleryItemTypeException)
                    {
                        Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                        return;
                    }
                    catch (InvalidGalleryFileNameException)
                    {
                        Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                        return;
                    }
                }
                catch (GalleryNotFoundException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                    return;
                }
            }
            else if (mode == "upload")
            {
                page.template.SetTemplate("Gallery", "groupgalleryupload");

                if (!page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                {
                    Functions.Generate403();
                    return;
                }

                Dictionary<string, string> licenses = new Dictionary<string, string>();
                DataTable licensesTable = core.db.SelectQuery("SELECT license_id, license_title FROM licenses");

                licenses.Add("0", "Default ZinZam License");
                foreach (DataRow licenseRow in licensesTable.Rows)
                {
                    licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
                }

                page.template.ParseVariables("S_GALLERY_LICENSE", Functions.BuildSelectBox("license", licenses, "0"));
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

                if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                {
                    page.template.ParseVariables("U_UPLOAD_PHOTO", HttpUtility.HtmlEncode(Gallery.BuildGalleryUpload(page.ThisGroup)));
                }

                GroupGallery gallery = new GroupGallery(core.db, page.ThisGroup);

                List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

                if (galleryItems.Count > 0)
                {
                    page.template.ParseVariables("PHOTOS", "TRUE");
                }

                long galleryComments = 0;
                int i = 0;
                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                    galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryItem.ItemTitle));
                    galleryVariableCollection.ParseVariables("PHOTO_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(page.ThisGroup, galleryItem.Path)));
                    galleryVariableCollection.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemComments)));
                    galleryVariableCollection.ParseVariables("VIEWS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemViews)));
                    galleryVariableCollection.ParseVariables("INDEX", HttpUtility.HtmlEncode(i.ToString()));
                    galleryVariableCollection.ParseVariables("ID", HttpUtility.HtmlEncode(galleryItem.ItemId.ToString()));

                    string thumbUri = string.Format("/group/{0}/images/_thumb/{1}",
                        page.ThisGroup.Slug, galleryItem.Path);
                    galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(thumbUri));

                    Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemId, "PHOTO");

                    galleryComments += galleryItem.ItemComments;
                    i++;
                }

                page.template.ParseVariables("PAGINATION", Display.GeneratePagination(string.Format("/group/{0}/gallery",
                    page.ThisGroup.Slug), p, (int)Math.Ceiling(page.ThisGroup.GalleryItems / 12.0)));

            }
        }

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
                        Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                        return;
                    }

                    if (!page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                    {
                        Functions.Generate403();
                        return;
                    }

                    NetworkGallery parent = new NetworkGallery(core.db, page.TheNetwork);

                    string slug = HttpContext.Current.Request.Files["photo-file"].FileName;

                    try
                    {
                        string saveFileName = GalleryItem.HashFileUpload(HttpContext.Current.Request.Files["photo-file"].InputStream);
                        if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                        {
                            TPage.EnsureStoragePathExists(saveFileName);
                            HttpContext.Current.Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                        }

                        NetworkGalleryItem.Create(page, page.TheNetwork, parent, title, ref slug, HttpContext.Current.Request.Files["photo-file"].FileName, saveFileName, HttpContext.Current.Request.Files["photo-file"].ContentType, (ulong)HttpContext.Current.Request.Files["photo-file"].ContentLength, description, 0x0011, license, Classification.RequestClassification());

                        page.template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(page.TheNetwork, slug)));
                        Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                        return;
                    }
                    catch (GalleryItemTooLargeException)
                    {
                        Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                        return;
                    }
                    catch (GalleryQuotaExceededException)
                    {
                        Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                        return;
                    }
                    catch (InvalidGalleryItemTypeException)
                    {
                        Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                        return;
                    }
                    catch (InvalidGalleryFileNameException)
                    {
                        Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                        return;
                    }
                }
                catch (GalleryNotFoundException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                    return;
                }
            }
            else if (mode == "upload")
            {
                page.template.SetTemplate("Gallery", "groupgalleryupload");

                if (!page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    Functions.Generate403();
                    return;
                }

                Dictionary<string, string> licenses = new Dictionary<string, string>();
                DataTable licensesTable = core.db.SelectQuery("SELECT license_id, license_title FROM licenses");

                licenses.Add("0", "Default ZinZam License");
                foreach (DataRow licenseRow in licensesTable.Rows)
                {
                    licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
                }

                page.template.ParseVariables("S_GALLERY_LICENSE", Functions.BuildSelectBox("license", licenses, "0"));
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

                if (page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.ParseVariables("U_UPLOAD_PHOTO", HttpUtility.HtmlEncode(Gallery.BuildGalleryUpload(page.TheNetwork)));
                }

                NetworkGallery gallery = new NetworkGallery(core.db, page.TheNetwork);

                List<GalleryItem> galleryItems = gallery.GetItems(core, p, 12);

                if (galleryItems.Count > 0)
                {
                    page.template.ParseVariables("PHOTOS", "TRUE");
                }

                long galleryComments = 0;
                int i = 0;
                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = page.template.CreateChild("photo_list");

                    galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryItem.ItemTitle));
                    galleryVariableCollection.ParseVariables("PHOTO_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(page.TheNetwork, galleryItem.Path)));
                    galleryVariableCollection.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemComments)));
                    galleryVariableCollection.ParseVariables("VIEWS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(galleryItem.ItemViews)));
                    galleryVariableCollection.ParseVariables("INDEX", HttpUtility.HtmlEncode(i.ToString()));
                    galleryVariableCollection.ParseVariables("ID", HttpUtility.HtmlEncode(galleryItem.ItemId.ToString()));

                    string thumbUri = string.Format("/network/{0}/images/_thumb/{1}",
                        page.TheNetwork.NetworkNetwork, galleryItem.Path);
                    galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(thumbUri));

                    Display.RatingBlock(galleryItem.ItemRating, galleryVariableCollection, galleryItem.ItemId, "PHOTO");

                    galleryComments += galleryItem.ItemComments;

                    i++;
                }

                page.template.ParseVariables("PAGINATION", Display.GeneratePagination(string.Format("/network/{0}/gallery",
                    page.TheNetwork.NetworkNetwork), p, (int)Math.Ceiling(page.TheNetwork.GalleryItems / 12.0)));

            }
        }
    }

    public class GallerySlugNotUniqueException : Exception
    {
    }

    public class GallerySlugNotValidException : Exception
    {
    }

    public class GalleryPermissionException : Exception
    {
    }

    public class GalleryNotFoundException : Exception
    {
    }

    public class GalleryNotAMemberObjectException : Exception
    {
    }

    public class GalleryCannotUpdateRootGalleryException : Exception
    {
    }

    public class GalleryCannotUpdateChildrenException : Exception
    {
    }
}
