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
    [DataTable("forum_settings")]
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
    public class ForumSettings : NumberedItem, IPermissibleItem
    {
        [DataField("forum_settings_id", DataFieldKeys.Primary)]
        private long settingsId;
        [DataField("forum_item", DataFieldKeys.Unique)]
        private ItemKey ownerKey;
        [DataField("forum_topics")]
        private long topics;
        [DataField("forum_posts")]
        private long posts;
        [DataField("forum_topics_per_page")]
        private int topicsPerPage;
        [DataField("forum_posts_per_page")]
        private int postsPerPage;
        [DataField("forum_allow_topics_root")]
        private bool allowTopicsAtRoot;
        [DataField("forum_simple_permissions")]
        private bool simplePermissions;

        private Primitive owner;
        private Access access;

        public long Posts
        {
            get
            {
                return posts;
            }
        }

        public long Topics
        {
            get
            {
                return topics;
            }
        }

        public int TopicsPerPage
        {
            get
            {
                return topicsPerPage;
            }
            set
            {
                SetProperty("topicsPerPage", value);
            }
        }

        public int PostsPerPage
        {
            get
            {
                return postsPerPage;
            }
            set
            {
                SetProperty("postsPerPage", value);
            }
        }

        public bool AllowTopicsAtRoot
        {
            get
            {
                return allowTopicsAtRoot;
            }
            set
            {
                SetProperty("allowTopicsAtRoot", value);
            }
        }

        public List<ForumMemberRank> GetRanks()
        {
            List<ForumMemberRank> ranks = new List<ForumMemberRank>();

            SelectQuery query = ForumMemberRank.GetSelectQueryStub(typeof(ForumMemberRank));
            query.AddCondition("rank_owner_id", ownerKey.Id);
            query.AddCondition("rank_owner_type_id", ownerKey.TypeId);

            DataTable ranksDataTable = core.Db.Query(query);

            foreach (DataRow dr in ranksDataTable.Rows)
            {
                ranks.Add(new ForumMemberRank(core, dr));
            }

            return ranks;
        }

        public ForumSettings(Core core, long settingsId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumSettings_ItemLoad);

            try
            {
                LoadItem(settingsId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumSettingsException();
            }
        }

        public ForumSettings(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(ForumSettings_ItemLoad);

            try
            {
                LoadItem("forum_item_id", "forum_item_type_id", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumSettingsException();
            }
        }

        void ForumSettings_ItemLoad()
        {
        }

        public static ForumSettings Create(Core core, Primitive owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            //if (!thisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                // todo: throw new exception
                // commented out due to errors on live site
                //throw new UnauthorisedToCreateItemException();
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ForumSettings)));
            iQuery.AddField("forum_item_id", owner.Id);
            iQuery.AddField("forum_item_type_id", owner.TypeId);
            iQuery.AddField("forum_topics", 0);
            iQuery.AddField("forum_posts", 0);
            iQuery.AddField("forum_topics_per_page", 10);
            iQuery.AddField("forum_posts_per_page", 10);
            iQuery.AddField("forum_allow_topics_root", true);

            long settingsId = core.Db.Query(iQuery);

            ForumSettings settings = new ForumSettings(core, settingsId);

            if (owner is UserGroup)
            {
                settings.Access.CreateAllGrantsForPrimitive(UserGroup.GroupOperatorsGroupKey);
                settings.Access.CreateGrantForPrimitive(UserGroup.GroupMembersGroupKey, "VIEW", "VIEW_TOPICS", "LIST_TOPICS", "REPLY_TOPICS", "CREATE_TOPICS");
                settings.Access.CreateGrantForPrimitive(User.EveryoneGroupKey, "VIEW", "VIEW_TOPICS", "LIST_TOPICS");
            }
            if (owner is ApplicationEntry)
            {
                settings.Access.CreateGrantForPrimitive(User.RegisteredUsersGroupKey, "VIEW", "VIEW_TOPICS", "LIST_TOPICS", "REPLY_TOPICS", "CREATE_TOPICS");
                settings.Access.CreateGrantForPrimitive(User.EveryoneGroupKey, "VIEW", "VIEW_TOPICS", "LIST_TOPICS");
            }

            return settings;
        }

        public new long Update()
        {
            if (owner is UserGroup)
            {
                if (!((UserGroup)owner).IsGroupOperator(core.Session.LoggedInMember))
                {
                    // todo: throw new exception
                    throw new UnauthorisedToCreateItemException();
                }
            }

            UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(ForumSettings)));
            uQuery.AddField("forum_topics_per_page", topicsPerPage);
            uQuery.AddField("forum_posts_per_page", postsPerPage);
            uQuery.AddField("forum_allow_topics_root", allowTopicsAtRoot);

            uQuery.AddCondition("forum_item_id", ownerKey.Id);
            uQuery.AddCondition("forum_item_type_id", ownerKey.TypeId);

            return core.Db.Query(uQuery);
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }

        public static void ShowForumHeader(Core core, PPage page)
        {
            page.template.Parse("U_FORUM_INDEX", core.Uri.AppendSid(string.Format("{0}forum",
                page.Owner.UriStub)));
            page.template.Parse("U_UCP", core.Uri.AppendSid(string.Format("{0}forum/ucp",
                page.Owner.UriStub)));
            page.template.Parse("U_MEMBERS", core.Uri.AppendSid(string.Format("{0}forum/memberlist",
                page.Owner.UriStub)));

            if (page is GPage)
            {
                if (core.Session.IsLoggedIn && ((GPage)page).Group.IsGroupMember(core.Session.LoggedInMember))
                {
                    page.template.Parse("IS_FORUM_MEMBER", "TRUE");
                }
                else
                {
                    page.template.Parse("IS_FORUM_MEMBER", "FALSE");
                }
            }
            page.template.Parse("U_FAQ", core.Uri.AppendSid(string.Format("{0}forum/help",
                page.Owner.UriStub)));
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
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
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public bool GetDefaultCan(string permission)
        {
            return false;
        }

        public override long Id
        {
            get
            {
                return settingsId;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "Forum Settings: " + Owner.DisplayName + " (" + Owner.Key + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public List<Forum> GetForums()
        {
            List<Forum> forums = new List<Forum>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(Forum));
            query.AddCondition("forum_item_id", ownerKey.Id);
            query.AddCondition("forum_item_type_id", ownerKey.TypeId);
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
    }

    public class InvalidForumSettingsException : Exception
    {
    }
}
