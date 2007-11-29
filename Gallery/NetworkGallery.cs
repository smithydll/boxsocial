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
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{
    public class NetworkGallery : Gallery
    {
        public NetworkGallery(Mysql db, Network owner)
            : base(db, owner)
        {
        }

        public NetworkGallery(Mysql db, Network owner, DataRow galleryRow, bool hasIcon)
            : base(db, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        public NetworkGallery(Mysql db, Network owner, long galleryId)
            : base(db, owner, galleryId)
        {
        }

        public override List<GalleryItem> GetItems(TPage page)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(page))
            {
                items.Add(new NetworkGalleryItem(db, (Network)owner, dr));
            }

            return items;
        }

        public override List<GalleryItem> GetItems(TPage page, int currentPage, int perPage)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(page, currentPage, perPage))
            {
                items.Add(new NetworkGalleryItem(db, (Network)owner, dr));
            }

            return items;
        }

        public override List<Gallery> GetGalleries(TPage page)
        {
            List<Gallery> items = new List<Gallery>();

            foreach (DataRow dr in GetGalleryDataRows(page))
            {
                items.Add(new NetworkGallery(db, (Network)owner, dr, false));
            }

            return items;
        }

        public static new void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            db.UpdateQuery(string.Format("UPDATE network_info SET network_gallery_items = network_gallery_items + {1}, network_bytes = network_bytes + {2} WHERE network_id = {0}",
                owner.Id, items, bytes), true);
        }
    }
}
