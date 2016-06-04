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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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
        /// Enable cron
        /// </summary>
        public override bool CronEnabled
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Have the cron run every minute
        /// </summary>
        public override int CronFrequency
        {
            get
            {
                return 60;
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
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public override bool ExecuteJob(Job job)
        {
            if (job.ItemId == 0)
            {
                return true;
            }

            switch (job.Function)
            {
                case "notifyBlogComment":
                    BlogEntry.NotifyBlogComment(core, job);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool ExecuteCron()
        {
            // In this cron we will find any queued unpublished blog posts
            // ready to publish and publish them

            SelectQuery query = BlogEntry.GetSelectQueryStub(core, typeof(BlogEntry));
            query.AddCondition("post_status", (byte)PublishStatuses.Queued);
            query.AddCondition("post_published_ut", ConditionEquality.LessThanEqual, UnixTime.UnixTimeStamp());

            DataTable blogPosts = core.Db.Query(query);

            foreach (DataRow row in blogPosts.Rows)
            {
                BlogEntry be = new BlogEntry(core, row);

                core.CreateNewSession(core.PrimitiveCache[be.OwnerId]);

                UpdateQuery uQuery = new UpdateQuery(typeof(Blog));
                uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Addition, 1));
                uQuery.AddField("blog_queued_entries", new QueryOperation("blog_queued_entries", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("user_id", be.OwnerId);

                core.Db.Query(uQuery);

                uQuery = new UpdateQuery(typeof(BlogEntry));
                uQuery.AddField("post_status", (byte)PublishStatuses.Published);
                uQuery.AddCondition("post_id", be.Id);

                core.Db.Query(uQuery);

                core.Search.Index(be);
                core.CallingApplication.PublishToFeed(core, be.Author, be, be.Title);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callName"></param>
        public override void ExecuteCall(string callName)
        {
            switch (callName)
            {
                case "get_posts":
                    long userId = core.Functions.RequestLong("id", core.Session.LoggedInMember.Id);
                    int page = Math.Max(core.Functions.RequestInt("page", 1), 1);
                    int perPage = Math.Max(Math.Min(20, core.Functions.RequestInt("per_page", 10)), 1);

                    try
                    {
                        Blog blog = new Blog(core, userId);

                        List<BlogEntry> blogEntries = blog.GetEntries(string.Empty, string.Empty, 0, 0, 0, page, perPage);

                        core.Response.WriteObject(blogEntries);
                    }
                    catch (InvalidBlogException)
                    {
                    }

                    break;
                case "get_post":
                    long postId = core.Functions.RequestLong("id", 0);

                    if (postId > 0)
                    {
                        BlogEntry entry = new BlogEntry(core, postId);

                        if (entry.Access.Can("VIEW"))
                        {
                            core.Response.WriteObject(entry);
                        }
                    }
                    break;
            }
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
        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                slugs.Add("blog", new PageSlugAttribute("Blog", AppPrimitives.Member));
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
            Template template = GetPostTemplate(e.core, e.Owner);
            if (template != null)
            {
                VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
                javaScriptVariableCollection.Parse("URI", @"/scripts/jquery.sceditor.bbcode.min.js");

                VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");
                styleSheetVariableCollection.Parse("URI", @"/styles/jquery.sceditor.theme.default.min.css");

                core.Template.Parse("OWNER_STUB", e.Owner.UriStubAbsolute);
                e.core.AddPostPanel(e.core.Prose.GetString("BLOG"), template);
            }
        }

        public Template GetPostTemplate(Core core, Primitive owner)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "postblog");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            string formSubmitUri = core.Hyperlink.AppendSid(owner.AccountUriStub, true);
            template.Parse("U_ACCOUNT", formSubmitUri);
            template.Parse("S_ACCOUNT", formSubmitUri);

            template.Parse("USER_DISPLAY_NAME", owner.DisplayName);

            Blog blog = null;

            try
            {
                blog = new Blog(core, (User)owner);
            }
            catch (InvalidBlogException)
            {
                if (owner.ItemKey.Equals(core.LoggedInMemberItemKey))
                {
                    blog = Blog.Create(core);
                }
                else
                {
                    return null;
                }
            }

            /* Title TextBox */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 127;

            /* Post TextBox */
            TextBox postTextBox = new TextBox("post");
            postTextBox.IsFormatted = true;
            postTextBox.Lines = 15;

            /* Tags TextBox */
            TagSelectBox tagsTextBox = new TagSelectBox(core, "tags");
            //tagsTextBox.MaxLength = 127;

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
            System.Data.Common.DbDataReader licensesReader = core.Db.ReaderQuery(ContentLicense.GetSelectQueryStub(core, typeof(ContentLicense)));

            licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
            while(licensesReader.Read())
            {
                ContentLicense li = new ContentLicense(core, licensesReader);
                licensesSelectBox.Add(new SelectBoxItem(li.Id.ToString(), li.Title));
            }

            licensesReader.Close();
            licensesReader.Dispose();

            SelectBox categoriesSelectBox = new SelectBox("category");
            SelectQuery query = Category.GetSelectQueryStub(core, typeof(Category));
            query.AddSort(SortOrder.Ascending, "category_title");

            System.Data.Common.DbDataReader categoriesReader = core.Db.ReaderQuery(query);

            while (categoriesReader.Read())
            {
                Category cat = new Category(core, categoriesReader);
                categoriesSelectBox.Add(new SelectBoxItem(cat.Id.ToString(), cat.Title));
            }

            categoriesReader.Close();
            categoriesReader.Dispose();

            categoriesSelectBox.SelectedKey = 1.ToString();

            /* Parse the form fields */
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_BLOG_TEXT", postTextBox);
            template.Parse("S_TAGS", tagsTextBox);

            template.Parse("S_BLOG_LICENSE", licensesSelectBox);
            template.Parse("S_BLOG_CATEGORY", categoriesSelectBox);

            template.Parse("S_PUBLISH_FEED", publishToFeedCheckBox);

            return template;
        }
    }
}
