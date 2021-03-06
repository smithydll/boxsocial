﻿/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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
    [AccountSubModule("galleries", "display-pic")]
    public class AccountGalleriesPhotoDisplayPic : AccountSubModule
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
        /// Initializes a new instance of the AccountGalleriesPhotoDisplayPic class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesPhotoDisplayPic(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGalleriesPhotoDisplayPic_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoDisplayPic_Show);
        }

        void AccountGalleriesPhotoDisplayPic_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoDisplayPic_Show(object sender, EventArgs e)
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

                // check for public view permissions on the image
                if (ugi.PermissiveParent.Access.IsPublic())
                {

                    LoggedInMember.UserInfo.DisplayPictureId = pictureId;
                    LoggedInMember.UserInfo.Update();

                    core.Display.ShowMessage("Display Picture Changed", "You have successfully changed your display picture.");
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Cannot set as display picture", "You must use a photo with public view permissions as your display picture.");
                    return;
                }
            }
            catch (GalleryItemNotFoundException)
            {
                core.Display.ShowMessage("Cannot change display picture", "You could not change your display picture to the selected image.");
                return;
            }
        }
    }
}
