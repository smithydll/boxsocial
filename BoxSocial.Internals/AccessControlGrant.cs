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
	/*
	 * A primitive is granted permission to use an item with an allowance level. 
	 */
	
	public enum AccessControlGrants : sbyte
	{
		Deny = -1,
		Inherit = 0,
		Allow = 1,
	}
	
	[DataTable("account_control_grants")]
	public class AccessControlGrant : Item
	{
		[DataField("grant_primitive", DataFieldKeys.Unique, "u_key")]
		private ItemKey primitiveKey;
        [DataFieldKey(DataFieldKeys.Index, "i_grant")]
		[DataField("grant_item_id", DataFieldKeys.Unique, "u_key")] //new UniqueKey("u_key"), new Index("i_grant"))]
		private long itemId;
        [DataFieldKey(DataFieldKeys.Index, "i_grant")]
        [DataField("grant_item_type_id", DataFieldKeys.Unique, "u_key")] //new UniqueKey("u_key"), new Index("i_grant"))]
		private long itemTypeId;
		[DataField("grant_permission_id", DataFieldKeys.Unique, "u_key")]
		private long permissionId;
		[DataField("grant_allow")]
		private sbyte grantAllow;
		
		private IPermissibleItem item;
		private Primitive owner;
        private ItemKey itemKey;

        public ItemKey ItemKey
        {
            get
            {
                if (itemKey == null)
                {
                    itemKey = new ItemKey(itemId, itemTypeId);
                }

                return itemKey;
            }
        }

        public ItemKey PrimitiveKey
        {
            get
            {
                return primitiveKey;
            }
        }

        public long PermissionId
        {
            get
            {
                return permissionId;
            }
        }

        public AccessControlGrants Allow
        {
            get
            {
                return (AccessControlGrants)grantAllow;
            }
        }
		
		public Primitive Owner
        {
            get
            {
                if (owner == null || primitiveKey.Id != owner.Id || primitiveKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(primitiveKey);
                    owner = core.PrimitiveCache[primitiveKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

		
		internal AccessControlGrant(Core core, IPermissibleItem item, DataRow grantRow)
			: base(core)
		{
			this.item = item;
			ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
		}
        
        private AccessControlGrant(Core core, ItemKey primitive, ItemKey item, long permissionId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            SelectQuery query = new SelectQuery(typeof(AccessControlGrant));
            
            query.AddCondition("grant_primitive_id", primitive.Id);
            query.AddCondition("grant_primitive_type_id", primitive.TypeId);
            query.AddCondition("grant_item_id", item.Id);
            query.AddCondition("grant_item_type_id", item.TypeId);
            query.AddCondition("grant_permission_id", permissionId);
            
            DataTable grantDataTable = core.Db.Query(query);
            
            if (grantDataTable.Rows.Count == 1)
            {
                try
                {
                    loadItemInfo(grantDataTable.Rows[0]);
                }
                catch (InvalidItemException)
                {
                    AccessControlPermission acp = new AccessControlPermission(core, permissionId);
                    throw new InvalidAccessControlGrantException(acp.Name);
                }
            }
            else
            {
                AccessControlPermission acp = new AccessControlPermission(core, permissionId);
                throw new InvalidAccessControlGrantException(acp.Name);
            }
        }
		
		private void AccessControlGrant_ItemLoad()
        {
        }

		
		// Cannot use the built-in for un-numbered stuffs 
		public static AccessControlGrant Create(Core core, ItemKey primitive, ItemKey item, long permissionId, AccessControlGrants allow)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			InsertQuery iQuery = new InsertQuery(typeof(AccessControlGrant));
            
            iQuery.AddField("grant_primitive_id", primitive.Id);
            iQuery.AddField("grant_primitive_type_id", primitive.TypeId);
            iQuery.AddField("grant_item_id", item.Id);
            iQuery.AddField("grant_item_type_id", item.TypeId);
            iQuery.AddField("grant_permission_id", permissionId);
            iQuery.AddField("grant_allow", (sbyte)allow);
            
            core.Db.Query(iQuery);
			
			return new AccessControlGrant(core, primitive, item, permissionId);
		}
		
		public bool Can(User user)
		{
			bool isDenied = false;
			bool isAllowed = false;
			
			return (!isDenied && isAllowed);
		}

        public static List<AccessControlGrant> GetGrants(Core core, List<IPermissibleItem> items)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            Dictionary<long, IPermissibleItem> itemDictionary = new Dictionary<long, IPermissibleItem>();
            List<long> itemIds = new List<long>();
            long itemTypeId = 0;

            foreach (IPermissibleItem item in items)
            {
                if (itemTypeId == item.ItemKey.TypeId || itemTypeId == 0)
                {
                    itemIds.Add(item.ItemKey.Id);
                    itemTypeId = item.ItemKey.TypeId;
                    itemDictionary.Add(item.ItemKey.Id, item);
                }
            }

            SelectQuery sQuery = Item.GetSelectQueryStub(typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", ConditionEquality.In, itemIds);
            sQuery.AddCondition("grant_item_type_id", itemTypeId);

            DataTable grantsTable = core.Db.Query(sQuery);

            foreach (DataRow dr in grantsTable.Rows)
            {
                grants.Add(new AccessControlGrant(core, itemDictionary[(long)dr["grant_item_id"]], dr));
            }

            return grants;
        }
		
		public static List<AccessControlGrant> GetGrants(Core core, IPermissibleItem item)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			List<AccessControlGrant> grants = new List<AccessControlGrant>();
			
			SelectQuery sQuery = Item.GetSelectQueryStub(typeof(AccessControlGrant));
			sQuery.AddCondition("grant_item_id", item.ItemKey.Id);
			sQuery.AddCondition("grant_item_type_id", item.ItemKey.TypeId);
			
			DataTable grantsTable = core.Db.Query(sQuery);
			
			foreach (DataRow dr in grantsTable.Rows)
			{
				grants.Add(new AccessControlGrant(core, item, dr));
			}
			
			return grants;
		}

        public static List<AccessControlGrant> GetGrants(Core core, IPermissibleItem item, long permissionId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", item.ItemKey.Id);
            sQuery.AddCondition("grant_item_type_id", item.ItemKey.TypeId);
            sQuery.AddCondition("grant_permission_id", permissionId);

            DataTable grantsTable = core.Db.Query(sQuery);

            foreach (DataRow dr in grantsTable.Rows)
            {
                grants.Add(new AccessControlGrant(core, item, dr));
            }

            return grants;
        }
		
		public override string Uri 
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
    
    internal struct UnsavedAccessControlGrant
    {
        private long permissionId;
        private sbyte grantAllow;
        
        private ItemKey key;
        private Core core;

        public long PermissionId
        {
            get
            {
                return permissionId;
            }
        }

        public AccessControlGrants Allow
        {
            get
            {
                return (AccessControlGrants)grantAllow;
            }
            set
            {
                grantAllow = (sbyte)value;
            }
        }
        
        public UnsavedAccessControlGrant(Core core, ItemKey key, long permissionId, AccessControlGrants grantAllow)
        {
            this.core = core;
            this.key = key;
            this.permissionId = permissionId;
            this.grantAllow = (sbyte)grantAllow;
        }
    }
	
	public class InvalidAccessControlGrantException : Exception
    {
        public InvalidAccessControlGrantException(string permission)
            : base("`" + permission + "` permission not found")
        {
        }
    }
}
