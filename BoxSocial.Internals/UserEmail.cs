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
    public enum EmailAddressTypes : byte
    {
        Personal = 0x01,
        Business = 0x02,
        Student = 0x04,
        Other = 0x08,
    }

    [DataTable("user_emails")]
    [Permission("VIEW", "Can view the e-mail address", PermissionTypes.View)]
    [Permission("RECIEVE_FROM", "Can receive e-mail from", PermissionTypes.Interact)]
    public sealed class UserEmail : NumberedItem, IPermissibleItem
    {
        [DataField("email_id", DataFieldKeys.Primary)]
        private long emailId;
        [DataField("email_user_id", typeof(User))]
        private long userId;
        [DataField("email_email", DataFieldKeys.Unique ,127)]
        private string emailEmail;
        [DataField("email_type")]
        private byte emailType;
        [DataField("email_verified")]
        private bool emailVerified;
        [DataField("email_time_ut")]
        private long emailTimeRaw;
        [DataField("email_activate_code", 32)]
        private string emailActivateKey;
        [DataField("email_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access access;

        public long EmailId
        {
            get
            {
                return emailId;
            }
        }

        public string Email
        {
            get
            {
                return emailEmail;
            }
        }

        public EmailAddressTypes EmailType
        {
            get
            {
                return (EmailAddressTypes)emailType;
            }
            set
            {
                SetProperty("emailType", (byte)value);
            }
        }

        public bool IsActivated
        {
            get
            {
                return emailVerified;
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

        public UserEmail(Core core, long emailId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserEmail_ItemLoad);

            try
            {
                LoadItem(emailId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserEmailException();
            }
        }

        public UserEmail(Core core, string email)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserEmail_ItemLoad);

            try
            {
                LoadItem("email_email", email);
            }
            catch
            {
                throw new InvalidUserEmailException();
            }
        }

        public UserEmail(Core core, DataRow emailRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserEmail_ItemLoad);

            try
            {
                loadItemInfo(emailRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserEmailException();
            }
        }

        private void UserEmail_ItemLoad()
        {
        }

        public static UserEmail Create(Core core, string email, EmailAddressTypes type)
        {
            return Create(core, core.Session.LoggedInMember, email, type, false);
        }

        public static UserEmail Create(Core core, User owner, string email, EmailAddressTypes type, bool isRegistration)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!User.CheckEmailValid(email))
            {
                throw new EmailInvalidException();
            }

            if (!User.CheckEmailUnique(core, email))
            {
                throw new EmailAlreadyRegisteredException();
            }

            string activateKey = User.GenerateActivationSecurityToken();

            InsertQuery iquery = new InsertQuery(UserEmail.GetTable(typeof(UserEmail)));
            iquery.AddField("email_user_id", owner.Id);
            iquery.AddField("email_email", email);
            iquery.AddField("email_type", (byte)type);
            if (!isRegistration)
            {
                iquery.AddField("email_verified", false);
            }
            else
            {
                iquery.AddField("email_verified", true);
            }
            iquery.AddField("email_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("email_activate_code", activateKey);

            long emailId = core.Db.Query(iquery);

            if (!isRegistration)
            {
                string activateUri = string.Format("http://" + Linker.Domain + "/register/?mode=activate-email&id={0}&key={1}",
                    emailId, activateKey);

                RawTemplate emailTemplate = new RawTemplate(core.Http.TemplateEmailPath, "email_activation.eml");

                emailTemplate.Parse("TO_NAME", owner.DisplayName);
                emailTemplate.Parse("U_ACTIVATE", activateUri);
                emailTemplate.Parse("USERNAME", owner.UserName);

                core.Email.SendEmail(email, core.Settings.SiteTitle + " email activation", emailTemplate.ToString());
            }

            UserEmail newEmail = new UserEmail(core, emailId);

            Access.CreateGrantForPrimitive(core, newEmail, Friend.FriendsGroupKey, "VIEW");
            Access.CreateGrantForPrimitive(core, newEmail, User.EveryoneGroupKey, "RECIEVE_FROM");

            return newEmail;
        }

        public bool Activate(string activationCode)
        {
            if (activationCode == this.emailActivateKey)
            {
                emailVerified = true;
                UpdateThis();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override long Id
        {
            get
            {
                return emailId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
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
                return new ItemKey(userId, typeof(User));
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
                return "Email (" + Email + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidUserEmailException : Exception
    {
    }
}
