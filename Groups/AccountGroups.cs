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
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    [AccountModule("groups")]
    public class AccountGroups : AccountModule
    {
        public AccountGroups(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManageGroups);
            //RegisterSubModule += new RegisterSubModuleHandler(ManageGroupMemberships);
            //RegisterSubModule += new RegisterSubModuleHandler(DeleteGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(EditGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(JoinGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(LeaveGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(InviteGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOfficer);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOperator);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupRemoveOfficer);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupResignOperator);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupApproveMember);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupBanMember);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Groups";
            }
        }

        public override int Order
        {
            get
            {
                return 7;
            }
        }
    }
}
