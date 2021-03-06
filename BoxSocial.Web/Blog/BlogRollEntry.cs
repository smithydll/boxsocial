﻿/*
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
using System.Collections.Generic;
using System.Data;
using System.Text;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    /// <summary>
    /// Represents a blogroll entry
    /// </summary>
    [DataTable("blog_roll_entries")]
    public class BlogRollEntry : NumberedItem
    {
        /// <summary>
        /// Blog roll entry Id
        /// </summary>
        [DataField("entry_id", DataFieldKeys.Primary)]
        private long blogRollEntryId;

        /// <summary>
        /// Blog Id
        /// </summary>
        [DataField("user_id")]
        private long blogId;
        
        /// <summary>
        /// Blog roll user Id
        /// </summary>
        [DataField("entry_user_id")]
        private long userId;

        /// <summary>
        /// Blog roll entry uri
        /// </summary>
        [DataField("entry_uri", 255)]
        private string uri;

        /// <summary>
        /// Blog roll entry title
        /// </summary>
        [DataField("entry_title", 63)]
        private string title;

        public long BlogRollEntryId
        {
            get
            {
                return blogRollEntryId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string EntryUri
        {
            get
            {
                return uri;
            }
            set
            {
                SetProperty("uri", value);
            }
        }

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

        public User User
        {
            get
            {
                return core.PrimitiveCache[userId];
            }
        }

        public BlogRollEntry(Core core, long blogRollId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogRollEntry_ItemLoad);

            try
            {
                LoadItem(blogRollId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBlogRollEntryException();
            }
        }

        public BlogRollEntry(Core core, DataRow blogRollRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogRollEntry_ItemLoad);

            loadItemInfo(blogRollRow);
        }

        public BlogRollEntry(Core core, System.Data.Common.DbDataReader blogRollRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BlogRollEntry_ItemLoad);

            loadItemInfo(blogRollRow);
        }

        protected override void loadItemInfo(DataRow linkRow)
        {
            loadValue(linkRow, "entry_id", out blogRollEntryId);
            loadValue(linkRow, "user_id", out blogId);
            loadValue(linkRow, "entry_user_id", out userId);
            loadValue(linkRow, "entry_uri", out uri);
            loadValue(linkRow, "entry_title", out title);

            itemLoaded(linkRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader linkRow)
        {
            loadValue(linkRow, "entry_id", out blogRollEntryId);
            loadValue(linkRow, "user_id", out blogId);
            loadValue(linkRow, "entry_user_id", out userId);
            loadValue(linkRow, "entry_uri", out uri);
            loadValue(linkRow, "entry_title", out title);

            itemLoaded(linkRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void BlogRollEntry_ItemLoad()
        {
            core.LoadUserProfile(UserId);
        }

        public static BlogRollEntry Create(Core core, long userId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            User member = new User(core, userId);

            InsertQuery iquery = new InsertQuery("blog_roll_entries");
            iquery.AddField("user_id", core.LoggedInMemberId);
            iquery.AddField("entry_user_id", member.Id);

            long blogRollEntryId = core.Db.Query(iquery);

            return new BlogRollEntry(core, blogRollEntryId);
        }

        public static BlogRollEntry Create(Core core, string title, string uri)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(uri))
            {
                throw new NoNullAllowedException();
            }

            InsertQuery iquery = new InsertQuery("blog_roll_entries");
            iquery.AddField("user_id", core.LoggedInMemberId);
            iquery.AddField("entry_uri", uri);
            iquery.AddField("entry_title", title);

            long blogRollEntryId = core.Db.Query(iquery);

            return new BlogRollEntry(core, blogRollEntryId);
        }

        public override long Id
        {
            get
            {
                return blogRollEntryId;
            }
        }

        public override string Uri
        {
            get
            {
                if (string.IsNullOrEmpty(uri))
                {
                    if (userId > 0)
                    {
                        core.LoadUserProfile(userId);
                        Blog.BuildUri(core, core.PrimitiveCache[userId]);
                    }
                }
                else
                {
                    return uri;
                }
                return "";
            }
        }
    }

    public class InvalidBlogRollEntryException : Exception
    {
    }
}
