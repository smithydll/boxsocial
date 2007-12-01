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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    public class AccountBlog : AccountModule
    {

        public AccountBlog(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageBlog);
            RegisterSubModule += new RegisterSubModuleHandler(WritePost);
            RegisterSubModule += new RegisterSubModuleHandler(ManageDrafts);
            // TODO: Blog Preferences
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
            
        }

        public override string Name
        {
            get {
                return "Blog";
            }
        }

        public override string Key
        {
            get {
                return "blog";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        private void ManageDrafts(string submodule)
        {
            subModules.Add("drafts", "Draft Blog Posts");
            if (submodule != "drafts") return;

            ManageBlog(submodule, true);
        }

        private void ManageBlog(string submodule)
        {
            subModules.Add("manage", "Manage Blog Posts");
            if (submodule != "manage" && !string.IsNullOrEmpty(submodule)) return;

            ManageBlog(submodule, false);
        }

        private void ManageBlog(string submodule, bool drafts)
        {
            template.SetTemplate("account_blog_manage.html");

            string status = "PUBLISH";

            if (drafts)
            {
                status = "DRAFT";
            }

            DataTable blogTable = db.SelectQuery(string.Format("SELECT ub.post_comments, ub.post_id, ub.post_title, ub.post_time_ut FROM blog_postings ub WHERE user_id = {0} AND post_status = '{1}' ORDER BY post_time_ut DESC;",
                loggedInMember.UserId, status));

            for (int i = 0; i < blogTable.Rows.Count; i++)
            {
                DataRow blogRow = blogTable.Rows[i];

                VariableCollection blogVariableCollection = template.CreateChild("blog_list");

                DateTime postedTime = tz.DateTimeFromMysql(blogRow["post_time_ut"]);

                blogVariableCollection.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(((uint)blogRow["post_comments"]).ToString()));
                blogVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)blogRow["post_title"]));
                blogVariableCollection.ParseVariables("POSTED", HttpUtility.HtmlEncode(tz.DateTimeToString(postedTime)));

                blogVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(ZzUri.BuildBlogPostUri(loggedInMember, postedTime.Year, postedTime.Month, (long)blogRow["post_id"])));

                blogVariableCollection.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(string.Format("/account/?module=blog&amp;sub=write&amp;action=edit&amp;id={0}",
                    (long)blogRow["post_id"])));
                blogVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(string.Format("/account/?module=blog&amp;sub=write&amp;action=delete&amp;id={0}",
                    (long)blogRow["post_id"])));

                if (i % 2 == 0)
                {
                    blogVariableCollection.ParseVariables("INDEX_EVEN", "TRUE");
                }
            }
        }

        /// <summary>
        ///  Blog Post
        /// </summary>
        private void WritePost(string submodule)
        {
            subModules.Add("write", "Write New Blog Post");
            if (submodule != "write") return;

            if (Request.Form["publish"] != null || Request.Form["save"] != null)
            {
                WriteBlogSave();
                return;
            }

            long postId = 0;
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

            if (Request.QueryString["id"] != null)
            {
                try
                {
                    postId = long.Parse(Request.QueryString["id"]);
                }
                catch
                {
                }
            }

            if (postId > 0)
            {
                if (Request.QueryString["action"] == "delete")
                {
                    db.UpdateQuery(string.Format("DELETE FROM blog_postings WHERE post_id = {0} AND user_id = {1}",
                        postId, loggedInMember.UserId), true);

                    db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries - 1 WHERE user_id = {0}",
                        loggedInMember.UserId), false);

                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("blog", "manage")));
                    Display.ShowMessage(core, "Blog Post Deleted", "The blog post has been deleted from the database.");
                    return;
                }
                else if (Request.QueryString["action"] == "edit")
                {
                    DataTable postTable = db.SelectQuery(string.Format("SELECT post_time_ut, post_access, post_category, post_license, post_title, post_text FROM blog_postings WHERE post_id = {0} AND user_id = {1}",
                        postId, loggedInMember.UserId));

                    if (postTable.Rows.Count == 1)
                    {
                        postTitle = (string)postTable.Rows[0]["post_title"];
                        postText = (string)postTable.Rows[0]["post_text"];
                        licenseId = (byte)postTable.Rows[0]["post_license"];
                        categoryId = (short)postTable.Rows[0]["post_category"];
                        blogPermissions = (ushort)postTable.Rows[0]["post_access"];

                        postTime = tz.DateTimeFromMysql(postTable.Rows[0]["post_time_ut"]);
                    }
                }
            }

            template.ParseVariables("S_POST_YEAR", Functions.BuildSelectBox("post-year", postYears, postTime.Year.ToString()));
            template.ParseVariables("S_POST_MONTH", Functions.BuildSelectBox("post-month", postMonths, postTime.Month.ToString()));
            template.ParseVariables("S_POST_DAY", Functions.BuildSelectBox("post-day", postDays, postTime.Day.ToString()));

            template.ParseVariables("S_POST_HOUR", HttpUtility.HtmlEncode(postTime.Hour.ToString()));
            template.ParseVariables("S_POST_MINUTE", HttpUtility.HtmlEncode(postTime.Minute.ToString()));

            template.SetTemplate("account_post.html");

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            Dictionary<string, string> licenses = new Dictionary<string, string>();
            DataTable licensesTable = db.SelectQuery("SELECT license_id, license_title FROM licenses");

            licenses.Add("0", "Default ZinZam License");
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
            }

            Dictionary<string, string> categories = new Dictionary<string, string>();
            DataTable categoriesTable = db.SelectQuery("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categories.Add(((short)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]);
            }

            template.ParseVariables("S_BLOG_LICENSE", Functions.BuildSelectBox("license", licenses, licenseId.ToString()));
            template.ParseVariables("S_BLOG_CATEGORY", Functions.BuildSelectBox("category", categories, categoryId.ToString()));
            template.ParseVariables("S_BLOG_PERMS", Functions.BuildPermissionsBox(blogPermissions, permissions));

            template.ParseVariables("S_TITLE", HttpUtility.HtmlEncode(postTitle));
            template.ParseVariables("S_BLOG_TEXT", HttpUtility.HtmlEncode(postText));
            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(postId.ToString()));
        }

        public void WriteBlogSave()
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
                Blog myBlog = new Blog(db, loggedInMember);
            }
            catch //if (!loggedInMember.HasBlog)
            {
                db.UpdateQuery(string.Format("INSERT INTO user_blog (user_id) VALUES ({0});",
                    loggedInMember.UserId));
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

            try
            {
                // edit post
                postId = long.Parse(Request.Form["id"]);
            }
            catch
            {
            }

            try
            {
                license = short.Parse(Request.Form["license"]);
            }
            catch
            {
            }

            try
            {
                category = short.Parse(Request.Form["category"]);
            }
            catch
            {
            }

            try
            {
                postYear = int.Parse(Request.Form["post-year"]);
                postMonth = int.Parse(Request.Form["post-month"]);
                postDay = int.Parse(Request.Form["post-day"]);

                postHour = int.Parse(Request.Form["post-hour"]);
                postMinute = int.Parse(Request.Form["post-minute"]);

                postEditTimestamp = !string.IsNullOrEmpty(Request.Form["edit-timestamp"]);

                postTime = new DateTime(postYear, postMonth, postDay, postHour, postMinute, 0);
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(title))
            {
                template.ParseVariables("ERROR", "You must give the blog post a title.");
                return;
            }

            if (string.IsNullOrEmpty(postBody))
            {
                template.ParseVariables("ERROR", "You cannot save an empty blog post. You must post some content.");
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
                    Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(Request), status, loggedInMember.UserId, postId, category, sqlPostTime), false);

                /* do not count edits as new postings*/
                /*db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries + 1 WHERE user_id = {0}",
                    loggedInMember.UserId), false);*/
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

                postId = db.UpdateQuery(string.Format("INSERT INTO blog_postings (user_id, post_time_ut, post_title, post_modified_ut, post_ip, post_text, post_license, post_access, post_status, post_category) VALUES ({0}, {8}, '{1}', UNIX_TIMESTAMP(), '{2}', '{3}', {4}, {5}, '{6}', {7})",
                    loggedInMember.UserId, Mysql.Escape(title), session.IPAddress.ToString(), Mysql.Escape(postBody), license, Functions.GetPermission(Request), status, category, sqlPostTime), true);

                postGuid = string.Format("http://zinzam.com/{0}/blog/{1:0000}/{2:00}/{3}",
                    loggedInMember.UserName, DateTime.Now.Year, DateTime.Now.Month, postId);

                db.UpdateQuery(string.Format("UPDATE blog_postings SET post_guid = '{0}' WHERE post_id = {1} and user_id = {2}",
                    postGuid, postId, loggedInMember.UserId), true);

                db.UpdateQuery(string.Format("UPDATE user_blog SET blog_entries = blog_entries + 1 WHERE user_id = {0}",
                    loggedInMember.UserId), false);

            }

            if (status == "DRAFT")
            {
                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("blog", "drafts")));
                Display.ShowMessage(core, "Draft Saved", "Your draft has been saved.");
            }
            else
            {
                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("blog", "manage")));
                Display.ShowMessage(core, "Blog Post Published", "Your blog post has been published.");
            }
        }
    }
}
