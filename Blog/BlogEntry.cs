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
    public class BlogEntry : NumberedItem, ICommentableItem
    {
        /// <summary>
        /// A list of database fields associated with a blog entry.
        /// </summary>
        /// <remarks>
        /// A blog entry uses the table prefix be.
        /// </remarks>

        [DataField("post_id", DataFieldKeys.Primary)]
        private long postId;
        [DataField("user_id")]
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
        [DataField("post_category")]
        private short category;
        [DataField("post_guid", 255)]
        private string guid;
        [DataField("post_ip", 50)]
        private string ip;
        [DataField("post_time_ut")]
        private long createdRaw;
        [DataField("post_modified_ut")]
        private long modifiedRaw;

        private Primitive owner;

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
                return comments;
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

            SelectQuery query = new SelectQuery(TrackBack.GetTable(typeof(TrackBack)));
            query.AddFields(TrackBack.GetFieldsPrefixed(typeof(TrackBack)));
            query.AddCondition("post_id", postId);

            DataTable trackBacksTable = db.Query(query);

            foreach (DataRow dr in trackBacksTable.Rows)
            {
                trackBacks.Add(new TrackBack(core, dr));
            }

            return trackBacks;
        }

        public static BlogEntry Create(Core core, Primitive owner, string title, string body, byte license, string status, short category, long postTime)
        {
            Item item = Item.Create(core, typeof(BlogEntry), new FieldValuePair("user_id", core.session.LoggedInMember.Id),
                new FieldValuePair("post_time_ut", postTime),
                new FieldValuePair("post_title", title),
                new FieldValuePair("post_modified_ut", postTime),
                new FieldValuePair("post_ip", core.session.IPAddress.ToString()),
                new FieldValuePair("post_text", body),
                new FieldValuePair("post_license", license),
                new FieldValuePair("post_status", status),
                new FieldValuePair("post_category", category));

            return (BlogEntry)item;
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
                UnixTime tz = new UnixTime(core, ((User)owner).TimeZoneCode);
                return core.Uri.BuildBlogPostUri((User)owner, GetCreatedDate(tz).Year, GetCreatedDate(tz).Month, postId);
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

        #region IPermissibleItem Members

        /// <summary>
        /// Returns the parent object for ACLs.
        /// </summary>
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a blog entry has not been found.
    /// </summary>
    public class InvalidBlogEntryException : Exception
    {
    }
}
