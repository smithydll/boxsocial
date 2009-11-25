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
    [Permission("VIEW", "Can view the phone number")]
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
        [DataField("phone_time_ut")]
        private long phoneTimeRaw;

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

        private void UserPhoneNumber_ItemLoad()
        {
        }

        public static UserPhoneNumber Create(Core core, string phoneNumber, PhoneNumberTypes phoneType)
        {
            InsertQuery iquery = new InsertQuery(UserPhoneNumber.GetTable(typeof(UserPhoneNumber)));
            iquery.AddField("phone_user_id", core.session.LoggedInMember.Id);
            iquery.AddField("phone_number", phoneNumber);
            iquery.AddField("phone_type", (byte)phoneType);
            iquery.AddField("phone_time_ut", UnixTime.UnixTimeStamp());

            long phoneId = core.db.Query(iquery);

            return new UserPhoneNumber(core, phoneId);
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
                throw new NotImplementedException();
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public bool GetDefaultCan(string permission)
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
    }

    public class InvalidUserPhoneNumberException : Exception
    {
    }
}
