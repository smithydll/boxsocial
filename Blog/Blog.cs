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
    [DataTable("user_blog")]
    public class Blog : NumberedItem
    {

        /// <summary>
        /// A list of database fields associated with a blog.
        /// </summary>
        /// <remarks>
        /// A blog uses the table prefix ub.
        /// </remarks>
        public const string BLOG_FIELDS = "ub.user_id, ub.blog_title, ub.blog_entries, ub.blog_comments, ub.blog_visits, ub.blog_access";

        [DataField("user_id", DataFieldKeys.Primary)]
        private long userId;
        [DataField("blog_title", 63)]
        private string title;
        [DataField("blog_entries")]
        private long entries;
        [DataField("blog_comments")]
        private long comments;
        [DataField("blog_visits")]
        private long visits;
        [DataField("blog_access")]
        private ushort permissions;

        private User owner;
        private Access blogAccess;

        /// <summary>
        /// Gets the id of the owner of the blog.
        /// </summary>
        public long UserId
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
                if (blogAccess == null)
                {
                    blogAccess = new Access(core, permissions, Owner);
                }
                return blogAccess;
            }
        }

        public User Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.UserProfiles[userId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        /// <summary>
        /// Initialises a new instance of the Blog class.
        /// </summary>
        /// <param name="core">Core Token</param>
        /// <param name="owner">Owner whose blog to retrieve</param>
        public Blog(Core core, User owner)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Blog_ItemLoad);

            try
            {
                LoadItem(owner.Id);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBlogException();
            }
        }

        private void Blog_ItemLoad()
        {
            core.LoadUserProfile(userId);
            blogAccess = new Access(core, permissions, Owner);
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
            blogAccess = new Access(core, permissions, owner);
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
            return GetEntries(null, post, -1, -1, 1, 1, ref readAccessLevel);
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
            return GetEntries(category, -1, -1, -1, currentPage, perPage, ref readAccessLevel);
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
            return GetEntries(null, -1, year, -1, currentPage, perPage, ref readAccessLevel);
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
            return GetEntries(null, -1, year, month, currentPage, perPage, ref readAccessLevel);
        }

        /// <summary>
        /// Gets a list of draft entries in the blog fullfilling a given criteria.
        /// </summary>
        /// <param name="category">Category to select</param>
        /// <param name="post">Post id to select</param>
        /// <param name="year">Year to select</param>
        /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        internal List<BlogEntry> GetDrafts(string category, long post, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            return GetEntries(category, post, year, month, currentPage, perPage, ref readAccessLevel, true);
        }

        /// <summary>
        /// Gets a list of published entries in the blog fullfilling a given criteria.
        /// </summary>
        /// <param name="category">Category to select</param>
        /// <param name="post">Post id to select</param>
        /// <param name="year">Year to select</param>
        /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        internal List<BlogEntry> GetEntries(string category, long post, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel)
        {
            return GetEntries(category, post, year, month, currentPage, perPage, ref readAccessLevel, false);
        }

        /// <summary>
        /// Gets a list of entries in the blog fullfilling a given criteria.
        /// </summary>
        /// <param name="category">Category to select</param>
        /// <param name="post">Post id to select</param>
        /// <param name="year">Year to select</param>
        /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <param name="readAccessLevel">Access level user has to read</param>
        /// <param name="drafts">Flag to select draft posts or published posts (true for drafts)</param>
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        private List<BlogEntry> GetEntries(string category, long post, int year, int month, int currentPage, int perPage, ref ushort readAccessLevel, bool drafts)
        {
            List<BlogEntry> entries = new List<BlogEntry>();

            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);
            readAccessLevel = Owner.GetAccessLevel(core.session.LoggedInMember);

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

            string status = (drafts) ? "DRAFT" : "PUBLISH";

            DataTable blogEntriesTable = db.Query(string.Format("SELECT {7} FROM blog_postings be {4} WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = '{8}' {3} ORDER BY post_time_ut DESC LIMIT {5}, {6};",
                Owner.UserId, loggedIdUid, readAccessLevel, sqlWhere, sqlInnerJoin, (bpage - 1) * 10, 10, BlogEntry.BLOG_ENTRY_FIELDS, status));

            foreach (DataRow dr in blogEntriesTable.Rows)
            {
                entries.Add(new BlogEntry(core, owner, dr));
            }

            return entries;
        }

        /// <summary>
        /// Gets a list of entries in the blog roll for the blog
        /// </summary>
        /// <returns></returns>
        public List<BlogRollEntry> GetBlogRoll()
        {
            List<BlogRollEntry> blogRollEntries = new List<BlogRollEntry>();
            SelectQuery query = new SelectQuery("blog_roll_entries bre");
            //query.AddFields();
            query.AddCondition("bre.user_id", userId);

            DataTable blogRollTable = db.Query(query);

            foreach (DataRow dr in blogRollTable.Rows)
            {
                blogRollEntries.Add(new BlogRollEntry(core, dr));
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
                //page.template.Parse("PAGE_LIST", Display.GeneratePageList(page.ProfileOwner, core.session.LoggedInMember, true));
                Display.ParsePageList(page.ProfileOwner, true);
                page.template.Parse("U_PROFILE", page.ProfileOwner.Uri);
                page.template.Parse("U_GALLERY", Linker.BuildGalleryUri(page.ProfileOwner));
                page.template.Parse("U_FRIENDS", Linker.BuildFriendsUri(page.ProfileOwner));

                if (page.ProfileOwner.UserId == core.LoggedInMemberId)
                {
                    page.template.Parse("U_POST", AccountModule.BuildModuleUri("blog", "write"));
                }
            }

            List<BlogEntry> blogEntries = myBlog.GetEntries(category, post, year, month, p, 10, ref readAccessLevel);

            page.template.Parse("BLOGPOSTS", blogEntries.Count.ToString());

            if (!rss)
            {
                DataTable archiveTable = core.db.Query(string.Format("SELECT DISTINCT YEAR(FROM_UNIXTIME(post_time_ut)) as year, MONTH(FROM_UNIXTIME(post_time_ut)) as month FROM blog_postings WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY year DESC, month DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                page.template.Parse("ARCHIVES", archiveTable.Rows.Count.ToString());

                for (int i = 0; i < archiveTable.Rows.Count; i++)
                {
                    VariableCollection archiveVariableCollection = page.template.CreateChild("archive_list");

                    archiveVariableCollection.Parse("TITLE", string.Format("{0} {1}",
                        Functions.IntToMonth((int)archiveTable.Rows[i]["month"]), ((int)archiveTable.Rows[i]["year"]).ToString()));

                    archiveVariableCollection.Parse("URL", Blog.BuildUri(page.ProfileOwner, (int)archiveTable.Rows[i]["year"], (int)archiveTable.Rows[i]["month"]));
                }

                DataTable categoriesTable = core.db.Query(string.Format("SELECT DISTINCT YEAR(post_category) as category, category_title, category_path FROM blog_postings INNER JOIN global_categories ON post_category = category_id WHERE user_id = {0} AND (post_access & {2:0} OR user_id = {1}) AND post_status = 'PUBLISH' ORDER BY category_title DESC;",
                    page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

                page.template.Parse("CATEGORIES", categoriesTable.Rows.Count.ToString());

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoryVariableCollection = page.template.CreateChild("category_list");

                    categoryVariableCollection.Parse("TITLE", (string)categoriesTable.Rows[i]["category_title"]);

                    categoryVariableCollection.Parse("URL", Blog.BuildUri(page.ProfileOwner, (string)categoriesTable.Rows[i]["category_path"]));
                }

                List<BlogRollEntry> blogRollEntries = myBlog.GetBlogRoll();

                page.template.Parse("BLOG_ROLL_ENTRIES", blogRollEntries.Count.ToString());

                foreach (BlogRollEntry bre in blogRollEntries)
                {
                    VariableCollection breVariableCollection = page.template.CreateChild("blog_roll_list");

                    if (!string.IsNullOrEmpty(bre.Title))
                    {
                        breVariableCollection.Parse("TITLE", bre.Title);
                    }
                    else if (bre.User != null)
                    {
                        breVariableCollection.Parse("TITLE", bre.User.DisplayName);
                    }

                    breVariableCollection.Parse("URI", bre.Uri);
                }
            }

            if (!string.IsNullOrEmpty(category))
            {
                page.template.Parse("U_RSS", Linker.BuildBlogRssUri(page.ProfileOwner, category));
            }
            else if (post > 0)
            {
                page.template.Parse("U_RSS", Linker.BuildBlogPostRssUri(page.ProfileOwner, year, month, post));
            }
            else if (month > 0)
            {
                page.template.Parse("U_RSS", Linker.BuildBlogRssUri(page.ProfileOwner, year, month));
            }
            else if (year > 0)
            {
                page.template.Parse("U_RSS", Linker.BuildBlogRssUri(page.ProfileOwner, year));
            }
            else
            {
                page.template.Parse("U_RSS", Linker.BuildBlogRssUri(page.ProfileOwner));
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
                    doc.channels[0].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, category));
                }
                else if (post > 0)
                {
                    doc.channels[0].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, year, month, post));
                }
                else if (month > 0)
                {
                    doc.channels[0].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, year, month));
                }
                else if (year > 0)
                {
                    doc.channels[0].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, year));
                }
                else
                {
                    doc.channels[0].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner));
                }
                doc.channels[0].category = "Blog";

                doc.channels[0].items = new RssDocumentItem[blogEntries.Count];
                for (int i = 0; i < blogEntries.Count; i++)
                {
                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.tz);

                    doc.channels[0].items[i] = new RssDocumentItem();
                    doc.channels[0].items[i].description = Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.session.LoggedInMember, page.ProfileOwner);
                    doc.channels[0].items[i].link = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));
                    doc.channels[0].items[i].guid = Linker.StripSid(Blog.BuildAbsoluteUri(page.ProfileOwner, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));

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

                    blogPostVariableCollection.Parse("TITLE", blogEntries[i].Title);
                    blogPostVariableCollection.Parse("COMMENTS", blogEntries[i].Comments.ToString());

                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.tz);

                    string postUrl = HttpUtility.HtmlEncode(string.Format("{0}blog/{1}/{2:00}/{3}",
                            page.ProfileOwner.UriStub, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));

                    blogPostVariableCollection.Parse("DATE", core.tz.DateTimeToString(postDateTime));
                    blogPostVariableCollection.Parse("URL", postUrl);
                    //blogPostVariableCollection.ParseRaw("POST", Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.session.LoggedInMember, page.ProfileOwner));
                    Display.ParseBbcode(blogPostVariableCollection, "POST", blogEntries[i].Body, page.ProfileOwner);
                    if (blogEntries[i].PostId == post)
                    {
                        comments = blogEntries[i].Comments;
                        page.template.Parse("BLOG_POST_COMMENTS", Functions.LargeIntegerToString(comments));
                        page.template.Parse("BLOGPOST_ID", blogEntries[i].PostId.ToString());

                        myBlog.blogAccess = new Access(core,  blogEntries[i].Permissions, page.ProfileOwner);
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
                        page.template.Parse("CAN_COMMENT", "TRUE");
                    }
                    Display.DisplayComments(page.template, page.ProfileOwner, new BlogEntry(core, post));
                    page.template.Parse("SINGLE", "TRUE");
                }

                string pageUri = "";
                string breadcrumbExtension = (page.ProfileOwner.ProfileHomepage == "/blog") ? "" : "blog/";

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "blog", "Blog" });

                if (!string.IsNullOrEmpty(category))
                {
                    breadCrumbParts.Add(new string[] { "categories/" + category, year.ToString() });
                    pageUri = Blog.BuildUri(page.ProfileOwner, category);
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
                        pageUri = Blog.BuildUri(page.ProfileOwner, year, month, post);
                    }
                    else if (month > 0)
                    {
                        pageUri = Blog.BuildUri(page.ProfileOwner, year, month);
                    }
                    else if (year > 0)
                    {
                        pageUri = Blog.BuildUri(page.ProfileOwner, year);
                    }
                    else
                    {
                        pageUri = myBlog.Uri;
                    }
                }
                //page.template.Parse("BREADCRUMBS", page.ProfileOwner.GenerateBreadCrumbs(breadCrumbParts));
                page.ProfileOwner.ParseBreadCrumbs(breadCrumbParts);

                if (post <= 0)
                {
                    //page.template.ParseRaw("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(myBlog.Entries / 10.0), true));
                    Display.ParsePagination(pageUri, p, (int)Math.Ceiling(myBlog.Entries / 10.0), true);
                }
                else
                {
                    //page.template.ParseRaw("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(comments / 10.0)));
                    Display.ParsePagination(pageUri, p, (int)Math.Ceiling(comments / 10.0));
                }
            }
        }

        public override long Id
        {
            get
            {
                return userId;
            }
        }

        public override string Namespace
        {
            get
            {
                return "BLOG";
            }
        }

        public override string Uri
        {
            get
            {
                return BuildUri(owner);
            }
        }

        public static string BuildUri(User member)
        {
            if (member.ProfileHomepage == "/blog")
            {
                return Linker.AppendSid(string.Format("{0}",
                    member.UriStub));
            }
            else
            {
                return Linker.AppendSid(string.Format("{0}blog",
                    member.UriStub));
            }
        }

        public static string BuildAbsoluteUri(User member)
        {
            if (member.ProfileHomepage == "/blog")
            {
                return Linker.AppendAbsoluteSid(string.Format("{0}",
                    member.UriStubAbsolute));
            }
            else
            {
                return Linker.AppendAbsoluteSid(string.Format("{0}blog",
                    member.UriStubAbsolute));
            }
        }

        public static string BuildUri(User member, string category)
        {
            return Linker.AppendSid(string.Format("{0}blog/category/{1}",
                member.UriStub, category));
        }

        public static string BuildAbsoluteUri(User member, string category)
        {
            return Linker.AppendAbsoluteSid(string.Format("{0}blog/category/{1}",
                member.UriStubAbsolute, category));
        }

        public static string BuildUri(User member, int year)
        {
            return Linker.AppendSid(string.Format("{0}blog/{1:0000}",
                member.UriStub, year));
        }

        public static string BuildAbsoluteUri(User member, int year)
        {
            return Linker.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}",
                member.UriStubAbsolute, year));
        }

        public static string BuildUri(User member, int year, int month)
        {
            return Linker.AppendSid(string.Format("{0}blog/{1:0000}/{2:00}",
                member.UriStub, year, month));
        }

        public static string BuildAbsoluteUri(User member, int year, int month)
        {
            return Linker.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}/{2:00}",
                member.UriStubAbsolute, year, month));
        }

        public static string BuildUri(User member, int year, int month, long postId)
        {
            return Linker.AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStub, year, month, postId));
        }

        public static string BuildAbsoluteUri(User member, int year, int month, long postId)
        {
            return Linker.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStubAbsolute, year, month, postId));
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
