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
    [AccountSubModule("galleries", "gallery-cover")]
    public class AccountGalleriesPhotoCover : AccountSubModule
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
        public AccountGalleriesPhotoCover(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountGalleriesPhotoCover_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoCover_Show);
        }

        void AccountGalleriesPhotoCover_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoCover_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long pictureId = core.Functions.RequestLong("id", 0);

            if (pictureId == 0)
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x07)");
                return;
            }

            // check the image exists
            // check the image is owned by the user trying to set it as their display picture
            try
            {
                GalleryItem ugi = new GalleryItem(core, LoggedInMember, pictureId);

                string galleryFullPath = ugi.ParentPath;
                int indexOfLastSlash = galleryFullPath.LastIndexOf('/');
                string galleryPath;
                string galleryParentPath;

                if (indexOfLastSlash >= 0)
                {
                    galleryPath = galleryFullPath.Substring(indexOfLastSlash).TrimStart(new char[] { '/' });
                    galleryParentPath = galleryFullPath.Substring(0, indexOfLastSlash).TrimEnd(new char[] { '/' });
                }
                else
                {
                    galleryPath = galleryFullPath;
                    galleryParentPath = "";
                }

                DataTable galleryTable = db.Query(string.Format("SELECT gallery_id FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{2}' AND gallery_path = '{1}';",
                    LoggedInMember.UserId, Mysql.Escape(galleryPath), Mysql.Escape(galleryParentPath)));

                if (galleryTable.Rows.Count == 1)
                {
                    // only worry about view permissions, don't worry about comment permissions
                    if (true)
                    {
                        long galleryId = (long)galleryTable.Rows[0]["gallery_id"];

                        db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_highlight_id = {0} WHERE user_id = {1} AND gallery_id = {2}",
                            pictureId, LoggedInMember.UserId, galleryId));

                        SetRedirectUri(Gallery.BuildGalleryUri(core, LoggedInMember, galleryFullPath));
                        core.Display.ShowMessage("Gallery Cover Image Changed", "You have successfully changed the cover image of the gallery.");
                        return;
                    }
                    else
                    {
                        core.Display.ShowMessage("Cannot change gallery cover", "You must use a photo with equal view permissions as the gallery it is the cover of.");
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Cannot change gallery cover", "You could not change the gallery cover image to the selected image.");
                    return;
                }
            }
            catch (GalleryItemNotFoundException)
            {
                core.Display.ShowMessage("Cannot change gallery cover", "You could not change the gallery cover image to the selected image.");
                return;
            }
        }


    }
}
