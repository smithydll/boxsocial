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
    [DataTable("user_emails")]
    public sealed class UserEmail : Item
    {
        [DataField("email_id", DataFieldKeys.Primary)]
        private long emailId;
        [DataField("email_user_id")]
        private long userId;
        [DataField("email_email", 127)]
        private string emailEmail;
        [DataField("email_verified")]
        private bool emailVerified;
        [DataField("email_time_ut")]
        private long emailTimeRaw;
        [DataField("email_activate_code")]
        private string emailActivateKey;
        [DataField("email_access")]
        private ushort permissions;

        private User owner;
        private Access emailAccess;

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

        public Access EmailAccess
        {
            get
            {
                if (emailAccess == null)
                {
                    emailAccess = new Access(db, permissions, Owner);
                }
                return emailAccess;
            }
        }

        public User Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.UserProfiles[userId];
                    return owner;
                }
                else
                {
                    return owner;
                }
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

        public static UserEmail Create(Core core, string email, ushort permissions)
        {
            if (!User.CheckEmailValid(email))
            {
                throw new EmailInvalidException();
            }

            if (!User.CheckEmailUnique(core.db, email))
            {
                throw new EmailAlreadyRegisteredException();
            }

            string activateKey = User.GenerateActivationSecurityToken();

            InsertQuery iquery = new InsertQuery(UserEmail.GetTable(typeof(User)));
            iquery.AddField("email_user_id", core.LoggedInMemberId);
            iquery.AddField("email_email", email);
            iquery.AddField("email_verified", false);
            iquery.AddField("email_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("email_activate_code", activateKey);
            iquery.AddField("email_access", permissions);

            long emailId = core.db.Query(iquery);

            string activateUri = string.Format("http://zinzam.com/register/?mode=activate-email&id={0}&key={1}",
                emailId, activateKey);

            Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "email_activation.eml");

            emailTemplate.Parse("TO_NAME", core.session.LoggedInMember.DisplayName);
            emailTemplate.Parse("U_ACTIVATE", activateUri);
            emailTemplate.Parse("USERNAME", core.session.LoggedInMember.UserName);

            BoxSocial.Internals.Email.SendEmail(email, "ZinZam email activation", emailTemplate.ToString());

            return new UserEmail(core, emailId);
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

        public override string Namespace
        {
            get { throw new NotImplementedException(); }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidUserEmailException : Exception
    {
    }
}
