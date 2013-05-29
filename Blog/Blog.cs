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
    [DataTable("user_blog", "BLOG")]
    [Permission("VIEW", "Can view your blog", PermissionTypes.View)]
    [Permission("COMMENT_ITEMS", "Can comment on your blog entries", PermissionTypes.Interact)]
    [Permission("RATE_ITEMS", "Can rate your blog entries", PermissionTypes.Interact)]
    [Permission("POST_ITEMS", "Can post blog entries to your blog", PermissionTypes.CreateAndEdit)]
    public class Blog : NumberedItem, IPermissibleItem
    {
        [DataField("user_id", DataFieldKeys.Primary)]
        private long userId;
        [DataField("blog_title", 63)]
        private string title;
        [DataField("blog_entries")]
        private long entries;
        [DataField("blog_drafts")]
        private long drafts;
        [DataField("blog_comments")]
        private long comments;
        [DataField("blog_visits")]
        private long visits;
        [DataField("allow_trackback")]
        private bool allowTrackBack;
        [DataField("allow_pingback")]
        private bool allowPingBack;
        [DataField("blog_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access access;

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

        public long Drafts
        {
            get
            {
                return drafts;
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
        /// Gets the access information (permissions) for the blog.
        /// </summary>
        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        /// <summary>
        /// Gets the owner of the blog
        /// </summary>
        public Primitive Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.PrimitiveCache[userId];
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
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (owner == null)
            {
                throw new InvalidUserException();
            }

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

        public Blog(Core core, long userId)
            : base(core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            ItemLoad += new ItemLoadHandler(Blog_ItemLoad);

            try
            {
                LoadItem(userId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBlogException();
            }
        }

        /// <summary>
        /// ItemLoad event
        /// </summary>
        private void Blog_ItemLoad()
        {
            core.LoadUserProfile(userId);
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
        }

        /// <summary>
        /// Creates a new blog for the logged in user.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Blog Create(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = new SelectQuery("user_blog ub");
            query.AddFields("ub.user_id");
            query.AddCondition("ub.user_id", core.LoggedInMemberId);

            if (core.Db.Query(query).Rows.Count == 0)
            {
                core.Db.UpdateQuery(string.Format("INSERT INTO user_blog (user_id) VALUES ({0});",
                        core.LoggedInMemberId));

                Blog newBlog =  new Blog(core, core.Session.LoggedInMember);

                Access.CreateAllGrantsForOwner(core, newBlog);
                newBlog.Access.CreateGrantForPrimitive(Friend.FriendsGroupKey, "VIEW", "COMMENT_ITEMS", "RATE_ITEMS");
                newBlog.Access.CreateGrantForPrimitive(User.EveryoneGroupKey, "VIEW");

                return newBlog;
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
        /// <returns>A blog entry as a list</returns>
        public List<BlogEntry> GetEntry(UPage page, int post)
        {
            return GetEntries(null, null, post, -1, -1, 1, 1);
        }

        /// <summary>
        /// Gets a list of blog entries in a category.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(UPage page, string category, string tag, int currentPage, int perPage)
        {
            return GetEntries(category, tag, -1, -1, -1, currentPage, perPage);
        }

        /// <summary>
        /// Gets a list of blog entries made in a year.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(UPage page, int year, int currentPage, int perPage)
        {
            return GetEntries(null, null, -1, year, -1, currentPage, perPage);
        }

        /// <summary>
        /// Gets a list of blog entries made in a year.
        /// </summary>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to select</param>
        /// /// <param name="month">Month to select</param>
        /// <param name="currentPage">Current page</param>
        /// <param name="perPage">Number to show on each page</param>
        /// <returns>A list of blog entries</returns>
        public List<BlogEntry> GetEntry(UPage page, int year, int month, int currentPage, int perPage)
        {
            return GetEntries(null, null, -1, year, month, currentPage, perPage);
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
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        internal List<BlogEntry> GetDrafts(string category, string tag, long post, int year, int month, int currentPage, int perPage)
        {
            bool moreContent = false;
            return GetEntries(category, tag, post, year, month, currentPage, perPage, true, out moreContent);
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
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        internal List<BlogEntry> GetEntries(string category, string tag, long post, int year, int month, int currentPage, int perPage)
        {
            bool moreContent = false;
            return GetEntries(category, tag, post, year, month, currentPage, perPage, false, out moreContent);
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
        /// <param name="drafts">Flag to select draft posts or published posts (true for drafts)</param>
        /// <returns>A list of blog entries</returns>
        /// <remarks>A number of conditions may be omitted. Integer values can be omitted by passing -1. String values by passing a null or empty string.</remarks>
        private List<BlogEntry> GetEntries(string category, string tag, long post, int year, int month, int currentPage, int perPage, bool drafts, out bool moreContent)
        {
            List<BlogEntry> entries = new List<BlogEntry>();
            moreContent = false;

            long loggedIdUid = User.GetMemberId(core.Session.LoggedInMember);

            SelectQuery query = null;

            if (string.IsNullOrEmpty(category) && string.IsNullOrEmpty(tag))
            {
                query = Item.GetSelectQueryStub(typeof(BlogEntry));
                query.AddField(new DataField(typeof(BlogEntry), "post_id"));

                if (post > 0)
                {
                    query.AddCondition("post_id", post);
                }

                if (year > 0)
                {
                    query.AddCondition("YEAR(FROM_UNIXTIME(post_time_ut))", year);
                }

                if (month > 0)
                {
                    query.AddCondition("MONTH(FROM_UNIXTIME(post_time_ut))", month);
                }
            }
            else if (string.IsNullOrEmpty(tag))
            {
                Item.GetSelectQueryStub(typeof(BlogEntry));
                query.AddField(new DataField(typeof(BlogEntry), "post_id"));

                query.AddCondition("category_path", category);
                query.AddJoin(JoinTypes.Inner, "global_categories", "post_category", "category_id");
            }
            else
            {
                query = Item.GetSelectQueryStub(typeof(ItemTag));
                query.AddFields(Item.GetFieldsPrefixed(typeof(BlogEntry)));

                query.AddJoin(JoinTypes.Inner, new DataField(typeof(ItemTag), "item_id"), new DataField(typeof(BlogEntry), "post_id"));
                query.AddCondition("item_type_id", ItemType.GetTypeId(typeof(BlogEntry)));
                query.AddCondition("tag_text_normalised", tag);
            }

            int bpage = currentPage;
            if (post > 0)
            {
                bpage = 1;
            }

            int limitStart = (bpage - 1) * perPage;

            PublishStatuses status = (drafts) ? PublishStatuses.Draft : PublishStatuses.Publish;

            query.AddCondition("post_status", (byte)status);
            query.AddCondition("user_id", UserId);
            query.AddSort(SortOrder.Descending, "post_time_ut");
            /*query.LimitStart = (bpage - 1) * perPage;
            query.LimitCount = perPage;*/

            DataTable blogEntriesTable = db.Query(query);

            /* Check ACLs, not the most efficient, but will only check mostly newer content which should be viewed first. */

            int offset = 0;
            int i = 0;

            while (i < limitStart + perPage + 1 && offset < blogEntriesTable.Rows.Count)
            {
                List<IPermissibleItem> tempBlogEntries = new List<IPermissibleItem>();
                int j = 0;
                for (j = offset; j < Math.Min(offset + perPage * 2, blogEntriesTable.Rows.Count); j++)
                {
                    BlogEntry entry = new BlogEntry(core, owner, blogEntriesTable.Rows[j]);
                    tempBlogEntries.Add(entry);
                }

                if (tempBlogEntries.Count > 0)
                {
                    core.AcessControlCache.CacheGrants(tempBlogEntries);

                    foreach (IPermissibleItem entry in tempBlogEntries)
                    {
                        if (entry.Access.Can("VIEW"))
                        {
                            if (i >= limitStart + perPage)
                            {
                                moreContent = true;
                                break;
                            }
                            if (i >= limitStart)
                            {
                                entries.Add((BlogEntry)entry);
                            }
                            i++;
                        }
                    }
                }
                else
                {
                    break;
                }

                offset = j;
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

        public List<TrackBack> GetTrackBacksUnapproved(int currentPage, int perPage)
        {
            return GetTrackBacksAll(currentPage, perPage, TrackBackApprovalStatus.Unapproved);
        }

        public List<TrackBack> GetTrackBacksApproved(int currentPage, int perPage)
        {
            return GetTrackBacksAll(currentPage, perPage, TrackBackApprovalStatus.Approved);
        }

        public List<TrackBack> GetTrackBacksAll(int currentPage, int perPage)
        {
            return GetTrackBacksAll(currentPage, perPage, TrackBackApprovalStatus.Any);
        }

        private List<TrackBack> GetTrackBacksAll(int currentPage, int perPage, TrackBackApprovalStatus approval)
        {
            List<TrackBack> trackBacks = new List<TrackBack>();

            SelectQuery query = TrackBack.GetSelectQueryStub(typeof(TrackBack));
            query.AddCondition("blog_id", Id);
            query.AddCondition("trackback_spam", false);
            query.LimitStart = (currentPage - 1) * perPage;
            query.LimitCount = perPage;

            if (approval == TrackBackApprovalStatus.Approved)
            {
                query.AddCondition("trackback_approved", true);
            }
            else if (approval == TrackBackApprovalStatus.Unapproved)
            {
                query.AddCondition("trackback_approved", false);
            }

            DataTable trackBacksTable = db.Query(query);

            foreach (DataRow dr in trackBacksTable.Rows)
            {
                trackBacks.Add(new TrackBack(core, this, dr));
            }

            return trackBacks;
        }

        public List<PingBack> GetPingBacks(int currentPage, int perPage)
        {
            List<PingBack> pingBacks = new List<PingBack>();

            SelectQuery query = PingBack.GetSelectQueryStub(typeof(PingBack));
            query.AddCondition("blog_id", Id);
            query.LimitStart = (currentPage - 1) * perPage;
            query.LimitCount = perPage;

            DataTable pingBacksTable = db.Query(query);

            foreach (DataRow dr in pingBacksTable.Rows)
            {
                pingBacks.Add(new PingBack(core, this, dr));
            }

            return pingBacks;
        }

        /// <summary>
        /// Show the blog
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        public static void Show(Core core, UPage page)
        {
            Show(core, page, null, null, -1, -1, -1);
        }

        /// <summary>
        /// Show the blog category
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="category">Category to show</param>
        public static void Show(Core core, UPage page, string category)
        {
            Show(core, page, category, null, -1, -1, -1);
        }

        /// <summary>
        /// Show the blog entries for a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to show</param>
        public static void Show(Core core, UPage page, int year)
        {
            Show(core, page, null, null, -1, year, -1);
        }

        /// <summary>
        /// Show the blog entries for a month in a year
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="year">Year to show</param>
        /// <param name="month">Month to show</param>
        public static void Show(Core core, UPage page, int year, int month)
        {
            Show(core, page, null, null, -1, year, month);
        }

        /// <summary>
        /// Show the blog entry
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="page">Page calling</param>
        /// <param name="post">Post to show</param>
        /// <param name="year">Year to show</param>
        /// <param name="month">Month to show</param>
        public static void Show(Core core, UPage page, long post, int year, int month)
        {
            Show(core, page, null, null, post, year, month);
        }

        /// <summary>
        /// Show the blog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Show(object sender, ShowBlogEventArgs e)
        {
            switch (e.Display)
            {
                case BlogDisplayType.All:
                    Show(e.Core, (UPage)e.Page);
                    break;
                case BlogDisplayType.Year:
                    Show(e.Core, (UPage)e.Page, e.Year);
                    break;
                case BlogDisplayType.Month:
                    Show(e.Core, (UPage)e.Page, e.Year, e.Month);
                    break;
                case BlogDisplayType.Category:
                    Show(e.Core, (UPage)e.Page, e.Category, null, -1, -1, -1);
                    break;
                case BlogDisplayType.Tag:
                    Show(e.Core, (UPage)e.Page, null, e.Tag, -1, -1, -1);
                    break;
                case BlogDisplayType.Post:
                    Show(e.Core, (UPage)e.Page, e.ItemId, e.Year, e.Month);
                    break;
            }
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
        private static void Show(Core core, UPage page, string category, string tag, long post, int year, int month)
        {
            page.template.SetTemplate("Blog", "viewblog");

            bool rss = false;
            long comments = 0;
            string postTitle = null;

            Blog myBlog;
            try
            {
                myBlog = new Blog(core, page.User);
            }
            catch (InvalidBlogException)
            {
                return;
            }

            if (!myBlog.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            try
            {
                rss = bool.Parse(core.Http.Query["rss"]);
            }
            catch { }

            /*if (rss)
            {
                core.Http.SwitchContextType("text/xml");
            }*/

            page.User.LoadProfileInfo();

            bool moreContent = false;
            List<BlogEntry> blogEntries = myBlog.GetEntries(category, tag, post, year, month, page.TopLevelPageNumber, 10, false, out moreContent);

            if (!rss)
            {
                core.Display.ParsePageList(page.User, true);
                page.template.Parse("U_PROFILE", page.User.Uri);
                page.template.Parse("U_FRIENDS", core.Uri.BuildFriendsUri(page.User));

                page.template.Parse("USER_THUMB", page.User.UserThumbnail);
                page.template.Parse("USER_COVER_PHOTO", page.User.CoverPhoto);

                if (page.User.UserId == core.LoggedInMemberId)
                {
                    page.template.Parse("U_POST", core.Uri.BuildAccountSubModuleUri(myBlog.Owner, "blog", "write"));
                }
            }

            if (!rss)
            {
                DataTable archiveTable = core.Db.Query(string.Format("SELECT DISTINCT YEAR(FROM_UNIXTIME(post_time_ut)) as year, MONTH(FROM_UNIXTIME(post_time_ut)) as month FROM blog_postings WHERE user_id = {0} AND post_status = 'PUBLISH' ORDER BY year DESC, month DESC;",
                    page.User.UserId, core.LoggedInMemberId));

                page.template.Parse("ARCHIVES", archiveTable.Rows.Count.ToString());

                for (int i = 0; i < archiveTable.Rows.Count; i++)
                {
                    VariableCollection archiveVariableCollection = page.template.CreateChild("archive_list");

                    archiveVariableCollection.Parse("TITLE", string.Format("{0} {1}",
                        core.Functions.IntToMonth((int)archiveTable.Rows[i]["month"]), ((int)archiveTable.Rows[i]["year"]).ToString()));

                    archiveVariableCollection.Parse("URL", Blog.BuildUri(core, page.User, (int)archiveTable.Rows[i]["year"], (int)archiveTable.Rows[i]["month"]));
                }

                DataTable categoriesTable = core.Db.Query(string.Format("SELECT DISTINCT post_category, category_title, category_path FROM blog_postings INNER JOIN global_categories ON post_category = category_id WHERE user_id = {0} AND post_status = 'PUBLISH' ORDER BY category_title DESC;",
                    page.User.UserId, core.LoggedInMemberId));

                page.template.Parse("CATEGORIES", categoriesTable.Rows.Count.ToString());

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoryVariableCollection = page.template.CreateChild("category_list");

                    categoryVariableCollection.Parse("TITLE", (string)categoriesTable.Rows[i]["category_title"]);

                    categoryVariableCollection.Parse("URL", Blog.BuildUri(core, page.User, (string)categoriesTable.Rows[i]["category_path"]));
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
                page.template.Parse("U_RSS", core.Uri.BuildBlogRssUri(page.User, category));
            }
            else if (post > 0)
            {
                page.template.Parse("U_RSS", core.Uri.BuildBlogPostRssUri(page.User, year, month, post));
            }
            else if (month > 0)
            {
                page.template.Parse("U_RSS", core.Uri.BuildBlogRssUri(page.User, year, month));
            }
            else if (year > 0)
            {
                page.template.Parse("U_RSS", core.Uri.BuildBlogRssUri(page.User, year));
            }
            else
            {
                page.template.Parse("U_RSS", core.Uri.BuildBlogRssUri(page.User));
            }

            if (rss)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RssDocument));
                RssDocument doc = new RssDocument();
                doc.version = "2.0";

                doc.channels = new RssChannel[1];
                doc.channels[0] = new RssChannel();

                doc.channels[0].title = string.Format("RSS Feed for {0} blog", page.User.DisplayNameOwnership);
                doc.channels[0].description = string.Format("RSS Feed for {0} blog", page.User.DisplayNameOwnership);
                if (!string.IsNullOrEmpty(category))
                {
                    doc.channels[0].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, category));
                }
                else if (post > 0)
                {
                    doc.channels[0].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, year, month, post));
                }
                else if (month > 0)
                {
                    doc.channels[0].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, year, month));
                }
                else if (year > 0)
                {
                    doc.channels[0].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, year));
                }
                else
                {
                    doc.channels[0].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User));
                }
                doc.channels[0].category = "Blog";

                doc.channels[0].items = new RssDocumentItem[blogEntries.Count];
                for (int i = 0; i < blogEntries.Count; i++)
                {
                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.Tz);

                    doc.channels[0].items[i] = new RssDocumentItem();
                    doc.channels[0].items[i].description = core.Bbcode.Parse(HttpUtility.HtmlEncode(blogEntries[i].Body), core.Session.LoggedInMember, page.User);
                    doc.channels[0].items[i].link = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));
                    doc.channels[0].items[i].guid = core.Uri.StripSid(Blog.BuildAbsoluteUri(core, page.User, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));

                    doc.channels[0].items[i].pubdate = postDateTime.ToString();

                    if (i == 0)
                    {
                        doc.channels[0].pubdate = postDateTime.ToString();
                    }
                }

                core.Http.WriteXml(serializer, doc);
                if (core.Db != null)
                {
                    core.Db.CloseConnection();
                }
                core.Http.End();
            }
            else
            {
                int postsOnPage = 0;


                for (int i = 0; i < blogEntries.Count; i++)
                {
                    postsOnPage++;
                    VariableCollection blogPostVariableCollection = page.template.CreateChild("blog_list");

                    blogPostVariableCollection.Parse("TITLE", blogEntries[i].Title);

                    DateTime postDateTime = blogEntries[i].GetCreatedDate(core.Tz);

                    string postUrl = HttpUtility.HtmlEncode(string.Format("{0}blog/{1}/{2:00}/{3}",
                            page.User.UriStub, postDateTime.Year, postDateTime.Month, blogEntries[i].PostId));

                    blogPostVariableCollection.Parse("DATE", core.Tz.DateTimeToString(postDateTime));
                    blogPostVariableCollection.Parse("URL", postUrl);
                    blogPostVariableCollection.Parse("ID", blogEntries[i].Id);
                    blogPostVariableCollection.Parse("TYPE_ID", blogEntries[i].ItemKey.TypeId);

                    core.Display.ParseBbcode(blogPostVariableCollection, "POST", blogEntries[i].Body, page.User);

                    if (core.Session.IsLoggedIn)
                    {
                        if (blogEntries[i].Owner.IsItemOwner(core.Session.LoggedInMember))
                        {
                            blogPostVariableCollection.Parse("IS_OWNER", "TRUE");
                            blogPostVariableCollection.Parse("U_DELETE", blogEntries[i].DeleteUri);
                        }
                    }

                    if (blogEntries[i].Info.Likes > 0)
                    {
                        blogPostVariableCollection.Parse("LIKES", string.Format(" {0:d}", blogEntries[i].Info.Likes));
                        blogPostVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", blogEntries[i].Info.Dislikes));
                    }

                    if (blogEntries[i].Info.Comments > 0)
                    {
                        blogPostVariableCollection.Parse("POST_COMMENTS", string.Format(" ({0:d})", blogEntries[i].Info.Comments));
                    }

                    if (blogEntries[i].Access.IsPublic())
                    {
                        blogPostVariableCollection.Parse("SHAREABLE", "TRUE");
                        blogPostVariableCollection.Parse("PUBLIC", "TRUE");
                    }

                    if (blogEntries[i].PostId == post)
                    {
                        comments = blogEntries[i].Comments;
                        page.template.Parse("BLOG_POST_COMMENTS", core.Functions.LargeIntegerToString(comments));
                        page.template.Parse("BLOGPOST_ID", blogEntries[i].PostId.ToString());
                    }

                    if (post > 0)
                    {
                        postTitle = blogEntries[i].Title;
                    }

                }

                if (post > 0)
                {
                    if (postsOnPage != 1)
                    {
                        core.Functions.Generate404();
                        return;
                    }

                    //if (myBlog.Access.Can("COMMENT_ITEMS"))
                    {
                        if (blogEntries[0].Access.Can("COMMENT"))
                        {
                            page.template.Parse("CAN_COMMENT", "TRUE");
                        }
                    }
                    core.Display.DisplayComments(page.template, page.User, blogEntries[0]);
                    page.template.Parse("SINGLE", "TRUE");
                }

                page.template.Parse("BLOGPOSTS", postsOnPage.ToString());

                string pageUri = "";
                string breadcrumbExtension = (page.User.UserInfo.ProfileHomepage == "/blog") ? "" : "blog/";

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "blog", "Blog" });

                if (!string.IsNullOrEmpty(category))
                {
                    Category cat = new Category(core, category);
                    breadCrumbParts.Add(new string[] { "categories/" + category, cat.Title });
                    pageUri = Blog.BuildUri(core, page.User, category);
                }
                else if (!string.IsNullOrEmpty(tag))
                {
                    Tag currentTag = new Tag(core, tag);
                    breadCrumbParts.Add(new string[] { "tag/" + tag, currentTag.TagText });
                    pageUri = Blog.BuildTagUri(core, page.User, tag);
                }
                else
                {
                    if (year > 0)
                    {
                        breadCrumbParts.Add(new string[] { year.ToString(), year.ToString() });
                    }
                    if (month > 0)
                    {
                        breadCrumbParts.Add(new string[] { month.ToString(), core.Functions.IntToMonth(month) });
                    }
                    if (post > 0)
                    {

                        breadCrumbParts.Add(new string[] { post.ToString(), postTitle });
                    }

                    if (post > 0)
                    {
                        pageUri = Blog.BuildUri(core, page.User, year, month, post);
                    }
                    else if (month > 0)
                    {
                        pageUri = Blog.BuildUri(core, page.User, year, month);
                    }
                    else if (year > 0)
                    {
                        pageUri = Blog.BuildUri(core, page.User, year);
                    }
                    else
                    {
                        pageUri = myBlog.Uri;
                    }
                }

                page.User.ParseBreadCrumbs(breadCrumbParts);

                if (post <= 0)
                {
                    core.Display.ParsePagination(pageUri, page.TopLevelPageNumber, page.TopLevelPageNumber + (moreContent ? 1 : 0), PaginationOptions.Blog);
                }
                else
                {
                    core.Display.ParsePagination(pageUri, page.TopLevelPageNumber, (int)Math.Ceiling(comments / 10.0));
                }

                page.CanonicalUri = pageUri;
            }
        }

        /// <summary>
        /// Gets the blog.
        /// </summary>
        public override long Id
        {
            get
            {
                return userId;
            }
        }

        /// <summary>
        /// Gets the URI of the blog.
        /// </summary>
        public override string Uri
        {
            get
            {
                return BuildUri(core, owner);
            }
        }

        /// <summary>
        /// Build the blog URI.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="member">The owner of the blog</param>
        /// <returns>The URI</returns>
        public static string BuildUri(Core core, User member)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (member.UserInfo.ProfileHomepage == "/blog")
            {
                return core.Uri.AppendSid(string.Format("{0}",
                    member.UriStub));
            }
            else
            {
                return core.Uri.AppendSid(string.Format("{0}blog",
                    member.UriStub));
            }
        }

        /// <summary>
        /// Build an absolute URI to a blog.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="member">The owner of the blog</param>
        /// <returns>The URI</returns>
        public static string BuildAbsoluteUri(Core core, User member)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (member.UserInfo.ProfileHomepage == "/blog")
            {
                return core.Uri.AppendAbsoluteSid(string.Format("{0}",
                    member.UriStubAbsolute));
            }
            else
            {
                return core.Uri.AppendAbsoluteSid(string.Format("{0}blog",
                    member.UriStubAbsolute));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string BuildUri(Core core, User member, string category)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendSid(string.Format("{0}blog/category/{1}",
                member.UriStub, category));
        }

        public static string BuildTagUri(Core core, User member, string tag)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendSid(string.Format("{0}blog/tag/{1}",
                member.UriStub, tag));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string BuildAbsoluteUri(Core core, User member, string category)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("{0}blog/category/{1}",
                member.UriStubAbsolute, category));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static string BuildUri(Core core, User member, int year)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendSid(string.Format("{0}blog/{1:0000}",
                member.UriStub, year));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static string BuildAbsoluteUri(Core core, User member, int year)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}",
                member.UriStubAbsolute, year));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static string BuildUri(Core core, User member, int year, int month)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendSid(string.Format("{0}blog/{1:0000}/{2:00}",
                member.UriStub, year, month));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static string BuildAbsoluteUri(Core core, User member, int year, int month)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}/{2:00}",
                member.UriStubAbsolute, year, month));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public static string BuildUri(Core core, User member, int year, int month, long postId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStub, year, month, postId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="member"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public static string BuildAbsoluteUri(Core core, User member, int year, int month, long postId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStubAbsolute, year, month, postId));
        }
        
        /// <summary>
        /// 
        /// </summary>
        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(userId, typeof(User));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayTitle
        {
            get
            {
                return "Blog: " + Owner.DisplayName + " (" + Owner.Key + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            switch (permission)
            {
                case "COMMENT_ITEMS":
                    return "COMMENT";
                default:
                    return permission;
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

    public enum BlogDisplayType
    {
        All,
        Category,
        Tag,
        Year,
        Month,
        Post,
    }

    public class ShowBlogEventArgs : ShowPPageEventArgs
    {
        private BlogDisplayType display;
        private short year;
        private short month;
        private string category;
        private string tag;

        public BlogDisplayType Display
        {
            get
            {
                return display;
            }
        }

        public short Year
        {
            get
            {
                return year;
            }
        }

        public short Month
        {
            get
            {
                return month;
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
        }

        public string Tag
        {
            get
            {
                return tag;
            }
        }

        public ShowBlogEventArgs(PPage page)
            : base (page)
        {
            this.display = BlogDisplayType.All;
        }

        public ShowBlogEventArgs(PPage page, BlogDisplayType display, string key)
            : base(page)
        {
            if (display == BlogDisplayType.Category)
            {
                this.category = key;
            }
            else if (display == BlogDisplayType.Tag)
            {
                this.tag = key;
            }
            this.display = display;
        }

        public ShowBlogEventArgs(PPage page, short year)
            : base(page)
        {
            this.year = year;
            this.display = BlogDisplayType.Year;
        }

        public ShowBlogEventArgs(PPage page, short year, short month)
            : base(page)
        {
            this.year = year;
            this.month = month;
            this.display = BlogDisplayType.Month;
        }

        public ShowBlogEventArgs(PPage page, long itemId)
            : base(page, itemId)
        {
            this.display = BlogDisplayType.Post;
        }
    }
}

