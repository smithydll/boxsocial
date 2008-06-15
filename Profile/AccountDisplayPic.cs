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
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "display-picture")]
    public class AccountDisplayPic : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Display Picture";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountDisplayPic()
        {
            this.Load += new EventHandler(AccountDisplayPic_Load);
            this.Show += new EventHandler(AccountDisplayPic_Show);
        }

        void AccountDisplayPic_Load(object sender, EventArgs e)
        {
        }

        void AccountDisplayPic_Show(object sender, EventArgs e)
        {
            SetTemplate("account_display_picture");

            loggedInMember.LoadProfileInfo();

            template.Parse("S_DISPLAY_PICTURE", Linker.AppendSid("/account", true));

            if (!string.IsNullOrEmpty(loggedInMember.UserThumbnail))
            {
                template.Parse("I_DISPLAY_PICTURE", loggedInMember.UserThumbnail);
            }

            Save(new EventHandler(AccountDisplayPic_Save));
        }

        void AccountDisplayPic_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string meSlug = "display-pictures";

            UserGallery profileGallery;
            try
            {
                profileGallery = new UserGallery(core, loggedInMember, meSlug);
            }
            catch (GalleryNotFoundException)
            {
                UserGallery root = new UserGallery(core, loggedInMember);
                profileGallery = UserGallery.Create(core, root, "Display Pictures", ref meSlug, "All my uploaded display pictures", 0);
            }

            if (profileGallery != null)
            {
                string title = "";
                string description = "";
                string slug = "";

                try
                {
                    slug = Request.Files["photo-file"].FileName;
                }
                catch
                {
                    DisplayGenericError();
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

                    GalleryItem galleryItem = UserGalleryItem.Create(core, loggedInMember, profileGallery, title, ref slug, Request.Files["photo-file"].FileName, saveFileName, Request.Files["photo-file"].ContentType, (ulong)Request.Files["photo-file"].ContentLength, description, 0x3331, 0, Classifications.Everyone);

                    db.UpdateQuery(string.Format("UPDATE user_info SET user_icon = {0} WHERE user_id = {1}",
                        galleryItem.Id, loggedInMember.UserId));

                    SetRedirectUri(BuildUri());
                    Display.ShowMessage("Display Picture set", "You have successfully uploaded a new display picture.");
                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    SetError("The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    SetError("You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    SetError("You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            else
            {
                DisplayGenericError();
                return;
            }
        }
    }
}
