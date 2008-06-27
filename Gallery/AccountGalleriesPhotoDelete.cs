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
    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule("galleries", "delete")]
    public class AccountGalleriesPhotoDelete : AccountSubModule
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
        /// 
        /// </summary>
        public AccountGalleriesPhotoDelete()
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
            long id = Functions.RequestLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            try
            {
                UserGalleryItem ugi = new UserGalleryItem(core, loggedInMember, id);

                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", ModuleKey);
                hiddenFieldList.Add("sub", Key);
                hiddenFieldList.Add("mode", "confirm");
                hiddenFieldList.Add("id", ugi.Id.ToString());

                Display.ShowConfirmBox(Linker.AppendSid("/account", true),
                    "Confirm Delete Photo",
                    string.Format("Are you sure you want to delete the photo `{0}`",
                    ugi.ItemTitle), hiddenFieldList);
            }
            catch
            {
                DisplayGenericError();
            }
        }

        void AccountGalleriesPhotoDelete_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = Functions.FormLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
            }

            if (Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                try
                {
                    UserGalleryItem photo = new UserGalleryItem(core, loggedInMember, id);

                    try
                    {
                        photo.Delete(core);

                        SetRedirectUri(AccountModule.BuildModuleUri("galleries", "galleries"));
                        Display.ShowMessage("Photo Deleted", "You have successfully deleted the photo from the gallery.");
                    }
                    // TODO: not photo owner exception
                    /*catch (Invalid)
                    {
                        Display.ShowMessage("Unauthorised", "You are unauthorised to delete this photo.");
                        return;
                    }*/
                    catch
                    {
                        SetRedirectUri(photo.Uri);
                        Display.ShowMessage("Cannot Delete Photo", "An Error occured while trying to delete the photo, you may not be authorised to delete it.");
                        return;
                    }
                }
                catch (InvalidGalleryItemTypeException)
                {
                    Display.ShowMessage("Cannot Delete Photo", "An Error occured while trying to delete the photo, you may not be authorised to delete it.");
                    return;
                }
            }
            else
            {
            }
        }
    }
}
