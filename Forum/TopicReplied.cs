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
    [DataTable("forum_topic_replied")]
    public class TopicReplied : Item
    {
        [DataField("topic_id", DataFieldKeys.Unique, "ftr_key")]
        private long topicId;
        [DataField("user_id", DataFieldKeys.Unique, "ftr_key")]
        private long userId;
        [DataField("forum_id")]
        private long forumId;
        [DataField("last_replied_time_ut")]
        private long lastRepliedTime;

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

        public long LastRepliedTimeRaw
        {
            get
            {
                return lastRepliedTime;
            }
        }

        public DateTime GetLastRepliedTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(lastRepliedTime);
        }

        public TopicReplied(Core core, User user, long topicId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TopicReplied_ItemLoad);

            SelectQuery query = GetSelectQueryStub();
            query.AddCondition("topic_id", topicId);
            query.AddCondition("user_id", user.Id);

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

        public TopicReplied(Core core, DataRow dr)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TopicReplied_ItemLoad);

            try
            {
                loadItemInfo(dr);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTopicRepliedException();
            }
        }

        void TopicReplied_ItemLoad()
        {
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidTopicRepliedException : Exception
    {
    }
}
