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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{
    public class UserGalleryItem : GalleryItem
    {

        public UserGalleryItem(Mysql db, Member owner, long itemId)
            : base(db, (Primitive)owner, itemId)
        {
        }

        public UserGalleryItem(Mysql db, Member owner, DataRow itemRow)
            : base(db, (Primitive) owner, itemRow)
        {
        }

        public UserGalleryItem(Mysql db, Member owner, string path)
            : base(db, (Primitive)owner, path)
        {
        }

        public static GalleryItem Create(TPage page, Member owner, Gallery parent, string title, ref string slug, string fileName, string storageName, string contentType, ulong bytes, string description, ushort permissions, byte license, Classifications classification)
        {
            long itemId = GalleryItem.create(page, (Primitive)owner, parent, title, ref slug, fileName, storageName, contentType, bytes, description, permissions, license, classification);

            UserGalleryItem myGalleryItem = new UserGalleryItem(page.db, owner, itemId);
            if (Access.FriendsCanRead(myGalleryItem.Permissions))
            {
                ApplicationEntry ae = new ApplicationEntry(page.db, owner, "Gallery");

                Action action = ae.GetMostRecentFeedAction(owner);

                bool update = false;
                if (action != null)
                {
                    TimeSpan ts = page.tz.Now.Subtract(action.GetTime(page.tz));
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
                        ae.UpdateFeedAction(action, "uploaded new photos", string.Format("{0} [iurl={1}][thumb]{2}/{3}[/thumb][/iurl]",
                            action.Body, myGalleryItem.BuildUri(), myGalleryItem.ParentPath, myGalleryItem.Path));
                    }
                    else
                    {
                        // otherwise we'll just leave as is
                    }
                }
                else
                {
                    ae.PublishToFeed(owner, "uploaded a new photo", string.Format("[iurl={0}][thumb]{1}/{2}[/thumb][/iurl]",
                        myGalleryItem.BuildUri(), myGalleryItem.ParentPath, myGalleryItem.Path));
                }
            }
            return myGalleryItem;
        }

        public override string BuildUri()
        {
            return Linker.AppendSid(string.Format("/{0}/gallery/{1}/{2}",
                ((Member)owner).UserName, parentPath, path));
        }
    }
}
