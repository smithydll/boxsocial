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
using System.Collections.Generic;
using System.Data;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a user gallery
    /// </summary>
    public class UserGallery : Gallery
    {
        /// <summary>
        /// Initialises a new instance of the UserGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        public UserGallery(Core core, User owner)
            : base(core, (Primitive)owner)
        {
        }

        /// <summary>
        /// Initialises a new instance of the UserGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryRow">Raw data row of user gallery</param>
        /// <param name="hasIcon">True if the raw data contains icon data</param>
        public UserGallery(Core core, User owner, DataRow galleryRow, bool hasIcon)
            : base(core, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        /// <summary>
        /// Initialises a new instance of the UserGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="galleryId">Gallery Id</param>
        public UserGallery(Core core, User owner, long galleryId)
            : base(core, (Primitive)owner, galleryId)
        {
        }

        /// <summary>
        /// Initialises a new instance of the UserGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery owner</param>
        /// <param name="path">Gallery path</param>
        public UserGallery(Core core, User owner, string path)
            : base(core, (Primitive)owner, path)
        {
        }

        /// <summary>
        /// Returns a list of gallery photos
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>A list of photos</returns>
        public override List<GalleryItem> GetItems(Core core)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core))
            {
                items.Add(new UserGalleryItem(core, (User)owner, dr));
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
        public override List<GalleryItem> GetItems(Core core, int currentPage, int perPage)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core, currentPage, perPage))
            {
                items.Add(new UserGalleryItem(core, (User)owner, dr));
            }

            return items;
        }

        /// <summary>
        /// Returns a list of sub-galleries
        /// </summary>
        /// <param name="core">Core token</param>
        /// <returns>A list of sub-galleries</returns>
        public override List<Gallery> GetGalleries(Core core)
        {
            List<Gallery> items = new List<Gallery>();

            foreach (DataRow dr in GetGalleryDataRows(core))
            {
                items.Add(new UserGallery(core, (User)owner, dr, true));
            }

            return items;
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
        public static UserGallery Create(Core core, Gallery parent, string title, ref string slug, string description, ushort permissions)
        {
            long galleryId = create(core, parent, title, ref slug, description, permissions);
            return new UserGallery(core, core.session.LoggedInMember, galleryId);
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
        public static new void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            if (parent.Items > 0)
            {
                UpdateQuery uQuery = new UpdateQuery("user_galleries");
                uQuery.AddField("gallery_items", new QueryOperation("gallery_items", QueryOperations.Addition, items));
                uQuery.AddField("gallery_bytes", new QueryOperation("gallery_bytes", QueryOperations.Addition, bytes));
                uQuery.AddCondition("gallery_id", parent.GalleryId);
                uQuery.AddCondition("user_id", owner.Id);

                db.Query(uQuery);
            }
            else
            {

                UpdateQuery uQuery = new UpdateQuery("user_galleries");
                uQuery.AddField("gallery_items", new QueryOperation("gallery_items", QueryOperations.Addition, items));
                uQuery.AddField("gallery_bytes", new QueryOperation("gallery_bytes", QueryOperations.Addition, bytes));
                uQuery.AddField("gallery_highlight_id", itemId);
                uQuery.AddCondition("gallery_id", parent.GalleryId);
                uQuery.AddCondition("user_id", owner.Id);

                db.BeginTransaction();
                db.Query(uQuery);
            }
        }
    }
}
