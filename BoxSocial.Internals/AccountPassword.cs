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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "password")]
    public class AccountPassword : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Change Password";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountPassword class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountPassword(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountPassword_Load);
            this.Show += new EventHandler(AccountPassword_Show);
        }

        void AccountPassword_Load(object sender, EventArgs e)
        {
        }

        void AccountPassword_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_password.html");

            Save(new EventHandler(AccountPassword_Save));
        }

        void AccountPassword_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();
            
            string password = core.Http.Form["old-password"];

            password = User.HashPassword(password);

            SelectQuery query = new SelectQuery(User.GetTable(typeof(User)));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddJoin(JoinTypes.Inner, "user_info", "user_id", "user_id");
            query.AddCondition("user_keys.user_id", core.LoggedInMemberId);
            query.AddCondition("user_password", password);

            DataTable userTable = db.Query(query);
            if (userTable.Rows.Count != 1)
            {
                SetError("The old password you entered does not match your old password, make sure you have entered your old password correctly.");
                return;
            }
            else if (core.Http.Form["new-password"] != core.Http.Form["confirm-password"])
            {
                SetError("The passwords you entered do not match, make sure you have entered your desired password correctly.");
                return;
            }
            else if (((string)core.Http.Form["new-password"]).Length < 6)
            {
                SetError("The password you entered is too short. Please choose a strong password of 6 characters or more.");
                return;
            }

            UpdateQuery uquery = new UpdateQuery("user_info");
            uquery.AddField("user_password", User.HashPassword(core.Http.Form["new-password"]));
            uquery.AddCondition("user_id", core.LoggedInMemberId);

            long rowsChanged = db.Query(uquery);

            if (rowsChanged == 1)
            {
				SetInformation("You have successfully changed your password. Keep your password safe and do not share it with anyone.");
                //SetRedirectUri(BuildUri());
                //Display.ShowMessage("Changed Password", "You have successfully changed your password. Keep your password safe and do not share it with anyone.");
            }
            else
            {
                DisplayGenericError();
                return;
            }
        }
    }
}
