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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public enum UserSubGroupLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Common = Key | Info,
        All = Key | Info,
    }

    [DataTable("sub_group_keys", "SUBGROUP")]
    public class SubUserGroup : Primitive
    {
        [DataField("sub_group_id", DataFieldKeys.Primary)]
        private long subGroupId;
        [DataField("sub_group_parent_id")]
        private long parentId;
        [DataField("sub_group_name", DataFieldKeys.Unique, 64)]
        private string slug;
        [DataField("sub_group_name_display", 64)]
        private string displayName;
        [DataField("sub_group_type", 15)]
        private string subGroupType;
        [DataField("sub_group_colour")]
        private uint colour;
        [DataField("sub_group_reg_date_ut")]
        private long timestampCreated;
        [DataField("sub_group_reg_ip", 50)]
        private string registrationIp;
        [DataField("sub_group_members")]
        private long memberCount;
        [DataField("sub_group_abstract", MYSQL_TEXT)]
        private string description;

        private string displayNameOwnership = null;
        private UserGroup parent;

        public long SubGroupId
        {
            get
            {
                return Id;
            }
        }

        public override long Id
        {
            get
            {
                return subGroupId;
            }
        }

        public override string Key
        {
            get
            {
                return slug;
            }
        }

        public override string Type
        {
            get
            {
                return "SUBGROUP";
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public UserGroup Parent
        {
            get
            {
                if (parent != null || parent.Id != parentId)
                {
                    ItemKey key = new ItemKey(parentId, ItemType.GetTypeId(typeof(UserGroup)));
                    core.PrimitiveCache.LoadPrimitiveProfile(key);
                    parent = (UserGroup)core.PrimitiveCache[key];
                }
                return parent;
            }
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("{0}account/",
                    UriStub, Key);
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.SubGroup;
            }
        }

        public override string UriStub
        {
            get
            {
                return Parent.UriStub + "groups/" + slug + "/";
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return Parent.UriStubAbsolute + "groups/" + slug + "/";
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(UriStub);
            }
        }

        public override string TitleName
        {
            get
            {
                return "the group " + DisplayName;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the group " + DisplayNameOwnership;
            }
        }

        public override string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != string.Empty) ? displayName : displayName;

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

        public override bool CanModerateComments(User member)
        {
            throw new NotImplementedException();
        }

        public override bool IsCommentOwner(User member)
        {
            throw new NotImplementedException();
        }

        public override ushort GetAccessLevel(User viewer)
        {
            throw new NotImplementedException();
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            throw new NotImplementedException();
        }

        public override Access Access
        {
            get { throw new NotImplementedException(); }
        }

        public override List<AccessControlPermission> AclPermissions
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsItemGroupMember(User viewer, ItemKey key)
        {
            throw new NotImplementedException();
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            throw new NotImplementedException();
        }

        public override bool GetIsMemberOfPrimitive(User viewer, ItemKey primitiveKey)
        {
            throw new NotImplementedException();
        }

        public override bool CanEditPermissions()
        {
            throw new NotImplementedException();
        }

        public override bool CanEditItem()
        {
            throw new NotImplementedException();
        }

        public override bool CanDeleteItem()
        {
            throw new NotImplementedException();
        }

        public override bool GetDefaultCan(string permission)
        {
            throw new NotImplementedException();
        }

        public override string DisplayTitle
        {
            get { throw new NotImplementedException(); }
        }

        public SubUserGroup(Core core, long groupId)
            : this(core, groupId, UserSubGroupLoadOptions.Info)
        {
        }

        public SubUserGroup(Core core, long groupId, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                LoadItem(groupId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        public SubUserGroup(Core core, string groupName)
            : this(core, groupName, UserSubGroupLoadOptions.Info)
        {
        }

        public SubUserGroup(Core core, string groupName, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                LoadItem("sub_group_name", groupName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        public SubUserGroup(Core core, DataRow groupRow, UserSubGroupLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SubUserGroup_ItemLoad);

            try
            {
                loadItemInfo(groupRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSubGroupException();
            }
        }

        void SubUserGroup_ItemLoad()
        {
            throw new NotImplementedException();
        }

        public static SubUserGroup Create(Core core, UserGroup parent, string groupTitle, string groupSlug, string groupDescription, string groupType)
        {
            Mysql db = core.Db;
            SessionState session = core.Session;

            if (core.Session.LoggedInMember == null)
            {
                return null;
            }

            if (!parent.CheckSubGroupNameUnique(groupSlug))
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

            Item item = Item.Create(core, typeof(SubUserGroup), new FieldValuePair("sub_group_parent_id", parent.Id),
                new FieldValuePair("sub_group_name", groupSlug),
                new FieldValuePair("sub_group_name_display", groupTitle),
                new FieldValuePair("sub_group_type", groupType),
                new FieldValuePair("sub_group_reg_ip", core.Session.IPAddress.ToString()),
                new FieldValuePair("sub_group_reg_date_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("sub_group_colour", 0x000000),
                new FieldValuePair("sub_group_members", 0));

            return (SubUserGroup)item;
        }

        public static void Show(object sender, ShowPageEventArgs e)
        {
            e.Page.template.SetTemplate("Groups", "viewsubgroup");

            SubUserGroup subgroup = new SubUserGroup(e.Core, e.Core.PagePathParts[1].Value);
        }
    }

    public class InvalidSubGroupException : Exception
    {
    }
}
