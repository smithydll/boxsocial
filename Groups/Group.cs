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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public class UserGroup : Primitive
    {
        public const string GROUP_INFO_FIELDS = "gi.group_id, gi.group_name, gi.group_name_display, gi.group_type, gi.group_abstract, gi.group_members, gi.group_officers, gi.group_operators, gi.group_reg_date_ut, gi.group_category, gi.group_comments, gi.group_gallery_items";

        private Mysql db;
        private long groupId;
        private string slug;
        private string displayName;
        private string displayNameOwnership;
        private string groupType;
        private string groupDescription;
        private long timestampCreated;
        private uint groupOperators;
        private uint groupOfficers;
        private ulong groupMembers;
        private short rawCategory;
        private string category;
        private ulong comments;
        private uint galleryItems;

        private Dictionary<Member, bool> groupMemberCache = new Dictionary<Member,bool>();
        private Dictionary<Member, bool> groupMemberPendingCache = new Dictionary<Member, bool>();
        private Dictionary<Member, bool> groupMemberBannedCache = new Dictionary<Member, bool>();
        private Dictionary<Member, bool> groupMemberAbsoluteCache = new Dictionary<Member, bool>();
        private Dictionary<Member, bool> groupOperatorCache = new Dictionary<Member, bool>();

        public long GroupId
        {
            get
            {
                return groupId;
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

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != "") ? displayName : slug;

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

        public string GroupType
        {
            get
            {
                return groupType;
            }
        }

        public string Description
        {
            get
            {
                return groupDescription;
            }
        }

        public ulong Members
        {
            get
            {
                return groupMembers;
            }
        }

        public uint Officers
        {
            get
            {
                return groupOfficers;
            }
        }

        public uint Operators
        {
            get
            {
                return groupOperators;
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
        }

        public short RawCategory
        {
            get
            {
                return rawCategory;
            }
        }

        public ulong Comments
        {
            get
            {
                return comments;
            }
        }

        public uint GalleryItems
        {
            get
            {
                return galleryItems;
            }
        }

        public DateTime DateCreated(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timestampCreated);
        }

        public UserGroup(Mysql db, long groupId)
        {
            this.db = db;

            DataTable groupTable = db.SelectQuery(string.Format("SELECT {1}, c.category_title FROM group_keys gk INNER JOIN group_info gi ON gk.group_id = gi.group_id INNER JOIN global_categories c ON gi.group_category = c.category_id WHERE gk.group_id = {0}",
                groupId, GROUP_INFO_FIELDS));

            if (groupTable.Rows.Count == 1)
            {
                loadGroupInfo(groupTable.Rows[0]);
                category = (string)groupTable.Rows[0]["category_title"];
            }
            else
            {
                throw new InvalidGroupException();
            }
        }

        public UserGroup(Mysql db, string groupSlug)
        {
            this.db = db;

            DataTable groupTable = db.SelectQuery(string.Format("SELECT {1}, c.category_title FROM group_keys gk INNER JOIN group_info gi ON gk.group_id = gi.group_id INNER JOIN global_categories c ON gi.group_category = c.category_id WHERE gk.group_name = '{0}'",
                Mysql.Escape(groupSlug), GROUP_INFO_FIELDS));

            if (groupTable.Rows.Count == 1)
            {
                loadGroupInfo(groupTable.Rows[0]);
                category = (string)groupTable.Rows[0]["category_title"];
            }
            else
            {
                throw new InvalidGroupException();
            }
        }

        public UserGroup(Mysql db, DataRow groupRow)
        {
            this.db = db;

            loadGroupInfo(groupRow);
        }

        private void loadGroupInfo(DataRow groupRow)
        {
            groupId = (long)groupRow["group_id"];
            slug = (string)groupRow["group_name"];
            displayName = (string)groupRow["group_name_display"];
            groupType = (string)groupRow["group_type"];
            if (!(groupRow["group_abstract"] is DBNull))
            {
                groupDescription = (string)groupRow["group_abstract"];
            }
            timestampCreated = (long)groupRow["group_reg_date_ut"];
            groupOperators = (uint)groupRow["group_operators"];
            groupOfficers = (uint)groupRow["group_officers"];
            groupMembers = (ulong)groupRow["group_members"];
            comments = (ulong)groupRow["group_comments"];
            rawCategory = (short)groupRow["group_category"];
            galleryItems = (uint)groupRow["group_gallery_items"];
        }

        public List<GroupMember> GetMembers(int page, int perPage)
        {
            List<GroupMember> members = new List<GroupMember>();

            DataTable membersTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3}, {4}, go.user_id AS user_id_go FROM group_members gm INNER JOIN user_info ui ON gm.user_id = ui.user_id INNER JOIN user_profile up ON gm.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id LEFT JOIN group_operators go ON ui.user_id = go.user_id AND gm.group_id = go.group_id WHERE gm.group_id = {0} AND gm.group_member_approved = 1 LIMIT {5}, {6};",
                groupId, Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS, GroupMember.USER_GROUP_FIELDS, (page - 1) * perPage, perPage));

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new GroupMember(db, dr, true, true, true));
            }

            return members;
        }

        public bool IsGroupInvitee(Member member)
        {
            DataTable inviteTable = db.SelectQuery(string.Format("SELECT user_id FROM group_invites WHERE group_id = {0} AND user_id = {1}",
                groupId, member.UserId));

            if (inviteTable.Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void preLoadMemberCache(Member member)
        {
            SelectQuery query = new SelectQuery("group_members");
            query.AddFields("user_id", "group_member_approved");
            query.AddCondition("group_id", groupId);
            query.AddCondition("user_id", member.UserId);

            DataTable memberTable = db.SelectQuery(query);

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

        public bool IsGroupMemberAbsolute(Member member)
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

        public bool IsGroupMemberPending(Member member)
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

        public bool IsGroupMember(Member member)
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

        public bool IsGroupMemberBanned(Member member)
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

        public bool IsGroupOperator(Member member)
        {
            if (member != null)
            {
                if (groupOperatorCache.ContainsKey(member))
                {
                    return groupOperatorCache[member];
                }
                else
                {
                    DataTable operatorTable = db.SelectQuery(string.Format("SELECT user_id FROM group_operators WHERE group_id = {0} AND user_id = {1}",
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

            if (!CheckGroupNameUnique(db, groupSlug))
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

            long groupId = db.UpdateQuery(string.Format("INSERT INTO group_keys (group_name) VALUES ('{0}')",
                Mysql.Escape(groupSlug)), true);

            /*
             * DONE: change zinzam.com DB to make group_id on group_info UNIQUE and not PRIMARY
             */
            db.UpdateQuery(string.Format("INSERT INTO group_info (group_id, group_name, group_name_display, group_type, group_reg_date_ut, group_category, group_reg_ip, group_abstract, group_operators, group_members) VALUES ({0}, '{1}', '{2}', '{3}', UNIX_TIMESTAMP(), {4}, '{5}', '{6}', 1, 1);",
                groupId, Mysql.Escape(groupSlug), Mysql.Escape(groupTitle), Mysql.Escape(groupType), groupCategory, Mysql.Escape(session.IPAddress.ToString()), Mysql.Escape(groupDescription)), true);

            if (groupType != "PRIVATE")
            {
                db.UpdateQuery(string.Format("UPDATE global_categories SET category_groups = category_groups + 1 WHERE category_id = {0}",
                    groupCategory), true);
            }

            db.UpdateQuery(string.Format("INSERT INTO group_members (user_id, group_id, group_member_approved, group_member_ip, group_member_date_ut) VALUES ({0}, {1}, 1, '{2}', UNIX_TIMESTAMP())",
                session.LoggedInMember.UserId, groupId, Mysql.Escape(session.IPAddress.ToString())), true);

            db.UpdateQuery(string.Format("INSERT INTO group_operators (user_id, group_id) VALUES ({0}, {1})",
                session.LoggedInMember.UserId, groupId), false);

            UserGroup newGroup = new UserGroup(db, groupId);

            // Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(db, null, "Profile");
                profileAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(db, null, "Groups");
                groupsAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(db, null, "Gallery");
                galleryAe.Install(core, newGroup);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(db, null, "GuestBook");
                guestbookAe.Install(core, newGroup);
            }
            catch
            {
            }

            return newGroup;
        }

        public static bool CheckGroupNameUnique(Mysql db, string groupName)
        {
            if (db.SelectQuery(string.Format("SELECT group_name FROM group_keys WHERE LCASE(group_name) = '{0}';",
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

        public override bool CanModerateComments(Member member)
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

        public override bool IsCommentOwner(Member member)
        {
            return false;
        }

        public override ushort GetAccessLevel(Member viewer)
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

        public override void GetCan(ushort accessBits, Member viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
                return ZzUri.AppendSid(string.Format("/group/{0}",
                    Slug));
            }
        }

        public string MemberlistUri
        {
            get
            {
                return ZzUri.AppendSid(string.Format("/group/{0}/members",
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
                return AccountModule.BuildModuleUri("groups", "delete", string.Format("id={0}", GroupId));
            }
        }

        public string JoinUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "join", true, string.Format("id={0}", GroupId));
            }
        }

        public string LeaveUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "leave", true, string.Format("id={0}", GroupId));
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

        public static List<UserGroup> GetUserGroups(Mysql db, Member member)
        {
            List<UserGroup> groups = new List<UserGroup>();

            DataTable groupsTable = db.SelectQuery(string.Format("SELECT {1} FROM group_members gm INNER JOIN group_keys gk ON gm.group_id = gk.group_id INNER JOIN group_info gi ON gk.group_id = gi.group_id WHERE gm.user_id = {0} ORDER BY group_name_display ASC;",
                member.UserId, UserGroup.GROUP_INFO_FIELDS));

            foreach (DataRow dr in groupsTable.Rows)
            {
                groups.Add(new UserGroup(db, dr));
            }

            return groups;
        }

        public static void Show(Core core, GPage page)
        {
            page.template.SetTemplate("Groups", "viewgroup");

            page.template.ParseVariables("U_GROUP", HttpUtility.HtmlEncode(page.ThisGroup.Uri));
            page.template.ParseVariables("GROUP_DISPLAY_NAME", HttpUtility.HtmlEncode(page.ThisGroup.DisplayName));

            string langMembers = (page.ThisGroup.Members != 1) ? "members" : "member";
            string langIsAre = (page.ThisGroup.Members != 1) ? "are" : "is";

            page.template.ParseVariables("DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(page.ThisGroup.Description), core.session.LoggedInMember));
            page.template.ParseVariables("DATE_CREATED", HttpUtility.HtmlEncode(core.tz.DateTimeToString(page.ThisGroup.DateCreated(core.tz))));
            page.template.ParseVariables("CATEGORY", HttpUtility.HtmlEncode(page.ThisGroup.Category));

            page.template.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(page.ThisGroup.Members.ToString()));
            page.template.ParseVariables("OPERATORS", HttpUtility.HtmlEncode(page.ThisGroup.Operators.ToString()));
            page.template.ParseVariables("OFFICERS", HttpUtility.HtmlEncode(page.ThisGroup.Officers.ToString()));
            page.template.ParseVariables("L_MEMBERS", HttpUtility.HtmlEncode(langMembers));
            page.template.ParseVariables("L_IS_ARE", HttpUtility.HtmlEncode(langIsAre));
            page.template.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(page.ThisGroup.MemberlistUri));

            if (page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                page.template.ParseVariables("IS_OPERATOR", "TRUE");
            }

            if (core.session.IsLoggedIn)
            {
                if (!page.ThisGroup.IsGroupMemberAbsolute(core.session.LoggedInMember))
                {
                    page.template.ParseVariables("U_JOIN", HttpUtility.HtmlEncode(page.ThisGroup.JoinUri));
                }
                else
                {
                    if (!page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
                    {
                        if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                        {
                            page.template.ParseVariables("U_LEAVE", HttpUtility.HtmlEncode(page.ThisGroup.LeaveUri));
                        }
                        else if (page.ThisGroup.IsGroupMemberPending(core.session.LoggedInMember))
                        {
                            page.template.ParseVariables("U_CANCEL", HttpUtility.HtmlEncode(page.ThisGroup.LeaveUri));
                        }
                    }

                    if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                    {
                        page.template.ParseVariables("U_INVITE", HttpUtility.HtmlEncode(page.ThisGroup.InviteUri));
                    }
                }
            }

            List<GroupMember> members = page.ThisGroup.GetMembers(1, 8);
            foreach (GroupMember member in members)
            {
                VariableCollection membersVariableCollection = page.template.CreateChild("member_list");

                membersVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(member.DisplayName));
                membersVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(member)));
                membersVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(member.UserIcon));
            }

            DataTable operatorsTable = core.db.SelectQuery(string.Format("SELECT {1} FROM group_operators go INNER JOIN user_info ui ON go.user_id = ui.user_id WHERE go.group_id = {0};",
                page.ThisGroup.GroupId, Member.USER_INFO_FIELDS));

            for (int i = 0; i < operatorsTable.Rows.Count; i++)
            {
                Member groupOperator = new Member(core.db, operatorsTable.Rows[i], false, false);
                string userDisplayName = (groupOperator.DisplayName != "") ? groupOperator.DisplayName : groupOperator.UserName;

                VariableCollection operatorsVariableCollection = page.template.CreateChild("operator_list");

                operatorsVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(userDisplayName));
                operatorsVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(groupOperator)));
                if (core.session.LoggedInMember != null)
                {
                    if (groupOperator.UserId == core.session.LoggedInMember.UserId)
                    {
                        operatorsVariableCollection.ParseVariables("U_RESIGN", HttpUtility.HtmlEncode(page.ThisGroup.ResignOperatorUri));
                    }
                }
            }

            DataTable officersTable = core.db.SelectQuery(string.Format("SELECT {1}, {2}, officer_title FROM group_officers go INNER JOIN group_members gm ON gm.user_id = go.user_id AND gm.group_id = go.group_id INNER JOIN user_info ui ON go.user_id = ui.user_id WHERE go.group_id = {0};",
                page.ThisGroup.GroupId, Member.USER_INFO_FIELDS, GroupMember.USER_GROUP_FIELDS));

            for (int i = 0; i < officersTable.Rows.Count; i++)
            {
                GroupMember groupOfficer = new GroupMember(core.db, officersTable.Rows[i], true, false, false);
                string userDisplayName = (groupOfficer.DisplayName != "") ? groupOfficer.DisplayName : groupOfficer.UserName;

                VariableCollection officersVariableCollection = page.template.CreateChild("officer_list");

                officersVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(userDisplayName));
                officersVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(groupOfficer)));
                officersVariableCollection.ParseVariables("OFFICER_TITLE", HttpUtility.HtmlEncode((string)officersTable.Rows[i]["officer_title"]));
                officersVariableCollection.ParseVariables("U_REMOVE", HttpUtility.HtmlEncode(groupOfficer.RemoveOfficerUri((string)officersTable.Rows[i]["officer_title"])));
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Group, page.ThisGroup));
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            page.template.SetTemplate("Groups", "viewgroupmemberlist");

            int p = Functions.RequestInt("p", 1);

            page.template.ParseVariables("MEMBERS_TITLE", HttpUtility.HtmlEncode("Member list for " + page.ThisGroup.DisplayName));
            page.template.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(((ulong)page.ThisGroup.Members).ToString()));

            if (page.ThisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                page.template.ParseVariables("GROUP_OPERATOR", "TRUE");

                DataTable approvalTable = core.db.SelectQuery(string.Format("SELECT {1}, {2}, {3}, {4} group_member_date_ut, go.user_id AS user_id_go FROM group_members gm INNER JOIN user_info ui ON gm.user_id = ui.user_id INNER JOIN user_profile up ON gm.user_id = up.user_id LEFT JOIN (countries c, gallery_items gi) ON (c.country_iso = up.profile_country AND gi.gallery_item_id = ui.user_icon) LEFT JOIN group_operators go ON ui.user_id = go.user_id AND gm.group_id = go.group_id WHERE gm.group_id = {0} AND gm.group_member_approved = 0 ORDER BY group_member_date_ut ASC",
                    page.ThisGroup.GroupId, Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS, GroupMember.USER_GROUP_FIELDS));

                if (approvalTable.Rows.Count > 0)
                {
                    page.template.ParseVariables("IS_WAITING_APPROVAL", "TRUE");
                }

                for (int i = 0; i < approvalTable.Rows.Count; i++)
                {
                    GroupMember approvalMember = new GroupMember(core.db, approvalTable.Rows[i], true, true, false);

                    VariableCollection approvalVariableCollection = page.template.CreateChild("approval_list");

                    approvalVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(approvalMember.DisplayName));
                    approvalVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(approvalMember)));
                    approvalVariableCollection.ParseVariables("U_APPROVE", HttpUtility.HtmlEncode(approvalMember.ApproveMemberUri));
                }

            }

            List<GroupMember> members = page.ThisGroup.GetMembers(p, 18);
            foreach (GroupMember member in members)
            {
                VariableCollection memberVariableCollection = page.template.CreateChild("member_list");

                memberVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(member.DisplayName));
                memberVariableCollection.ParseVariables("JOIN_DATE", HttpUtility.HtmlEncode(page.tz.DateTimeToString(member.GetGroupMemberJoinDate(page.tz))));
                memberVariableCollection.ParseVariables("USER_AGE", HttpUtility.HtmlEncode(member.GetAgeString()));
                memberVariableCollection.ParseVariables("USER_COUNTRY", HttpUtility.HtmlEncode(member.Country));
                memberVariableCollection.ParseVariables("USER_CAPTION", "");

                memberVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(member)));
                if (!member.IsOperator)
                {
                    // let's say you can't ban an operator, show ban link if not an operator
                    memberVariableCollection.ParseVariables("U_BAN", HttpUtility.HtmlEncode(member.BanUri));
                    memberVariableCollection.ParseVariables("U_MAKE_OPERATOR", HttpUtility.HtmlEncode(member.MakeOperatorUri));
                }
                memberVariableCollection.ParseVariables("U_MAKE_OFFICER", HttpUtility.HtmlEncode(member.MakeOfficerUri));
                memberVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(member.UserIcon));
            }

            string pageUri = page.ThisGroup.MemberlistUri;
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, p, (int)Math.Ceiling(page.ThisGroup.Members / 18.0)));
            page.template.ParseVariables("BREADCRUMBS", page.ThisGroup.GenerateBreadCrumbs("members"));
        }
    }

    public class InvalidGroupException : Exception
    {
    }
}
