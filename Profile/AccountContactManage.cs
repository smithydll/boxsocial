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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "contact")]
    public class AccountContactManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Contact Details";
            }
        }

        public override int Order
        {
            get
            {
                return 6;
            }
        }

        public AccountContactManage()
        {
            this.Load += new EventHandler(AccountContactManage_Load);
            this.Show += new EventHandler(AccountContactManage_Show);
        }

        void AccountContactManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit-address", new ModuleModeHandler(AccountContactManage_EditAddress));
            AddModeHandler("add-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("edit-email", new ModuleModeHandler(AccountContactManage_AddEmail));
            AddModeHandler("add-phone", new ModuleModeHandler(AccountContactManage_AddPhone));
            AddModeHandler("edit-phone", new ModuleModeHandler(AccountContactManage_AddPhone));

            AddSaveHandler("edit-address", new EventHandler(AccountContactManage_EditAddress_save));
        }

        void AccountContactManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_contact_manage");

            List<UserEmail> emails = core.session.LoggedInMember.GetEmailAddresses();

            foreach (UserEmail email in emails)
            {
                VariableCollection emailsVariableCollection = template.CreateChild("email_list");

                emailsVariableCollection.Parse("EMAIL_ID", email.Id.ToString());
                emailsVariableCollection.Parse("EMAIL_ADDRESS", email.Email);
            }

            template.Parse("ADD_EMAIL", BuildUri("add-email"));
        }

        void AccountContactManage_EditAddress(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_address_edit");
        }

        void AccountContactManage_AddEmail(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_email_edit");

            switch (e.Mode)
            {
                case "add-email":
                    break;
                case "edit-email":

                    break;
            }
        }

        void AccountContactManage_AddPhone(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_phone_edit");
        }

        void AccountContactManage_EditAddress_save(object sender, EventArgs e)
        {
        }
    }
}
