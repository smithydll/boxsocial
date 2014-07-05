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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public enum UserGroupLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Icon = Key | 0x08,
        Common = Key | Info,
        All = Key | Info | Icon,
    }

    [DataTable("group_keys", "GROUP")]
    [Primitive("GROUP", UserGroupLoadOptions.All, "group_id", "group_name")]
    [Permission("VIEW", "Can view the group", PermissionTypes.View)]
    [Permission("COMMENT", "Can write on the guest book", PermissionTypes.Interact)]
    [Permission("VIEW_MEMBERS", "Can view the group members", PermissionTypes.View)]
    [Permission("DELETE_COMMENTS", "Can delete comments from the guest book", PermissionTypes.Delete)]
    public class UserGroup : Primitive, ICommentableItem, IPermissibleItem, INotifiableItem
    {
        public static int GROUPS_PER_PAGE = 10;

        [DataField("group_id", DataFieldKeys.Primary)]
        private long groupId;
        [DataField("group_name", DataFieldKeys.Unique, 64)]
        private string slug;
        [DataField("group_domain", DataFieldKeys.Index, 63)]
        private string domain;
        [DataField("group_simple_permissions")]
        private bool simplePermissions;

        private string groupIconUri;
        private string groupCoverPhotoUri;

        private UserGroupInfo groupInfo;
        private Access access;

        private Dictionary<ItemKey, bool> groupMemberCache = new Dictionary<ItemKey, bool>();
        private Dictionary<ItemKey, bool> groupMemberPendingCache = new Dictionary<ItemKey, bool>();
        private Dictionary<ItemKey, bool> groupMemberBannedCache = new Dictionary<ItemKey, bool>();
        private Dictionary<ItemKey, bool> groupMemberAbsoluteCache = new Dictionary<ItemKey, bool>();
        private Dictionary<ItemKey, bool> groupOperatorCache = new Dictionary<ItemKey, bool>();

        public event CommentHandler OnCommentPosted;

        public long GroupId
        {
            get
            {
                return groupId;
            }
        }

        public string Domain
        {
            get
            {
                return domain;
            }
        }

        public UserGroupInfo GroupInfo
        {
            get
            {
                if (groupInfo == null)
                {
                    groupInfo = new UserGroupInfo(core, groupId);
                }
                return groupInfo;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override long Id
        {
            get
            {
                return GroupId;
            }
        }

        public override string Type
        {
            get
            {
                return "GROUP";
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Group;
            }
        }

        public string Slug
        {
            get
            {
                return slug;
            }
        }

        public override string Key
        {
            get
            {
                return slug;
            }
        }

        public override string DisplayName
        {
            get
            {
                return groupInfo.DisplayName;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                return groupInfo.DisplayNameOwnership;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the group " + DisplayNameOwnership;
            }
        }

        public override string TitleName
        {
            get
            {
                return "the group " + DisplayName;
            }
        }

        public string GroupType
        {
            get
            {
                return groupInfo.GroupType;
            }
        }

        public string Description
        {
            get
            {
                return groupInfo.Description;
            }
        }

        public long Members
        {
            get
            {
                return groupInfo.Members;
            }
        }

        public long Officers
        {
            get
            {
                return groupInfo.Officers;
            }
        }

        public long Operators
        {
            get
            {
                return groupInfo.Operators;
            }
        }

        public string Category
        {
            get
            {
                return groupInfo.Category;
            }
        }

        public short RawCategory
        {
            get
            {
                return groupInfo.RawCategory;
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public long GalleryItems
        {
            get
            {
                return groupInfo.GalleryItems;
            }
        }

        public DateTime DateCreated(UnixTime tz)
        {
            return groupInfo.DateCreated(tz);
        }

        public UserGroup(Core core, long groupId)
            : this(core, groupId, UserGroupLoadOptions.Info | UserGroupLoadOptions.Icon)
        {
        }

        public UserGroup(Core core, long groupId, UserGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroup_ItemLoad);

            bool containsInfoData = false;
            bool containsIconData = false;

            if (loadOptions == UserGroupLoadOptions.Key)
            {
                try
                {
                    LoadItem(groupId);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidGroupException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(UserGroup.GetTable(typeof(UserGroup)));
                query.AddFields(UserGroup.GetFieldsPrefixed(typeof(UserGroup)));
                query.AddCondition("`group_keys`.`group_id`", groupId);

                if ((loadOptions & UserGroupLoadOptions.Info) == UserGroupLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserGroupInfo.GetTable(typeof(UserGroupInfo)), "group_id", "group_id");
                    query.AddFields(UserGroupInfo.GetFieldsPrefixed(typeof(UserGroupInfo)));

                    if ((loadOptions & UserGroupLoadOptions.Icon) == UserGroupLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("group_info", "group_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }
                }

                DataTable groupTable = db.Query(query);

                if (groupTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(UserGroup), groupTable.Rows[0]);

                    if (containsInfoData)
                    {
                        groupInfo = new UserGroupInfo(core, groupTable.Rows[0]);
                    }

                    if (containsIconData)
                    {
                        loadUserGroupIcon(groupTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidGroupException();
                }
            }
        }

        public UserGroup(Core core, string groupName)
            : this(core, groupName, UserGroupLoadOptions.Info | UserGroupLoadOptions.Icon)
        {
        }

        public UserGroup(Core core, string groupName, UserGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroup_ItemLoad);

            bool containsInfoData = false;
            bool containsIconData = false;

            if (loadOptions == UserGroupLoadOptions.Key)
            {
                try
                {
                    LoadItem("group_name", groupName);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidGroupException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(UserGroup.GetTable(typeof(UserGroup)));
                query.AddFields(UserGroup.GetFieldsPrefixed(typeof(UserGroup)));
                query.AddCondition("`group_keys`.`group_name`", groupName);

                if ((loadOptions & UserGroupLoadOptions.Info) == UserGroupLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserGroupInfo.GetTable(typeof(UserGroupInfo)), "group_id", "group_id");
                    query.AddFields(UserGroupInfo.GetFieldsPrefixed(typeof(UserGroupInfo)));

                    if ((loadOptions & UserGroupLoadOptions.Icon) == UserGroupLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("group_info", "group_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }
                }

                DataTable groupTable = db.Query(query);

                if (groupTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(UserGroup), groupTable.Rows[0]);

                    if (containsInfoData)
                    {
                        groupInfo = new UserGroupInfo(core, groupTable.Rows[0]);
                    }

                    if (containsIconData)
                    {
                        loadUserGroupIcon(groupTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidGroupException();
                }
            }
        }

        public UserGroup(Core core, DataRow groupRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroup_ItemLoad);

            if (groupRow != null)
            {
                loadItemInfo(typeof(UserGroup), groupRow);

                groupInfo = new UserGroupInfo(core, groupRow);

                loadUserGroupIcon(groupRow);
            }
            else
            {
                throw new InvalidGroupException();
            }
        }

        public UserGroup(Core core, DataRow groupRow, UserGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroup_ItemLoad);

            if (groupRow != null)
            {
                loadItemInfo(typeof(UserGroup), groupRow);

                if ((loadOptions & UserGroupLoadOptions.Info) == UserGroupLoadOptions.Info)
                {
                    groupInfo = new UserGroupInfo(core, groupRow);
                }

                if ((loadOptions & UserGroupLoadOptions.Icon) == UserGroupLoadOptions.Icon)
                {
                    loadUserGroupIcon(groupRow);
                }
            }
            else
            {
                throw new InvalidGroupException();
            }
        }

        void UserGroup_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(UserGroup_CommentPosted);
        }

        bool UserGroup_CommentPosted(CommentPostedEventArgs e)
        {
            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        private void loadUserGroupIcon(DataRow groupRow)
        {
        }

        public List<SubUserGroup> GetSubGroups()
        {
            return GetSubGroups(0, 0, null);
        }

        public List<SubUserGroup> GetSubGroups(int page, int perPage)
        {
            return GetSubGroups(page, perPage, null);
        }

        public List<SubUserGroup> GetSubGroups(int page, int perPage, string filter)
        {
            List<SubUserGroup> subGroups = new List<SubUserGroup>();

            SelectQuery query = SubUserGroup.GetSelectQueryStub(typeof(SubUserGroup));
            query.AddCondition("sub_group_parent_id", Id);
            query.AddSort(SortOrder.Ascending, "sub_group_reg_date_ut");
            if (!string.IsNullOrEmpty(filter))
            {
                query.AddCondition("sub_group_name_first", filter);
            }
            if (page > 0 && perPage > 0)
            {
                query.LimitStart = (page - 1) * perPage;
                query.LimitCount = perPage;
            }

            DataTable subGroupsDataTable = Query(query);

            foreach (DataRow dr in subGroupsDataTable.Rows)
            {
                subGroups.Add(new SubUserGroup(core, dr, UserSubGroupLoadOptions.Common));
            }

            return subGroups;
        }

        public List<GroupMember> GetMembers(int page, int perPage)
        {
            return GetMembers(page, perPage, null);
        }

        public List<GroupMember> GetMembers(int page, int perPage, string filter)
        {
            List<GroupMember> members = new List<GroupMember>();

            SelectQuery query = new SelectQuery("group_members");
            query.AddJoin(JoinTypes.Inner, "user_keys", "user_id", "user_id");
            query.AddFields(GroupMember.GetFieldsPrefixed(typeof(GroupMember)));
            query.AddField(new DataField("group_operators", "user_id", "user_id_go"));
            TableJoin tj = query.AddJoin(JoinTypes.Left, new DataField("group_members", "user_id"), new DataField("group_operators", "user_id"));
            tj.AddCondition(new DataField("group_members", "group_id"), new DataField("group_operators", "group_id"));
            query.AddCondition("`group_members`.`group_id`", groupId);
            query.AddCondition("group_member_approved", true);
            if (!string.IsNullOrEmpty(filter))
            {
                query.AddCondition(new DataField("user_keys", "user_name_first"), filter[0]);
            }
            query.AddSort(SortOrder.Ascending, "group_member_date_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new GroupMember(core, dr));
            }

            return members;
        }

        public List<GroupMember> GetOperators()
        {
            return GetOperators(1, 255);
        }

        public List<GroupMember> GetOperators(int page, int perPage)
        {
            List<GroupMember> operators = new List<GroupMember>();

            SelectQuery query = new SelectQuery("group_operators");
            query.AddField(new DataField("group_operators", "user_id"));
            query.AddCondition("group_id", groupId);

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                operators.Add(new GroupMember(core, dr));
            }

            return operators;
        }

        public List<GroupOfficer> GetOfficers()
        {
            return GetOfficers(1, 255);
        }

        public List<GroupOfficer> GetOfficers(int page, int perPage)
        {
            List<GroupOfficer> officers = new List<GroupOfficer>();

            SelectQuery query = new SelectQuery("group_officers");
            query.AddField(new DataField("group_officers", "user_id"));
            query.AddField(new DataField("group_officers", "officer_title"));
            query.AddField(new DataField("group_officers", "group_id"));
            query.AddCondition("group_id", groupId);

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                officers.Add(new GroupOfficer(core, dr));
            }

            return officers;
        }

        public bool IsGroupInvitee(User member)
        {
            DataTable inviteTable = db.Query(string.Format("SELECT user_id FROM group_invites WHERE group_id = {0} AND user_id = {1}",
                groupId, member.UserId));

            if (inviteTable.Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void preLoadMemberCache(ItemKey itemKey)
        {
            SelectQuery query = new SelectQuery("group_members");
            query.AddFields("user_id", "group_member_approved");
            query.AddCondition("group_id", groupId);
            query.AddCondition("user_id", itemKey.Id);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count > 0)
            {
                switch ((GroupMemberApproval)(byte)memberTable.Rows[0]["group_member_approved"])
                {
                    case GroupMemberApproval.Pending:
                        groupMemberCache.Add(itemKey, false);
                        groupMemberPendingCache.Add(itemKey, true);
                        groupMemberBannedCache.Add(itemKey, false);
                        groupMemberAbsoluteCache.Add(itemKey, true);
                        break;
                    case GroupMemberApproval.Member:
                        groupMemberCache.Add(itemKey, true);
                        groupMemberPendingCache.Add(itemKey, false);
                        groupMemberBannedCache.Add(itemKey, false);
                        groupMemberAbsoluteCache.Add(itemKey, true);
                        break;
                    case GroupMemberApproval.Banned:
                        groupMemberCache.Add(itemKey, false);
                        groupMemberPendingCache.Add(itemKey, false);
                        groupMemberBannedCache.Add(itemKey, true);
                        groupMemberAbsoluteCache.Add(itemKey, false);
                        break;
                }
            }
            else
            {
                groupMemberCache.Add(itemKey, false);
                groupMemberPendingCache.Add(itemKey, false);
                groupMemberBannedCache.Add(itemKey, false);
                groupMemberAbsoluteCache.Add(itemKey, false);
            }
        }

        public bool IsGroupMemberAbsolute(ItemKey member)
        {
            if (member != null)
            {
                if (groupMemberAbsoluteCache.ContainsKey(member))
                {
                    return groupMemberAbsoluteCache[member];
                }
                else
                {
                    preLoadMemberCache(member);
                    return groupMemberAbsoluteCache[member];
                }
            }
            return false;
        }

        public bool IsGroupMemberPending(ItemKey member)
        {
            if (member != null)
            {
                if (groupMemberPendingCache.ContainsKey(member))
                {
                    return groupMemberPendingCache[member];
                }
                else
                {
                    preLoadMemberCache(member);
                    return groupMemberPendingCache[member];
                }
            }
            return false;
        }

        public bool IsGroupMember(ItemKey member)
        {
            if (member != null)
            {
                if (groupMemberCache.ContainsKey(member))
                {
                    return groupMemberCache[member];
                }
                else
                {
                    preLoadMemberCache(member);
                    return groupMemberCache[member];
                }
            }
            return false;
        }

        public bool IsGroupMemberBanned(ItemKey member)
        {
            if (member != null)
            {
                if (groupMemberCache.ContainsKey(member))
                {
                    return groupMemberBannedCache[member];
                }
                else
                {
                    preLoadMemberCache(member);
                    return groupMemberBannedCache[member];
                }
            }
            return false;
        }

        public bool IsGroupOperator(ItemKey member)
        {
            if (member != null)
            {
                if (groupOperatorCache.ContainsKey(member))
                {
                    return groupOperatorCache[member];
                }
                else
                {
                    DataTable operatorTable = db.Query(string.Format("SELECT user_id FROM group_operators WHERE group_id = {0} AND user_id = {1}",
                        groupId, member.Id));

                    if (operatorTable.Rows.Count > 0)
                    {
                        groupOperatorCache.Add(member, true);
                        return true;
                    }
                    else
                    {
                        groupOperatorCache.Add(member, false);
                        return false;
                    }
                }
            }
            return false;
        }

        public static SelectQuery GetSelectQueryStub(UserGroupLoadOptions loadOptions)
        {
            SelectQuery query = new SelectQuery(UserGroup.GetTable(typeof(UserGroup)));
            query.AddFields(UserGroup.GetFieldsPrefixed(typeof(UserGroup)));

            if ((loadOptions & UserGroupLoadOptions.Info) == UserGroupLoadOptions.Info)
            {
                query.AddJoin(JoinTypes.Inner, UserGroupInfo.GetTable(typeof(UserGroupInfo)), "group_id", "group_id");
                query.AddFields(UserGroupInfo.GetFieldsPrefixed(typeof(UserGroupInfo)));

                if ((loadOptions & UserGroupLoadOptions.Icon) == UserGroupLoadOptions.Icon)
                {
                    query.AddJoin(JoinTypes.Left, new DataField("group_info", "group_icon"), new DataField("gallery_items", "gallery_item_id"));
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                }
            }

            return query;
        }

        public static SelectQuery UserGroup_GetSelectQueryStub()
        {
            return GetSelectQueryStub(UserGroupLoadOptions.All);
        }

        public static UserGroup Create(Core core, string groupTitle, string groupSlug, string groupDescription, long groupCategory, string groupType)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Mysql db = core.Db;
            SessionState session = core.Session;

            if (core.Session.LoggedInMember == null)
            {
                return null;
            }

            if (!CheckGroupNameUnique(core, groupSlug))
            {
                return null;
            }

            switch (groupType)
            {
                case "open":
                    groupType = "OPEN";
                    break;
                case "request":
                    groupType = "REQUEST";
                    break;
                case "closed":
                    groupType = "CLOSED";
                    break;
                case "private":
                    groupType = "PRIVATE";
                    break;
                default:
                    return null;
            }

            db.BeginTransaction();

            InsertQuery iQuery = new InsertQuery(UserGroup.GetTable(typeof(UserGroup)));
            iQuery.AddField("group_name", groupSlug);
            iQuery.AddField("group_domain", string.Empty);

            long groupId = db.Query(iQuery);

            iQuery = new InsertQuery(UserGroupInfo.GetTable(typeof(UserGroupInfo)));
            iQuery.AddField("group_id", groupId);
            iQuery.AddField("group_name", groupSlug);
            iQuery.AddField("group_name_display", groupTitle);
            iQuery.AddField("group_type", groupType);
            iQuery.AddField("group_abstract", groupDescription);
            iQuery.AddField("group_reg_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("group_operators", 1);
            iQuery.AddField("group_officers", 0);
            iQuery.AddField("group_members", 1);
            iQuery.AddField("group_category", groupCategory);
            iQuery.AddField("group_gallery_items", 0);
            iQuery.AddField("group_home_page", "/profile");
            iQuery.AddField("group_style", string.Empty);

            iQuery.AddField("group_reg_ip", session.IPAddress.ToString());
            iQuery.AddField("group_icon", 0);
            iQuery.AddField("group_bytes", 0);
            iQuery.AddField("group_views", 0);

            db.Query(iQuery);

            if (groupType != "PRIVATE")
            {
                db.UpdateQuery(string.Format("UPDATE global_categories SET category_groups = category_groups + 1 WHERE category_id = {0}",
                    groupCategory));
            }

            db.UpdateQuery(string.Format("INSERT INTO group_members (user_id, group_id, group_member_approved, group_member_ip, group_member_date_ut) VALUES ({0}, {1}, 1, '{2}', UNIX_TIMESTAMP())",
                session.LoggedInMember.UserId, groupId, Mysql.Escape(session.IPAddress.ToString())));

            db.UpdateQuery(string.Format("INSERT INTO group_operators (user_id, group_id) VALUES ({0}, {1})",
                session.LoggedInMember.UserId, groupId));

            UserGroup newGroup = new UserGroup(core, groupId);

            // Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, "Profile");
                profileAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, "Groups");
                groupsAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, "Gallery");
                galleryAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, "GuestBook");
                guestbookAe.Install(core, newGroup);
            }
            catch
            {
            }

            return newGroup;
        }

        public bool CheckSubGroupNameUnique(string groupName)
        {
            SelectQuery query = new SelectQuery(typeof(SubUserGroup));
            query.AddField(new DataField(typeof(SubUserGroup), "sub_group_name"));
            query.AddCondition(new QueryFunction("sub_group_name", QueryFunctions.ToLowerCase), groupName);
            query.AddCondition("sub_group_parent_id", Id);

            if (Query(query).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

        public static bool CheckGroupNameUnique(Core core, string groupName)
        {
            if (core.Db.Query(string.Format("SELECT group_name FROM group_keys WHERE LCASE(group_name) = '{0}';",
                Mysql.Escape(groupName.ToLower()))).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

        public static bool CheckGroupNameValid(string groupName)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            disallowedNames.Add("about");
            disallowedNames.Add("copyright");
            disallowedNames.Add("register");
            disallowedNames.Add("sign-in");
            disallowedNames.Add("log-in");
            disallowedNames.Add("help");
            disallowedNames.Add("safety");
            disallowedNames.Add("privacy");
            disallowedNames.Add("terms-of-service");
            disallowedNames.Add("site-map");
            disallowedNames.Add(WebConfigurationManager.AppSettings["boxsocial-title"].ToLower());
            disallowedNames.Add("blogs");
            disallowedNames.Add("profiles");
            disallowedNames.Add("search");
            disallowedNames.Add("communities");
            disallowedNames.Add("community");
            disallowedNames.Add("constitution");
            disallowedNames.Add("profile");
            disallowedNames.Add("my-profile");
            disallowedNames.Add("history");
            disallowedNames.Add("get-active");
            disallowedNames.Add("statistics");
            disallowedNames.Add("blog");
            disallowedNames.Add("categories");
            disallowedNames.Add("members");
            disallowedNames.Add("users");
            disallowedNames.Add("upload");
            disallowedNames.Add("support");
            disallowedNames.Add("account");
            disallowedNames.Add("history");
            disallowedNames.Add("browse");
            disallowedNames.Add("feature");
            disallowedNames.Add("featured");
            disallowedNames.Add("favourites");
            disallowedNames.Add("likes");
            disallowedNames.Add("dev");
            disallowedNames.Add("dcma");
            disallowedNames.Add("coppa");
            disallowedNames.Add("guidelines");
            disallowedNames.Add("press");
            disallowedNames.Add("jobs");
            disallowedNames.Add("careers");
            disallowedNames.Add("feedback");
            disallowedNames.Add("create");
            disallowedNames.Add("subscribe");
            disallowedNames.Add("subscriptions");
            disallowedNames.Add("rate");
            disallowedNames.Add("comment");
            disallowedNames.Add("mail");
            disallowedNames.Add("video");
            disallowedNames.Add("videos");
            disallowedNames.Add("music");
            disallowedNames.Add("podcast");
            disallowedNames.Add("podcasts");
            disallowedNames.Add("security");
            disallowedNames.Add("bugs");
            disallowedNames.Add("beta");
            disallowedNames.Add("friend");
            disallowedNames.Add("friends");
            disallowedNames.Add("family");
            disallowedNames.Add("promotion");
            disallowedNames.Add("birthday");
            disallowedNames.Add("account");
            disallowedNames.Add("settings");
            disallowedNames.Add("admin");
            disallowedNames.Add("administrator");
            disallowedNames.Add("administrators");
            disallowedNames.Add("root");
            disallowedNames.Add("my-account");
            disallowedNames.Add("member");
            disallowedNames.Add("anonymous");
            disallowedNames.Add("legal");
            disallowedNames.Add("contact");
            disallowedNames.Add("aonlinesite");
            disallowedNames.Add("images");
            disallowedNames.Add("image");
            disallowedNames.Add("styles");
            disallowedNames.Add("style");
            disallowedNames.Add("theme");
            disallowedNames.Add("header");
            disallowedNames.Add("footer");
            disallowedNames.Add("head");
            disallowedNames.Add("foot");
            disallowedNames.Add("bin");
            disallowedNames.Add("images");
            disallowedNames.Add("templates");
            disallowedNames.Add("cgi-bin");
            disallowedNames.Add("cgi");
            disallowedNames.Add("web.config");
            disallowedNames.Add("report");
            disallowedNames.Add("rules");
            disallowedNames.Add("script");
            disallowedNames.Add("scripts");
            disallowedNames.Add("css");
            disallowedNames.Add("img");
            disallowedNames.Add("App_Data");
            disallowedNames.Add("test");
            disallowedNames.Add("sitepreview");
            disallowedNames.Add("plesk-stat");
            disallowedNames.Add("jakarta");
            disallowedNames.Add("storage");
            disallowedNames.Add("netalert");
            disallowedNames.Add("group");
            disallowedNames.Add("groups");
            disallowedNames.Add("create");
            disallowedNames.Add("edit");
            disallowedNames.Add("delete");
            disallowedNames.Add("remove");
            disallowedNames.Add("sid");
            disallowedNames.Add("network");
            disallowedNames.Add("networks");
            disallowedNames.Add("cart");
            disallowedNames.Add("api");
            disallowedNames.Add("feed");
            disallowedNames.Add("rss");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(groupName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(groupName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            groupName = groupName.Normalize().ToLower();

            if (groupName.Length < 2)
            {
                matches++;
            }

            if (groupName.Length > 64)
            {
                matches++;
            }

            if (groupName.EndsWith(".aspx"))
            {
                matches++;
            }

            if (groupName.EndsWith(".asax"))
            {
                matches++;
            }

            if (groupName.EndsWith(".php"))
            {
                matches++;
            }

            if (groupName.EndsWith(".html"))
            {
                matches++;
            }

            if (groupName.EndsWith(".gif"))
            {
                matches++;
            }

            if (groupName.EndsWith(".png"))
            {
                matches++;
            }

            if (groupName.EndsWith(".js"))
            {
                matches++;
            }

            if (groupName.EndsWith(".bmp"))
            {
                matches++;
            }

            if (groupName.EndsWith(".jpg"))
            {
                matches++;
            }

            if (groupName.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (groupName.EndsWith(".zip"))
            {
                matches++;
            }

            if (groupName.EndsWith(".jsp"))
            {
                matches++;
            }

            if (groupName.EndsWith(".cfm"))
            {
                matches++;
            }

            if (groupName.EndsWith(".exe"))
            {
                matches++;
            }

            if (groupName.StartsWith("."))
            {
                matches++;
            }

            if (groupName.EndsWith("."))
            {
                matches++;
            }

            if (matches > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool CanModerateComments(User member)
        {
            if (IsGroupOperator(member.ItemKey))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool IsItemOwner(User member)
        {
            return false;
        }

        public override ushort GetAccessLevel(User viewer)
        {
            switch (GroupType)
            {
                case "OPEN":
                case "REQUEST":
                case "CLOSED":
                    return 0x0001;
                case "PRIVATE":
                    if (IsGroupMember(viewer.ItemKey))
                    {
                        return 0x0001;
                    }
                    break;
            }

            return 0x0000;
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = string.Empty;
            string path = this.UriStub;

            if (core.IsMobile)
            {
                if (parts.Count > 1)
                {
                    for (int i = 0; i < parts.Count - 2; i++)
                    {
                        if (!parts[i][0].StartsWith("*"))
                        {
                            path += parts[i][0] + "/";
                        }
                    }
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        parts[parts.Count - 2][1], core.Hyperlink.AppendSid(path + parts[parts.Count - 2][0].TrimStart(new char[] { '*' })));
                }
                if (parts.Count == 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        DisplayName, core.Hyperlink.AppendSid(path));
                }
                if (parts.Count == 0)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        core.Prose.GetString("HOME"), core.Hyperlink.AppendCoreSid("/"));
                }
            }
            else
            {
                output = string.Format("<a href=\"{1}\">{0}</a>",
                        DisplayName, core.Hyperlink.AppendSid(path));

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i][0] != string.Empty)
                    {
                        output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                            parts[i][1], core.Hyperlink.AppendSid(path + parts[i][0].TrimStart(new char[] { '*' })));
                        if (!parts[i][0].StartsWith("*"))
                        {
                            path += parts[i][0] + "/";
                        }
                    }
                }
            }

            return output;
        }

        public override string UriStub
        {
            get
            {
                if (string.IsNullOrEmpty(domain))
                {
                    if (core.Http != null && core.Http.Domain != Hyperlink.Domain)
                    {
                        return core.Hyperlink.Uri + "group/" + Slug.ToLower() + "/";
                    }
                    else
                    {
                        return string.Format("/group/{0}/",
                            Slug.ToLower());
                    }
                }
                else
                {
                    if (core.Http != null && domain == core.Http.Domain)
                    {
                        return "/";
                    }
                    else
                    {
                        return string.Format("http://{0}/",
                            domain);
                    }
                }
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                if (string.IsNullOrEmpty(domain))
                {
                    return core.Hyperlink.AppendAbsoluteSid(UriStub);
                }
                else
                {
                    return core.Hyperlink.AppendAbsoluteSid(string.Format("http://{0}/",
                            domain));
                }
            }
        }

        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(UriStub);
            }
        }

        public string MemberlistUri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}members",
                    UriStub));
            }
        }

        public string GetMemberlistUri(string filter)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}members?filter={1}",
                    UriStub, filter));
        }

        public string DeleteUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "groups", "delete", GroupId, true);
            }
        }

        public string JoinUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", "join", GroupId, true);
            }
        }

        public string LeaveUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", "leave", GroupId, true);
            }
        }

        public string InviteUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "invite", GroupId, true);
            }
        }

        public string ResignOperatorUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", "resign-operator", GroupId, true);
            }
        }

        public static List<UserGroup> GetUserGroups(Core core, User member)
        {
            return GetUserGroups(core, member, 1, 10);
        }

        public static List<UserGroup> GetUserGroups(Core core, User member, int page, int perPage)
        {
            List<UserGroup> groups = new List<UserGroup>();

            SelectQuery query = GetSelectQueryStub(UserGroupLoadOptions.Common);
            query.AddJoin(JoinTypes.Left, GetTable(typeof(GroupMember)), "group_id", "group_id");
            query.AddCondition("user_id", member.Id);
            /*if ((!string.IsNullOrEmpty(filter)) && filter.Length == 1)
            {
                query.AddCondition(new DataField(typeof(User), "user_name_first"), filter);
            }*/
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable groupsTable = core.Db.Query(query);

            foreach (DataRow dr in groupsTable.Rows)
            {
                groups.Add(new UserGroup(core, dr, UserGroupLoadOptions.Common));
            }

            return groups;
        }

        public static List<UserGroup> GetUserGroups(Core core, Category category)
        {
            List<UserGroup> groups = new List<UserGroup>();

            SelectQuery query = GetSelectQueryStub(UserGroupLoadOptions.Common);
            query.AddCondition("group_category", category.Id);

            DataTable groupsTable = core.Db.Query(query);

            foreach (DataRow dr in groupsTable.Rows)
            {
                groups.Add(new UserGroup(core, dr, UserGroupLoadOptions.Common));
            }

            return groups;
        }

        public static List<UserGroup> GetUserGroups(Core core, Category category, int page)
        {
            List<UserGroup> groups = new List<UserGroup>();

            SelectQuery query = GetSelectQueryStub(UserGroupLoadOptions.Common);
            query.AddCondition("group_category", category.Id);
            query.LimitStart = (page - 1) * GROUPS_PER_PAGE;
            query.LimitCount = GROUPS_PER_PAGE;

            DataTable groupsTable = core.Db.Query(query);

            foreach (DataRow dr in groupsTable.Rows)
            {
                groups.Add(new UserGroup(core, dr, UserGroupLoadOptions.Common));
            }

            return groups;
        }

        public static void Show(Core core, GPage page)
        {
            core.Template.SetTemplate("Groups", "viewgroup");

            core.Template.Parse("U_GROUP", page.Group.Uri);
            core.Template.Parse("GROUP_DISPLAY_NAME", page.Group.DisplayName);

            core.Template.Parse("PRIMITIVE_THUMB", page.Owner.Thumbnail);
            core.Template.Parse("USER_ICON", page.Owner.Thumbnail);
            core.Template.Parse("USER_COVER_PHOTO", page.Owner.CoverPhoto);
            core.Template.Parse("USER_MOBILE_COVER_PHOTO", page.Owner.MobileCoverPhoto);

            string langMembers = (page.Group.Members != 1) ? "members" : "member";
            string langIsAre = (page.Group.Members != 1) ? "are" : "is";

            //page.template.ParseRaw("DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(page.ThisGroup.Description), core.session.LoggedInMember));
            core.Display.ParseBbcode("DESCRIPTION", page.Group.Description);
            core.Template.Parse("DATE_CREATED", core.Tz.DateTimeToString(page.Group.DateCreated(core.Tz)));
            core.Template.Parse("CATEGORY", page.Group.Category);

            core.Template.Parse("MEMBERS", page.Group.Members.ToString());
            core.Template.Parse("OPERATORS", page.Group.Operators.ToString());
            core.Template.Parse("OFFICERS", page.Group.Officers.ToString());
            core.Template.Parse("L_MEMBERS", langMembers);
            core.Template.Parse("L_IS_ARE", langIsAre);
            core.Template.Parse("U_MEMBERLIST", page.Group.MemberlistUri);

            if (core.Session.IsLoggedIn)
            {
                if (page.Group.IsGroupOperator(core.Session.LoggedInMember.ItemKey))
                {
                    core.Template.Parse("IS_OPERATOR", "TRUE");
                    core.Template.Parse("U_GROUP_ACCOUNT", core.Hyperlink.AppendSid(page.Group.AccountUriStub));
                }

                if (!page.Group.IsGroupMemberAbsolute(core.Session.LoggedInMember.ItemKey))
                {
                    core.Template.Parse("U_JOIN", page.Group.JoinUri);
                }
                else
                {
                    if (!page.Group.IsGroupOperator(core.Session.LoggedInMember.ItemKey))
                    {
                        if (page.Group.IsGroupMember(core.Session.LoggedInMember.ItemKey))
                        {
                            core.Template.Parse("U_LEAVE", page.Group.LeaveUri);
                        }
                        else if (page.Group.IsGroupMemberPending(core.Session.LoggedInMember.ItemKey))
                        {
                            core.Template.Parse("U_CANCEL", page.Group.LeaveUri);
                        }
                    }

                    if (page.Group.IsGroupMember(core.Session.LoggedInMember.ItemKey))
                    {
                        core.Template.Parse("U_INVITE", page.Group.InviteUri);
                    }
                }
            }

            List<GroupMember> members = page.Group.GetMembers(1, 8);

            foreach (GroupMember member in members)
            {
                VariableCollection membersVariableCollection = core.Template.CreateChild("member_list");

                membersVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                membersVariableCollection.Parse("U_PROFILE", member.Uri);
                membersVariableCollection.Parse("ICON", member.Icon);
                membersVariableCollection.Parse("TILE", member.Tile);
            }

            List<GroupMember> operators = page.Group.GetOperators();

            foreach (GroupMember groupOperator in operators)
            {
                VariableCollection operatorsVariableCollection = core.Template.CreateChild("operator_list");

                operatorsVariableCollection.Parse("USER_DISPLAY_NAME", groupOperator.DisplayName);
                operatorsVariableCollection.Parse("U_PROFILE", groupOperator.Uri);
                if (core.Session.LoggedInMember != null)
                {
                    if (groupOperator.UserId == core.Session.LoggedInMember.UserId)
                    {
                        operatorsVariableCollection.Parse("U_RESIGN", page.Group.ResignOperatorUri);
                    }
                }
            }

            List<GroupOfficer> officers = page.Group.GetOfficers();

            foreach (GroupOfficer groupOfficer in officers)
            {
                VariableCollection officersVariableCollection = core.Template.CreateChild("officer_list");

                officersVariableCollection.Parse("USER_DISPLAY_NAME", groupOfficer.DisplayName);
                officersVariableCollection.Parse("U_PROFILE", groupOfficer.Uri);
                officersVariableCollection.Parse("OFFICER_TITLE", groupOfficer.OfficeTitle);
                if (core.LoggedInMemberId > 0)
                {
                    if (page.Group.IsGroupOperator(core.Session.LoggedInMember.ItemKey))
                    {
                        officersVariableCollection.Parse("U_REMOVE", groupOfficer.BuildRemoveOfficerUri());
                    }
                }
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            if (!core.IsMobile)
            {
                breadCrumbParts.Add(new string[] { "profile", "Profile" });
            }

            page.Owner.ParseBreadCrumbs(breadCrumbParts);

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Group, page.Group));
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            core.Template.SetTemplate("Groups", "viewgroupmemberlist");

            core.Template.Parse("MEMBERS_TITLE", "Member list for " + page.Group.DisplayName);
            core.Template.Parse("MEMBERS", ((ulong)page.Group.Members).ToString());

            core.Template.Parse("U_FILTER_ALL", page.Group.MemberlistUri);
            core.Template.Parse("U_FILTER_BEGINS_A", page.Group.GetMemberlistUri("a"));
            core.Template.Parse("U_FILTER_BEGINS_B", page.Group.GetMemberlistUri("b"));
            core.Template.Parse("U_FILTER_BEGINS_C", page.Group.GetMemberlistUri("c"));
            core.Template.Parse("U_FILTER_BEGINS_D", page.Group.GetMemberlistUri("d"));
            core.Template.Parse("U_FILTER_BEGINS_E", page.Group.GetMemberlistUri("e"));
            core.Template.Parse("U_FILTER_BEGINS_F", page.Group.GetMemberlistUri("f"));
            core.Template.Parse("U_FILTER_BEGINS_G", page.Group.GetMemberlistUri("g"));
            core.Template.Parse("U_FILTER_BEGINS_H", page.Group.GetMemberlistUri("h"));
            core.Template.Parse("U_FILTER_BEGINS_I", page.Group.GetMemberlistUri("i"));
            core.Template.Parse("U_FILTER_BEGINS_J", page.Group.GetMemberlistUri("j"));
            core.Template.Parse("U_FILTER_BEGINS_K", page.Group.GetMemberlistUri("k"));
            core.Template.Parse("U_FILTER_BEGINS_L", page.Group.GetMemberlistUri("l"));
            core.Template.Parse("U_FILTER_BEGINS_M", page.Group.GetMemberlistUri("m"));
            core.Template.Parse("U_FILTER_BEGINS_N", page.Group.GetMemberlistUri("n"));
            core.Template.Parse("U_FILTER_BEGINS_O", page.Group.GetMemberlistUri("o"));
            core.Template.Parse("U_FILTER_BEGINS_P", page.Group.GetMemberlistUri("p"));
            core.Template.Parse("U_FILTER_BEGINS_Q", page.Group.GetMemberlistUri("q"));
            core.Template.Parse("U_FILTER_BEGINS_R", page.Group.GetMemberlistUri("r"));
            core.Template.Parse("U_FILTER_BEGINS_S", page.Group.GetMemberlistUri("s"));
            core.Template.Parse("U_FILTER_BEGINS_T", page.Group.GetMemberlistUri("t"));
            core.Template.Parse("U_FILTER_BEGINS_U", page.Group.GetMemberlistUri("u"));
            core.Template.Parse("U_FILTER_BEGINS_V", page.Group.GetMemberlistUri("v"));
            core.Template.Parse("U_FILTER_BEGINS_W", page.Group.GetMemberlistUri("w"));
            core.Template.Parse("U_FILTER_BEGINS_X", page.Group.GetMemberlistUri("x"));
            core.Template.Parse("U_FILTER_BEGINS_Y", page.Group.GetMemberlistUri("y"));
            core.Template.Parse("U_FILTER_BEGINS_Z", page.Group.GetMemberlistUri("z"));

            if (page.Group.IsGroupOperator(core.LoggedInMemberItemKey))
            {
                core.Template.Parse("GROUP_OPERATOR", "TRUE");

                SelectQuery query = GroupMember.GetSelectQueryStub(UserLoadOptions.All);
                query.AddCondition("group_members.group_id", page.Group.Id);
                query.AddCondition("group_member_approved", false);
                query.AddSort(SortOrder.Ascending, "group_member_date_ut");

                DataTable approvalTable = core.Db.Query(query);

                if (approvalTable.Rows.Count > 0)
                {
                    core.Template.Parse("IS_WAITING_APPROVAL", "TRUE");
                }

                for (int i = 0; i < approvalTable.Rows.Count; i++)
                {
                    GroupMember approvalMember = new GroupMember(core, approvalTable.Rows[i], UserLoadOptions.Profile);

                    VariableCollection approvalVariableCollection = core.Template.CreateChild("approval_list");

                    approvalVariableCollection.Parse("USER_DISPLAY_NAME", approvalMember.DisplayName);
                    approvalVariableCollection.Parse("U_PROFILE", approvalMember.Uri);
                    approvalVariableCollection.Parse("U_APPROVE", approvalMember.ApproveMemberUri);
                }

            }

            List<GroupMember> members = page.Group.GetMembers(page.TopLevelPageNumber, 18, core.Functions.GetFilter());
            foreach (GroupMember member in members)
            {
                VariableCollection memberVariableCollection = core.Template.CreateChild("member_list");

                memberVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                memberVariableCollection.Parse("JOIN_DATE", page.tz.DateTimeToString(member.GetGroupMemberJoinDate(page.tz)));
                memberVariableCollection.Parse("USER_AGE", member.Profile.AgeString);
                memberVariableCollection.Parse("USER_COUNTRY", member.Profile.Country);
                memberVariableCollection.Parse("USER_CAPTION", string.Empty);

                memberVariableCollection.Parse("U_PROFILE", member.Uri);
                if (core.LoggedInMemberId > 0)
                {
                    if (page.Group.IsGroupOperator(core.LoggedInMemberItemKey))
                    {
                        if (!member.IsOperator)
                        {
                            // let's say you can't ban an operator, show ban link if not an operator
                            memberVariableCollection.Parse("U_BAN", member.BanUri);
                            memberVariableCollection.Parse("U_MAKE_OPERATOR", member.MakeOperatorUri);
                        }

                        memberVariableCollection.Parse("U_MAKE_OFFICER", member.MakeOfficerUri);
                    }
                }
                memberVariableCollection.Parse("ICON", member.Icon);
                memberVariableCollection.Parse("TILE", member.Tile);
                memberVariableCollection.Parse("MOBILE_COVER", member.MobileCoverPhoto);

                memberVariableCollection.Parse("ID", member.Id);
                memberVariableCollection.Parse("TYPE", member.TypeId);
                memberVariableCollection.Parse("LOCATION", member.Profile.Country);
                memberVariableCollection.Parse("ABSTRACT", page.Core.Bbcode.Parse(member.Profile.Autobiography));
                memberVariableCollection.Parse("SUBSCRIBERS", member.Info.Subscribers);

                if (Subscription.IsSubscribed(page.Core, member.ItemKey))
                {
                    memberVariableCollection.Parse("SUBSCRIBERD", "TRUE");
                    memberVariableCollection.Parse("U_SUBSCRIBE", page.Core.Hyperlink.BuildUnsubscribeUri(member.ItemKey));
                }
                else
                {
                    memberVariableCollection.Parse("U_SUBSCRIBE", page.Core.Hyperlink.BuildSubscribeUri(member.ItemKey));
                }

                if (page.Core.Session.SignedIn && member.Id == page.Core.LoggedInMemberId)
                {
                    memberVariableCollection.Parse("ME", "TRUE");
                }
            }

            string pageUri = page.Group.MemberlistUri;
            core.Display.ParsePagination(pageUri, 18, page.Group.Members);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "members", "Members" });

            page.Group.ParseBreadCrumbs(breadCrumbParts);
        }

        private static void prepareNewCaptcha(Core core)
        {
            // prepare the captcha
            string captchaString = Captcha.GenerateCaptchaString();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            Confirmation.ClearStale(core, core.Session.SessionId, 2);

            // create a new confimation code
            Confirmation confirm = Confirmation.Create(core, core.Session.SessionId, captchaString, 2);

            core.Template.Parse("U_CAPTCHA", core.Hyperlink.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }

        internal static void ShowRegister(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Groups", "creategroup.html");

            if (e.Core.Session.IsLoggedIn == false)
            {
                e.Template.Parse("REDIRECT_URI", "/sign-in/?redirect=/groups/register");
                e.Core.Display.ShowMessage("Not Logged In", "You must be logged in to register a new group.");
                return;
            }

            string selected = "checked=\"checked\" ";
            long category = 1;
            bool categoryError = false;
            bool typeError = false;
            bool categoryFound = true;
            string slug = e.Core.Http.Form["slug"];
            string title = e.Core.Http.Form["title"];

            try
            {
                category = short.Parse(e.Core.Http["category"]);
            }
            catch
            {
                categoryError = true;
            }

            if (string.IsNullOrEmpty(slug))
            {
                slug = title;
            }

            if (!string.IsNullOrEmpty(title))
            {
                // normalise slug if it has been fiddeled with
                slug = slug.ToLower().Normalize(NormalizationForm.FormD);
                string normalisedSlug = "";

                for (int i = 0; i < slug.Length; i++)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                    {
                        normalisedSlug += slug[i];
                    }
                }
                slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");
            }

            SelectBox categoriesSelectBox = new SelectBox("category");

            SelectQuery query = Item.GetSelectQueryStub(typeof(Category));
            query.AddSort(SortOrder.Ascending, "category_title");

            DataTable categoriesTable = e.Db.Query(query);
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                Category cat = new Category(e.Core, categoryRow);

                categoriesSelectBox.Add(new SelectBoxItem(cat.Id.ToString(), cat.Title));

                if (category == cat.Id)
                {
                    categoryFound = true;
                }
            }

            categoriesSelectBox.SelectedKey = category.ToString();

            if (!categoryFound)
            {
                categoryError = true;
            }

            if (e.Core.Http.Form["submit"] == null)
            {
                prepareNewCaptcha(e.Core);

                e.Template.Parse("S_CATEGORIES", categoriesSelectBox);
                e.Template.Parse("S_OPEN_CHECKED", selected);
            }
            else
            {
                // submit the form
                e.Template.Parse("GROUP_TITLE", (string)e.Core.Http.Form["title"]);
                e.Template.Parse("GROUP_NAME_SLUG", slug);
                e.Template.Parse("GROUP_DESCRIPTION", (string)e.Core.Http.Form["description"]);
                e.Template.Parse("S_CATEGORIES", categoriesSelectBox);

                switch ((string)e.Core.Http.Form["type"])
                {
                    case "open":
                        e.Template.Parse("S_OPEN_CHECKED", selected);
                        break;
                    case "request":
                        e.Template.Parse("S_REQUEST_CHECKED", selected);
                        break;
                    case "closed":
                        e.Template.Parse("S_CLOSED_CHECKED", selected);
                        break;
                    case "private":
                        e.Template.Parse("S_PRIVATE_CHECKED", selected);
                        break;
                    default:
                        typeError = true;
                        break;
                }

                DataTable confirmTable = e.Db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 2 AND session_id = '{0}' LIMIT 1",
                    Mysql.Escape(e.Core.Session.SessionId)));

                if (confirmTable.Rows.Count != 1)
                {
                    e.Template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha(e.Core);
                }
                else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != ((string)e.Core.Http.Form["captcha"]).ToLower())
                {
                    e.Template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha(e.Core);
                }
                else if (!UserGroup.CheckGroupNameValid(slug))
                {
                    e.Template.Parse("ERROR", "Group slug is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha(e.Core);
                }
                else if (!UserGroup.CheckGroupNameUnique(e.Core, slug))
                {
                    e.Template.Parse("ERROR", "Group slug is already taken, please choose another one.");
                    prepareNewCaptcha(e.Core);
                }
                else if (categoryError)
                {
                    e.Template.Parse("ERROR", "Invalid Category selected, you may have to reload the page.");
                    prepareNewCaptcha(e.Core);
                }
                else if (typeError)
                {
                    e.Template.Parse("ERROR", "Invalid group type selected, you may have to reload the page.");
                    prepareNewCaptcha(e.Core);
                }
                else if ((string)e.Core.Http.Form["agree"] != "true")
                {
                    e.Template.Parse("ERROR", "You must accept the " + e.Core.Settings.SiteTitle + " Terms of Service to create a group.");
                    prepareNewCaptcha(e.Core);
                }
                else
                {
                    UserGroup newGroup = null;
                    try
                    {
                        newGroup = UserGroup.Create(e.Core, e.Core.Http.Form["title"], slug, e.Core.Http.Form["description"], category, e.Core.Http.Form["type"]);
                    }
                    catch (InvalidOperationException)
                    {
                        /*Response.Write("InvalidOperationException<br />");
                        Response.Write(e.Db.QueryList);
                        Response.End();*/
                    }
                    catch (InvalidGroupException)
                    {
                        /*Response.Write("InvalidGroupException<br />");
                        Response.Write(e.Db.QueryList);
                        Response.End();*/
                    }

                    if (newGroup == null)
                    {
                        e.Template.Parse("ERROR", "Bad registration details");
                        prepareNewCaptcha(e.Core);
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        e.Db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 2 AND session_id = '{0}'",
                            Mysql.Escape(e.Core.Session.SessionId)));

                        //Response.Redirect("/", true);
                        e.Template.Parse("REDIRECT_URI", newGroup.Uri);
                        e.Core.Display.ShowMessage("Group Created", "You have have created a new group. You will be redirected to the group home page in a second.");
                        return; /* stop processing the display of this page */
                    }
                }
            }
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("{0}account/",
                    UriStub, Key);
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion

        public override Access Access
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

        public override bool IsSimplePermissions
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

        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(User.EveryoneGroupKey, "EVERYONE", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(User.RegisteredUsersGroupKey, "REGISTERED_USERS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(UserGroup.GroupOperatorsGroupKey, "OPERATORS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(UserGroup.GroupOfficersGroupKey, "OFFICERS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(UserGroup.GroupMembersGroupKey, "MEMBERS", null, string.Empty));

            return ppgs;
        }

        public override List<User> GetPermissionUsers()
        {
            List<GroupMember> members = GetMembers(1, 10);

            List<User> users = new List<User>();

            foreach (GroupMember member in members)
            {
                users.Add(member);
            }

            return users;
        }

        public override List<User> GetPermissionUsers(string namePart)
        {
            List<GroupMember> members = GetMembers(1, 10, namePart);

            List<User> users = new List<User>();

            foreach (GroupMember member in members)
            {
                users.Add(member);
            }

            return users;
        }

        public static List<PrimitivePermissionGroup> UserGroup_GetPrimitiveGroups(Core core, Primitive owner)
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            if (owner is User)
            {
                List<UserGroup> groups = UserGroup.GetUserGroups(core, (User)owner);

                foreach (UserGroup group in groups)
                {
                    ppgs.Add(new PrimitivePermissionGroup(group.TypeId, group.Id, group.DisplayName, group.Tile));
                }
            }

            return ppgs;
        }

        public bool GetDefaultAccess(string permission)
        {
            switch (permission)
            {
                case "VIEW":
                    switch (GroupType.ToUpper())
                    {
                        case "OPEN":
                            return true;
                        case "REQUEST":
                        case "CLOSED":
                            if (IsGroupMember(core.Session.LoggedInMember.ItemKey))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        case "PRIVATE":
                            return false;
                    }
                    break;
                case "COMMENT":
                    if (IsGroupMember(core.Session.LoggedInMember.ItemKey))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }

            return false;
        }

        public override bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey)
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsGroupMember(viewer);
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsGroupOperator(core.Session.LoggedInMember.ItemKey);
            }

            return false;
        }

        public override bool CanEditItem()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsGroupOperator(core.Session.LoggedInMember.ItemKey);
            }

            return false;
        }

        public override bool CanDeleteItem()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsGroupOperator(core.Session.LoggedInMember.ItemKey);
            }

            return false;
        }

        public override bool GetDefaultCan(string permission, ItemKey viewer)
        {
            switch (permission)
            {
                case "COMMENT":
                    return IsGroupMember(viewer);
                case "DELETE_COMMENTS":
                    return IsGroupOperator(viewer);
            }
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "Group: " + DisplayName;
            }
        }
        public override  string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }


        public static ItemKey GroupOperatorsGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(GroupOperator)));
            }
        }

        public static ItemKey GroupOfficersGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(GroupOfficer)));
            }
        }

        public static ItemKey GroupMembersGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(GroupMember)));
            }
        }

        internal static string BuildCategoryUri(Core core, Internals.Category category)
        {
            return core.Hyperlink.AppendSid("/groups/" + category.Path);
        }

        public string Noun
        {
            get
            {
                return "guest book";
            }
        }

        public string GroupTiny
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_tiny/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_tiny/{0}.png",
                        Key));
                }
            }
        }

        public override string Thumbnail
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_thumb/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_thumb/{0}.png",
                        Key));
                }
            }
        }

        public string GroupMobile
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_mobile/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_mobile/{0}.png",
                        Key));
                }
            }
        }
        /// <summary>
        /// 50x50 display tile
        /// </summary>
        public override string Icon
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_icon/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_icon/{0}.png",
                        Key));
                }
            }
        }

        /// <summary>
        /// 100x100 display tile
        /// </summary>
        public override string Tile
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_tile/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_tile/{0}.png",
                        Key));
                }
            }
        }

        /// <summary>
        /// 200x200 display tile
        /// </summary>
        public string GroupSquare
        {
            get
            {
                if (GroupInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_square/_{1}.png",
                        UriStub, Key);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/group/_square/{0}.png",
                        Key));
                }
            }
        }


        public override string CoverPhoto
        {
            get
            {
                if (groupCoverPhotoUri == "FALSE")
                {
                    return "FALSE";
                }
                else if (groupCoverPhotoUri != null)
                {
                    return string.Format("{0}images/_cover{1}",
                        UriStub, groupCoverPhotoUri);
                }
                else
                {
                    SelectQuery query = new SelectQuery("gallery_items");
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    query.AddCondition("gallery_item_id", GroupInfo.CoverPhotoId);

                    DataTable coverTable = db.Query(query);

                    if (coverTable.Rows.Count == 1)
                    {
                        if (!(coverTable.Rows[0]["gallery_item_uri"] is DBNull))
                        {
                            groupCoverPhotoUri = string.Format("/{0}/{1}",
                                (string)coverTable.Rows[0]["gallery_item_parent_path"], (string)coverTable.Rows[0]["gallery_item_uri"]);

                            return string.Format("{0}images/_cover{1}",
                                UriStub, groupCoverPhotoUri);
                        }
                    }

                    groupCoverPhotoUri = "FALSE";
                    return "FALSE";
                }
            }
        }

        public override string MobileCoverPhoto
        {
            get
            {
                if (groupCoverPhotoUri == "FALSE")
                {
                    return "FALSE";
                }
                else if (groupCoverPhotoUri != null)
                {
                    return string.Format("{0}images/_mcover{1}",
                        UriStub, groupCoverPhotoUri);
                }
                else
                {
                    SelectQuery query = new SelectQuery("gallery_items");
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    query.AddCondition("gallery_item_id", GroupInfo.CoverPhotoId);

                    DataTable coverTable = db.Query(query);

                    if (coverTable.Rows.Count == 1)
                    {
                        if (!(coverTable.Rows[0]["gallery_item_uri"] is DBNull))
                        {
                            groupCoverPhotoUri = string.Format("/{0}/{1}",
                                (string)coverTable.Rows[0]["gallery_item_parent_path"], (string)coverTable.Rows[0]["gallery_item_uri"]);

                            return string.Format("{0}images/_mcover{1}",
                                UriStub, groupCoverPhotoUri);
                        }
                    }

                    groupCoverPhotoUri = "FALSE";
                    return "FALSE";
                }
            }
        }


        public Dictionary<string, string> GetNotificationActions(string verb)
        {
            Dictionary<string, string> actions = new Dictionary<string, string>();
            switch (verb)
            {
                case "invite":
                    actions.Add("invite-join", core.Prose.GetString("JOIN"));
                    break;
            }
            return actions;
        }

        public string GetNotificationActionUrl(string action)
        {
            switch (action)
            {
                case "invite-join":
                    return JoinUri;
            }

            return string.Empty;
        }

        public string Title
        {
            get
            {
                return DisplayName;
            }
        }
    }

    public class InvalidGroupException : Exception
    {
    }
}
