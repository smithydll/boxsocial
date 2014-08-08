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

namespace BoxSocial.Groups
{

    public enum GroupMemberApproval : byte
    {
        Pending = 0,
        Member = 1,
        Banned = 2,
    }

    [PseudoPrimitive]
    [DataTable("group_members")]
    [PermissionGroup]
    public class GroupMember : User
    {
        [DataFieldKey(DataFieldKeys.Unique, "u_member")]
        [DataField("user_id", DataFieldKeys.Index)]
        private new long userId;
        [DataFieldKey(DataFieldKeys.Unique, "u_member")]
        [DataField("group_id")]
        private long groupId;
        [DataField("group_member_date_ut")]
        private long memberJoinDateRaw;
        [DataField("group_member_ip", 50)]
        private string memberJoinIp;
        [DataField("group_member_approved")]
        private byte memberApproval;
        [DataField("group_member_colour", 6)]
        private string memberColour;
        [DataField("group_default_subgroup")]
        private long memberDefaultSubGroup;

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

        public GroupMember(Core core, UserGroup group, long userId)
            : base(core)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(core, UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("group_members.group_id", group.GroupId);

            System.Data.Common.DbDataReader memberReader = db.ReaderQuery(query);

            if (memberReader.HasRows)
            {
                memberReader.Read();

                loadItemInfo(memberReader);
                loadUserInfo(memberReader);
                loadUserIcon(memberReader);

                memberReader.Close();
                memberReader.Dispose();
            }
            else
            {
                memberReader.Close();
                memberReader.Dispose();

                throw new InvalidUserException();
            }
        }

        public GroupMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(memberRow);

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

        public GroupMember(Core core, System.Data.Common.DbDataReader memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(memberRow);
        }

        public GroupMember(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.PrimitiveCache[userId]);

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

        public GroupMember(Core core, System.Data.Common.DbDataReader memberRow)
            : base(core)
        {
            loadItemInfo(memberRow);
            loadUser(memberRow);
            //core.LoadUserProfile(userId);
            //loadUserFromUser(core.PrimitiveCache[userId]);
            core.ItemCache.RequestItem(new ItemKey(Id, ItemType.GetTypeId(core, typeof(UserInfo))));
        }

        protected override void loadItemInfo(DataRow userRow)
        {
            loadGroupMember(userRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader userRow)
        {
            loadGroupMember(userRow);
        }

        protected void loadGroupMember(DataRow memberRow)
        {
            try
            {
                loadValue(memberRow, "user_id", out userId);
                loadValue(memberRow, "group_id", out groupId);
                loadValue(memberRow, "group_member_date_ut", out memberJoinDateRaw);
                loadValue(memberRow, "group_member_ip", out memberJoinIp);
                loadValue(memberRow, "group_member_approved", out memberApproval);
                loadValue(memberRow, "group_member_colour", out memberColour);
                loadValue(memberRow, "group_default_subgroup", out memberDefaultSubGroup);

                itemLoaded(memberRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        protected void loadGroupMember(System.Data.Common.DbDataReader memberRow)
        {
            //try
            {
                loadValue(memberRow, "user_id", out userId);
                loadValue(memberRow, "group_id", out groupId);
                loadValue(memberRow, "group_member_date_ut", out memberJoinDateRaw);
                loadValue(memberRow, "group_member_ip", out memberJoinIp);
                loadValue(memberRow, "group_member_approved", out memberApproval);
                loadValue(memberRow, "group_member_colour", out memberColour);
                loadValue(memberRow, "group_default_subgroup", out memberDefaultSubGroup);

                for (int i = 0; i < memberRow.FieldCount; i++)
                {
                    if (memberRow.GetName(i) == "user_id_go")
                    {
                        isOperator = true;
                        break;
                    }
                }

                itemLoaded(memberRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            /*catch
            {
                throw new InvalidItemException();
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
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", true, "mode=make-officer", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string RemoveOfficerUri(string title)
        {
            return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", true,
                "mode=remove-officer", string.Format("id={0},{1},{2}", groupId, userId, HttpUtility.UrlEncode(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(title)))));
        }

        public string MakeOperatorUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", true, "mode=make-operator", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string BanUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", true, "mode=ban-member", string.Format("id={0},{1}", groupId, userId));
            }
        }

        public string ApproveMemberUri
        {
            get
            {
                return core.Hyperlink.BuildAccountSubModuleUri("groups", "memberships", true, "mode=approve", string.Format("id={0},{1}", groupId, userId));
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

        public static new SelectQuery GetSelectQueryStub(Core core, UserLoadOptions loadOptions)
        {
            SelectQuery query = GetSelectQueryStub(core, typeof(GroupMember));
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddField(new DataField("group_operators", "user_id", "user_id_go"));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");
            TableJoin tj1 = query.AddJoin(JoinTypes.Left, "group_operators", "user_id", "user_id");
            tj1.AddCondition("group_operators.group_id", new DataField(GroupMember.GetTable(typeof(GroupMember)), "group_id"));
            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
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
