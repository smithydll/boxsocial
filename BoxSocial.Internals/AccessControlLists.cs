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

        public SelectBox BuildGroupsSelectBox(string name, Primitive owner)
        {
            SelectBox sb = new SelectBox(name);

            //sb.Add(new SelectBoxItem(string.Format("{0},{1}", ItemType.GetTypeId(typeof(User)), -1), "Everyone"));

            List<PrimitivePermissionGroup> ownerGroups = owner.GetPrimitivePermissionGroups();

            foreach (PrimitivePermissionGroup ppg in ownerGroups)
            {
                if (!string.IsNullOrEmpty(ppg.LanguageKey))
                {
                    sb.Add(new SelectBoxItem(string.Format("{0},{1}", ppg.TypeId, ppg.ItemId), core.prose.GetString(ppg.LanguageKey)));
                }
                else
                {
                    sb.Add(new SelectBoxItem(string.Format("{0},{1}", ppg.TypeId, ppg.ItemId), ppg.DisplayName));
                }
            }

            return sb;
        }

        public void ParseACL(Template template, Primitive owner, string variable)
        {
            Template aclTemplate = new Template(core.Http.TemplatePath, "std.acl.html");

            List<AccessControlPermission> itemPermissions = GetPermissions(core, item);
            List<AccessControlGrant> itemGrants = AccessControlGrant.GetGrants(core, (NumberedItem)item);

            if (itemGrants != null)
            {
                foreach (AccessControlGrant itemGrant in itemGrants)
                {
                    core.UserProfiles.LoadPrimitiveProfile(itemGrant.PrimitiveKey);
                }
            }

            if (itemPermissions != null)
            {
                foreach (AccessControlPermission itemPermission in itemPermissions)
                {
                    VariableCollection permissionVariableCollection = aclTemplate.CreateChild("permission");
                    permissionVariableCollection.Parse("ID", itemPermission.Id.ToString());
                    permissionVariableCollection.Parse("TITLE", itemPermission.Name);
                    permissionVariableCollection.Parse("DESCRIPTION", itemPermission.Description);
    
                    if (itemGrants != null)
                    {
                        foreach (AccessControlGrant itemGrant in itemGrants)
                        {
                            if (itemGrant.PermissionId == itemPermission.Id)
                            {
                                VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");
        
                                grantVariableCollection.Parse("DISPLAY_NAME", core.UserProfiles[itemGrant.PrimitiveKey].DisplayName);
        
                                RadioList allowrl = new RadioList("allow[" + itemGrant.PermissionId + "," + itemGrant.PrimitiveKey.TypeId + "," + itemGrant.PrimitiveKey.Id +"]");
        
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
        
                                grantVariableCollection.Parse("S_ALLOW", allowrl["allow"]);
                                grantVariableCollection.Parse("S_DENY", allowrl["deny"]);
                                grantVariableCollection.Parse("S_INHERIT", allowrl["inherit"]);
                            }
                        }
        
                        foreach (AccessControlGrant itemGrant in itemGrants)
                        {
                            VariableCollection grantsVariableCollection = template.CreateChild("grants");
                        }
                    }
                    
                    // Owner
                    /*List<PrimitivePermissionGroup> ownerGroups = owner.GetPrimitivePermissionGroups();

                    foreach (PrimitivePermissionGroup ppg in ownerGroups)
                    {
                        VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");
                        
                        grantVariableCollection.Parse("DISPLAY_NAME", ppg.LanguageKey);
                        
                        RadioList allowrl = new RadioList("allow[" + itemPermission + "," + ppg.TypeId + "," + ppg.ItemId +"]");
    
                        allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));
                        
                        grantVariableCollection.Parse("S_ALLOW", allowrl["allow"]);
                        grantVariableCollection.Parse("S_DENY", allowrl["deny"]);
                        grantVariableCollection.Parse("S_INHERIT", allowrl["inherit"]);
                    }*/
    
                    SelectBox groupsSelectBox = BuildGroupsSelectBox(string.Format("new-permission-group[{0}]", itemPermission.Id), owner);
    
                    permissionVariableCollection.Parse("S_PERMISSION_GROUPS", groupsSelectBox);
    
                    RadioList allowNewrl = new RadioList("new-permission-group-allow");
    
                    allowNewrl.Add(new RadioListItem(allowNewrl.Name, "allow", "Allow"));
                    allowNewrl.Add(new RadioListItem(allowNewrl.Name, "deny", "Deny"));
                    allowNewrl.Add(new RadioListItem(allowNewrl.Name, "inherit", "Inherit"));
    
                    allowNewrl.SelectedKey = "inherit";
    
                    permissionVariableCollection.Parse("S_ALLOW", allowNewrl["allow"].ToString());
                    permissionVariableCollection.Parse("S_DENY", allowNewrl["deny"].ToString());
                    permissionVariableCollection.Parse("S_INHERIT", allowNewrl["inherit"].ToString());
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
            return GetPermissions(core, item.ItemKey);
        }

        public static List<AccessControlPermission> GetPermissions(Core core, ItemKey itemKey)
        {
            List<AccessControlPermission> permissions = new List<AccessControlPermission>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", itemKey.TypeId);

            DataTable permissionsDataTable = core.db.Query(query);

            foreach (DataRow permissionsDataRow in permissionsDataTable.Rows)
            {
                permissions.Add(new AccessControlPermission(core, permissionsDataRow));
            }

            return permissions;
        }
        
        public static List<string> GetPermissionStrings(Type type)
        {
            List<string> permissions = new List<string>();
            bool attributeFound = false;
            foreach (Attribute attr in type.GetCustomAttributes(typeof(PermissionAttribute), false))
            {
                PermissionAttribute pattr = (PermissionAttribute)attr;
                if (pattr != null)
                {
                    if (pattr.Key != null)
                    {
                        permissions.Add(pattr.Key);
                    }
                    attributeFound = true;
                }
            }
            
            return permissions;
        }
        
        public static List<PermissionInfo> GetPermissionInfo(Type type)
        {
            List<PermissionInfo> permissions = new List<PermissionInfo>();
            bool attributeFound = false;
            foreach (Attribute attr in type.GetCustomAttributes(typeof(PermissionAttribute), false))
            {
                PermissionAttribute pattr = (PermissionAttribute)attr;
                if (pattr != null)
                {
                    if (pattr.Key != null)
                    {
                        permissions.Add(new PermissionInfo(pattr.Key, pattr.Description));
                    }
                    attributeFound = true;
                }
            }
            
            return permissions;
        }

        public static void SavePermissions()
        {
        }
    }
}
