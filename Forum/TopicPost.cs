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
    [DataTable("forum_post")]
    public class TopicPost : NumberedItem
    {
        [DataField("post_id", DataFieldKeys.Primary)]
        private long postId;
        [DataField("topic_id", typeof(ForumTopic), DataFieldKeys.Index)]
        private long topicId;
        [DataField("forum_id")]
        private long forumId;
        [DataField("user_id")]
        private long userId;
        [DataField("post_title", 127)]
        private string postTitle;
        [DataField("post_text", MYSQL_MEDIUM_TEXT)]
        private string postText;
        [DataField("post_time_ut")]
        private long createdRaw;
        [DataField("post_modified_ut")]
        private long modifiedRaw;
        [DataField("post_modified_count")]
        private int modifiedCount;
        [DataField("post_modified_user_id")]
        private long modifiedUserId;
        [DataField("post_ip", 50)]
        private string postIp;

        private User poster;
        private Forum forum;
        private ForumTopic topic;

        public long PostId
        {
            get
            {
                return postId;
            }
        }

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

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string Title
        {
            get
            {
                return postTitle;
            }
            set
            {
                SetProperty("postTitle", value);
            }
        }

        public string Text
        {
            get
            {
                return postText;
            }
            set
            {
                SetProperty("postText", value);
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

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public User Poster
        {
            get
            {
                return (User)Owner;
            }
        }

        public ForumTopic Topic
        {
            get
            {
                if (topic == null)
                {
                    topic = new ForumTopic(core, topicId);
                }
                return topic;
            }
        }

        public Forum Forum
        {
            get
            {
                if (forum == null)
                {
                    if (topic != null)
                    {
                        forum = topic.Forum;
                    }
                    else
                    {
                        forum = new Forum(core, forumId);
                    }
                }
                return forum;
            }
        }

        public TopicPost(Core core, long postId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Post_ItemLoad);

            try
            {
                LoadItem(postId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPostException();
            }
        }

        internal TopicPost(Core core, Forum forum, ForumTopic topic, long postId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Post_ItemLoad);
            this.forum = forum;
            this.topic = topic;

            try
            {
                LoadItem(postId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPostException();
            }
        }

        public TopicPost(Core core, DataRow postRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Post_ItemLoad);

            try
            {
                loadItemInfo(postRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPostException();
            }
        }

        public TopicPost(Core core, ForumTopic topic, DataRow postRow)
            : base(core)
        {
            this.topic = topic;

            ItemLoad += new ItemLoadHandler(Post_ItemLoad);

            try
            {
                loadItemInfo(postRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPostException();
            }
        }

        void Post_ItemLoad()
        {
        }

        public static Dictionary<long, TopicPost> GetTopicLastPosts(Core core, List<ForumTopic> topics)
        {
            Dictionary<long, TopicPost> posts = new Dictionary<long, TopicPost>();
            Dictionary<long, ForumTopic> reverseLookup = new Dictionary<long, ForumTopic>();
            List<long> topicLastPostIds = new List<long>();

            foreach (ForumTopic topic in topics)
            {
                reverseLookup.Add(topic.LastPostId, topic);

                topicLastPostIds.Add(topic.LastPostId);
            }

            if (topicLastPostIds.Count > 0)
            {
                SelectQuery query = TopicPost.GetSelectQueryStub(typeof(TopicPost));
                query.AddCondition("post_id", ConditionEquality.In, topicLastPostIds);

                DataTable postsTable = core.Db.Query(query);

                List<long> posterIds = new List<long>();

                foreach (DataRow dr in postsTable.Rows)
                {
                    long postId = (long)dr["post_id"];
                    TopicPost tp = new TopicPost(core, reverseLookup[postId], dr);
                    posterIds.Add(tp.UserId);
                    posts.Add(tp.Id, tp);
                }

                core.LoadUserProfiles(posterIds);
            }

            return posts;
        }

        public static Dictionary<long, TopicPost> GetPosts(Core core, List<long> postIds)
        {
            Dictionary<long, TopicPost> posts = new Dictionary<long, TopicPost>();

            if (postIds.Count > 0)
            {
                SelectQuery query = TopicPost.GetSelectQueryStub(typeof(TopicPost));
                query.AddCondition("post_id", ConditionEquality.In, postIds);

                DataTable postsTable = core.Db.Query(query);

                List<long> posterIds = new List<long>();

                foreach (DataRow dr in postsTable.Rows)
                {
                    TopicPost tp = new TopicPost(core, dr);
                    posterIds.Add(tp.UserId);
                    posts.Add(tp.Id, tp);
                }

                //core.LoadUserProfiles(posterIds);
            }

            return posts;
        }

        public override long Id
        {
            get
            {
                return postId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}forum/topic-{1}?m={2}#p{2}",
                    Forum.Owner.UriStub, topicId, postId));
            }
        }

        public string EditUri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}forum/post?p={1}&mode=reply",
                    Forum.Owner.UriStub, postId));
            }
        }

        internal static TopicPost Create(Core core, Forum forum, ForumTopic topic, string subject, string text)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (forum == null)
            {
                if (topic.ForumId == forum.Id)
                {
                    forum = topic.Forum;
                }
            }
            else
            {
                if (topic.ForumId != forum.Id)
                {
                    forum = topic.Forum;
                }
            }

            if (forum == null)
            {
                throw new InvalidForumException();
            }

            if (topic == null)
            {
                throw new InvalidTopicException();
            }

            if (!forum.Access.Can("REPLY_TOPICS"))
            {
                // todo: throw new exception
                throw new UnauthorisedToCreateItemException();
            }

            InsertQuery iQuery = new InsertQuery(NumberedItem.GetTable(typeof(TopicPost)));
            iQuery.AddField("topic_id", topic.Id);
            iQuery.AddField("forum_id", forum.Id);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("post_title", subject);
            iQuery.AddField("post_text", text);
            iQuery.AddField("post_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("post_modified_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("post_ip", core.Session.IPAddress.ToString());

            long postId = core.Db.Query(iQuery);

            return new TopicPost(core, forum, topic, postId);
        }

        public ushort Permissions
        {
            get
            {
                return 0x0000;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (poster == null || userId != poster.Id)
                {
                    core.LoadUserProfile(userId);
                    poster = core.PrimitiveCache[userId];
                    return poster;
                }
                else
                {
                    return poster;
                }

            }
        }

        #region IPermissibleItem Members


        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        #endregion
    }

    public class InvalidPostException : Exception
    {
    }
}
