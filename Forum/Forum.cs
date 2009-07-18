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
    public class Forum : NumberedItem, IPermissibleItem, IComparable, INestableItem, IOrderableItem
    {
        [DataField("forum_id", DataFieldKeys.Primary)]
        private long forumId;
        [DataField("forum_parent_id")]
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
        [DataField("forum_access")]
        private ushort permissions;
        [DataField("forum_last_post_time_ut")]
        private long lastPostTimeRaw;
        [DataField("forum_last_post_id")]
        private long lastPostId;
        /*[DataField("forum_item_id")]
        private long ownerId;
        [DataField("forum_item_type", 63)]
        private string ownerType;*/
		[DataField("forum_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("forum_order")]
        private int forumOrder;
		[DataField("forum_level")]
        private int forumLevel;
        [DataField("forum_parents", MYSQL_TEXT)]
        private string parents;

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

        public int Order
        {
            get
            {
                return forumOrder;
            }
            set
            {
                SetProperty("forumOrder", value);
            }
        }
		
		public int Level
		{
			get
			{
				return forumLevel;
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
                SetProperty("parentId", value);

                Forum parent;
                if (parentId > 0)
                {
                    if (Owner is UserGroup)
                    {
                        parent = new Forum(core, (UserGroup)Owner, parentId);
                    }
                    else
                    {
                        parent = new Forum(core, parentId);
                    }
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

                foreach (ParentTreeNode ptn in parent.Parents.Nodes)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(ptn.ParentTitle, ptn.ParentId));
                }

                if (parent.Id > 0)
                {
                    parentTree.Nodes.Add(new ParentTreeNode(parent.Title, parent.Id));
                }

                XmlSerializer xs = new XmlSerializer(typeof(ParentTreeNode));
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

        public Access ForumAccess
        {
            get
            {
                if (forumAccess == null)
                {
                    forumAccess = new Access(core, permissions, Owner);
                    forumAccess.SetSessionViewer(core.session);
                }
                return forumAccess;
            }
        }

        public Forum(Core core, UserGroup owner)
            : base(core)
        {
            this.owner = owner;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            forumId = 0;
            forumLocked = false;
			forumLevel = 0;
			permissions = 0x1111;
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
            : base(core)
        {
            this.owner = owner;

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

            DataTable forumsTable = core.db.Query(query);

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

            DataTable forumsTable = core.db.Query(query);

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

            DataTable forumsTable = core.db.Query(query);

			bool isFirst = true;
			long topLevelParent = -1;
            foreach (DataRow dr in forumsTable.Rows)
            {
				Forum forum;
                if (parent.Owner is UserGroup)
                {
                    forum = new Forum(core, (UserGroup)parent.Owner, dr);
                }
                else
                {
                    forum = new Forum(core, dr);
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
			
			DataTable forumsTable = core.db.Query(query);
			
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
					forum.Access.SetSessionViewer(core.session);
					
					if (forum.Access.CanRead)
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

            DataTable topicsTable = db.Query(query);

            foreach (DataRow dr in topicsTable.Rows)
            {
                topics.Add(new ForumTopic(core, dr));
            }

            return topics;
        }

        public static Forum Create(Core core, Forum parent, string title, string description, ushort permissions, bool isCategory)
        {
            string parents;
            int order = 0;
			int level = 0;
			
            //core.db.BeginTransaction();

            if (parent == null)
            {
                throw new InvalidForumException();
            }
			
			level = parent.Level + 1;

            if (parent.Owner is UserGroup)
            {
                if (!((UserGroup)parent.Owner).IsGroupOperator(core.session.LoggedInMember))
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
            query.AddSort(SortOrder.Descending, "forum_order");
            query.LimitCount = 1;

            DataTable orderTable = core.db.Query(query);

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

            core.db.Query(uQuery);

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
            iquery.AddField("forum_access", permissions);
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

            long forumId = core.db.Query(iquery);

            if (parent.Owner is UserGroup)
            {
                return new Forum(core, (UserGroup)parent.Owner, forumId);
            }
            else
            {
                return new Forum(core, forumId);
            }
        }

        public void MoveUp()
        {
            if (Order == 0)
            {
                return;
            }

            SelectQuery query = Forum.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_parent_id", ParentId);
            query.AddCondition("forum_order", ConditionEquality.LessThan, Order);
            query.AddSort(SortOrder.Descending, "forum_order");
            query.LimitCount = 2;

            DataTable levelForumsDataTable = db.Query(query);

            Forum record0 = null;
            Forum record1 = null;
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

                levelForumsDataTable = core.db.Query(query);

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

                    levelForumsDataTable = core.db.Query(query);

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
            int p = Functions.RequestInt("p", 1);
            string mark = HttpContext.Current.Request.QueryString["mark"];
            ForumSettings settings;
            try
            {
                settings = new ForumSettings(core, page.ThisGroup);
            }
            catch (InvalidForumSettingsException)
            {
                ForumSettings.Create(core, page.ThisGroup);
                settings = new ForumSettings(core, page.ThisGroup);
            }
            Forum thisForum = null;
            long topicsCount = 0;

            page.template.SetTemplate("Forum", "viewforum");
            ForumSettings.ShowForumHeader(core, page);

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

            if (mark == "topics")
            {
                thisForum.ReadAll(false);
            }

            if (mark == "forums")
            {
                thisForum.ReadAll(true);
            }
			
			thisForum.Access.SetSessionViewer(core.session);
			if (!thisForum.Access.CanRead)
			{
                core.Functions.Generate403();
			}

            if (core.LoggedInMemberId > 0 && (!page.ThisGroup.IsGroupMember(core.session.LoggedInMember)))
            {
                page.template.Parse("U_JOIN", page.ThisGroup.JoinUri);
            }

            topicsCount = thisForum.Topics;

            thisForum.ForumAccess.SetSessionViewer(core.session);

            if (!thisForum.ForumAccess.CanRead)
            {
                core.Functions.Generate403();
                return;
            }
			
			if (!string.IsNullOrEmpty(thisForum.Rules))
			{
                core.Display.ParseBbcode(page.template, "RULES", thisForum.Rules);
			}
			
			List<Forum> forums = GetForumLevels(core, thisForum, 2);

            //List<Forum> forums = thisForum.GetForums();
			List<Forum> accessibleForums = new List<Forum>();
			
			foreach (Forum forum in forums)
			{
				forum.Access.SetSessionViewer(core.session);
				if (forum.Access.CanRead)
				{
					accessibleForums.Add(forum);
				}
			}
			forums = accessibleForums;

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
                    core.Display.ParseBbcode(forumVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                        lastPosts[forum.LastPostId].Uri, Functions.TrimStringToWord(lastPosts[forum.LastPostId].Title, 20), core.tz.DateTimeToString(lastPosts[forum.LastPostId].GetCreatedDate(core.tz))));
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
                List<ForumTopic> topics = thisForum.GetTopics(p, settings.TopicsPerPage);
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

                    topicVariableCollection.Parse("TITLE", topic.Title);
                    topicVariableCollection.Parse("URI", topic.Uri);
                    topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                    topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());
					topicVariableCollection.Parse("DATE", core.tz.DateTimeToString(topic.GetCreatedDate(core.tz)));
					topicVariableCollection.Parse("USERNAME", core.UserProfiles[topic.PosterId].DisplayName);
					topicVariableCollection.Parse("U_POSTER", core.UserProfiles[topic.PosterId].Uri);

                    if (topicLastPosts.ContainsKey(topic.LastPostId))
                    {
                        core.Display.ParseBbcode(topicVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                            topicLastPosts[topic.LastPostId].Uri, Functions.TrimStringToWord(topicLastPosts[topic.LastPostId].Title, 20), core.tz.DateTimeToString(topicLastPosts[topic.LastPostId].GetCreatedDate(core.tz))));
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

                if (thisForum.ForumAccess.CanCreate)
                {
                    page.template.Parse("U_NEW_TOPIC", thisForum.NewTopicUri);
                }
            }

            core.Display.ParsePagination(thisForum.Uri, p, (int)Math.Ceiling((topicsCount) / (double)settings.TopicsPerPage));

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

            page.ThisGroup.ParseBreadCrumbs(breadCrumbParts);
			
            if (thisForum.Id == 0)
            {
                page.template.Parse("INDEX_STATISTICS", "TRUE");
                page.template.Parse("FORUM_POSTS", core.Functions.LargeIntegerToString(settings.Posts));
                page.template.Parse("FORUM_TOPICS", core.Functions.LargeIntegerToString(settings.Topics));
                page.template.Parse("GROUP_MEMBERS", core.Functions.LargeIntegerToString(page.ThisGroup.Members));
            }
        }

        public Access Access
        {
            get
            {
                return ForumAccess;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || (ownerKey.Id != owner.Id && ownerKey.TypeId != owner.TypeId))
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerKey);
                    owner = core.UserProfiles[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public List<string> PermissibleActions
        {
            get
            {
                List<string> permissions = new List<string>();
                permissions.Add("Can Read");
                permissions.Add("Can Post");
                return permissions;
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
    }

    public class InvalidForumException : Exception
    {
    }
}
