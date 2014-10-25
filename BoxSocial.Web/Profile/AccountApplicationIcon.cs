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
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule(AppPrimitives.Application, "applications", "icon")]
    public class AccountApplicationIcon : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Application Icon";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        public AccountApplicationIcon(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountApplicationIcon_Load);
            this.Show += new EventHandler(AccountApplicationIcon_Show);
        }

        void AccountApplicationIcon_Load(object sender, EventArgs e)
        {
        }

        void AccountApplicationIcon_Show(object sender, EventArgs e)
        {
            SetTemplate("account_application_icon");

            if (Owner.GetType() != typeof(ApplicationEntry))
            {
                DisplayGenericError();
                return;
            }

            ApplicationEntry ae = (ApplicationEntry)Owner;

            if (!string.IsNullOrEmpty(ae.Thumbnail))
            {
                template.Parse("I_ICON", ae.Thumbnail);
            }

            Save(new EventHandler(AccountApplicationIcon_Save));
        }

        void AccountApplicationIcon_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            if (Owner.GetType() != typeof(ApplicationEntry))
            {
                DisplayGenericError();
                return;
            }

            ApplicationEntry ae = (ApplicationEntry)Owner;

            string meSlug = "application-icons";

            BoxSocial.Applications.Gallery.Gallery profileGallery;
            try
            {
                profileGallery = new BoxSocial.Applications.Gallery.Gallery(core, ae, meSlug);
            }
            catch (InvalidGalleryException)
            {
                BoxSocial.Applications.Gallery.Gallery root = new BoxSocial.Applications.Gallery.Gallery(core, ae);
                profileGallery = BoxSocial.Applications.Gallery.Gallery.Create(core, ae, root, "Application Icons", ref meSlug, "Application Icons");
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
                    if (ae.GalleryIcon > 0)
                    {
                        try
                        {
                            GalleryItem gi = new GalleryItem(core, ae.GalleryIcon);
                            gi.Delete();
                        }
                        catch
                        {
                        }
                    }

                    MemoryStream stream = new MemoryStream();
                    core.Http.Files["photo-file"].InputStream.CopyTo(stream);

                    db.BeginTransaction();

                    GalleryItem galleryItem = GalleryItem.Create(core, ae, profileGallery, title, ref slug, core.Http.Files["photo-file"].FileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, 0, Classifications.Everyone, stream, false);

                    db.UpdateQuery(string.Format("UPDATE applications SET application_gallery_icon = {0} WHERE application_id = {1}",
                        galleryItem.Id, ae.Id));

                    //db.CommitTransaction();
                    stream.Close();

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Icon uploaded", "You have successfully uploaded a new application icon.");
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
