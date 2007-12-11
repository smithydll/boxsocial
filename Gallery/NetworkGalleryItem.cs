/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
    public class NetworkGalleryItem : GalleryItem
    {

        public NetworkGalleryItem(Mysql db, Network owner, long itemId)
            : base(db, (Primitive)owner, itemId)
        {
        }

        public NetworkGalleryItem(Mysql db, Network owner, DataRow itemRow)
            : base(db, (Primitive)owner, itemRow)
        {
        }

        public NetworkGalleryItem(Mysql db, Network owner, string path)
            : base(db, (Primitive)owner, path)
        {
        }

        public static GalleryItem Create(TPage page, Network owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(page, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);
            return new NetworkGalleryItem(page.db, owner, itemId);
        }

        public override string BuildUri()
        {
            return ZzUri.AppendSid(string.Format("network/{0}/gallery/{1}/{2}",
                owner.Key, parentPath, path));
        }
    }
}
