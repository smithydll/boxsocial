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
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum")]
    public class Forum : NumberedItem, IPermissibleItem, IComparable
    {
        [DataField("forum_id", DataFieldKeys.Primary)]
        private long forumId;
        [DataField("forum_parent_id", typeof(Forum))]
        private long parentId;
        [DataField("forum_title", 127)]
        private string forumTitle;
        [DataField("forum_description", 1023)]
        private string forumDescription;
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
        [DataField("forum_item_id")]
        private long ownerId;
        [DataField("forum_item_type", 63)]
        private string ownerType;
        [DataField("forum_order")]
        private int forumOrder;
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

                parents = sb.ToString();
            }
        }

        public ParentTree Parents
        {
            get
            {
                if (parentTree == null)
                {
                    XmlSerializer xs = new XmlSerializer(typeof(ParentTree));;
                    StringReader sr = new StringReader(parents);

                    parentTree = (ParentTree)xs.Deserialize(sr);
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
            this.ownerId = owner.Id;
            this.ownerType = owner.Type;
            forumId = 0;
            forumLocked = false;
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

        void Forum_ItemLoad()
        {
        }

        public static SelectQuery Forum_GetSelectQueryStub(Core core)
        {
            SelectQuery query = Forum.GetSelectQueryStub(typeof(Forum));

            if (core.LoggedInMemberId > 0)
            {
                query.AddFields(TopicPost.GetFieldsPrefixed(typeof(ForumReadStatus)));
                TableJoin tj1 = query.AddJoin(JoinTypes.Left, ForumReadStatus.GetTable(typeof(ForumReadStatus)), "forum_id", "forum_id");
                tj1.AddCondition("`forum_read_status`.`user_id`", core.LoggedInMemberId);
            }

            // TODO: forum sort order
            //query.AddSort(SortOrder.Descending, "forum_order");

            return query;
        }

        public List<Forum> GetForums()
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = new SelectQuery("forum");
            query.AddCondition("forum_item_id", ownerId);
            query.AddCondition("forum_item_type", ownerType);
            query.AddCondition("forum_parent_id", forumId);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                forums.Add(new Forum(core, dr));
            }

            return forums;
        }

        public static List<Forum> GetForums(Core core, List<long> forumIds)
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = new SelectQuery("forum");
            query.AddCondition("forum_id", ConditionEquality.In, forumIds);
            query.AddSort(SortOrder.Ascending, "forum_order");

            DataTable forumsTable = core.db.Query(query);

            foreach (DataRow dr in forumsTable.Rows)
            {
                forums.Add(new Forum(core, dr));
            }

            return forums;
        }

        public List<ForumTopic> GetTopics(int currentPage, int perPage)
        {
            return getSubItems(typeof(ForumTopic), currentPage, perPage, true).ConvertAll<ForumTopic>(new Converter<Item, ForumTopic>(convertToForumTopic));
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

        public static Forum Create(Core core, Forum parent, string title, string description, ushort permissions, bool isCategory)
        {
            string parents;
            int order = 0;
            core.db.BeginTransaction();

            if (parent == null)
            {
                throw new InvalidForumException();
            }

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
            query.AddCondition("forum_parent_id", parent.Id);
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

                UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(Forum)));
                uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Addition, 1));
                uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, order);
                uQuery.AddCondition("forum_item_id", parent.Owner.Id);
                uQuery.AddCondition("forum_item_type", parent.Owner.Type);

                core.db.Query(uQuery);
            }

            ParentTree parentTree = new ParentTree();

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

            parents = sb.ToString();

            InsertQuery iquery = new InsertQuery(GetTable(typeof(Forum)));
            iquery.AddField("forum_parent_id", parent.Id);
            iquery.AddField("forum_title", title);
            iquery.AddField("forum_description", description);
            iquery.AddField("forum_access", permissions);
            iquery.AddField("forum_order", order);
            iquery.AddField("forum_category", isCategory);
            iquery.AddField("forum_locked", false);
            iquery.AddField("forum_topics", 0);
            iquery.AddField("forum_posts", 0);
            iquery.AddField("forum_item_id", parent.Owner.Id);
            iquery.AddField("forum_item_type", parent.Owner.Type);
            iquery.AddField("forum_parents", parents);

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

        public string NewTopicUri
        {
            get
            {
                if (Owner.GetType() == typeof(UserGroup))
                {
                    return Linker.AppendSid(string.Format("/group/{0}/forum/post?f={1}&mode=post",
                        Owner.Key, forumId));
                }
                else if (Owner.GetType() == typeof(Network))
                {
                    return Linker.AppendSid(string.Format("/network/{0}/forum/post?f={1}&mode=post",
                        Owner.Key, forumId));
                }
                else
                {
                    return "/";
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

            page.template.Parse("FORUMS", forums.Count.ToString());

            // ForumId, TopicPost
            Dictionary<long, TopicPost> lastPosts;
            List<long> lastPostIds = new List<long>();
            List<long> subForumIds = new List<long>();

            foreach (Forum forum in forums)
            {
                lastPostIds.Add(forum.LastPostId);

                if (forum.IsCategory)
                {
                    subForumIds.Add(forum.Id);
                }
            }

            List<Forum> subForums = Forum.GetForums(core, subForumIds);

            foreach (Forum forum in subForums)
            {
                forums.Add(forum);
            }

            forums.Sort();

            lastPosts = TopicPost.GetPosts(core, lastPostIds);

            VariableCollection lastForumVariableCollection = null;
            bool lastCategory = true;
            foreach (Forum forum in forums)
            {
                VariableCollection forumVariableCollection = page.template.CreateChild("forum_list");

                forumVariableCollection.Parse("TITLE", forum.Title);
                forumVariableCollection.Parse("URI", forum.Uri);
                forumVariableCollection.Parse("POSTS", forum.Posts.ToString());
                forumVariableCollection.Parse("TOPICS", forum.Topics.ToString());

                if (lastPosts.ContainsKey(forum.Id))
                {
                    forumVariableCollection.Parse("LAST_POST", lastPosts[forum.Id].Title);
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
                    lastCategory = true;
                }
                else
                {
                    if (lastCategory)
                    {
                        forumVariableCollection.Parse("IS_FIRST", "TRUE");
                    }
                    lastForumVariableCollection = forumVariableCollection;
                }
            }

            if (lastForumVariableCollection != null)
            {
                lastForumVariableCollection.Parse("IS_LAST", "TRUE");
            }

            List<ForumTopic> topics = thisForum.GetTopics(p, 10);

            page.template.Parse("TOPICS", topics.Count.ToString());

            // PostId, TopicPost
            Dictionary<long, TopicPost> topicLastPosts;

            topicLastPosts = TopicPost.GetTopicLastPosts(core, topics);

            page.template.Parse("TOPICS", topics.Count.ToString());

            foreach (ForumTopic topic in topics)
            {
                VariableCollection topicVariableCollection = page.template.CreateChild("topic_list");

                topicVariableCollection.Parse("TITLE", topic.Title);
                topicVariableCollection.Parse("URI", topic.Uri);
                topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());

                if (topicLastPosts.ContainsKey(topic.LastPostId))
                {
                    Display.ParseBbcode(topicVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                        topicLastPosts[topic.LastPostId].Uri, topicLastPosts[topic.LastPostId].Title, core.tz.DateTimeToString(topicLastPosts[topic.LastPostId].GetCreatedDate(core.tz))));
                }
                else
                {
                    topicVariableCollection.Parse("LAST_POST", "No posts");
                }

                if (topic.IsRead)
                {
                    topicVariableCollection.Parse("IS_READ", "TRUE");
                }
                else
                {
                    topicVariableCollection.Parse("IS_READ", "FALSE");
                }
            }

            if (thisForum.ForumAccess.CanCreate)
            {
                page.template.Parse("U_NEW_TOPIC", thisForum.NewTopicUri);
            }

            Display.ParsePagination(thisForum.Uri, p, (int)Math.Ceiling((thisForum.Topics + 1) / 10.0));

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", "Forum" });

            if (thisForum.Id > 0)
            {
                breadCrumbParts.Add(new string[] { thisForum.Id.ToString(), thisForum.Title });
            }

            page.ThisGroup.ParseBreadCrumbs(breadCrumbParts);
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
                if (owner == null || (ownerId != owner.Id && ownerType != owner.Type))
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerType, ownerId);
                    owner = core.UserProfiles[ownerType, ownerId];
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
            if (obj is Forum)
            {
                return Order.CompareTo(((Forum)obj).Order);
            }
            else
            {
                return -1;
            }
        }
    }

    public class InvalidForumException : Exception
    {
    }
}