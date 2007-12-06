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
    public class GroupGallery : Gallery
    {

        public GroupGallery(Mysql db, UserGroup owner) : base(db, owner)
        {
        }

        public GroupGallery(Mysql db, UserGroup owner, DataRow galleryRow, bool hasIcon)
            : base(db, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        public GroupGallery(Mysql db, UserGroup owner, long galleryId)
            : base(db, owner, galleryId)
        {
        }

        public override List<GalleryItem> GetItems(Core core)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core))
            {
                items.Add(new GroupGalleryItem(db, (UserGroup)owner, dr));
            }

            return items;
        }

        public override List<GalleryItem> GetItems(Core core, int currentPage, int perPage)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core, currentPage, perPage))
            {
                items.Add(new GroupGalleryItem(db, (UserGroup)owner, dr));
            }

            return items;
        }

        public override List<Gallery> GetGalleries(TPage page)
        {
            List<Gallery> items = new List<Gallery>();

            foreach (DataRow dr in GetGalleryDataRows(page))
            {
                items.Add(new GroupGallery(db, (UserGroup)owner, dr, false));
            }

            return items;
        }

        public static new void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            db.UpdateQuery(string.Format("UPDATE group_info SET group_gallery_items = group_gallery_items + {1}, group_bytes = group_bytes + {2} WHERE group_id = {0}",
                owner.Id, items, bytes), true);
        }

    }
}
