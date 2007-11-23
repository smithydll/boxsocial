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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    public class Blog
    {
        public const string BLOG_FIELDS = "ub.blog_title, ub.blog_entries, ub.blog_comments, ub.blog_visits, ub.blog_access";

        private Mysql db;

        private int userId;
        private Primitive owner;
        private string title;
        private long entries;
        private long comments;
        private long visits;
        private ushort permissions;
        private Access blogAccess;

        public int UserId
        {
            get
            {
                return userId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public long Entries
        {
            get
            {
                return entries;
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public long Visits
        {
            get
            {
                return visits;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access BlogAccess
        {
            get
            {
                return blogAccess;
            }
        }

        public Blog(Mysql db, Member owner)
        {
            this.db = db;
            this.owner = owner;

            DataTable blogTable = db.SelectQuery(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_blog ub ON uk.user_id = ub.user_id WHERE uk.user_id = {0}",
                owner.UserId, Blog.BLOG_FIELDS));

            if (blogTable.Rows.Count == 1)
            {
                loadUserBlog(blogTable.Rows[0]);
            }
        }

        private void loadUserBlog(DataRow blogRow)
        {
            title = (string)blogRow["blog_title"];
            entries = (long)blogRow["blog_entries"];
            comments = (long)blogRow["blog_comments"];
            visits = (long)blogRow["blog_visits"];
            permissions = (ushort)blogRow["blog_access"];
            blogAccess = new Access(db, permissions, owner);
        }

        public List<BlogEntry> GetEntries(PPage page, string category, int post, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            List<BlogEntry> entries = new List<BlogEntry>();

            long loggedIdUid = Member.GetMemberId(page.loggedInMember);
            readAccessLevel = page.ProfileOwner.GetAccessLevel(page.loggedInMember);

            string sqlWhere = "";
            string sqlInnerJoin = "";

            if (string.IsNullOrEmpty(category))
            {
                if (post > 0)
                {
                    sqlWhere = string.Format("{0} AND post_id = {1}",
                        sqlWhere, post);
                }

                if (year > 0)
                {
                    sqlWhere = string.Format("{0} AND YEAR(FROM_UNIXTIME(post_time_ut)) = {1}",
                        sqlWhere, year);
                }

                if (month > 0)
                {
                    sqlWhere = string.Format("{0} AND MONTH(FROM_UNIXTIME(post_time_ut)) = {1}",
                        sqlWhere, month);
                }
            }
            else
            {
                sqlWhere = string.Format("{0} AND category_path = '{1}'",
                        sqlWhere, Mysql.Escape(category));

                sqlInnerJoin = string.Format("{0} global_categories ON post_category = category_id",
                    sqlInnerJoin);
            }

            if (!string.IsNullOrEmpty(sqlInnerJoin))
            {
                sqlInnerJoin = string.Format("INNER JOIN {0}",
                    sqlInnerJoin);
            }

            int bpage = currentPage;
            if (post > 0)
            {
                bpage = 1;
            }

            DataTable blogEntriesTable = db.SelectQuery(string.Format("SELECT {7} FROM blog_postings be {4} WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' {3} ORDER BY post_time_ut DESC LIMIT {5}, {6};",
                page.ProfileOwner.UserId, loggedIdUid, readAccessLevel, sqlWhere, sqlInnerJoin, (bpage - 1) * 10, 10, BlogEntry.BLOG_ENTRY_FIELDS));

            foreach (DataRow dr in blogEntriesTable.Rows)
            {
                entries.Add(new BlogEntry(db, owner, dr));
            }

            return entries;
        }

        public static void Show(Core core, PPage page, string category, int post, int year, int month)
        {
            core.template.SetTemplate("viewblog.html");

            bool rss = false;
            long comments = 0;
            int p = 1;

            Blog myBlog = new Blog(core.db, page.ProfileOwner);

            long loggedIdUid = myBlog.BlogAccess.SetSessionViewer(core.session);
            ushort readAccessLevel = 0x0000;

            if (!myBlog.BlogAccess.CanRead)
            {
                Functions.Generate403(core);
                return;
            }

            try
            {
                rss = bool.Parse(HttpContext.Current.Request.QueryString["rss"]);
            }
            catch { }

            try
            {
                p = int.Parse(HttpContext.Current.Request.QueryString["p"]);
            }
            catch { }

            if (rss)
            {
                HttpContext.Current.Response.ContentType = "text/xml";
            }

            page.ProfileOwner.LoadProfileInfo();

            if (!rss)
            {
                core.template.ParseVariables("PAGE_LIST", Display.GeneratePageList(core.db, page.ProfileOwner, core.session.LoggedInMember, true));
                core.template.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode((ZzUri.BuildProfileUri(page.ProfileOwner))));
                core.template.ParseVariables("U_BLOG", HttpUtility.HtmlEncode((ZzUri.BuildBlogUri(page.ProfileOwner))));
                core.template.ParseVariables("U_GALLERY", HttpUtility.HtmlEncode((ZzUri.BuildGalleryUri(page.ProfileOwner))));
                core.template.ParseVariables("U_FRIENDS", HttpUtility.HtmlEncode((ZzUri.BuildFriendsUri(page.ProfileOwner))));
            }

            List<BlogEntry> blogEntries = myBlog.GetEntries(page, category, post, year, month, p, 10, ref readAccessLevel);

            core.template.ParseVariables("BLOGPOSTS", HttpUtility.HtmlEncode(blogEntries.Count.ToString()));

            if (!rss)
            {
                DataTable archiveTable = core.db.SelectQuery(string.Format("SELECT DISTINCT YEAR(FROM_UNIXTIME(post_time_ut)) as year, MONTH(FROM_UNIXTIME(post_time_ut)) as month FROM blog_postings WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY year DESC, month DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                core.template.ParseVariables("ARCHIVES", HttpUtility.HtmlEncode(archiveTable.Rows.Count.ToString()));

                for (int i = 0; i < archiveTable.Rows.Count; i++)
                {
                    VariableCollection archiveVariableCollection = core.template.CreateChild("archive_list");

                    archiveVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(string.Format("{0} {1}",
                        Functions.IntToMonth((int)archiveTable.Rows[i]["month"]), ((int)archiveTable.Rows[i]["year"]).ToString())));

                    archiveVariableCollection.ParseVariables("URL", HttpUtility.HtmlEncode(ZzUri.BuildBlogUri(page.ProfileOwner, (int)archiveTable.Rows[i]["year"], (int)archiveTable.Rows[i]["month"])));
                }

                DataTable categoriesTable = core.db.SelectQuery(string.Format("SELECT DISTINCT YEAR(post_category) as category, category_title, category_path FROM blog_postings INNER JOIN global_categories ON post_category = category_id WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY category_title DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                core.template.ParseVariables("CATEGORIES", HttpUtility.HtmlEncode(categoriesTable.Rows.Count.ToString()));

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoryVariableCollection = core.template.CreateChild("category_list");

                    categoryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)categoriesTable.Rows[i]["category_title"]));

                    categoryVariableCollection.ParseVariables("URL", HttpUtility.HtmlEncode(ZzUri.BuildBlogUri(page.ProfileOwner, (string)categoriesTable.Rows[i]["category_path"])));
                }
            }

            if (!string.IsNullOrEmpty(category))
            {
                core.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(ZzUri.BuildBlogRssUri(page.ProfileOwner, category)));
            }
            else if (post > 0)
            {
                core.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(ZzUri.BuildBlogPostRssUri(page.ProfileOwner, year, month, post)));
            }
            else if (month > 0)
            {
                core.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(ZzUri.BuildBlogRssUri(page.ProfileOwner, year, month)));
            }
            else if (year > 0)
            {
                core.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(ZzUri.BuildBlogRssUri(page.ProfileOwner, year)));
            }
            else
            {
                core.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(ZzUri.BuildBlogRssUri(page.ProfileOwner)));
            }

            if (rss)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RssDocument));
                RssDocument doc = new RssDocument();
                doc.version = "2.0";

                doc.channels = new RssChannel[1];
                doc.channels[0] = new RssChannel();

                doc.channels[0].title = string.Format("RSS Feed for {0} blog", page.ProfileOwner.UserNameOwnership);
                doc.channels[0].description = string.Format("RSS Feed for {0} blog", page.ProfileOwner.UserNameOwnership);
                if (!string.IsNullOrEmpty(category))
                {
                    doc.channels[0].link = "http://zinzam.com" + ZzUri.BuildBlogUri(page.ProfileOwner, category);
                }
                else if (post > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + ZzUri.BuildBlogPostUri(page.ProfileOwner, year, month, post);
                }
                else if (month > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + ZzUri.BuildBlogUri(page.ProfileOwner, year, month);
                }
                else if (year > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + ZzUri.BuildBlogUri(page.ProfileOwner, year);
                }
                else
                {
                    doc.channels[0].link = "http://zinzam.com" + ZzUri.BuildBlogUri(page.ProfileOwner);
                }
                doc.channels[0].category = "Blog";

                doc.channels[0].items = new RssDocumentItem[blogEntries.Count];
                for (int i = 0; i < blogEntries.Count; i++)
                {
                    doc.channels[0].items[i] = new RssDocumentItem();
                    doc.channels[0].items[i].description = Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.session.LoggedInMember, page.ProfileOwner);
                    doc.channels[0].items[i].link = "http://zinzam.com" + ZzUri.BuildBlogPostUri(page.ProfileOwner, year, month, blogEntries[i].PostId);
                    doc.channels[0].items[i].guid = "http://zinzam.com" + ZzUri.BuildBlogPostUri(page.ProfileOwner, year, month, blogEntries[i].PostId);

                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.tz);

                    doc.channels[0].items[i].pubdate = postDateTime.ToString();

                    if (i == 0)
                    {
                        doc.channels[0].pubdate = postDateTime.ToString();
                    }
                }

                serializer.Serialize(HttpContext.Current.Response.Output, doc);
                if (core.db != null)
                {
                    core.db.CloseConnection();
                }
                HttpContext.Current.Response.End();
            }
            else
            {
                for (int i = 0; i < blogEntries.Count; i++)
                {
                    VariableCollection blogPostVariableCollection = core.template.CreateChild("blog_list");

                    blogPostVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(blogEntries[i].Title));
                    blogPostVariableCollection.ParseVariables("COMMENTS", HttpUtility.HtmlEncode(blogEntries[i].Comments.ToString()));

                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.tz);

                    string postUrl = HttpUtility.HtmlEncode(string.Format("/{0}/blog/{1}/{2:00}/{3}",
                            page.ProfileOwner.UserName, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));

                    blogPostVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(core.tz.DateTimeToString(postDateTime)));
                    blogPostVariableCollection.ParseVariables("URL", HttpUtility.HtmlEncode(postUrl));
                    blogPostVariableCollection.ParseVariables("POST", Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.session.LoggedInMember, page.ProfileOwner));
                    if (blogEntries[i].PostId == post)
                    {
                        comments = blogEntries[i].Comments;
                        core.template.ParseVariables("BLOG_POST_COMMENTS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(comments)));
                        core.template.ParseVariables("BLOGPOST_ID", HttpUtility.HtmlEncode(blogEntries[i].PostId.ToString()));

                        // TODO: something
                        //myBlog.BlogAccess.SetAccessBits(blogEntries[i].Permissions);
                        myBlog.blogAccess = new Access(core.db, blogEntries[i].Permissions, page.ProfileOwner);
                        myBlog.blogAccess.SetViewer(core.session.LoggedInMember);
                    }
                }

                if (post > 0)
                {
                    if (myBlog.BlogAccess.CanComment)
                    {
                        core.template.ParseVariables("CAN_COMMENT", "TRUE");
                    }
                    Display.DisplayComments(page, page.ProfileOwner, post, "BLOGPOST", comments);
                    core.template.ParseVariables("SINGLE", "TRUE");
                }

                string pageUri = "";
                string breadcrumbExtension = (page.ProfileOwner.ProfileHomepage == Member.HOMEPAGE_BLOG) ? "" : "blog/";

                if (!string.IsNullOrEmpty(category))
                {
                    pageUri = ZzUri.BuildBlogUri(page.ProfileOwner, category);
                    core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, string.Format("blog/{1}",
                        breadcrumbExtension, "categories/" + category)));
                }
                else if (post > 0)
                {
                    // TODO: fix this for id/title
                    core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, string.Format("blog/{1}/{2}",
                        breadcrumbExtension, year, month)));
                    pageUri = ZzUri.BuildBlogPostUri(page.ProfileOwner, year, month, post);
                }
                else if (month > 0)
                {
                    pageUri = ZzUri.BuildBlogUri(page.ProfileOwner, year, month);
                    core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, string.Format("blog/{1}/{2}",
                        breadcrumbExtension, year, month)));
                }
                else if (year > 0)
                {
                    pageUri = ZzUri.BuildBlogUri(page.ProfileOwner, year);
                    core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, string.Format("blog/{1}",
                        breadcrumbExtension, year)));
                }
                else
                {
                    pageUri = ZzUri.BuildBlogUri(page.ProfileOwner);
                    core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, string.Format("{0}",
                        breadcrumbExtension)));
                }

                if (post <= 0)
                {
                    core.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(myBlog.Entries / 10.0), true));
                }
                else
                {
                    core.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(comments / 10.0)));
                }
            }
        }
    }
}
