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

namespace BoxSocial.Networks
{
    public class NetworkMember : Member
    {
        public const string USER_NETWORK_FIELDS = "nm.user_id, nm.network_id, nm.member_join_date_ut, nm.member_join_ip, nm.member_email, nm.member_active, nm.member_activate_code";

        private int networkId;
        private long memberJoinDateRaw;
        private string memberEmail;
        private bool memberActive;
        private string memberActivateCode;

        public int NetworkId
        {
            get
            {
                return networkId;
            }
        }

        public string MemberEmail
        {
            get
            {
                return memberEmail;
            }
        }

        public bool IsMemberActive
        {
            get
            {
                return memberActive;
            }
        }

        public string MemberActivationCode
        {
            get
            {
                return memberActivateCode;
            }
        }

        public DateTime GetNetworkMemberJoinDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(memberJoinDateRaw);
        }

        public NetworkMember(Mysql db, int networkId, int memberId)
        {
            this.db = db;

            SelectQuery query = new SelectQuery("network_members nm");
            query.AddFields(NetworkMember.USER_NETWORK_FIELDS, Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS);
            query.AddJoin(JoinTypes.Inner, "user_info ui", "nm.user_id", "ui.user_id");
            query.AddJoin(JoinTypes.Inner, "user_profile up", "nm.user_id", "up.user_id");
            query.AddJoin(JoinTypes.Left, "countries c", "up.profile_country", "c.country_iso");
            query.AddJoin(JoinTypes.Left, "gallery_items gi", "ui.user_icon", "gi.gallery_item_id");
            query.AddCondition("nm.user_id", memberId);
            query.AddCondition("nm.network_id", networkId);

            /*DataTable memberTable = db.Query(string.Format("SELECT {2}, {3}, {4}, {5} FROM network_members nm INNER JOIN user_info ui ON nm.user_id = ui.user_id INNER JOIN user_profile up ON nm.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE nm.user_id = {0} AND nm.network_id = {1}",
                memberId, networkId, USER_INFO_FIELDS, USER_PROFILE_FIELDS, USER_ICON_FIELDS, USER_NETWORK_FIELDS));*/

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                loadMemberInfo(memberTable.Rows[0]);
                loadUserInfo(memberTable.Rows[0]);
                loadUserProfile(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);
            }
            else
            {
                throw new Exception("Invalid User Exception");
            }
        }

        public NetworkMember(Mysql db, DataRow memberRow)
            : this (db, memberRow, false, false, false)
        {
        }

        public NetworkMember(Mysql db, DataRow memberRow, bool containsUserInfo)
            : this(db, memberRow, containsUserInfo, false, false)
        {
        }

        public NetworkMember(Mysql db, DataRow memberRow, bool containsUserInfo, bool containsUserProfile)
            : this(db, memberRow, containsUserInfo, containsUserProfile, false)
        {
        }

        public NetworkMember(Mysql db, DataRow memberRow, bool containsUserInfo, bool containsUserProfile, bool containsUserIcon)
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

        public NetworkMember(Mysql db, Network theNetwork, Member member)
        {
            DataTable memberTable = db.Query(string.Format("SELECT {2} FROM network_members nm WHERE nm.user_id = {0} AND nm.network_id = {1}",
                member.UserId, theNetwork.NetworkId, USER_NETWORK_FIELDS));

            if (memberTable.Rows.Count == 1)
            {
                loadMemberInfo(memberTable.Rows[0]);
            }
            else
            {
                throw new Exception("Invalid User Exception");
            }

            loadUserFromUser(member);
        }

        private void loadMemberInfo(DataRow memberRow)
        {
            networkId = (int)memberRow["network_id"];
            userId = (int)memberRow["user_id"];
            memberJoinDateRaw = (long)memberRow["member_join_date_ut"];
            memberEmail = (string)memberRow["member_email"];
            memberActive = ((byte)memberRow["member_active"] > 0) ? true : false;
            memberActivateCode = (string)memberRow["member_activate_code"];
        }

        /// <summary>
        /// {networkId, NetworkMember}
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Dictionary<int, NetworkMember> GetUserNetworks(Mysql db, Member member)
        {
            Dictionary<int, NetworkMember> networks = new Dictionary<int, NetworkMember>();

            DataTable userNetworks = db.Query(string.Format("SELECT {1} FROM network_members nm WHERE user_id = {0} AND member_active = 1;",
                member.UserId, NetworkMember.USER_NETWORK_FIELDS));

            foreach (DataRow memberRow in userNetworks.Rows)
            {
                networks.Add((int)memberRow["network_id"], new NetworkMember(db, memberRow));
            }

            return networks;
        }

        public static bool CheckNetworkEmailUnique(Mysql db, string eMail)
        {
            DataTable networkMemberTable = db.Query(string.Format("SELECT user_id, member_email FROM network_members WHERE LCASE(member_email) = '{0}';",
                Mysql.Escape(eMail.ToLower())));
            if (networkMemberTable.Rows.Count > 0)
            {
                lastEmailId = (int)networkMemberTable.Rows[0]["user_id"];
                return false;
            }

            return true;
        }
    }
}
