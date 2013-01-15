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
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Access
    /// </summary>
    public class Access
    {
        private Core core;
        private Mysql db;

        private Primitive owner;
        private User viewer;

        private List<AccessControlGrant> grants;
        private List<long> permissionsEnacted;
        private Dictionary<string, AccessControlPermission> permissions;
        private Dictionary<string, bool> cachedPermissions;
        private IPermissibleItem item;
        private ItemKey itemKey;

        public Access(Core core, IPermissibleItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.core = core;
            this.item = item;
            this.owner = item.Owner;
            this.itemKey = item.ItemKey;
            this.viewer = core.Session.LoggedInMember;
        }

        internal Access(Core core, ItemKey key, IPermissibleItem leaf)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.core = core;
            this.item = null;
            this.owner = leaf.Owner;
            this.itemKey = key;
            this.viewer = core.Session.LoggedInMember;
        }

        internal Primitive Owner
        {
            get
            {
                return owner;
            }
        }

        internal User Viewer
        {
            get
            {
                return viewer;
            }
            set
            {
                viewer = value;
            }
        }

        internal ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        internal IPermissibleItem Item
        {
            get
            {
                if (item == null)
                {
                    item = (IPermissibleItem)NumberedItem.Reflect(core, ItemKey);
                }
                return item;
            }
        }

        private List<AccessControlGrant> Grants
        {
            get
            {
                if (grants == null)
                {
                    permissionsEnacted = new List<long>();

                    grants = core.AcessControlCache.GetGrants(this.ItemKey);

                    foreach (AccessControlGrant grant in grants)
                    {
                        if (!permissionsEnacted.Contains(grant.PermissionId))
                        {
                            permissionsEnacted.Add(grant.PermissionId);
                        }
                    }
                }
                return grants;
            }
        }

        public string AclUri
        {
            get
            {
                return BuildAclUri(core, item);
            }
        }

        public void CachePermissions()
        {
            FillPermissions();
        }

        private bool CachePermission(string permission, bool access)
        {
            if (cachedPermissions == null)
            {
                cachedPermissions = new Dictionary<string, bool>(StringComparer.Ordinal);
            }

            if (cachedPermissions.ContainsKey(permission))
            {
                cachedPermissions[permission] = access;
            }
            else
            {
                cachedPermissions.Add(permission, access);
            }

            return access;
        }

        public bool Can(string permission)
        {
            return Can(permission, (IPermissibleItem)item, false);
        }

        private bool Can(string permission, IPermissibleItem leaf, bool inherit)
        {
            bool allow = false;
            bool deny = false;
            long permissionId = 0;

            if (cachedPermissions != null && cachedPermissions.ContainsKey(permission))
            {
                return cachedPermissions[permission];
            }

            AccessControlPermission acp = null;

            if (permissions == null)
            {
                try
                {
                    acp = core.AcessControlCache[ItemKey.TypeId, permission];
                }
                catch (InvalidAccessControlPermissionException)
                {
                    acp = null;
                }
            }
            else
            {
                if (permissions.ContainsKey(permission))
                {
                    acp = permissions[permission];
                }
                else
                {
                    acp = null;
                }
            }

            if (acp == null && permission == "EDIT_PERMISSIONS")
            {
                return CachePermission(permission, Owner.CanEditPermissions());
            }

            // Fall back if no overriding permission attribute for edit exists in the database
            if (acp == null && permission == "EDIT")
            {
                return CachePermission(permission, Owner.CanEditItem());
            }

            // Fall back if no overriding permission attribute for delete exists in the database
            if (acp == null && permission == "DELETE")
            {
                return CachePermission(permission, Owner.CanDeleteItem());
            }

            if (acp == null)
            {
                if (inherit)
                {
                    return CachePermission(permission, false);
                }
                else
                {
                    /* Fall through to owner, this is useful where items can
                     * only have the same VIEW status as their parent (usually
                     * a Primitive). Normally you would call on the parent
                     * directly, however there are a few use cases where you
                     * don't know if the item has a separately defined VIEW
                     * permission or not.
                     */
                    if (ItemKey != Item.PermissiveParentKey)
                    {
                        Access parentAccess = new Access(core, Item.PermissiveParentKey, leaf);
                        return parentAccess.Can(permission, Item, true);
                        //return item.PermissiveParent.Access.Can(permission, item, true);
                    }
                    else
                    {
                        throw new InvalidAccessControlPermissionException(permission);
                    }
                }
            }

            permissionId = acp.Id;

            if (Grants != null)
            {
                foreach (AccessControlGrant grant in Grants)
                {
                    if (grant.PermissionId > 0 && grant.PermissionId == acp.Id)
                    {
                        core.PrimitiveCache.LoadPrimitiveProfile(grant.PrimitiveKey);
                    }
                }

                foreach (AccessControlGrant grant in Grants)
                {
                    if (grant.PermissionId > 0 && grant.PermissionId == acp.Id)
                    {
                        if (owner != null)
                        {
                            if (owner.GetIsMemberOfPrimitive(viewer, grant.PrimitiveKey))
                            {
                                switch (grant.Allow)
                                {
                                    case AccessControlGrants.Allow:
                                        allow = true;
                                        break;
                                    case AccessControlGrants.Deny:
                                        deny = true;
                                        break;
                                    case AccessControlGrants.Inherit:
                                        break;
                                }
                            }
                            if (grant.PrimitiveKey.Equals(User.CreatorKey) && viewer != null && owner.ItemKey.Equals(viewer.ItemKey))
                            {
                                switch (grant.Allow)
                                {
                                    case AccessControlGrants.Allow:
                                        allow = true;
                                        break;
                                    case AccessControlGrants.Deny:
                                        deny = true;
                                        break;
                                    case AccessControlGrants.Inherit:
                                        break;
                                }
                            }
                        }
                        if (Item.IsItemGroupMember(viewer, grant.PrimitiveKey))
                        {
                            switch (grant.Allow)
                            {
                                case AccessControlGrants.Allow:
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    deny = true;
                                    break;
                                case AccessControlGrants.Inherit:
                                    break;
                            }
                        }
                        if (grant.PrimitiveKey.Equals(User.RegisteredUsersGroupKey) && viewer != null)
                        {
                            switch (grant.Allow)
                            {
                                case AccessControlGrants.Allow:
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    deny = true;
                                    break;
                                case AccessControlGrants.Inherit:
                                    break;
                            }
                        }
                        if (grant.PrimitiveKey.Equals(User.EveryoneGroupKey))
                        {
                            switch (grant.Allow)
                            {
                                case AccessControlGrants.Allow:
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    deny = true;
                                    break;
                                case AccessControlGrants.Inherit:
                                    break;
                            }
                        }
                    }
                }
            }

            if (Grants == null || Grants.Count == 0 || (!permissionsEnacted.Contains(permissionId)))
            {
                if (owner == null && viewer != null)
                {
                    if (viewer.ItemKey.Equals(leaf.ItemKey))
                    {
                        return CachePermission(permission, true);
                    }
                    else
                    {
                        return CachePermission(permission, leaf.GetDefaultCan(permission));
                    }
                }
                else if (ItemKey.Equals(owner.ItemKey))
                {
                    if (viewer != null && owner.ItemKey.Equals(viewer.ItemKey))
                    {
                        return CachePermission(permission, true);
                    }
                    else
                    {
                        return CachePermission(permission, leaf.GetDefaultCan(permission));
                    }
                }
                else
                {
                    if ((typeof(INestableItem).IsAssignableFrom(ItemKey.Type)))
                    {
                        INestableItem ni = (INestableItem)Item;
                        ParentTree parents = ni.GetParents();

                        if (parents == null || parents.Nodes.Count == 0)
                        {
                            if (Item.PermissiveParentKey == null)
                            {
                                return CachePermission(permission, Owner.Access.Can(permission, leaf, true));
                            }
                            else
                            {
                                Access parentAccess = new Access(core, Item.PermissiveParentKey, leaf);
                                return CachePermission(permission, parentAccess.Can(Item.ParentPermissionKey(Item.PermissiveParentKey.Type, permission), leaf, true));
                                //return CachePermission(permission, item.PermissiveParent.Access.Can(item.ParentPermissionKey(item.PermissiveParentKey.Type, permission), leaf, true));
                            }
                        }
                        else
                        {
                            Access parentAccess = new Access(core, new ItemKey(parents.Nodes[parents.Nodes.Count - 1].ParentId, ni.ParentTypeId), leaf);
                            return CachePermission(permission, parentAccess.Can(permission, leaf, true));
                            //return CachePermission(permission, ((IPermissibleItem)NumberedItem.Reflect(core, new ItemKey(parents.Nodes[parents.Nodes.Count - 1].ParentId, ni.ParentTypeId))).Access.Can(permission, leaf, true));
                        }
                    }
                    else
                    {
                        if (Item.PermissiveParentKey == null)
                        {
                            return CachePermission(permission, Owner.Access.Can(permission, leaf, true));
                        }
                        else
                        {
                            Access parentAccess = new Access(core, Item.PermissiveParentKey, leaf);
                            return CachePermission(permission, parentAccess.Can(Item.ParentPermissionKey(Item.PermissiveParentKey.Type, permission), leaf, true));
                            //return CachePermission(permission, item.PermissiveParent.Access.Can(item.ParentPermissionKey(item.PermissiveParent.GetType(), permission), leaf, true));
                        }
                    }
                }
            }

            return CachePermission(permission, (allow && (!deny)));
        }

        public static string BuildAclUri(Core core, IPermissibleItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", item.Id, item.ItemKey.TypeId), true);
        }

        public static string BuildAclUri(Core core, IPermissibleItem item, bool simple)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Uri.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}&aclmode={2}", item.Id, item.ItemKey.TypeId, (simple ? "simple" : "detailed")), true);
        }

        public static void LoadGrants(Core core, List<IPermissibleItem> items)
        {
        }

        public static void CreateAllGrantsForOwner(Core core, IPermissibleItem item)
        {
            List<AccessControlPermission> permissions = AccessControlPermission.GetPermissions(core, item);

            foreach (AccessControlPermission permission in permissions)
            {
                AccessControlGrant.Create(core, item.Owner.ItemKey, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public static void CreateDefaultSimpleGrantsForOthers(Core core, IPermissibleItem item)
        {
            List<AccessControlPermission> permissions = AccessControlPermission.GetPermissions(core, item);

            foreach (AccessControlPermission permission in permissions)
            {
                // View Permissions
                if (permission.PermissionType == PermissionTypes.View)
                {
                    //AccessControlGrant.Create(core, item.Owner.ItemKey, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
                }
                // Interact Permissions
                if (permission.PermissionType == PermissionTypes.Interact)
                {
                    //AccessControlGrant.Create(core, item.Owner.ItemKey, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
                }
            }
        }

        public static void CreateGrantForPrimitive(Core core, IPermissibleItem item, ItemKey grantee, params string[] permissionNames)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = AccessControlPermission.GetSelectQueryStub(typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", item.ItemKey.TypeId);
            query.AddCondition("permission_name", ConditionEquality.In, permissionNames);
            query.AddSort(SortOrder.Ascending, "permission_type");

            DataTable permissionDataTable = core.Db.Query(query);

            foreach (DataRow dr in permissionDataTable.Rows)
            {
                AccessControlPermission permission = new AccessControlPermission(core, dr);
                AccessControlGrant.Create(core, grantee, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public static void CreateGrantForPrimitive(Core core, long itemTypeId, long itemId, ItemKey grantee, params string[] permissionNames)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = AccessControlPermission.GetSelectQueryStub(typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", itemTypeId);
            query.AddCondition("permission_name", ConditionEquality.In, permissionNames);
            query.AddSort(SortOrder.Ascending, "permission_type");

            DataTable permissionDataTable = core.Db.Query(query);

            foreach (DataRow dr in permissionDataTable.Rows)
            {
                AccessControlPermission permission = new AccessControlPermission(core, dr);
                AccessControlGrant.Create(core, grantee, new ItemKey(itemId, itemTypeId), permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        private void FillPermissions()
        {
            if (permissions == null)
            {
                List<AccessControlPermission> permissionsList = AccessControlPermission.GetPermissions(core, item);
                permissions = new Dictionary<string, AccessControlPermission>(StringComparer.Ordinal);

                foreach (AccessControlPermission permission in permissionsList)
                {
                    permissions.Add(permission.Name, permission);
                }
            }
        }

        public void CreateAllGrantsForOwner()
        {
            FillPermissions();

            foreach (AccessControlPermission permission in permissions.Values)
            {
                AccessControlGrant.Create(core, Owner.ItemKey, ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public void CreateAllGrantsForPrimitive(ItemKey grantee)
        {
            FillPermissions();

            foreach (AccessControlPermission permission in permissions.Values)
            {
                AccessControlGrant.Create(core, grantee, ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public void CreateGrantForPrimitive(ItemKey grantee, params string[] permissionNames)
        {
            FillPermissions();

            foreach (string permissionName in permissionNames)
            {
                AccessControlPermission permission = permissions[permissionName];
                AccessControlGrant.Create(core, grantee, ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }
    }
}
