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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.Forms;
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

        public void ParseACL(Template template, string variable)
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

                        RadioList allowrl = new RadioList(itemGrant.PermissionId + "," + itemGrant.PrimitiveKey.TypeId + "," + itemGrant.PrimitiveKey.Id);

                        allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));

                        switch (itemGrant.Allow)
                        {
                            case AccessControlGrants.Allow:
                                allowrl.SelectedKey = "allow";
                                break;
                            case AccessControlGrants.Deny:
                                allowrl.SelectedKey = "deny";
                                break;
                            case AccessControlGrants.Inherit:
                                allowrl.SelectedKey = "inherit";
                                break;
                        }

                        permissionVariableCollection.Parse("S_ALLOW", allowrl["allow"].ToString());
                        permissionVariableCollection.Parse("S_DENY", allowrl["deny"].ToString());
                        permissionVariableCollection.Parse("S_INHERIT", allowrl["inherit"].ToString());
                    }
                }
            }

            if (string.IsNullOrEmpty(variable))
            {
                variable = "S_PERMISSIONS";
            }

            template.ParseRaw(variable, aclTemplate.ToString());
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
