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

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [AccountSubModule("erp", "permissions", true)]
    public class AccountEnterpriseResourcePlanningPermissions : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "ERP Permissions";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountEnterpriseResourcePlanningPermissions(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountEnterpriseResourcePlanningPermissions_Load);
            this.Show += new EventHandler(AccountEnterpriseResourcePlanningPermissions_Show);
        }

        void AccountEnterpriseResourcePlanningPermissions_Load(object sender, EventArgs e)
        {
        }

        void AccountEnterpriseResourcePlanningPermissions_Show(object sender, EventArgs e)
        {
            Save(new EventHandler(AccountEnterpriseResourcePlanningPermissions_Save));

            SetTemplate("account_erp_permissions");

            ErpSettings settings = new ErpSettings(core, Owner);
            AccessControlLists acl = new AccessControlLists(core, settings);
            acl.ParseACL(template, LoggedInMember, "S_ERP_PERMS");
        }

        void AccountEnterpriseResourcePlanningPermissions_Save(object sender, EventArgs e)
        {
            ErpSettings settings = new ErpSettings(core, Owner);
            AccessControlLists acl = new AccessControlLists(core, settings);
            acl.SavePermissions();

            SetInformation("The permissions have been saved in the database.");
        }
    }
}
