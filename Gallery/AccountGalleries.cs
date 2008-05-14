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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Gallery
{
    public class AccountGalleries : AccountModule
    {
        public AccountGalleries(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageGalleries);
            // TODO: Gallery Preferences
            RegisterSubModule += new RegisterSubModuleHandler(UploadPhoto);
            RegisterSubModule += new RegisterSubModuleHandler(NewGallery);
            RegisterSubModule += new RegisterSubModuleHandler(MarkPhotoAsDisplayPicture);
            RegisterSubModule += new RegisterSubModuleHandler(MarkPhotoAsGalleryCover);
            RegisterSubModule += new RegisterSubModuleHandler(EditPhoto);
            RegisterSubModule += new RegisterSubModuleHandler(RotatePhoto);
            RegisterSubModule += new RegisterSubModuleHandler(PhotoDelete);
            RegisterSubModule += new RegisterSubModuleHandler(PhotoTag);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Galleries";
            }
        }

        public override string Key
        {
            get
            {
                return "galleries";
            }
        }

        public override int Order
        {
            get
            {
                return 6;
            }
        }

        private void ManageGalleries(string submodule)
        {
            subModules.Add("galleries", "Manage Galleries");
            if (submodule != "galleries" && !string.IsNullOrEmpty(submodule)) return;

            if (Request.QueryString["mode"] == "delete")
            {
                GalleryDelete();
                return;
            }
            else if (Request.QueryString["mode"] == "edit")
            {
                NewGallery();
                return;
            }

            long parentGalleryId = 0;
            string galleryParentPath = "";

            try
            {
                parentGalleryId = long.Parse(Request.QueryString["id"]);
            }
            catch { }

            if (parentGalleryId > 0)
            {
                DataTable galleryParentTable = db.Query(string.Format("SELECT gallery_parent_path, gallery_path FROM user_galleries WHERE user_id = {0} AND gallery_id = {1}",
                    loggedInMember.UserId, parentGalleryId));

                if (galleryParentTable.Rows.Count > 0)
                {
                    template.ParseVariables("U_NEW_GALLERY", HttpUtility.HtmlEncode(Linker.BuildNewGalleryUri(parentGalleryId)));
                    template.ParseVariables("U_UPLOAD_PHOTO", HttpUtility.HtmlEncode(Linker.BuildPhotoUploadUri(parentGalleryId)));

                    if (!(galleryParentTable.Rows[0]["gallery_parent_path"] is DBNull))
                    {
                        if ((string)galleryParentTable.Rows[0]["gallery_parent_path"] == "")
                        {
                            galleryParentPath = (string)galleryParentTable.Rows[0]["gallery_path"];
                        }
                        else
                        {
                            galleryParentPath = (string)galleryParentTable.Rows[0]["gallery_parent_path"] + "/" + (string)galleryParentTable.Rows[0]["gallery_path"];
                        }
                    }
                }
            }
            else
            {
                template.ParseVariables("U_NEW_GALLERY", HttpUtility.HtmlEncode(Linker.BuildNewGalleryUri(0)));
            }

            template.SetTemplate("Gallery", "account_galleries");

            DataTable galleriesTable = db.Query(string.Format("SELECT gallery_id, gallery_items, gallery_title, gallery_path, gallery_parent_path FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{1}' ORDER BY gallery_title ASC",
                loggedInMember.UserId, Mysql.Escape(galleryParentPath)));

            for (int i = 0; i < galleriesTable.Rows.Count; i++)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("gallery_list");

                galleryVariableCollection.ParseVariables("NAME", HttpUtility.HtmlEncode((string)galleriesTable.Rows[i]["gallery_title"]));
                galleryVariableCollection.ParseVariables("ITEMS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString((int)galleriesTable.Rows[i]["gallery_items"])));

                galleryVariableCollection.ParseVariables("U_MANAGE", HttpUtility.HtmlEncode(string.Format("/account/?module=galleries&sub=galleries&id={0}",
                    (long)galleriesTable.Rows[i]["gallery_id"])));

                if (!(galleriesTable.Rows[i]["gallery_parent_path"] is DBNull))
                {
                    if ((string)galleriesTable.Rows[i]["gallery_parent_path"] == "")
                    {
                        galleryVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(Gallery.BuildGalleryUri(loggedInMember, (string)galleriesTable.Rows[i]["gallery_path"])));
                    }
                    else
                    {
                        galleryVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(Gallery.BuildGalleryUri(loggedInMember, (string)galleriesTable.Rows[i]["gallery_parent_path"] + "/" + (string)galleriesTable.Rows[i]["gallery_path"])));
                    }
                }

                galleryVariableCollection.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(Linker.BuildGalleryEditUri((long)galleriesTable.Rows[i]["gallery_id"])));
                galleryVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildGalleryDeleteUri((long)galleriesTable.Rows[i]["gallery_id"])));
            }
        }

        private void NewGallery(string submodule)
        {
            subModules.Add("new", null);
            if (submodule != "new") return;

            NewGallery();
        }

        private void NewGallery()
        {
            if (Request.Form["save"] != null)
            {
                SaveNewGallery();
            }

            AuthoriseRequestSid();

            long galleryId = 0;
            bool edit = false;

            try
            {
                galleryId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            if (Request.QueryString["mode"] == "edit")
            {
                edit = true;
            }

            template.SetTemplate("Gallery", "account_galleries_add");

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            if (!edit)
            {
                if (galleryId > 0)
                {
                    DataTable galleryTable = db.Query(string.Format("SELECT gallery_access FROM user_galleries WHERE gallery_id = {0} AND user_id = {1}",
                        galleryId, loggedInMember.UserId));

                    if (galleryTable.Rows.Count == 1)
                    {

                        ushort galleryAccess = (ushort)galleryTable.Rows[0]["gallery_access"];

                        Dictionary<string, string> licenses = new Dictionary<string, string>();
                        DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

                        template.ParseVariables("S_GALLERY_PERMS", Functions.BuildPermissionsBox(galleryAccess, permissions));
                    }
                    else
                    {
                        Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                    }
                }
                else
                {
                    template.ParseVariables("S_GALLERY_PERMS", Functions.BuildPermissionsBox(0x3333, permissions));
                }
            }
            else
            {
                // edit
                template.ParseVariables("EDIT", "TRUE");

                DataTable galleryTable = db.Query(string.Format("SELECT gallery_title, gallery_access, gallery_abstract FROM user_galleries WHERE gallery_id = {0} AND user_id = {1}",
                        galleryId, loggedInMember.UserId));

                if (galleryTable.Rows.Count == 1)
                {
                    template.ParseVariables("S_TITLE", HttpUtility.HtmlEncode((string)galleryTable.Rows[0]["gallery_title"]));

                    if (!(galleryTable.Rows[0]["gallery_abstract"] is DBNull))
                    {
                        template.ParseVariables("S_DESCRIPTION", HttpUtility.HtmlEncode((string)galleryTable.Rows[0]["gallery_abstract"]));
                    }

                    ushort galleryAccess = (ushort)galleryTable.Rows[0]["gallery_access"];

                    Dictionary<string, string> licenses = new Dictionary<string, string>();
                    DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

                    template.ParseVariables("S_GALLERY_PERMS", Functions.BuildPermissionsBox(galleryAccess, permissions));
                }
                else
                {
                    Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            template.ParseVariables("S_GALLERY_ID", HttpUtility.HtmlEncode(galleryId.ToString()));
        }

        private void GalleryDelete()
        {
            AuthoriseRequestSid();

            long galleryId = 0;

            try
            {
                galleryId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot Delete Gallery", "No gallery specified to delete. Please go back and try again.");
                return;
            }

            /*long[] stuffDeleted = GalleryDeleteChildren(galleryId);
            long itemsDeleted = stuffDeleted[0];
            long bytesDeleted = stuffDeleted[1];

            // committ the transaction
            db.UpdateQuery(string.Format("UPDATE user_info SET user_gallery_items = user_gallery_items - {1}, user_bytes = user_bytes - {2} WHERE user_id = {1}",
                loggedInMember.UserId, itemsDeleted, bytesDeleted), false);*/

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

        private void SaveNewGallery()
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
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x01)");
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
                    UserGallery parent;
                    if (galleryId > 0)
                    {
                        parent = new UserGallery(core, loggedInMember, galleryId);
                    }
                    else
                    {
                        parent = new UserGallery(core, loggedInMember);
                    }

                    if (parent.FullPath.Length + slug.Length + 1 < 192)
                    {
                        if (UserGallery.Create(core, parent, title, ref slug, description, Functions.GetPermission()) != null)
                        {
                            SetRedirectUri(AccountModule.BuildModuleUri("galleries", "galleries",
                                string.Format("id={0}", parent.GalleryId)));
                            Display.ShowMessage("Gallery Created", "You have successfully created a new gallery.");
                            return;
                        }
                        else
                        {
                            Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x02)");
                            return;
                        }
                    }
                    else
                    {
                        Display.ShowMessage("Gallery Path Too Long", "The gallery path you have given is too long. Try using a shorter name or less nesting.");
                        return;
                    }
                }
                catch (GallerySlugNotUniqueException)
                {
                    Display.ShowMessage("Gallery with same name already exists", "You have tried to create a gallery with the same name of one that already exits. Go back and give the gallery a unique name.");
                    return;
                }
                catch (GallerySlugNotValidException)
                {
                    Display.ShowMessage("Gallery name invalid", "The name of the gallery you have created is invalid, please choose another name.");
                    return;
                }
                catch (Exception ex)
                {
                    Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x03) " + ex.ToString());
                    return;
                }
            }
            else
            {
                // save edit
                try
                {
                    UserGallery gallery = new UserGallery(core, loggedInMember, galleryId);

                    try
                    {
                        if (gallery.ParentPath.Length + slug.Length + 1 < 192)
                        {
                            gallery.Update(page, title, slug, description, Functions.GetPermission());

                            SetRedirectUri(AccountModule.BuildModuleUri("galleries", "galleries",
                                string.Format("id={0}", gallery.ParentId)));
                            Display.ShowMessage("Gallery Edit Saved", "You have saved the edits to the gallery.");
                            return;
                        }
                        else
                        {
                            Display.ShowMessage("Gallery Path Too Long", "The gallery path you have given is too long. Try using a shorter name or less nesting.");
                            return;
                        }
                    }
                    catch (GallerySlugNotUniqueException)
                    {
                        Display.ShowMessage("Gallery with same name already exists", "You have tried to create a gallery with the same name of one that already exits. Go back and give the gallery a unique name.");
                        return;
                    }
                    catch (GallerySlugNotValidException)
                    {
                        Display.ShowMessage("Gallery name invalid", "The name of the gallery you have created is invalid, please choose another name.");
                        return;
                    }
                    catch
                    {
                        Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x04)");
                        return;
                    }
                }
                catch
                {
                    Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x05)");
                    return;
                }
            }
        }

        private void UploadPhoto(string submodule)
        {
            subModules.Add("upload", null);
            if (submodule != "upload") return;

            if (Request.Form["save"] != null)
            {
                SavePhoto();
            }

            AuthoriseRequestSid();

            long galleryId = 0;
            try
            {
                galleryId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            template.SetTemplate("Gallery", "account_galleries_upload");

            DataTable galleryTable = db.Query(string.Format("SELECT gallery_access FROM user_galleries WHERE gallery_id = {0} AND user_id = {1}",
                galleryId, loggedInMember.UserId));


            if (galleryTable.Rows.Count == 1)
            {

                ushort galleryAccess = (ushort)galleryTable.Rows[0]["gallery_access"];

                List<string> permissions = new List<string>();
                permissions.Add("Can Read");
                permissions.Add("Can Comment");

                template.ParseVariables("S_GALLERY_LICENSE", ContentLicense.BuildLicenseSelectBox(db, 0));
                template.ParseVariables("S_GALLERY_PERMS", Functions.BuildPermissionsBox(galleryAccess, permissions));
                template.ParseVariables("S_GALLERY_ID", HttpUtility.HtmlEncode(galleryId.ToString()));
                template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
                template.ParseVariables("S_PHOTO_CLASSIFICATION", Classification.BuildClassificationBox(Classifications.Everyone));
            }
            else
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }

        private void RotatePhoto(string submodule)
        {
            if (submodule != "rotate-photo") return;

            AuthoriseRequestSid();

            long photoId = Functions.RequestLong("id", 0);

            if (photoId > 0)
            {
                try
                {
                    UserGalleryItem photo = new UserGalleryItem(core, loggedInMember, photoId);

                    System.Drawing.RotateFlipType rotation = System.Drawing.RotateFlipType.RotateNoneFlipNone;

                    switch (Request.QueryString["rotation"])
                    {
                        case "right":
                        case "90": // right 90
                            rotation = System.Drawing.RotateFlipType.Rotate90FlipNone;
                            break;
                        case "180": // 180
                            rotation = System.Drawing.RotateFlipType.Rotate180FlipNone;
                            break;
                        case "left":
                        case "270": // left 90
                            rotation = System.Drawing.RotateFlipType.Rotate270FlipNone;
                            break;
                    }

                    photo.Rotate(core, rotation);

                    SetRedirectUri(Gallery.BuildPhotoUri(loggedInMember, photo.ParentPath, photo.Path));
                    Display.ShowMessage("Image rotated", "You have successfully rotated the image.");
                    return;
                }
                catch (GalleryItemNotFoundException)
                {
                    Display.ShowMessage("Error", "An error has occured, go back.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }
        }

        private void EditPhoto(string submodule)
        {
            subModules.Add("edit-photo", null);
            if (submodule != "edit-photo") return;

            if (Request.Form["save"] != null)
            {
                SavePhoto();
            }

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

                template.ParseVariables("S_PHOTO_LICENSE", ContentLicense.BuildLicenseSelectBox(db, license));
                template.ParseVariables("S_PHOTO_PERMS", Functions.BuildPermissionsBox(photoAccess, permissions));
                template.ParseVariables("S_PHOTO_TITLE", HttpUtility.HtmlEncode(title));
                template.ParseVariables("S_PHOTO_DESCRIPTION", HttpUtility.HtmlEncode(description));
                template.ParseVariables("S_PHOTO_ID", HttpUtility.HtmlEncode(photoId.ToString()));
                template.ParseVariables("S_PHOTO_CLASSIFICATION", Classification.BuildClassificationBox((Classifications)(byte)photoTable.Rows[0]["gallery_item_classification"]));
            }
            else
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }

        private void MarkPhotoAsGalleryCover(string submodule)
        {
            subModules.Add("gallery-cover", null);
            if (submodule != "gallery-cover") return;

            AuthoriseRequestSid();

            long pictureId = 0;

            try
            {
                pictureId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x06)");
                return;
            }

            // check the image exists
            // check the image is owned by the user trying to set it as their display picture
            DataTable photoTable = db.Query(string.Format("SELECT gallery_item_parent_path, gallery_item_access, user_id FROM gallery_items WHERE user_id = {0} AND gallery_item_id = {1}",
                loggedInMember.UserId, pictureId));

            if (photoTable.Rows.Count == 1)
            {
                ushort galleryItemAccess = (ushort)photoTable.Rows[0]["gallery_item_access"];
                string galleryFullPath = (string)photoTable.Rows[0]["gallery_item_parent_path"];
                int indexOfLastSlash = galleryFullPath.LastIndexOf('/');
                string galleryPath;
                string galleryParentPath;

                if (indexOfLastSlash >= 0)
                {
                    galleryPath = galleryFullPath.Substring(indexOfLastSlash).TrimStart(new char[] { '/' });
                    galleryParentPath = galleryFullPath.Substring(0, indexOfLastSlash).TrimEnd(new char[] { '/' });
                }
                else
                {
                    galleryPath = galleryFullPath;
                    galleryParentPath = "";
                }

                DataTable galleryTable = db.Query(string.Format("SELECT gallery_id, gallery_access FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{2}' AND gallery_path = '{1}';",
                    loggedInMember.UserId, Mysql.Escape(galleryPath), Mysql.Escape(galleryParentPath)));

                /*Response.Write(string.Format("SELECT gallery_id, gallery_access FROM user_galleries WHERE user_id = {0} AND gallery_parent_path = '{2}' AND gallery_path = '{1}';",
                    loggedInMember.UserId, Mysql.Escape(galleryPath), Mysql.Escape(galleryParentPath)));*/

                if (galleryTable.Rows.Count == 1)
                {
                    ushort galleryAccess = (ushort)galleryTable.Rows[0]["gallery_access"];

                    // only worry about view permissions, don't worry about comment permissions
                    if ((galleryItemAccess & galleryAccess & 0x1111) == (galleryAccess & 0x1111))
                    {
                        long galleryId = (long)galleryTable.Rows[0]["gallery_id"];

                        db.UpdateQuery(string.Format("UPDATE user_galleries SET gallery_highlight_id = {0} WHERE user_id = {1} AND gallery_id = {2}",
                            pictureId, loggedInMember.UserId, galleryId));

                        SetRedirectUri(Gallery.BuildGalleryUri(loggedInMember, galleryFullPath));
                        Display.ShowMessage("Gallery Cover Image Changed", "You have successfully changed the cover image of the gallery.");
                        return;
                    }
                    else
                    {
                        Display.ShowMessage("Cannot change gallery cover", "You must use a photo with equal view permissions as the gallery it is the cover of.");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Cannot change gallery cover", "You could not change the gallery cover image to the selected image.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Cannot change gallery cover", "You could not change the gallery cover image to the selected image.");
                return;
            }
        }

        private void MarkPhotoAsDisplayPicture(string submodule)
        {
            subModules.Add("display-pic", null);
            if (submodule != "display-pic") return;

            AuthoriseRequestSid();

            long pictureId = 0;

            try
            {
                pictureId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x07)");
                return;
            }

            // check the image exists
            // check the image is owned by the user trying to set it as their display picture
            DataTable photoTable = db.Query(string.Format("SELECT gallery_item_access, user_id FROM gallery_items WHERE user_id = {0} AND gallery_item_id = {1}",
                loggedInMember.UserId, pictureId));

            if (photoTable.Rows.Count == 1)
            {
                // check for public view permissions on the image
                ushort photoAccess = (ushort)photoTable.Rows[0]["gallery_item_access"];
                if ((photoAccess & 4369) == 4369)
                {


                    db.UpdateQuery(string.Format("UPDATE user_info SET user_icon = {0} WHERE user_id = {1}",
                        pictureId, loggedInMember.UserId));

                    Display.ShowMessage("Display Picture Changed", "You have successfully changed your display picture.");
                    return;
                }
                else
                {
                    Display.ShowMessage("Cannot set as display picture", "You must use a photo with public view permissions as your display picture.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Cannot change display picture", "You could not change your display picture to the selected image.");
                return;
            }
        }

        private void SavePhoto()
        {
            long galleryId = 0;
            long photoId = 0;
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
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x08)");
                return;
            }

            if (Request.Form["mode"] == "edit")
            {
                edit = true;
                try
                {
                    photoId = long.Parse(Request.Form["id"]);
                }
                catch
                {
                    Display.ShowMessage("Invalid submission", "You have made an invalid form submission. (0x09)");
                    return;
                }
            }

            if (!edit)
            {
                try
                {
                    UserGallery parent = new UserGallery(core, loggedInMember, galleryId);

                    string slug = Request.Files["photo-file"].FileName;

                    try
                    {
                        string saveFileName = GalleryItem.HashFileUpload(Request.Files["photo-file"].InputStream);
                        if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                        {
                            TPage.EnsureStoragePathExists(saveFileName);
                            Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                        }

                        UserGalleryItem.Create(core, loggedInMember, parent, title, ref slug, Request.Files["photo-file"].FileName, saveFileName, Request.Files["photo-file"].ContentType, (ulong)Request.Files["photo-file"].ContentLength, description, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());

                        SetRedirectUri(Gallery.BuildPhotoUri(loggedInMember, parent.FullPath, slug));
                        Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                        return;
                    }
                    catch (GalleryItemTooLargeException)
                    {
                        Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                        return;
                    }
                    catch (GalleryQuotaExceededException)
                    {
                        Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                        return;
                    }
                    catch (InvalidGalleryItemTypeException)
                    {
                        Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                        return;
                    }
                    catch (InvalidGalleryFileNameException)
                    {
                        Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                        return;
                    }
                }
                catch (GalleryNotFoundException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                    return;
                }
            }
            else
            {
                // edit

                try
                {
                    UserGalleryItem galleryItem = new UserGalleryItem(core, loggedInMember, photoId);
                    galleryItem.Update(page, title, description, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());

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

        private void PhotoTag(string submodule)
        {
            if (submodule != "tag") return;
        }

        private void PhotoDelete(string submodule)
        {
            if (submodule != "delete") return;

            if (Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                DeletePhotoSave();
                return;
            }
            else if (Display.GetConfirmBoxResult() == ConfirmBoxResult.None)
            {
                AuthoriseRequestSid();

                long photoId = Functions.RequestLong("id", 0);

                if (photoId <= 0)
                {
                    Display.ShowMessage("Cannot Delete Photo", "No photo specified to delete. Please go back and try again.");
                    return;
                }

                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", "galleries");
                hiddenFieldList.Add("sub", "delete");
                hiddenFieldList.Add("id", photoId.ToString());

                Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                        "Confirm photo deletion.",
                        "Are you sure you want to delete this photo?",
                        hiddenFieldList);
            }
        }

        private void DeletePhotoSave()
        {
            AuthoriseRequestSid();

            long photoId = Functions.FormLong("id", 0);

            if (photoId <= 0)
            {
                Display.ShowMessage("Cannot Delete Photo", "No photo specified to delete. Please go back and try again.");
                return;
            }

            try
            {
                UserGalleryItem photo = new UserGalleryItem(core, loggedInMember, photoId);

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
    }
}
