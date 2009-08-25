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
    [AccountSubModule("galleries", "rotate-photo")]
    public class AccountGalleriesPhotoRotate : AccountSubModule
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
        public AccountGalleriesPhotoRotate()
        {
            this.Load += new EventHandler(AccountGalleriesPhotoRotate_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoRotate_Show);
        }

        void AccountGalleriesPhotoRotate_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoRotate_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long photoId = Functions.RequestLong("id", 0);

            if (photoId > 0)
            {
                try
                {
                    GalleryItem photo = new GalleryItem(core, LoggedInMember, photoId);

                    System.Drawing.RotateFlipType rotation = System.Drawing.RotateFlipType.RotateNoneFlipNone;

                    switch (Request.QueryString["rotation"])
                    {
                        case "right":
                        case "90": // right 90
                            rotation = System.Drawing.RotateFlipType.Rotate90FlipNone;
                            break;
                        case "180": // 180
                            rotation = System.Drawing.RotateFlipType.Rotate180FlipNone;
                            break;
                        case "left":
                        case "270": // left 90
                            rotation = System.Drawing.RotateFlipType.Rotate270FlipNone;
                            break;
                    }

                    photo.Rotate(core, rotation);

                    SetRedirectUri(Gallery.BuildPhotoUri(core, LoggedInMember, photo.ParentPath, photo.Path));
                    core.Display.ShowMessage("Image rotated", "You have successfully rotated the image.");
                    return;
                }
                catch (GalleryItemNotFoundException)
                {
                    core.Display.ShowMessage("Error", "An error has occured, go back.");
                    return;
                }
            }
            else
            {
                core.Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }
        }
    }
}
