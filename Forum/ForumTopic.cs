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
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public enum TopicStates : byte
    {
        Normal = 0,
        Sticky = 1,
        Announcement = 2,
        Global = 3,
    }

    [DataTable("forum_topics")]
    public class ForumTopic : NumberedItem
    {
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
        [DataField("topic_status")]
        private byte topicStatus;
        [DataField("topic_locked")]
        private bool topicLocked;
		[DataField("topic_moved")]
		private bool topicMoved;
		[DataField("topic_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;

        private Forum forum;
        private TopicReadStatus readStatus = null;
        private bool readStatusLoaded;
        private Primitive owner;

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

        public Primitive ForumOwner
        {
            get
            {
                if (owner == null || (ownerKey.Id != owner.Id && ownerKey.TypeId != owner.TypeId))
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Forum Forum
        {
            get
            {
                if (forum == null || forum.Id != forumId)
                {
                    if (forumId == 0)
                    {
                        forum = new Forum(core, ForumOwner);
                    }
                    else
                    {
                        forum = new Forum(core, forumId);
                    }
                }
                return forum;
            }
        }

        public TopicReadStatus ReadStatus
        {
            get
            {
                if (readStatus == null)
                {
                    if (readStatusLoaded)
                    {
                        return null;
                    }
                    else
                    {
                        try
                        {
                            readStatus = new TopicReadStatus(core, topicId);
                            readStatusLoaded = true;
                            return readStatus;
                        }
                        catch (InvalidTopicReadStatusException)
                        {
                            readStatusLoaded = true;
                            return null;
                        }
                    }
                }
                else
                {
                    return readStatus;
                }
            }
        }

        public bool IsRead
        {
            get
            {
                if (!readStatusLoaded)
                {
                    return false;
                }
                else
                {
                    if (readStatus != null)
                    {
                        if (readStatus.ReadTimeRaw < lastPostTimeRaw)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
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

        public long TimeLastPostRaw
        {
            get
            {
                return lastPostTimeRaw;
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

        public TopicStates Status
        {
            get
            {
                return (TopicStates)topicStatus;
            }
            set
            {
                SetProperty("topicStatus", (byte)value);
            }
        }

        public bool IsLocked
        {
            get
            {
                return topicLocked;
            }
            set
            {
                SetProperty("topicLocked", value);
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

            SelectQuery query = ForumTopic_GetSelectQueryStub(core);
            query.AddCondition("`forum_topics`.`topic_id`", topicId);

            DataTable topicTable = db.Query(query);

            if (topicTable.Rows.Count == 1)
            {
                loadItemInfo(topicTable.Rows[0]);
            }
            else
            {
                throw new InvalidTopicException();
            }

            try
            {
                readStatus = new TopicReadStatus(core, topicTable.Rows[0]);
                readStatusLoaded = true;
            }
            catch (InvalidTopicReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
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

            try
            {
                readStatus = new TopicReadStatus(core, topicRow);
                readStatusLoaded = true;
            }
            catch (InvalidTopicReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
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

            try
            {
                readStatus = new TopicReadStatus(core, topicRow);
                readStatusLoaded = true;
            }
            catch (InvalidTopicReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
            }
        }

        void Topic_ItemLoad()
        {
            ItemDeleted += new ItemDeletedEventHandler(ForumTopic_ItemDeleted);
        }

        void ForumTopic_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            /* */
        }

        public new long Delete()
        {
            /* Do not delete sub items, post delete method will update post counts in a more efficient manner */
            return ((Item)this).Delete();
        }

        public static SelectQuery ForumTopic_GetSelectQueryStub(Core core)
        {
            SelectQuery query = ForumTopic.GetSelectQueryStub(typeof(ForumTopic));

            query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicPost)));
            query.AddJoin(JoinTypes.Left, TopicPost.GetTable(typeof(TopicPost)), "topic_last_post_id", "post_id");
            if (core.LoggedInMemberId > 0)
            {
                query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicReadStatus)));
                TableJoin tj1 = query.AddJoin(JoinTypes.Left, TopicReadStatus.GetTable(typeof(TopicReadStatus)), "topic_id", "topic_id");
                tj1.AddCondition("`forum_topic_read_status`.`user_id`", core.LoggedInMemberId);
            }

            query.AddSort(SortOrder.Descending, "topic_last_post_time_ut");

            return query;
        }

        public static ForumTopic Create(Core core, Forum forum, string subject, string text, TopicStates status)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (forum == null)
            {
                throw new InvalidForumException();
            }
            
            core.Db.BeginTransaction();

            if (!forum.Access.Can("CREATE_TOPICS"))
            {
                // todo: throw new exception
                throw new UnauthorisedToCreateItemException();
            }

            if ((status == TopicStates.Announcement || status == TopicStates.Global) && (!forum.Access.Can("CREATE_ANNOUNCEMENTS")))
            {
                throw new UnauthorisedToCreateItemException();
            }

            if (status == TopicStates.Sticky && (!forum.Access.Can("CREATE_STICKY")))
            {
                throw new UnauthorisedToCreateItemException();
            }

            if (forum.Owner is UserGroup)
            {
                ForumSettings settings = new ForumSettings(core, (UserGroup)forum.Owner);

                if (forum.Id == 0 && (!settings.AllowTopicsAtRoot))
                {
                    throw new UnauthorisedToCreateItemException();
                }

                if (!((UserGroup)forum.Owner).IsGroupOperator(core.Session.LoggedInMember.ItemKey))
                {
                    status = TopicStates.Normal;
                }
            }

            InsertQuery iquery = new InsertQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            iquery.AddField("forum_id", forum.Id);
            iquery.AddField("topic_title", subject);
            iquery.AddField("user_id", core.LoggedInMemberId);
            iquery.AddField("topic_posts", 0);
            iquery.AddField("topic_views", 0);
            iquery.AddField("topic_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("topic_modified_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("topic_last_post_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("topic_status", (byte)status);
            iquery.AddField("topic_locked", false);
            iquery.AddField("topic_last_post_id", 0);
            iquery.AddField("topic_first_post_id", 0);
			iquery.AddField("topic_item_id", forum.Owner.Id);
			iquery.AddField("topic_item_type_id", forum.Owner.TypeId);

            long topicId = core.Db.Query(iquery);

            ForumTopic topic = new ForumTopic(core, forum, topicId);

            TopicPost post = TopicPost.Create(core, forum, topic, subject, text);

            UpdateQuery uQuery = new UpdateQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            uQuery.AddField("topic_first_post_id", post.Id);
            uQuery.AddField("topic_last_post_id", post.Id);
            uQuery.AddField("topic_last_post_time_ut", post.TimeCreatedRaw);
            uQuery.AddCondition("topic_id", topic.Id);

            long rowsUpdated = core.Db.Query(uQuery);
			
			topic.firstPostId = post.Id;
			topic.lastPostId = post.Id;
			topic.lastPostTimeRaw = post.TimeCreatedRaw;

            if (rowsUpdated != 1)
            {
                core.Db.RollBackTransaction();
                core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            if (forum.Id > 0)
            {
                List<long> parentForumIds = new List<long>();
                parentForumIds.Add(forum.Id);

                if (forum.Parents != null)
                {
                    foreach (ParentTreeNode ptn in forum.Parents.Nodes)
                    {
                        parentForumIds.Add(ptn.ParentId);
                    }
                }

                uQuery = new UpdateQuery(Forum.GetTable(typeof(Forum)));
                uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, 1));
                uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Addition, 1));
                uQuery.AddField("forum_last_post_id", post.Id);
                uQuery.AddField("forum_last_post_time_ut", post.TimeCreatedRaw);
                uQuery.AddCondition("forum_id", ConditionEquality.In, parentForumIds);

                rowsUpdated = core.Db.Query(uQuery);

                if (rowsUpdated < 1)
                {
                    core.Db.RollBackTransaction();
                    core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
                }
            }

            uQuery = new UpdateQuery(ForumSettings.GetTable(typeof(ForumSettings)));
            uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, 1));
			uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Addition, 1));
            uQuery.AddCondition("forum_item_id", forum.Owner.Id);
            uQuery.AddCondition("forum_item_type_id", forum.Owner.TypeId);

            rowsUpdated = core.Db.Query(uQuery);

            if (rowsUpdated != 1)
            {
                core.Db.RollBackTransaction();
                core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            /*uQuery = new UpdateQuery(ForumMember.GetTable(typeof(ForumMember)));
            uQuery.AddField("posts", new QueryOperation("posts", QueryOperations.Addition, 1));
            uQuery.AddCondition("user_id", core.session.LoggedInMember.Id);
            uQuery.AddCondition("item_id", forum.Owner.Id);
            uQuery.AddCondition("item_type_id", forum.Owner.TypeId);

            rowsUpdated = core.db.Query(uQuery);

            if (rowsUpdated == 0)
            {
                ForumMember fm = ForumMember.Create(core, forum.Owner, core.session.LoggedInMember, true);

                uQuery = new UpdateQuery(ForumMember.GetTable(typeof(ForumMember)));
                uQuery.AddField("posts", new QueryOperation("posts", QueryOperations.Addition, 1));
                uQuery.AddCondition("user_id", core.session.LoggedInMember.Id);
                uQuery.AddCondition("item_id", forum.Owner.Id);
                uQuery.AddCondition("item_type_id", forum.Owner.TypeId);

                core.db.Query(uQuery);
            }*/

            ForumMember fm = null;

            try
            {
                fm = new ForumMember(core, forum.Owner, core.Session.LoggedInMember);
            }
            catch (InvalidForumMemberException)
            {
                fm = ForumMember.Create(core, forum.Owner, core.Session.LoggedInMember, true);
            }

            fm.ForumPosts += 1;

            /*Dictionary<long, ForumMemberRank> ranks = ForumMemberRank.GetRanks(core, forum.Owner);

            if (!(ranks.ContainsKey(fm.ForumRankId) && ranks[fm.ForumRankId].RankSpecial))
            {
                int rankLastMin = 0;
                foreach (ForumMemberRank rank in ranks.Values)
                {
                    if ((!rank.RankSpecial) && fm.ForumPosts >= rank.RankPosts && rank.RankPosts > rankLastMin)
                    {
                        fm.ForumRankId = rank.Id;
                        rankLastMin = rank.RankPosts;
                    }
                }
            }*/

            fm.Update(typeof(ForumMember));

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
                core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }

            if (forumId > 0)
            {
                List<long> parentForumIds = new List<long>();
                parentForumIds.Add(Forum.Id);

                if (Forum.Parents != null)
                {
                    foreach (ParentTreeNode ptn in Forum.Parents.Nodes)
                    {
                        parentForumIds.Add(ptn.ParentId);
                    }
                }

                uQuery = new UpdateQuery(Forum.GetTable(typeof(Forum)));
                uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, 1));
                uQuery.AddField("forum_last_post_id", post.Id);
                uQuery.AddField("forum_last_post_time_ut", post.TimeCreatedRaw);
                uQuery.AddCondition("forum_id", ConditionEquality.In, parentForumIds);

                rowsUpdated = db.Query(uQuery);

                if (rowsUpdated < 1)
                {
                    db.RollBackTransaction();
                    core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
                }
            }

            uQuery = new UpdateQuery(ForumSettings.GetTable(typeof(ForumSettings)));
            uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, 1));
            uQuery.AddCondition("forum_item_id", Forum.Owner.Id);
            uQuery.AddCondition("forum_item_type_id", Forum.Owner.TypeId);

            rowsUpdated = db.Query(uQuery);

            if (rowsUpdated != 1)
            {
                db.RollBackTransaction();
                core.Display.ShowMessage("ERROR", "Error, rolling back transaction");
            }
			
			/*uQuery = new UpdateQuery(ForumMember.GetTable(typeof(ForumMember)));
            uQuery.AddField("posts", new QueryOperation("posts", QueryOperations.Addition, 1));
			uQuery.AddCondition("user_id", core.session.LoggedInMember.Id);
            uQuery.AddCondition("item_id", Forum.Owner.Id);
            uQuery.AddCondition("item_type_id", Forum.Owner.TypeId);
				
			rowsUpdated = db.Query(uQuery);
			
			if (rowsUpdated == 0)
			{
				ForumMember fm = ForumMember.Create(core, Forum.Owner, core.session.LoggedInMember, true);

                uQuery = new UpdateQuery(ForumMember.GetTable(typeof(ForumMember)));
                uQuery.AddField("posts", new QueryOperation("posts", QueryOperations.Addition, 1));
                uQuery.AddCondition("user_id", core.session.LoggedInMember.Id);
                uQuery.AddCondition("item_id", Forum.Owner.Id);
                uQuery.AddCondition("item_type_id", Forum.Owner.TypeId);

                db.Query(uQuery);
			}*/

            ForumMember fm = null;

            try
            {
                fm = new ForumMember(core, Forum.Owner, core.Session.LoggedInMember);
            }
            catch (InvalidForumMemberException)
            {
                fm = ForumMember.Create(core, Forum.Owner, core.Session.LoggedInMember, true);
            }

            fm.ForumPosts += 1;

            /*Dictionary<long, ForumMemberRank> ranks = ForumMemberRank.GetRanks(core, Forum.Owner);

            if (!(ranks.ContainsKey(fm.ForumRankId) && ranks[fm.ForumRankId].RankSpecial))
            {
                int rankLastMin = 0;
                foreach (ForumMemberRank rank in ranks.Values)
                {
                    if ((!rank.RankSpecial) && fm.ForumPosts >= rank.RankPosts && rank.RankPosts > rankLastMin)
                    {
                        fm.ForumRankId = rank.Id;
                        rankLastMin = rank.RankPosts;
                    }
                }
            }*/

            fm.Update(typeof(ForumMember));

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
                query.AddCondition("post_id", ConditionEquality.LessThan, m);

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
            return getSubItems(typeof(TopicPost), currentPage, perPage, true, "post_time_ut", true).ConvertAll<TopicPost>(new Converter<Item, TopicPost>(convertToTopicPost));
        }

        public List<TopicPost> GetLastPosts(int number)
        {
            return getSubItems(typeof(TopicPost), 1, number, true, "post_time_ut", false).ConvertAll<TopicPost>(new Converter<Item, TopicPost>(convertToTopicPost));
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

        public override string Uri
        {
            get
            {

                if (forumId == 0)
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}forum/topic-{1}",
                        Forum.Owner.UriStub, topicId));
                }
                else
                {
                    return core.Hyperlink.AppendSid(string.Format("{0}forum/{1}/topic-{2}",
                        Forum.Owner.UriStub, Forum.Id, topicId));
                }

            }
        }

        public string ReplyUri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}forum/post?t={1}&mode=reply",
                    Forum.Owner.UriStub, topicId));
            }
        }

        public void Read(TopicPost lastVisiblePost)
        {
            //db.BeginTransaction();

            UpdateQuery uQuery = new UpdateQuery(ForumTopic.GetTable(typeof(ForumTopic)));
            uQuery.AddField("topic_views", new QueryOperation("topic_views", QueryOperations.Addition, 1));
            uQuery.AddCondition("topic_id", topicId);

            db.Query(uQuery);

            if ((!IsRead) && core.LoggedInMemberId > 0)
            {
                if (readStatus != null)
                {
                    if (readStatus.ReadTimeRaw < lastVisiblePost.TimeCreatedRaw)
                    {
                        UpdateQuery uQuery2 = new UpdateQuery(TopicReadStatus.GetTable(typeof(TopicReadStatus)));
                        uQuery2.AddField("read_time_ut", lastVisiblePost.TimeCreatedRaw);
                        uQuery2.AddCondition("topic_id", topicId);
                        uQuery2.AddCondition("user_id", core.LoggedInMemberId);

                        db.Query(uQuery2);
                    }
                }
                else
                {
                    TopicReadStatus.Create(core, this, lastVisiblePost);
                }
            }
        }

        public static void Show(Core core, GPage page, long forumId, long topicId)
        {
            long m = core.Functions.RequestLong("m", 0); // post, seeing as p has been globally reserved for page and cannot be used for post, we use m for message
            Forum thisForum = null;

            core.Template.SetTemplate("Forum", "viewtopic");
            ForumSettings.ShowForumHeader(core, page);

            ForumSettings settings = new ForumSettings(core, page.Owner);

            try
            {
                if (forumId == 0)
                {
                    thisForum = new Forum(page.Core, page.Group);
                }
                else
                {
                    thisForum = new Forum(page.Core, forumId);
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

                if (!thisForum.Access.Can("VIEW_TOPICS"))
                {
                    core.Functions.Generate403();
                    return;
                }

                if (page is GPage)
                {
                    if (core.LoggedInMemberId > 0 && (!((GPage)page).Group.IsGroupMember(core.Session.LoggedInMember.ItemKey)))
                    {
                        core.Template.Parse("U_JOIN", ((GPage)page).Group.JoinUri);
                    }
                }

                core.Template.Parse("TOPIC_TITLE", thisTopic.Title);

                List<TopicPost> posts;
                if (m > 0)
                {
                    posts = thisTopic.GetPosts(m, settings.PostsPerPage);
                }
                else
                {
                    posts = thisTopic.GetPosts(page.TopLevelPageNumber, settings.PostsPerPage);
                }

                core.Template.Parse("POSTS", posts.Count.ToString());
				
				List<long> posterIds = new List<long>();
				List<long> rankIds = new List<long>();
				
				foreach (TopicPost post in posts)
				{
					if (!posterIds.Contains(post.UserId))
					{
						posterIds.Add(post.UserId);
					}
				}
				
				Dictionary<long, ForumMember> postersList = ForumMember.GetMembers(core, thisForum.Owner, posterIds);
				
				foreach (ForumMember fm in postersList.Values)
				{
					if (!rankIds.Contains(fm.ForumRankId))
					{
						rankIds.Add(fm.ForumRankId);
					}
				}
				
				/*Dictionary<long, ForumMemberRank> ranksList = null;
				
				if (rankIds.Count > 0)
				{
					ranksList = ForumMemberRank.GetRanks(core, thisForum.Owner, rankIds);
				}
				else
				{
					ranksList = new Dictionary<long, ForumMemberRank>();
				}*/

                Dictionary<long, ForumMemberRank> ranksList = ForumMemberRank.GetRanks(core, thisForum.Owner);

                foreach (ForumMember fm in postersList.Values)
                {
                    if (!(ranksList.ContainsKey(fm.ForumRankId) && ranksList[fm.ForumRankId].RankSpecial))
                    {
                        int rankLastMin = 0;
                        foreach (ForumMemberRank rank in ranksList.Values)
                        {
                            if ((!rank.RankSpecial) && fm.ForumPosts >= rank.RankPosts && rank.RankPosts > rankLastMin)
                            {
                                fm.ForumRankId = rank.Id;
                                rankLastMin = rank.RankPosts;
                            }
                        }
                    }
                }

                foreach (TopicPost post in posts)
                {
                    VariableCollection postVariableCollection = core.Template.CreateChild("post_list");

                    postVariableCollection.Parse("SUBJECT", post.Title);
					postVariableCollection.Parse("POST_TIME", core.Tz.DateTimeToString(post.GetCreatedDate(core.Tz)));
                    postVariableCollection.Parse("URI", post.Uri);
					//postVariableCollection.Parse("POST_MODIFIED", core.tz.DateTimeToString(post.GetModifiedDate(core.tz)));
                    postVariableCollection.Parse("ID", post.Id.ToString());
                    core.Display.ParseBbcode(postVariableCollection, "TEXT", post.Text);
                    if (postersList.ContainsKey(post.UserId))
                    {
                        postVariableCollection.Parse("U_USER", post.Poster.Uri);
                        postVariableCollection.Parse("USER_DISPLAY_NAME", postersList[post.UserId].UserInfo.DisplayName);
                        postVariableCollection.Parse("USER_TILE", postersList[post.UserId].UserTile);
                        postVariableCollection.Parse("USER_ICON", postersList[post.UserId].UserIcon);
                        postVariableCollection.Parse("USER_JOINED", core.Tz.DateTimeToString(postersList[post.UserId].UserInfo.GetRegistrationDate(core.Tz)));
                        postVariableCollection.Parse("USER_COUNTRY", postersList[post.UserId].Profile.Country);
                        postVariableCollection.Parse("USER_POSTS", postersList[post.UserId].ForumPosts.ToString());
                        core.Display.ParseBbcode(postVariableCollection, "SIGNATURE", postersList[post.UserId].ForumSignature);

                        if (ranksList.ContainsKey(postersList[post.UserId].ForumRankId))
                        {
                            postVariableCollection.Parse("USER_RANK", ranksList[postersList[post.UserId].ForumRankId].RankTitleText);
                        }
                    }
                    else
                    {
                        postVariableCollection.Parse("USER_DISPLAY_NAME", "Anonymous");
                    }

                    if (thisTopic.ReadStatus == null)
                    {
                        postVariableCollection.Parse("IS_READ", "FALSE");
                    }
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
                    }
                }

                if (posts.Count > 0)
                {
                    thisTopic.Read(posts[posts.Count - 1]);
                }

                if (thisForum.Access.Can("CREATE_TOPICS"))
                {
                    core.Template.Parse("U_NEW_TOPIC", thisForum.NewTopicUri);
                }
                if (thisForum.Access.Can("REPLY_TOPICS") && (!thisTopic.IsLocked))
                {
                    core.Template.Parse("U_NEW_REPLY", thisTopic.ReplyUri);
                }

                core.Display.ParsePagination(thisTopic.Uri, settings.PostsPerPage, thisTopic.Posts + 1);

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "forum", "Forum" });

                if (thisForum.Parents != null)
                {
                    foreach (ParentTreeNode ptn in thisForum.Parents.Nodes)
                    {
                        breadCrumbParts.Add(new string[] { "*" + ptn.ParentId.ToString(), ptn.ParentTitle });
                    }
                }
                
                if (thisForum.Id > 0)
                {
                    breadCrumbParts.Add(new string[] { thisForum.Id.ToString(), thisForum.Title });
                }

                breadCrumbParts.Add(new string[] { "topic-" + thisTopic.Id.ToString(), thisTopic.Title });

                page.Group.ParseBreadCrumbs(breadCrumbParts);
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
