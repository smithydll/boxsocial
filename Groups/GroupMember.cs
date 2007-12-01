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
    public class GroupMember : Member
    {
        public const string USER_GROUP_FIELDS = "gm.user_id, gm.group_id, gm.group_member_approved, gm.group_member_ip, gm.group_member_date_ut";

        private long groupId;
        private long memberJoinDateRaw;
        private bool memberApproved;
        private bool isOperator;

        public bool IsOperator
        {
            get
            {
                return isOperator;
            }
        }

        public DateTime GetGroupMemberJoinDate(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(memberJoinDateRaw);
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
            memberApproved = ((byte)memberRow["group_member_approved"] > 0) ? true : false;
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
                return ZzUri.AppendSid(string.Format("/account/?module=groups&sub=make-officer&id={0},{1}",
                    groupId, UserId), true);
            }
        }

        public string RemoveOfficerUri(string title)
        {

            return ZzUri.AppendSid(string.Format("/account/?module=groups&sub=remove-officer&id={0},{1},{2}",
                groupId, UserId, HttpUtility.UrlEncode(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(title)))), true);

        }

        public string MakeOperatorUri
        {
            get
            {
                return ZzUri.AppendSid(string.Format("/account/?module=groups&sub=make-operator&id={0},{1}",
                    groupId, UserId), true);
            }
        }

        public string ApproveMemberUri
        {
            get
            {
                return ZzUri.AppendSid(string.Format("/account/?module=groups&sub=approve&id={0},{1}",
                    groupId, UserId), true);
            }
        }
    }
}
