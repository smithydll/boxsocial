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
        [DataField("forum_last_post_id")]
        private long lastPostId;
        [DataField("forum_item_id")]
        private long ownerId;
        [DataField("forum_item_type", 63)]
        private string ownerType;

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

        public string Description
        {
            get
            {
                return forumDescription;
            }
            set
            {
                SetProperty("forumDescription", value);
            }
        }

        public long Topics
        {
            get
            {
                return forumTopics;
            }
        }

        public long Posts
        {
            get
            {
                return forumPosts;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
            set
            {
                SetProperty("permissions", value);
            }
        }

        public long LastPostId
        {
            get
            {
                return lastPostId;
            }
        }

        public Access ForumAccess
        {
            get
            {
                return forumAccess;
            }
        }

        public Forum(Core core, UserGroup owner)
            : base(core)
        {
            forumId = 0;
        }

        public Forum(Core core, long forumId)
            : this(core, null, forumId)
        {
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
            if (owner.Id != ownerId)
            {
                if (ownerType == "GROUP")
                {
                    owner = new UserGroup(core, ownerId);
                }
                else if (ownerType == "NETWORK")
                {
                    owner = new Network(core, ownerId);
                }
            }

            forumAccess = new Access(core.db, permissions, owner);
        }

        public List<Forum> GetForums()
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = new SelectQuery("forum");
            query.AddCondition("forum_item_id", ownerId);
            query.AddCondition("forum_item_type", ownerType);
            query.AddCondition("forum_parent_id", forumId);

            DataTable forumsTable = db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                forums.Add(new Forum(core, dr));
            }

            return forums;
        }

        public List<ForumTopic> GetTopics(int currentPage, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            SelectQuery query = new SelectQuery("forum_topics");
            query.AddCondition("forum_id", forumId);
            query.AddCondition("topic_item_id", ownerId);
            query.AddCondition("topic_item_type", ownerType);
            query.AddSort(SortOrder.Descending, "topic_last_post_id");

            DataTable topicsTable = db.Query(query);

            /*DataTable topicsTable = db.Query(string.Format(@"SELECT {0}
                FROM forum_topics
                LEFT JOIN topic_posts tp ON tp.post_id = ft.topic_last_post_id
                WHERE ft.item_id = {1} AND ft.item_type = '{2}'
                ORDER BY ft.topic_last_post_time DESC",
                ForumTopic.FORUM_TOPIC_INFO_FIELDS, owner.Id, owner.GetType()));*/

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
            }

            return topics;
        }

        public List<ForumTopic> GetTopicsFlat(int currentPge, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            SelectQuery query = new SelectQuery("forum_topics");
            query.AddCondition("topic_item_id", ownerId);
            query.AddCondition("topic_item_type", ownerType);
            query.AddSort(SortOrder.Descending, "topic_last_post_id");

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
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

        public static void Show(Core core, GPage page, long forumId)
        {
            Forum thisForum = null;

            page.template.SetTemplate("Forum", "viewforum");

            try
            {
                if (forumId > 0)
                {
                    thisForum = new Forum(page.Core, page.ThisGroup, forumId);
                }
                else
                {
                    thisForum = new Forum(page.Core, page.ThisGroup);
                }
            }
            catch (InvalidForumException)
            {
                return;
            }

            thisForum.ForumAccess.SetSessionViewer(core.session);

            if (!thisForum.ForumAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            List<Forum> forums = thisForum.GetForums();

            page.template.ParseVariables("FORUMS", HttpUtility.HtmlEncode(forums.Count.ToString()));

            // ForumId, TopicPost
            Dictionary<long, TopicPost> lastPosts;
            List<long> lastPostIds = new List<long>();

            foreach (Forum forum in forums)
            {
                lastPostIds.Add(forum.LastPostId);
            }

            lastPosts = TopicPost.GetPosts(core, lastPostIds);

            foreach (Forum forum in forums)
            {
                VariableCollection forumVariableCollection = page.template.CreateChild("forum_list");

                forumVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(forum.Title));
                forumVariableCollection.ParseVariables("POSTS", HttpUtility.HtmlEncode(forum.Posts.ToString()));
                forumVariableCollection.ParseVariables("TOPICS", HttpUtility.HtmlEncode(forum.Topics.ToString()));

                if (lastPosts.ContainsKey(forum.Id))
                {
                    forumVariableCollection.ParseVariables("LAST_POST", HttpUtility.HtmlEncode(lastPosts[forum.Id].Title));
                }
                else
                {
                    forumVariableCollection.ParseVariables("LAST_POST", HttpUtility.HtmlEncode("No posts"));
                }
            }

            List<ForumTopic> topics = thisForum.GetTopics(1, 10);

            page.template.ParseVariables("TOPICS", HttpUtility.HtmlEncode(topics.Count.ToString()));

            // TopicId, TopicPost
            Dictionary<long, TopicPost> topicLastPosts;
            List<long> topicLastPostIds = new List<long>();

            foreach (ForumTopic topic in topics)
            {
                lastPostIds.Add(topic.LastPostId);
            }

            topicLastPosts = TopicPost.GetPosts(core, topicLastPostIds);

            foreach (ForumTopic topic in topics)
            {
                VariableCollection topicVariableCollection = page.template.CreateChild("topic_list");

                topicVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(topic.Title));
                topicVariableCollection.ParseVariables("VIEWS", HttpUtility.HtmlEncode(topic.Views.ToString()));
                topicVariableCollection.ParseVariables("REPLIES", HttpUtility.HtmlEncode(topic.Posts.ToString()));

                if (topicLastPosts.ContainsKey(topic.Id))
                {
                    topicVariableCollection.ParseVariables("LAST_POST", HttpUtility.HtmlEncode(topicLastPosts[topic.Id].Title));
                }
                else
                {
                    topicVariableCollection.ParseVariables("LAST_POST", HttpUtility.HtmlEncode("No posts"));
                }
            }
        }
    }

    public class InvalidForumException : Exception
    {
    }
}