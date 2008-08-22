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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Represents a user gallery item
    /// </summary>
    public class UserGalleryItem : GalleryItem
    {

        /// <summary>
        /// Initialises a new instance of the UserGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemId">Gallery item Id</param>
        public UserGalleryItem(Core core, User owner, long itemId)
            : base(core, (Primitive)owner, itemId)
        {
        }

        /// <summary>
        /// Initialises a new instance of the UserGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="itemRow">Raw data row of gallery item</param>
        public UserGalleryItem(Core core, User owner, DataRow itemRow)
            : base(core, (Primitive)owner, itemRow)
        {
        }

        /// <summary>
        /// Initialises a new instance of the UserGalleryItem class.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Gallery item owner</param>
        /// <param name="path">Gallery item path</param>
        public UserGalleryItem(Core core, User owner, string path)
            : base(core, (Primitive)owner, path)
        {
        }

        /// <summary>
        /// Creates a new user gallery item
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
        public static GalleryItem Create(Core core, User owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(core, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);

            UserGalleryItem myGalleryItem = new UserGalleryItem(core, owner, itemId);
            if (Access.FriendsCanRead(myGalleryItem.Permissions))
            {
                Action action = AppInfo.Entry.GetMostRecentFeedAction(owner);

                bool update = false;
                if (action != null)
                {
                    TimeSpan ts = core.tz.Now.Subtract(action.GetTime(core.tz));
                    if (ts.TotalDays < 2)
                    {
                        update = true;
                    }
                    else
                    {
                        update = false;
                    }
                }
                else
                {
                    update = false;
                }

                if (update)
                {
                    if (Regex.Matches(action.Body, Regex.Escape("[/thumb]")).Count < 4)
                    {
                        AppInfo.Entry.UpdateFeedAction(action, "uploaded new photos", string.Format("{0} [iurl={1}][thumb]{2}/{3}[/thumb][/iurl]",
                            action.Body, myGalleryItem.BuildUri(), myGalleryItem.ParentPath, myGalleryItem.Path));
                    }
                    else
                    {
                        // otherwise we'll just leave as is
                    }
                }
                else
                {
                    AppInfo.Entry.PublishToFeed(owner, "uploaded a new photo", string.Format("[iurl={0}][thumb]{1}/{2}[/thumb][/iurl]",
                        myGalleryItem.BuildUri(), myGalleryItem.ParentPath, myGalleryItem.Path));
                }
            }
            return myGalleryItem;
        }

        /// <summary>
        /// Returns user gallery item URI
        /// </summary>
        /// <returns></returns>
        public override string BuildUri()
        {
            return Linker.AppendSid(string.Format("{0}gallery/{1}/{2}",
                ((User)owner).UriStub, parentPath, path));
        }

        /// <summary>
        /// Returns user gallery delete item URI
        /// </summary>
        /// <returns></returns>
        public string BuildDeleteUri()
        {
            return AccountModule.BuildModuleUri("galleries", "delete", true, string.Format("id={0}", Id));
        }

        /// <summary>
        /// Returns user gallery item URI
        /// </summary>
        public override string Uri
        {
            get
            {
                return BuildUri();
            }
        }

        public override string ThumbUri
        {
            get
            {
                return Linker.AppendSid(string.Format("{0}images/_tiny/{1}",
                owner.UriStub, path));
            }
        }
    }
}
