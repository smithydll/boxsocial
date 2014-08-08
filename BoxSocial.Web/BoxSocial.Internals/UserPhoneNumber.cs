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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum PhoneNumberTypes : byte
    {
        Home = 0x01,
        Mobile = 0x02,
        VoIP = 0x04,
        Business = 0x11,
        BusinessMobile = 0x12,
        Fax = 0x15,
        Other = 0xFF,
    }

    [DataTable("user_phone")]
    [Permission("VIEW", "Can view the phone number", PermissionTypes.View)]
    public class UserPhoneNumber : NumberedItem, IPermissibleItem
    {
        [DataField("phone_id", DataFieldKeys.Primary)]
        private long phoneId;
        [DataField("phone_user_id", typeof(User))]
        private long userId;
        [DataField("phone_number", 15)]
        private string phoneNumber;
        [DataField("phone_type")]
        private byte phoneType;
        [DataField("phone_validated")]
        private bool phoneValidated;
        [DataField("phone_time_ut")]
        private long phoneTimeRaw;
        [DataField("phone_validated_time_ut")]
        private long phoneValidatedTime;
        [DataField("phone_activate_code", 15)]
        private string phoneActivateKey;
        [DataField("phone_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access access;

        public long PhoneId
        {
            get
            {
                return phoneId;
            }
        }

        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }
            set
            {
                SetProperty("phoneNumber", value);
            }
        }

        public PhoneNumberTypes PhoneType
        {
            get
            {
                return (PhoneNumberTypes)phoneType;
            }
            set
            {
                SetProperty("phoneType", (byte)value);
            }
        }

        public bool Validated
        {
            get
            {
                return phoneValidated;
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public User Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.PrimitiveCache[userId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public UserPhoneNumber(Core core, long phoneId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserPhoneNumber_ItemLoad);

            try
            {
                LoadItem(phoneId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserPhoneNumberException();
            }
        }

        public UserPhoneNumber(Core core, DataRow phoneRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserPhoneNumber_ItemLoad);

            try
            {
                loadItemInfo(phoneRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserPhoneNumberException();
            }
        }

        public UserPhoneNumber(Core core, System.Data.Common.DbDataReader phoneRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserPhoneNumber_ItemLoad);

            try
            {
                loadItemInfo(phoneRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserPhoneNumberException();
            }
        }

        protected override void loadItemInfo(DataRow phoneRow)
        {
            loadValue(phoneRow, "phone_id", out phoneId);
            loadValue(phoneRow, "phone_user_id", out userId);
            loadValue(phoneRow, "phone_number", out phoneNumber);
            loadValue(phoneRow, "phone_type", out phoneType);
            loadValue(phoneRow, "phone_validated", out phoneValidated);
            loadValue(phoneRow, "phone_validated_time_ut", out phoneValidatedTime);
            loadValue(phoneRow, "phone_activate_code", out phoneActivateKey);
            loadValue(phoneRow, "phone_simple_permissions", out simplePermissions);

            itemLoaded(phoneRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader phoneRow)
        {
            loadValue(phoneRow, "phone_id", out phoneId);
            loadValue(phoneRow, "phone_user_id", out userId);
            loadValue(phoneRow, "phone_number", out phoneNumber);
            loadValue(phoneRow, "phone_type", out phoneType);
            loadValue(phoneRow, "phone_validated", out phoneValidated);
            loadValue(phoneRow, "phone_validated_time_ut", out phoneValidatedTime);
            loadValue(phoneRow, "phone_activate_code", out phoneActivateKey);
            loadValue(phoneRow, "phone_simple_permissions", out simplePermissions);

            itemLoaded(phoneRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void UserPhoneNumber_ItemLoad()
        {
        }

        public static UserPhoneNumber Create(Core core, string phoneNumber, PhoneNumberTypes phoneType)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iquery = new InsertQuery(UserPhoneNumber.GetTable(typeof(UserPhoneNumber)));
            iquery.AddField("phone_user_id", core.Session.LoggedInMember.Id);
            iquery.AddField("phone_number", phoneNumber);
            iquery.AddField("phone_type", (byte)phoneType);
            iquery.AddField("phone_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("phone_simple_permissions", true);

            long phoneId = core.Db.Query(iquery);

            UserPhoneNumber newPhoneNumber = new UserPhoneNumber(core, phoneId);

            Access.CreateGrantForPrimitive(core, newPhoneNumber, User.GetCreatorKey(core), "VIEW");
            Access.CreateGrantForPrimitive(core, newPhoneNumber, Friend.GetFriendsGroupKey(core), "VIEW");

            return newPhoneNumber;
        }

        public override long Id
        {
            get
            {
                return phoneId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Primitive IPermissibleItem.Owner
        {
            get
            {
                return Owner;
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(userId, ItemType.GetTypeId(core, typeof(User)));
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "Phone Number (" + PhoneNumber + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidUserPhoneNumberException : Exception
    {
    }
}
