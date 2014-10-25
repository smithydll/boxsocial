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
    [AccountModule(AppPrimitives.Application, "applications")]
    public class AccountApplications : AccountModule
    {
        public AccountApplications(Account account)
            : base(account)
        {
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                if (Owner is ApplicationEntry)
                {
                    return core.Prose.GetString("APPLICATION");
                }
                else
                {
                    return core.Prose.GetString("APPLICATIONS");
                }
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }
    }
}
