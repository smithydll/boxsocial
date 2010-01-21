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
        
        private List<AccessControlPermission> itemPermissions = null;
        private List<AccessControlGrant> itemGrants = null;
        private List<UnsavedAccessControlGrant> unsavedGrants = null;

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
            
            ownerGroups.AddRange(core.GetPrimitivePermissionGroups(owner));

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
            aclTemplate.SetProse(core.prose);
            
            if (itemPermissions == null)
            {
                itemPermissions = GetPermissions(core, item);
            }
            if (itemGrants == null)
            {
                itemGrants = AccessControlGrant.GetGrants(core, item);
            }
            if (unsavedGrants == null)
            {
                unsavedGrants = new List<UnsavedAccessControlGrant>();
            }

            if (itemGrants != null)
            {
                foreach (AccessControlGrant itemGrant in itemGrants)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(itemGrant.PrimitiveKey);
                }
            }

            bool first = true;
            PermissionTypes lastType = PermissionTypes.View;
            VariableCollection permissionTypeVariableCollection = null;

            if (itemPermissions != null)
            {
                foreach (AccessControlPermission itemPermission in itemPermissions)
                {
                    if (first || itemPermission.PermissionType != lastType)
                    {
                        permissionTypeVariableCollection = aclTemplate.CreateChild("permision_types");

                        permissionTypeVariableCollection.Parse("TITLE", AccessControlLists.PermissionTypeToString(itemPermission.PermissionType));

                        first = false;
                        lastType = itemPermission.PermissionType;
                    }
                    VariableCollection permissionVariableCollection = permissionTypeVariableCollection.CreateChild("permission");
                    permissionVariableCollection.Parse("ID", itemPermission.Id.ToString());
                    permissionVariableCollection.Parse("TITLE", itemPermission.Name);
                    permissionVariableCollection.Parse("DESCRIPTION", itemPermission.Description);
                    
                    SelectBox groupsSelectBox = BuildGroupsSelectBox(string.Format("new-permission-group[{0}]", itemPermission.Id), owner);
    
                    if (itemGrants != null)
                    {
                        foreach (AccessControlGrant itemGrant in itemGrants)
                        {
                            if (itemGrant.PermissionId == itemPermission.Id)
                            {
                                string gsbk = string.Format("{0},{1}", itemGrant.PrimitiveKey.TypeId, itemGrant.PrimitiveKey.Id);
                                if (groupsSelectBox.ContainsKey(gsbk))
                                {
                                    groupsSelectBox[gsbk].Selectable = false;
                                }
                                
                                VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");
        
                                if (groupsSelectBox.ContainsKey(itemGrant.PrimitiveKey.ToString()))
                                {
                                    grantVariableCollection.Parse("DISPLAY_NAME", groupsSelectBox[itemGrant.PrimitiveKey.ToString()].Text);
                                    groupsSelectBox[itemGrant.PrimitiveKey.ToString()].Selectable = false;
                                }
                                else
                                {
                                    grantVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[itemGrant.PrimitiveKey].DisplayName);
                                }
        
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
                                
                                if (core.Http.Form["allow[" + itemPermission.Id + "," + itemGrant.PrimitiveKey.TypeId + "," + itemGrant.PrimitiveKey.Id +"]"] != null)
                                {
                                    allowrl.SelectedKey = core.Http.Form["allow[" + itemPermission.Id + "," + itemGrant.PrimitiveKey.TypeId + "," + itemGrant.PrimitiveKey.Id +"]"];
                                }
        
                                grantVariableCollection.Parse("S_ALLOW", allowrl["allow"]);
                                grantVariableCollection.Parse("S_DENY", allowrl["deny"]);
                                grantVariableCollection.Parse("S_INHERIT", allowrl["inherit"]);
                                
                                grantVariableCollection.Parse("ID", string.Format("{0},{1}", itemGrant.PrimitiveKey.TypeId, itemGrant.PrimitiveKey.Id));
                                grantVariableCollection.Parse("PERMISSION_ID", itemPermission.Id.ToString());
                                grantVariableCollection.Parse("IS_NEW", "FALSE");
                            }
                        }
        
                        foreach (AccessControlGrant itemGrant in itemGrants)
                        {
                            VariableCollection grantsVariableCollection = template.CreateChild("grants");
                        }
                    }
                    
                    if (core.Http.Form["save"] == null)
                    {
                    foreach (SelectBoxItem gsbi in groupsSelectBox)
                    {
                        if (core.Http.Form[string.Format("new-grant[{0},{1}]", itemPermission.Id, gsbi.Key)] != null)
                        {
                            ItemKey ik = new ItemKey(gsbi.Key);
                        
                            UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, itemPermission.Id, AccessControlGrants.Inherit);
                            
                            VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");
                            
                            grantVariableCollection.Parse("DISPLAY_NAME", gsbi.Text);
                            
                            RadioList allowrl = new RadioList("allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]");
                            
                            allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                            allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                            allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));
                            
                            if (core.Http.Form["allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]"] != null)
                            {
                                allowrl.SelectedKey = core.Http.Form["allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]"];
                            }
                            else
                            {
                                switch (uacg.Allow)
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
                            }
                            
                            grantVariableCollection.Parse("S_ALLOW", allowrl["allow"]);
                            grantVariableCollection.Parse("S_DENY", allowrl["deny"]);
                            grantVariableCollection.Parse("S_INHERIT", allowrl["inherit"]);
                            
                            grantVariableCollection.Parse("ID", string.Format("{0},{1}", ik.TypeId, ik.Id));
                            grantVariableCollection.Parse("PERMISSION_ID", itemPermission.Id.ToString());
                            grantVariableCollection.Parse("IS_NEW", "TRUE");
                            
                            gsbi.Selectable = false;
                        }
                    }
                    }
                    
                    if (core.Http.Form[string.Format("add-permission[{0}]", itemPermission.Id)] != null)
                    {
                        string groupSelectBoxId = core.Http.Form[string.Format("new-permission-group[{0}]", itemPermission.Id)];
                        
                        ItemKey ik = new ItemKey(groupSelectBoxId);
                        
                        UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, itemPermission.Id, AccessControlGrants.Inherit);
                        
                        VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");
                        
                        grantVariableCollection.Parse("DISPLAY_NAME", groupsSelectBox[groupSelectBoxId].Text);
                        
                        RadioList allowrl = new RadioList("allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]");
                        
                        allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                        allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));
                        
                        switch (uacg.Allow)
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
                        
                        grantVariableCollection.Parse("ID", string.Format("{0},{1}", ik.TypeId, ik.Id));
                        grantVariableCollection.Parse("PERMISSION_ID", itemPermission.Id.ToString());
                        grantVariableCollection.Parse("IS_NEW", "TRUE");
                        
                        groupsSelectBox[groupSelectBoxId].Selectable = false;
                    }
    
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

        private static string PermissionTypeToString(PermissionTypes permissionTypes)
        {
            switch (permissionTypes)
            {
                case PermissionTypes.View:
                    return "View";
                case PermissionTypes.Interact:
                    return "Interact";
                case PermissionTypes.CreateAndEdit:
                    return "Create and Edit";
                case PermissionTypes.Delete:
                    return "Delete";
                default:
                    return "Other";
            }
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
            query.AddSort(SortOrder.Ascending, "permission_type");

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
                        permissions.Add(new PermissionInfo(pattr.Key, pattr.Description, pattr.Type));
                    }
                    attributeFound = true;
                }
            }
            
            return permissions;
        }

        public void SavePermissions()
        {
            if (itemPermissions == null)
            {
                itemPermissions = GetPermissions(core, item);
            }
            if (itemGrants == null)
            {
                itemGrants = AccessControlGrant.GetGrants(core, item);
            }
            if (unsavedGrants == null)
            {
                unsavedGrants = new List<UnsavedAccessControlGrant>();
            }
            
            if (itemPermissions != null)
            {
                foreach (AccessControlPermission itemPermission in itemPermissions)
                {
                    SelectBox groupsSelectBox = BuildGroupsSelectBox(string.Format("new-permission-group[{0}]", itemPermission.Id), item.Owner);
                    
                    foreach (SelectBoxItem gsbi in groupsSelectBox)
                    {
                        if (core.Http.Form[string.Format("new-grant[{0},{1}]", itemPermission.Id, gsbi.Key)] != null)
                        {
                            ItemKey ik = new ItemKey(gsbi.Key);
                        
                            UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, itemPermission.Id, AccessControlGrants.Inherit);
                                                        
                            if (core.Http.Form["allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]"] != null)
                            {
                                switch (core.Http.Form["allow[" + itemPermission.Id + "," + ik.TypeId + "," + ik.Id +"]"])
                                {
                                    case "allow":
                                        uacg.Allow = AccessControlGrants.Allow;
                                        break;
                                    case "deny":
                                        uacg.Allow = AccessControlGrants.Deny;
                                        break;
                                    case "inherit":
                                        uacg.Allow = AccessControlGrants.Inherit;
                                        break;
                                }
                            }
                            
                            try
                            {
                                AccessControlGrant.Create(core, ik, item.ItemKey, itemPermission.Id, uacg.Allow);
                            }
                            catch (InvalidAccessControlGrantException)
                            {
                            }
                        }
                    }
                }
            }
        }
    }
}
