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
    /*
     * TODO:
     * ALTER TABLE `zinzam0_zinzam`.`user_galleries` ADD COLUMN `gallery_bytes` BIGINT NOT NULL AFTER `gallery_parent_id`;
     */
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

        public override List<GalleryItem> GetItems(Core core)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core))
            {
                items.Add(new UserGalleryItem(db, (Member)owner, dr));
            }

            return items;
        }

        public override List<GalleryItem> GetItems(Core core, int currentPage, int perPage)
        {
            List<GalleryItem> items = new List<GalleryItem>();

            foreach (DataRow dr in GetItemDataRows(core, currentPage, perPage))
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
                UpdateQuery uQuery = new UpdateQuery("user_galleries");
                uQuery.AddField("gallery_items", new QueryOperation("gallery_items", QueryOperations.Addition, items));
                uQuery.AddField("gallery_bytes", new QueryOperation("gallery_bytes", QueryOperations.Addition, bytes));
                uQuery.AddCondition("gallery_id", parent.GalleryId);
                uQuery.AddCondition("user_id", owner.Id);

                db.UpdateQuery(uQuery, true);
            }
            else
            {

                UpdateQuery uQuery = new UpdateQuery("user_galleries");
                uQuery.AddField("gallery_items", new QueryOperation("gallery_items", QueryOperations.Addition, items));
                uQuery.AddField("gallery_bytes", new QueryOperation("gallery_bytes", QueryOperations.Addition, bytes));
                uQuery.AddField("gallery_highlight_id", itemId);
                uQuery.AddCondition("gallery_id", parent.GalleryId);
                uQuery.AddCondition("user_id", owner.Id);

                db.UpdateQuery(uQuery, true);
            }
        }
    }
}
