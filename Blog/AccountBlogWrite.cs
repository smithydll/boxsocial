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
                return "Write New Blog Post";
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
        /// Constructor for the Account sub module
        /// </summary>
        public AccountBlogWrite()
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

            long postId = Functions.RequestLong("id", 0);
            ushort blogPermissions = 0x3333;
            byte licenseId = 0;
            short categoryId = 1;
            string postTitle = "";
            string postText = "";
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
                postMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), Functions.IntToMonth(i)));
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
                if (Request.QueryString["mode"] == "edit")
                {
                    try
                    {
                        BlogEntry be = new BlogEntry(core, postId);

                        postTitle = be.Title;
                        postText = be.Body;
                        licenseId = be.License;
                        categoryId = be.Category;
                        blogPermissions = be.Permissions;

                        postTime = be.GetCreatedDate(tz);

                        if (be.OwnerId != core.LoggedInMemberId)
                        {
                            DisplayError("You must be the owner of the blog entry to modify it.");
                            return;
                        }
                    }
                    catch (InvalidBlogEntryException)
                    {
                        DisplayError("Blog entry does not exist.");
                        return;
                    }
                }
            }

            template.Parse("S_POST_YEAR", postYearsSelectBox);
            template.Parse("S_POST_MONTH", postMonthsSelectBox);
            template.Parse("S_POST_DAY", postDaysSelectBox);
            template.Parse("S_POST_HOUR", postTime.Hour.ToString());
            template.Parse("S_POST_MINUTE", postTime.Minute.ToString());

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            SelectBox licensesSelectBox = new SelectBox("license");
            DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

            licensesSelectBox.Add(new SelectBoxItem("0", "Default License"));
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                licensesSelectBox.Add(new SelectBoxItem(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]));
            }

            licensesSelectBox.SelectedKey = licenseId.ToString();

            SelectBox categoriesSelectBox = new SelectBox("category");
            DataTable categoriesTable = db.Query("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");

            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categoriesSelectBox.Add(new SelectBoxItem(((short)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]));
            }

            categoriesSelectBox.SelectedKey = categoryId.ToString();

            template.Parse("S_BLOG_LICENSE", licensesSelectBox);
            template.Parse("S_BLOG_CATEGORY", categoriesSelectBox);
            Display.ParsePermissionsBox(template, "S_BLOG_PERMS", blogPermissions, permissions);

            template.Parse("S_TITLE", postTitle);
            template.Parse("S_BLOG_TEXT", postText);
            template.Parse("S_ID", postId.ToString());

            Save(new EventHandler(AccountBlogWrite_Save));
            if (Request.Form["publish"] != null)
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
            string title = Request.Form["title"];
            string postBody = Request.Form["post"];
            short license = 0;
            short category = 1;
            long postId = 0;
            string status = "PUBLISH";
            string postGuid = "";

            /*
             * Create a blog if they do not already have one
             */
            try
            {
                Blog myBlog = new Blog(core, LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                Blog.Create(core);
            }

            bool postEditTimestamp = false;
            int postYear, postMonth, postDay, postHour, postMinute;
            DateTime postTime = DateTime.Now;

            if (Request.Form["publish"] != null)
            {
                status = "PUBLISH";
            }

            if (Request.Form["save"] != null)
            {
                status = "DRAFT";
            }

            postId = Functions.FormLong("id", 0);
            license = Functions.FormShort("license", license);
            category = Functions.FormShort("category", category);

            try
            {
                postYear = Functions.FormInt("post-year", 0);
                postMonth = Functions.FormInt("post-month", 0);
                postDay = Functions.FormInt("post-day", 0);

                postHour = Functions.FormInt("post-hour", 0);
                postMinute = Functions.FormInt("post-minute", 0);

                postEditTimestamp = !string.IsNullOrEmpty(Request.Form["edit-timestamp"]);

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
                if (postEditTimestamp)
                {
                    sqlPostTime = string.Format(", post_time_ut = {0}",
                        tz.GetUnixTimeStamp(postTime));
                }
                else
                {
                    sqlPostTime = "";
                }

                db.UpdateQuery(string.Format("UPDATE blog_postings SET post_title = '{0}', post_modified_ut = UNIX_TIMESTAMP(), post_ip = '{1}', post_text = '{2}', post_license = {3}, post_access = {4}, post_status = '{5}', post_category = {8}{9} WHERE user_id = {6} AND post_id = {7}",
                    Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(), status, LoggedInMember.UserId, postId, category, sqlPostTime));

                /* do not count edits as new postings*/
            }
            else if (postId == 0) // else if to make sure only one triggers
            {
                // save new
                if (postEditTimestamp)
                {
                    sqlPostTime = string.Format("{0}",
                        tz.GetUnixTimeStamp(postTime));
                }
                else
                {
                    sqlPostTime = "UNIX_TIMESTAMP()";
                }

                db.BeginTransaction();
                postId = db.UpdateQuery(string.Format("INSERT INTO blog_postings (user_id, post_time_ut, post_title, post_modified_ut, post_ip, post_text, post_license, post_access, post_status, post_category) VALUES ({0}, {8}, '{1}', UNIX_TIMESTAMP(), '{2}', '{3}', {4}, {5}, '{6}', {7})",
                    LoggedInMember.UserId, Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(), status, category, sqlPostTime));

                postGuid = string.Format("http://zinzam.com/{0}/blog/{1:0000}/{2:00}/{3}",
                    LoggedInMember.UserName, DateTime.Now.Year, DateTime.Now.Month, postId);

                db.UpdateQuery(string.Format("UPDATE blog_postings SET post_guid = '{0}' WHERE post_id = {1} and user_id = {2}",
                    postGuid, postId, LoggedInMember.UserId));

                db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries + 1 WHERE user_id = {0}",
                    LoggedInMember.UserId));

                if (status == "PUBLISH")
                {
                    BlogEntry myBlogEntry = new BlogEntry(core, postId);

                    if (Access.FriendsCanRead(myBlogEntry.Permissions))
                    {
                        DateTime postDateTime = myBlogEntry.GetCreatedDate(core.tz);

                        string postUrl = HttpUtility.HtmlEncode(string.Format("/{0}/blog/{1}/{2:00}/{3}",
                            LoggedInMember.UserName, postDateTime.Year, postDateTime.Month, myBlogEntry.PostId));

                        AppInfo.Entry.PublishToFeed(LoggedInMember, "posted a new Blog Entry", string.Format("[iurl={0}]{1}[/iurl]",
                            postUrl, myBlogEntry.Title));
                    }
                }

            }

            if (status == "DRAFT")
            {
                SetRedirectUri(BuildUri("drafts"));
                Display.ShowMessage("Draft Saved", "Your draft has been saved.");
            }
            else
            {
                SetRedirectUri(BuildUri("manage"));
                Display.ShowMessage("Blog Post Published", "Your blog post has been published.");
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

            long postId = Functions.RequestLong("id", 0);

            db.BeginTransaction();
            db.UpdateQuery(string.Format("DELETE FROM blog_postings WHERE post_id = {0} AND user_id = {1}",
                postId, LoggedInMember.UserId));

            db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries - 1 WHERE user_id = {0}",
                LoggedInMember.UserId));

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Blog Post Deleted", "The blog post has been deleted from the database.");
            return;
        }
    }
}
