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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [AccountSubModule("erp", "settings", true)]
    public class AccountEnterpriseResourcePlanningSettings : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "ERP Settings";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountEnterpriseResourcePlanningSettings(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountEnterpriseResourcePlanningSettings_Load);
            this.Show += new EventHandler(AccountEnterpriseResourcePlanningSettings_Show);
        }

        void AccountEnterpriseResourcePlanningSettings_Load(object sender, EventArgs e)
        {
        }

        void AccountEnterpriseResourcePlanningSettings_Show(object sender, EventArgs e)
        {
            SetTemplate("account_erp_settings");
        }
    }
}
