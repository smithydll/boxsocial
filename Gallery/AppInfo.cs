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
                return Properties.Resources.icon;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
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
        }

        /// <summary>
        /// Builds installation info for the application.
        /// </summary>
        /// <returns>Installation information for the application</returns>
        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Group | AppPrimitives.Network);

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
        }

        [Show(@"^/gallery(|/)$", AppPrimitives.Member | AppPrimitives.Musician | AppPrimitives.Group | AppPrimitives.Network)]
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
        [Show(@"gallery/([A-Za-z0-9\-_/]+)", AppPrimitives.Member | AppPrimitives.Musician | AppPrimitives.Group | AppPrimitives.Network)]
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
        }

        /// <summary>
        /// Show a primitive's gallery item
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"gallery/([A-Za-z0-9\-_\.]+)", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician | AppPrimitives.Member)]
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
        }
        
        [Show(@"gallery/([A-Za-z0-9\-_/]+)/([A-Za-z0-9\-_\.]+)", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician | AppPrimitives.Member)]
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
        [Show(@"images/([A-Za-z0-9\-_/\.@]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician)]
        private void showImage(Core core, object sender)
        {
            if (sender is PPage)
            {
                GalleryItem.ShowImage(sender, new ShowPPageEventArgs((PPage)sender, core.PagePathParts[1].Value));
            }
        }

        /// <summary>
        /// Provides a list of primitives the application supports.
        /// </summary>
        /// <returns>List of primitives given support of</returns>
        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Musician;
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

            if (!(!thisGroup.IsGroupMember(e.core.LoggedInMemberItemKey) && thisGroup.GroupType == "CLOSED"))
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegallery");
                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                // show recent photographs in the gallery
                Gallery gallery = new Gallery(e.core, thisGroup);

                List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6, 0);

                template.Parse("PHOTOS", thisGroup.GalleryItems.ToString());

                foreach (GalleryItem galleryItem in galleryItems)
                {
                    VariableCollection galleryVariableCollection = template.CreateChild("photo_list");

                    galleryVariableCollection.Parse("TITLE", galleryItem.ItemTitle);
                    galleryVariableCollection.Parse("PHOTO_URI", galleryItem.Uri);

                    galleryVariableCollection.Parse("THUMBNAIL", galleryItem.ThumbnailUri);
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
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            Gallery gallery = new Gallery(e.core, theNetwork);

            List<GalleryItem> galleryItems = gallery.GetItems(e.core, 1, 6, 0);

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
