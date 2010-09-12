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
    [PseudoPrimitive]
    [DataTable("sub_group_members")]
    [PermissionGroup]
    public class SubGroupMember : User
    {
        [DataField("user_id")]
        private new long userId;
        [DataField("group_id")]
        private long groupId;
        [DataField("sub_group_id")]
        private long subGroupId;
        [DataField("sub_group_member_date_ut")]
        private long memberJoinDateRaw;
        [DataField("sub_group_member_ip", 50)]
        private string memberJoinIp;
        [DataField("sub_group_member_approved")]
        private byte memberApproval;
        [DataField("sub_group_member_is_leader")]
        private bool isGroupLeader;
        [DataField("sub_group_member_default")]
        private bool isDefaultGroup;

        public SubGroupMember(Core core, SubUserGroup group, long userId)
            : base(core)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("sub_group_members.sub_group_id", group.SubGroupId);

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

        public SubGroupMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(SubGroupMember), memberRow);
        }

        public SubGroupMember(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(typeof(SubGroupMember), memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.PrimitiveCache[userId]);
        }

        private void loadMemberInfo(DataRow memberRow)
        {
            groupId = (long)memberRow["group_id"];
            userId = (int)memberRow["user_id"];
            memberJoinDateRaw = (long)memberRow["sub_group_member_date_ut"];
            memberApproval = (byte)memberRow["sub_group_member_approved"];
            isGroupLeader = (bool)memberRow["sub_group_member_is_leader"];
            isDefaultGroup = (bool)memberRow["sub_group_member_default"];
        }
    }
}
