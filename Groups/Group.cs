/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
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

    [DataTable("group_keys")]
    public class UserGroup : Primitive, ICommentableItem
    {
        public const string GROUP_INFO_FIELDS = "gi.group_id, gi.group_name, gi.group_name_display, gi.group_type, gi.group_abstract, gi.group_members, gi.group_officers, gi.group_operators, gi.group_reg_date_ut, gi.group_category, gi.group_comments, gi.group_gallery_items";

        [DataField("group_id", DataFieldKeys.Primary)]
        private long groupId;
        [DataField("group_name", DataFieldKeys.Unique, 64)]
        private string slug;

        private UserGroupInfo groupInfo;

        private Dictionary<User, bool> groupMemberCache = new Dictionary<User,bool>();
        private Dictionary<User, bool> groupMemberPendingCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupMemberBannedCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupMemberAbsoluteCache = new Dictionary<User, bool>();
        private Dictionary<User, bool> groupOperatorCache = new Dictionary<User, bool>();

        public long GroupId
        {
            get
            {
                return groupId;
            }
        }

        public UserGroupInfo Info
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
                return groupInfo.Comments;
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
                : this (core, groupName, UserGroupLoadOptions.Info | UserGroupLoadOptions.Icon)
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
        }

        private void loadUserGroupIcon(DataRow groupRow)
        {
        }

        public List<GroupMember> GetMembers(int page, int perPage)
        {
            List<GroupMember> members = new List<GroupMember>();

            SelectQuery query = new SelectQuery("group_members");
            query.AddFields(GroupMember.GetFieldsPrefixed(typeof(GroupMember)));
            query.AddField(new DataField("group_operators", "user_id", "user_id_go"));
            TableJoin tj = query.AddJoin(JoinTypes.Left, new DataField("group_members", "user_id"), new DataField("group_operators", "user_id"));
            tj.AddCondition(new DataField("group_members", "group_id"), new DataField("group_operators", "group_id"));
            query.AddCondition("`group_members`.`group_id`", groupId);
            query.AddCondition("group_member_approved", true);
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
                memberIds.Add((long)(int)dr["user_id"]);
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

        private void preLoadMemberCache(User member)
        {
            SelectQuery query = new SelectQuery("group_members");
            query.AddFields("user_id", "group_member_approved");
            query.AddCondition("group_id", groupId);
            query.AddCondition("user_id", member.UserId);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count > 0)
            {
                switch ((GroupMemberApproval)(byte)memberTable.Rows[0]["group_member_approved"])
                {
                    case GroupMemberApproval.Pending:
                        groupMemberCache.Add(member, false);
                        groupMemberPendingCache.Add(member, true);
                        groupMemberBannedCache.Add(member, false);
                        groupMemberAbsoluteCache.Add(member, true);
                        break;
                    case GroupMemberApproval.Member:
                        groupMemberCache.Add(member, true);
                        groupMemberPendingCache.Add(member, false);
                        groupMemberBannedCache.Add(member, false);
                        groupMemberAbsoluteCache.Add(member, true);
                        break;
                    case GroupMemberApproval.Banned:
                        groupMemberCache.Add(member, false);
                        groupMemberPendingCache.Add(member, false);
                        groupMemberBannedCache.Add(member, true);
                        groupMemberAbsoluteCache.Add(member, false);
                        break;
                }
            }
            else
            {
                groupMemberCache.Add(member, false);
                groupMemberPendingCache.Add(member, false);
                groupMemberBannedCache.Add(member, false);
                groupMemberAbsoluteCache.Add(member, false);
            }
        }

        public bool IsGroupMemberAbsolute(User member)
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

        public bool IsGroupMemberPending(User member)
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

        public bool IsGroupMember(User member)
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

        public bool IsGroupMemberBanned(User member)
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

        public bool IsGroupOperator(User member)
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
                        groupId, member.UserId));

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

        public static UserGroup Create(Core core, string groupTitle, string groupSlug, string groupDescription, short groupCategory, string groupType)
        {
            Mysql db = core.db;
            SessionState session = core.session;

            if (core.session.LoggedInMember == null)
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
            long groupId = db.UpdateQuery(string.Format("INSERT INTO group_keys (group_name) VALUES ('{0}')",
                Mysql.Escape(groupSlug)));

            /*
             * DONE: change zinzam.com DB to make group_id on group_info UNIQUE and not PRIMARY
             */
            db.UpdateQuery(string.Format("INSERT INTO group_info (group_id, group_name, group_name_display, group_type, group_reg_date_ut, group_category, group_reg_ip, group_abstract, group_operators, group_members) VALUES ({0}, '{1}', '{2}', '{3}', UNIX_TIMESTAMP(), {4}, '{5}', '{6}', 1, 1);",
                groupId, Mysql.Escape(groupSlug), Mysql.Escape(groupTitle), Mysql.Escape(groupType), groupCategory, Mysql.Escape(session.IPAddress.ToString()), Mysql.Escape(groupDescription)));

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
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                profileAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, null, "Groups");
                groupsAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, null, "Gallery");
                galleryAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                guestbookAe.Install(core, newGroup);
            }
            catch
            {
            }

            return newGroup;
        }

        public static bool CheckGroupNameUnique(Core core, string groupName)
        {
            if (core.db.Query(string.Format("SELECT group_name FROM group_keys WHERE LCASE(group_name) = '{0}';",
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
            disallowedNames.Add("zinzam");
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
            if (IsGroupOperator(member))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool IsCommentOwner(User member)
        {
            return false;
        }

        public override ushort GetAccessLevel(User viewer)
        {
            switch (GroupType)
            {
                case "OPEN":
                case "CLOSED":
                    return 0x0001;
                case "PRIVATE":
                    if (IsGroupMember(viewer))
                    {
                        return 0x0001;
                    }
                    break;
            }

            return 0x0000;
        }

        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            bool isGroupMember = IsGroupMember(viewer);
            bool isGroupOperator = IsGroupOperator(viewer);
            switch (GroupType)
            {
                case "OPEN":
                    if (isGroupOperator)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = true;
                    }
                    else if (isGroupMember)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = false;
                    }
                    else
                    {
                        canRead = true;
                        canComment = false;
                        canCreate = false;
                        canChange = false;
                    }
                    break;
                case "CLOSED":
                case "PRIVATE":
                    if (isGroupOperator)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = true;
                    }
                    else if (isGroupMember)
                    {
                        canRead = true;
                        canComment = true;
                        canCreate = true;
                        canChange = false;
                    }
                    else
                    {
                        canRead = false;
                        canComment = false;
                        canCreate = false;
                        canChange = false;
                    }
                    break;
                default:
                    canRead = false;
                    canComment = false;
                    canCreate = false;
                    canChange = false;
                    break;
            }
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/group/{0}", Slug);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    DisplayName, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    path += "/" + parts[i][0];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path);
                }
            }

            return output;
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(string.Format("/group/{0}",
                    Slug));
            }
        }

        public string MemberlistUri
        {
            get
            {
                return Linker.AppendSid(string.Format("/group/{0}/members",
                    Slug));
            }
        }

        public string EditUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "edit", string.Format("id={0}", GroupId));
            }
        }

        public string DeleteUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "groups", "delete", GroupId);
            }
        }

        public string JoinUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "memberships", "join", GroupId);
            }
        }

        public string LeaveUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "memberships", "leave", GroupId);
            }
        }

        public string InviteUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "invite", true, string.Format("id={0}", GroupId));
            }
        }

        public string ResignOperatorUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "resign-operator", true, string.Format("id={0}", GroupId));
            }
        }

        public static List<UserGroup> GetUserGroups(Core core, User member)
        {
            List<UserGroup> groups = new List<UserGroup>();

            DataTable groupsTable = core.db.Query(string.Format("SELECT {1} FROM group_members gm INNER JOIN group_keys gk ON gm.group_id = gk.group_id INNER JOIN group_info gi ON gk.group_id = gi.group_id WHERE gm.user_id = {0} ORDER BY group_name_display ASC;",
                member.UserId, UserGroup.GROUP_INFO_FIELDS));

            foreach (DataRow dr in groupsTable.Rows)
            {
                groups.Add(new UserGroup(core, dr, UserGroupLoadOptions.Common));
            }

            return groups;
        }

        public static void Show(Core core, GPage page)
        {
            page.template.SetTemplate("Groups", "viewgroup");

            page.template.Parse("U_GROUP", page.ThisGroup.Uri);
            page.template.Parse("GROUP_DISPLAY_NAME", page.ThisGroup.DisplayName);

            string langMembers = (page.ThisGroup.Members != 1) ? "members" : "member";
            string langIsAre = (page.ThisGroup.Members != 1) ? "are" : "is";

            //page.template.ParseRaw("DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(page.ThisGroup.Description), core.session.LoggedInMember));
            Display.ParseBbcode("DESCRIPTION", page.ThisGroup.Description);
            page.template.Parse("DATE_CREATED", core.tz.DateTimeToString(page.ThisGroup.DateCreated(core.tz)));
            page.template.Parse("CATEGORY", page.ThisGroup.Category);

            page.template.Parse("MEMBERS", page.ThisGroup.Members.ToString());
            page.template.Parse("OPERATORS", page.ThisGroup.Operators.ToString());
            page.template.Parse("OFFICERS", page.ThisGroup.Officers.ToString());
            page.template.Parse("L_MEMBERS", langMembers);
            page.template.Parse("L_IS_ARE", langIsAre);
            page.template.Parse("U_MEMBERLIST", page.ThisGroup.MemberlistUri);

            if (page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                page.template.Parse("IS_OPERATOR", "TRUE");
            }

            if (core.session.IsLoggedIn)
            {
                if (!page.ThisGroup.IsGroupMemberAbsolute(core.session.LoggedInMember))
                {
                    page.template.Parse("U_JOIN", page.ThisGroup.JoinUri);
                }
                else
                {
                    if (!page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
                    {
                        if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                        {
                            page.template.Parse("U_LEAVE", page.ThisGroup.LeaveUri);
                        }
                        else if (page.ThisGroup.IsGroupMemberPending(core.session.LoggedInMember))
                        {
                            page.template.Parse("U_CANCEL", page.ThisGroup.LeaveUri);
                        }
                    }

                    if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                    {
                        page.template.Parse("U_INVITE", page.ThisGroup.InviteUri);
                    }
                }
            }

            List<GroupMember> members = page.ThisGroup.GetMembers(1, 8);

            foreach (GroupMember member in members)
            {
                VariableCollection membersVariableCollection = page.template.CreateChild("member_list");

                membersVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                membersVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(member));
                membersVariableCollection.Parse("ICON", member.UserIcon);
            }

            List<GroupMember> operators = page.ThisGroup.GetOperators();

            foreach (GroupMember groupOperator in operators)
            {
                VariableCollection operatorsVariableCollection = page.template.CreateChild("operator_list");

                operatorsVariableCollection.Parse("USER_DISPLAY_NAME", groupOperator.DisplayName);
                operatorsVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(groupOperator));
                if (core.session.LoggedInMember != null)
                {
                    if (groupOperator.UserId == core.session.LoggedInMember.UserId)
                    {
                        operatorsVariableCollection.Parse("U_RESIGN", page.ThisGroup.ResignOperatorUri);
                    }
                }
            }

            List<GroupOfficer> officers = page.ThisGroup.GetOfficers();

            foreach (GroupOfficer groupOfficer in officers)
            {
                VariableCollection officersVariableCollection = page.template.CreateChild("officer_list");

                officersVariableCollection.Parse("USER_DISPLAY_NAME", groupOfficer.DisplayName);
                officersVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(groupOfficer));
                officersVariableCollection.Parse("OFFICER_TITLE", groupOfficer.OfficeTitle);
                officersVariableCollection.Parse("U_REMOVE", groupOfficer.BuildRemoveOfficerUri());
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Group, page.ThisGroup));
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            page.template.SetTemplate("Groups", "viewgroupmemberlist");

            int p = Functions.RequestInt("p", 1);

            page.template.Parse("MEMBERS_TITLE", "Member list for " + page.ThisGroup.DisplayName);
            page.template.Parse("MEMBERS", ((ulong)page.ThisGroup.Members).ToString());

            if (page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                page.template.Parse("GROUP_OPERATOR", "TRUE");

                SelectQuery query = GroupMember.GetSelectQueryStub(UserLoadOptions.All);
                query.AddCondition("group_members.group_id", page.ThisGroup.Id);
                query.AddCondition("group_member_approved", false);
                query.AddSort(SortOrder.Ascending, "group_member_date_ut");

                DataTable approvalTable = core.db.Query(query);

                if (approvalTable.Rows.Count > 0)
                {
                    page.template.Parse("IS_WAITING_APPROVAL", "TRUE");
                }

                for (int i = 0; i < approvalTable.Rows.Count; i++)
                {
                    GroupMember approvalMember = new GroupMember(core, approvalTable.Rows[i], UserLoadOptions.Profile);

                    VariableCollection approvalVariableCollection = page.template.CreateChild("approval_list");

                    approvalVariableCollection.Parse("USER_DISPLAY_NAME", approvalMember.DisplayName);
                    approvalVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(approvalMember));
                    approvalVariableCollection.Parse("U_APPROVE", approvalMember.ApproveMemberUri);
                }

            }

            List<GroupMember> members = page.ThisGroup.GetMembers(p, 18);
            foreach (GroupMember member in members)
            {
                VariableCollection memberVariableCollection = page.template.CreateChild("member_list");

                memberVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                memberVariableCollection.Parse("JOIN_DATE", page.tz.DateTimeToString(member.GetGroupMemberJoinDate(page.tz)));
                memberVariableCollection.Parse("USER_AGE", member.AgeString);
                memberVariableCollection.Parse("USER_COUNTRY", member.Country);
                memberVariableCollection.Parse("USER_CAPTION", "");

                memberVariableCollection.Parse("U_PROFILE", Linker.BuildProfileUri(member));
                if (!member.IsOperator)
                {
                    // let's say you can't ban an operator, show ban link if not an operator
                    memberVariableCollection.Parse("U_BAN", member.BanUri);
                    memberVariableCollection.Parse("U_MAKE_OPERATOR", member.MakeOperatorUri);
                }
                memberVariableCollection.Parse("U_MAKE_OFFICER", member.MakeOfficerUri);
                memberVariableCollection.Parse("ICON", member.UserIcon);
            }

            string pageUri = page.ThisGroup.MemberlistUri;
            Display.ParsePagination(pageUri, p, (int)Math.Ceiling(page.ThisGroup.Members / 18.0));
            page.ThisGroup.ParseBreadCrumbs("members");
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/group/{0}/account/",
                    Key);
            }
        }

        public override string Namespace
        {
            get
            {
                return Type;
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
    }

    public class InvalidGroupException : Exception
    {
    }
}
