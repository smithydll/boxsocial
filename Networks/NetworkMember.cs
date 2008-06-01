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
    [DataTable("network_members")]
    public class NetworkMember : User
    {
        public const string USER_NETWORK_FIELDS = "nm.user_id, nm.network_id, nm.member_join_date_ut, nm.member_join_ip, nm.member_email, nm.member_active, nm.member_activate_code";

        [DataField("user_id")]
        private new long userId; // hide the parent variable to have it register in the table
        [DataField("network_id")]
        private long networkId;
        [DataField("member_join_date_ut")]
        private long memberJoinDateRaw;
        [DataField("member_email", 255)]
        private string memberEmail;
        [DataField("member_active")]
        private bool memberActive;
        [DataField("member_activate_code", 64)]
        private string memberActivateCode;
        [DataField("member_join_ip", 50)]
        private string memberJoinIpRaw;

        public long NetworkId
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

        public NetworkMember(Core core, int networkId, User user)
            : base(core)
        {
            this.userInfo = user.Info;
            this.userProfile = user.Profile;
        }

        public NetworkMember(Core core, int networkId, int memberId)
            : base(core)
        {
            SelectQuery query = new SelectQuery("network_members nm");
            query.AddFields(NetworkMember.USER_NETWORK_FIELDS, User.USER_INFO_FIELDS, User.USER_PROFILE_FIELDS, User.USER_ICON_FIELDS);
            query.AddJoin(JoinTypes.Inner, "user_info ui", "nm.user_id", "ui.user_id");
            query.AddJoin(JoinTypes.Inner, "user_profile up", "nm.user_id", "up.user_id");
            query.AddJoin(JoinTypes.Left, "countries c", "up.profile_country", "c.country_iso");
            query.AddJoin(JoinTypes.Left, "gallery_items gi", "ui.user_icon", "gi.gallery_item_id");
            query.AddCondition("nm.user_id", memberId);
            query.AddCondition("nm.network_id", networkId);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(memberTable.Rows[0]);
                loadUserInfo(memberTable.Rows[0]);
                loadUserProfile(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public NetworkMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(memberRow);
        }

        public NetworkMember(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(memberRow);
            loadUserFromUser(core.UserProfiles[userId]);
        }

        public NetworkMember(Core core, Network theNetwork, User member)
            : base(core)
        {
            DataTable memberTable = db.Query(string.Format("SELECT {2} FROM network_members nm WHERE nm.user_id = {0} AND nm.network_id = {1}",
                member.UserId, theNetwork.NetworkId, USER_NETWORK_FIELDS));

            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(memberTable.Rows[0]);
            }
            else
            {
                throw new InvalidUserException();
            }

            loadUserFromUser(member);
        }

        /// <summary>
        /// {networkId, NetworkMember}
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Dictionary<int, NetworkMember> GetUserNetworks(Core core, User member)
        {
            Dictionary<int, NetworkMember> networks = new Dictionary<int, NetworkMember>();

            DataTable userNetworks = core.db.Query(string.Format("SELECT {1} FROM network_members nm WHERE user_id = {0} AND member_active = 1;",
                member.UserId, NetworkMember.USER_NETWORK_FIELDS));

            foreach (DataRow memberRow in userNetworks.Rows)
            {
                networks.Add((int)memberRow["network_id"], new NetworkMember(core, memberRow, UserLoadOptions.Key));
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
