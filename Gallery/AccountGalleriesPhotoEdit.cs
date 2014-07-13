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
    [AccountSubModule("galleries", "edit-photo")]
    public class AccountGalleriesPhotoEdit : AccountSubModule
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
        /// Initializes a new instance of the AccountGalleriesPhotoEdit class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesPhotoEdit(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGalleriesPhotoEdit_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoEdit_Show);
        }

        void AccountGalleriesPhotoEdit_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoEdit_Show(object sender, EventArgs e)
        {
            Save(new EventHandler(AccountGalleriesPhotoEdit_Save));

            AuthoriseRequestSid();

            long photoId = 0;
            try
            {
                photoId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            SetTemplate("account_galleries_photo_edit");

            try
            {
                GalleryItem photo = new GalleryItem(core, photoId);

                core.Display.ParseLicensingBox(template, "S_PHOTO_LICENSE", photo.LicenseId);

                template.Parse("S_PHOTO_TITLE", photo.ItemTitle);
                template.Parse("S_PHOTO_DESCRIPTION", photo.ItemAbstract);
                template.Parse("S_PHOTO_ID", photoId.ToString());

                core.Display.ParseClassification(template, "S_PHOTO_CLASSIFICATION", photo.Classification);
            }
            catch (GalleryItemNotFoundException)
            {
                core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }

        void AccountGalleriesPhotoEdit_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long photoId = core.Functions.FormLong("id", 0);
            string title = core.Http.Form["title"];
            string description = core.Http.Form["description"];

            if (photoId == 0)
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x09)");
                return;
            }

            try
            {
                GalleryItem galleryItem = new GalleryItem(core, LoggedInMember, photoId);
                //galleryItem.Update(title, description, core.Functions.GetLicense(), core.Functions.GetClassification());
                galleryItem.ItemTitle = title;
                galleryItem.ItemAbstract = description;
                galleryItem.LicenseId = core.Functions.GetLicenseId();
                galleryItem.Classification = core.Functions.GetClassification();

                galleryItem.Update();

                SetRedirectUri(Gallery.BuildPhotoUri(core, LoggedInMember, galleryItem.ParentPath, galleryItem.Path));
                core.Display.ShowMessage("Changes to Photo Saved", "You have successfully saved the changes to the photo.");
                return;
            }
            catch (GalleryItemNotFoundException)
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x0A)");
                return;
            }
        }
    }
}
