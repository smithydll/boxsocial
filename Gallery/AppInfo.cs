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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Gallery
{

    /// <summary>
    /// Application constructor class for the Gallery application
    /// </summary>
    public class AppInfo : Application
    {

        /// <summary>
        /// Constructor for the Gallery application
        /// </summary>
        /// <param name="core"></param>
        public AppInfo(Core core)
            : base(core)
        {
        }

        /// <summary>
        /// Application title
        /// </summary>
        public override string Title
        {
            get
            {
                return "Gallery";
            }
        }

        /// <summary>
        /// Default stub
        /// </summary>
        public override string Stub
        {
            get
            {
                return "gallery";
            }
        }

        /// <summary>
        /// A description of the application
        /// </summary>
        public override string Description
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Gets a value indicating if the application implements a comment
        /// handler.
        /// </summary>
        public override bool UsesComments
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating if the application implements a ratings
        /// handler.
        /// </summary>
        public override bool UsesRatings
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the application icon for the Gallery application.
        /// </summary>
        public override System.Drawing.Image Icon
        {
            get
            {
                return Properties.Resources.gallery;
            }
        }

        /// <summary>
        /// Gets the application stylesheet for the Blog application.
        /// </summary>
        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        /// <summary>
        /// Gets the application javascript for the Blog application.
        /// </summary>
        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        /// <summary>
        /// Initialises the application
        /// </summary>
        /// <param name="core">Core token</param>
        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
			
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(GalleryItem)), photoCanPostComment, photoCanDeleteComment, photoAdjustCommentCount, photoCommentPosted);
            //core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(UserGalleryItem)), photoCanPostComment, photoCanDeleteComment, photoAdjustCommentCount, photoCommentPosted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(GroupGalleryItem)), photoCanPostComment, photoCanDeleteComment, photoAdjustCommentCount, photoCommentPosted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(NetworkGalleryItem)), photoCanPostComment, photoCanDeleteComment, photoAdjustCommentCount, photoCommentPosted);

            core.RegisterRatingHandle(ItemKey.GetTypeId(typeof(GalleryItem)), photoRated);
            //core.RegisterRatingHandle(ItemKey.GetTypeId(typeof(UserGalleryItem)), photoRated);
            core.RegisterRatingHandle(ItemKey.GetTypeId(typeof(GroupGalleryItem)), photoRated);
            core.RegisterRatingHandle(ItemKey.GetTypeId(typeof(NetworkGalleryItem)), photoRated);
        }

        /// <summary>
        /// Builds installation info for the application.
        /// </summary>
        /// <returns>Installation information for the application</returns>
        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddSlug("gallery", @"^/gallery(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);

            aii.AddSlug("images", @"^/images/([A-Za-z0-9\-_/\.]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("galleries");

            aii.AddCommentType("PHOTO");

            return aii;
        }

        /// <summary>
        /// Builds a list of page slugs stubs the application handles.
        /// </summary>
        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("gallery", "Photo Gallery");
                return slugs;
            }
        }

        /// <summary>
        /// Handles the application load event.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that caused the application to load</param>
        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            //core.RegisterApplicationPage(@"^/gallery(|/)$", showGallery, 1);
            //core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_/]+)(|/)$", showSubGallery, 2);
            //core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)$", showUserPhoto, 3);
            //core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_\.]+)$", showPhoto, 4);

            //core.RegisterApplicationPage(@"^/images/([A-Za-z0-9\-_/\.]+)", showImage, 5);
        }

        /// <summary>
        /// Callback on a gallery item being rated.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data</param>
        private void photoRated(ItemRatedEventArgs e)
        {
            /*UpdateQuery uQuery = new UpdateQuery("gallery_items");
            uQuery.se*/
            core.db.BeginTransaction();
            core.db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_rating = (gallery_item_rating * gallery_item_ratings + {0}) / (gallery_item_ratings + 1), gallery_item_ratings = gallery_item_ratings + 1 WHERE gallery_item_id = {1}",
                e.Rating, e.ItemId));
        }

        /// <summary>
        /// Callback on a comment being posted to a gallery item.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data</param>
        private void photoCommentPosted(CommentPostedEventArgs e)
        {
        }

        /// <summary>
        /// Determines if a user can post a comment to a gallery item.
        /// </summary>
        /// <param name="itemId">Gallery item id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can post a comment, false otherwise</returns>
        private bool photoCanPostComment(ItemKey itemKey, User member)
        {
            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem), false);
            query.AddCondition("gallery_item_id", itemKey.Id);

            DataTable galleryItemTable = core.db.Query(query);

            if (galleryItemTable.Rows.Count == 1)
            {
                ItemKey ik = new ItemKey((long)galleryItemTable.Rows[0]["gallery_item_item_Id"], (long)galleryItemTable.Rows[0]["gallery_item_item_type_id"]);

                core.UserProfiles.LoadPrimitiveProfile(ik);
                Primitive owner = core.UserProfiles[ik];

                /*switch ((string)galleryItemTable.Rows[0]["gallery_item_item_type"])
                {
                    case "USER":
                        owner = new User(core, (long)galleryItemTable.Rows[0]["gallery_item_item_id"]);
                        break;
                    case "GROUP":
                        owner = new UserGroup(core, (long)galleryItemTable.Rows[0]["gallery_item_item_id"]);
                        break;
                    case "NETWORK":
                        owner = new Network(core, (long)galleryItemTable.Rows[0]["gallery_item_item_id"]);
                        break;
                }*/

                Access photoAccess = new Access(core, (ushort)galleryItemTable.Rows[0]["gallery_item_access"], owner);
                photoAccess.SetViewer(member);

                if (photoAccess.CanComment)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        /// <summary>
        /// Determines if a user can delete a comment from a gallery item
        /// </summary>
        /// <param name="itemId">Gallery item id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can delete a comment, false otherwise</returns>
        private bool photoCanDeleteComment(ItemKey itemKey, User member)
        {
            SelectQuery query = GalleryItem.GetSelectQueryStub(typeof(GalleryItem), false);
            query.AddCondition("gallery_item_id", itemKey.Id);

            DataTable galleryItemTable = core.db.Query(query);

            if (galleryItemTable.Rows.Count == 1)
            {
                long itid = (long)galleryItemTable.Rows[0]["gallery_item_item_type_id"];

                if (itid == ItemKey.GetTypeId(typeof(User)))
                {
                        if ((long)galleryItemTable.Rows[0]["gallery_item_item_id"] == member.Id)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                }
                else if (itid == ItemKey.GetTypeId(typeof(UserGroup)))
                {
                    UserGroup group = new UserGroup(core, (long)galleryItemTable.Rows[0]["gallery_item_item_id"]);
                    if (group.IsGroupOperator(member))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        /// <summary>
        /// Adjusts the comment count for the gallery item.
        /// </summary>
        /// <param name="itemId">Gallery item id</param>
        /// <param name="adjustment">Amount to adjust the comment count by</param>
        private void photoAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_comments = gallery_item_comments + {1} WHERE gallery_item_id = {0};",
                itemKey.Id, adjustment));
        }
        
        [Show(@"^/gallery(|/)$", AppPrimitives.Member)]
        private void showRootGallery(Core core, object sender)
        {
            if (sender is PPage)
            {
                Gallery.Show(sender, new ShowPPageEventArgs((PPage)sender, ""));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        /// <summary>
        /// Show a gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"^/gallery/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member)]
        private void showGallery(Core core, object sender)
        {
            if (sender is PPage)
            {
                Gallery.Show(sender, new ShowPPageEventArgs((PPage)sender, core.PagePathParts[1].Value));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
            /*if (sender is UPage)
            {
                Gallery.Show(core, (UPage)sender, "");
            }
            else if (sender is GPage)
            {
                Gallery.Show(core, (GPage)sender);
            }
            else if (sender is NPage)
            {
                Gallery.Show(core, (NPage)sender);
            }*/
        }

        /// <summary>
        /// Show a sub gallery
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showSubGallery(Core core, object sender)
        {
            if (sender is UPage)
            {
                Gallery.Show(core, (UPage)sender, core.PagePathParts[1].Value);
            }
        }

        /// <summary>
        /// Show a user's gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        /*private void showUserPhoto(Core core, object sender)
        {
            if (sender is UPage)
            {
                GalleryItem.Show(core, (UPage)sender, core.PagePathParts[1].Value, core.PagePathParts[2].Value);
            }
        }*/

        /// <summary>
        /// Show a primitive's gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"^/gallery/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician | AppPrimitives.Member)]
        private void showPhoto(Core core, object sender)
        {
            if (sender is PPage)
            {
                GalleryItem.Show(sender, new ShowPPageEventArgs((PPage)sender, core.PagePathParts[1].Value));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
            /*if (sender is GPage)
            {
                GalleryItem.Show(core, (GPage)sender, core.PagePathParts[1].Value);
                return;
            }
            else if (sender is NPage)
            {
                GalleryItem.Show(core, (NPage)sender, core.PagePathParts[1].Value);
                return;
            }*/
        }
        
        [Show(@"^/gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician | AppPrimitives.Member)]
        private void showGalleryPhoto(Core core, object sender)
        {
            if (sender is PPage)
            {
                GalleryItem.Show(sender, new ShowPPageEventArgs((PPage)sender, core.PagePathParts[1].Value + "/" + core.PagePathParts[2].Value));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        /// <summary>
        /// Show an image file
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"^/images/([A-Za-z0-9\-_/\.]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician)]
        private void showImage(Core core, object sender)
        {
            if (sender is UPage)
            {
                GalleryItem.ShowImage(core, (UPage)sender, core.PagePathParts[1].Value);
            }
            else if (sender is GPage)
            {
                GalleryItem.ShowImage(core, (GPage)sender, core.PagePathParts[1].Value);
                return;
            }
            else if (sender is NPage)
            {
                GalleryItem.ShowImage(core, (NPage)sender, core.PagePathParts[1].Value);
                return;
            }
        }

        /// <summary>
        /// Provides a list of primitives the application supports.
        /// </summary>
        /// <returns>List of primitives given support of</returns>
        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network;
        }

        /// <summary>
        /// Hook interface for any application hooks provided by a page.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data</param>
        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                ShowGroupGallery(e);
            }
            if (e.PageType == AppPrimitives.Network)
            {
                ShowNetworkGallery(e);
            }
        }

        /// <summary>
        /// Hook showing gallery items on a group profile.
        /// </summary>
        /// <param name="e">Hook event arguments</param>
        public void ShowGroupGallery(HookEventArgs e)
        {
            UserGroup thisGroup = (UserGroup)e.Owner;

            if (!(!thisGroup.IsGroupMember(e.core.session.LoggedInMember) && thisGroup.GroupType == "CLOSED"))
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegallery");

                // show recent photographs in the gallery
                GroupGallery gallery = new GroupGallery(e.core, thisGroup);

                List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6);

                template.Parse("PHOTOS", thisGroup.GalleryItems.ToString());

                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                    galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                    galleryVariableCollection.Parse("PHOTO_URI", galleryItem.Uri);

                    galleryVariableCollection.Parse("THUMBNAIL", galleryItem.ThumbUri);
                }

                template.Parse("U_GROUP_GALLERY", Gallery.BuildGalleryUri(core, thisGroup));

                e.core.AddMainPanel(template);
            }
        }

        /// <summary>
        /// Hook showing gallery items on a network profile.
        /// </summary>
        /// <param name="e">Hook event arguments</param>
        public void ShowNetworkGallery(HookEventArgs e)
        {
            Network theNetwork = (Network)e.Owner;

            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegallery");

            NetworkGallery gallery = new NetworkGallery(e.core, theNetwork);

            List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6);

            template.Parse("PHOTOS", theNetwork.GalleryItems.ToString());

            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                galleryVariableCollection.Parse("PHOTO_URI", Gallery.BuildPhotoUri(core, theNetwork, galleryItem.Path));

                string thumbUri = string.Format("/network/{0}/images/_tiny/{1}",
                    theNetwork.NetworkNetwork, galleryItem.Path);
                galleryVariableCollection.Parse("THUMBNAIL", thumbUri);
            }

            template.Parse("U_NETWORK_GALLERY", Gallery.BuildGalleryUri(core, theNetwork));

            e.core.AddMainPanel(template);
        }
    }
}
