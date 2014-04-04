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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "galleries", "galleries", true)]
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
        /// Initializes a new instance of the AccountGalleriesManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountGalleriesManage_Load);
            this.Show += new EventHandler(AccountGalleriesManage_Show);
        }

        void AccountGalleriesManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("new", new ModuleModeHandler(AccountGalleriesManage_New));
            AddModeHandler("edit", new ModuleModeHandler(AccountGalleriesManage_New));
            AddModeHandler("delete", new ModuleModeHandler(AccountGalleriesManage_Delete));
        }

        void AccountGalleriesManage_Show(object sender, EventArgs e)
        {
            /*if (Owner.Type == "GROUP")
            {
                SetTemplate("account_galleries_group");
                return;
            }*/

            SetTemplate("account_galleries");

            long parentGalleryId = core.Functions.RequestLong("id", 0);
            string galleryParentPath = "";
            Gallery pg = null;

            if (parentGalleryId > 0)
            {
                try
                {
                    pg = new Gallery(core, Owner, parentGalleryId);

                    template.Parse("U_NEW_GALLERY", BuildUri("galleries", "new", pg.Id));
                    template.Parse("U_UPLOAD_PHOTO", BuildUri("upload", new Dictionary<string, string> { { "gallery-id", pg.Id.ToString() } }));
                    template.Parse("U_EDIT_PERMISSIONS", core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", pg.Id, ItemType.GetTypeId(typeof(Gallery))), true));

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
                pg = new Gallery(core, Owner);
                template.Parse("U_NEW_GALLERY", BuildUri("galleries", "new", 0));
                template.Parse("U_EDIT_PERMISSIONS", core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", pg.Settings.Id, ItemType.GetTypeId(typeof(GallerySettings))), true));
            }

            List<Gallery> ugs = pg.GetGalleries();

            foreach (Gallery ug in ugs)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("gallery_list");

                galleryVariableCollection.Parse("NAME", ug.GalleryTitle);
                galleryVariableCollection.Parse("ITEMS", core.Functions.LargeIntegerToString(ug.Items));

                galleryVariableCollection.Parse("U_MANAGE", BuildUri("galleries", ug.Id));
                galleryVariableCollection.Parse("U_VIEW", ug.Uri);
                galleryVariableCollection.Parse("U_EDIT_PERMISSIONS", ug.AclUri);
                galleryVariableCollection.Parse("U_EDIT", BuildUri("galleries", "edit", ug.Id));
                galleryVariableCollection.Parse("U_DELETE", BuildUri("galleries", "delete", ug.Id));
            }
        }

        void AccountGalleriesManage_New(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_galleries_add");

            long galleryId = core.Functions.RequestLong("id", 0);
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
                        Gallery pg = new Gallery(core, Owner, galleryId);

                        Dictionary<string, string> licenses = new Dictionary<string, string>();
                        DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");
                    }
                    catch (InvalidGalleryException)
                    {
                        core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                        return;
                    }
                }
                else
                {
                    // New Gallery
                }
            }
            else
            {
                // edit
                template.Parse("EDIT", "TRUE");

                try
                {
                    Gallery ug = new Gallery(core, Owner, galleryId);

                    template.Parse("S_TITLE", ug.GalleryTitle);
                    template.Parse("S_DESCRIPTION", ug.GalleryAbstract);

                    Dictionary<string, string> licenses = new Dictionary<string, string>();
                    DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");
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

            long galleryId = core.Functions.RequestLong("id", 0);

            if (galleryId == 0)
            {
                core.Display.ShowMessage("Cannot Delete Gallery", "No gallery specified to delete. Please go back and try again.");
                return;
            }

            try
            {
                Gallery gallery = new Gallery(core, Owner, galleryId);
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
                galleryId = long.Parse(core.Http.Form["id"]);
                title = core.Http.Form["title"];
                description = core.Http.Form["description"];
            }
            catch
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x01)");
                return;
            }

            if (core.Http.Form["mode"] == "edit")
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
                        parent = new Gallery(core, Owner, galleryId);
                    }
                    else
                    {
                        parent = new Gallery(core, Owner);
                    }

                    if (parent.FullPath.Length + slug.Length + 1 < 192)
                    {
                        if (Gallery.Create(core, Owner, parent, title, ref slug, description) != null)
                        {
                            SetRedirectUri(BuildUri("galleries", parent.GalleryId));
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
                    Gallery gallery = new Gallery(core, Owner, galleryId);

                    try
                    {
                        if (gallery.ParentPath.Length + slug.Length + 1 < 192)
                        {
                            gallery.Update(core, title, slug, description);

                            SetRedirectUri(BuildUri("galleries", gallery.ParentId));
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
