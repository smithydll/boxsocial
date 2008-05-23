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
    [DataTable("forum")]
    public class Forum : Item
    {
        [DataField("forum_id", DataFieldKeys.Primary)]
        private long forumId;
        [DataField("forum_parent_id")]
        private long parentId;
        [DataField("forum_title", 127)]
        private string forumTitle;
        [DataField("forum_description", 1023)]
        private string forumDescription;
        [DataField("forum_topics")]
        private long forumTopics;
        [DataField("forum_posts")]
        private long forumPosts;
        [DataField("forum_access")]
        private ushort permissions;
        [DataField("forum_item_id")]
        private long owner_id;
        [DataField("forum_item_type", 63)]
        private string owner_type;

        private Primitive owner;
        private Access forumAccess;

        public string Title
        {
            get
            {
                return forumTitle;
            }
            set
            {
                SetProperty("forumTitle", value);
            }
        }

        public Forum(Core core, UserGroup owner, long forumId)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Forum_ItemLoad);

            try
            {
                LoadItem(forumId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumException();
            }
        }

        public Forum(Core core, DataRow forumDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Forum_ItemLoad);

            try
            {
                loadItemInfo(forumDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumException();
            }
        }

        void Forum_ItemLoad()
        {
            if (owner.Id != owner_id)
            {
                if (owner_type == "GROUP")
                {
                    owner = new UserGroup(core, owner_id);
                }
                else if (owner_type == "NETWORK")
                {
                    owner = new Network(core, owner_id);
                }
            }

            forumAccess = new Access(core.db, permissions, owner);
        }

        public List<Forum> GetForums()
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = new SelectQuery("forum");
            query.AddCondition("forum_item_id", owner_id);
            query.AddCondition("forum_item_type", owner_type);
            query.AddCondition("forum_parent_id", forumId);

            DataTable forumsTable = db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                forums.Add(new Forum(core, dr));
            }

            return forums;
        }

        public List<ForumTopic> GetTopics(TPage page, int currentPage, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            DataTable topicsTable = db.Query(string.Format(@"SELECT {0}
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

        public override long Id
        {
            get
            {
                return forumId;
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
            get
            {
                if (owner.GetType() == typeof(UserGroup))
                {
                    return Linker.AppendSid(string.Format("/group/{0}/forum/{1}/",
                        owner.Key, forumId));
                }
                else if (owner.GetType() == typeof(Network))
                {
                    return Linker.AppendSid(string.Format("/network/{0}/forum/{1}/",
                        owner.Key, forumId));
                }
                else
                {
                    return "/";
                }
            }
        }

        public static void Show(GPage page, long forumId)
        {
        }
    }

    public class InvalidForumException : Exception
    {
    }
}
