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

    /// <summary>
    /// Represents a user blog.
    /// </summary>
    public class Blog
    {

        /// <summary>
        /// A list of database fields associated with a blog.
        /// </summary>
        /// <remarks>
        /// A blog uses the table prefix ub.
        /// </remarks>
        public const string BLOG_FIELDS = "ub.user_id, ub.blog_title, ub.blog_entries, ub.blog_comments, ub.blog_visits, ub.blog_access";

        private Core core;
        private Mysql db;

        private int userId;
        private Primitive owner;
        private string title;
        private long entries;
        private long comments;
        private long visits;
        private ushort permissions;
        private Access blogAccess;

        /// <summary>
        /// Gets the id of the owner of the blog.
        /// </summary>
        public int UserId
        {
            get
            {
                return userId;
            }
        }

        /// <summary>
        /// Gets the title given to the blog.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
        }

        /// <summary>
        /// Gets a count of the entries posted to the blog.
        /// </summary>
        public long Entries
        {
            get
            {
                return entries;
            }
        }

        /// <summary>
        /// Gets a count of comments posted to entries in the blog.
        /// </summary>
        public long Comments
        {
            get
            {
                return comments;
            }
        }

        /// <summary>
        /// Gets a count of visits made to the blog.
        /// </summary>
        public long Visits
        {
            get
            {
                return visits;
            }
        }

        /// <summary>
        /// Gets the permission mask for the blog.
        /// </summary>
        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        /// <summary>
        /// Gets the access information (permissions) for the blog.
        /// </summary>
        public Access BlogAccess
        {
            get
            {
                return blogAccess;
            }
        }

        /// <summary>
        /// Initialises a new instance of the Blog class.
        /// </summary>
        /// <param name="db">Database object</param>
        /// <param name="owner">Owner whose blog to retrieve</param>
        public Blog(Core core, Member owner)
        {
            this.core = core;
            this.db = core.db;
            this.owner = owner;

            DataTable blogTable = db.Query(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_blog ub ON uk.user_id = ub.user_id WHERE uk.user_id = {0}",
                owner.UserId, Blog.BLOG_FIELDS));

            if (blogTable.Rows.Count == 1)
            {
                loadUserBlog(blogTable.Rows[0]);
            }
            else
            {
                throw new InvalidBlogException();
            }
        }

        /// <summary>
        /// Loads the database information into the Blog class object.
        /// </summary>
        /// <param name="blogRow">Raw database information about the blog</param>
        private void loadUserBlog(DataRow blogRow)
        {
            userId = (int)blogRow["user_id"];
            title = (string)blogRow["blog_title"];
            entries = (long)blogRow["blog_entries"];
            comments = (long)blogRow["blog_comments"];
            visits = (long)blogRow["blog_visits"];
            permissions = (ushort)blogRow["blog_access"];
            blogAccess = new Access(db, permissions, owner);
        }

        /// <summary>
        /// Creates a new blog for the logged in user.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Blog Create(Core core)
        {
            SelectQuery query = new SelectQuery("user_blog ub");
            query.AddFields("ub.user_id");
            query.AddCondition("ub.user_id", core.LoggedInMemberId);

            if (core.db.Query(query).Rows.Count == 0)
            {
                core.db.UpdateQuery(string.Format("INSERT INTO user_blog (user_id) VALUES ({0});",
                        core.LoggedInMemberId));

                return new Blog(core, core.session.LoggedInMember);
            }
            else
            {
                throw new CannotCreateBlogException();
            }
        }

        /// <summary>
        /// Gets a blog entry.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="post">Post id to get</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A blog entry as a list</returns>
        public List<BlogEntry> GetEntry(PPage page, int post, ref ushort readAccessLevel)
        {
            return GetEntries(page, null, post, -1, -1, 1, 1, ref readAccessLevel);
        }

        /// <summary>
        /// Gets a list of blog entries in a category.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(PPage page, string category, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            return GetEntries(page, category, -1, -1, -1, currentPage, perPage, ref readAccessLevel);
        }

        /// <summary>
        /// Gets a list of blog entries made in a year.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(PPage page, int year, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            return GetEntries(page, null, -1, year, -1, currentPage, perPage, ref readAccessLevel);
        }

        /// <summary>
        /// Gets a list of blog entries made in a year.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to select</param>
        /// /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(PPage page, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            return GetEntries(page, null, -1, year, month, currentPage, perPage, ref readAccessLevel);
        }

        /// <summary>
        /// Gets a list of entries in the blog fullfilling a given criteria.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to select</param>
        /// <param name="post">Post id to select</param>
        /// <param name="year">Year to select</param>
        /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        private List<BlogEntry> GetEntries(PPage page, string category, long post, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel)
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

            DataTable blogEntriesTable = db.Query(string.Format("SELECT {7} FROM blog_postings be {4} WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' {3} ORDER BY post_time_ut DESC LIMIT {5}, {6};",
                page.ProfileOwner.UserId, loggedIdUid, readAccessLevel, sqlWhere, sqlInnerJoin, (bpage - 1) * 10, 10, BlogEntry.BLOG_ENTRY_FIELDS));

            foreach (DataRow dr in blogEntriesTable.Rows)
            {
                entries.Add(new BlogEntry(core, owner, dr));
            }

            return entries;
        }

        /// <summary>
        /// Gets a list of entries in the blog roll for the blog
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public List<BlogRollEntry> GetBlogRoll(PPage page)
        {
            List<BlogRollEntry> blogRollEntries = new List<BlogRollEntry>();
            SelectQuery query = new SelectQuery("blog_roll_entries bre");
            //query.AddFields();
            query.AddCondition("bre.user_id", userId);

            DataTable blogRollTable = page.db.Query(query);

            foreach (DataRow dr in blogRollTable.Rows)
            {
                //blogRollEntries.Add(new BlogRollEntry(page.db, dr));
            }

            return blogRollEntries;
        }

        /// <summary>
        /// Show the blog
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        public static void Show(Core core, PPage page)
        {
            Show(core, page, null, -1, -1, -1);
        }

        /// <summary>
        /// Show the blog category
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to show</param>
        public static void Show(Core core, PPage page, string category)
        {
            Show(core, page, category, -1, -1, -1);
        }

        /// <summary>
        /// Show the blog entries for a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to show</param>
        public static void Show(Core core, PPage page, int year)
        {
            Show(core, page, null, -1, year, -1);
        }

        /// <summary>
        /// Show the blog entries for a month in a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to show</param>
        /// <param name="month">Month to show</param>
        public static void Show(Core core, PPage page, int year, int month)
        {
            Show(core, page, null, -1, year, month);
        }

        /// <summary>
        /// Show the blog entry
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="post">Post to show</param>
        /// <param name="year">Year to show</param>
        /// <param name="month">Month to show</param>
        public static void Show(Core core, PPage page, long post, int year, int month)
        {
            Show(core, page, null, post, year, month);
        }

        /// <summary>
        /// Show the blog
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to show</param>
        /// <param name="post">Post to show</param>
        /// <param name="year">Year to show</param>
        /// <param name="month">Month to show</param>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        private static void Show(Core core, PPage page, string category, long post, int year, int month)
        {
            page.template.SetTemplate("Blog", "viewblog");

            bool rss = false;
            long comments = 0;
            int p = 1;
            string postTitle = null;

            Blog myBlog;
            try
            {
                myBlog = new Blog(core, page.ProfileOwner);
            }
            catch (InvalidBlogException)
            {
                return;
            }

            long loggedIdUid = myBlog.BlogAccess.SetSessionViewer(core.session);
            ushort readAccessLevel = 0x0000;

            /* TODO: see what's wrong here, for not just rely on the application layer security settings */
            /*if (!myBlog.BlogAccess.CanRead)
            {
                Functions.Generate403(core);
                return;
            }*/

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
                page.template.ParseVariables("PAGE_LIST", Display.GeneratePageList(page.ProfileOwner, core.session.LoggedInMember, true));
                page.template.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode((Linker.BuildProfileUri(page.ProfileOwner))));
                page.template.ParseVariables("U_BLOG", HttpUtility.HtmlEncode((Linker.BuildBlogUri(page.ProfileOwner))));
                page.template.ParseVariables("U_GALLERY", HttpUtility.HtmlEncode((Linker.BuildGalleryUri(page.ProfileOwner))));
                page.template.ParseVariables("U_FRIENDS", HttpUtility.HtmlEncode((Linker.BuildFriendsUri(page.ProfileOwner))));

                if (page.ProfileOwner.UserId == core.LoggedInMemberId)
                {
                    page.template.ParseVariables("U_POST", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("blog", "write")));
                }
            }

            List<BlogEntry> blogEntries = myBlog.GetEntries(page, category, post, year, month, p, 10, ref readAccessLevel);

            page.template.ParseVariables("BLOGPOSTS", HttpUtility.HtmlEncode(blogEntries.Count.ToString()));

            if (!rss)
            {
                DataTable archiveTable = core.db.Query(string.Format("SELECT DISTINCT YEAR(FROM_UNIXTIME(post_time_ut)) as year, MONTH(FROM_UNIXTIME(post_time_ut)) as month FROM blog_postings WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY year DESC, month DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                page.template.ParseVariables("ARCHIVES", HttpUtility.HtmlEncode(archiveTable.Rows.Count.ToString()));

                for (int i = 0; i < archiveTable.Rows.Count; i++)
                {
                    VariableCollection archiveVariableCollection = page.template.CreateChild("archive_list");

                    archiveVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(string.Format("{0} {1}",
                        Functions.IntToMonth((int)archiveTable.Rows[i]["month"]), ((int)archiveTable.Rows[i]["year"]).ToString())));

                    archiveVariableCollection.ParseVariables("URL", HttpUtility.HtmlEncode(Linker.BuildBlogUri(page.ProfileOwner, (int)archiveTable.Rows[i]["year"], (int)archiveTable.Rows[i]["month"])));
                }

                DataTable categoriesTable = core.db.Query(string.Format("SELECT DISTINCT YEAR(post_category) as category, category_title, category_path FROM blog_postings INNER JOIN global_categories ON post_category = category_id WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY category_title DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                page.template.ParseVariables("CATEGORIES", HttpUtility.HtmlEncode(categoriesTable.Rows.Count.ToString()));

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoryVariableCollection = page.template.CreateChild("category_list");

                    categoryVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)categoriesTable.Rows[i]["category_title"]));

                    categoryVariableCollection.ParseVariables("URL", HttpUtility.HtmlEncode(Linker.BuildBlogUri(page.ProfileOwner, (string)categoriesTable.Rows[i]["category_path"])));
                }
            }

            if (!string.IsNullOrEmpty(category))
            {
                page.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(Linker.BuildBlogRssUri(page.ProfileOwner, category)));
            }
            else if (post > 0)
            {
                page.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(Linker.BuildBlogPostRssUri(page.ProfileOwner, year, month, post)));
            }
            else if (month > 0)
            {
                page.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(Linker.BuildBlogRssUri(page.ProfileOwner, year, month)));
            }
            else if (year > 0)
            {
                page.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(Linker.BuildBlogRssUri(page.ProfileOwner, year)));
            }
            else
            {
                page.template.ParseVariables("U_RSS", HttpUtility.HtmlEncode(Linker.BuildBlogRssUri(page.ProfileOwner)));
            }

            if (rss)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RssDocument));
                RssDocument doc = new RssDocument();
                doc.version = "2.0";

                doc.channels = new RssChannel[1];
                doc.channels[0] = new RssChannel();

                doc.channels[0].title = string.Format("RSS Feed for {0} blog", page.ProfileOwner.DisplayNameOwnership);
                doc.channels[0].description = string.Format("RSS Feed for {0} blog", page.ProfileOwner.DisplayNameOwnership);
                if (!string.IsNullOrEmpty(category))
                {
                    doc.channels[0].link = "http://zinzam.com" + Linker.BuildBlogUri(page.ProfileOwner, category);
                }
                else if (post > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + Linker.BuildBlogPostUri(page.ProfileOwner, year, month, post);
                }
                else if (month > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + Linker.BuildBlogUri(page.ProfileOwner, year, month);
                }
                else if (year > 0)
                {
                    doc.channels[0].link = "http://zinzam.com" + Linker.BuildBlogUri(page.ProfileOwner, year);
                }
                else
                {
                    doc.channels[0].link = "http://zinzam.com" + Linker.BuildBlogUri(page.ProfileOwner);
                }
                doc.channels[0].category = "Blog";

                doc.channels[0].items = new RssDocumentItem[blogEntries.Count];
                for (int i = 0; i < blogEntries.Count; i++)
                {
                    doc.channels[0].items[i] = new RssDocumentItem();
                    doc.channels[0].items[i].description = Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.session.LoggedInMember, page.ProfileOwner);
                    doc.channels[0].items[i].link = "http://zinzam.com" + Linker.BuildBlogPostUri(page.ProfileOwner, year, month, blogEntries[i].PostId);
                    doc.channels[0].items[i].guid = "http://zinzam.com" + Linker.BuildBlogPostUri(page.ProfileOwner, year, month, blogEntries[i].PostId);

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
                    VariableCollection blogPostVariableCollection = page.template.CreateChild("blog_list");

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
                        page.template.ParseVariables("BLOG_POST_COMMENTS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(comments)));
                        page.template.ParseVariables("BLOGPOST_ID", HttpUtility.HtmlEncode(blogEntries[i].PostId.ToString()));

                        myBlog.blogAccess = new Access(core.db, blogEntries[i].Permissions, page.ProfileOwner);
                        myBlog.blogAccess.SetViewer(core.session.LoggedInMember);
                    }

                    if (post > 0)
                    {
                        postTitle = blogEntries[i].Title;
                    }
                }

                if (post > 0)
                {
                    if (myBlog.BlogAccess.CanComment)
                    {
                        page.template.ParseVariables("CAN_COMMENT", "TRUE");
                    }
                    Display.DisplayComments(page.template, page.ProfileOwner, new BlogEntry(core, post));
                    page.template.ParseVariables("SINGLE", "TRUE");
                }

                string pageUri = "";
                string breadcrumbExtension = (page.ProfileOwner.ProfileHomepage == "/blog") ? "" : "blog/";

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "blog", "Blog" });

                if (!string.IsNullOrEmpty(category))
                {
                    breadCrumbParts.Add(new string[] { "categories/" + category, year.ToString() });
                    pageUri = Linker.BuildBlogUri(page.ProfileOwner, category);
                }
                else
                {
                    if (year > 0)
                    {
                        breadCrumbParts.Add(new string[] { year.ToString(), year.ToString() });
                    }
                    if (month > 0)
                    {
                        breadCrumbParts.Add(new string[] { month.ToString(), Functions.IntToMonth(month) });
                    }
                    if (post > 0)
                    {
                        
                        breadCrumbParts.Add(new string[] { post.ToString(), postTitle });
                    }

                    if (post > 0)
                    {
                        pageUri = Linker.BuildBlogPostUri(page.ProfileOwner, year, month, post);
                    }
                    else if (month > 0)
                    {
                        pageUri = Linker.BuildBlogUri(page.ProfileOwner, year, month);
                    }
                    else if (year > 0)
                    {
                        pageUri = Linker.BuildBlogUri(page.ProfileOwner, year);
                    }
                    else
                    {
                        pageUri = Linker.BuildBlogUri(page.ProfileOwner);
                    }
                }
                page.template.ParseVariables("BREADCRUMBS", page.ProfileOwner.GenerateBreadCrumbs(breadCrumbParts));

                if (post <= 0)
                {
                    page.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(myBlog.Entries / 10.0), true));
                }
                else
                {
                    page.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(comments / 10.0)));
                }
            }
        }
    }

    /// <summary>
    /// An exception class thrown when user does not already have a blog.
    /// </summary>
    public class InvalidBlogException : Exception
    {
    }

    /// <summary>
    /// An exception class thrown when user already has a blog.
    /// </summary>
    public class CannotCreateBlogException : Exception
    {
    }
}
