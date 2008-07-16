﻿/*
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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    [AccountSubModule("blog", "write")]
    public class AccountBlogWrite : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Write New Blog Post";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountBlogWrite()
        {
            this.Load += new EventHandler(AccountBlogWrite_Load);
            this.Show += new EventHandler(AccountBlogWrite_Show);
        }

        void AccountBlogWrite_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountBlogWrite_Delete));
        }

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

            Dictionary<string, string> postYears = new Dictionary<string, string>();
            for (int i = DateTime.Now.AddYears(-7).Year; i <= DateTime.Now.Year; i++)
            {
                postYears.Add(i.ToString(), i.ToString());
            }

            Dictionary<string, string> postMonths = new Dictionary<string, string>();
            for (int i = 1; i < 13; i++)
            {
                postMonths.Add(i.ToString(), Functions.IntToMonth(i));
            }

            Dictionary<string, string> postDays = new Dictionary<string, string>();
            for (int i = 1; i < 32; i++)
            {
                postDays.Add(i.ToString(), i.ToString());
            }

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

            Display.ParseSelectBox(template, "S_POST_YEAR", "post-year", postYears, postTime.Year.ToString());
            Display.ParseSelectBox(template, "S_POST_MONTH", "post-month", postMonths, postTime.Month.ToString());
            Display.ParseSelectBox(template, "S_POST_DAY", "post-day", postDays, postTime.Day.ToString());

            template.Parse("S_POST_HOUR", postTime.Hour.ToString());
            template.Parse("S_POST_MINUTE", postTime.Minute.ToString());

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            Dictionary<string, string> licenses = new Dictionary<string, string>();
            DataTable licensesTable = db.Query("SELECT license_id, license_title FROM licenses");

            licenses.Add("0", "Default ZinZam License");
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
            }

            Dictionary<string, string> categories = new Dictionary<string, string>();
            DataTable categoriesTable = db.Query("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categories.Add(((short)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]);
            }

            Display.ParseSelectBox(template, "S_BLOG_LICENSE", "license", licenses, licenseId.ToString());
            Display.ParseSelectBox(template, "S_BLOG_CATEGORY", "category", categories, categoryId.ToString());
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
                Blog myBlog = new Blog(core, loggedInMember);
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
                    Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(), status, loggedInMember.UserId, postId, category, sqlPostTime));

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
                    loggedInMember.UserId, Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(), status, category, sqlPostTime));

                postGuid = string.Format("http://zinzam.com/{0}/blog/{1:0000}/{2:00}/{3}",
                    loggedInMember.UserName, DateTime.Now.Year, DateTime.Now.Month, postId);

                db.UpdateQuery(string.Format("UPDATE blog_postings SET post_guid = '{0}' WHERE post_id = {1} and user_id = {2}",
                    postGuid, postId, loggedInMember.UserId));

                db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries + 1 WHERE user_id = {0}",
                    loggedInMember.UserId));

                if (status == "PUBLISH")
                {
                    BlogEntry myBlogEntry = new BlogEntry(core, postId);

                    if (Access.FriendsCanRead(myBlogEntry.Permissions))
                    {
                        DateTime postDateTime = myBlogEntry.GetCreatedDate(core.tz);

                        string postUrl = HttpUtility.HtmlEncode(string.Format("/{0}/blog/{1}/{2:00}/{3}",
                            loggedInMember.UserName, postDateTime.Year, postDateTime.Month, myBlogEntry.PostId));

                        AppInfo.Entry.PublishToFeed(loggedInMember, "posted a new Blog Entry", string.Format("[iurl={0}]{1}[/iurl]",
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

        void AccountBlogWrite_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long postId = Functions.RequestLong("id", 0);

            db.BeginTransaction();
            db.UpdateQuery(string.Format("DELETE FROM blog_postings WHERE post_id = {0} AND user_id = {1}",
                postId, loggedInMember.UserId));

            db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries - 1 WHERE user_id = {0}",
                loggedInMember.UserId));

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Blog Post Deleted", "The blog post has been deleted from the database.");
            return;
        }
    }
}