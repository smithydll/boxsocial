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

    public class GroupMember : Member
    {
        public const string USER_GROUP_FIELDS = "gm.user_id, gm.group_id, gm.group_member_approved, gm.group_member_ip, gm.group_member_date_ut";

        private long groupId;
        private long memberJoinDateRaw;
        private GroupMemberApproval memberApproval;
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

        public GroupMember(Mysql db, UserGroup group, long userId)
        {
            this.db = db;

            /*SelectQuery query = new SelectQuery("group_members gm");
            query.AddFields(Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS, GroupMember.USER_GROUP_FIELDS, "go.user_id AS user_id_go");
            query.joins.Add(new TableJoin(JoinTypes.Inner, "user_info ui", "gm.user_id", "ui.user_id"));
            query.joins.Add(new TableJoin(JoinTypes.Left, "gallery_items gi", "ui.user_icon", "gi.gallery_item_id"));
            query.joins.Add(new TableJoin(JoinTypes.Left, "group_operators go", "ui.user_id", "go.user_id"));*/

            DataTable memberTable = db.Query(string.Format("SELECT {2}, {3}, {4}, go.user_id AS user_id_go FROM group_members gm INNER JOIN user_info ui ON gm.user_id = ui.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id LEFT JOIN group_operators go ON ui.user_id = go.user_id AND gm.group_id = go.group_id WHERE gm.group_id = {0} AND gm.user_id = {1};",
                group.GroupId, userId, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS, GroupMember.USER_GROUP_FIELDS));

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

        public GroupMember(Mysql db, DataRow memberRow, bool containsUserInfo, bool containsUserProfile, bool containsUserIcon)
        {
            this.db = db;
            loadMemberInfo(memberRow);

            if (containsUserInfo)
            {
                loadUserInfo(memberRow);
            }

            if (containsUserProfile)
            {
                loadUserProfile(memberRow);
            }

            if (containsUserIcon)
            {
                loadUserIcon(memberRow);
            }
        }

        private void loadMemberInfo(DataRow memberRow)
        {
            groupId = (long)memberRow["group_id"];
            userId = (int)memberRow["user_id"];
            memberJoinDateRaw = (long)memberRow["group_member_date_ut"];
            memberApproval = (GroupMemberApproval)(byte)memberRow["group_member_approved"];
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
                return AccountModule.BuildModuleUri("groups", "make-officer", true, string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string RemoveOfficerUri(string title)
        {
            return AccountModule.BuildModuleUri("groups", "remove-officer", true,
                string.Format("id={0},{1},{2}", groupId, userId, HttpUtility.UrlEncode(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(title)))));
        }

        public string MakeOperatorUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "make-operator", true, string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string BanUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "ban-member", true, string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string ApproveMemberUri
        {
            get
            {
                return AccountModule.BuildModuleUri("groups", "approve", true, string.Format("id={0},{1}", groupId, userId));
            }
        }

        public void Ban()
        {
            UpdateQuery query = new UpdateQuery("group_members");
            query.AddField("group_member_approved", (byte)GroupMemberApproval.Banned);
            query.AddCondition("user_id", userId);
            query.AddCondition("group_id", groupId);

            db.UpdateQuery(query);
        }

        public void UnBan()
        {
            DeleteQuery query = new DeleteQuery("group_members");
            query.AddCondition("user_id", userId);
            query.AddCondition("group_id", groupId);

            db.UpdateQuery(query);
        }
    }
}
