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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public struct AccessControlPermissionKey : IComparable
    {
        long typeId;
        string permission;

        public long TypeId
        {
            get
            {
                return typeId;
            }
        }

        public string Permission
        {
            get
            {
                return permission;
            }
        }

        public AccessControlPermissionKey(long typeId, string permission)
        {
            this.typeId = typeId;
            this.permission = permission;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(AccessControlPermissionKey)) return false;
            AccessControlPermissionKey acpk = (AccessControlPermissionKey)obj;

            if (typeId != acpk.typeId)
                return false;
            if (permission != acpk.permission)
                return false;
            return true;
        }

        public int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(AccessControlPermissionKey))
            {
                return -1;
            }

            AccessControlPermissionKey key = (AccessControlPermissionKey)obj;

            int c = key.TypeId.CompareTo(TypeId);
            if (c == 0)
            {
                return key.Permission.CompareTo(Permission);
            }
            else
            {
                return c;
            }
        }
    }

    public struct AccessControlGrantKey : IComparable
    {
        long typeId;
        long itemId;

        public long TypeId
        {
            get
            {
                return typeId;
            }
        }

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public AccessControlGrantKey(long typeId, long itemId)
        {
            this.typeId = typeId;
            this.itemId = itemId;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(AccessControlGrantKey)) return false;
            AccessControlGrantKey acgk = (AccessControlGrantKey)obj;

            if (typeId != acgk.typeId)
                return false;
            if (itemId != acgk.itemId)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return TypeId.GetHashCode() ^ ItemId.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(AccessControlGrantKey))
            {
                return -1;
            }

            int c = ((AccessControlGrantKey)obj).TypeId.CompareTo(TypeId);
            if (c == 0)
            {
                return ((AccessControlGrantKey)obj).ItemId.CompareTo(ItemId);
            }
            else
            {
                return c;
            }
        }
    }

    public class AccessControlCache
    {
        private Core core;
        private static Object permissionCacheLock = new Object();
        private static Dictionary<AccessControlPermissionKey, AccessControlPermission> permissionCache = null;
        private static Object grantsCacheLock = new Object();
        private static Dictionary<AccessControlGrantKey, List<AccessControlGrant>> grantsCache = null;

        public AccessControlCache(Core core)
        {
            this.core = core;

            lock (permissionCacheLock)
            {
                if (permissionCache == null || permissionCache.Count > 10000)
                {
                    permissionCache = new Dictionary<AccessControlPermissionKey, AccessControlPermission>();
                }
            }
            lock (grantsCacheLock)
            {
                grantsCache = new Dictionary<AccessControlGrantKey, List<AccessControlGrant>>();
            }
        }

        public AccessControlPermission this[long typeId, string permission]
        {
            get
            {
                AccessControlPermissionKey key = new AccessControlPermissionKey(typeId, permission);

                lock (permissionCacheLock)
                {
                    AccessControlPermission acp = null;
                    if (permissionCache.TryGetValue(key, out acp))
                    {
                        return acp.Clone();
                    }

                    /*if (permissionCache.ContainsKey(key))
                    {
                        return permissionCache[key];
                    }*/
                }

                AccessControlPermission p = new AccessControlPermission(core, typeId, permission);

                lock (permissionCacheLock)
                {
                    if (!permissionCache.ContainsKey(key))
                    {
                        permissionCache.Add(key, p);
                    }
                }

                return p;
            }
        }

        public void CacheGrants(List<IPermissibleItem> items)
        {
            Dictionary<ItemKey, AccessControlGrantKey> keys = new Dictionary<ItemKey, AccessControlGrantKey>();

            foreach (IPermissibleItem item in items)
            {
                if (!keys.ContainsKey(item.ItemKey))
                {
                    keys.Add(item.ItemKey, new AccessControlGrantKey(item.ItemKey.TypeId, item.ItemKey.Id));
                }
            }

            List<AccessControlGrant> grants = AccessControlGrant.GetGrants(core, items);

            foreach (AccessControlGrant grant in grants)
            {
                AccessControlGrantKey key = keys[grant.ItemKey];

                lock (grantsCacheLock)
                {
                    if (!grantsCache.ContainsKey(key))
                    {
                        grantsCache.Add(key, new List<AccessControlGrant>());
                    }
                    grantsCache[key].Add(grant);
                }
            }

            foreach (IPermissibleItem item in items)
            {
                AccessControlGrantKey key = keys[item.ItemKey];
                lock (grantsCacheLock)
                {
                    if (!grantsCache.ContainsKey(key))
                    {
                        grantsCache.Add(key, new List<AccessControlGrant>());
                    }
                }
            }
        }

        public List<AccessControlGrant> GetGrants(IPermissibleItem item)
        {
            return GetGrants(item.ItemKey);
        }

        internal List<AccessControlGrant> GetGrants(ItemKey itemKey)
        {
            AccessControlGrantKey key = new AccessControlGrantKey(itemKey.TypeId, itemKey.Id);

            List<AccessControlGrant> acgs2 = new List<AccessControlGrant>();
            bool success = false;
            lock (grantsCacheLock)
            {
                List<AccessControlGrant> acgs = null;
                if (grantsCache.TryGetValue(key, out acgs))
                {
                    success = true;
                    foreach (AccessControlGrant grant in acgs)
                    {
                        acgs2.Add(grant.Clone());
                    }
                }

                /*if (grantsCache.ContainsKey(key))
                {
                    return grantsCache[key];
                }*/
            }
            if (success)
            {
                return acgs2;
            }

            List<AccessControlGrant> g = AccessControlGrant.GetGrants(core, itemKey);
            List<AccessControlGrant> g2 = new List<AccessControlGrant>();
            lock (grantsCacheLock)
            {
                if (!grantsCache.ContainsKey(key))
                {
                    foreach (AccessControlGrant grant in g)
                    {
                        g2.Add(grant);
                    }
                    grantsCache.Add(key, g2);
                }
            }

            return g;
        }
    }
}
