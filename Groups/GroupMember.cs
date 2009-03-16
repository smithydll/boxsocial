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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{

    public enum GroupMemberApproval : byte
    {
        Pending = 0,
        Member = 1,
        Banned = 2,
    }

    [DataTable("group_members")]
    public class GroupMember : User
    {
        public const string USER_GROUP_FIELDS = "gm.user_id, gm.group_id, gm.group_member_approved, gm.group_member_ip, gm.group_member_date_ut";

        [DataField("user_id")]
        private new long userId;
        [DataField("group_id")]
        private long groupId;
        [DataField("group_member_date_ut")]
        private long memberJoinDateRaw;
        [DataField("group_member_approved")]
        private byte memberApproval;

        private bool isOperator;

        public bool IsOperator
        {
            get
            {
                return isOperator;
            }
        }

        public DateTime GetGroupMemberJoinDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(memberJoinDateRaw);
        }

        public GroupMember(Core core, UserGroup group, long userId) : base(core)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("group_members.group_id", group.GroupId);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                loadMemberInfo(memberTable.Rows[0]);
                loadUserInfo(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public GroupMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(GroupMember), memberRow);

            /*try
            {*/
            if (memberRow != null && memberRow.Table.Columns.Contains("user_id_go"))
            {
                if (!(memberRow["user_id_go"] is DBNull))
                {
                    isOperator = true;
                }
            }
            /*}
            catch
            {
                // TODO: is there a better way?
                //isOperator = false;
            }*/
        }

        public GroupMember(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(typeof(GroupMember), memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.UserProfiles[userId]);

            /*try
            {*/
            if (memberRow != null && memberRow.Table.Columns.Contains("user_id_go"))
            {
                if (!(memberRow["user_id_go"] is DBNull))
                {
                    isOperator = true;
                }
            }
            /*}
            catch
            {
                // TODO: is there a better way?
                //isOperator = false;
            }*/
        }

        private void loadMemberInfo(DataRow memberRow)
        {
            groupId = (long)memberRow["group_id"];
            userId = (int)memberRow["user_id"];
            memberJoinDateRaw = (long)memberRow["group_member_date_ut"];
            memberApproval = (byte)memberRow["group_member_approved"];
            try
            {
                if (memberRow["user_id_go"] is DBNull)
                {
                    isOperator = false;
                }
                else
                {
                    isOperator = true;
                }
            }
            catch
            {
                // TODO: is there a better way?
                isOperator = false;
            }
        }

        public string MakeOfficerUri
        {
            get
            {
                return Linker.BuildAccountSubModuleUri("groups", "memberships", true, "mode=make-officer", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string RemoveOfficerUri(string title)
        {
            return Linker.BuildAccountSubModuleUri("groups", "memberships", true,
                "mode=remove-officer", string.Format("id={0},{1},{2}", groupId, userId, HttpUtility.UrlEncode(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(title)))));
        }

        public string MakeOperatorUri
        {
            get
            {
                return Linker.BuildAccountSubModuleUri("groups", "memberships", true, "mode=make-operator", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string BanUri
        {
            get
            {
                return Linker.BuildAccountSubModuleUri("groups", "memberships", true, "mode=ban-member", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string ApproveMemberUri
        {
            get
            {
                return Linker.BuildAccountSubModuleUri("groups", "memberships", true, "mode=approve", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public void Ban()
        {
            UpdateQuery query = new UpdateQuery("group_members");
            query.AddField("group_member_approved", (byte)GroupMemberApproval.Banned);
            query.AddCondition("user_id", userId);
            query.AddCondition("group_id", groupId);

            db.Query(query);
        }

        public void UnBan()
        {
            DeleteQuery query = new DeleteQuery("group_members");
            query.AddCondition("user_id", userId);
            query.AddCondition("group_id", groupId);

            db.Query(query);
        }

        public static new SelectQuery GetSelectQueryStub(UserLoadOptions loadOptions)
        {
            SelectQuery query = GetSelectQueryStub(typeof(GroupMember));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddField(new DataField("group_operators", "user_id", "user_id_go"));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");
            TableJoin tj1 = query.AddJoin(JoinTypes.Left, "group_operators", "user_id", "user_id");
            tj1.AddCondition("group_operators.group_id", new DataField(GroupMember.GetTable(typeof(GroupMember)), "group_id"));
            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            }
            if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }

            return query;
        }
    }
}
