﻿/*
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

        public AccountProfilePermissions()
        {
            this.Load += new EventHandler(AccountProfilePermissions_Load);
            this.Show += new EventHandler(AccountProfilePermissions_Show);
        }

        void AccountProfilePermissions_Load(object sender, EventArgs e)
        {
        }

        void AccountProfilePermissions_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_permissions.html");

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            Display.ParsePermissionsBox(template, "S_PROFILE_PERMS", loggedInMember.Permissions, permissions);

            Save(new EventHandler(AccountProfilePermissions_Save));
        }

        void AccountProfilePermissions_Save(object sender, EventArgs e)
        {
            ushort permission = Functions.GetPermission();

            db.UpdateQuery(string.Format("UPDATE user_profile SET profile_access = {1} WHERE user_id = {0};",
                loggedInMember.UserId, permission));

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Permissions Saved", "Your profile permissions have been saved in the database.");
        }
    }
}