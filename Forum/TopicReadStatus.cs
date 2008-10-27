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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_topic_read_status")]
    public class TopicReadStatus : Item
    {
        [DataField("topic_id", DataFieldKeys.Unique, "ftrs_key")]
        private long topicId;
        [DataField("user_id", DataFieldKeys.Unique, "ftrs_key")]
        private long userId;
        [DataField("forum_id")]
        private long forumId;
        [DataField("read_time_ut")]
        private long readTime;

        public long TopicId
        {
            get
            {
                return topicId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public long ForumId
        {
            get
            {
                return forumId;
            }
        }

        public long ReadTimeRaw
        {
            get
            {
                return readTime;
            }
        }

        public DateTime GetReadTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(readTime);
        }

        public TopicReadStatus(Core core, long topicId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TopicReadStatus_ItemLoad);

            SelectQuery query = GetSelectQueryStub();
            query.AddCondition("topic_id", topicId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            DataTable itemTable = db.Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);
            }
            else
            {
                throw new InvalidTopicReadStatusException();
            }
        }

        public TopicReadStatus(Core core, DataRow dr)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TopicReadStatus_ItemLoad);

            try
            {
                loadItemInfo(dr);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTopicReadStatusException();
            }
        }

        void TopicReadStatus_ItemLoad()
        {
        }

        internal static void Create(Core core, ForumTopic topic, TopicPost lastVisiblePost)
        {
            if (core.LoggedInMemberId > 0)
            {
                InsertQuery iQuery = new InsertQuery(GetTable(typeof(TopicReadStatus)));
                iQuery.AddField("topic_id", topic.Id);
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("forum_id", topic.ForumId);
                iQuery.AddField("read_time_ut", lastVisiblePost.TimeCreatedRaw);

                core.db.Query(iQuery);
            }
        }

        internal static void Create(Core core, ForumTopic topic)
        {
            if (core.LoggedInMemberId > 0)
            {
                InsertQuery iQuery = new InsertQuery(GetTable(typeof(TopicReadStatus)));
                iQuery.AddField("topic_id", topic.Id);
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("forum_id", topic.ForumId);
                iQuery.AddField("read_time_ut", UnixTime.UnixTimeStamp()); // topic.LastPostTimRaw

                core.db.Query(iQuery);
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidTopicReadStatusException : Exception
    {
    }
}
