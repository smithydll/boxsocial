﻿/*
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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_topics")]
    public class ForumTopic : Item
    {
        public const string FORUM_TOPIC_INFO_FIELDS = "ft.topic_id, ft.topic_title, ft.user_id, ft.item_id, ft.item_type, ft.topic_views, ft.topic_time, ft.topic_last_post_id, ft.topic_last_post_time";

        [DataField("topic_id", DataFieldKeys.Primary)]
        private long topicId;
        [DataField("forum_id", typeof(Forum))]
        private long forumId;
        [DataField("topic_title", 127)]
        private string topicTitle;
        [DataField("user_id")]
        private long userId;
        [DataField("topic_posts")]
        private long topicPosts;
        [DataField("topic_views")]
        private long topicViews;
        [DataField("topic_time_ut")]
        private long createdRaw;
        [DataField("topic_modified_ut")]
        private long modifiedRaw;
        [DataField("topic_last_post_id")]
        private long lastPostId;
        [DataField("topic_first_post_id")]
        private long firstPostId;
        [DataField("topic_item_id")]
        private long owner_id;
        [DataField("topic_item_type", 63)]
        private string owner_type;

        private Forum forum;

        public long TopicId
        {
            get
            {
                return topicId;
            }
        }

        public long ForumId
        {
            get
            {
                return forumId;
            }
        }

        public Forum Forum
        {
            get
            {
                return forum;
            }
        }

        public string Title
        {
            get
            {
                return topicTitle;
            }
            set
            {
                SetProperty("topicTitle", value);
            }
        }

        public long PosterId
        {
            get
            {
                return userId;
            }
        }

        public long Posts
        {
            get
            {
                return topicPosts;
            }
        }

        public long Views
        {
            get
            {
                return topicViews;
            }
        }

        public long TimeCreatedRaw
        {
            get
            {
                return createdRaw;
            }
        }

        public long TimeModifiedRaw
        {
            get
            {
                return modifiedRaw;
            }
        }

        public long LastPostId
        {
            get
            {
                return lastPostId;
            }
        }

        public long FirstPostId
        {
            get
            {
                return firstPostId;
            }
        }

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public ForumTopic(Core core, long topicId)
            : this(core, null, topicId)
        {
        }

        public ForumTopic(Core core, Forum forum, long topicId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Topic_ItemLoad);

            this.forum = forum;

            try
            {
                LoadItem(topicId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTopicException();
            }
        }

        public ForumTopic(Core core, DataRow topicRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Topic_ItemLoad);

            try
            {
                loadItemInfo(topicRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTopicException();
            }
        }

        void Topic_ItemLoad()
        {
            /*if (forum == null)
            {
                forum = new Forum(core, forumId);
            }*/
        }


        public static SelectQuery ForumTopic_GetSelectQueryStub()
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ForumTopic));

            query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicPost)));
            query.AddJoin(JoinTypes.Left, TopicPost.GetTable(typeof(TopicPost)), "topic_last_post_id", "post_id");

            query.AddSort(SortOrder.Descending, "topic_last_post_time");

            return query;
        }

        public static ForumTopic Create(Core core, Forum forum, string topic, string messageBody)
        {
            InsertQuery iquery = new InsertQuery("forum_topics");

            long topicId = core.db.Query(iquery);

            return new ForumTopic(core, forum, topicId);
        }

        public List<TopicPost> GetPosts(TPage page, int currentPage, int perPage)
        {
            List<TopicPost> posts = new List<TopicPost>();

            SelectQuery query = new SelectQuery("posts");


            return posts;
        }

        public override long Id
        {
            get
            {
                return topicId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidTopicException : Exception
    {
    }
}
