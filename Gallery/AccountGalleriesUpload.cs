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
    [AccountSubModule("galleries", "upload")]
    public class AccountGalleriesUpload : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return -1;
            }
        }
        public AccountGalleriesUpload()
        {
            this.Load += new EventHandler(AccountGalleriesUpload_Load);
            this.Show += new EventHandler(AccountGalleriesUpload_Show);
        }

        void AccountGalleriesUpload_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesUpload_Show(object sender, EventArgs e)
        {
            SetTemplate("account_galleries_upload");

            long galleryId = 0;
            try
            {
                galleryId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            DataTable galleryTable = db.Query(string.Format("SELECT gallery_access FROM user_galleries WHERE gallery_id = {0} AND user_id = {1}",
                galleryId, loggedInMember.UserId));


            if (galleryTable.Rows.Count == 1)
            {

                ushort galleryAccess = (ushort)galleryTable.Rows[0]["gallery_access"];

                List<string> permissions = new List<string>();
                permissions.Add("Can Read");
                permissions.Add("Can Comment");

                Display.ParseLicensingBox(template, "S_GALLERY_LICENSE", 0);
                Display.ParsePermissionsBox(template, "S_GALLERY_PERMS", galleryAccess, permissions);

                template.Parse("S_GALLERY_ID", galleryId.ToString());
                template.Parse("S_FORM_ACTION", Linker.AppendSid("/account/", true));

                Display.ParseClassification(template, "S_PHOTO_CLASSIFICATION", Classifications.Everyone);
            }
            else
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            Save(new EventHandler(AccountGalleriesUpload_Save));
        }

        void AccountGalleriesUpload_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long galleryId = Functions.RequestLong("id", 0);
            long photoId = 0;
            string title = Request.Form["title"];
            string description = Request.Form["description"];

            if (galleryId == 0)
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x08)");
                return;
            }

            try
            {
                UserGallery parent = new UserGallery(core, loggedInMember, galleryId);

                string slug = "";

                try
                {
                    slug = Request.Files["photo-file"].FileName;
                }
                catch
                {
                    Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                    return;
                }

                try
                {
                    string saveFileName = GalleryItem.HashFileUpload(Request.Files["photo-file"].InputStream);
                    if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                    {
                        TPage.EnsureStoragePathExists(saveFileName);
                        Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                    }

                    UserGalleryItem.Create(core, loggedInMember, parent, title, ref slug, Request.Files["photo-file"].FileName, saveFileName, Request.Files["photo-file"].ContentType, (ulong)Request.Files["photo-file"].ContentLength, description, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());

                    SetRedirectUri(Gallery.BuildPhotoUri(loggedInMember, parent.FullPath, slug));
                    Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            catch (InvalidGalleryException)
            {
                Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                return;
            }
        }
    }
}
