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
    public class PermissionCacheKey : IComparable
    {
        private string permission;
        private ItemKey viewer;

        public string Permission
        {
            get
            {
                return permission;
            }
        }

        public ItemKey Viewer
        {
            get
            {
                return viewer;
            }
        }

        public PermissionCacheKey(string permission, ItemKey viewer)
        {
            this.permission = permission;
            this.viewer = viewer;
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj.GetType() != typeof(PermissionCacheKey))
            {
                return -1;
            }

            PermissionCacheKey key = (PermissionCacheKey)obj;

            if (permission == key.Permission)
            {
                return viewer.Id.CompareTo(key.Viewer.Id);
            }
            else
            {
                return permission.CompareTo(key.Permission);
            }
        }

        public override int GetHashCode()
        {
            return permission.GetHashCode() ^ viewer.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(PermissionCacheKey)) return false;
            PermissionCacheKey key = (PermissionCacheKey)obj;

            if (permission != key.Permission)
                return false;
            if (viewer.Id != key.Viewer.Id)
                return false;
            return true;
        }
    }

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
        private Dictionary<PermissionCacheKey, bool> cachedPermissions;
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

        internal Access(Core core, ItemKey key, Primitive leafOwner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.core = core;
            this.item = null;
            this.owner = leafOwner;
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
                    core.ItemCache.RequestItem(ItemKey);
                    item = (IPermissibleItem)core.ItemCache[ItemKey];
                    //item = (IPermissibleItem)NumberedItem.Reflect(core, ItemKey);
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

        private bool CachePermission(string permission, ItemKey viewer, bool access)
        {
            if (cachedPermissions == null)
            {
                cachedPermissions = new Dictionary<PermissionCacheKey, bool>(8);
            }

            PermissionCacheKey key = new PermissionCacheKey(permission, viewer);

            if (cachedPermissions.ContainsKey(key))
            {
                cachedPermissions[key] = access;
            }
            else
            {
                cachedPermissions.Add(key, access);
            }

            return access;
        }

        public bool IsPublic()
        {
            //HttpContext.Current.Response.Write("<br />" + item.ItemKey.TypeString + ", " + item.Id + ", PUBLIC " + User.EveryoneGroupKey.Id + ", " + (Grants != null ? Grants.Count : 0) + " ");

            return Can("VIEW", item.ItemKey, false, User.EveryoneGroupKey);
        }

        public bool IsPrivateFriendsOrMembers()
        {
            return Can("VIEW", item.ItemKey, false, Friend.FriendsGroupKey);
        }

        public bool Can(string permission)
        {
            ItemKey viewer = User.EveryoneGroupKey;
            if (Viewer != null)
            {
                viewer = Viewer.ItemKey;
            }
            //HttpContext.Current.Response.Write("<br />" + item.ItemKey.TypeString + ", " + item.Id + ", " + viewer.Id + ", " + (Grants != null ? Grants.Count : 0) + " ");

            return Can(permission, item.ItemKey, false, viewer);
        }

        private bool Can(string permission, ItemKey leaf, bool inherit, ItemKey viewer)
        {
            bool allow = false;
            bool deny = false;
            long permissionId = 0;

            PermissionCacheKey cachedKey = new PermissionCacheKey(permission, viewer);

            if (cachedPermissions != null && cachedPermissions.ContainsKey(cachedKey))
            {
                //HttpContext.Current.Response.Write("<br />cached result ");
                return cachedPermissions[cachedKey];
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
                return CachePermission(permission, viewer, Owner.CanEditPermissions());
            }

            // Fall back if no overriding permission attribute for edit exists in the database
            if (acp == null && permission == "EDIT")
            {
                return CachePermission(permission, viewer, Owner.CanEditItem());
            }

            // Fall back if no overriding permission attribute for delete exists in the database
            if (acp == null && permission == "DELETE")
            {
                return CachePermission(permission, viewer, Owner.CanDeleteItem());
            }

            if (acp == null)
            {
                if (inherit)
                {
                    return CachePermission(permission, viewer, false);
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
                    if (ItemKey != Item.PermissiveParentKey && Item.PermissiveParentKey != null)
                    {
                        Access parentAccess = new Access(core, Item.PermissiveParentKey, Owner);
                        //HttpContext.Current.Response.Write("parent result null acp");
                        return parentAccess.Can(permission, itemKey, true, viewer);
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
                            if (grant.PrimitiveKey.TypeId == ItemType.GetTypeId(typeof(User)) && viewer.Id > 0 && grant.PrimitiveKey.Id == viewer.Id)
                            {
                                switch (grant.Allow)
                                {
                                    case AccessControlGrants.Allow:
                                        //HttpContext.Current.Response.Write(" allow User");
                                        allow = true;
                                        break;
                                    case AccessControlGrants.Deny:
                                        //HttpContext.Current.Response.Write(" deny User");
                                        deny = true;
                                        break;
                                    case AccessControlGrants.Inherit:
                                        break;
                                }
                            }
                            if (owner.GetIsMemberOfPrimitive(viewer, grant.PrimitiveKey))
                            {
                                switch (grant.Allow)
                                {
                                    case AccessControlGrants.Allow:
                                        //HttpContext.Current.Response.Write(" allow GetIsMemberOfPrimitive");
                                        allow = true;
                                        break;
                                    case AccessControlGrants.Deny:
                                        //HttpContext.Current.Response.Write(" deny GetIsMemberOfPrimitive");
                                        deny = true;
                                        break;
                                    case AccessControlGrants.Inherit:
                                        break;
                                }
                            }
                            if (grant.PrimitiveKey.Equals(User.CreatorKey) && viewer != null && owner.ItemKey.Equals(viewer))
                            {
                                switch (grant.Allow)
                                {
                                    case AccessControlGrants.Allow:
                                        //HttpContext.Current.Response.Write(" allow Creator");
                                        allow = true;
                                        break;
                                    case AccessControlGrants.Deny:
                                        //HttpContext.Current.Response.Write(" deny Creator");
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
                                    //HttpContext.Current.Response.Write(" allow IsItemGroupMember");
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    //HttpContext.Current.Response.Write(" deny IsItemGroupMember");
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
                                    //HttpContext.Current.Response.Write(" allow User.RegisteredUsersGroupKey");
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    //HttpContext.Current.Response.Write(" deny User.RegisteredUsersGroupKey");
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
                                    //HttpContext.Current.Response.Write(" allow User.EveryoneGroupKey");
                                    allow = true;
                                    break;
                                case AccessControlGrants.Deny:
                                    //HttpContext.Current.Response.Write(" deny User.EveryoneGroupKey");
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
                IPermissibleItem leafItem = null;
                //HttpContext.Current.Response.Write(" fall back");
                if (owner == null && viewer != null)
                {
                    if (viewer.Equals(leaf))
                    {
                        //HttpContext.Current.Response.Write(" cached result 0x02");
                        return CachePermission(permission, viewer, true);
                    }
                    else
                    {
                        //HttpContext.Current.Response.Write(" cached result 0x03");
                        if (leafItem == null)
                        {
                            leafItem = (IPermissibleItem)NumberedItem.Reflect(core, leaf);
                        }
                        return CachePermission(permission, viewer, leafItem.GetDefaultCan(permission, viewer));
                    }
                }
                else if (ItemKey.Equals(owner.ItemKey))
                {
                    if (viewer != null && owner.ItemKey.Equals(viewer))
                    {
                        //HttpContext.Current.Response.Write(" cached result 0x04");
                        return CachePermission(permission, viewer, true);
                    }
                    else
                    {
                        //HttpContext.Current.Response.Write(" cached result 0x05");
                        if (leafItem == null)
                        {
                            leafItem = (IPermissibleItem)NumberedItem.Reflect(core, leaf);
                        }
                        return CachePermission(permission, viewer, leafItem.GetDefaultCan(permission, viewer));
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
                                //HttpContext.Current.Response.Write(" cached result 0x06");
                                return CachePermission(permission, viewer, Item.GetDefaultCan(permission, viewer)); //Owner.Access.Can(permission, leaf, true, viewer));
                            }
                            else
                            {
                                Access parentAccess = new Access(core, Item.PermissiveParentKey, Owner);
                                //HttpContext.Current.Response.Write(" cached result 0x07");
                                return CachePermission(permission, viewer, parentAccess.Can(Item.ParentPermissionKey(Item.PermissiveParentKey.Type, permission), leaf, true, viewer));
                                //return CachePermission(permission, item.PermissiveParent.Access.Can(item.ParentPermissionKey(item.PermissiveParentKey.Type, permission), leaf, true));
                            }
                        }
                        else
                        {
                            Access parentAccess = new Access(core, new ItemKey(parents.Nodes[parents.Nodes.Count - 1].ParentId, ni.ParentTypeId), Owner);
                            //HttpContext.Current.Response.Write(" cached result 0x08");
                            return CachePermission(permission, viewer, parentAccess.Can(permission, leaf, true, viewer));
                            //return CachePermission(permission, ((IPermissibleItem)NumberedItem.Reflect(core, new ItemKey(parents.Nodes[parents.Nodes.Count - 1].ParentId, ni.ParentTypeId))).Access.Can(permission, leaf, true));
                        }
                    }
                    else
                    {
                        if (Item.PermissiveParentKey == null)
                        {
                            //HttpContext.Current.Response.Write(" cached result 0x09");
                            return CachePermission(permission, viewer, Item.GetDefaultCan(permission, viewer)); //Owner.Access.Can(permission, leaf, true, viewer));
                        }
                        else
                        {
                            Access parentAccess = new Access(core, Item.PermissiveParentKey, Owner);
                            //HttpContext.Current.Response.Write(" cached result 0x10");
                            return CachePermission(permission, viewer, parentAccess.Can(Item.ParentPermissionKey(Item.PermissiveParentKey.Type, permission), leaf, true, viewer));
                            //return CachePermission(permission, item.PermissiveParent.Access.Can(item.ParentPermissionKey(item.PermissiveParent.GetType(), permission), leaf, true));
                        }
                    }
                }
            }

            //HttpContext.Current.Response.Write(" cached result 0x10 " + allow.ToString() + ", " + (allow && (!deny)).ToString());
            return CachePermission(permission, viewer, (allow && (!deny)));
        }

        public static string BuildAclUri(Core core, IPermissibleItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", item.Id, item.ItemKey.TypeId), true);
        }

        public static string BuildAclUri(Core core, IPermissibleItem item, bool simple)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}&aclmode={2}", item.Id, item.ItemKey.TypeId, (simple ? "simple" : "detailed")), true);
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
                permissions = new Dictionary<string, AccessControlPermission>(8, StringComparer.Ordinal);

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
