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
    public class UserGallery : Gallery
    {
        public UserGallery(Mysql db, Member owner)
            : base(db, (Primitive)owner)
        {
        }

        public UserGallery(Mysql db, Member owner, DataRow galleryRow, bool hasIcon)
            : base(db, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        public UserGallery(Mysql db, Member owner, long galleryId)
            : base(db, (Primitive)owner, galleryId)
        {
        }

        public UserGallery(Mysql db, Member owner, string path)
            : base(db, (Primitive)owner, path)
        {
        }

        public override List<GalleryItem> GetItems(TPage page)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(page))
            {
                items.Add(new UserGalleryItem(db, (Member)owner, dr));
            }

            return items;
        }

        public override List<GalleryItem> GetItems(TPage page, int currentPage, int perPage)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(page, currentPage, perPage))
            {
                items.Add(new UserGalleryItem(db, (Member)owner, dr));
            }

            return items;
        }

        public override List<Gallery> GetGalleries(TPage page)
        {
            List<Gallery> items = new List<Gallery>();

            foreach (DataRow dr in GetGalleryDataRows(page))
            {
                items.Add(new UserGallery(db, (Member)owner, dr, true));
            }

            return items;
        }

        public static UserGallery Create(TPage page, Gallery parent, string title, ref string slug, string description, ushort permissions)
        {
            long galleryId = create(page, parent, title, ref slug, description, permissions);
            return new UserGallery(page.db, page.loggedInMember, galleryId);
        }

        public static new void UpdateGalleryInfo(Mysql db, Primitive owner, Gallery parent, long itemId, int items, long bytes)
        {
            if (parent.Items > 0)
            {
                db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_items = gallery_items + 1 WHERE gallery_id = {0} AND user_id = {1}",
                    parent.GalleryId, owner.Id), true);
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_items = gallery_items + 1, gallery_highlight_id = {2} WHERE gallery_id = {0} AND user_id = {1}",
                    parent.GalleryId, owner.Id, itemId), true);
            }
        }
    }
}
