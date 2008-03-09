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
    public class BlogEntry : Item
    {
        /// <summary>
        /// A list of database fields associated with a blog entry.
        /// </summary>
        /// <remarks>
        /// A blog entry uses the table prefix be.
        /// </remarks>
        public const string BLOG_ENTRY_FIELDS = "be.post_id, be.user_id, be.post_title, be.post_text, be.post_views, be.post_trackbacks, be.post_comments, be.post_access, be.post_status, be.post_license, be.post_category, be.post_guid, be.post_ip, be.post_time_ut, be.post_modified_ut";

        private Mysql db;

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
        /// Gets the blog entry id.
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
        public override long Comments
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
        /// <param name="db">Database</param>
        /// <param name="postId">Post Id to retrieve</param>
        public BlogEntry(Mysql db, long postId)
        {
            this.db = db;

            DataTable postEntryDataTable = db.SelectQuery(string.Format("SELECT {0} FROM blog_postings be WHERE be.post_id = {1}",
                BlogEntry.BLOG_ENTRY_FIELDS, postId));

            if (postEntryDataTable.Rows.Count == 1)
            {
                loadBlogEntryInfo(postEntryDataTable.Rows[0]);

                this.owner = new Member(db, ownerId, true);
            }
            else
            {
                throw new Exception("Invalid Blog Entry Exception");
            }
        }

        /// <summary>
        /// Initialses a new instance of the BlogEntry class.
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="owner">Owner whose blog post has been retrieved</param>
        /// <param name="postEntryRow">Raw data row of blog entry</param>
        public BlogEntry(Mysql db, Primitive owner, DataRow postEntryRow)
        {
            this.db = db;
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
                owner = new Member(db, OwnerId);
            }
            blogEntryAccess = new Access(db, access, owner);
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
                return this.GetType().FullName;
            }
        }

        /// <summary>
        /// Gets the URI for the blog post.
        /// </summary>
        public override string Uri
        {
            get
            {
                UnixTime tz = new UnixTime(((Member)owner).TimeZoneCode);
                return Linker.BuildBlogPostUri((Member)owner, GetCreatedDate(tz).Year, GetCreatedDate(tz).Month, postId);
            }
        }

        public override float Rating
        {
            get
            {
                return 0;
            }
        }
    }
}
