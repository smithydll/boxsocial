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
        [DataField("topic_last_post_time_ut")]
        private long lastPostTimeRaw;
        [DataField("topic_last_post_id")]
        private long lastPostId;
        [DataField("topic_first_post_id")]
        private long firstPostId;

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
                if (forum == null)
                {
                    forum = new Forum(core, forumId);
                }
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
        }


        public static SelectQuery ForumTopic_GetSelectQueryStub()
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ForumTopic));

            query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicPost)));
            query.AddJoin(JoinTypes.Left, TopicPost.GetTable(typeof(TopicPost)), "topic_last_post_id", "post_id");

            query.AddSort(SortOrder.Descending, "topic_last_post_time_ut");

            return query;
        }

        public static ForumTopic Create(Core core, Forum forum, string subject, string text)
        {
            core.db.BeginTransaction();

            if (forum == null)
            {
                throw new InvalidForumException();
            }

            if (!forum.ForumAccess.CanCreate)
            {
                // todo: throw new exception
                throw new UnauthorisedToCreateItemException();
            }

            InsertQuery iquery = new InsertQuery("forum_topics");
            iquery.AddField("forum_id", forum.Id);
            iquery.AddField("topic_title", subject);
            iquery.AddField("user_id", core.LoggedInMemberId);
            iquery.AddField("topic_posts", 0);
            iquery.AddField("topic_views", 0);
            iquery.AddField("topic_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("topic_modified_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("topic_last_post_time_ut", UnixTime.UnixTimeStamp());

            long topicId = core.db.Query(iquery);

            ForumTopic topic = new ForumTopic(core, forum, topicId);

            TopicPost post = TopicPost.Create(core, forum, topic, subject, text);

            UpdateQuery uQuery = new UpdateQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            uQuery.AddField("topic_posts", new QueryOperation("topic_posts", QueryOperations.Addition, 1));
            uQuery.AddField("topic_first_post_id", post.Id);
            uQuery.AddField("topic_last_post_id", post.Id);
            uQuery.AddField("topic_last_post_time_ut", post.TimeCreatedRaw);

            long rowsUpdated = core.db.Query(uQuery);

            if (rowsUpdated != 1)
            {
                core.db.RollBackTransaction();
                Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            return topic;
        }

        public TopicPost AddReply(Core core, Forum forum, string subject, string text)
        {
            db.BeginTransaction();

            TopicPost post = TopicPost.Create(core, forum, this, subject, text);

            UpdateQuery uQuery = new UpdateQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            uQuery.AddField("topic_posts", new QueryOperation("topic_posts", QueryOperations.Addition, 1));
            uQuery.AddField("topic_last_post_id", post.Id);
            uQuery.AddField("topic_last_post_time_ut", post.TimeCreatedRaw);

            long rowsUpdated = db.Query(uQuery);

            if (rowsUpdated != 1)
            {
                db.RollBackTransaction();
                Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            return post;
        }

        public List<TopicPost> GetPosts(int currentPage, int perPage)
        {
            return getSubItems(typeof(TopicPost), currentPage, perPage).ConvertAll<TopicPost>(new Converter<Item, TopicPost>(convertToTopicPost));
        }

        public TopicPost convertToTopicPost(Item input)
        {
            return (TopicPost)input;
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

        public string ReplyUri
        {
            get
            {
                if (Forum.Owner.GetType() == typeof(UserGroup))
                {
                    return Linker.AppendSid(string.Format("/group/{0}/forum/post?f={1}&mode=reply",
                        Forum.Owner.Key, forumId));
                }
                else if (Forum.Owner.GetType() == typeof(Network))
                {
                    return Linker.AppendSid(string.Format("/network/{0}/forum/post?f={1}&mode=reply",
                        Forum.Owner.Key, forumId));
                }
                else
                {
                    return "/";
                }
            }
        }

        public static void Show(Core core, GPage page, long forumId, long topicId)
        {
            Forum thisForum = null;

            page.template.SetTemplate("Forum", "viewtopic");

            try
            {
                thisForum = new Forum(page.Core, page.ThisGroup);
            }
            catch (InvalidForumException)
            {
                // ignore
            }

            try
            {
                ForumTopic thisTopic = new ForumTopic(core, topicId);

                if (thisForum == null)
                {
                    thisForum = thisTopic.Forum;
                }

                if (!thisForum.ForumAccess.CanRead)
                {
                    Functions.Generate403();
                    return;
                }

                List<TopicPost> posts = thisTopic.GetPosts(1, 10);

                foreach (TopicPost post in posts)
                {
                    VariableCollection postVariableCollection = page.template.CreateChild("post_list");
                }
            }
            catch (InvalidTopicException)
            {
                return;
            }
        }
    }

    public class InvalidTopicException : Exception
    {
    }
}
