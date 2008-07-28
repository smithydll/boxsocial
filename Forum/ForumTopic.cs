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
    public class ForumTopic : NumberedItem
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
                    if (forumId == 0)
                    {
                    }
                    else
                    {
                        forum = new Forum(core, forumId);
                    }
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

        public ForumTopic(Core core, Forum forum, DataRow topicRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Topic_ItemLoad);

            this.forum = forum;

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
            SelectQuery query = NumberedItem.GetSelectQueryStub(typeof(ForumTopic));

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
            uQuery.AddField("topic_first_post_id", post.Id);
            uQuery.AddField("topic_last_post_id", post.Id);
            uQuery.AddField("topic_last_post_time_ut", post.TimeCreatedRaw);
            uQuery.AddCondition("topic_id", topic.Id);

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

            topicPosts++;

            UpdateQuery uQuery = new UpdateQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            uQuery.AddField("topic_posts", new QueryOperation("topic_posts", QueryOperations.Addition, 1));
            uQuery.AddField("topic_last_post_id", post.Id);
            uQuery.AddField("topic_last_post_time_ut", post.TimeCreatedRaw);
            uQuery.AddCondition("topic_id", post.TopicId);

            long rowsUpdated = db.Query(uQuery);

            if (rowsUpdated != 1)
            {
                db.RollBackTransaction();
                Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            return post;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m">Page with message id#</param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public List<TopicPost> GetPosts(long m, int perPage)
        {
            int p = 1; // currentPage

            if (m > 0)
            {
                SelectQuery query = new SelectQuery(TopicPost.GetTable(typeof(TopicPost)));
                query.AddFields("COUNT(*) AS total");
                query.AddCondition("topic_id", topicId);
                query.AddCondition("post_id", ConditionEquality.LessThanEqual, m);

                query.AddSort(SortOrder.Ascending, "post_time_ut");

                DataRow postsRow = db.Query(query).Rows[0];

                long before = (long)postsRow["total"];
                long after = Posts - before;

                /*if (item.CommentSortOrder == SortOrder.Ascending)
                {*/
                    p = (int)(before / perPage + 1);
                /*}
                else
                {
                    p = (int)(after / perPage + 1);
                }*/
            }

            return GetPosts(p, perPage);
        }

        public List<TopicPost> GetPosts(int currentPage, int perPage)
        {
            return getSubItems(typeof(TopicPost), currentPage, perPage, true).ConvertAll<TopicPost>(new Converter<Item, TopicPost>(convertToTopicPost));
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
            get
            {
                if (Forum.Owner.GetType() == typeof(UserGroup))
                {
                    if (forumId == 0)
                    {
                        return Linker.AppendSid(string.Format("/group/{0}/forum/topic-{1}",
                            Forum.Owner.Key, topicId));
                    }
                    else
                    {
                        return Linker.AppendSid(string.Format("/group/{0}/forum/{1}/topic-{2}",
                            Forum.Owner.Key, Forum.Id, topicId));
                    }
                }
                else if (Forum.Owner.GetType() == typeof(Network))
                {
                    return Linker.AppendSid(string.Format("/network/{0}/forum/{1}/topic-{2}",
                        Forum.Owner.Key, forumId, topicId));
                }
                else
                {
                    return "/";
                }
            }
        }

        public string ReplyUri
        {
            get
            {
                if (Forum.Owner.GetType() == typeof(UserGroup))
                {
                    return Linker.AppendSid(string.Format("/group/{0}/forum/post?t={1}&mode=reply",
                        Forum.Owner.Key, topicId));
                }
                else if (Forum.Owner.GetType() == typeof(Network))
                {
                    return Linker.AppendSid(string.Format("/network/{0}/forum/post?t={1}&mode=reply",
                        Forum.Owner.Key, topicId));
                }
                else
                {
                    return "/";
                }
            }
        }

        public static void Show(Core core, GPage page, long forumId, long topicId)
        {
            int p = Functions.RequestInt("p", 1);
            long m = Functions.RequestLong("m", 0); // post, seeing as p has been globally reserved for page and cannot be used for post, we use m for message
            Forum thisForum = null;

            page.template.SetTemplate("Forum", "viewtopic");

            try
            {
                if (forumId == 0)
                {
                    thisForum = new Forum(page.Core, page.ThisGroup);
                }
                else
                {
                    thisForum = new Forum(page.Core, page.ThisGroup, forumId);
                }
            }
            catch (InvalidForumException)
            {
                // ignore
            }

            try
            {
                ForumTopic thisTopic = new ForumTopic(core, thisForum, topicId);

                if (thisForum == null)
                {
                    thisForum = thisTopic.Forum;
                }

                if (!thisForum.ForumAccess.CanRead)
                {
                    Functions.Generate403();
                    return;
                }

                page.template.Parse("TOPIC_TITLE", thisTopic.Title);

                List<TopicPost> posts;
                if (m > 0)
                {
                    posts = thisTopic.GetPosts(m, 10);
                }
                else
                {
                    posts = thisTopic.GetPosts(p, 10);
                }

                page.template.Parse("POSTS", posts.Count.ToString());

                foreach (TopicPost post in posts)
                {
                    VariableCollection postVariableCollection = page.template.CreateChild("post_list");

                    postVariableCollection.Parse("SUBJECT", post.Title);
                    postVariableCollection.Parse("ID", post.Id.ToString());
                    Display.ParseBbcode(postVariableCollection, "TEXT", post.Text);
                    postVariableCollection.Parse("USER_DISPLAY_NAME", post.Poster.Info.DisplayName);
                    postVariableCollection.Parse("USER_TILE", post.Poster.UserIcon);
                    postVariableCollection.Parse("USER_JOINED", core.tz.DateTimeToString(post.Poster.Info.GetRegistrationDate(core.tz)));
                }

                if (thisForum.ForumAccess.CanCreate)
                {
                    page.template.Parse("U_NEW_TOPIC", thisForum.NewTopicUri);
                    page.template.Parse("U_NEW_REPLY", thisTopic.ReplyUri);
                }

                Display.ParsePagination(thisTopic.Uri, p, (int)Math.Ceiling((thisTopic.Posts + 1) / 10.0));

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "forum", "Forum" });
                
                if (thisForum.Id > 0)
                {
                    breadCrumbParts.Add(new string[] { thisForum.Id.ToString(), thisForum.Title });
                }

                breadCrumbParts.Add(new string[] { "topic-" + thisTopic.Id.ToString(), thisTopic.Title });

                page.ThisGroup.ParseBreadCrumbs(breadCrumbParts);
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
