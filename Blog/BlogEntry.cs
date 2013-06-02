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
using BoxSocial.Internals;
using BoxSocial.IO;

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
        Publish = 0x00,
        /// <summary>
        /// 
        /// </summary>
        Draft = 0x01,
    }

    /// <summary>
    /// Represents a blog entry
    /// </summary>
    [DataTable("blog_postings", "BLOGPOST")]
    [Permission("VIEW", "Can view this blog entry", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on this blog entry", PermissionTypes.Interact)]
    [Permission("RATE", "Can rate this blog entry", PermissionTypes.Interact)]
    public class BlogEntry : NumberedItem, ICommentableItem, IPermissibleItem, ILikeableItem, ISearchableItem, IShareableItem
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
        [DataField("post_views")]
        private long views;
        [DataField("post_trackbacks")]
        private long trackbacks;
        [DataField("post_comments")]
        private long comments;
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
        [DataField("post_modified_ut")]
        private long modifiedRaw;
        [DataField("post_allow_comment")]
        private bool allowComment;
        [DataField("post_simple_permissions")]
        private bool simplePermissions;

        private Primitive owner;
        private Blog blog;
        private Access access;

        /// <summary>
        /// Gets the blog entry id
        /// </summary>
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
        public long OwnerId
        {
            get
            {
                return ownerId;
            }
        }

        /// <summary>
        /// Gets the owner of the blog post
        /// </summary>
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

        /// <summary>
        /// Gets the number of views for the blog entry.
        /// </summary>
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

        public long PublishedDateRaw
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

        /// <summary>
        /// ItemLoad event
        /// </summary>
        void BlogEntry_ItemLoad()
        {
            ItemUpdated += new EventHandler(BlogEntry_ItemUpdated);
            ItemDeleted += new ItemDeletedEventHandler(BlogEntry_ItemDeleted);
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

            SelectQuery query = TrackBack.GetSelectQueryStub(typeof(TrackBack));
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

            SelectQuery query = PingBack.GetSelectQueryStub(typeof(PingBack));
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
        public static BlogEntry Create(Core core, AccessControlToken token, Blog blog, string title, string body, byte license, string status, short category, long postTime)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (blog == null)
            {
                throw new InvalidBlogException();
            }

            if (blog.UserId != core.Session.LoggedInMember.Id)
            {
                throw new UnauthorisedToCreateItemException();
            }

            /*if (!blog.Access.Can("POST_ITEMS"))
            {
            }*/

            BlogEntry blogEntry = (BlogEntry)Item.Create(core, typeof(BlogEntry), new FieldValuePair("user_id", blog.UserId),
                new FieldValuePair("post_time_ut", postTime),
                new FieldValuePair("post_title", title),
                new FieldValuePair("post_modified_ut", postTime),
                new FieldValuePair("post_ip", core.Session.IPAddress.ToString()),
                new FieldValuePair("post_text", body),
                new FieldValuePair("post_license", license),
                new FieldValuePair("post_status", status),
                new FieldValuePair("post_category", category),
                new FieldValuePair("post_simple_permissions", true));

            AccessControlLists acl = new AccessControlLists(core, blogEntry);
            acl.SaveNewItemPermissions(token);

            core.Search.Index(blogEntry);

            return blogEntry;
        }

        /// <summary>
        /// Gets the blog post id.
        /// </summary>
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
        public override string Uri
        {
            get
            {
                UnixTime tz = new UnixTime(core, ((User)Owner).UserInfo.TimeZoneCode);
                return core.Hyperlink.BuildBlogPostUri((User)Owner, GetCreatedDate(tz).Year, GetCreatedDate(tz).Month, postId);
            }
        }

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
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Blog;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(ownerId, typeof(Blog));
            }
        }

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

        public long Likes
        {
            get
            {
                return Info.Likes;
            }
        }

        public long Dislikes
        {
            get
            {
                return Info.Dislikes;
            }
        }


        public string IndexingString
        {
            get
            {
                return core.Bbcode.Flatten(Body);
            }
        }

        public string IndexingTitle
        {
            get
            {
                return Title;
            }
        }

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

            DateTime postDateTime = GetCreatedDate(core.Tz);

            string postUrl = HttpUtility.HtmlEncode(string.Format("{0}blog/{1}/{2:00}/{3}",
                    Owner.UriStub, postDateTime.Year, postDateTime.Month, PostId));

            blogPostVariableCollection.Parse("DATE", core.Tz.DateTimeToString(postDateTime));
            blogPostVariableCollection.Parse("URL", postUrl);
            blogPostVariableCollection.Parse("ID", Id);
            blogPostVariableCollection.Parse("TYPE_ID", ItemKey.TypeId);

            core.Display.ParseBbcode(blogPostVariableCollection, "POST", Body, Owner);

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

        public long SharedTimes
        {
            get
            {
                return 0;
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(ownerId, ItemType.GetTypeId(typeof(User)));
            }
        }

        public string ShareString
        {
            get
            {
                string strippedBody = HttpUtility.HtmlDecode(core.Bbcode.StripTags(HttpUtility.HtmlEncode(Body)));

                if (string.IsNullOrEmpty(strippedBody.Trim()))
                {
                    List<string> images = core.Bbcode.ExtractImages(Body, Owner, false, true);

                    if (images.Count > 0)
                    {
                        return "[img]" + images[0] + "[/img]";
                    }

                    return string.Empty;
                }
                else
                {
                    List<string> images = core.Bbcode.ExtractImages(Body, Owner, true, true);
                    string floatImage = string.Empty;

                    if (images.Count > 0)
                    {
                        floatImage = "[float=left][img]" + images[0] + "[/img][/float]";
                    }

                    return floatImage + Functions.TrimStringToWord(strippedBody, 256, true);
                }
            }
        }

        public string ShareUri
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(string.Format("/share?item={0}&type={1}", ItemKey.Id, ItemKey.TypeId), true);
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
