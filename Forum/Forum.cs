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
    public class Forum
    {
        protected Mysql db;
        protected Primitive owner;

        public Forum(Mysql db, UserGroup owner)
        {
            this.db = db;
            this.owner = owner;
        }

        public List<ForumTopic> GetTopics(TPage page, int currentPage, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            DataTable topicsTable = db.SelectQuery(string.Format(@"SELECT {0}
                FROM forum_topics
                LEFT JOIN topic_posts tp ON tp.post_id = ft.topic_last_post_id
                WHERE ft.item_id = {1} AND ft.item_type = '{2}'
                ORDER BY ft.topic_last_post_time DESC",
                ForumTopic.FORUM_TOPIC_INFO_FIELDS, owner.Id, owner.GetType()));

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(db, dr));
            }

            return topics;
        }
    }
}