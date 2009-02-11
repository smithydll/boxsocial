﻿/*
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
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{

    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule("galleries", "tag")]
    public class AccountGalleriesPhotoTag : AccountSubModule
    {

        /// <summary>
        /// 
        /// </summary>
        public override string Title
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Order
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AccountGalleriesPhotoTag()
        {
            this.Load += new EventHandler(AccountGalleriesPhotoTag_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoTag_Show);
        }

        void AccountGalleriesPhotoTag_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoTag_Show(object sender, EventArgs e)
        {
            Save(new EventHandler(AccountGalleriesPhotoTag_Save));

            AuthoriseRequestSid();

            SetTemplate("account_galleries_photo_tag");

            try
            {
                long photoId = Functions.RequestLong("id", 0);

                UserGalleryItem photo = new UserGalleryItem(core, LoggedInMember, photoId);

                /* TODO: change to building path in photo class */
                string displayUri = string.Format("/{0}/images/_display/{1}/{2}",
                    Owner.Key, photo.ParentPath, photo.Path);
                template.Parse("S_PHOTO_TITLE", photo.ItemTitle);
                template.Parse("S_PHOTO_DISPLAY", displayUri);
                template.Parse("ID", photo.Id.ToString());

                List<UserTag> tags = UserTag.GetTags(core, photo);

                if (tags.Count > 0)
                {
                    template.Parse("HAS_USER_TAGS", "TRUE");
                }

                template.Parse("TAG_COUNT", tags.Count.ToString());

                int i = 0;
                foreach (UserTag tag in tags)
                {
                    VariableCollection tagsVariableCollection = template.CreateChild("user_tags");

                    tagsVariableCollection.Parse("INDEX", i.ToString());
                    tagsVariableCollection.Parse("TAG_ID", tag.TagId.ToString());
                    tagsVariableCollection.Parse("TAG_X", (tag.TagLocation.X / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("TAG_Y", (tag.TagLocation.Y / 1000 - 50).ToString());
                    tagsVariableCollection.Parse("TAG_TITLE", tag.TaggedMember.DisplayName);
                    tagsVariableCollection.Parse("TAG_USER_ID", tag.TaggedMember.Id.ToString());

                    i++;
                }
            }
            catch (GalleryItemNotFoundException)
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }

        void AccountGalleriesPhotoTag_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            try
            {
                long photoId = Functions.RequestLong("id", 0);

                UserGalleryItem photo = new UserGalleryItem(core, LoggedInMember, photoId);

                long tagCount = Functions.FormLong("tags", 0);

                List<UserTag> tags = UserTag.GetTags(core, photo);

                List<string> tagInfo = new List<string>();

				/*
				 * Tag info of the format x,y,title,uid
				 */
                for (int i = 0; i < tagCount; i++)
                {
                    tagInfo.Add(Request.Form["tag[" + i + "]"]);
                }
				
				Dictionary<long, Point> newTags = new Dictionary<long, Point>();
				
				foreach (string ti in tagInfo)
				{
					bool tagExists = false;
					string[] parts = ti.Split(',');
					
					if (parts.Length != 4)
					{
						continue;
					}
					
					int x, y;
					string title = parts[2];
					long uid;
					
					if ((!int.TryParse(parts[0], out x)) || (!int.TryParse(parts[1], out y)) || (!long.TryParse(parts[3], out uid)))
					{
						continue;
					}
					
					foreach (UserTag tag in tags)
					{
						if (tag.TaggedMember.Id == uid)
						{
							continue;
						}
						else
						{
							core.LoadUserProfile(uid);
							newTags.Add(uid, new Point(x, y));
						}
					}
				}
				
				foreach (long uid in newTags.Keys)
				{
					UserTag ut = UserTag.Create(core, photo, core.session.LoggedInMember, core.UserProfiles[uid], newTags[uid]);
				}
            }
            catch (GalleryItemNotFoundException)
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x0A)");
                return;
            }
        }
    }
}
