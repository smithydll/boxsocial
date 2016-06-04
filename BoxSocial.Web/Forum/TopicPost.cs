/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_post")]
    public class TopicPost : NumberedItem, ISearchableItem, IPermissibleSubItem
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

        public TopicPost(Core core, System.Data.Common.DbDataReader postRow)
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

        public TopicPost(Core core, ForumTopic topic, System.Data.Common.DbDataReader postRow)
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

        protected override void loadItemInfo(DataRow postRow)
        {
            loadValue(postRow, "post_id", out postId);
            loadValue(postRow, "topic_id", out topicId);
            loadValue(postRow, "forum_id", out forumId);
            loadValue(postRow, "user_id", out userId);
            loadValue(postRow, "post_title", out postTitle);
            loadValue(postRow, "post_text", out postText);
            loadValue(postRow, "post_time_ut", out createdRaw);
            loadValue(postRow, "post_modified_ut", out modifiedRaw);
            loadValue(postRow, "post_modified_count", out modifiedCount);
            loadValue(postRow, "post_modified_user_id", out modifiedUserId);
            loadValue(postRow, "post_ip", out postIp);

            itemLoaded(postRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader postRow)
        {
            loadValue(postRow, "post_id", out postId);
            loadValue(postRow, "topic_id", out topicId);
            loadValue(postRow, "forum_id", out forumId);
            loadValue(postRow, "user_id", out userId);
            loadValue(postRow, "post_title", out postTitle);
            loadValue(postRow, "post_text", out postText);
            loadValue(postRow, "post_time_ut", out createdRaw);
            loadValue(postRow, "post_modified_ut", out modifiedRaw);
            loadValue(postRow, "post_modified_count", out modifiedCount);
            loadValue(postRow, "post_modified_user_id", out modifiedUserId);
            loadValue(postRow, "post_ip", out postIp);

            itemLoaded(postRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void Post_ItemLoad()
        {
            ItemUpdated += new EventHandler(TopicPost_ItemUpdated);
            ItemDeleted += new ItemDeletedEventHandler(TopicPost_ItemDeleted);
        }

        void TopicPost_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(this, new SearchField("forum_id", ForumId.ToString()));
        }

        void TopicPost_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            core.Search.DeleteFromIndex(this);
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
                SelectQuery query = TopicPost.GetSelectQueryStub(core, typeof(TopicPost));
                query.AddCondition("post_id", ConditionEquality.In, topicLastPostIds);

                System.Data.Common.DbDataReader postsReader = core.Db.ReaderQuery(query);

                List<long> posterIds = new List<long>();

                while(postsReader.Read())
                {
                    long postId = (long)postsReader["post_id"];
                    TopicPost tp = new TopicPost(core, reverseLookup[postId], postsReader);
                    posterIds.Add(tp.UserId);
                    posts.Add(tp.Id, tp);
                }

                postsReader.Close();
                postsReader.Dispose();

                core.LoadUserProfiles(posterIds);
            }

            return posts;
        }

        public static Dictionary<long, TopicPost> GetPosts(Core core, List<long> postIds)
        {
            Dictionary<long, TopicPost> posts = new Dictionary<long, TopicPost>();

            if (postIds.Count > 0)
            {
                SelectQuery query = TopicPost.GetSelectQueryStub(core, typeof(TopicPost));
                query.AddCondition("post_id", ConditionEquality.In, postIds);

                System.Data.Common.DbDataReader postsReader = core.Db.ReaderQuery(query);

                List<long> posterIds = new List<long>();

                while (postsReader.Read())
                {
                    TopicPost tp = new TopicPost(core, postsReader);
                    posterIds.Add(tp.UserId);
                    posts.Add(tp.Id, tp);
                }

                postsReader.Close();
                postsReader.Dispose();

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
                if (core.IsMobile)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}forum/topic-{1}?m={2}",
                        Forum.Owner.UriStub, topicId, postId));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}forum/topic-{1}?m={2}#p{2}",
                        Forum.Owner.UriStub, topicId, postId));
                }
            }
        }

        public string EditUri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}forum/post?p={1}&mode=reply",
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

            TopicPost newPost = new TopicPost(core, forum, topic, postId);

            core.Search.Index(newPost, new SearchField("forum_id", forum.Id.ToString()));

            return newPost;
        }

        public ushort Permissions
        {
            get
            {
                return 0x0000;
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(userId, ItemType.GetTypeId(core, typeof(User)));
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

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Forum;
            }
        }

        public string IndexingString
        {
            get
            {
                return Text;
            }
        }

        public string IndexingTitle
        {
            get
            {
                return Title;
            }
        }

        public string IndexingTags
        {
            get
            {
                return string.Empty;
            }
        }

        public Template RenderPreview()
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "topicpost");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            VariableCollection postVariableCollection = template.CreateChild("post_list");

            Dictionary<long, ForumMember> postersList = ForumMember.GetMembers(core, Forum.Owner, new List<long> { UserId });

            postVariableCollection.Parse("SUBJECT", Title);
            postVariableCollection.Parse("POST_TIME", core.Tz.DateTimeToString(GetCreatedDate(core.Tz)));
            postVariableCollection.Parse("URI", Uri);
            //postVariableCollection.Parse("POST_MODIFIED", core.tz.DateTimeToString(post.GetModifiedDate(core.tz)));
            postVariableCollection.Parse("ID", Id.ToString());
            core.Display.ParseBbcode(postVariableCollection, "TEXT", Text);
            if (postersList.ContainsKey(UserId))
            {
                postVariableCollection.Parse("U_USER", postersList[UserId].Uri);
                postVariableCollection.Parse("USER_DISPLAY_NAME", postersList[UserId].UserInfo.DisplayName);
                postVariableCollection.Parse("USER_TILE", postersList[UserId].Tile);
                postVariableCollection.Parse("USER_ICON", postersList[UserId].Icon);
                postVariableCollection.Parse("USER_JOINED", core.Tz.DateTimeToString(postersList[UserId].UserInfo.GetRegistrationDate(core.Tz)));
                postVariableCollection.Parse("USER_COUNTRY", postersList[UserId].Profile.Country);
                postVariableCollection.Parse("USER_POSTS", postersList[UserId].ForumPosts.ToString());
                core.Display.ParseBbcode(postVariableCollection, "SIGNATURE", postersList[UserId].ForumSignature);

                /*if (ranksList.ContainsKey(postersList[post.UserId].ForumRankId))
                {
                    postVariableCollection.Parse("USER_RANK", ranksList[postersList[UserId].ForumRankId].RankTitleText);
                }*/
            }
            else
            {
                postVariableCollection.Parse("USER_DISPLAY_NAME", "Anonymous");
            }

            /*if (thisTopic.ReadStatus == null)
            {*/
                postVariableCollection.Parse("IS_READ", "FALSE");
            /*}
            else
            {
                if (thisTopic.ReadStatus.ReadTimeRaw < post.TimeCreatedRaw)
                {
                    postVariableCollection.Parse("IS_READ", "FALSE");
                }
                else
                {
                    postVariableCollection.Parse("IS_READ", "TRUE");
                }
            }*/

            return template;
        }
    }

    public class InvalidPostException : Exception
    {
    }
}
