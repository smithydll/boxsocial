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
using BoxSocial.Forms;
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
            core.PostHooks += new Core.HookHandler(core_PostHooks);
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

        void core_PostHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                PostContent(e);
            }
        }

        void PostContent(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "postblog");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            string formSubmitUri = core.Hyperlink.AppendSid(e.Owner.AccountUriStub, true);
            template.Parse("U_ACCOUNT", formSubmitUri);
            template.Parse("S_ACCOUNT", formSubmitUri);

            template.Parse("USER_DISPLAY_NAME", e.Owner.DisplayName);

            Blog blog = new Blog(core, (User)e.Owner);

            /* Title TextBox */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 127;

            /* Post TextBox */
            TextBox postTextBox = new TextBox("post");
            postTextBox.IsFormatted = true;
            postTextBox.Lines = 15;

            /* Tags TextBox */
            TextBox tagsTextBox = new TextBox("tags");
            tagsTextBox.MaxLength = 127;

            CheckBox publishToFeedCheckBox = new CheckBox("publish-feed");
            publishToFeedCheckBox.IsChecked = true;

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", blog.ItemKey);
            HiddenField aclModeField = new HiddenField("aclmode");
            aclModeField.Value = "simple";

            template.Parse("S_PERMISSIONS", permissionSelectBox);
            template.Parse("S_ACLMODE", aclModeField);

            DateTime postTime = DateTime.Now;

            SelectBox postYearsSelectBox = new SelectBox("post-year");
            for (int i = DateTime.Now.AddYears(-7).Year; i <= DateTime.Now.Year; i++)
            {
                postYearsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            postYearsSelectBox.SelectedKey = postTime.Year.ToString();

            SelectBox postMonthsSelectBox = new SelectBox("post-month");
            for (int i = 1; i < 13; i++)
            {
                postMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
            }

            postMonthsSelectBox.SelectedKey = postTime.Month.ToString();

            SelectBox postDaysSelectBox = new SelectBox("post-day");
            for (int i = 1; i < 32; i++)
            {
                postDaysSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            postDaysSelectBox.SelectedKey = postTime.Day.ToString();

            template.Parse("S_POST_YEAR", postYearsSelectBox);
            template.Parse("S_POST_MONTH", postMonthsSelectBox);
            template.Parse("S_POST_DAY", postDaysSelectBox);
            template.Parse("S_POST_HOUR", postTime.Hour.ToString());
            template.Parse("S_POST_MINUTE", postTime.Minute.ToString());

            SelectBox licensesSelectBox = new SelectBox("license");
            DataTable licensesTable = core.Db.Query(ContentLicense.GetSelectQueryStub(typeof(ContentLicense)));

            licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                ContentLicense li = new ContentLicense(core, licenseRow);
                licensesSelectBox.Add(new SelectBoxItem(li.Id.ToString(), li.Title));
            }

            SelectBox categoriesSelectBox = new SelectBox("category");
            SelectQuery query = Category.GetSelectQueryStub(typeof(Category));
            query.AddSort(SortOrder.Ascending, "category_title");

            DataTable categoriesTable = core.Db.Query(query);

            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                Category cat = new Category(core, categoryRow);
                categoriesSelectBox.Add(new SelectBoxItem(cat.Id.ToString(), cat.Title));
            }

            categoriesSelectBox.SelectedKey = 1.ToString();

            /* Parse the form fields */
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_BLOG_TEXT", postTextBox);
            template.Parse("S_TAGS", tagsTextBox);

            template.Parse("S_BLOG_LICENSE", licensesSelectBox);
            template.Parse("S_BLOG_CATEGORY", categoriesSelectBox);

            template.Parse("S_PUBLISH_FEED", publishToFeedCheckBox);

            e.core.AddPostPanel("Blog", template);
        }
    }
}
