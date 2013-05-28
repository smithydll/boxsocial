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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{

    /// <summary>
    /// Application constructor class for the Blog application
    /// </summary>
    public class AppInfo : Application
    {

        /// <summary>
        /// Constructor for the Blog application
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
                return "Blog";
            }
        }

        /// <summary>
        /// Default stub
        /// </summary>
        public override string Stub
        {
            get
            {
                return "blog";
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
                return false;
            }
        }

        /// <summary>
        /// Gets the application icon for the Blog application.
        /// </summary>
        public override System.Drawing.Image Icon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
            if (core == null)
            {
                throw new NullCoreException();
            }

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
            ApplicationInstallationInfo aii = GetInstallInfo();

            aii.AddCommentType("BLOGPOST");

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
                slugs.Add("blog", "Blog");
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

        /// <summary>
        /// Shows the blog
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"blog", AppPrimitives.Member)]
        private void showBlog(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(core, (UPage)sender);
            }
        }

        /// <summary>
        /// Shows blog posts in a category
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"blog/category/([a-z0-9\-]+)", AppPrimitives.Member)]
        private void showBlogCategory(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(core, (UPage)sender, core.PagePathParts[1].Value);
            }
        }

        /// <summary>
        /// Shows blog posts made in a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"blog/([0-9]{4})", AppPrimitives.Member)]
        private void showBlogYear(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(core, (UPage)sender, int.Parse(core.PagePathParts[1].Value));
            }
        }

        /// <summary>
        /// Show the blog posts made in a month
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"blog/([0-9]{4})/([0-9]{1,2})", AppPrimitives.Member)]
        private void showBlogMonth(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(core, (UPage)sender, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        /// <summary>
        /// Show a blog post
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        [Show(@"blog/([0-9]{4})/([0-9]{1,2})/([0-9]+)", AppPrimitives.Member)]
        private void showBlogPost(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(core, (UPage)sender, long.Parse(core.PagePathParts[3].Value), int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        [Show(@"blog/tag/([a-z0-9\-]+)", AppPrimitives.Member)]
        private void showBlogTag(Core core, object sender)
        {
            if (sender is UPage)
            {
                Blog.Show(sender, new ShowBlogEventArgs((UPage)sender, BlogDisplayType.Tag, core.PagePathParts[1].Value));
            }
        }

        /// <summary>
        /// Provides a list of primitives the application supports.
        /// </summary>
        /// <returns>List of primitives given support of</returns>
        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member;
        }

        /// <summary>
        /// Hook interface for any application hooks provided by a page.
        /// </summary>
        /// <param name="eventArgs">An EventArgs that contains the event data</param>
        void core_PageHooks(HookEventArgs eventArgs)
        {

        }
    }
}
