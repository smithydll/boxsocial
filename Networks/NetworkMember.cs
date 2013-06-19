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
    [PseudoPrimitive]
    [DataTable("network_members")]
    [PermissionGroup]
    public class NetworkMember : User
    {
        [DataField("user_id")]
        private new long userId; // hide the parent variable to have it register in the table
        [DataField("network_id", typeof(Network))]
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

        public NetworkMember(Core core, long networkId, User user)
            : base(core)
        {
            this.userInfo = user.UserInfo;
            this.userProfile = user.Profile;
        }

        public NetworkMember(Core core, long networkId, long memberId)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("network_members.user_id", memberId);
            query.AddCondition("network_id", networkId);

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
            loadUserFromUser(core.PrimitiveCache[userId]);
        }

        public NetworkMember(Core core, Network theNetwork, User member)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", member.UserId);
            query.AddCondition("network_members.network_id", theNetwork.Id);

            DataTable memberTable = db.Query(query);

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

            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", member.UserId);
            query.AddCondition("network_members.member_active", true);

            DataTable userNetworks = core.Db.Query(query);

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
                //lastEmailId = (long)networkMemberTable.Rows[0]["user_id"];
                return false;
            }

            return true;
        }

        public static new SelectQuery GetSelectQueryStub(UserLoadOptions loadOptions)
        {
            SelectQuery query = GetSelectQueryStub(typeof(NetworkMember));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");
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
            /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }*/

            return query;
        }
    }
}
