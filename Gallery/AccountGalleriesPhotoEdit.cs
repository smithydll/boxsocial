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
    [AccountSubModule("galleries", "edit-photo")]
    public class AccountGalleriesPhotoEdit : AccountSubModule
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

        public AccountGalleriesPhotoEdit()
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
                photoId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            template.SetTemplate("Gallery", "account_galleries_photo_edit");

            DataTable photoTable = db.Query(string.Format("SELECT gallery_item_abstract, gallery_item_title, gallery_item_license, gallery_item_access,gallery_item_classification FROM gallery_items WHERE user_id = {0} AND gallery_item_id = {1};",
                loggedInMember.UserId, photoId));

            if (photoTable.Rows.Count == 1)
            {
                ushort photoAccess = (ushort)photoTable.Rows[0]["gallery_item_access"];
                byte license = (byte)photoTable.Rows[0]["gallery_item_license"];
                string title = (string)photoTable.Rows[0]["gallery_item_title"];
                string description = "";

                if (!(photoTable.Rows[0]["gallery_item_abstract"] is DBNull))
                {
                    description = (string)photoTable.Rows[0]["gallery_item_abstract"];
                }

                List<string> permissions = new List<string>();
                permissions.Add("Can Read");
                permissions.Add("Can Comment");

                Display.ParseLicensingBox(template, "S_PHOTO_LICENSE", license);
                Display.ParsePermissionsBox(template, "S_PHOTO_PERMS", photoAccess, permissions);

                template.Parse("S_PHOTO_TITLE", title);
                template.Parse("S_PHOTO_DESCRIPTION", description);
                template.Parse("S_PHOTO_ID", photoId.ToString());

                Display.ParseClassification(template, "S_PHOTO_CLASSIFICATION", (Classifications)(byte)photoTable.Rows[0]["gallery_item_classification"]);
            }
            else
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }

        void AccountGalleriesPhotoEdit_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long photoId = Functions.FormLong("id", 0);
            string title = Request.Form["title"];
            string description = Request.Form["description"];

            if (photoId == 0)
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x09)");
                return;
            }

            try
            {
                UserGalleryItem galleryItem = new UserGalleryItem(core, loggedInMember, photoId);
                galleryItem.Update(title, description, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());

                SetRedirectUri(Gallery.BuildPhotoUri(loggedInMember, galleryItem.ParentPath, galleryItem.Path));
                Display.ShowMessage("Changes to Photo Saved", "You have successfully saved the changes to the photo.");
                return;
            }
            catch (GalleryItemNotFoundException)
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x0A)");
                return;
            }
        }
    }
}
