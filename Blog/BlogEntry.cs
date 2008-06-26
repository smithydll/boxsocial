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
    /// Represents a blog entry
    /// </summary>
    public class BlogEntry : Item, ICommentableItem
    {
        /// <summary>
        /// A list of database fields associated with a blog entry.
        /// </summary>
        /// <remarks>
        /// A blog entry uses the table prefix be.
        /// </remarks>
        public const string BLOG_ENTRY_FIELDS = "be.post_id, be.user_id, be.post_title, be.post_text, be.post_views, be.post_trackbacks, be.post_comments, be.post_access, be.post_status, be.post_license, be.post_category, be.post_guid, be.post_ip, be.post_time_ut, be.post_modified_ut";

        private long postId;
        private long ownerId;
        private Primitive owner;
        private string title;
        private string body;
        private uint views;
        private uint trackbacks;
        private uint comments;
        private ushort access;
        private Access blogEntryAccess;
        private string status;
        private byte license;
        private short category;
        private string guid;
        private string ip;
        private long createdRaw;
        private long modifiedRaw;

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
                return owner;
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
        }

        /// <summary>
        /// Gets the number of views for the blog entry.
        /// </summary>
        public uint Views
        {
            get
            {
                return views;
            }
        }

        /// <summary>
        /// Gets the number of trackbacks for the blog entry.
        /// </summary>
        public uint Trackbacks
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
                return (long)comments;
            }
        }

        /// <summary>
        /// Gets the permission mask for the blog entry.
        /// </summary>
        public ushort Permissions
        {
            get
            {
                return access;
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
        }

        /// <summary>
        /// Gets the access information (permissions) for the blog entry.
        /// </summary>
        public Access BlogEntryAccess
        {
            get
            {
                return blogEntryAccess;
            }
        }

        /// <summary>
        /// Gets the status of the blog post.
        /// </summary>
        /// <remarks>
        /// Valid values are PUBLISH and DRAFT
        /// </remarks>
        public string Status
        {
            get
            {
                return status;
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
        public BlogEntry(Core core, long postId) : base(core)
        {
            DataTable postEntryDataTable = db.Query(string.Format("SELECT {0} FROM blog_postings be WHERE be.post_id = {1}",
                BlogEntry.BLOG_ENTRY_FIELDS, postId));

            if (postEntryDataTable.Rows.Count == 1)
            {
                loadBlogEntryInfo(postEntryDataTable.Rows[0]);

                this.owner = new User(core, ownerId, UserLoadOptions.All);
            }
            else
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
            this.owner = owner;

            loadBlogEntryInfo(postEntryRow);
        }

        /// <summary>
        /// Loads the database information into the BlogEntry class object.
        /// </summary>
        /// <param name="postEntryRow">Raw database information about the blog entry</param>
        private void loadBlogEntryInfo(DataRow postEntryRow)
        {
            postId = (long)postEntryRow["post_id"];
            ownerId = (int)postEntryRow["user_id"];
            title = (string)postEntryRow["post_title"];
            body = (string)postEntryRow["post_text"];
            views = (uint)postEntryRow["post_views"];
            trackbacks = (uint)postEntryRow["post_trackbacks"];
            comments = (uint)postEntryRow["post_comments"];
            access = (ushort)postEntryRow["post_access"];
            status = (string)postEntryRow["post_status"];
            license = (byte)postEntryRow["post_license"];
            category = (short)postEntryRow["post_category"];
            guid = (string)postEntryRow["post_guid"];
            ip = (string)postEntryRow["post_ip"];
            createdRaw = (long)postEntryRow["post_time_ut"];
            modifiedRaw = (long)postEntryRow["post_modified_ut"];

            if (owner == null)
            {
                owner = new User(core, OwnerId);
            }
            blogEntryAccess = new Access(core, access, owner);
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
        /// Gets the BlogPost class namespace.
        /// </summary>
        public override string Namespace
        {
            get
            {
                return "BLOGPOST";
            }
        }

        /// <summary>
        /// Gets the URI of the blog post.
        /// </summary>
        public override string Uri
        {
            get
            {
                UnixTime tz = new UnixTime(((User)owner).TimeZoneCode);
                return Linker.BuildBlogPostUri((User)owner, GetCreatedDate(tz).Year, GetCreatedDate(tz).Month, postId);
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
    }

    public class InvalidBlogEntryException : Exception
    {
    }
}
