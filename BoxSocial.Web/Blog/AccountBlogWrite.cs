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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Applications.Gallery;
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
        public AccountBlogWrite(Core core, Primitive owner)
            : base(core, owner)
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

            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
            javaScriptVariableCollection.Parse("URI", @"/scripts/jquery.sceditor.bbcode.min.js");

            VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");
            styleSheetVariableCollection.Parse("URI", @"/styles/jquery.sceditor.theme.default.min.css");

            core.Template.Parse("OWNER_STUB", Owner.UriStubAbsolute);

            Blog blog = new Blog(core, (User)Owner);

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
			
            long postId = core.Functions.RequestLong("id", 0);
            byte licenseId = (byte)0;
            short categoryId = (short)1;
            DateTime postTime = core.Tz.Now;

            SelectBox postYearsSelectBox = new SelectBox("post-year");
            for (int i = core.Tz.Now.AddYears(-7).Year; i <= core.Tz.Now.Year; i++)
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

            if (postId > 0 && core.Http.Query["mode"] == "edit")
            {
                try
                {
                    BlogEntry be = new BlogEntry(core, postId);

                    titleTextBox.Value = be.Title;
                    postTextBox.Value = be.Body;

                    licenseId = be.License;
                    categoryId = be.Category;

                    postTime = be.GetPublishedDate(tz);

                    List<Tag> tags = Tag.GetTags(core, be);

                    //string tagList = string.Empty;

                    foreach (Tag tag in tags)
                    {
                        /*if (tagList != string.Empty)
                        {
                            tagList += ", ";
                        }
                        tagList += tag.TagText;*/
                        tagsTextBox.AddTag(tag);
                    }

                    //tagsTextBox.Value = tagList;

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
            else
            {
                template.Parse("IS_NEW", "TRUE");

                PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", blog.ItemKey);
                HiddenField aclModeField = new HiddenField("aclmode");
                aclModeField.Value = "simple";

                template.Parse("S_PERMISSIONS", permissionSelectBox);
                template.Parse("S_ACLMODE", aclModeField);
            }

            template.Parse("S_POST_YEAR", postYearsSelectBox);
            template.Parse("S_POST_MONTH", postMonthsSelectBox);
            template.Parse("S_POST_DAY", postDaysSelectBox);
            template.Parse("S_POST_HOUR", postTime.Hour.ToString());
            template.Parse("S_POST_MINUTE", postTime.Minute.ToString());

            SelectBox licensesSelectBox = new SelectBox("license");
            DataTable licensesTable = db.Query(ContentLicense.GetSelectQueryStub(core, typeof(ContentLicense)));

            licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
				ContentLicense li = new ContentLicense(core, licenseRow);
                licensesSelectBox.Add(new SelectBoxItem(li.Id.ToString(), li.Title));
            }

            licensesSelectBox.SelectedKey = licenseId.ToString();

            SelectBox categoriesSelectBox = new SelectBox("category");
            SelectQuery query = Category.GetSelectQueryStub(core, typeof(Category));
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

            template.Parse("S_PUBLISH_FEED", publishToFeedCheckBox);

            template.Parse("S_ID", postId.ToString());

            foreach (Emoticon emoticon in core.Emoticons)
            {
                if (emoticon.Category == "modifier") continue;
                if (emoticon.Category == "people" && emoticon.Code.Length < 3)
                {
                    VariableCollection emoticonVariableCollection = template.CreateChild("emoticon_list");
                    emoticonVariableCollection.Parse("CODE", emoticon.Code);
                    emoticonVariableCollection.Parse("URI", emoticon.File);
                }
                else
                {
                    //VariableCollection emoticonVariableCollection = template.CreateChild("emoticon_hidden_list");
                    //emoticonVariableCollection.Parse("CODE", emoticon.Code);
                    //emoticonVariableCollection.Parse("URI", emoticon.File);
                }
            }

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
            //string tags = core.Http.Form["tags"];
            string postBody = core.Http.Form["post"];
            bool publishToFeed = (core.Http.Form["publish-feed"] != null);

            byte license = 0;
            short category = 1;
            long postId = 0;
            PublishStatuses publishStatus = PublishStatuses.Published;
            string postGuid = "";
            long currentTimestamp = UnixTime.UnixTimeStamp();

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
            DateTime postTime = core.Tz.DateTimeFromMysql(currentTimestamp);

            if (core.Http.Form["publish"] != null)
            {
                publishStatus = PublishStatuses.Published;
            }

            if (core.Http.Form["save"] != null)
            {
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

                long postTimeRaw;
                bool doPublish = false;
                if (postEditTimestamp)
                {
                    postTimeRaw = tz.GetUnixTimeStamp(postTime);

                    if (postTimeRaw > UnixTime.UnixTimeStamp())
                    {
                        publishStatus = PublishStatuses.Queued;
                    }
                }

                if (publishStatus != myBlogEntry.Status)
                {
                    switch (publishStatus)
                    {
                        case PublishStatuses.Published:
                            UpdateQuery uQuery = new UpdateQuery(typeof(Blog));
                            uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Addition, 1));
                            switch (myBlogEntry.Status)
                            {
                                case PublishStatuses.Draft:
                                    uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Subtraction, 1));
                                    break;
                                case PublishStatuses.Queued:
                                    uQuery.AddField("blog_queued_entries", new QueryOperation("blog_queued_entries", QueryOperations.Subtraction, 1));
                                    break;
                            }
                            uQuery.AddCondition("user_id", Owner.Id);

                            db.Query(uQuery);

                            doPublish = true;
                            break;
                        case PublishStatuses.Draft:
                            uQuery = new UpdateQuery(typeof(Blog));
                            uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Addition, 1));
                            switch (myBlogEntry.Status)
                            {
                                case PublishStatuses.Published:
                                    uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Subtraction, 1));
                                    break;
                                case PublishStatuses.Queued:
                                    uQuery.AddField("blog_queued_entries", new QueryOperation("blog_queued_entries", QueryOperations.Subtraction, 1));
                                    break;
                            }
                            uQuery.AddCondition("user_id", Owner.Id);

                            db.Query(uQuery);
                            break;
                        case PublishStatuses.Queued:
                            uQuery = new UpdateQuery(typeof(Blog));
                            uQuery.AddField("blog_queued_entries", new QueryOperation("blog_queued_entries", QueryOperations.Addition, 1));
                            switch (myBlogEntry.Status)
                            {
                                case PublishStatuses.Published:
                                    uQuery.AddField("blog_entries", new QueryOperation("blog_entries", QueryOperations.Subtraction, 1));
                                    break;
                                case PublishStatuses.Draft:
                                    uQuery.AddField("blog_drafts", new QueryOperation("blog_drafts", QueryOperations.Subtraction, 1));
                                    break;
                            }
                            uQuery.AddCondition("user_id", Owner.Id);

                            db.Query(uQuery);
                            break;
                    }
                }

                // Save image attachments
                {
                    postBody = core.Bbcode.ExtractAndSaveImageData(postBody, myBlogEntry, saveImage);
                }

                myBlogEntry.Title = title;
                myBlogEntry.BodyCache = string.Empty;
                myBlogEntry.Body = postBody;
                myBlogEntry.License = license;
                myBlogEntry.Category = category;
                myBlogEntry.ModifiedDateRaw = currentTimestamp;
                if (postEditTimestamp)
                {
                    myBlogEntry.PublishedDateRaw = tz.GetUnixTimeStamp(postTime);
                }
                myBlogEntry.Status = publishStatus;

                myBlogEntry.Update();

                if (publishToFeed && publishStatus == PublishStatuses.Published && doPublish)
                {
                    core.Search.Index(myBlogEntry);
                    core.CallingApplication.PublishToFeed(core, LoggedInMember, myBlogEntry, myBlogEntry.Title);
                }

                Tag.LoadTagsIntoItem(core, myBlogEntry, TagSelectBox.FormTags(core, "tags"));
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
                    postTimeRaw = currentTimestamp;
                }

                if (postTimeRaw > UnixTime.UnixTimeStamp())
                {
                    publishStatus = PublishStatuses.Queued;
                }

                db.BeginTransaction();

                BlogEntry myBlogEntry = BlogEntry.Create(core, AccessControlLists.GetNewItemPermissionsToken(core), myBlog, title, postBody, license, publishStatus, category, postTimeRaw);

                /*AccessControlLists acl = new AccessControlLists(core, myBlogEntry);
                acl.SaveNewItemPermissions();*/

                postGuid = core.Hyperlink.StripSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                    LoggedInMember.UriStubAbsolute, DateTime.Now.Year, DateTime.Now.Month, postId));

                myBlogEntry.Guid = postGuid;
                long updated = myBlogEntry.Update();

                if (updated > 0)
                {
                    
                }

                switch (publishStatus)
                {
                    case PublishStatuses.Published:
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
                    case PublishStatuses.Queued:
                        uQuery = new UpdateQuery(typeof(Blog));
                        uQuery.AddField("blog_queued_entries", new QueryOperation("blog_queued_entries", QueryOperations.Addition, 1));
                        uQuery.AddCondition("user_id", Owner.Id);

                        db.Query(uQuery);
                        break;
                }

                Tag.LoadTagsIntoItem(core, myBlogEntry, TagSelectBox.FormTags(core, "tags"), true);

                // Save image attachments
                {
                    postBody = core.Bbcode.ExtractAndSaveImageData(postBody, myBlogEntry, saveImage);

                    myBlogEntry.BodyCache = string.Empty;
                    myBlogEntry.Body = postBody;
                    myBlogEntry.Update(); // only triggers if postBody has been updated
                }

                if (publishToFeed && publishStatus == PublishStatuses.Published)
                {
                    core.Search.Index(myBlogEntry);
                    core.CallingApplication.PublishToFeed(core, LoggedInMember, myBlogEntry, myBlogEntry.Title);
                }

            }

            switch (publishStatus)
            {
                case PublishStatuses.Draft:
                    SetRedirectUri(BuildUri("drafts"));
                    core.Display.ShowMessage("Draft Saved", "Your draft has been saved.");
                    break;
                case PublishStatuses.Published:
                    SetRedirectUri(BuildUri("manage"));
                    core.Display.ShowMessage("Blog Post Published", "Your blog post has been published.");
                    break;
                case PublishStatuses.Queued:
                    SetRedirectUri(BuildUri("queue"));
                    core.Display.ShowMessage("Blog Post Queued", "Your blog post has been placed in the publish queue.");
                    break;
            }
        }

        private string saveImage(NumberedItem post, string imageType, byte[] imageData)
        {
            BlogEntry myBlogEntry = null;
            if (post is BlogEntry)
            {
                myBlogEntry = (BlogEntry)myBlogEntry;
            }

            string imagePath = string.Empty;

            Gallery.Gallery parent = null;
            Gallery.Gallery grandParent = null;

            string grandParentSlug = "photos-from-posts";
            try
            {
                grandParent = new Gallery.Gallery(core, Owner, grandParentSlug);
            }
            catch (InvalidGalleryException)
            {
                Gallery.Gallery root = new Gallery.Gallery(core, Owner);
                grandParent = Gallery.Gallery.Create(core, Owner, root, "Photos From Posts", ref grandParentSlug, "All my unsorted uploads");
            }

            string gallerySlug = "blog-" + post.Id.ToString();

            try
            {
                parent = new Gallery.Gallery(core, Owner, gallerySlug);

                parent.GalleryTitle = myBlogEntry.Title;
                parent.Update();
            }
            catch (InvalidGalleryException)
            {
                parent = Gallery.Gallery.Create(core, Owner, grandParent, myBlogEntry.Title, ref gallerySlug, string.Empty);
            }

            AccessControlLists acl = new AccessControlLists(core, parent);
            acl.SaveNewItemPermissions();

            MemoryStream stream = new MemoryStream();
            stream.Write(imageData, 0, imageData.Length);

            string slug = "image-" + parent.Items.ToString();
            GalleryItem newGalleryItem = GalleryItem.Create(core, Owner, parent, string.Empty, ref slug, slug, imageType, (ulong)imageData.Length, string.Empty, core.Functions.GetLicenseId(), core.Functions.GetClassification(), stream, true /*, width, height*/);

            imagePath = newGalleryItem.FullPath;

            return imagePath;
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
