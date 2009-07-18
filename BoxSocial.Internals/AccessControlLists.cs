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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class AccessControlLists
    {
        private Core core;
        private IPermissibleItem item;

        public AccessControlLists(Core core, IPermissibleItem item)
        {
            this.core = core;
            this.item = item;
        }

        public void ParseACL(Template template)
        {
            Template aclTemplate = new Template("std.acl.html");

            List<AccessControlPermission> itemPermissions = null;
            List<AccessControlGrant> itemGrants = null;

            foreach (AccessControlGrant itemGrant in itemGrants)
            {
                core.UserProfiles.LoadPrimitiveProfile(itemGrant.PrimitiveKey);
            }

            foreach (AccessControlPermission itemPermission in itemPermissions)
            {
                VariableCollection permissionVariableCollection = new VariableCollection("permission");
                permissionVariableCollection.Parse("ID", itemPermission.Id.ToString());
                permissionVariableCollection.Parse("TITLE", itemPermission.Name);
                permissionVariableCollection.Parse("DESCRIPTION", itemPermission.Description);

                foreach (AccessControlGrant itemGrant in itemGrants)
                {
                    if (itemGrant.PermissionId == itemPermission.Id)
                    {
                        VariableCollection grantVariableCollection = new VariableCollection("grant");
                        
                        permissionVariableCollection.Parse("DISPLAY_NAME", core.UserProfiles[itemGrant.PrimitiveKey].DisplayName);

                        switch (itemGrant.Allow)
                        {
                            case AccessControlGrants.Allow:
                                break;
                            case AccessControlGrants.Deny:
                                break;
                            case AccessControlGrants.Inherit:
                                break;
                        }
                    }
                }
            }
        }

        public static List<AccessControlPermission> GetPermissions(Core core, IPermissibleItem item)
        {
            List<AccessControlPermission> permissions = new List<AccessControlPermission>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", item.Key.TypeId);

            DataTable permissionsDataTable = core.db.Query(query);

            foreach (DataRow permissionsDataRow in permissionsDataTable.Rows)
            {
                permissions.Add(new AccessControlPermission(core, permissionsDataRow));
            }

            return permissions;
        }
    }
}
