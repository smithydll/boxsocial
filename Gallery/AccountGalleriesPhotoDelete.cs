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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "galleries", "delete")]
    public class AccountGalleriesPhotoDelete : AccountSubModule
    {
        GalleryItem galleryItem;

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
        /// Initializes a new instance of the AccountGalleriesPhotoDelete class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesPhotoDelete(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGalleriesPhotoDelete_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoDelete_Show);
        }

        void AccountGalleriesPhotoDelete_Load(object sender, EventArgs e)
        {
            AddSaveHandler("confirm", new EventHandler(AccountGalleriesPhotoDelete_Save));
        }

        void AccountGalleriesPhotoDelete_Show(object sender, EventArgs e)
        {
            long id = core.Functions.RequestLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            try
            {
                GalleryItem ugi = new GalleryItem(core, Owner, id);

                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", ModuleKey);
                hiddenFieldList.Add("sub", Key);
                hiddenFieldList.Add("mode", "confirm");
                hiddenFieldList.Add("id", ugi.Id.ToString());

                core.Display.ShowConfirmBox(core.Hyperlink.AppendSid(Owner.AccountUriStub, true),
                    "Confirm Delete Photo",
                    string.Format("Are you sure you want to delete the photo `{0}`",
                    ugi.Path), hiddenFieldList);
            }
            catch
            {
                DisplayGenericError();
            }
        }

        void AccountGalleriesPhotoDelete_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = core.Functions.FormLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                try
                {
                    GalleryItem photo = new GalleryItem(core, Owner, id);

                    try
                    {
                        photo.Delete();

                        SetRedirectUri(BuildUri("galleries", "galleries"));
                        core.Display.ShowMessage("Photo Deleted", "You have successfully deleted the photo from the gallery.");
                    }
                    // TODO: not photo owner exception
                    /*catch (Invalid)
                    {
                        Display.ShowMessage("Unauthorised", "You are unauthorised to delete this photo.");
                        return;
                    }*/
                    catch (Exception ex)
                    {
                        SetRedirectUri(photo.Uri);
                        core.Display.ShowMessage("Cannot Delete Photo", "An Error occured while trying to delete the photo, you may not be authorised to delete it." + ex.ToString());
                        return;
                    }
                }
                catch (InvalidGalleryItemTypeException)
                {
                    core.Display.ShowMessage("Cannot Delete Photo", "An Error occured while trying to delete the photo, you may not be authorised to delete it.");
                    return;
                }
            }
            else
            {
            }
        }

        public Access Access
        {
            get
            {
                if (galleryItem == null)
                {
                    galleryItem = new GalleryItem(core, core.Functions.FormLong("id", core.Functions.RequestLong("id", 0)));
                }

                return galleryItem.Parent.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "DELETE_ITEMS";
            }
        }
    }
}
