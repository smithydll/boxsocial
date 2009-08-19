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
    /// Represents a network's gallery
    /// </summary>
    public class NetworkGallery : Gallery
    {

        /// <summary>
        /// Initialises a new instance of the NetworkGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Network owning</param>
        public NetworkGallery(Core core, Network owner)
            : base(core, owner)
        {
        }

        /// <summary>
        /// Initialises a new instance of the NetworkGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Network owning</param>
        /// <param name="galleryRow">Raw data row</param>
        /// <param name="hasIcon">Raw data contains icon</param>
        public NetworkGallery(Core core, Network owner, DataRow galleryRow, bool hasIcon)
            : base(core, (Primitive)owner, galleryRow, hasIcon)
        {
        }

        /// <summary>
        /// Initialises a new instance of the NetworkGallery class
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Network owning</param>
        /// <param name="galleryId">Gallery Id</param>
        public NetworkGallery(Core core, Network owner, long galleryId)
            : base(core, owner, galleryId)
        {
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
            db.UpdateQuery(string.Format("UPDATE network_info SET network_gallery_items = network_gallery_items + {1}, network_bytes = network_bytes + {2} WHERE network_id = {0}",
                owner.Id, items, bytes));
        }
    }
}
