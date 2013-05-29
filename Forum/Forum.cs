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
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Forms;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum")]
    [Permission("VIEW", "Can view the forum", PermissionTypes.View)]
    [Permission("LIST_TOPICS", "Can list the topics in the forum", PermissionTypes.View)]
    [Permission("VIEW_TOPICS", "Can view the topics in the forum", PermissionTypes.View)]
    [Permission("REPLY_TOPICS", "Can reply to the topics in the forum", PermissionTypes.Interact)]
    [Permission("CREATE_TOPICS", "Can post new topics", PermissionTypes.Interact)]
    [Permission("EDIT_POSTS", "Can edit posts", PermissionTypes.CreateAndEdit)]
    [Permission("EDIT_OWN_POSTS", "Can edit own posts", PermissionTypes.CreateAndEdit)]
    [Permission("DELETE_OWN_POSTS", "Can delete own posts", PermissionTypes.Delete)]
    [Permission("DELETE_TOPICS", "Can delete topics", PermissionTypes.Delete)]
    [Permission("LOCK_TOPICS", "Can lock topics", PermissionTypes.CreateAndEdit)]
    [Permission("MOVE_TOPICS", "Can move topics to/from forum", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_ANNOUNCEMENTS", "Can create announcements", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_STICKY", "Can create sticky topics", PermissionTypes.CreateAndEdit)]
    [Permission("REPORT_POSTS", "Can report posts", PermissionTypes.Interact)]
    public class Forum : NumberedItem, IPermissibleItem, IComparable, INestableItem, IOrderableItem
    {
        [DataField("forum_id", DataFieldKeys.Primary)]
        private long forumId;
        [DataField("forum_parent_id", typeof(Forum))]
        private long parentId;
        [DataField("forum_title", 127)]
        private string forumTitle;
        [DataField("forum_description", 1023)]
        private string forumDescription;
		[DataField("forum_rules", 1023)]
        private string forumRules;
        [DataField("forum_locked")]
        private bool forumLocked;
        [DataField("forum_category")]
        private bool isCategory;
        [DataField("forum_topics")]
        private long forumTopics;
        [DataField("forum_posts")]
        private long forumPosts;
        [DataField("forum_last_post_time_ut")]
        private long lastPostTimeRaw;
        [DataField("forum_last_post_id")]
        private long lastPostId;
		[DataField("forum_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("forum_order")]
        private int forumOrder;
		[DataField("forum_level")]
        private int forumLevel;
        [DataField("forum_parents", MYSQL_TEXT)]
        private string parents;
        [DataField("forum_simple_permissions")]
        private bool simplePermissions;

        private ForumSettings settings;
        private Primitive owner;
        private Access forumAccess;
        private ForumReadStatus readStatus = null;
        private bool readStatusLoaded;
        private ParentTree parentTree;

        public ForumReadStatus ReadStatus
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
                            readStatus = new ForumReadStatus(core, forumId);
                            readStatusLoaded = true;
                            return readStatus;
                        }
                        catch (InvalidForumReadStatusException)
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
		
		public string Rules
		{
			get
			{
				return forumRules;
			}
			set
			{
				SetProperty("forumRules", value);
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

        public long LastPostId
        {
            get
            {
                return lastPostId;
            }
        }

        public int Order
        {
            get
            {
                return forumOrder;
            }
            set
            {
                SetPropertyByRef(new { forumOrder }, value);
            }
        }
		
		public int Level
		{
			get
			{
				return forumLevel;
			}
            internal set
            {
                SetPropertyByRef(new { forumLevel }, value);
            }
		}

        public long ParentTypeId
        {
            get
            {
                return ItemType.GetTypeId(typeof(Forum));
            }
        }

        public long ParentId
        {
            get
            {
                return parentId;
            }
            set
            {
                SetPropertyByRef(new { parentId }, value);

                Forum parent;
                if (parentId > 0)
                {
                    parent = new Forum(core, parentId);
                }
                else
                {
                    if (Owner is UserGroup)
                    {
                        parent = new Forum(core, (UserGroup)Owner);
                    }
                    else
                    {
                        // ignore
                        parent = null;
                    }
                }

                parentTree = new ParentTree();

                if (parent.Parents != null)
                {
                    foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                    {
                        parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentId));
                    }
                }

                if (parent.Id > 0)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(parent.Title, parent.Id));
                }

                XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
                StringBuilder sb = new StringBuilder();
                StringWriter stw = new StringWriter(sb);

                xs.Serialize(stw, parentTree);
                stw.Flush();
                stw.Close();

                SetProperty("parents", sb.ToString());
            }
        }

        public bool IsLocked
        {
            get
            {
                return forumLocked;
            }
            set
            {
                SetProperty("forumLocked", value);
            }
        }
		
		public ParentTree GetParents()
		{
			return Parents;
		}

        internal string ParentsRaw
        {
            get
            {
                return parents;
            }
            set
            {
                SetPropertyByRef(new { parents }, value);
            }
        }

        public ParentTree Parents
        {
            get
            {
                if (parentTree == null)
                {
                    if (string.IsNullOrEmpty(parents))
                    {
                        return null;
                    }
                    else
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(ParentTree)); ;
                        StringReader sr = new StringReader(parents);

                        parentTree = (ParentTree)xs.Deserialize(sr);
                    }
                }

                return parentTree;
            }
        }

        public bool IsCategory
        {
            get
            {
                return isCategory;
            }
            set
            {
                SetProperty("isCategory", value);
            }
        }

        public Forum(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            forumId = 0;
            forumLocked = false;
            forumLevel = 0;
            forumTitle = "Forums";
        }

        public Forum(Core core, ForumSettings settings)
            : base(core)
        {
            this.settings = settings;
            this.owner = settings.Owner;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            forumId = 0;
            forumLocked = false;
			forumLevel = 0;
            forumTitle = "Forums";
        }

        public Forum(Core core, long forumId)
            : this(core, null, forumId)
        {
        }

        public Forum(Core core, ForumSettings settings, long forumId)
            : base(core)
        {
            this.settings = settings;
            ItemLoad += new ItemLoadHandler(Forum_ItemLoad);

            SelectQuery query = Forum_GetSelectQueryStub(core);
            query.AddCondition("`forum`.`forum_id`", forumId);

            DataTable forumTable = db.Query(query);

            if (forumTable.Rows.Count == 1)
            {
                loadItemInfo(forumTable.Rows[0]);
            }
            else
            {
                throw new InvalidForumException();
            }

            try
            {
                readStatus = new ForumReadStatus(core, forumTable.Rows[0]);
                readStatusLoaded = true;
            }
            catch (InvalidForumReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
            }
        }

        public Forum(Core core, DataRow forumDataRow)
            : this(core, (ForumSettings)null, forumDataRow)
        {
        }

        public Forum(Core core, ForumSettings settings, DataRow forumDataRow)
            : base(core)
        {
            this.settings = settings;
            ItemLoad += new ItemLoadHandler(Forum_ItemLoad);

            try
            {
                loadItemInfo(forumDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumException();
            }

            try
            {
                readStatus = new ForumReadStatus(core, forumDataRow);
                readStatusLoaded = true;
            }
            catch (InvalidForumReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
            }
        }

        public Forum(Core core, UserGroup owner, DataRow forumDataRow)
            : this(core, (ForumSettings)null, owner, forumDataRow)
        {
        }

        public Forum(Core core, ForumSettings settings, UserGroup owner, DataRow forumDataRow)
            : base(core)
        {
            this.owner = owner;
            this.settings = settings;
            ItemLoad += new ItemLoadHandler(Forum_ItemLoad);

            try
            {
                loadItemInfo(forumDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumException();
            }

            try
            {
                readStatus = new ForumReadStatus(core, forumDataRow);
                readStatusLoaded = true;
            }
            catch (InvalidForumReadStatusException)
            {
                readStatus = null;
                readStatusLoaded = true;
            }
        }

        void Forum_ItemLoad()
        {
            ItemDeleted += new ItemDeletedEventHandler(Forum_ItemDeleted);
        }

        void Forum_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            long postAdjust = forumPosts;
            long topicAdjust = forumTopics;
            List<long> parentIds = new List<long>();
            List<long> childIds = new List<long>();
            childIds.Add(Id);

            List<Forum> children = GetForums();

            /* Delete Children First */
            foreach (Forum child in children)
            {
                childIds.Add(child.Id);
                child.Delete(true);
            }

            if (!e.ParentDeleted)
            {
                foreach (ParentTreeNode parent in Parents.Nodes)
                {
                    parentIds.Add(parent.ParentId);
                }

                /* Update parent forums */
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                    uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Subtraction, postAdjust));
                    uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Subtraction, topicAdjust));
                    uQuery.AddCondition("forum_id", ConditionEquality.In, parentIds);

                    db.Query(uQuery);
                }

                /* Update forum statistics */
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(ForumSettings));
                    uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Subtraction, postAdjust));
                    uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Subtraction, topicAdjust));
                    uQuery.AddCondition("forum_item_id", ownerKey.Id);
                    uQuery.AddCondition("forum_item_type_id", ownerKey.TypeId);

                    db.Query(uQuery);
                }

                /* Delete topics */
                {
                    DeleteQuery dQuery = new DeleteQuery(typeof(ForumTopic));
                    dQuery.AddCondition("forum_id", ConditionEquality.In, childIds);

                    db.Query(dQuery);
                }

                /* Delete posts and update post counts */
                {
                    DeleteQuery dQuery = new DeleteQuery(typeof(TopicPost));
                    dQuery.AddCondition("forum_id", ConditionEquality.In, childIds);

                    db.Query(dQuery);
                }

                /* */
                {
                    DeleteQuery dQuery = new DeleteQuery(typeof(TopicReadStatus));
                    dQuery.AddCondition("forum_id", ConditionEquality.In, childIds);

                    db.Query(dQuery);
                }

                /* */
                {
                    DeleteQuery dQuery = new DeleteQuery(typeof(ForumReadStatus));
                    dQuery.AddCondition("forum_id", ConditionEquality.In, childIds);

                    db.Query(dQuery);
                }
            }
        }

        public new long Delete()
        {
            /* Do not delete sub items, post delete method will update post counts in a more efficient manner */
            return ((Item)this).Delete();
        }

        public static SelectQuery Forum_GetSelectQueryStub(Core core)
        {
            SelectQuery query = Forum.GetSelectQueryStub(typeof(Forum));

            if (core.LoggedInMemberId > 0)
            {
                query.AddFields(ForumReadStatus.GetFieldsPrefixed(typeof(ForumReadStatus)));
                TableJoin tj1 = query.AddJoin(JoinTypes.Left, ForumReadStatus.GetTable(typeof(ForumReadStatus)), "forum_id", "forum_id");
                tj1.AddCondition("`forum_read_status`.`user_id`", core.LoggedInMemberId);
            }

            // TODO: forum sort order
            //query.AddSort(SortOrder.Descending, "forum_order");

            return query;
        }
		
		public List<Item> GetChildren()
		{
			List<Item> ret = new List<Item>();
			
			foreach (Item i in GetForums())
			{
				ret.Add(i);
			}
			
			return ret;
		}

        public List<Forum> GetForums()
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_item_id", ownerKey.Id);
            query.AddCondition("forum_item_type_id", ownerKey.TypeId);
            query.AddCondition("forum_parent_id", forumId);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                if (Owner is UserGroup)
                {
                    forums.Add(new Forum(core, (UserGroup)Owner, dr));
                }
                else
                {
                    forums.Add(new Forum(core, dr));
                }
            }

            return forums;
        }

        public static List<Forum> GetForums(Core core, Primitive owner, List<long> forumIds)
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_id", ConditionEquality.In, forumIds);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = core.Db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                if (owner is UserGroup)
                {
                    forums.Add(new Forum(core, (UserGroup)owner, dr));
                }
                else
                {
                    forums.Add(new Forum(core, dr));
                }
            }

            return forums;
        }

        public static List<Forum> GetSubForums(Core core, Primitive owner, List<long> forumIds)
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_parent_id", ConditionEquality.In, forumIds);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = core.Db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                if (owner is UserGroup)
                {
                    forums.Add(new Forum(core, (UserGroup)owner, dr));
                }
                else
                {
                    forums.Add(new Forum(core, dr));
                }
            }

            return forums;
        }
		
		public static List<Forum> GetForumLevels(Core core, Forum parent, int levels)
		{
			List<Forum> forums = new List<Forum>();
			
			SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
			query.AddCondition("forum_item_id", parent.Owner.Id);
			query.AddCondition("forum_item_type_id", parent.Owner.TypeId);
            query.AddCondition("forum_level", ConditionEquality.GreaterThan, parent.Level);
			query.AddCondition("forum_level", ConditionEquality.LessThanEqual, parent.Level + levels);
			query.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, parent.Order);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = core.Db.Query(query);

			bool isFirst = true;
			long topLevelParent = -1;
            foreach (DataRow dr in forumsTable.Rows)
            {
				Forum forum;
                if (parent.Owner is UserGroup)
                {
                    forum = new Forum(core, parent.Settings, (UserGroup)parent.Owner, dr);
                }
                else
                {
                    forum = new Forum(core, parent.Settings, dr);
                }
				
				if (isFirst)
				{
					if (forum.Order != parent.Order + 1 && forum.Order != 0)
					{
						break;
					}
					isFirst = false;
				}
				
				if (topLevelParent == -1)
				{
					forums.Add(forum);
					topLevelParent = forum.parentId;
				}
				else
				{
					if ((forum.Level == (parent.Level + 1)) && (forum.parentId != topLevelParent))
					{
						break;
					}
					else
					{
						forums.Add(forum);
					}
				}
            }
			
			return forums;
		}
		
		public static SelectBox BuildForumJumpBox(Core core, Primitive owner, long currentForum)
		{
			SelectBox sb = new SelectBox("forum");
			
			sb.Add(new SelectBoxItem("", "Select a forum"));
			sb.Add(new SelectBoxItem("", "--------------------"));
			
			SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
			query.AddCondition("forum_item_id", owner.Id);
			query.AddCondition("forum_item_type_id", owner.TypeId);
            query.AddSort(SortOrder.Ascending, "forum_order");
			
			DataTable forumsTable = core.Db.Query(query);
			
			foreach (DataRow dr in forumsTable.Rows)
            {
				Forum forum;
                if (owner is UserGroup)
                {
                    forum = new Forum(core, (UserGroup)owner, dr);
                }
                else
                {
                    forum = new Forum(core, dr);
                }
				if (forum != null)
				{
					if (forum.Access.Can("VIEW"))
					{
						sb.Add(new SelectBoxItem(forum.Id.ToString(), forum.Title));
					}
				}
            }
			
			if (sb.ContainsKey(currentForum.ToString()))
			{
				sb.SelectedKey = currentForum.ToString();
			}
			
			return sb;
		}

        public List<ForumTopic> GetTopics(int currentPage, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            //return getSubItems(typeof(ForumTopic), currentPage, perPage, true).ConvertAll<ForumTopic>(new Converter<Item, ForumTopic>(convertToForumTopic));

            SelectQuery query = ForumTopic.GetSelectQueryStub(typeof(ForumTopic));

            query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicPost)));
            query.AddJoin(JoinTypes.Left, TopicPost.GetTable(typeof(TopicPost)), "topic_last_post_id", "post_id");
            if (core.LoggedInMemberId > 0)
            {
                query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicReadStatus)));
                TableJoin tj1 = query.AddJoin(JoinTypes.Left, TopicReadStatus.GetTable(typeof(TopicReadStatus)), "topic_id", "topic_id");
                tj1.AddCondition("`forum_topic_read_status`.`user_id`", core.LoggedInMemberId);
            }

            query.AddCondition("`forum_topics`.`forum_id`", forumId);
			query.AddCondition("`forum_topics`.`topic_item_id`", Owner.Id);
			query.AddCondition("`forum_topics`.`topic_item_type_id`", Owner.TypeId);
            query.AddCondition("topic_status", ConditionEquality.NotIn, new byte[] { (byte)TopicStates.Global, (byte)TopicStates.Announcement });
            query.AddSort(SortOrder.Descending, "topic_status");
            query.AddSort(SortOrder.Descending, "topic_last_post_time_ut");
            query.LimitStart = (currentPage - 1) * perPage;
            query.LimitCount = perPage;

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, this, dr));
            }

            return topics;
        }

        public List<ForumTopic> GetAnnouncements()
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            SelectQuery query = ForumTopic.GetSelectQueryStub(typeof(ForumTopic));

            query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicPost)));
            query.AddJoin(JoinTypes.Left, TopicPost.GetTable(typeof(TopicPost)), "topic_last_post_id", "post_id");
            if (core.LoggedInMemberId > 0)
            {
                query.AddFields(TopicPost.GetFieldsPrefixed(typeof(TopicReadStatus)));
                TableJoin tj1 = query.AddJoin(JoinTypes.Left, TopicReadStatus.GetTable(typeof(TopicReadStatus)), "topic_id", "topic_id");
                tj1.AddCondition("`forum_topic_read_status`.`user_id`", core.LoggedInMemberId);
            }

            query.AddCondition("`forum_topics`.`forum_id`", forumId);
			query.AddCondition("`forum_topics`.`topic_item_id`", Owner.Id);
			query.AddCondition("`forum_topics`.`topic_item_type_id`", Owner.TypeId);
            query.AddCondition("topic_status", ConditionEquality.In, new byte[] { (byte)TopicStates.Global, (byte)TopicStates.Announcement });
            query.AddSort(SortOrder.Descending, "topic_status");
            query.AddSort(SortOrder.Descending, "topic_last_post_time_ut");

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, this, dr));
            }

            return topics;
        }

        public ForumTopic convertToForumTopic(Item input)
        {
            return (ForumTopic)input;
        }

        /*public List<ForumTopic> GetTopics(int currentPage, int perPage)
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
                ForumTopic.FORUM_TOPIC_INFO_FIELDS, owner.Id, owner.GetType()));*\/

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
            }

            return topics;
        }*/

        public List<ForumTopic> GetTopicsFlat(int currentPge, int perPage)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            SelectQuery query = new SelectQuery("forum_topics");
            query.AddCondition("topic_item_id", ownerKey.Id);
            query.AddCondition("topic_item_type_id", ownerKey.TypeId);
            query.AddSort(SortOrder.Descending, "topic_last_post_id");
            query.LimitStart = (currentPge - 1) * perPage;
            query.LimitCount = perPage;

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
            }

            return topics;
        }

        public List<ForumTopic> GetTopicsFlat(params long[] topicIds)
        {
            List<ForumTopic> topics = new List<ForumTopic>();

            SelectQuery query = new SelectQuery("forum_topics");
            query.AddCondition("topic_id", ConditionEquality.In, topicIds);
            query.AddCondition("topic_item_id", ownerKey.Id);
            query.AddCondition("topic_item_type_id", ownerKey.TypeId);
            query.AddSort(SortOrder.Descending, "topic_last_post_id");

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
            }

            return topics;
        }

        public void LockTopics(List<long> topicIds)
        {
            if (topicIds.Count > 0)
            {
                if (Access.Can("LOCK_TOPICS"))
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(ForumTopic));
                    uQuery.AddField("topic_locked", true);
                    uQuery.AddCondition("forum_id", Id);
                    uQuery.AddCondition("topic_id", ConditionEquality.In, topicIds);

                    db.Query(uQuery);
                }
                /*else
                {
                    throw new UnauthorisedToUpdateItemException();
                }*/
            }
        }

        public void UnLockTopics(List<long> topicIds)
        {
            if (topicIds.Count > 0)
            {
                if (Access.Can("LOCK_TOPICS"))
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(ForumTopic));
                    uQuery.AddField("topic_locked", false);
                    uQuery.AddCondition("forum_id", Id);
                    uQuery.AddCondition("topic_id", ConditionEquality.In, topicIds);

                    db.Query(uQuery);
                }
                /*else
                {
                    throw new UnauthorisedToUpdateItemException();
                }*/
            }
        }

        public void DeleteTopics(List<long> topicIds)
        {
            if (Access.Can("DELETE_TOPICS"))
            {
                foreach (long topicId in topicIds)
                {
                    Item.DeleteItem(typeof(ForumTopic), topicId);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="parent"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="rules"></param>
        /// <param name="permissions"></param>
        /// <param name="isCategory"></param>
        /// <returns></returns>
        /// <exception cref="NullCoreException"></exception>
        /// <exception cref="InvalidForumException"></exception>
        /// <exception cref="UnauthorisedToCreateItemException"></exception>
        public static Forum Create(Core core, Forum parent, string title, string description, string rules, ushort permissions, bool isCategory)
        {
            string parents;
            int order = 0;
			int level = 0;
			
            //core.db.BeginTransaction();

            if (core == null)
            {
                throw new NullCoreException();
            }

            if (parent == null)
            {
                throw new InvalidForumException();
            }
			
			level = parent.Level + 1;

            if (parent.Owner is UserGroup)
            {
                if (!((UserGroup)parent.Owner).IsGroupOperator(core.Session.LoggedInMember.ItemKey))
                {
                    // todo: throw new exception
                    throw new UnauthorisedToCreateItemException();
                }
            }

            SelectQuery query = new SelectQuery(GetTable(typeof(Forum)));
            query.AddFields("forum_order");
            query.AddCondition("forum_order", ConditionEquality.GreaterThan, parent.Order);
			query.AddCondition("forum_item_id", parent.Owner.Id);
            query.AddCondition("forum_item_type_id", parent.Owner.TypeId);
            query.AddCondition("forum_parent_id", parent.Id);
            query.AddSort(SortOrder.Descending, "forum_order");
            query.LimitCount = 1;

            DataTable orderTable = core.Db.Query(query);

            if (orderTable.Rows.Count == 1)
            {
                order = (int)orderTable.Rows[0]["forum_order"] + 1;
            }
            else
            {
                order = parent.Order + 1;
            }
			
			// increment all items below in the order
			UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(Forum)));
            uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Addition, 1));
            uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, order);
            uQuery.AddCondition("forum_item_id", parent.Owner.Id);
            uQuery.AddCondition("forum_item_type_id", parent.Owner.TypeId);

            core.Db.Query(uQuery);

            ParentTree parentTree = new ParentTree();

            if (parent.Parents != null)
            {
                foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentId));
                }
            }

            if (parent.Id > 0)
            {
                parentTree.Nodes.Add(new ParentTreeNode(parent.Title, parent.Id));
            }

            XmlSerializer xs = new XmlSerializer(typeof(ParentTree));
            StringBuilder sb = new StringBuilder();
            StringWriter stw = new StringWriter(sb);

            xs.Serialize(stw, parentTree);
            stw.Flush();
            stw.Close();

            parents = sb.ToString();

            InsertQuery iquery = new InsertQuery(GetTable(typeof(Forum)));
            iquery.AddField("forum_parent_id", parent.Id);
            iquery.AddField("forum_title", title);
            iquery.AddField("forum_description", description);
            iquery.AddField("forum_rules", rules);
            iquery.AddField("forum_order", order);
			iquery.AddField("forum_level", level);
            iquery.AddField("forum_category", isCategory);
            iquery.AddField("forum_locked", false);
            iquery.AddField("forum_topics", 0);
            iquery.AddField("forum_posts", 0);
            iquery.AddField("forum_item_id", parent.Owner.Id);
            iquery.AddField("forum_item_type_id", parent.Owner.TypeId);
            iquery.AddField("forum_parents", parents);
            iquery.AddField("forum_last_post_id", 0);
            iquery.AddField("forum_last_post_time_ut", UnixTime.UnixTimeStamp());

            long forumId = core.Db.Query(iquery);

            Forum forum = new Forum(core, forumId);

            /* LOAD THE DEFAULT ITEM PERMISSIONS */
            //Access.CreateAllGrantsForOwner(core, forum);

            if (parent.Owner is UserGroup)
            {
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "VIEW");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "VIEW_TOPICS");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "LIST_TOPICS");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "REPLY_TOPICS");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "CREATE_TOPICS");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupMembersGroupKey, "REPORT_POSTS");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupOperatorsGroupKey, "CREATE_STICKY");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupOfficersGroupKey, "CREATE_STICKY");
                Access.CreateGrantForPrimitive(core, forum, UserGroup.GroupOperatorsGroupKey, "CREATE_ANNOUNCEMENT");
            }

            return forum;
        }

        public void MoveUp()
        {
            if (Order == 0)
            {
                return;
            }

            SelectQuery query = Forum.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_parent_id", ParentId);
            query.AddCondition("forum_item_id", Owner.Id);
            query.AddCondition("forum_item_type_id", Owner.TypeId);
            query.AddCondition("forum_order", ConditionEquality.LessThan, Order);
            query.AddSort(SortOrder.Descending, "forum_order");
            query.LimitCount = 2;

            DataTable levelForumsDataTable = db.Query(query);

            Forum record0 = null;
            Forum record1 = null;
            int difference = 0;
            int differenceAbove = 0;

            if (levelForumsDataTable.Rows.Count == 0)
            {
                /* Cannot move up */
                return;
            }

            if (levelForumsDataTable.Rows.Count >= 1)
            {
                record0 = new Forum(core, levelForumsDataTable.Rows[0]);
                difference = record0.Order - Order;
            }

            if (levelForumsDataTable.Rows.Count >= 2)
            {
                record1 = new Forum(core, levelForumsDataTable.Rows[1]);
                differenceAbove = record1.Order - record0.Order;
            }

            // Get IDs of forums from this to next forum down
            // move all up by difference

            // Get IDs of foruns from next forum up to this
            // move all down by difference2

        }

        public void MoveDown()
        {
            SelectQuery query = Forum.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_parent_id", ParentId);
			query.AddCondition("forum_item_id", Owner.Id);
			query.AddCondition("forum_item_type_id", Owner.TypeId);
            query.AddCondition("forum_order", ConditionEquality.GreaterThan, Order);
            query.AddSort(SortOrder.Ascending, "forum_order");
            query.LimitCount = 2;

            DataTable levelForumsDataTable = db.Query(query);

            Forum record0 = null;
            Forum record1 = null;
            int difference = 0;
            int differenceBelow = 0;

            if (levelForumsDataTable.Rows.Count == 0)
            {
                /* Cannot move down */
                return;
            }
            if (levelForumsDataTable.Rows.Count >= 1)
            {
                record0 = new Forum(core, levelForumsDataTable.Rows[0]);
                difference = record0.Order - Order;
            }
            if (levelForumsDataTable.Rows.Count >= 2)
            {
                record1 = new Forum(core, levelForumsDataTable.Rows[1]);
                differenceBelow = record1.Order - record0.Order;
            }

            query = Forum.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, Order);
            if (record0 != null)
            {
                query.AddCondition("forum_order", ConditionEquality.LessThan, record0.Order);
            }
            else
            {
                /* TODO: test */
                query = Forum.GetSelectQueryStub(typeof(Forum));
                query.AddCondition("forum_parent_id", ParentId); /* or any level below */
				query.AddCondition("forum_item_id", Owner.Id);
				query.AddCondition("forum_item_type_id", Owner.TypeId);
                query.AddCondition("forum_order", ConditionEquality.GreaterThan, Order);
                query.AddSort(SortOrder.Descending, "forum_order");
                query.LimitCount = 1;

                levelForumsDataTable = core.Db.Query(query);

                Forum record = null;
                if (levelForumsDataTable.Rows.Count == 1)
                {
                    record = new Forum(core, levelForumsDataTable.Rows[0]);

                    query.AddCondition("forum_order", ConditionEquality.LessThanEqual, record.Order);
                }
            }

            List<long> updateIds = new List<long>();

            foreach (DataRow dr in db.Query(query).Rows)
            {
                updateIds.Add((long)dr["forum_id"]);
            }

            db.BeginTransaction();
            UpdateQuery uQuery;

            if (record0 != null)
            {
                uQuery = new UpdateQuery(Item.GetTable(typeof(Forum)));
                uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Subtraction, difference));
                uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, record0.Order);
                if (record1 != null)
                {
                    uQuery.AddCondition("forum_order", ConditionEquality.LessThan, record1.Order);
                }
                else
                {
                    /* TODO: test */
                    query = Forum.GetSelectQueryStub(typeof(Forum));
                    query.AddCondition("forum_parent_id", ParentId); /* or any level below */
					query.AddCondition("forum_item_id", Owner.Id);
					query.AddCondition("forum_item_type_id", Owner.TypeId);
                    query.AddCondition("forum_order", ConditionEquality.GreaterThan, Order);
                    query.AddSort(SortOrder.Descending, "forum_order");
                    query.LimitCount = 1;

                    levelForumsDataTable = core.Db.Query(query);

                    Forum record = null;
                    if (levelForumsDataTable.Rows.Count == 1)
                    {
                        record = new Forum(core, levelForumsDataTable.Rows[0]);

                        query.AddCondition("forum_order", ConditionEquality.LessThanEqual, record.Order);
                    }
                }

                db.Query(uQuery);
            }

            if (updateIds.Count > 0)
            {
                uQuery = new UpdateQuery(Item.GetTable(typeof(Forum)));
                uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Addition, differenceBelow));
                uQuery.AddCondition("forum_id", ConditionEquality.In, updateIds);

                db.Query(uQuery);
            }
        }

        public bool MoveTopics(long toForumId, params long[] topicIds)
        {
            if (!Access.Can("MOVE_TOPICS"))
            {
                return false;
            }

            Forum toForum = new Forum(core, toForumId);
            if (!toForum.Access.Can("MOVE_TOPICS"))
            {
                return false;
            }

            if (toForum.ownerKey.Id != ownerKey.Id || toForum.ownerKey.TypeId != ownerKey.TypeId)
            {
                return false;
            }

            /* Can move the topics */

            /* Validate the topics belong to the from forum */
            List<ForumTopic> topics = GetTopicsFlat(topicIds);

            if (topics.Count < topicIds.Length)
            {
                return false;
            }

            long posts = 0;
            bool newerPosts = false;
            long lastPost = toForum.LastPostId;
            long lastPostTime = toForum.lastPostTimeRaw;

            bool newestPost = false;

            for (int i = 0; i < topics.Count; i++)
            {
                posts++;
                posts += topics[i].Posts;
                if (topics[i].TimeLastPostRaw > toForum.lastPostTimeRaw)
                {
                    lastPost = topics[i].LastPostId;
                    lastPostTime = topics[i].TimeLastPostRaw;
                }
                if (topics[i].LastPostId == toForum.LastPostId)
                {
                    newestPost = true;
                }
            }

            db.BeginTransaction();

            UpdateQuery uQuery = new UpdateQuery(typeof(ForumTopic));
            uQuery.AddField("forum_id", toForumId);
            uQuery.AddCondition("topic_id", ConditionEquality.In, topicIds);

            db.Query(uQuery);

            uQuery = new UpdateQuery(typeof(Forum));
            uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Subtraction, topicIds.Length));
            uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Subtraction, posts));
            if (newestPost)
            {
            }
            uQuery.AddCondition("forum_id", forumId);

            db.Query(uQuery);

            uQuery = new UpdateQuery(typeof(Forum));
            uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Addition, topicIds.Length));
            uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, posts));
            if (newerPosts)
            {
                uQuery.AddField("forum_last_post_id", lastPost);
                uQuery.AddField("forum_last_post_time_ut", lastPostTime);
            }
            uQuery.AddCondition("forum_id", forumId);

            db.Query(uQuery);

            return true;
        }

        public override long Id
        {
            get
            {
                return forumId;
            }
        }

        public override string Uri
        {
            get
            {
                if (forumId == 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/",
                        Owner.UriStub));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/{1}",
                        Owner.UriStub, forumId));
                }
            }
        }

        public string NewTopicUri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}forum/post?f={1}&mode=post",
                    Owner.UriStub, forumId));
            }
        }

        public string MarkTopicsReadUri
        {
            get
            {
                if (forumId == 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/?mark=topics",
                        Owner.UriStub));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/{1}?mark=topics",
                        Owner.UriStub, forumId));
                }
            }
        }

        public string MarkForumsReadUri
        {
            get
            {
                if (forumId == 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/?mark=forums",
                        Owner.UriStub));
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/{1}?mark=forums",
                        Owner.UriStub, forumId));
                }
            }
        }

        public string ModeratorControlPanelUri
        {
            get
            {
                if (forumId == 0)
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/mcp",
                        Owner.UriStubAbsolute), true);
                }
                else
                {
                    return core.Uri.AppendSid(string.Format("{0}forum/mcp?f={1}",
                        Owner.UriStubAbsolute, forumId), true);
                }
            }
        }

        public void ReadAll()
        {
            ReadAll(false);
        }

        public void ReadAll(bool subForums)
        {
            if (!(IsRead) && core.LoggedInMemberId > 0)
            {
                if (readStatus != null)
                {
                    UpdateQuery uQuery2 = new UpdateQuery(ForumReadStatus.GetTable(typeof(ForumReadStatus)));
                    uQuery2.AddField("read_time_ut", UnixTime.UnixTimeStamp());
                    uQuery2.AddCondition("forum_id", forumId);
                    uQuery2.AddCondition("user_id", core.LoggedInMemberId);

                    db.Query(uQuery2);
                }
                else
                {
                    ForumReadStatus.Create(core, this);
                }
            }

            if (subForums)
            {
                List<Forum> forums = GetForums();

                foreach (Forum forum in forums)
                {
                    forum.ReadAll(true);
                }
            }
        }

        public static void Show(Core core, GPage page, long forumId)
        {
            string mark = core.Http.Query["mark"];
            ForumSettings settings;
            try
            {
                settings = new ForumSettings(core, page.Group);
            }
            catch (InvalidForumSettingsException)
            {
                ForumSettings.Create(core, page.Group);
                settings = new ForumSettings(core, page.Group);
            }
            Forum thisForum = null;
            long topicsCount = 0;

            page.template.SetTemplate("Forum", "viewforum");
            ForumSettings.ShowForumHeader(core, page);

            try
            {
                if (forumId > 0)
                {
                    thisForum = new Forum(page.Core, settings, forumId);
                }
                else
                {
                    thisForum = new Forum(page.Core, settings);
                }
            }
            catch (InvalidForumException)
            {
                return;
            }

            if (mark == "topics")
            {
                thisForum.ReadAll(false);
            }

            if (mark == "forums")
            {
                thisForum.ReadAll(true);
            }

            if (core.LoggedInMemberId > 0 && (!page.Group.IsGroupMember(core.Session.LoggedInMember.ItemKey)))
            {
                page.template.Parse("U_JOIN", page.Group.JoinUri);
            }

            topicsCount = thisForum.Topics;
			
			if (!string.IsNullOrEmpty(thisForum.Rules))
			{
                core.Display.ParseBbcode(page.template, "RULES", thisForum.Rules);
			}
			
			List<Forum> forums = GetForumLevels(core, thisForum, 2);
            List<IPermissibleItem> items = new List<IPermissibleItem>();

            //List<Forum> forums = thisForum.GetForums();
			List<Forum> accessibleForums = new List<Forum>();

            foreach (Forum forum in forums)
            {
                items.Add(forum);
            }
            items.Add(thisForum);

            core.AcessControlCache.CacheGrants(items);

			foreach (Forum forum in forums)
			{
				if (forum.Access.Can("VIEW"))
				{
					accessibleForums.Add(forum);
				}
			}
			forums = accessibleForums;

            if (!thisForum.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            page.template.Parse("FORUMS", forums.Count.ToString());

            // ForumId, TopicPost
            Dictionary<long, TopicPost> lastPosts;
            List<long> lastPostIds = new List<long>();
			
			foreach (Forum forum in forums)
            {
                lastPostIds.Add(forum.LastPostId);
            }

            lastPosts = TopicPost.GetPosts(core, lastPostIds);

            VariableCollection lastForumVariableCollection = null;
            bool lastCategory = true;
            bool first = true;
            long lastCategoryId = 0;
			Forum lastForum = null;
            foreach (Forum forum in forums)
            {
                if (lastForum != null && (!lastForum.IsCategory) && lastForum.Id == forum.parentId && lastForumVariableCollection != null)
				{
					VariableCollection subForumVariableCollection = lastForumVariableCollection.CreateChild("sub_forum_list");
					
					subForumVariableCollection.Parse("TITLE", forum.Title);
					subForumVariableCollection.Parse("URI", forum.Uri);
					
					continue;
				}
				
                if ((first && (!forum.IsCategory)) || (lastCategoryId != forum.parentId && (!forum.IsCategory)))
                {
                    VariableCollection defaultVariableCollection = page.template.CreateChild("forum_list");
                    defaultVariableCollection.Parse("TITLE", "Forum");
                    defaultVariableCollection.Parse("IS_CATEGORY", "TRUE");
                    if (lastForumVariableCollection != null)
                    {
                        lastForumVariableCollection.Parse("IS_LAST", "TRUE");
                    }
                    first = false;
                    lastCategoryId = forum.parentId;
                    lastCategory = true;
                }

                VariableCollection forumVariableCollection = page.template.CreateChild("forum_list");

                forumVariableCollection.Parse("TITLE", forum.Title);
                core.Display.ParseBbcode(forumVariableCollection, "DESCRIPTION", forum.Description);
                forumVariableCollection.Parse("URI", forum.Uri);
                forumVariableCollection.Parse("POSTS", forum.Posts.ToString());
                forumVariableCollection.Parse("TOPICS", forum.Topics.ToString());

                if (lastPosts.ContainsKey(forum.LastPostId))
                {
                    forumVariableCollection.Parse("LAST_POST_URI", lastPosts[forum.LastPostId].Uri);
                    forumVariableCollection.Parse("LAST_POST_TITLE", lastPosts[forum.LastPostId].Title);
                    core.Display.ParseBbcode(forumVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                        lastPosts[forum.LastPostId].Uri, Functions.TrimStringToWord(lastPosts[forum.LastPostId].Title, 20), core.Tz.DateTimeToString(lastPosts[forum.LastPostId].GetCreatedDate(core.Tz))));
                }
                else
                {
                    forumVariableCollection.Parse("LAST_POST", "No posts");
                }

                if (forum.IsRead)
                {
                    forumVariableCollection.Parse("IS_READ", "TRUE");
                }
                else
                {
                    forumVariableCollection.Parse("IS_READ", "FALSE");
                }

                if (forum.IsCategory)
                {
                    forumVariableCollection.Parse("IS_CATEGORY", "TRUE");
                    if (lastForumVariableCollection != null)
                    {
                        lastForumVariableCollection.Parse("IS_LAST", "TRUE");
                    }
                    lastCategoryId = forum.Id;
                    lastCategory = true;
                }
                else
                {
                    topicsCount -= forum.Topics;
                    forumVariableCollection.Parse("IS_FORUM", "TRUE");
                    if (lastCategory)
                    {
                        forumVariableCollection.Parse("IS_FIRST", "TRUE");
                    }
                    lastForumVariableCollection = forumVariableCollection;
                    lastCategory = false;
                }
                first = false;
				lastForum = forum;
            }

            if (lastForumVariableCollection != null)
            {
                lastForumVariableCollection.Parse("IS_LAST", "TRUE");
            }

            if ((settings.AllowTopicsAtRoot && forumId == 0) || forumId > 0)
            {
                List<ForumTopic> announcements = thisForum.GetAnnouncements();
                List<ForumTopic> topics = thisForum.GetTopics(page.TopLevelPageNumber, settings.TopicsPerPage);
                List<ForumTopic> allTopics = new List<ForumTopic>();
                allTopics.AddRange(announcements);
                allTopics.AddRange(topics);

                topicsCount -= announcements.Count; // aren't counted in pagination

                page.template.Parse("ANNOUNCEMENTS", announcements.Count.ToString());
                //page.template.Parse("TOPICS", topics.Count.ToString());

                // PostId, TopicPost
                Dictionary<long, TopicPost> topicLastPosts;

                topicLastPosts = TopicPost.GetTopicLastPosts(core, allTopics);

                page.template.Parse("TOPICS", allTopics.Count.ToString());
				
				foreach (ForumTopic topic in allTopics)
                {
					core.LoadUserProfile(topic.PosterId);
				}

                foreach (ForumTopic topic in allTopics)
                {
                    VariableCollection topicVariableCollection = page.template.CreateChild("topic_list");

                    int pages = (int)Math.Ceiling((topic.Posts) / (double)settings.PostsPerPage);
                    if (pages > 1)
                    {
                        core.Display.ParsePagination(topicVariableCollection, "PAGINATION", topic.Uri, 0, pages, PaginationOptions.Minimal);
                    }
                    else
                    {
                        topicVariableCollection.Parse("PAGINATION", "FALSE");
                    }

                    topicVariableCollection.Parse("TITLE", topic.Title);
                    topicVariableCollection.Parse("URI", topic.Uri);
                    topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                    topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());
					topicVariableCollection.Parse("DATE", core.Tz.DateTimeToString(topic.GetCreatedDate(core.Tz)));
					topicVariableCollection.Parse("USERNAME", core.PrimitiveCache[topic.PosterId].DisplayName);
					topicVariableCollection.Parse("U_POSTER", core.PrimitiveCache[topic.PosterId].Uri);

                    if (topicLastPosts.ContainsKey(topic.LastPostId))
                    {
                        topicVariableCollection.Parse("LAST_POST_URI", topicLastPosts[topic.LastPostId].Uri);
                        topicVariableCollection.Parse("LAST_POST_TITLE", topicLastPosts[topic.LastPostId].Title);
                        core.Display.ParseBbcode(topicVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                            topicLastPosts[topic.LastPostId].Uri, Functions.TrimStringToWord(topicLastPosts[topic.LastPostId].Title, 20), core.Tz.DateTimeToString(topicLastPosts[topic.LastPostId].GetCreatedDate(core.Tz))));
                    }
                    else
                    {
                        topicVariableCollection.Parse("LAST_POST", "No posts");
                    }
					
					switch (topic.Status)
					{
						case TopicStates.Normal:
							if (topic.IsRead)
		                    {
								if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_NORMAL_READ_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_NORMAL_READ_UNLOCKED", "TRUE");
								}
		                    }
		                    else
		                    {
		                        if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_NORMAL_UNREAD_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_NORMAL_UNREAD_UNLOCKED", "TRUE");
								}
		                    }
							break;
						case TopicStates.Sticky:
							if (topic.IsRead)
		                    {
								if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_STICKY_READ_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_STICKY_READ_UNLOCKED", "TRUE");
								}
		                    }
		                    else
		                    {
		                        if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_STICKY_UNREAD_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_STICKY_UNREAD_UNLOCKED", "TRUE");
								}
		                    }

							break;
						case TopicStates.Announcement:
						case TopicStates.Global:
							if (topic.IsRead)
		                    {
								if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_ANNOUNCEMENT_READ_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_ANNOUNCEMENT_READ_UNLOCKED", "TRUE");
								}
		                    }
		                    else
		                    {
		                        if (topic.IsLocked)
								{
									topicVariableCollection.Parse("IS_ANNOUNCEMENT_UNREAD_LOCKED", "TRUE");
								}
								else
								{
									topicVariableCollection.Parse("IS_ANNOUNCEMENT_UNREAD_UNLOCKED", "TRUE");
								}
		                    }

							break;
					}
                }

                if (!thisForum.IsCategory)
                {
                    if (thisForum.Access.Can("CREATE_TOPICS"))
                    {
                        page.template.Parse("U_NEW_TOPIC", thisForum.NewTopicUri);
                    }
                }
            }

            core.Display.ParsePagination(thisForum.Uri, page.TopLevelPageNumber, (int)Math.Ceiling((topicsCount) / (double)settings.TopicsPerPage));

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

            page.Group.ParseBreadCrumbs(breadCrumbParts);
			
            if (thisForum.Id == 0)
            {
                page.template.Parse("INDEX_STATISTICS", "TRUE");
                page.template.Parse("FORUM_POSTS", core.Functions.LargeIntegerToString(settings.Posts));
                page.template.Parse("FORUM_TOPICS", core.Functions.LargeIntegerToString(settings.Topics));
                page.template.Parse("GROUP_MEMBERS", core.Functions.LargeIntegerToString(page.Group.Members));
            }

            PermissionsList permissions = new PermissionsList(core);
            bool flagPermissionsBlock = false;

            if (thisForum.Access.Can("CREATE_TOPICS"))
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_CREATE_TOPICS"), true);
                flagPermissionsBlock = true;
            }
            else
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_CREATE_TOPICS"), false);
                flagPermissionsBlock = true;
            }
            if (thisForum.Access.Can("REPLY_TOPICS"))
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_POST_REPLIES"), true);
                flagPermissionsBlock = true;
            }
            else
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_POST_REPLIES"), false);
                flagPermissionsBlock = true;
            }
            if (thisForum.Access.Can("EDIT_OWN_POSTS"))
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_EDIT_YOUR_POSTS"), true);
                flagPermissionsBlock = true;
            }
            if (thisForum.Access.Can("DELETE_OWN_POSTS"))
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_DELETE_YOUR_POSTS"), true);
                flagPermissionsBlock = true;
            }
            if (thisForum.Access.Can("DELETE_TOPICS") || thisForum.Access.Can("LOCK_TOPICS"))
            {
                permissions.Add(core.Prose.GetString("YOU_CAN_MODERATE_FORUM"), true, thisForum.ModeratorControlPanelUri);
                flagPermissionsBlock = true;
            }

            if (flagPermissionsBlock)
            {
                permissions.Parse("PERMISSION_BLOCK");
            }
        }

        public ForumSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    try
                    {
                        settings = new ForumSettings(core, Owner);
                    }
                    catch (InvalidForumSettingsException)
                    {
                        settings = ForumSettings.Create(core, Owner);
                    }
                    return settings;
                }
                else
                {
                    return settings;
                }
            }
        }

        public Access Access
        {
            get
            {
                if (Id == 0)
                {
                    return Settings.Access;
                }
                else
                {
                    if (forumAccess == null)
                    {
                        forumAccess = new Access(core, this);
                    }
                    return forumAccess;
                }
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public Primitive Owner
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

        public int CompareTo(object obj)
        {
            if (obj.GetType() == typeof(Forum))
            {
                return Order.CompareTo(((Forum)obj).Order);
            }
            else
            {
                return -1;
            }
        }

        public void AddSequenceConditon(UpdateQuery uQuery)
        {
            uQuery.AddCondition("forum_parent_id", parentId);
            uQuery.AddCondition("forum_item_id", Owner.Id);
            uQuery.AddCondition("forum_item_type_id", Owner.TypeId);
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                if (parentId == 0)
                {
                    return Settings;
                }
                else
                {
                    return new Forum(core, ParentId);
                }
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                if (parentId == 0)
                {
                    return Settings.ItemKey;
                }
                else
                {
                    return new ItemKey(parentId, typeof(Forum));
                }
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "Forum: " + Title;
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidForumException : Exception
    {
    }
}
