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
        private Dictionary<string, AccessControlPermission> permissions;
        private IPermissibleItem item;

        public Access(Core core, IPermissibleItem item)
        {
            this.core = core;
            this.item = item;
            this.owner = item.Owner;
            this.viewer = core.session.LoggedInMember;
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

        private List<AccessControlGrant> Grants
        {
            get
            {
                if (grants == null)
                {
                    grants = AccessControlGrant.GetGrants(core, this.item);
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

        public bool Can(string permission)
        {
            return Can(permission, (IPermissibleItem)item, false);
        }

        private bool Can(string permission, IPermissibleItem leaf, bool inherit)
        {
            bool allow = false;
            bool deny = false;

            AccessControlPermission acp = null;

            try
            {
                acp = new AccessControlPermission(core, item.ItemKey.TypeId, permission);
            }
            catch (InvalidAccessControlPermissionException)
            {
                acp = null;
            }

            if (acp == null && permission == "EDIT_PERMISSIONS")
            {
                IPermissibleItem pi = (IPermissibleItem)this.item;

                return pi.Owner.CanEditPermissions();
            }

            if (acp == null)
            {
                if (inherit)
                {
                    return false;
                }
                else
                {
                    throw new InvalidAccessControlPermissionException();
                }
            }

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
                        }
                    }
                }
            }

            if (Grants == null || Grants.Count == 0)
            {
                if (owner == null && viewer != null)
                {
                    if (viewer.ItemKey.Equals(leaf.ItemKey))
                    {
                        return true;
                    }
                    else
                    {
                        return leaf.GetDefaultCan(permission);
                    }
                }
                else if (item.ItemKey.Equals(owner.ItemKey))
                {
                    if (viewer != null && owner.ItemKey.Equals(viewer.ItemKey))
                    {
                        return true;
                    }
                    else
                    {
                        return leaf.GetDefaultCan(permission);
                    }
                }
                else
                {
                    if (item is INestableItem)
                    {
                        INestableItem ni = (INestableItem)item;
                        ParentTree parents = ni.GetParents();

                        if (parents == null || parents.Nodes.Count == 0)
                        {
                            if (item.PermissiveParent == null)
                            {
                                return owner.Access.Can(permission, leaf, true);
                            }
                            else
                            {
                                return item.PermissiveParent.Access.Can(permission, leaf, true);
                            }
                        }
                        else
                        {
                            return ((IPermissibleItem)NumberedItem.Reflect(core, new ItemKey(parents.Nodes[parents.Nodes.Count - 1].ParentId, ni.ParentTypeId))).Access.Can(permission, leaf, true);
                        }
                    }
                    else
                    {
                        return owner.Access.Can(permission, leaf, true);
                    }
                }
            }

            return (allow && (!deny));
        }

        public static string BuildAclUri(Core core, IPermissibleItem item)
        {
            return core.Uri.AppendAbsoluteSid(string.Format("acl.aspx?id={0}&type={1}", item.Id, item.ItemKey.TypeId), true);
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

        public static void CreateGrantForPrimitive(Core core, IPermissibleItem item, ItemKey grantee, params string[] permissionNames)
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(AccessControlPermission));
            query.AddCondition("permission_item_type_id", item.ItemKey.TypeId);
            query.AddCondition("permission_name", ConditionEquality.In, permissionNames);

            DataTable permissionDataTable = core.db.Query(query);

            foreach (DataRow dr in permissionDataTable.Rows)
            {
                AccessControlPermission permission = new AccessControlPermission(core, dr);
                AccessControlGrant.Create(core, grantee, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        private void FillPermissions()
        {
            if (permissions == null)
            {
                List<AccessControlPermission> permissionsList = AccessControlPermission.GetPermissions(core, item);
                permissions = new Dictionary<string, AccessControlPermission>();

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
                AccessControlGrant.Create(core, item.Owner.ItemKey, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public void CreateAllGrantsForPrimitive(ItemKey grantee)
        {
            FillPermissions();

            foreach (AccessControlPermission permission in permissions.Values)
            {
                AccessControlGrant.Create(core, grantee, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }

        public void CreateGrantForPrimitive(ItemKey grantee, params string[] permissionNames)
        {
            FillPermissions();

            foreach (string permissionName in permissionNames)
            {
                AccessControlPermission permission = permissions[permissionName];
                AccessControlGrant.Create(core, grantee, item.ItemKey, permission.PermissionId, AccessControlGrants.Allow);
            }
        }
    }
}
