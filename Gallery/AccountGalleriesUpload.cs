﻿/*
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Configuration;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// Account sub module for uploading photos.
    /// </summary>
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "galleries", "upload")]
    public class AccountGalleriesUpload : AccountSubModule
    {

        /// <summary>
        /// Sub module title.
        /// </summary>
        public override string Title
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Sub module order.
        /// </summary>
        public override int Order
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountGalleriesUpload class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGalleriesUpload(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountGalleriesUpload_Load);
            this.Show += new EventHandler(AccountGalleriesUpload_Show);
        }

        /// <summary>
        /// Load procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Default show procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Show(object sender, EventArgs e)
        {
            SetTemplate("account_galleries_upload");

            long galleryId = core.Functions.RequestLong("gallery-id", 0);

            if (galleryId == 0)
            {
                // Invalid gallery
                DisplayGenericError();
                return;
            }

            try
            {
                Gallery gallery = new Gallery(core, Owner, galleryId);

                CheckBox publishToFeedCheckBox = new CheckBox("publish-feed");
                publishToFeedCheckBox.IsChecked = true;

                CheckBox highQualityCheckBox = new CheckBox("high-quality");
                highQualityCheckBox.IsChecked = false;
                
                core.Display.ParseLicensingBox(template, "S_GALLERY_LICENSE", 0);

                template.Parse("S_PUBLISH_FEED", publishToFeedCheckBox);
                template.Parse("S_HIGH_QUALITY", highQualityCheckBox);
                template.Parse("S_GALLERY_ID", galleryId.ToString());

                core.Display.ParseClassification(template, "S_PHOTO_CLASSIFICATION", Classifications.Everyone);
            }
            catch (InvalidGalleryException)
            {
                DisplayGenericError();
                return;
            }

            Save(new EventHandler(AccountGalleriesUpload_Save));
        }

        /// <summary>
        /// Save procedure for uploading photos.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountGalleriesUpload_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long galleryId = core.Functions.FormLong("gallery-id", 0);
            string title = core.Http.Form["title"];
            string galleryTitle = core.Http.Form["gallery-title"];
            string description = core.Http.Form["description"];
            bool publishToFeed = (core.Http.Form["publish-feed"] != null);
            bool highQualitySave = (core.Http.Form["high-quality"] != null);

            if (string.IsNullOrEmpty(galleryTitle))
            {
                galleryTitle = "Uploaded " + core.Tz.Now.ToString("MMMM dd, yyyy");
            }

            bool newGallery = core.Http.Form["album"] == "create";

            int filesUploaded = 0;
            for (int i = 0; i < core.Http.Files.Count; i++)
            {
                //HttpContext.Current.Response.Write(core.Http.Files.GetKey(i));
                //HttpContext.Current.Response.Write(core.Http.Files.Get(i).FileName);
                if (core.Http.Files.GetKey(i).StartsWith("photo-file"))
                {
                    filesUploaded++;
                    if (core.Http.Files[i] == null || core.Http.Files[i].ContentLength == 0)
                    {
                        core.Display.ShowMessage("No files selected", "You need to select some files to upload");
                    }
                }
            }

            if (filesUploaded == 0)
            {
                core.Display.ShowMessage("No files selected", "You need to select some files to upload");
                return;
            }

            try
            {
                Gallery parent = null;

                if (newGallery)
                {
                    Gallery grandParent = new Gallery(core, Owner);

                    string gallerySlug = string.Empty;

                    try
                    {
                        parent = Gallery.Create(core, LoggedInMember, grandParent, galleryTitle, ref gallerySlug, string.Empty);
                    }
                    catch (GallerySlugNotUniqueException)
                    {
                        core.Display.ShowMessage("Gallery not unique", "Please give a different name to the gallery");
                    }

                    AccessControlLists acl = new AccessControlLists(core, parent);
                    acl.SaveNewItemPermissions();
                }
                else
                {
                    parent = new Gallery(core, Owner, galleryId);
                }

                string slug = string.Empty;
                try
                {
                    for (int i = 0; i < core.Http.Files.Count; i++)
                    {
                        if (!core.Http.Files.GetKey(i).StartsWith("photo-file"))
                        {
                            continue;
                        }

                        slug = core.Http.Files[i].FileName;

                        MemoryStream stream = new MemoryStream();
                        core.Http.Files[i].InputStream.CopyTo(stream);

                        db.BeginTransaction();

                        GalleryItem newGalleryItem = GalleryItem.Create(core, Owner, parent, title, ref slug, core.Http.Files[i].FileName, core.Http.Files[i].ContentType, (ulong)core.Http.Files[i].ContentLength, description, core.Functions.GetLicenseId(), core.Functions.GetClassification(), stream, highQualitySave /*, width, height*/);
                        stream.Close();

                        if (publishToFeed && i < 3)
                        {
                            core.CallingApplication.PublishToFeed(core, LoggedInMember, parent, newGalleryItem.ItemKey, Functions.SingleLine(core.Bbcode.Flatten(newGalleryItem.ItemAbstract)));
                        }
                    }

                    //db.CommitTransaction();

                    if (core.IsAjax)
                    {
                        Dictionary<string, string> returnValues = new Dictionary<string, string>();

                        returnValues.Add("update", "true");
                        returnValues.Add("message", description);
                        returnValues.Add("template", string.Empty);

                        core.Ajax.SendDictionary("statusPosted", returnValues);
                    }
                    else
                    {
                        if (filesUploaded == 1)
                        {
                            SetRedirectUri(Gallery.BuildPhotoUri(core, Owner, parent.FullPath, slug));
                        }
                        else
                        {
                            SetRedirectUri(parent.Uri);
                        }
                        core.Display.ShowMessage("Photo Uploaded", "You have successfully uploaded a photo.");
                    }

                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to " + Functions.BytesToString(core.Settings.MaxFileSize) + " in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            catch (InvalidGalleryException)
            {
                db.RollBackTransaction();
                core.Display.ShowMessage("Submission failed", "Submission failed, Invalid Gallery.");
                return;

            }
        }
    }
}
