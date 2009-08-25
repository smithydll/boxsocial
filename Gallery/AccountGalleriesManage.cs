﻿/*
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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group, "galleries", "galleries", true)]
    public class AccountGalleriesManage : AccountSubModule
    {

        /// <summary>
        /// 
        /// </summary>
        public override string Title
        {
            get
            {
                return "Manage Galleries";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AccountGalleriesManage()
        {
            this.Load += new EventHandler(AccountGalleriesManage_Load);
            this.Show += new EventHandler(AccountGalleriesManage_Show);
        }

        void AccountGalleriesManage_Load(object sender, EventArgs e)
        {
            if (Owner.Type == "USER")
            {
                AddModeHandler("new", new ModuleModeHandler(AccountGalleriesManage_New));
                AddModeHandler("edit", new ModuleModeHandler(AccountGalleriesManage_New));
                AddModeHandler("delete", new ModuleModeHandler(AccountGalleriesManage_Delete));
            }
        }

        void AccountGalleriesManage_Show(object sender, EventArgs e)
        {
            if (Owner.Type == "GROUP")
            {
                SetTemplate("account_galleries_group");
                return;
            }

            SetTemplate("account_galleries");

            long parentGalleryId = Functions.RequestLong("id", 0);
            string galleryParentPath = "";
            Gallery pg = null;

            if (parentGalleryId > 0)
            {
                try
                {
                    pg = new Gallery(core, LoggedInMember, parentGalleryId);

                    template.Parse("U_NEW_GALLERY", core.Uri.BuildNewGalleryUri(pg.Id));
                    template.Parse("U_UPLOAD_PHOTO", core.Uri.BuildPhotoUploadUri(pg.Id));

                    galleryParentPath = pg.FullPath;
                }
                catch (InvalidGalleryException)
                {
                    DisplayGenericError();
                    return;
                }
            }
            else
            {
                pg = new Gallery(core, LoggedInMember);
                template.Parse("U_NEW_GALLERY", core.Uri.BuildNewGalleryUri(0));
            }

            List<Gallery> ugs = pg.GetGalleries(core);

            foreach (Gallery ug in ugs)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("gallery_list");

                galleryVariableCollection.Parse("NAME", ug.GalleryTitle);
                galleryVariableCollection.Parse("ITEMS", core.Functions.LargeIntegerToString(ug.Items));

                galleryVariableCollection.Parse("U_MANAGE", core.Uri.BuildAccountSubModuleUri(ModuleKey, "galleries", ug.Id));
                galleryVariableCollection.Parse("U_VIEW", Gallery.BuildGalleryUri(core, LoggedInMember, ug.FullPath));
                galleryVariableCollection.Parse("U_EDIT", core.Uri.BuildGalleryEditUri(ug.Id));
                galleryVariableCollection.Parse("U_DELETE", core.Uri.BuildGalleryDeleteUri(ug.Id));
            }
        }

        void AccountGalleriesManage_New(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_galleries_add");

            long galleryId = Functions.RequestLong("id", 0);
            bool edit = false;

            if (e.Mode == "edit")
            {
                edit = true;
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            if (!edit)
            {
                if (galleryId > 0)
                {
                    try
                    {
                        Gallery pg = new Gallery(core, LoggedInMember, galleryId);

                        Dictionary<string, string> licenses = new Dictionary<string, string>();
                        DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

                        core.Display.ParsePermissionsBox(template, "S_GALLERY_PERMS", pg.Permissions, permissions);
                    }
                    catch (InvalidGalleryException)
                    {
                        core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                        return;
                    }
                }
                else
                {
                    core.Display.ParsePermissionsBox(template, "S_GALLERY_PERMS", 0x3333, permissions);
                }
            }
            else
            {
                // edit
                template.Parse("EDIT", "TRUE");

                try
                {
                    Gallery ug = new Gallery(core, LoggedInMember, galleryId);

                    template.Parse("S_TITLE", ug.GalleryTitle);
                    template.Parse("S_DESCRIPTION", ug.GalleryAbstract);

                    Dictionary<string, string> licenses = new Dictionary<string, string>();
                    DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

                    core.Display.ParsePermissionsBox(template, "S_GALLERY_PERMS", ug.Permissions, permissions);
                }
                catch (InvalidGalleryException)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                    return;
                }
            }

            template.Parse("S_GALLERY_ID", galleryId.ToString());

            Save(new EventHandler(AccountGalleriesManage_Save));
        }

        void AccountGalleriesManage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long galleryId = Functions.RequestLong("id", 0);

            if (galleryId == 0)
            {
                core.Display.ShowMessage("Cannot Delete Gallery", "No gallery specified to delete. Please go back and try again.");
                return;
            }

            try
            {
                Gallery gallery = new Gallery(core, LoggedInMember, galleryId);
                Gallery.Delete(core, gallery);

                SetRedirectUri(BuildUri("galleries", "galleries"));
                core.Display.ShowMessage("Gallery Deleted", "You have successfully deleted a gallery.");
            }
            catch
            {
                core.Display.ShowMessage("Cannot Delete Gallery", "An Error occured while trying to delete the gallery.");
                return;
            }
        }

        void AccountGalleriesManage_Save(object sender, EventArgs e)
        {
            long galleryId = 0;
            string title = "";
            string description = "";
            bool edit = false;

            try
            {
                galleryId = long.Parse(Request.Form["id"]);
                title = Request.Form["title"];
                description = Request.Form["description"];
            }
            catch
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x01)");
                return;
            }

            if (Request.Form["mode"] == "edit")
            {
                edit = true;
            }

            string slug = Gallery.GetSlugFromTitle(title, "");

            if (!edit)
            {
                try
                {
                    Gallery parent;
                    if (galleryId > 0)
                    {
                        parent = new Gallery(core, LoggedInMember, galleryId);
                    }
                    else
                    {
                        parent = new Gallery(core, LoggedInMember);
                    }

                    if (parent.FullPath.Length + slug.Length + 1 < 192)
                    {
                        if (Gallery.Create(core, LoggedInMember, parent, title, ref slug, description, Functions.GetPermission()) != null)
                        {
                            SetRedirectUri(core.Uri.BuildAccountSubModuleUri("galleries", "galleries", parent.GalleryId));
                            core.Display.ShowMessage("Gallery Created", "You have successfully created a new gallery.");
                            return;
                        }
                        else
                        {
                            core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x02)");
                            return;
                        }
                    }
                    else
                    {
						SetError("The gallery path you have given is too long. Try using a shorter name or less nesting.");
                        //Display.ShowMessage("Gallery Path Too Long", "The gallery path you have given is too long. Try using a shorter name or less nesting.");
                        return;
                    }
                }
                catch (GallerySlugNotUniqueException)
                {
					SetError("You have tried to create a gallery with the same name of one that already exits. Please give the gallery a unique name.");
                    //Display.ShowMessage("Gallery with same name already exists", "You have tried to create a gallery with the same name of one that already exits. Go back and give the gallery a unique name.");
                    return;
                }
                catch (GallerySlugNotValidException)
                {
					SetError("The name of the gallery you have created is invalid, please choose another name.");
                    //Display.ShowMessage("Gallery name invalid", "The name of the gallery you have created is invalid, please choose another name.");
                    return;
                }
                catch (Exception ex)
                {
                    core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x03) " + ex.ToString());
                    return;
                }
            }
            else
            {
                // save edit
                try
                {
                    Gallery gallery = new Gallery(core, LoggedInMember, galleryId);

                    try
                    {
                        if (gallery.ParentPath.Length + slug.Length + 1 < 192)
                        {
                            gallery.Update(core, title, slug, description, Functions.GetPermission());

                            SetRedirectUri(core.Uri.BuildAccountSubModuleUri("galleries", "galleries", gallery.ParentId));
                            core.Display.ShowMessage("Gallery Edit Saved", "You have saved the edits to the gallery.");
                            return;
                        }
                        else
                        {
                            core.Display.ShowMessage("Gallery Path Too Long", "The gallery path you have given is too long. Try using a shorter name or less nesting.");
                            return;
                        }
                    }
                    catch (GallerySlugNotUniqueException)
                    {
                        core.Display.ShowMessage("Gallery with same name already exists", "You have tried to create a gallery with the same name of one that already exits. Go back and give the gallery a unique name.");
                        return;
                    }
                    catch (GallerySlugNotValidException)
                    {
                        core.Display.ShowMessage("Gallery name invalid", "The name of the gallery you have created is invalid, please choose another name.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x04) " + ex.ToString());
                        return;
                    }
                }
                catch
                {
                    core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x05)");
                    return;
                }
            }
        }
    }
}
