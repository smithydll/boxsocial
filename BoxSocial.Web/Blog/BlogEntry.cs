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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Blog
{

    /// <summary>
    /// 
    /// </summary>
    public enum PublishStatuses : byte
    {
        /// <summary>
        /// 
        /// </summary>
        /// Unpublished = 0x00,
        /// <summary>
        /// 
        /// </summary>
        Published = 0x00,
        /// <summary>
        /// 
        /// </summary>
        Draft = 0x01,
        /// <summary>
        /// 
        /// </summary>
        Queued = 0x02,
    }

    /// <summary>
    /// Represents a blog entry
    /// </summary>
    [DataTable("blog_postings", "BLOGPOST")]
    [Permission("VIEW", "Can view this blog entry", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on this blog entry", PermissionTypes.Interact)]
    [Permission("RATE", "Can rate this blog entry", PermissionTypes.Interact)]
    [JsonObject("blog_post")]
    public class BlogEntry : NumberedItem, ICommentableItem, IPermissibleItem, ILikeableItem, ISearchableItem, IShareableItem, IActionableItem
    {
        /// <summary>
        /// A list of database fields associated with a blog entry.
        /// </summary>
        /// <remarks>
        /// A blog entry uses the table prefix be.
        /// </remarks>

        [DataField("post_id", DataFieldKeys.Primary)]
        private long postId;
        [DataField("user_id", typeof(User))]
        private long ownerId;
        [DataField("post_title", 127)]
        private string title;
        [DataField("post_text", MYSQL_MEDIUM_TEXT)]
        private string body;
        [DataField("post_text_cache", MYSQL_MEDIUM_TEXT)]
        private string bodyCache;
        [DataField("post_views")]
        private long views;
        [DataField("post_trackbacks")]
        private long trackbacks;
        [DataField("post_status")]
        private byte status;
        [DataField("post_license")]
        private byte license;
        [DataField("post_category", DataFieldKeys.Index)]
        private short category;
        [DataField("post_guid", 255)]
        private string guid;
        [DataField("post_ip", 50)]
        private string ip;
        [DataField("post_time_ut")]
        private long createdRaw;
        [DataField("post_published_ut")]
        private long publishedRaw;
        [DataField("post_modified_ut")]
        private long modifiedRaw;
        [DataField("post_allow_comment")]
        private bool allowComment;
        [DataField("post_simple_permissions")]
        private bool simplePermissions;
        [DataField("post_gallery_id", typeof(Gallery.Gallery))]
        private long galleryId;
        [DataField("post_claim_id")]
        private long claimId;
        [DataField("post_claim_ut")]
        private long claimTime;
        [DataField("post_claimed")]
        private bool claimed;

        private User author;
        private Primitive owner;
        private Blog blog;
        private Access access;

        public event CommentHandler OnCommentPosted;

        /// <summary>
        /// Gets the blog entry id
        /// </summary>
        [JsonProperty("id")]
        public long PostId
        {
            get
            {
                return postId;
            }
        }

        /// <summary>
        /// Gets the id of the 
        /// </summary>
        [JsonIgnore]
        public long OwnerId
        {
            get
            {
                return ownerId;
            }
        }

        /// <summary>
        /// Gets the author of the blog post
        /// </summary>
        public User Author
        {
            get
            {
                if (author == null || ownerId != owner.Id)
                {
                    core.LoadUserProfile(ownerId);
                    owner = author = (User)core.PrimitiveCache[ownerId];
                    return author;
                }
                else
                {
                    return author;
                }

            }
        }

        /// <summary>
        /// Gets the owner of the blog post
        /// </summary>
        [JsonProperty("owner")]
        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id)
                {
                    core.LoadUserProfile(ownerId);
                    owner = core.PrimitiveCache[ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }

            }
        }

        /// <summary>
        /// Gets the blog for the blog post
        /// </summary>
        [JsonIgnore]
        public Blog Blog
        {
            get
            {
                if (blog == null || blog.Id != ownerId)
                {
                    if (ownerId != 0)
                    {
                        blog = new Blog(core, ownerId);
                    }
                    else if (ownerId > 0 && Owner is User)
                    {
                        blog = new Blog(core, (User)Owner);
                    }
                    else
                    {
                        throw new InvalidBlogException();
                    }
                }
                return blog;
            }
        }

        /// <summary>
        /// Gets the title of the blog post
        /// </summary>
        [JsonProperty("title")]
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
            }
        }

        /// <summary>
        /// Gets the body of the blog post
        /// </summary>
        /// <remarks>
        /// BBcode encoded field
        /// </remarks>
        [JsonProperty("body")]
        public string Body
        {
            get
            {
                return body;
            }
            set
            {
                SetProperty("body", value);
            }
        }

        [JsonIgnore]
        public string BodyCache
        {
            get
            {
                return bodyCache;
            }
            set
            {
                SetProperty("bodyCache", value);
            }
        }

        /// <summary>
        /// Gets the number of views for the blog entry.
        /// </summary>
        [JsonIgnore]
        public long Views
        {
            get
            {
                return views;
            }
        }

        /// <summary>
        /// Gets the number of trackbacks for the blog entry.
        /// </summary>
        [JsonProperty("trackbacks")]
        public long Trackbacks
        {
            get
            {
                return trackbacks;
            }
        }

        /// <summary>
        /// Gets the number of comments for the blog entry.
        /// </summary>
        [JsonIgnore]
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        /// <summary>
        /// Gets the category Id for the blog entry.
        /// </summary>
        [JsonIgnore]
        public short Category
        {
            get
            {
                return category;
            }
            set
            {
                SetProperty("category", value);
            }
        }

        /// <summary>
        /// Gets the status of the blog post.
        /// </summary>
        [JsonIgnore]
        public PublishStatuses Status
        {
            get
            {
                return (PublishStatuses)status;
            }
            set
            {
                SetProperty("status", (byte)value);
            }
        }

        /// <summary>
        /// Gets the license for the blog post.
        /// </summary>
        [JsonIgnore]
        public byte License
        {
            get
            {
                return license;
            }
            set
            {
                SetProperty("license", value);
            }
        }

        /// <summary>
        /// Gets the GUID for the blog post.
        /// </summary>
        [JsonIgnore]
        public string Guid
        {
            get
            {
                return guid;
            }
            set
            {
                SetProperty("guid", value);
            }
        }

        [JsonIgnore]
        public long CreatedDateRaw
        {
            get
            {
                return createdRaw;
            }
            set
            {
                SetProperty("createdRaw", value);
            }
        }

        [JsonProperty("published_date_ut")]
        public long PublishedDateRaw
        {
            get
            {
                return publishedRaw;
            }
            set
            {
                SetProperty("publishedRaw", value);
            }
        }

        [JsonProperty("modified_date_ut")]
        public long ModifiedDateRaw
        {
            get
            {
                return modifiedRaw;
            }
            set
            {
                SetProperty("modifiedRaw", value);
            }
        }

        /// <summary>
        /// Gets the date the blog post was made.
        /// </summary>
        /// <param name="tz">Timezone</param>
        /// <returns>DateTime object</returns>
        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        /// <summary>
        /// Gets the date the blog post was published.
        /// </summary>
        /// <param name="tz">Timezone</param>
        /// <returns>DateTime object</returns>
        public DateTime GetPublishedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(publishedRaw);
        }

        /// <summary>
        /// Gets the date the blog post was last modified.
        /// </summary>
        /// <param name="tz">Timezone</param>
        /// <returns>DateTime object</returns>
        public DateTime GetModifiedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        /// <summary>
        /// Initialises a new instance of the BlogEntry class.
        /// </summary>
        /// <param name="core">Core Token</param>
        /// <param name="postId">Post Id to retrieve</param>
        public BlogEntry(Core core, long postId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogEntry_ItemLoad);

            try
            {
                LoadItem(postId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBlogEntryException();
            }
        }

        public BlogEntry(Core core, DataRow postEntryRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogEntry_ItemLoad);

            loadItemInfo(postEntryRow);
        }

        public BlogEntry(Core core, Blog blog, Primitive owner, DataRow postEntryRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogEntry_ItemLoad);

            this.blog = blog;
            this.owner = owner;

            loadItemInfo(postEntryRow);
        }

        public BlogEntry(Core core, Blog blog, Primitive owner, System.Data.Common.DbDataReader postEntryReader)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogEntry_ItemLoad);

            this.blog = blog;
            this.owner = owner;

            loadItemInfo(postEntryReader);
        }

        /// <summary>
        /// Initialses a new instance of the BlogEntry class.
        /// </summary>
        /// <param name="core">Core Token</param>
        /// <param name="owner">Owner whose blog post has been retrieved</param>
        /// <param name="postEntryRow">Raw data row of blog entry</param>
        public BlogEntry(Core core, Primitive owner, DataRow postEntryRow) : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogEntry_ItemLoad);

            this.owner = owner;

            loadItemInfo(postEntryRow);
        }

        protected override void loadItemInfo(DataRow postEntryRow)
        {
            loadValue(postEntryRow, "post_id", out postId);
            loadValue(postEntryRow, "user_id", out ownerId);
            loadValue(postEntryRow, "post_title", out title);
            loadValue(postEntryRow, "post_text", out body);
            loadValue(postEntryRow, "post_text_cache", out bodyCache);
            loadValue(postEntryRow, "post_views", out views);
            loadValue(postEntryRow, "post_trackbacks", out trackbacks);
            loadValue(postEntryRow, "post_status", out status);
            loadValue(postEntryRow, "post_license", out license);
            loadValue(postEntryRow, "post_category", out category);
            loadValue(postEntryRow, "post_guid", out guid);
            loadValue(postEntryRow, "post_ip", out ip);
            loadValue(postEntryRow, "post_time_ut", out createdRaw);
            loadValue(postEntryRow, "post_published_ut", out publishedRaw);
            loadValue(postEntryRow, "post_modified_ut", out modifiedRaw);
            loadValue(postEntryRow, "post_allow_comment", out allowComment);
            loadValue(postEntryRow, "post_simple_permissions", out simplePermissions);
            loadValue(postEntryRow, "post_gallery_id", out galleryId);
            loadValue(postEntryRow, "post_claim_id", out claimId);
            loadValue(postEntryRow, "post_claim_ut", out claimTime);
            loadValue(postEntryRow, "post_claimed", out claimed);

            itemLoaded(postEntryRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        /// <summary>
        /// ItemLoad event
        /// </summary>
        void BlogEntry_ItemLoad()
        {
            ItemUpdated += new EventHandler(BlogEntry_ItemUpdated);
            ItemDeleted += new ItemDeletedEventHandler(BlogEntry_ItemDeleted);
            OnCommentPosted += new CommentHandler(BlogEntry_CommentPosted);
        }

        bool BlogEntry_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                core.CallingApplication.QueueNotifications(core, e.Comment.ItemKey, "notifyBlogComment");
                /*core.CallingApplication.SendNotification(core, (User)Owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your blog.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                    e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));*/
            }

            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        /// <summary>
        /// Increment the number of views
        /// </summary>
        /// <param name="viewer">Person viewing the gallery item</param>
        public void Viewed(User viewer)
        {
            if (viewer != null)
            {
                if (owner is User)
                {
                    if (viewer.UserId == ((User)owner).UserId)
                    {
                        return;
                    }
                }
                db.UpdateQuery(string.Format("UPDATE blog_postings SET post_views = post_views + 1 WHERE post_id = {0};",
                    postId));
                // otherwise just update the view count
            }
            return;
        }

        public static void NotifyBlogComment(Core core, Job job)
        {
            Comment comment = new Comment(core, job.ItemId);
            BlogEntry ev = new BlogEntry(core, comment.CommentedItemKey.Id);

            if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.OwnerKey)))
            {
                core.CallingApplication.SendNotification(core, comment.User, (User)ev.Owner, ev.OwnerKey, ev.ItemKey, "_COMMENTED_BLOG_POST", comment.BuildUri(ev));
            }

            core.CallingApplication.SendNotification(core, comment.OwnerKey, comment.User, ev.OwnerKey, ev.ItemKey, "_COMMENTED_BLOG_POST", comment.BuildUri(ev));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlogEntry_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlogEntry_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            core.Search.DeleteFromIndex(this);
            ActionableItem.CleanUp(core, this);
        }

        /// <summary>
        /// Gets the trackbacks for the blog entry
        /// </summary>
        /// <returns></returns>
        public List<TrackBack> GetTrackBacks()
        {
            return GetTrackBacks(1, 10);
        }

        /// <summary>
        /// Gets the trackbacks for the blog entry
        /// </summary>
        /// <param name="page">The page</param>
        /// <param name="perPage">Number of trackbacks per page</param>
        /// <returns></returns>
        public List<TrackBack> GetTrackBacks(int page, int perPage)
        {
            List<TrackBack> trackBacks = new List<TrackBack>();

            SelectQuery query = TrackBack.GetSelectQueryStub(core, typeof(TrackBack));
            query.AddCondition("post_id", postId);

            DataTable trackBacksTable = db.Query(query);

            foreach (DataRow dr in trackBacksTable.Rows)
            {
                trackBacks.Add(new TrackBack(core, this, dr));
            }

            return trackBacks;
        }

        /// <summary>
        /// Gets the pingbacks for the blog entry
        /// </summary>
        /// <param name="page">The page</param>
        /// <param name="perPage">Number of pingbacks per page</param>
        /// <returns></returns>
        public List<PingBack> GetPingBacks(int page, int perPage)
        {
            List<PingBack> pingBacks = new List<PingBack>();

            SelectQuery query = PingBack.GetSelectQueryStub(core, typeof(PingBack));
            query.AddCondition("post_id", postId);

            DataTable pingBacksTable = db.Query(query);

            foreach (DataRow dr in pingBacksTable.Rows)
            {
                pingBacks.Add(new PingBack(core, this, dr));
            }

            return pingBacks;
        }

        /// <summary>
        /// Creates a new blog entry
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="blog"></param>
        /// <param name="title">Title for the new blog entry</param>
        /// <param name="body">Body for the new blog entry</param>
        /// <param name="license">License ID for the new blog entry</param>
        /// <param name="status">Publish status for the new blog entry</param>
        /// <param name="category">Category ID for the new blog entry</param>
        /// <param name="postTime">Post time for the new blog entry</param>
        /// <returns>The new blog entry retrieved from the DB</returns>
        /// <exception cref="NullCoreException">Throws exception when core token is null</exception>
        /// <exception cref="InvalidBlogException">Throws exception when blog token is null</exception>
        /// <exception cref="UnauthorisedToCreateItemException">Throws exception when unauthorised to create a new BlogEntry</exception>
        public static BlogEntry Create(Core core, AccessControlToken token, Blog blog, string title, string body, byte license, PublishStatuses status, short category, long postTime)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (blog == null)
            {
                throw new InvalidBlogException();
            }

            if (blog.UserId != core.LoggedInMemberId)
            {
                throw new UnauthorisedToCreateItemException();
            }

            /*if (!blog.Access.Can("POST_ITEMS"))
            {
            }*/

            string bodyCache = string.Empty;

            if (!body.Contains("[user") && !body.Contains("sid=true]"))
            {
                bodyCache = core.Bbcode.Parse(HttpUtility.HtmlEncode(body), null, blog.Owner, true, string.Empty, string.Empty);
            }

            long now = UnixTime.UnixTimeStamp();

            BlogEntry blogEntry = (BlogEntry)Item.Create(core, typeof(BlogEntry), new FieldValuePair("user_id", blog.UserId),
                new FieldValuePair("post_time_ut", now),
                new FieldValuePair("post_title", title),
                new FieldValuePair("post_published_ut", postTime),
                new FieldValuePair("post_modified_ut", now),
                new FieldValuePair("post_ip", core.Session.IPAddress.ToString()),
                new FieldValuePair("post_text", body),
                new FieldValuePair("post_text_cache", bodyCache),
                new FieldValuePair("post_license", license),
                new FieldValuePair("post_status", (byte)status),
                new FieldValuePair("post_category", category),
                new FieldValuePair("post_simple_permissions", true));

            AccessControlLists acl = new AccessControlLists(core, blogEntry);
            acl.SaveNewItemPermissions(token);

            return blogEntry;
        }

        /// <summary>
        /// Gets the blog post id.
        /// </summary>
        [JsonIgnore]
        public override long Id
        {
            get
            {
                return postId;
            }
        }

        /// <summary>
        /// Gets the URI of the blog post.
        /// </summary>
        [JsonProperty("uri")]
        public override string Uri
        {
            get
            {
                UnixTime tz = new UnixTime(core, ((User)Owner).UserInfo.TimeZoneCode);
                return core.Hyperlink.BuildBlogPostUri((User)Owner, GetPublishedDate(tz).Year, GetPublishedDate(tz).Month, postId);
            }
        }

        [JsonIgnore]
        public string DeleteUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("blog", "write", "delete", Id, true);
            }
        }

        /// <summary>
        /// Gets the sort order for the comments.
        /// </summary>
        [JsonIgnore]
        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        /// <summary>
        /// Gets the number of comments to be displayed per page.
        /// </summary>
        [JsonIgnore]
        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        /// <summary>
        /// Returns the parent object for ACLs.
        /// </summary>
        [JsonIgnore]
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Blog;
            }
        }

        [JsonIgnore]
        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(ownerId, ItemType.GetTypeId(core, typeof(Blog)));
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonIgnore]
        public string DisplayTitle
        {
            get
            {
                return "Blog Entry: " + Title;
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            switch (permission)
            {
                case "COMMENT":
                    return "COMMENT_ITEMS";
                case "RATE":
                    return "RATE_ITEMS";
                default:
                    return permission;
            }
        }

        [JsonIgnore]
        public long Likes
        {
            get
            {
                return Info.Likes;
            }
        }

        [JsonIgnore]
        public long Dislikes
        {
            get
            {
                return Info.Dislikes;
            }
        }


        [JsonIgnore]
        public string IndexingString
        {
            get
            {
                return core.Bbcode.Flatten(Body);
            }
        }

        [JsonIgnore]
        public string IndexingTitle
        {
            get
            {
                return Title;
            }
        }

        [JsonIgnore]
        public string IndexingTags
        {
            get
            {
                List<Tag> tags = getTags();
                string tagstring = string.Empty;

                foreach (Tag tag in tags)
                {
                    tagstring += " " + tag.TagText;
                }

                return tagstring;
            }
        }

        public Template RenderPreview()
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "blogentry");
            template.SetProse(core.Prose);

            VariableCollection blogPostVariableCollection = template.CreateChild("blog_list");

            blogPostVariableCollection.Parse("TITLE", Title);

            DateTime postDateTime = GetPublishedDate(core.Tz);

            string postUrl = HttpUtility.HtmlEncode(string.Format("{0}blog/{1}/{2:00}/{3}",
                    Owner.UriStub, postDateTime.Year, postDateTime.Month, PostId));

            blogPostVariableCollection.Parse("DATE", core.Tz.DateTimeToString(postDateTime));
            blogPostVariableCollection.Parse("URL", postUrl);
            blogPostVariableCollection.Parse("ID", Id);
            blogPostVariableCollection.Parse("TYPE_ID", ItemKey.TypeId);

            blogPostVariableCollection.Parse("POST", Functions.TrimStringToWord(HttpUtility.HtmlDecode(core.Bbcode.StripTags(HttpUtility.HtmlEncode(Body))), 256, true));

            if (core.Session.IsLoggedIn)
            {
                if (Owner.IsItemOwner(core.Session.LoggedInMember))
                {
                    blogPostVariableCollection.Parse("IS_OWNER", "TRUE");
                    blogPostVariableCollection.Parse("U_DELETE", DeleteUri);
                }
            }

            if (Info.Likes > 0)
            {
                blogPostVariableCollection.Parse("LIKES", string.Format(" {0:d}", Info.Likes));
                blogPostVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", Info.Dislikes));
            }

            if (Info.Comments > 0)
            {
                blogPostVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", Info.Comments));
            }

            return template;
        }

        [JsonIgnore]
        public long SharedTimes
        {
            get
            {
                return 0;
            }
        }

        [JsonProperty("owner_key")]
        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(ownerId, ItemType.GetTypeId(core, typeof(User)));
            }
        }

        [JsonIgnore]
        public string ShareString
        {
            get
            {
                string heading = "[h1]" + Title + "[/h1]\r\n\r\n";
                return heading + SummaryString;
            }
        }

        [JsonIgnore]
        public string SummaryString
        {
            get
            {
                UnixTime tz = new UnixTime(core, ((User)Owner).UserInfo.TimeZoneCode);

                string strippedBody = HttpUtility.HtmlDecode(core.Bbcode.StripTags(HttpUtility.HtmlEncode(Body)));
                string uri = string.Format("blog/{0:0000}/{1:00}/{2}",
                    GetPublishedDate(tz).Year, GetPublishedDate(tz).Month, Id);

                if (string.IsNullOrEmpty(strippedBody.Trim()))
                {
                    List<string> images = core.Bbcode.ExtractImages(Body, Owner, false, true);

                    if (images.Count > 0)
                    {
                        return "[iurl=\"" + uri + "\"][img]" + images[0] + "[/img][/iurl]";
                    }

                    return string.Empty;
                }
                else
                {
                    List<string> images = core.Bbcode.ExtractImages(Body, Owner, true, true);
                    string floatImage = string.Empty;

                    if (images.Count > 0)
                    {
                        floatImage = "[float=left][iurl=\"" + uri + "\"][img]" + images[0] + "[/img][/iurl][/float]";
                    }

                    return floatImage + Functions.TrimStringToWord(strippedBody, 256, true);
                }
            }
        }

        [JsonProperty("share_uri")]
        public string ShareUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/share?item={0}&type={1}", ItemKey.Id, ItemKey.TypeId), true);
            }
        }

        [JsonIgnore]
        public string Action
        {
            get
            {
                return "posted a new blog";
            }
        }

        public string GetActionBody(List<ItemKey> subItems)
        {
            return ShareString;
        }

        [JsonIgnore]
        public string Noun
        {
            get
            {
                return "blog";
            }
        }

        [JsonIgnore]
        public ActionableItemType PostType
        {
            get
            {
                return ActionableItemType.Text;
            }
        }

        [JsonIgnore]
        public byte[] Data
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public string DataContentType
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public string Caption
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public long ApplicationId
        {
            get
            {
                return 0;
            }
        }

        public bool CanComment
        {
            get
            {
                return Access.Can("COMMENT");
            }
        }
    }

    /// <summary>
    /// The exception that is thrown when a blog entry has not been found.
    /// </summary>
    public class InvalidBlogEntryException : InvalidItemException
    {
    }
}
