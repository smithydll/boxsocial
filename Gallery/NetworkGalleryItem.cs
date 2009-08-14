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

    /// <summary>
    /// 
    /// </summary>
    public class NetworkGalleryItem : GalleryItem
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="owner"></param>
        /// <param name="itemId"></param>
        public NetworkGalleryItem(Core core, Network owner, long itemId)
            : base(core, (Primitive)owner, itemId)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="owner"></param>
        /// <param name="itemRow"></param>
        public NetworkGalleryItem(Core core, Network owner, DataRow itemRow)
            : base(core, (Primitive)owner, itemRow)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="owner"></param>
        /// <param name="path"></param>
        public NetworkGalleryItem(Core core, Network owner, string path)
            : base(core, (Primitive)owner, path)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="owner"></param>
        /// <param name="parent"></param>
        /// <param name="title"></param>
        /// <param name="slug"></param>
        /// <param name="fileName"></param>
        /// <param name="storageName"></param>
        /// <param name="contentType"></param>
        /// <param name="bytes"></param>
        /// <param name="description"></param>
        /// <param name="permissions"></param>
        /// <param name="license"></param>
        /// <param name="classification"></param>
        /// <returns></returns>
        public static GalleryItem Create(Core core, Network owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(core, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);
            return new NetworkGalleryItem(core, owner, itemId);
        }
    }
}
