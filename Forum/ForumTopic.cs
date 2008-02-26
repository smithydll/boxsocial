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

namespace BoxSocial.Applications.Forum
{
    public class ForumTopic
    {
        public const string FORUM_TOPIC_INFO_FIELDS = "ft.topic_id, ft.topic_title, ft.user_id, ft.item_id, ft.item_type, ft.topic_views, ft.topic_time, ft.topic_last_post_id, ft.topic_last_post_time";

        private Mysql db;
        private long topicId;
        private string topicTitle;
        private int userId;

        public long TopicId
        {
            get
            {
                return topicId;
            }
        }

        public string TopicTitle
        {
            get
            {
                return TopicTitle;
            }
        }

        public int PosterId
        {
            get
            {
                return userId;
            }
        }

        public ForumTopic(Mysql db, DataRow topicRow)
        {
            this.db = db;

            LoadTopicInfo(topicRow);
        }

        public void LoadTopicInfo(DataRow topicRow)
        {
            topicId = (long)topicRow["topic_id"];
            topicTitle = (string)topicRow["topic_title"];
            userId = (int)topicRow["user_id"];
        }
    }
}
