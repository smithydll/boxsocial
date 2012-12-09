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
using System.Reflection;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public struct AccessControlPermissionKey
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
    }

    public struct AccessControlGrantKey
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
    }

    public class AccessControlCache
    {
        private Core core;
        private static Object permissionCacheLock = new Object();
        private static Dictionary<AccessControlPermissionKey, AccessControlPermission> permissionCache = null;
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
            grantsCache = new Dictionary<AccessControlGrantKey, List<AccessControlGrant>>();
        }

        public AccessControlPermission this[long typeId, string permission]
        {
            get
            {
                AccessControlPermissionKey key = new AccessControlPermissionKey(typeId, permission);

                lock (permissionCacheLock)
                {
                    if (permissionCache.ContainsKey(key))
                    {
                        return permissionCache[key];
                    }
                }

                AccessControlPermission p = new AccessControlPermission(core, typeId, permission);
                permissionCache.Add(key, p);

                return p;
            }
        }

        public void CacheGrants(List<IPermissibleItem> items)
        {
            long typeId = 0;
            Dictionary<long, AccessControlGrantKey> keys = new Dictionary<long, AccessControlGrantKey>();

            foreach (IPermissibleItem item in items)
            {
                typeId = item.ItemKey.TypeId;
                keys.Add(item.Id, new AccessControlGrantKey(typeId, item.Id));
            }

            List<AccessControlGrant> grants = AccessControlGrant.GetGrants(core, items);

            foreach (AccessControlGrant grant in grants)
            {
                AccessControlGrantKey key = keys[grant.ItemKey.Id];
                if (!grantsCache.ContainsKey(key))
                {
                    grantsCache.Add(key, new List<AccessControlGrant>());
                }
                grantsCache[key].Add(grant);
            }

            foreach (IPermissibleItem item in items)
            {
                AccessControlGrantKey key = keys[item.ItemKey.Id];
                if (!grantsCache.ContainsKey(key))
                {
                    grantsCache.Add(key, new List<AccessControlGrant>());
                }
            }
        }

        public List<AccessControlGrant> GetGrants(IPermissibleItem item)
        {
            AccessControlGrantKey key = new AccessControlGrantKey(item.ItemKey.TypeId, item.Id);

            if (grantsCache.ContainsKey(key))
            {
                return grantsCache[key];
            }

            List<AccessControlGrant> g = AccessControlGrant.GetGrants(core, item);
            grantsCache.Add(key, g);

            return g;
        }
    }
}
