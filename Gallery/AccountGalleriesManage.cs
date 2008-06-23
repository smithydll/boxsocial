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
    [AccountSubModule("galleries", "galleries", true)]
    public class AccountGalleriesManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Galleries";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountGalleriesManage()
        {
            this.Load += new EventHandler(AccountGalleriesManage_Load);
            this.Show += new EventHandler(AccountGalleriesManage_Show);
        }

        void AccountGalleriesManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", new ModuleModeHandler(AccountGalleriesManage_Edit));
            AddModeHandler("delete", new ModuleModeHandler(AccountGalleriesManage_Delete));
        }

        void AccountGalleriesManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_galleries");

            long parentGalleryId = Functions.RequestLong("id", 0);
            string galleryParentPath = "";
            UserGallery pg = null;

            if (parentGalleryId > 0)
            {
                try
                {
                    pg = new UserGallery(core, loggedInMember, parentGalleryId);

                    template.Parse("U_NEW_GALLERY", Linker.BuildNewGalleryUri(pg.Id));
                    template.Parse("U_UPLOAD_PHOTO", Linker.BuildPhotoUploadUri(pg.Id));

                    galleryParentPath = pg.FullPath;
                }
                catch (GalleryNotFoundException)
                {
                    DisplayGenericError();
                    return;
                }
            }
            else
            {
                pg = new UserGallery(core, loggedInMember);
                template.Parse("U_NEW_GALLERY", Linker.BuildNewGalleryUri(0));
            }

            List<Gallery> ugs = pg.GetGalleries(core);

            foreach (Gallery ug in ugs)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("gallery_list");

                galleryVariableCollection.Parse("NAME", ug.GalleryTitle);
                galleryVariableCollection.Parse("ITEMS", Functions.LargeIntegerToString(ug.Items));

                galleryVariableCollection.Parse("U_MANAGE", string.Format("/account/galleries/galleries?id={0}",
                    ug.Id));
                galleryVariableCollection.Parse("U_VIEW", Gallery.BuildGalleryUri(loggedInMember, ug.FullPath));
                galleryVariableCollection.Parse("U_EDIT", Linker.BuildGalleryEditUri(ug.Id));
                galleryVariableCollection.Parse("U_DELETE", Linker.BuildGalleryDeleteUri(ug.Id));
            }
        }

        void AccountGalleriesManage_Edit(object sender, EventArgs e)
        {

        }

        void AccountGalleriesManage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long galleryId = Functions.RequestLong("id", 0);

            if (galleryId == 0)
            {
                Display.ShowMessage("Cannot Delete Gallery", "No gallery specified to delete. Please go back and try again.");
                return;
            }

            try
            {
                UserGallery gallery = new UserGallery(core, loggedInMember, galleryId);
                Gallery.Delete(core, gallery);

                SetRedirectUri(AccountModule.BuildModuleUri("galleries", "galleries"));
                Display.ShowMessage("Gallery Deleted", "You have successfully deleted a gallery.");
            }
            catch
            {
                Display.ShowMessage("Cannot Delete Gallery", "An Error occured while trying to delete the gallery.");
                return;
            }
        }
    }
}
