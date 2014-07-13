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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Gallery;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule(AppPrimitives.Group, "groups", "cover-photo")]
    public class AccountGroupCoverPhoto : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Group Cover Photo";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountDisplayPic class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGroupCoverPhoto(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGroupCoverPhoto_Load);
            this.Show += new EventHandler(AccountGroupCoverPhoto_Show);
        }

        void AccountGroupCoverPhoto_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupCoverPhoto_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_cover_photo");

            if (Owner is UserGroup)
            {
                UserGroup group = (UserGroup)Owner;

                if (group.GroupInfo.CoverPhotoId > 0)
                {
                    GalleryItem gi = new GalleryItem(core, group.GroupInfo.CoverPhotoId);
                    template.Parse("I_COVER_PHOTO", gi.DisplayUri);

                    Size newSize = GalleryItem.GetSize(new Size(gi.ItemWidth, gi.ItemHeight), new Size(640, 640));

                    double scale = (double)newSize.Width / gi.ItemWidth;
                    int crop = (int)(gi.CropPositionVertical * scale);

                    double scale2 = newSize.Width / 960F;

                    int cropS = (int)(200 * scale2);
                    int cropB = newSize.Height - crop - cropS;

                    template.Parse("CROP_T", crop);
                    template.Parse("CROP_S", cropS);
                    template.Parse("CROP_B", cropB);
                    template.Parse("WIDTH", newSize.Width);
                    template.Parse("HEIGHT", newSize.Height);
                    template.Parse("CROP", gi.CropPositionVertical);
                    template.Parse("SCALE", scale);

                }

                Save(new EventHandler(AccountDisplayPic_Save));
            }
        }

        void AccountDisplayPic_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            if (Owner is UserGroup)
            {
                UserGroup group = (UserGroup)Owner;
                if (core.Http.Files["photo-file"].ContentLength == 0)
                {
                    int vcrop = core.Functions.FormInt("vcrop", 0);

                    if (group.GroupInfo.CoverPhotoId > 0)
                    {
                        GalleryItem coverItem = new GalleryItem(core, group.GroupInfo.CoverPhotoId);
                        coverItem.CropPositionVertical = vcrop;
                        coverItem.Update();

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Cover photo set", "You have successfully uploaded a new cover photo.");
                        return;
                    }
                }
                else
                {
                    string meSlug = "cover-photos";

                    BoxSocial.Applications.Gallery.Gallery profileGallery;
                    try
                    {
                        profileGallery = new BoxSocial.Applications.Gallery.Gallery(core, group, meSlug);
                    }
                    catch (InvalidGalleryException)
                    {
                        BoxSocial.Applications.Gallery.Gallery root = new BoxSocial.Applications.Gallery.Gallery(core, group);
                        profileGallery = BoxSocial.Applications.Gallery.Gallery.Create(core, group, root, "Cover Photos", ref meSlug, "Group cover photos");
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

                            GalleryItem galleryItem = GalleryItem.Create(core, group, profileGallery, title, ref slug, core.Http.Files["photo-file"].FileName, core.Http.Files["photo-file"].ContentType, (ulong)core.Http.Files["photo-file"].ContentLength, description, 0, Classifications.Everyone, stream, false);

                            db.UpdateQuery(string.Format("UPDATE group_info SET group_cover = {0} WHERE group_id = {1}",
                                galleryItem.Id, group.GroupId));

                            //db.CommitTransaction();
                            stream.Close();

                            SetRedirectUri(BuildUri());
                            core.Display.ShowMessage("Cover photo set", "You have successfully uploaded a new cover photo.");
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
    }
}
