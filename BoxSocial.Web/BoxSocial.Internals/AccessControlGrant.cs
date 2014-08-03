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

        public AccessControlGrant Clone()
        {
            return new AccessControlGrant(core, primitiveKey, itemId, itemTypeId, permissionId, grantAllow, item, owner, itemKey);
        }

        private AccessControlGrant(Core core, ItemKey primitiveKey, long itemId, long itemTypeId, long permissionId, sbyte grantAllow, IPermissibleItem item, Primitive owner, ItemKey itemKey)
            : base(core)
        {
            this.primitiveKey = primitiveKey;
            this.itemId = itemId;
            this.itemTypeId = itemTypeId;
            this.permissionId = permissionId;
            this.grantAllow = grantAllow;
            this.item = item;
            this.owner = owner;
            this.itemKey = itemKey;
        }

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
            set
            {
                grantAllow = (sbyte)value;

                UpdateQuery uQuery = new UpdateQuery(typeof(AccessControlGrant));
                uQuery.AddCondition("grant_primitive_id", PrimitiveKey.Id);
                uQuery.AddCondition("grant_primitive_type_id", PrimitiveKey.TypeId);
                uQuery.AddCondition("grant_item_id", ItemKey.Id);
                uQuery.AddCondition("grant_item_type_id", ItemKey.TypeId);
                uQuery.AddCondition("grant_permission_id", PermissionId);
                uQuery.AddField("grant_allow", grantAllow);

                core.Db.Query(uQuery);

                //HttpContext.Current.Response.Write(string.Format("Saved perms key: {0},{1},{2}<br />", PermissionId, PrimitiveKey.TypeId, PrimitiveKey.Id));
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

        internal AccessControlGrant(Core core, DataRow grantRow)
            : base(core)
        {
            this.item = null;
            ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
        }

        internal AccessControlGrant(Core core, System.Data.Common.DbDataReader grantRow)
            : base(core)
        {
            this.item = null;
            ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
        }
		
		internal AccessControlGrant(Core core, IPermissibleItem item, DataRow grantRow)
			: base(core)
		{
			this.item = item;
			ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
		}

        internal AccessControlGrant(Core core, IPermissibleItem item, System.Data.Common.DbDataReader grantRow)
            : base(core)
        {
            this.item = item;
            ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
        }
        
        internal AccessControlGrant(Core core, ItemKey primitive, ItemKey itemKey, long permissionId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            SelectQuery query = new SelectQuery(typeof(AccessControlGrant));
            
            query.AddCondition("grant_primitive_id", primitive.Id);
            query.AddCondition("grant_primitive_type_id", primitive.TypeId);
            query.AddCondition("grant_item_id", itemKey.Id);
            query.AddCondition("grant_item_type_id", itemKey.TypeId);
            query.AddCondition("grant_permission_id", permissionId);

            System.Data.Common.DbDataReader grantReader = core.Db.ReaderQuery(query);

            if (grantReader.HasRows)
            {
                grantReader.Read();

                try
                {
                    loadItemInfo(grantReader);
                }
                catch (InvalidItemException)
                {
                    AccessControlPermission acp = new AccessControlPermission(core, permissionId);
                    throw new InvalidAccessControlGrantException(acp.Name);
                }

                grantReader.Close();
                grantReader.Dispose();
            }
            else
            {
                grantReader.Close();
                grantReader.Dispose();

                AccessControlPermission acp = new AccessControlPermission(core, permissionId);
                throw new InvalidAccessControlGrantException(acp.Name);
            }
        }

        protected override void loadItemInfo(DataRow grantRow)
        {
            loadValue(grantRow, "grant_primitive", out primitiveKey);
            loadValue(grantRow, "grant_item_id", out itemId);
            loadValue(grantRow, "grant_item_type_id", out itemTypeId);
            loadValue(grantRow, "grant_permission_id", out permissionId);
            loadValue(grantRow, "grant_allow", out grantAllow);

            itemLoaded(grantRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader grantRow)
        {
            loadValue(grantRow, "grant_primitive", out primitiveKey);
            loadValue(grantRow, "grant_item_id", out itemId);
            loadValue(grantRow, "grant_item_type_id", out itemTypeId);
            loadValue(grantRow, "grant_permission_id", out permissionId);
            loadValue(grantRow, "grant_allow", out grantAllow);

            itemLoaded(grantRow);
        }
		
		private void AccessControlGrant_ItemLoad()
        {
        }

		
		// Cannot use the built-in for un-numbered stuffs 
		public static AccessControlGrant Create(Core core, ItemKey primitive, ItemKey itemKey, long permissionId, AccessControlGrants allow)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			InsertQuery iQuery = new InsertQuery(typeof(AccessControlGrant));
            
            iQuery.AddField("grant_primitive_id", primitive.Id);
            iQuery.AddField("grant_primitive_type_id", primitive.TypeId);
            iQuery.AddField("grant_item_id", itemKey.Id);
            iQuery.AddField("grant_item_type_id", itemKey.TypeId);
            iQuery.AddField("grant_permission_id", permissionId);
            iQuery.AddField("grant_allow", (sbyte)allow);
            
            core.Db.Query(iQuery);

            return new AccessControlGrant(core, primitive, itemKey, permissionId);
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

            if (items.Count == 0)
            {
                return new List<AccessControlGrant>();
            }

            Dictionary<ItemKey, IPermissibleItem> itemDictionary = new Dictionary<ItemKey, IPermissibleItem>(items.Count);

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
            foreach (IPermissibleItem item in items)
            {
                    if (!itemDictionary.ContainsKey(item.ItemKey))
                    {
                        itemDictionary.Add(item.ItemKey, item);
                        QueryCondition qc1 = sQuery.AddCondition(ConditionRelations.Or, "grant_item_id", item.ItemKey.Id);
                        qc1.AddCondition(ConditionRelations.And, "grant_item_type_id", item.ItemKey.TypeId);
                    }
            }

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            List<AccessControlGrant> grants = new List<AccessControlGrant>(16);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, itemDictionary[new ItemKey((long)grantsReader["grant_item_id"], (long)grantsReader["grant_item_type_id"])], grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();

            return grants;
        }

        public static List<AccessControlGrant> GetGrants(Core core, List<ItemKey> itemKeys)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            Dictionary<long, ItemKey> itemDictionary = new Dictionary<long, ItemKey>(itemKeys.Count);
            List<long> itemIds = new List<long>(itemKeys.Count);
            long itemTypeId = 0;

            foreach (ItemKey itemKey in itemKeys)
            {
                if (itemTypeId == itemKey.TypeId || itemTypeId == 0)
                {
                    itemIds.Add(itemKey.Id);
                    itemTypeId = itemKey.TypeId;
                    itemDictionary.Add(itemKey.Id, itemKey);
                }
            }

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", ConditionEquality.In, itemIds);
            sQuery.AddCondition("grant_item_type_id", itemTypeId);

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();

            return grants;
        }

        public static List<AccessControlGrant> GetGrants(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", itemKey.Id);
            sQuery.AddCondition("grant_item_type_id", itemKey.TypeId);

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();

            return grants;
        }
		
		public static List<AccessControlGrant> GetGrants(Core core, IPermissibleItem item)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
			sQuery.AddCondition("grant_item_id", item.ItemKey.Id);
			sQuery.AddCondition("grant_item_type_id", item.ItemKey.TypeId);

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, item, grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();
			
			return grants;
		}

        public static List<AccessControlGrant> GetGrants(Core core, ItemKey itemKey, long permissionId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", itemKey.Id);
            sQuery.AddCondition("grant_item_type_id", itemKey.TypeId);
            sQuery.AddCondition("grant_permission_id", permissionId);

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();

            return grants;
        }

        public static List<AccessControlGrant> GetGrants(Core core, IPermissibleItem item, long permissionId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", item.ItemKey.Id);
            sQuery.AddCondition("grant_item_type_id", item.ItemKey.TypeId);
            sQuery.AddCondition("grant_permission_id", permissionId);

            System.Data.Common.DbDataReader grantsReader = core.Db.ReaderQuery(sQuery);

            while (grantsReader.Read())
            {
                grants.Add(new AccessControlGrant(core, item, grantsReader));
            }

            grantsReader.Close();
            grantsReader.Dispose();

            return grants;
        }

        internal new long Delete()
        {
            DeleteQuery dQuery = new DeleteQuery(Item.GetTable(this.GetType()));
            dQuery.AddCondition("grant_primitive_id", PrimitiveKey.Id);
            dQuery.AddCondition("grant_primitive_type_id", PrimitiveKey.TypeId);
            dQuery.AddCondition("grant_item_id", ItemKey.Id);
            dQuery.AddCondition("grant_item_type_id", ItemKey.TypeId);
            dQuery.AddCondition("grant_permission_id", permissionId);

            long result = db.Query(dQuery);

            return result;
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

        private ItemKey itemKey;
        private ItemKey primitiveKey;
        private Core core;

        public ItemKey ItemKey
        {
            get
            {
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
            set
            {
                grantAllow = (sbyte)value;
            }
        }

        public UnsavedAccessControlGrant(Core core, ItemKey primitiveKey, ItemKey itemKey, long permissionId, AccessControlGrants grantAllow)
        {
            this.core = core;
            this.itemKey = itemKey;
            this.primitiveKey = primitiveKey;
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
