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
    public class AccountGalleriesUpload : AccountSubModule, IPermissibleControlPanelSubModule
    {
        private Gallery gallery;

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
        public AccountGalleriesUpload(Core core, Primitive owner)
            : base(core, owner)
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
            bool submittedTitle = true;

            if (string.IsNullOrEmpty(galleryTitle))
            {
                submittedTitle = false;
                galleryTitle = "Uploaded " + core.Tz.Now.ToString("MMMM dd, yyyy");
            }

            bool newGallery = core.Http.Form["album"] == "create";

            int filesUploaded = 0;
            for (int i = 0; i < core.Http.Files.Count; i++)
            {
                if (core.Http.Files.GetKey(i).StartsWith("photo-file", StringComparison.Ordinal))
                {
                    filesUploaded++;
                    if (core.Http.Files[i] == null || core.Http.Files[i].ContentLength == 0)
                    {
                        core.Response.ShowMessage("error", "No files selected", "You need to select some files to upload");
                    }
                }
            }

            if (filesUploaded == 0)
            {
                core.Response.ShowMessage("error", "No files selected", "You need to select some files to upload");
                return;
            }

            try
            {
                Gallery parent = null;

                if (newGallery)
                {
                    Gallery grandParent = null;

                    if (!submittedTitle)
                    {
                        string grandParentSlug = "photos-from-posts";
                        try
                        {
                            grandParent = new Gallery(core, Owner, grandParentSlug);
                        }
                        catch (InvalidGalleryException)
                        {
                            Gallery root = new Gallery(core, Owner);
                            grandParent = Gallery.Create(core, Owner, root, "Photos From Posts", ref grandParentSlug, "All my unsorted uploads");
                        }
                    }
                    else
                    {
                        grandParent = new Gallery(core, Owner);
                    }

                    string gallerySlug = string.Empty;

                    if (!submittedTitle)
                    {
                        gallerySlug = "photos-" + UnixTime.UnixTimeStamp().ToString();
                    }
                    else
                    {
                        gallerySlug = Gallery.GetSlugFromTitle(galleryTitle, "");
                    }

                    try
                    {
                        parent = Gallery.Create(core, Owner, grandParent, galleryTitle, ref gallerySlug, string.Empty);
                    }
                    catch (GallerySlugNotUniqueException)
                    {
                         core.Response.ShowMessage("error", "Gallery not unique", "Please give a different name to the gallery");
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
                        if (!core.Http.Files.GetKey(i).StartsWith("photo-file", StringComparison.Ordinal))
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
                            core.CallingApplication.PublishToFeed(core, LoggedInMember, parent, newGalleryItem, Functions.SingleLine(core.Bbcode.Flatten(newGalleryItem.ItemAbstract)));
                        }
                    }

                    //db.CommitTransaction();

                    if (core.ResponseFormat == ResponseFormats.Xml)
                    {
                        long newestId = core.Functions.FormLong("newest-id", 0);
                        long newerId = 0;

                        List<BoxSocial.Internals.Action> feedActions = Feed.GetNewerItems(core, LoggedInMember, newestId);

                        Template template = new Template("pane.feeditem.html");
                        template.Medium = core.Template.Medium;
                        template.SetProse(core.Prose);

                        foreach (BoxSocial.Internals.Action feedAction in feedActions)
                        {
                            VariableCollection feedItemVariableCollection = template.CreateChild("feed_days_list.feed_item");

                            if (feedAction.Id > newerId)
                            {
                                newerId = feedAction.Id;
                            }

                            core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.FormattedTitle);
                            core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

                            feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                            feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                            feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                            if (feedAction.ActionItemKey.GetType(core).Likeable)
                            {
                                feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                                if (feedAction.Info.Likes > 0)
                                {
                                    feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                                    feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                                }
                            }

                            if (feedAction.ActionItemKey.GetType(core).Commentable)
                            {
                                feedItemVariableCollection.Parse("COMMENTABLE", "TRUE");

                                if (feedAction.Info.Comments > 0)
                                {
                                    feedItemVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", feedAction.Info.Comments));
                                }
                            }

                            //Access access = new Access(core, feedAction.ActionItemKey, true);
                            if (feedAction.PermissiveParent.Access.IsPublic())
                            {
                                feedItemVariableCollection.Parse("IS_PUBLIC", "TRUE");
                                if (feedAction.ActionItemKey.GetType(core).Shareable)
                                {
                                    feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                                    //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                                    if (feedAction.Info.SharedTimes > 0)
                                    {
                                        feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                                    }
                                }
                            }

                            if (feedAction.Owner is User)
                            {
                                feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).Tile);
                                feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).Icon);
                            }
                        }


                        // Check for new messages and upload
                        Dictionary<string, string> returnValues = new Dictionary<string, string>();

                        returnValues.Add("update", "true");
                        returnValues.Add("message", description);
                        returnValues.Add("template", template.ToString());
                        returnValues.Add("newest-id", newerId.ToString());

                        core.Response.SendDictionary("statusPosted", returnValues);
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
                    core.Response.ShowMessage("error", "Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to " + Functions.BytesToString(core.Settings.MaxFileSize) + " in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    db.RollBackTransaction();
                    core.Response.ShowMessage("error", "Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    db.RollBackTransaction();
                    core.Response.ShowMessage("error", "Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    db.RollBackTransaction();
                    core.Response.ShowMessage("error", "Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            catch (InvalidGalleryException)
            {
                db.RollBackTransaction();
                core.Response.ShowMessage("error", "Submission failed", "Submission failed, Invalid Gallery.");
                return;

            }
        }

        public Access Access
        {
            get
            {
                if (gallery == null)
                {
                    gallery = new Gallery(core, core.Functions.RequestLong("gallery-id", 0));
                }

                return gallery.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "CREATE_ITEMS";
            }
        }
    }
}
