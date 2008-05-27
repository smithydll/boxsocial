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

        private void UserEmail_ItemLoad()
        {
            /*if (owner == null || ownerId != owner.Id)
            {
                owner = new Member(core, userId);
            }

            eventAccess = new Access(db, permissions, owner);*/
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
