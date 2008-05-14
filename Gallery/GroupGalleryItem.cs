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
    public class GroupGalleryItem : GalleryItem
    {

        public GroupGalleryItem(Core core, UserGroup owner, long itemId)
            : base(core, (Primitive)owner, itemId)
        {
        }

        public GroupGalleryItem(Core core, UserGroup owner, DataRow itemRow)
            : base(core, (Primitive)owner, itemRow)
        {
        }

        public GroupGalleryItem(Core core, UserGroup owner, string path)
            : base(core, (Primitive)owner, path)
        {
        }

        public static GalleryItem Create(Core core, UserGroup owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(core, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);
            return new GroupGalleryItem(core, owner, itemId);
        }

        public override string BuildUri()
        {
            return Linker.AppendSid(string.Format("group/{0}/gallery/{1}/{2}",
                owner.Key, parentPath, path));
        }

        public override string Uri
        {
            get
            {
                return BuildUri();
            }
        }
    }
}
