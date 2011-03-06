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

        /// <summary>
        /// Initializes a new instance of the AccountDisplayPic class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountDisplayPic(Core core)
            : base(core)
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

            LoggedInMember.LoadProfileInfo();

            if (!string.IsNullOrEmpty(LoggedInMember.UserThumbnail))
            {
                template.Parse("I_DISPLAY_PICTURE", LoggedInMember.UserThumbnail);
            }

            Save(new EventHandler(AccountDisplayPic_Save));
        }

        void AccountDisplayPic_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string meSlug = "display-pictures";

            BoxSocial.Applications.Gallery.Gallery profileGallery;
            try
            {
                profileGallery = new BoxSocial.Applications.Gallery.Gallery(core, LoggedInMember, meSlug);
            }
            catch (InvalidGalleryException)
            {
                BoxSocial.Applications.Gallery.Gallery root = new BoxSocial.Applications.Gallery.Gallery(core, LoggedInMember);
                profileGallery = BoxSocial.Applications.Gallery.Gallery.Create(core, LoggedInMember, root, "Display Pictures", ref meSlug, "All my uploaded display pictures");
            }

            if (profileGallery != null)
            {
                string title = "";
                string description = "";
                string slug = "";

                try
                {
                    slug = core.Http.Files["photo-file"].FileName;
                }
                catch
                {
                    DisplayGenericError();
                    return;
                }

                try
                {
                    string saveFileName = GalleryItem.HashFileUpload(core.Http.Files["photo-file"].InputStream);
                    if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                    {
                        TPage.EnsureStoragePathExists(saveFileName);
                        core.Http.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                    }

                    GalleryItem galleryItem = GalleryItem.Create(core, LoggedInMember, profileGallery, title, ref slug, core.Http.Files["photo-file"].FileName, saveFileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, 0, Classifications.Everyone);

                    db.UpdateQuery(string.Format("UPDATE user_info SET user_icon = {0} WHERE user_id = {1}",
                        galleryItem.Id, LoggedInMember.UserId));

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Display Picture set", "You have successfully uploaded a new display picture.");
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
                    core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
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
