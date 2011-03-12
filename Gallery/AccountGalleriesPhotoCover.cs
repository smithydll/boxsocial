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
        /// Initializes a new instance of the AccountGalleriesPhotoCover class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
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

                try
                {
                    Gallery gallery = new Gallery(core, Owner, galleryFullPath);

                    gallery.HighlightId = pictureId;
                    gallery.Update();

                    SetRedirectUri(Gallery.BuildGalleryUri(core, LoggedInMember, galleryFullPath));
                    core.Display.ShowMessage("Gallery Cover Image Changed", "You have successfully changed the cover image of the gallery.");
                    return;
                }
                catch (InvalidGalleryException)
                {
                    core.Display.ShowMessage("Cannot change gallery cover", "You could not change the gallery cover image to the selected image.");
                    return;
                }
                catch (UnauthorisedToUpdateItemException)
                {
                    core.Display.ShowMessage("Unauthorised", "You are unauthorised to complete this action.");
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
