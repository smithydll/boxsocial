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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public enum UserSubGroupLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Common = Key | Info,
        All = Key | Info,
    }

    [DataTable("sub_groups", "SUBGROUP")]
    public class SubUserGroup : Primitive
    {
        [DataField("sub_group_id", DataFieldKeys.Primary)]
        private long subGroupId;
        [DataField("sub_group_parent_id")]
        private long parentId;
        [DataField("sub_group_name", DataFieldKeys.Unique, 64)]
        private string slug;
        [DataField("sub_group_name_first")]
        private char first;
        [DataField("sub_group_name_display", 64)]
        private string displayName;
        [DataField("sub_group_type", 15)]
        private string subGroupType;
        [DataField("sub_group_colour")]
        private uint colour;
        [DataField("sub_group_reg_date_ut")]
        private long timestampCreated;
        [DataField("sub_group_reg_ip", 50)]
        private string registrationIp;
        [DataField("sub_group_members")]
        private long memberCount;
        [DataField("sub_group_leaders")]
        private long leaderCount;
        [DataField("sub_group_abstract", MYSQL_TEXT)]
        private string description;

        private string displayNameOwnership = null;
        private UserGroup parent;
        private Access access;

        private Dictionary<User, bool> groupMemberCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupMemberPendingCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupMemberAbsoluteCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupLeaderCache = new Dictionary<User, bool>();

        public long SubGroupId
        {
            get
            {
                return Id;
            }
        }

        public override long Id
        {
            get
            {
                return subGroupId;
            }
        }

        public override string Key
        {
            get
            {
                return slug;
            }
        }

        public override string Type
        {
            get
            {
                return "SUBGROUP";
            }
        }

        public string Title
        {
            get
            {
                return displayName;
            }
            set
            {
                SetProperty("displayName", value);
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                SetProperty("description", value);
            }
        }

        public long MemberCount
        {
            get
            {
                return memberCount;
            }
        }

        public long LeaderCount
        {
            get
            {
                return leaderCount;
            }
        }

        public string SubGroupType
        {
            get
            {
                return subGroupType;
            }
            set
            {
                SetProperty("subGroupType", value);
            }
        }

        public UserGroup Parent
        {
            get
            {
                if (parent == null || parent.Id != parentId)
                {
                    ItemKey key = new ItemKey(parentId, ItemType.GetTypeId(typeof(UserGroup)));
                    core.PrimitiveCache.LoadPrimitiveProfile(key);
                    parent = (UserGroup)core.PrimitiveCache[key];
                }
                return parent;
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

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.SubGroup;
            }
        }

        public override string UriStub
        {
            get
            {
                return Parent.UriStub + "groups/" + slug + "/";
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return Parent.UriStubAbsolute + "groups/" + slug + "/";
            }
        }

        public string GetUri(string filter)
        {
            return core.Uri.AppendSid(UriStub + "?filter=" + filter);
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(UriStub);
            }
        }

        public string JoinUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri(Parent, "groups", "subgroups", "join", Id, true);
            }
        }

        public string LeaveUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri(Parent, "groups", "subgroups", "leave", Id, true);
            }
        }

        public string EditMembersUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri(Parent, "groups", "subgroups", "members", Id, true);
            }
        }

        public string EditUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri(Parent, "groups", "subgroups", "edit", Id, true);
            }
        }

        public string DeleteUri
        {
            get
            {
                return core.Uri.BuildAccountSubModuleUri(Parent, "groups", "subgroups", "delete", Id, true);
            }
        }

        public override string TitleName
        {
            get
            {
                return "the group " + DisplayName;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the group " + DisplayNameOwnership;
            }
        }

        public override string DisplayName
        {
            get
            {
                return Title;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != string.Empty) ? displayName : displayName;

                    if (displayNameOwnership.EndsWith("s"))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
            }
        }

        public override bool CanModerateComments(User member)
        {
            return false;
        }

        public override bool IsCommentOwner(User member)
        {
            return false;
        }

        public override ushort GetAccessLevel(User viewer)
        {
            switch (SubGroupType)
            {
                case "OPEN":
                case "REQUEST":
                case "CLOSED":
                    return 0x0001;
                case "PRIVATE":
                    if (IsSubGroupMember(viewer))
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
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    DisplayName, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != string.Empty)
                {
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path + parts[i][0].TrimStart(new char[] { '*' }));
                    if (!parts[i][0].StartsWith("*"))
                    {
                        path += parts[i][0] + "/";
                    }
                }
            }

            return output;
        }

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

        public override List<AccessControlPermission> AclPermissions
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            return ppgs;
        }

        public override bool GetIsMemberOfPrimitive(User viewer, ItemKey primitiveKey)
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsSubGroupMember(viewer);
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            return Parent.CanEditPermissions();
        }

        public override bool CanEditItem()
        {
            return Parent.CanEditItem();
        }

        public override bool CanDeleteItem()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsSubGroupLeader(core.Session.LoggedInMember);
            }

            return false;
        }

        public override bool GetDefaultCan(string permission)
        {
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "User Group: " + DisplayName;
            }
        }

        public SubUserGroup(Core core, long groupId)
            : this(core, groupId, UserSubGroupLoadOptions.Info)
        {
        }

        public SubUserGroup(Core core, long groupId, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                LoadItem(groupId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        public SubUserGroup(Core core, string groupName)
            : this(core, groupName, UserSubGroupLoadOptions.Info)
        {
        }

        public SubUserGroup(Core core, string groupName, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                LoadItem("sub_group_name", groupName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        public SubUserGroup(Core core, DataRow groupRow, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                loadItemInfo(groupRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        void SubUserGroup_ItemLoad()
        {
        }

        private void preLoadMemberCache(User member)
        {
            SelectQuery query = SubGroupMember.GetSelectQueryStub(typeof(SubGroupMember));
            query.AddFields("user_id", "sub_group_member_approved");
            query.AddCondition("sub_group_id", Id);
            query.AddCondition("user_id", member.UserId);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count > 0)
            {
                switch ((GroupMemberApproval)(byte)memberTable.Rows[0]["sub_group_member_approved"])
                {
                    case GroupMemberApproval.Pending:
                        groupMemberCache.Add(member, false);
                        groupMemberPendingCache.Add(member, true);
                        groupMemberAbsoluteCache.Add(member, true);
                        break;
                    case GroupMemberApproval.Member:
                        groupMemberCache.Add(member, true);
                        groupMemberPendingCache.Add(member, false);
                        groupMemberAbsoluteCache.Add(member, true);
                        break;
                }

                if ((bool)memberTable.Rows[0]["sub_group_member_is_leader"])
                {
                    groupLeaderCache.Add(member, true);
                }
                else
                {
                    groupLeaderCache.Add(member, false);
                }
            }
            else
            {
                groupMemberCache.Add(member, false);
                groupMemberPendingCache.Add(member, false);
                groupMemberAbsoluteCache.Add(member, false);
                groupLeaderCache.Add(member, false);
            }
        }

        public bool IsSubGroupMember(User member)
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

        public bool IsSubGroupLeader(User member)
        {
            if (member != null)
            {
                if (groupLeaderCache.ContainsKey(member))
                {
                    return groupLeaderCache[member];
                }
                else
                {
                    preLoadMemberCache(member);
                    return groupLeaderCache[member];
                }
            }
            return false;
        }

        public List<SubGroupMember> GetLeaders()
        {
            List<SubGroupMember> leaders = new List<SubGroupMember>();

            SelectQuery query = new SelectQuery("sub_group_members");
            query.AddJoin(JoinTypes.Inner, "user_keys", "user_id", "user_id");
            query.AddFields(GroupMember.GetFieldsPrefixed(typeof(SubGroupMember)));
            query.AddCondition("`sub_group_members`.`sub_group_id`", subGroupId);
            query.AddCondition("sub_group_member_is_leader", true);
            query.AddCondition("sub_group_member_approved", true);
            query.AddSort(SortOrder.Ascending, "sub_group_member_date_ut");

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                leaders.Add(new SubGroupMember(core, dr));
            }

            return leaders;
        }

        public List<SubGroupMember> GetMembers(int page, int perPage)
        {
            return GetMembers(page, perPage, null);
        }

        public List<SubGroupMember> GetMembers(int page, int perPage, string filter)
        {
            List<SubGroupMember> members = new List<SubGroupMember>();

            SelectQuery query = new SelectQuery("sub_group_members");
            query.AddJoin(JoinTypes.Inner, "user_keys", "user_id", "user_id");
            query.AddFields(GroupMember.GetFieldsPrefixed(typeof(SubGroupMember)));
            query.AddCondition("`sub_group_members`.`sub_group_id`", subGroupId);
            query.AddCondition("sub_group_member_is_leader", false);
            query.AddCondition("sub_group_member_approved", true);
            if (!string.IsNullOrEmpty(filter))
            {
                query.AddCondition("user_keys.user_name_first", filter);
            }
            query.AddSort(SortOrder.Ascending, "sub_group_member_date_ut");
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
                members.Add(new SubGroupMember(core, dr));
            }

            return members;
        }

        public List<SubGroupMember> GetMembersWaitingApproval()
        {
            List<SubGroupMember> members = new List<SubGroupMember>();

            SelectQuery query = new SelectQuery("sub_group_members");
            query.AddJoin(JoinTypes.Inner, "user_keys", "user_id", "user_id");
            query.AddFields(GroupMember.GetFieldsPrefixed(typeof(SubGroupMember)));
            query.AddCondition("`sub_group_members`.`sub_group_id`", subGroupId);
            query.AddCondition("sub_group_member_is_leader", false);
            query.AddCondition("sub_group_member_approved", false);
            query.AddSort(SortOrder.Ascending, "sub_group_member_date_ut");

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new SubGroupMember(core, dr));
            }

            return members;
        }

        public bool AddMember(User member, bool approved, bool isLeader, bool isDefault)
        {
            if (Parent.IsGroupMember(member))
            {
                InsertQuery iQuery = new InsertQuery(typeof(SubGroupMember));
                iQuery.AddField("user_id", member.Id);
                iQuery.AddField("group_id", Parent.Id);
                iQuery.AddField("sub_group_id", Id);
                iQuery.AddField("sub_group_member_date_ut", UnixTime.UnixTimeStamp());
                iQuery.AddField("sub_group_member_ip", core.Session.IPAddress.ToString());
                iQuery.AddField("sub_group_member_approved", approved);
                iQuery.AddField("sub_group_member_is_leader", isLeader);
                iQuery.AddField("sub_group_member_default", isDefault);

                db.Query(iQuery);

                return true;
            }
            else
            {
                return false;
            }

        }

        public static SubUserGroup Create(Core core, UserGroup parent, string groupTitle, string groupSlug, string groupDescription, string groupType)
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

            if (!parent.CheckSubGroupNameUnique(groupSlug))
            {
                throw new GroupNameNotUniqueException();
            }

            switch (groupType.ToLower())
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

            Item item = Item.Create(core, typeof(SubUserGroup), new FieldValuePair("sub_group_parent_id", parent.Id),
                new FieldValuePair("sub_group_name", groupSlug),
                new FieldValuePair("sub_group_name_first", groupSlug[0]),
                new FieldValuePair("sub_group_name_display", groupTitle),
                new FieldValuePair("sub_group_type", groupType),
                new FieldValuePair("sub_group_reg_ip", core.Session.IPAddress.ToString()),
                new FieldValuePair("sub_group_reg_date_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("sub_group_colour", 0x000000),
                new FieldValuePair("sub_group_members", 0));

            /*db.UpdateQuery(string.Format("INSERT INTO sub_group_members (user_id, group_id, sub_group_id, sub_group_member_approved, sub_group_member_ip, sub_group_member_date_ut, sub_group_member_is_leader) VALUES ({0}, {1}, {2}, 1, '{3}', UNIX_TIMESTAMP(), 1)",
                session.LoggedInMember.UserId, parent.Id, ((SubUserGroup)item).Id, Mysql.Escape(session.IPAddress.ToString())));*/

            return (SubUserGroup)item;
        }

        public static void Show(object sender, ShowPageEventArgs e)
        {
            e.Page.template.SetTemplate("Groups", "viewsubgroup");

            SubUserGroup subgroup;

            try
            {
                subgroup = new SubUserGroup(e.Core, e.Core.PagePathParts[1].Value);
            }
            catch (InvalidSubGroupException)
            {
                e.Core.Functions.Generate404();
                return;
            }

            e.Template.Parse("U_FILTER_ALL", subgroup.Uri);
            e.Template.Parse("U_FILTER_BEGINS_A", subgroup.GetUri("a"));
            e.Template.Parse("U_FILTER_BEGINS_B", subgroup.GetUri("b"));
            e.Template.Parse("U_FILTER_BEGINS_C", subgroup.GetUri("c"));
            e.Template.Parse("U_FILTER_BEGINS_D", subgroup.GetUri("d"));
            e.Template.Parse("U_FILTER_BEGINS_E", subgroup.GetUri("e"));
            e.Template.Parse("U_FILTER_BEGINS_F", subgroup.GetUri("f"));
            e.Template.Parse("U_FILTER_BEGINS_G", subgroup.GetUri("g"));
            e.Template.Parse("U_FILTER_BEGINS_H", subgroup.GetUri("h"));
            e.Template.Parse("U_FILTER_BEGINS_I", subgroup.GetUri("i"));
            e.Template.Parse("U_FILTER_BEGINS_J", subgroup.GetUri("j"));
            e.Template.Parse("U_FILTER_BEGINS_K", subgroup.GetUri("k"));
            e.Template.Parse("U_FILTER_BEGINS_L", subgroup.GetUri("l"));
            e.Template.Parse("U_FILTER_BEGINS_M", subgroup.GetUri("m"));
            e.Template.Parse("U_FILTER_BEGINS_N", subgroup.GetUri("n"));
            e.Template.Parse("U_FILTER_BEGINS_O", subgroup.GetUri("o"));
            e.Template.Parse("U_FILTER_BEGINS_P", subgroup.GetUri("p"));
            e.Template.Parse("U_FILTER_BEGINS_Q", subgroup.GetUri("q"));
            e.Template.Parse("U_FILTER_BEGINS_R", subgroup.GetUri("r"));
            e.Template.Parse("U_FILTER_BEGINS_S", subgroup.GetUri("s"));
            e.Template.Parse("U_FILTER_BEGINS_T", subgroup.GetUri("t"));
            e.Template.Parse("U_FILTER_BEGINS_U", subgroup.GetUri("u"));
            e.Template.Parse("U_FILTER_BEGINS_V", subgroup.GetUri("v"));
            e.Template.Parse("U_FILTER_BEGINS_W", subgroup.GetUri("w"));
            e.Template.Parse("U_FILTER_BEGINS_X", subgroup.GetUri("x"));
            e.Template.Parse("U_FILTER_BEGINS_Y", subgroup.GetUri("y"));
            e.Template.Parse("U_FILTER_BEGINS_Z", subgroup.GetUri("z"));

            List<SubGroupMember> awaitingApproval = subgroup.GetMembersWaitingApproval();

            foreach (SubGroupMember member in awaitingApproval)
            {
                VariableCollection approvalVariableCollection = e.Core.Template.CreateChild("approval_list");

                approvalVariableCollection.Parse("U_MEMBER", member.Uri);
                approvalVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                approvalVariableCollection.Parse("LOCATION", member.Profile.Country);
                approvalVariableCollection.Parse("JOINED", e.Core.Tz.DateTimeToDateString(member.GetJoinedDate(e.Core.Tz)));
            }

            if (awaitingApproval.Count > 0)
            {
                e.Core.Template.Parse("IS_WAITING_APPROVAL", "TRUE");
            }

            List<SubGroupMember> leaders = subgroup.GetLeaders();

            foreach (SubGroupMember member in leaders)
            {
                VariableCollection leaderVariableCollection = e.Core.Template.CreateChild("leader_list");

                leaderVariableCollection.Parse("U_MEMBER", member.Uri);
                leaderVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                leaderVariableCollection.Parse("LOCATION", member.Profile.Country);
                leaderVariableCollection.Parse("JOINED", e.Core.Tz.DateTimeToDateString(member.GetJoinedDate(e.Core.Tz)));
            }

            List<SubGroupMember> members = subgroup.GetMembers(e.Page.TopLevelPageNumber, 20, e.Core.Functions.GetFilter());
            long memberCount = e.Db.LastQueryRows;

            foreach (SubGroupMember member in members)
            {
                VariableCollection memberVariableCollection = e.Core.Template.CreateChild("member_list");

                memberVariableCollection.Parse("U_MEMBER", member.Uri);
                memberVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                memberVariableCollection.Parse("LOCATION", member.Profile.Country);
                memberVariableCollection.Parse("JOINED", e.Core.Tz.DateTimeToDateString(member.GetJoinedDate(e.Core.Tz)));


                /*if (string.IsNullOrEmpty(member.UserThumbnail))
                {
                    memberVariableCollection.Parse("I_DISPLAY_PIC", string.Empty);
                }
                else
                {
                    Image displayPic = new Image("display-pic[" + member.Id.ToString() + "]", member.UserThumbnail);
                    memberVariableCollection.Parse("I_DISPLAY_PIC", displayPic);
                }*/
            }

            if (e.Core.Session.IsLoggedIn)
            {
                if (subgroup.IsSubGroupMember(e.Core.Session.LoggedInMember))
                {
                    if (!subgroup.IsSubGroupLeader(e.Core.Session.LoggedInMember))
                    {
                        e.Template.Parse("U_LEAVE", subgroup.LeaveUri);
                    }
                }
                else
                {
                    if (subgroup.SubGroupType == "OPEN" || subgroup.SubGroupType == "REQUEST")
                    {
                        e.Template.Parse("U_JOIN", subgroup.JoinUri);
                    }
                }
            }

            e.Core.Display.ParsePagination(subgroup.GetUri(e.Core.Functions.GetFilter()), e.Page.TopLevelPageNumber, (int)(Math.Ceiling(memberCount / 20.0)));
        }

        public static List<PrimitivePermissionGroup> SubUserGroup_GetPrimitiveGroups(Core core, Primitive owner)
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            if (owner is UserGroup)
            {
                List<SubUserGroup> groups = ((UserGroup)owner).GetSubGroups();

                foreach (SubUserGroup group in groups)
                {
                    ppgs.Add(new PrimitivePermissionGroup(group.TypeId, group.Id, group.DisplayName));
                }
            }

            return ppgs;
        }

        public override string StoreFile(MemoryStream file)
        {
            return core.Storage.SaveFile("zinzam.user", file);
        }
    }

    public class InvalidSubGroupException : Exception
    {
    }

    public class GroupNameNotUniqueException : Exception
    {
    }
}
