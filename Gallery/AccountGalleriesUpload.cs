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
    /// Account sub module for uploading photos.
    /// </summary>
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group, "galleries", "upload")]
    public class AccountGalleriesUpload : AccountSubModule
    {

        /// <summary>
        /// Sub module title.
        /// </summary>
        public override string Title
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Sub module order.
        /// </summary>
        public override int Order
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Constructor for the Account sub module
        /// </summary>
        public AccountGalleriesUpload()
        {
            this.Load += new EventHandler(AccountGalleriesUpload_Load);
            this.Show += new EventHandler(AccountGalleriesUpload_Show);
        }

        /// <summary>
        /// Load procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Default show procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Show(object sender, EventArgs e)
        {
            SetTemplate("account_galleries_upload");

            long galleryId = core.Functions.RequestLong("id", 0);

            if (galleryId == 0)
            {
                // Invalid gallery
                DisplayGenericError();
                return;
            }

            try
            {
                Gallery gallery = new Gallery(core, LoggedInMember, galleryId);

                core.Display.ParseLicensingBox(template, "S_GALLERY_LICENSE", 0);

                template.Parse("S_GALLERY_ID", galleryId.ToString());

                core.Display.ParseClassification(template, "S_PHOTO_CLASSIFICATION", Classifications.Everyone);
            }
            catch (InvalidGalleryException)
            {
                DisplayGenericError();
                return;
            }

            Save(new EventHandler(AccountGalleriesUpload_Save));
        }

        /// <summary>
        /// Save procedure for uploading photos.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long galleryId = core.Functions.FormLong("id", 0);
            string title = core.Http.Form["title"];
            string description = core.Http.Form["description"];

            if (core.Http.Files["photo-file"] == null)
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }

            try
            {
                Gallery parent = new Gallery(core, LoggedInMember, galleryId);

                string slug = core.Http.Files["photo-file"].FileName;

                try
                {
                    string saveFileName = GalleryItem.HashFileUpload(core.Http.Files["photo-file"].InputStream);
                    if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                    {
                        TPage.EnsureStoragePathExists(saveFileName);
                        core.Http.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                    }

                    GalleryItem.Create(core, LoggedInMember, parent, title, ref slug, core.Http.Files["photo-file"].FileName, saveFileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, core.Functions.GetLicense(), core.Functions.GetClassification());

                    SetRedirectUri(Gallery.BuildPhotoUri(core, LoggedInMember, parent.FullPath, slug));
                    core.Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    core.Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    core.Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    core.Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            catch (InvalidGalleryException)
            {
                core.Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                return;
            }
        }
    }
}
