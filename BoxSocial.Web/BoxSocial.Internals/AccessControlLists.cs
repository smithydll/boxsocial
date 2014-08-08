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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
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

            List<PrimitivePermissionGroup> ownerGroups = new List<PrimitivePermissionGroup>();
            int itemGroups = 0;

            Type type = item.GetType();
            if (type.GetMethod(type.Name + "_GetItemGroups", new Type[] { typeof(Core) }) != null)
            {
                ownerGroups.AddRange((List<PrimitivePermissionGroup>)type.InvokeMember(type.Name + "_GetItemGroups", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }));
                itemGroups = ownerGroups.Count;
            }
            
            ownerGroups.AddRange(core.GetPrimitivePermissionGroups(owner));

            int i = 0;
            foreach (PrimitivePermissionGroup ppg in ownerGroups)
            {
                if (i == 0 && itemGroups > 0)
                {
                    sb.Add(new SelectBoxItem("-1", "Item groups", false));
                }
                if (i == itemGroups)
                {
                    sb.Add(new SelectBoxItem("-2", "Friendship groups", false));
                }
                if (!string.IsNullOrEmpty(ppg.LanguageKey))
                {
                    sb.Add(new SelectBoxItem(string.Format("{0},{1}", ppg.TypeId, ppg.ItemId), " -- " + core.Prose.GetString(ppg.LanguageKey)));
                }
                else
                {
                    sb.Add(new SelectBoxItem(string.Format("{0},{1}", ppg.TypeId, ppg.ItemId), " -- " + ppg.DisplayName));
                }
                i++;
            }

            return sb;
        }

        public void ParseACL(Template template, Primitive owner, string variable)
        {
            Template aclTemplate = new Template("std.acl.html");
            aclTemplate.Medium = core.Template.Medium;
            aclTemplate.SetProse(core.Prose);
            
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

            bool simple = item.IsSimplePermissions;

            string mode = core.Http["aclmode"];
            switch (mode)
            {
                case "simple":
                    simple = true;
                    break;
                case "detailed":
                    simple = false;
                    break;
            }

            bool first = true;
            PermissionTypes lastType = PermissionTypes.View;
            VariableCollection permissionTypeVariableCollection = null;

            PermissionGroupSelectBox typeGroupSelectBox = null;
            List<PrimitivePermissionGroup> ownerGroups = null;

            if (itemPermissions != null)
            {
                foreach (AccessControlPermission itemPermission in itemPermissions)
                {
                    if (first || itemPermission.PermissionType != lastType)
                    {
                        if (typeGroupSelectBox != null)
                        {
                            permissionTypeVariableCollection.Parse("S_SIMPLE_SELECT", typeGroupSelectBox);
                        }

                        permissionTypeVariableCollection = aclTemplate.CreateChild("permision_types");
                        typeGroupSelectBox = new PermissionGroupSelectBox(core, "group-select-" + itemPermission.PermissionType.ToString(), item.ItemKey);

                        permissionTypeVariableCollection.Parse("TITLE", AccessControlLists.PermissionTypeToString(itemPermission.PermissionType));

                        first = false;
                        lastType = itemPermission.PermissionType;
                    }

                    if (simple)
                    {
                        if (ownerGroups == null)
                        {
                            ownerGroups = new List<PrimitivePermissionGroup>();
                            int itemGroups = 0;

                            Type type = item.GetType();
                            if (type.GetMethod(type.Name + "_GetItemGroups", new Type[] { typeof(Core) }) != null)
                            {
                                ownerGroups.AddRange((List<PrimitivePermissionGroup>)type.InvokeMember(type.Name + "_GetItemGroups", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }));
                                itemGroups = ownerGroups.Count;
                            }

                            ownerGroups.AddRange(core.GetPrimitivePermissionGroups(owner));
                        }
                        
                        VariableCollection permissionVariableCollection = permissionTypeVariableCollection.CreateChild("permission_desc");
                        permissionVariableCollection.Parse("ID", itemPermission.Id.ToString());
                        permissionVariableCollection.Parse("TITLE", itemPermission.Name);
                        permissionVariableCollection.Parse("DESCRIPTION", itemPermission.Description);

                        if (itemGrants != null)
                        {
                            foreach (AccessControlGrant itemGrant in itemGrants)
                            {
                                if (itemGrant.PermissionId == itemPermission.Id)
                                {
                                    switch (itemGrant.Allow)
                                    {
                                        case AccessControlGrants.Allow:
                                            PrimitivePermissionGroup ppg = null;

                                            ppg = new PrimitivePermissionGroup(itemGrant.PrimitiveKey, string.Empty, string.Empty);
                                            foreach (PrimitivePermissionGroup p in ownerGroups)
                                            {
                                                if (ppg.ItemKey.Equals(p.ItemKey))
                                                {
                                                    ppg = p;
                                                    break;
                                                }
                                            }

                                            if (!typeGroupSelectBox.ItemKeys.Contains(ppg))
                                            {
                                                typeGroupSelectBox.ItemKeys.Add(ppg);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
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

                                    if (groupsSelectBox.ContainsKey(gsbk))
                                    {
                                        string text = groupsSelectBox[gsbk].Text;
                                        if (text.StartsWith(" -- ", StringComparison.Ordinal))
                                        {
                                            text = text.Substring(4);
                                        }
                                        grantVariableCollection.Parse("DISPLAY_NAME", text);
                                        groupsSelectBox[gsbk].Selectable = false;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            grantVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[itemGrant.PrimitiveKey].DisplayName);
                                        }
                                        catch
                                        {
                                            grantVariableCollection.Parse("DISPLAY_NAME", "{{ERROR LOADING PRIMITIVE(" + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString() + ":" + (new ItemType(core, itemGrant.PrimitiveKey.TypeId)).Namespace + ")}}");
                                        }
                                    }

                                    RadioList allowrl = new RadioList("allow[" + itemGrant.PermissionId.ToString() + "," + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString() + "]");
                                    SelectBox allowsb = new SelectBox("allow[" + itemGrant.PermissionId.ToString() + "," + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString() + "]");
                                    Button deleteButton = new Button("delete", "Delete", itemGrant.PermissionId.ToString() + "," + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString());

                                    allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                                    allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                                    allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));

                                    allowsb.Add(new SelectBoxItem("allow", "Allow"));
                                    allowsb.Add(new SelectBoxItem("deny", "Deny"));
                                    allowsb.Add(new SelectBoxItem("inherit", "Inherit"));

                                    switch (itemGrant.Allow)
                                    {
                                        case AccessControlGrants.Allow:
                                            allowrl.SelectedKey = "allow";
                                            allowsb.SelectedKey = "allow";
                                            break;
                                        case AccessControlGrants.Deny:
                                            allowrl.SelectedKey = "deny";
                                            allowsb.SelectedKey = "deny";
                                            break;
                                        case AccessControlGrants.Inherit:
                                            allowrl.SelectedKey = "inherit";
                                            allowsb.SelectedKey = "inherit";
                                            break;
                                    }

                                    if (core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString() + "]"] != null)
                                    {
                                        allowrl.SelectedKey = core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + itemGrant.PrimitiveKey.TypeId.ToString() + "," + itemGrant.PrimitiveKey.Id.ToString() + "]"];
                                    }

                                    grantVariableCollection.Parse("S_GRANT", allowsb);

                                    grantVariableCollection.Parse("S_ALLOW", allowrl["allow"]);
                                    grantVariableCollection.Parse("S_DENY", allowrl["deny"]);
                                    grantVariableCollection.Parse("S_INHERIT", allowrl["inherit"]);

                                    grantVariableCollection.Parse("S_DELETE", deleteButton);

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

                                    UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, item.ItemKey, itemPermission.Id, AccessControlGrants.Inherit);

                                    VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");

                                    grantVariableCollection.Parse("DISPLAY_NAME", gsbi.Text);

                                    RadioList allowrl = new RadioList("allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]");
                                    SelectBox allowsb = new SelectBox("allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]");

                                    allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                                    allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                                    allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));

                                    allowsb.Add(new SelectBoxItem("allow", "Allow"));
                                    allowsb.Add(new SelectBoxItem("deny", "Deny"));
                                    allowsb.Add(new SelectBoxItem("inherit", "Inherit"));

                                    if (core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]"] != null)
                                    {
                                        allowrl.SelectedKey = core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]"];
                                    }
                                    else
                                    {
                                        switch (uacg.Allow)
                                        {
                                            case AccessControlGrants.Allow:
                                                allowrl.SelectedKey = "allow";
                                                allowsb.SelectedKey = "allow";
                                                break;
                                            case AccessControlGrants.Deny:
                                                allowrl.SelectedKey = "deny";
                                                allowsb.SelectedKey = "deny";
                                                break;
                                            case AccessControlGrants.Inherit:
                                                allowrl.SelectedKey = "inherit";
                                                allowsb.SelectedKey = "inherit";
                                                break;
                                        }
                                    }

                                    grantVariableCollection.Parse("S_GRANT", allowsb);

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

                            UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, item.ItemKey, itemPermission.Id, AccessControlGrants.Inherit);

                            VariableCollection grantVariableCollection = permissionVariableCollection.CreateChild("grant");

                            grantVariableCollection.Parse("DISPLAY_NAME", groupsSelectBox[groupSelectBoxId].Text);

                            RadioList allowrl = new RadioList("allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]");
                            SelectBox allowsb = new SelectBox("allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]");

                            allowrl.Add(new RadioListItem(allowrl.Name, "allow", "Allow"));
                            allowrl.Add(new RadioListItem(allowrl.Name, "deny", "Deny"));
                            allowrl.Add(new RadioListItem(allowrl.Name, "inherit", "Inherit"));

                            allowsb.Add(new SelectBoxItem("allow", "Allow"));
                            allowsb.Add(new SelectBoxItem("deny", "Deny"));
                            allowsb.Add(new SelectBoxItem("inherit", "Inherit"));

                            switch (uacg.Allow)
                            {
                                case AccessControlGrants.Allow:
                                    allowrl.SelectedKey = "allow";
                                    allowsb.SelectedKey = "allow";
                                    break;
                                case AccessControlGrants.Deny:
                                    allowrl.SelectedKey = "deny";
                                    allowsb.SelectedKey = "deny";
                                    break;
                                case AccessControlGrants.Inherit:
                                    allowrl.SelectedKey = "inherit";
                                    allowsb.SelectedKey = "inherit";
                                    break;
                            }

                            grantVariableCollection.Parse("S_GRANT", allowsb);

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
                        SelectBox allowNewsb = new SelectBox("new-permission-group-allow");

                        allowNewrl.Add(new RadioListItem(allowNewrl.Name, "allow", "Allow"));
                        allowNewrl.Add(new RadioListItem(allowNewrl.Name, "deny", "Deny"));
                        allowNewrl.Add(new RadioListItem(allowNewrl.Name, "inherit", "Inherit"));

                        allowNewsb.Add(new SelectBoxItem("allow", "Allow"));
                        allowNewsb.Add(new SelectBoxItem("deny", "Deny"));
                        allowNewsb.Add(new SelectBoxItem("inherit", "Inherit"));

                        allowNewrl.SelectedKey = "inherit";
                        allowNewsb.SelectedKey = "inherit";

                        permissionVariableCollection.Parse("S_GRANT", allowNewsb);

                        permissionVariableCollection.Parse("S_ALLOW", allowNewrl["allow"].ToString());
                        permissionVariableCollection.Parse("S_DENY", allowNewrl["deny"].ToString());
                        permissionVariableCollection.Parse("S_INHERIT", allowNewrl["inherit"].ToString());
                    }
                }

                if (typeGroupSelectBox != null)
                {
                    permissionTypeVariableCollection.Parse("S_SIMPLE_SELECT", typeGroupSelectBox);
                }
            }

            if (string.IsNullOrEmpty(variable))
            {
                variable = "S_PERMISSIONS";
            }

            /*PermissionGroupSelectBox groupSelectBox = new PermissionGroupSelectBox(core, "group-select", item.ItemKey);
            groupSelectBox.SelectMultiple = true;

            aclTemplate.Parse("S_SIMPLE_SELECT", groupSelectBox);*/

            if (simple)
            {
                aclTemplate.Parse("IS_SIMPLE", "TRUE");
            }

            aclTemplate.Parse("U_DETAILED", Access.BuildAclUri(core, item, false));
            aclTemplate.Parse("U_SIMPLE", Access.BuildAclUri(core, item, true));

            HiddenField modeField = new HiddenField("aclmode");
            if (simple)
            {
                modeField.Value = "simple";
            }
            else
            {
                modeField.Value = "detailed";
            }

            aclTemplate.Parse("S_ACLMODE", modeField);

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

            SelectQuery query = Item.GetSelectQueryStub(core, typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", itemKey.TypeId);
            query.AddSort(SortOrder.Ascending, "permission_type");

            System.Data.Common.DbDataReader permissionsReader = core.Db.ReaderQuery(query);

            while (permissionsReader.Read())
            {
                permissions.Add(new AccessControlPermission(core, permissionsReader));
            }

            permissionsReader.Close();
            permissionsReader.Dispose();

            return permissions;
        }
        
        public static List<string> GetPermissionStrings(Type type)
        {
            List<string> permissions = new List<string>();
            bool attributeFound = false;
            object[] attrs = type.GetCustomAttributes(typeof(PermissionAttribute), false);
            foreach (Attribute attr in attrs)
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
            object[] attrs = type.GetCustomAttributes(typeof(PermissionAttribute), false);
            foreach (Attribute attr in attrs)
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

        public void DeleteGrant(int permissionId, int primitiveTypeId, int primitiveId)
        {
            AccessControlGrant acg = new AccessControlGrant(core, new ItemKey(primitiveId, primitiveTypeId), item.ItemKey, permissionId);
            acg.Delete();
        }

        public static AccessControlToken GetNewItemPermissionsToken(Core core)
        {
            return GetNewItemPermissionsToken(core, null, "permissions");
        }

        public static AccessControlToken GetNewItemPermissionsToken(Core core, IPermissibleItem item, string fieldName)
        {
            AccessControlToken token = new AccessControlToken(core, item);

            List<PrimitivePermissionGroup> groups = PermissionGroupSelectBox.FormPermissionGroups(core, fieldName);

            token.AddGroups(groups);

            return token;
        }

        public void SaveNewItemPermissions(string fieldName)
        {
            SaveNewItemPermissions(GetNewItemPermissionsToken(core, item, fieldName));
        }

        public void SaveNewItemPermissions()
        {
            SaveNewItemPermissions("permissions");
        }

        public void SaveNewItemPermissions(AccessControlToken token)
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
                List<PrimitivePermissionGroup> groups = token.Groups;

                if (groups.Count > 0)
                {
                    foreach (AccessControlPermission itemPermission in itemPermissions)
                    {
                        List<ItemKey> keysGranted = new List<ItemKey>();
                        foreach (AccessControlGrant grant in itemGrants)
                        {
                            if (grant.PermissionId == itemPermission.Id)
                            {
                                if (grant.Allow == AccessControlGrants.Allow)
                                {
                                    keysGranted.Add(grant.PrimitiveKey);
                                }
                            }
                        }

                        List<ItemKey> keysPosted = new List<ItemKey>();
                        if (itemPermission.PermissionType == PermissionTypes.View ||
                            itemPermission.PermissionType == PermissionTypes.Interact)
                        {
                            foreach (PrimitivePermissionGroup ppg in groups)
                            {
                                bool flag = true;
                                if (ppg.ItemKey.Equals(User.GetEveryoneGroupKey(core)) &&
                                    itemPermission.PermissionType == PermissionTypes.Interact)
                                {
                                    flag = false;

                                    // Add registered users instead of everyone for interact by default
                                    if (!keysGranted.Contains(User.GetRegisteredUsersGroupKey(core)))
                                    {
                                        AccessControlGrant newACG = AccessControlGrant.Create(core, User.GetRegisteredUsersGroupKey(core), item.ItemKey, itemPermission.Id, AccessControlGrants.Allow);
                                        itemGrants.Add(newACG);
                                    }
                                    keysPosted.Add(User.GetRegisteredUsersGroupKey(core));
                                }

                                if (flag)
                                {
                                    // Only create if not exists
                                    if (!keysGranted.Contains(ppg.ItemKey))
                                    {
                                        AccessControlGrant newACG = AccessControlGrant.Create(core, ppg.ItemKey, item.ItemKey, itemPermission.Id, AccessControlGrants.Allow);
                                        itemGrants.Add(newACG);
                                    }
                                    keysPosted.Add(ppg.ItemKey);
                                }
                            }
                        }

                        if (!keysGranted.Contains(item.Owner.ItemKey))
                        {
                            AccessControlGrant newACG = AccessControlGrant.Create(core, item.Owner.ItemKey, item.ItemKey, itemPermission.Id, AccessControlGrants.Allow);
                            itemGrants.Add(newACG);
                        }
                        if (!keysPosted.Contains(item.Owner.ItemKey))
                        {
                            keysPosted.Add(item.Owner.ItemKey);
                        }

                        List<AccessControlGrant> grantsGrandfathered = new List<AccessControlGrant>();
                        foreach (AccessControlGrant grant in itemGrants)
                        {
                            if (grant.PermissionId == itemPermission.Id)
                            {
                                if (!keysPosted.Contains(grant.PrimitiveKey))
                                {
                                    grantsGrandfathered.Add(grant);
                                }
                            }
                        }

                        foreach (AccessControlGrant grant in grantsGrandfathered)
                        {
                            itemGrants.Remove(grant);
                            grant.Delete();
                        }
                    }

                    item.IsSimplePermissions = true;
                    item.Update();
                }
            }
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

            bool simple = item.IsSimplePermissions;

            string mode = core.Http.Form["aclmode"];
            switch (mode)
            {
                case "simple":
                    simple = true;
                    break;
                case "detailed":
                    simple = false;
                    break;
            }

            if (itemPermissions != null)
            {
                if (simple)
                {
                    //
                    // Simple
                    //

                    //HttpContext.Current.Response.Write("Simple<br />");
                    bool first = true;
                    PermissionTypes lastType = PermissionTypes.View;
                    List<PrimitivePermissionGroup> groups = null;

                    foreach (AccessControlPermission itemPermission in itemPermissions)
                    {
                        if (first || itemPermission.PermissionType != lastType)
                        {
                            groups = PermissionGroupSelectBox.FormPermissionGroups(core, "group-select-" + itemPermission.PermissionType.ToString());
                            //HttpContext.Current.Response.Write("Groups: " + groups.Count.ToString() + "<br />");

                            first = false;
                            lastType = itemPermission.PermissionType;
                        }

                        List<ItemKey> keysGranted = new List<ItemKey>();
                        foreach (AccessControlGrant grant in itemGrants)
                        {
                            if (grant.PermissionId == itemPermission.Id)
                            {
                                if (grant.Allow == AccessControlGrants.Allow)
                                {
                                    keysGranted.Add(grant.PrimitiveKey);
                                }
                            }
                        }

                        List<ItemKey> keysPosted = new List<ItemKey>();
                        foreach (PrimitivePermissionGroup ppg in groups)
                        {
                            // Only create if not exists
                            if (!keysGranted.Contains(ppg.ItemKey))
                            {
                                AccessControlGrant newACG = AccessControlGrant.Create(core, ppg.ItemKey, item.ItemKey, itemPermission.Id, AccessControlGrants.Allow);
                                itemGrants.Add(newACG);
                                //HttpContext.Current.Response.Write("Created<br />");
                            }
                            keysPosted.Add(ppg.ItemKey);
                        }

                        List<AccessControlGrant> grantsGrandfathered = new List<AccessControlGrant>();
                        foreach (AccessControlGrant grant in itemGrants)
                        {
                            if (grant.PermissionId == itemPermission.Id)
                            {
                                if (!keysPosted.Contains(grant.PrimitiveKey))
                                {
                                    grantsGrandfathered.Add(grant);
                                }
                            }
                        }

                        foreach (AccessControlGrant grant in grantsGrandfathered)
                        {
                            itemGrants.Remove(grant);
                            grant.Delete();
                            //HttpContext.Current.Response.Write("Deleted<br />");
                        }
                    }

                    item.IsSimplePermissions = true;
                    item.Update();
                }
                else
                {
                    //
                    // Detailed
                    //

                    foreach (AccessControlPermission itemPermission in itemPermissions)
                    {
                        SelectBox groupsSelectBox = BuildGroupsSelectBox(string.Format("new-permission-group[{0}]", itemPermission.Id), item.Owner);

                        foreach (SelectBoxItem gsbi in groupsSelectBox)
                        {
                            if (core.Http.Form[string.Format("new-grant[{0},{1}]", itemPermission.Id, gsbi.Key)] != null)
                            {
                                ItemKey ik = new ItemKey(gsbi.Key);

                                UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, ik, item.ItemKey, itemPermission.Id, AccessControlGrants.Inherit);

                                if (core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]"] != null)
                                {
                                    switch (core.Http.Form["allow[" + itemPermission.Id.ToString() + "," + ik.TypeId.ToString() + "," + ik.Id.ToString() + "]"])
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
                                    AccessControlGrant newACG = AccessControlGrant.Create(core, ik, item.ItemKey, itemPermission.Id, uacg.Allow);
                                    itemGrants.Add(newACG);
                                }
                                catch (InvalidAccessControlGrantException)
                                {
                                }
                            }
                        }


                    }
                    foreach (string key in core.Http.Form.AllKeys)
                    {
                        if (key.StartsWith("allow[", StringComparison.Ordinal) && key.EndsWith("]", StringComparison.Ordinal))
                        {
                            string[] parts = key.Substring(6, key.Length - 7).Split(new char[] { ',' });

                            if (parts.Length == 3)
                            {
                                long itemPermissionId = 0;
                                long primitiveKeyTypeId = 0;
                                long primitiveKeyId = 0;

                                long.TryParse(parts[0], out itemPermissionId);
                                long.TryParse(parts[1], out primitiveKeyTypeId);
                                long.TryParse(parts[2], out primitiveKeyId);

                                //HttpContext.Current.Response.Write("Reading perms key: " + key + "<br />");

                                ItemKey pk = new ItemKey(primitiveKeyId, primitiveKeyTypeId);

                                UnsavedAccessControlGrant uacg = new UnsavedAccessControlGrant(core, pk, item.ItemKey, itemPermissionId, AccessControlGrants.Inherit);

                                if (core.Http.Form[key] != null)
                                {
                                    switch (core.Http.Form[key])
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

                                foreach (AccessControlGrant grant in itemGrants)
                                {
                                    if (grant.ItemKey.Equals(uacg.ItemKey) && grant.PrimitiveKey.Equals(uacg.PrimitiveKey) && grant.PermissionId.Equals(uacg.PermissionId))
                                    {
                                        //HttpContext.Current.Response.Write("Found grant: " + key + "<br />");
                                        // We only want to trigger a database update if things have changed
                                        if (grant.Allow != uacg.Allow)
                                        {
                                            //HttpContext.Current.Response.Write("Saving perms key: " + key + ", " + uacg.Allow + "<br />");
                                            grant.Allow = uacg.Allow;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    item.IsSimplePermissions = false;
                    item.Update();
                }
            }
        }
    }
}
