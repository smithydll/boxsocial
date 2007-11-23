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
    public class BlogEntry
    {
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

        public long PostId
        {
            get
            {
                return postId;
            }
        }

        public long OwnerId
        {
            get
            {
                return ownerId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public uint Views
        {
            get
            {
                return views;
            }
        }

        public uint Trackbacks
        {
            get
            {
                return trackbacks;
            }
        }

        public uint Comments
        {
            get
            {
                return comments;
            }
        }

        public ushort Permissions
        {
            get
            {
                return access;
            }
        }

        public Access BlogEntryAccess
        {
            get
            {
                return blogEntryAccess;
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
        }

        public byte License
        {
            get
            {
                return license;
            }
        }

        public string Guid
        {
            get
            {
                return guid;
            }
        }

        public DateTime GetCreatedDate(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public BlogEntry(Mysql db, long postId)
        {
            this.db = db;

            DataTable postEntryDataTable = db.SelectQuery(string.Format("SELECT {0} FROM blog_postings be WHERE be.post_id = {1}",
                BlogEntry.BLOG_ENTRY_FIELDS, postId));

            if (postEntryDataTable.Rows.Count == 1)
            {
                loadBlogEntryInfo(postEntryDataTable.Rows[0]);
            }
            else
            {
                throw new Exception("Invalid Blog Entry Exception");
            }
        }

        public BlogEntry(Mysql db, Primitive owner, DataRow postEntryRow)
        {
            this.db = db;
            this.owner = owner;

            loadBlogEntryInfo(postEntryRow);
        }

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
    }
}
