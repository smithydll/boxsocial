/*
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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "galleries", "rotate-photo")]
    public class AccountGalleriesPhotoRotate : AccountSubModule
    {
        GalleryItem galleryItem;

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
        /// Initializes a new instance of the AccountGalleriesPhotoRotate class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesPhotoRotate(Core core, Primitive owner)
            : base(core, owner)
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

            long photoId = core.Functions.RequestLong("id", 0);

            if (photoId > 0)
            {
                try
                {
                    GalleryItem photo = new GalleryItem(core, Owner, photoId);

                    System.Drawing.RotateFlipType rotation = System.Drawing.RotateFlipType.RotateNoneFlipNone;

                    switch (core.Http.Query["rotation"])
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

                    SetRedirectUri(Gallery.BuildPhotoUri(core, Owner, photo.ParentPath, photo.Path, true));
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

        public Access Access
        {
            get
            {
                if (galleryItem == null)
                {
                    galleryItem = new GalleryItem(core, core.Functions.FormLong("id", core.Functions.RequestLong("id", 0)));
                }

                return galleryItem.Parent.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "EDIT_ITEMS";
            }
        }
    }
}
