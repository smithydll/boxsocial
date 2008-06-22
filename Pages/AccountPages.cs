/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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

namespace BoxSocial.Applications.Pages
{
    [AccountModule("pages")]
    public class AccountPages : AccountModule
    {
        public AccountPages(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManagePages);
            //RegisterSubModule += new RegisterSubModuleHandler(WritePage);
            //RegisterSubModule += new RegisterSubModuleHandler(ManageDrafts);
            //RegisterSubModule += new RegisterSubModuleHandler(ManageLists);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {       
        }

        public override string Name
        {
            get
            {
                return "Pages";
            }
        }

        public override int Order
        {
            get
            {
                return 5;
            }
        }
    }
}
