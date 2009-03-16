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
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
            }
        }

        /// <summary>
        /// Gets the application stylesheet for the Blog application.
        /// </summary>
        public override string StyleSheet
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the application javascript for the Blog application.
        /// </summary>
        public override string JavaScript
        {
            get
            {
                return null;
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
			
			ItemType itemType = new ItemType(core, typeof(BlogEntry).FullName);

            core.RegisterCommentHandle(itemType.TypeId, blogCanPostComment, blogCanDeleteComment, blogAdjustCommentCount, blogCommentPosted);
        }

        /// <summary>
        /// Builds installation info for the application.
        /// </summary>
        /// <returns>Installation information for the application</returns>
        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("blog", @"^/blog(|/)$", AppPrimitives.Member);
            aii.AddSlug("blog", @"^/blog/category/([a-z0-9\-]+)(|/)$", AppPrimitives.Member);
            aii.AddSlug("blog", @"^/blog/([0-9]{4})(|/)$", AppPrimitives.Member);
            aii.AddSlug("blog", @"^/blog/([0-9]{4})/([0-9]{1,2})(|/)$", AppPrimitives.Member);
            aii.AddSlug("blog", @"^/blog/([0-9]{4})/([0-9]{1,2})/([0-9]+)(|/)$", AppPrimitives.Member);

            aii.AddModule("blog");

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

            core.RegisterApplicationPage(@"^/blog(|/)$", showBlog, 1);
            core.RegisterApplicationPage(@"^/blog/category/([a-z0-9\-]+)(|/)$", showBlogCategory, 2);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})(|/)$", showBlogYear, 3);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})/([0-9]{1,2})(|/)$", showBlogMonth, 4);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})/([0-9]{1,2})/([0-9]+)(|/)$", showBlogPost, 5);
        }

        /// <summary>
        /// Callback on a comment being posted to the blog.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data</param>
        private void blogCommentPosted(CommentPostedEventArgs e)
        {
            // Notify of a new comment
            BlogEntry blogEntry = new BlogEntry(core, e.ItemId);
            User owner = (User)blogEntry.Owner;

            ApplicationEntry ae = new ApplicationEntry(core, owner, "Blog");

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_blog_notification");
            notificationTemplate.Parse("U_PROFILE", e.Comment.BuildUri(blogEntry));
            notificationTemplate.Parse("POSTER", e.Poster.DisplayName);
            notificationTemplate.Parse("COMMENT", Functions.TrimStringToWord(e.Comment.Body, Notification.NOTIFICATION_MAX_BODY));

            ae.SendNotification(owner, string.Format("[user]{0}[/user] commented on your blog.", e.Poster.Id), notificationTemplate.ToString());
        }

        /// <summary>
        /// Determines if a user can post a comment to a blog post.
        /// </summary>
        /// <param name="itemId">Blog post id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can post a comment, false otherwise</returns>
        private bool blogCanPostComment(ItemKey itemKey, User member)
        {
            BlogEntry blogEntry = new BlogEntry(core, itemKey.Id);
            blogEntry.BlogEntryAccess.SetViewer(member);

            if (blogEntry.BlogEntryAccess.CanComment)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a user can delete a comment from a blog post
        /// </summary>
        /// <param name="itemId">Blog post id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can delete a comment, false otherwise</returns>
        private bool blogCanDeleteComment(ItemKey itemKey, User member)
        {
            BlogEntry blogEntry = new BlogEntry(core, itemKey.Id);

            if (blogEntry.OwnerId == member.UserId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adjusts the comment count for the blog post.
        /// </summary>
        /// <param name="itemId">Blog post id</param>
        /// <param name="adjustment">Amount to adjust the comment count by</param>
        private void blogAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE blog_postings SET post_comments = post_comments + {1} WHERE post_id = {0};",
                itemKey.Id, adjustment));
        }

        /// <summary>
        /// Shows the blog
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showBlog(Core core, object sender)
        {
            if (sender.GetType() == typeof(PPage))
            {
                Blog.Show(core, (PPage)sender);
            }
        }

        /// <summary>
        /// Shows blog posts in a category
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showBlogCategory(Core core, object sender)
        {
            if (sender.GetType() == typeof(PPage))
            {
                Blog.Show(core, (PPage)sender, core.PagePathParts[1].Value);
            }
        }

        /// <summary>
        /// Shows blog posts made in a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showBlogYear(Core core, object sender)
        {
            if (sender.GetType() == typeof(PPage))
            {
                Blog.Show(core, (PPage)sender, int.Parse(core.PagePathParts[1].Value));
            }
        }

        /// <summary>
        /// Show the blog posts made in a month
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showBlogMonth(Core core, object sender)
        {
            if (sender.GetType() == typeof(PPage))
            {
                Blog.Show(core, (PPage)sender, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        /// <summary>
        /// Show a blog post
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="sender">Object that called the page</param>
        private void showBlogPost(Core core, object sender)
        {
            if (sender.GetType() == typeof(PPage))
            {
                Blog.Show(core, (PPage)sender, long.Parse(core.PagePathParts[3].Value), int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
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
