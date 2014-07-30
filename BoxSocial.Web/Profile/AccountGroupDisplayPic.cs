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
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule(AppPrimitives.Group, "groups", "display-picture")]
    public class AccountGroupDisplayPic : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Group Display Picture";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountGroupDisplayPic class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGroupDisplayPic(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGroupDisplayPic_Load);
            this.Show += new EventHandler(AccountGroupDisplayPic_Show);
        }

        void AccountGroupDisplayPic_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupDisplayPic_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_icon");

            if (Owner.GetType() != typeof(UserGroup))
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = (UserGroup)Owner;

            if (!string.IsNullOrEmpty(thisGroup.Thumbnail))
            {
                template.Parse("I_DISPLAY_PICTURE", thisGroup.Thumbnail);
            }

            Save(new EventHandler(AccountGroupDisplayPic_Save));
        }

        void AccountGroupDisplayPic_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            if (Owner.GetType() != typeof(UserGroup))
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = (UserGroup)Owner;

            string meSlug = "display-pictures";

            BoxSocial.Applications.Gallery.Gallery profileGallery;
            try
            {
                profileGallery = new BoxSocial.Applications.Gallery.Gallery(core, thisGroup, meSlug);
            }
            catch (InvalidGalleryException)
            {
                BoxSocial.Applications.Gallery.Gallery root = new BoxSocial.Applications.Gallery.Gallery(core, thisGroup);
                profileGallery = BoxSocial.Applications.Gallery.Gallery.Create(core, thisGroup, root, "Display Pictures", ref meSlug, "Group display pictures");
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
                    MemoryStream stream = new MemoryStream();
                    core.Http.Files["photo-file"].InputStream.CopyTo(stream);

                    db.BeginTransaction();

                    GalleryItem galleryItem = GalleryItem.Create(core, thisGroup, profileGallery, title, ref slug, core.Http.Files["photo-file"].FileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, 0, Classifications.Everyone, stream, false);

                    db.UpdateQuery(string.Format("UPDATE group_info SET group_icon = {0} WHERE group_id = {1}",
                        galleryItem.Id, thisGroup.GroupId));

                    //db.CommitTransaction();
                    stream.Close();

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
