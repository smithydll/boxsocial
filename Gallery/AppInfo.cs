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
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
        }

        public override string Title
        {
            get
            {
                return "Gallery";
            }
        }

        public override string Stub
        {
            get
            {
                return "gallery";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override bool UsesComments
        {
            get
            {
                return true;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return true;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                return Properties.Resources.gallery;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

            core.RegisterCommentHandle("PHOTO", photoCanPostComment, photoCanDeleteComment, photoAdjustCommentCount, photoCommentPosted);
            core.RegisterRatingHandle("PHOTO", photoRated);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddSlug("gallery", @"^/gallery(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Member);
            aii.AddSlug("gallery", @"^/gallery/([A-Za-z0-9\-_\.]+)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddSlug("images", @"^/images/([A-Za-z0-9\-_/\.]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("galleries");

            aii.AddCommentType("PHOTO");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("gallery", "Photo Gallery");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/gallery(|/)$", showGallery, 1);
            core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_/]+)(|/)$", showSubGallery, 2);
            core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)$", showUserPhoto, 3);
            core.RegisterApplicationPage(@"^/gallery/([A-Za-z0-9\-_\.]+)$", showPhoto, 4);

            core.RegisterApplicationPage(@"^/images/([A-Za-z0-9\-_/\.]+)", showImage, 5);
        }

        private void photoRated(ItemRatedEventArgs e)
        {
            /*UpdateQuery uQuery = new UpdateQuery("gallery_items");
            uQuery.se*/
            core.db.BeginTransaction();
            core.db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_rating = (gallery_item_rating * gallery_item_ratings + {0}) / (gallery_item_ratings + 1), gallery_item_ratings = gallery_item_ratings + 1 WHERE gallery_item_id = {1}",
                e.Rating, e.ItemId));
        }

        private void photoCommentPosted(CommentPostedEventArgs e)
        {
        }

        private bool photoCanPostComment(long itemId, User member)
        {
            DataTable galleryItemTable = core.db.Query(string.Format("SELECT {1} FROM gallery_items gi WHERE gi.gallery_item_id = {0};",
                itemId, GalleryItem.GALLERY_ITEM_INFO_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                Primitive owner = null;
                switch ((string)galleryItemTable.Rows[0]["gallery_item_item_type"])
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
                }

                Access photoAccess = new Access(core.db, (ushort)galleryItemTable.Rows[0]["gallery_item_access"], owner);
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

        private bool photoCanDeleteComment(long itemId, User member)
        {
            DataTable galleryItemTable = core.db.Query(string.Format("SELECT {1} FROM gallery_items gi WHERE gi.gallery_item_id = {0};",
                itemId, GalleryItem.GALLERY_ITEM_INFO_FIELDS));

            if (galleryItemTable.Rows.Count == 1)
            {
                switch ((string)galleryItemTable.Rows[0]["gallery_item_item_type"])
                {
                    case "USER":
                        if ((long)galleryItemTable.Rows[0]["gallery_item_item_id"] == member.Id)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case "GROUP":
                        UserGroup group = new UserGroup(core, (long)galleryItemTable.Rows[0]["gallery_item_item_id"]);
                        if (group.IsGroupOperator(member))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case "NETWORK":
                        return false;
                    default:
                        return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        private void photoAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_comments = gallery_item_comments + {1} WHERE gallery_item_id = {0};",
                itemId, adjustment));
        }

        private void showGallery(Core core, object sender)
        {
            if (sender is PPage)
            {
                Gallery.Show(core, (PPage)sender, "");
            }
            else if (sender is GPage)
            {
                Gallery.Show(core, (GPage)sender);
            }
            else if (sender is NPage)
            {
                Gallery.Show(core, (NPage)sender);
            }
        }

        private void showSubGallery(Core core, object sender)
        {
            if (sender is PPage)
            {
                Gallery.Show(core, (PPage)sender, core.PagePathParts[1].Value);
            }
        }

        private void showUserPhoto(Core core, object sender)
        {
            if (sender is PPage)
            {
                GalleryItem.Show(core, (PPage)sender, core.PagePathParts[1].Value, core.PagePathParts[2].Value);
            }
        }

        private void showPhoto(Core core, object sender)
        {
            if (sender is GPage)
            {
                GalleryItem.Show(core, (GPage)sender, core.PagePathParts[1].Value);
                return;
            }
            else if (sender is NPage)
            {
                GalleryItem.Show(core, (NPage)sender, core.PagePathParts[1].Value);
                return;
            }
        }

        private void showImage(Core core, object sender)
        {
            if (sender is PPage)
            {
                GalleryItem.ShowImage(core, (PPage)sender, core.PagePathParts[1].Value);
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

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network;
        }

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

        public void ShowGroupGallery(HookEventArgs e)
        {
            UserGroup thisGroup = (UserGroup)e.Owner;

            if (!(!thisGroup.IsGroupMember(e.core.session.LoggedInMember) && thisGroup.GroupType == "CLOSED"))
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegallery");

                // show recent photographs in the gallery
                GroupGallery gallery = new GroupGallery(e.core, thisGroup);

                List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6);

                template.ParseVariables("PHOTOS", HttpUtility.HtmlEncode(thisGroup.GalleryItems.ToString()));

                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                    galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryItem.ItemTitle));
                    galleryVariableCollection.ParseVariables("PHOTO_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(thisGroup, galleryItem.Path)));

                    string thumbUri = string.Format("/group/{0}/images/_tiny/{1}",
                        thisGroup.Slug, galleryItem.Path);
                    galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(thumbUri));
                }

                template.ParseVariables("U_GROUP_GALLERY", HttpUtility.HtmlEncode(Gallery.BuildGalleryUri(thisGroup)));

                e.core.AddMainPanel(template);
            }
        }

        public void ShowNetworkGallery(HookEventArgs e)
        {
            Network theNetwork = (Network)e.Owner;

            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegallery");

            NetworkGallery gallery = new NetworkGallery(e.core, theNetwork);

            List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6);

            template.ParseVariables("PHOTOS", HttpUtility.HtmlEncode(theNetwork.GalleryItems.ToString()));

            foreach (GalleryItem galleryItem in galleryItems)
            {
                VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                galleryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(galleryItem.ItemTitle));
                galleryVariableCollection.ParseVariables("PHOTO_URI", HttpUtility.HtmlEncode(Gallery.BuildPhotoUri(theNetwork, galleryItem.Path)));

                string thumbUri = string.Format("/network/{0}/images/_tiny/{1}",
                    theNetwork.NetworkNetwork, galleryItem.Path);
                galleryVariableCollection.ParseVariables("THUMBNAIL", HttpUtility.HtmlEncode(thumbUri));
            }

            template.ParseVariables("U_NETWORK_GALLERY", HttpUtility.HtmlEncode(Gallery.BuildGalleryUri(theNetwork)));

            e.core.AddMainPanel(template);
        }
    }
}
