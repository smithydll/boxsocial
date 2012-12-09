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
    [AccountSubModule("profile", "permissions")]
    public class AccountProfilePermissions : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Profile Permissions";
            }
        }

        public override int Order
        {
            get
            {
                return 8;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountProfilePermissions class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountProfilePermissions(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountProfilePermissions_Load);
            this.Show += new EventHandler(AccountProfilePermissions_Show);
        }

        void AccountProfilePermissions_Load(object sender, EventArgs e)
        {
        }

        void AccountProfilePermissions_Show(object sender, EventArgs e)
        {
			Save(new EventHandler(AccountProfilePermissions_Save));
            if (core.Http.Form["delete"] != null)
            {
                acl_Delete();
            }

            SetTemplate("account_permissions");

            /*List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");*/

            //core.Display.ParsePermissionsBox(template, "S_PROFILE_PERMS", LoggedInMember.Permissions, permissions);
            
            AccessControlLists acl = new AccessControlLists(core, LoggedInMember);
            acl.ParseACL(template, LoggedInMember, "S_PROFILE_PERMS");
        }

        private void AccountProfilePermissions_Save(object sender, EventArgs e)
        {
            AccessControlLists acl = new AccessControlLists(core, LoggedInMember);
            acl.SavePermissions();

			SetInformation("Your profile permissions have been saved in the database.");
        }

        private void acl_Delete()
        {
            AccessControlLists acl = new AccessControlLists(core, LoggedInMember);

            string value = core.Http.Form["delete"];

            if (!string.IsNullOrEmpty(value))
            {
                string[] vals = value.Split(new char[] { ',' });

                if (vals.Length == 3)
                {
                    int permissionId = 0;
                    int primitiveTypeId = 0;
                    int primitiveId = 0;

                    int.TryParse(vals[0], out permissionId);
                    int.TryParse(vals[1], out primitiveTypeId);
                    int.TryParse(vals[2], out primitiveId);

                    if (permissionId != 0 && primitiveTypeId != 0 && primitiveId != 0)
                    {
                        try
                        {
                            acl.DeleteGrant(permissionId, primitiveTypeId, primitiveId);
                        }
                        catch
                        {
                            core.Functions.Generate403();
                            return;
                        }
                    }
                }
            }
        }
    }
}
