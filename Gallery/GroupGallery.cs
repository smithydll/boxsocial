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
using BoxSocial.Groups;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a group's gallery
    /// </summary>
    public class GroupGallery : Gallery
    {

        /// <summary>
        /// Initialises a new instance of the GroupGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Group owning</param>
        public GroupGallery(Core core, UserGroup owner)
            : base(core, owner)
        {
        }

        /// <summary>
        /// Initialises a new instance of the GroupGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Group owning</param>
        /// <param name="galleryRow">Raw data row</param>
        /// <param name="hasIcon">Raw data contains icon</param>
        public GroupGallery(Core core, UserGroup owner, DataRow galleryRow, bool hasIcon)
            : base(core, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        /// <summary>
        /// Initialises a new instance of the GroupGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Group owning</param>
        /// <param name="galleryId">Gallery Id</param>
        public GroupGallery(Core core, UserGroup owner, long galleryId)
            : base(core, owner, galleryId)
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
                items.Add(new GroupGalleryItem(core, (UserGroup)owner, dr));
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
                items.Add(new GroupGalleryItem(core, (UserGroup)owner, dr));
            }

            return items;
        }

        /// <summary>
        /// Returns a list of sub-galleries
        /// </summary>
        /// <param name="core">Page calling</param>
        /// <returns>A list of sub-galleries</returns>
        public override List<Gallery> GetGalleries(Core core)
        {
            List<Gallery> items = new List<Gallery>();

            foreach (DataRow dr in GetGalleryDataRows(core))
            {
                items.Add(new GroupGallery(core, (UserGroup)owner, dr, false));
            }

            return items;
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
            db.BeginTransaction();
            db.UpdateQuery(string.Format("UPDATE group_info SET group_gallery_items = group_gallery_items + {1}, group_bytes = group_bytes + {2} WHERE group_id = {0}",
                owner.Id, items, bytes));
        }

    }
}
