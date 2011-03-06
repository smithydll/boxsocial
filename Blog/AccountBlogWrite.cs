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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{

    /// <summary>
    /// Account sub module for writing blog entries.
    /// </summary>
    [AccountSubModule("blog", "write")]
    public class AccountBlogWrite : AccountSubModule
    {

        /// <summary>
        /// Sub module title.
        /// </summary>
        public override string Title
        {
            get
            {
                return core.Prose.GetString("Blog", "WRITE_NEW_BLOG_POST");
            }
        }

        /// <summary>
        /// Sub module order.
        /// </summary>
        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountBlogWrite class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountBlogWrite(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountBlogWrite_Load);
            this.Show += new EventHandler(AccountBlogWrite_Show);
        }

        /// <summary>
        /// Load procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountBlogWrite_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountBlogWrite_Delete));
        }

        /// <summary>
        /// Default show procedure for account sub module.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountBlogWrite_Show(object sender, EventArgs e)
        {
            SetTemplate("account_post");

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
			
            long postId = core.Functions.RequestLong("id", 0);
            byte licenseId = (byte)0;
            short categoryId = (short)1;
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

            if (postId > 0)
            {
                if (core.Http.Query["mode"] == "edit")
                {
                    try
                    {
                        BlogEntry be = new BlogEntry(core, postId);

                        titleTextBox.Value = be.Title;
                        postTextBox.Value = be.Body;

                        licenseId = be.License;
                        categoryId = be.Category;

                        postTime = be.GetCreatedDate(tz);

                        List<Tag> tags = Tag.GetTags(core, be);

                        string tagList = string.Empty;

                        foreach (Tag tag in tags)
                        {
                            if (tagList != string.Empty)
                            {
                                tagList += ", ";
                            }
                            tagList += tag.TagText;
                        }

                        tagsTextBox.Value = tagList;

                        if (be.OwnerId != core.LoggedInMemberId)
                        {
                            DisplayError("You must be the owner of the blog entry to modify it.");
                            return;
                        }
                    }
                    catch (InvalidBlogEntryException)
                    {
                        DisplayError(core.Prose.GetString("Blog", "BLOG_ENTRY_DOES_NOT_EXIST"));
                        return;
                    }
                }
            }

            template.Parse("S_POST_YEAR", postYearsSelectBox);
            template.Parse("S_POST_MONTH", postMonthsSelectBox);
            template.Parse("S_POST_DAY", postDaysSelectBox);
            template.Parse("S_POST_HOUR", postTime.Hour.ToString());
            template.Parse("S_POST_MINUTE", postTime.Minute.ToString());

            SelectBox licensesSelectBox = new SelectBox("license");
            DataTable licensesTable = db.Query(ContentLicense.GetSelectQueryStub(typeof(ContentLicense)));

            licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
				ContentLicense li = new ContentLicense(core, licenseRow);
                licensesSelectBox.Add(new SelectBoxItem(li.Id.ToString(), li.Title));
            }

            licensesSelectBox.SelectedKey = licenseId.ToString();

            SelectBox categoriesSelectBox = new SelectBox("category");
			SelectQuery query = Category.GetSelectQueryStub(typeof(Category));
			query.AddSort(SortOrder.Ascending, "category_title");
			
            DataTable categoriesTable = db.Query(query);

            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
				Category cat = new Category(core, categoryRow);
                categoriesSelectBox.Add(new SelectBoxItem(cat.Id.ToString(), cat.Title));
            }

            categoriesSelectBox.SelectedKey = categoryId.ToString();


            /* Parse the form fields */
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_BLOG_TEXT", postTextBox);
            template.Parse("S_TAGS", tagsTextBox);

            template.Parse("S_BLOG_LICENSE", licensesSelectBox);
            template.Parse("S_BLOG_CATEGORY", categoriesSelectBox);

            template.Parse("S_ID", postId.ToString());

            Save(new EventHandler(AccountBlogWrite_Save));
            if (core.Http.Form["publish"] != null)
            {
                AccountBlogWrite_Save(this, new EventArgs());
            }
        }

        /// <summary>
        /// Save procedure for a blog entry.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountBlogWrite_Save(object sender, EventArgs e)
        {
            string title = core.Http.Form["title"];
            string tags = core.Http.Form["tags"];
            string postBody = core.Http.Form["post"];
            byte license = 0;
            short category = 1;
            long postId = 0;
            string status = "PUBLISH";
            PublishStatuses publishStatus = PublishStatuses.Publish;
            string postGuid = "";

            /*
             * Create a blog if they do not already have one
             */
            Blog myBlog = null;
            try
            {
                myBlog = new Blog(core, LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            bool postEditTimestamp = false;
            int postYear, postMonth, postDay, postHour, postMinute;
            DateTime postTime = DateTime.Now;

            if (core.Http.Form["publish"] != null)
            {
                status = "PUBLISH";
                publishStatus = PublishStatuses.Publish;
            }

            if (core.Http.Form["save"] != null)
            {
                status = "DRAFT";
                publishStatus = PublishStatuses.Draft;
            }

            postId = core.Functions.FormLong("id", 0);
            license = core.Functions.FormByte("license", license);
            category = core.Functions.FormShort("category", category);

            try
            {
                postYear = core.Functions.FormInt("post-year", 0);
                postMonth = core.Functions.FormInt("post-month", 0);
                postDay = core.Functions.FormInt("post-day", 0);

                postHour = core.Functions.FormInt("post-hour", 0);
                postMinute = core.Functions.FormInt("post-minute", 0);

                postEditTimestamp = !string.IsNullOrEmpty(core.Http.Form["edit-timestamp"]);

                postTime = new DateTime(postYear, postMonth, postDay, postHour, postMinute, 0);
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(title))
            {
                SetError("You must give the blog post a title.");
                return;
            }

            if (string.IsNullOrEmpty(postBody))
            {
                SetError("You cannot save an empty blog post. You must post some content.");
                return;
            }

            string sqlPostTime = "";

            // update, must happen before save new because it populates postId
            if (postId > 0)
            {
                db.BeginTransaction();

                BlogEntry myBlogEntry = new BlogEntry(core, postId);

                if (publishStatus != myBlogEntry.Status)
                {
                    switch (publishStatus)
                    {
                        case PublishStatuses.Publish:
                            UpdateQuery uQuery = new UpdateQuery(typeof(Blog));
                            uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Addition, 1));
                            uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Subtraction, 1));
                            uQuery.AddCondition("user_id", Owner.Id);

                            db.Query(uQuery);
                            break;
                        case PublishStatuses.Draft:
                            uQuery = new UpdateQuery(typeof(Blog));
                            uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Addition, 1));
                            uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Subtraction, 1));
                            uQuery.AddCondition("user_id", Owner.Id);

                            db.Query(uQuery);
                            break;
                    }
                }

                myBlogEntry.Title = title;
                myBlogEntry.Body = postBody;
                myBlogEntry.License = license;
                myBlogEntry.Status = publishStatus;
                myBlogEntry.Category = category;
                myBlogEntry.ModifiedDateRaw = UnixTime.UnixTimeStamp();
                if (postEditTimestamp)
                {
                    myBlogEntry.PublishedDateRaw = tz.GetUnixTimeStamp(postTime);
                }

                myBlogEntry.Update();

                Tag.LoadTagsIntoItem(core, myBlogEntry, tags);
            }
            else if (postId == 0) // else if to make sure only one triggers
            {
                long postTimeRaw;
                // save new
                if (postEditTimestamp)
                {
                    postTimeRaw = tz.GetUnixTimeStamp(postTime);
                }
                else
                {
                    postTimeRaw = UnixTime.UnixTimeStamp();
                }

                db.BeginTransaction();

                BlogEntry myBlogEntry = BlogEntry.Create(core, core.Session.LoggedInMember, title, postBody, license, status, category, postTimeRaw);

                postGuid = string.Format("http://zinzam.com/{0}/blog/{1:0000}/{2:00}/{3}",
                    LoggedInMember.UserName, DateTime.Now.Year, DateTime.Now.Month, postId);

                myBlogEntry.Guid = postGuid;
                myBlogEntry.Update();

                switch (publishStatus)
                {
                    case PublishStatuses.Publish:
                        UpdateQuery uQuery = new UpdateQuery(typeof(Blog));
                        uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Addition, 1));
                        uQuery.AddCondition("user_id", Owner.Id);

                        db.Query(uQuery);
                        break;
                    case PublishStatuses.Draft:
                        uQuery = new UpdateQuery(typeof(Blog));
                        uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Addition, 1));
                        uQuery.AddCondition("user_id", Owner.Id);

                        db.Query(uQuery);
                        break;
                }

                Tag.LoadTagsIntoItem(core, myBlogEntry, tags, true);

                if (publishStatus == PublishStatuses.Publish)
                {
                    // TODO Permissions
                    //if (Access.FriendsCanRead(myBlogEntry.Permissions))
                    {
                        DateTime postDateTime = myBlogEntry.GetCreatedDate(core.Tz);

                        string postUrl = HttpUtility.HtmlEncode(string.Format("/{0}/blog/{1}/{2:00}/{3}",
                            LoggedInMember.UserName, postDateTime.Year, postDateTime.Month, myBlogEntry.PostId));

                        core.CallingApplication.PublishToFeed(LoggedInMember, "posted a new Blog Entry", string.Format("[iurl={0}]{1}[/iurl]",
                            postUrl, myBlogEntry.Title));
                    }
                }

            }

            if (status == "DRAFT")
            {
                SetRedirectUri(BuildUri("drafts"));
                core.Display.ShowMessage("Draft Saved", "Your draft has been saved.");
            }
            else
            {
                SetRedirectUri(BuildUri("manage"));
                core.Display.ShowMessage("Blog Post Published", "Your blog post has been published.");
            }
        }

        /// <summary>
        /// Delete procedure for a blog entry.
        /// </summary>
        /// <param name="sender">Object calling load event</param>
        /// <param name="e">Load EventArgs</param>
        void AccountBlogWrite_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long postId = core.Functions.RequestLong("id", 0);

            db.BeginTransaction();
            /*db.UpdateQuery(string.Format("DELETE FROM blog_postings WHERE post_id = {0} AND user_id = {1}",
                postId, LoggedInMember.UserId));*/

            try
            {
                BlogEntry post = new BlogEntry(core, postId);
                if (post.Delete() > 0)
                {
                    db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries - 1 WHERE user_id = {0}",
                        LoggedInMember.UserId));
                }
            }
            catch (InvalidBlogEntryException)
            {
                DisplayError("Blog entry does not exist.");
                return;
            }

            SetRedirectUri(BuildUri("manage"));
            core.Display.ShowMessage("Blog Post Deleted", "The blog post has been deleted from the database.");
            return;
        }
    }
}
