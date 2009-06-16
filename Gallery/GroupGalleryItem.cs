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
    /// Represents a group gallery item
    /// </summary>
    public class GroupGalleryItem : GalleryItem
    {

        /// <summary>
        /// Initialises a new instance of the GroupGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemId">Gallery item Id</param>
        public GroupGalleryItem(Core core, UserGroup owner, long itemId)
            : base(core, (Primitive)owner, itemId)
        {
        }

        /// <summary>
        /// Initialises a new instance of the GroupGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public GroupGalleryItem(Core core, UserGroup owner, DataRow itemRow)
            : base(core, (Primitive)owner, itemRow)
        {
        }

        /// <summary>
        /// Initialises a new instance of the GroupGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="path">Gallery item path</param>
        public GroupGalleryItem(Core core, UserGroup owner, string path)
            : base(core, (Primitive)owner, path)
        {
        }

        /// <summary>
        /// Creates a new group gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Owner</param>
        /// <param name="parent">Gallery</param>
        /// <param name="title">Title</param>
        /// <param name="slug">Slug</param>
        /// <param name="fileName">File name</param>
        /// <param name="storageName">Storage name</param>
        /// <param name="contentType">Content type</param>
        /// <param name="bytes">Bytes</param>
        /// <param name="description">Description</param>
        /// <param name="permissions">Permissions mask</param>
        /// <param name="license">License</param>
        /// <param name="classification">Classification</param>
        /// <remarks>Slug is a reference</remarks>
        /// <returns>New gallery item</returns>
        public static GalleryItem Create(Core core, UserGroup owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(core, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);
            return new GroupGalleryItem(core, owner, itemId);
        }

        /// <summary>
        /// Returns group gallery item uri
        /// </summary>
        /// <returns></returns>
        public override string BuildUri()
        {
            return core.Uri.AppendSid(string.Format("{0}gallery/{1}",
                owner.UriStub, path));
        }

        /// <summary>
        /// Returns group gallery item uri
        /// </summary>
        public override string Uri
        {
            get
            {
                return BuildUri();
            }
        }

        /// <summary>
        /// Returns the group gallery item thumbnail uri
        /// </summary>
        public override string ThumbUri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}images/_tiny/{1}",
                owner.UriStub, path));
            }
        }
    }
}
