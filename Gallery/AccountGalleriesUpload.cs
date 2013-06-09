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
using System.Drawing;
using System.Drawing.Imaging;
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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "galleries", "upload")]
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
        /// Initializes a new instance of the AccountGalleriesUpload class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesUpload(Core core)
            : base(core)
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
                Gallery gallery = new Gallery(core, Owner, galleryId);

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
                Gallery parent = new Gallery(core, Owner, galleryId);

                string slug = core.Http.Files["photo-file"].FileName;

                try
                {
                    MemoryStream stream = new MemoryStream();
                    core.Http.Files["photo-file"].InputStream.CopyTo(stream);

                    // start here for new storage framework
                    //Owner.StoreFile(core.Http.Files["photo-file"].InputStream);

                    db.BeginTransaction();

                    Image image = Image.FromStream(stream);
                    int width = image.Width;
                    int height = image.Height;

                    RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone;
                    foreach (PropertyItem p in image.PropertyItems)
                    {
                        if (p.Id == 274)
                        {
                            switch ((int)p.Value[0])
                            {
                                case 1:
                                    rotate = RotateFlipType.RotateNoneFlipNone;
                                    break;
                                case 2:
                                    rotate = RotateFlipType.RotateNoneFlipX;
                                    break;
                                case 3:
                                    rotate = RotateFlipType.Rotate180FlipNone;
                                    break;
                                case 4:
                                    rotate = RotateFlipType.Rotate180FlipX;
                                    break;
                                case 5:
                                    rotate = RotateFlipType.Rotate90FlipX;
                                    break;
                                case 6:
                                    rotate = RotateFlipType.Rotate90FlipNone;
                                    break;
                                case 7:
                                    rotate = RotateFlipType.Rotate270FlipX;
                                    break;
                                case 8:
                                    rotate = RotateFlipType.Rotate270FlipNone;
                                    break;
                                default:
                                    rotate = RotateFlipType.RotateNoneFlipNone;
                                    break;
                            }
                        }
                    }

                    if (rotate != RotateFlipType.RotateNoneFlipNone)
                    {
                        image.RotateFlip(rotate);

                        width = image.Width;
                        height = image.Height;

                        ImageFormat iF = ImageFormat.Jpeg;
                        stream = new MemoryStream();
                        image.Save(stream, iF);
                        rotate = RotateFlipType.RotateNoneFlipNone;
                    }

                    string saveFileName = core.Storage.SaveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), stream);

                    GalleryItem.Create(core, Owner, parent, title, ref slug, core.Http.Files["photo-file"].FileName, saveFileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, core.Functions.GetLicenseId(), core.Functions.GetClassification(), width, height);
                    stream.Close();

                    db.CommitTransaction();

                    SetRedirectUri(Gallery.BuildPhotoUri(core, Owner, parent.FullPath, slug));
                    core.Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");

                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to " + Functions.BytesToString(core.Settings.MaxFileSize) + " in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            catch (InvalidGalleryException)
            {
                db.RollBackTransaction();
                core.Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                return;
            }
        }
    }
}
