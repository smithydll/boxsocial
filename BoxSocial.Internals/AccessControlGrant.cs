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
		ItemKey primitiveKey;
		[DataField("grant_item_id", DataFieldKeys.Unique, "u_key")]
		long itemId;
		[DataField("grant_item_type_id", DataFieldKeys.Unique, "u_key")]
		long itemTypeId;
		[DataField("grant_permission_id", DataFieldKeys.Unique, "u_key")]
		long permissionId;
		[DataField("grant_allow")]
		sbyte grantAllow;
		
		Item item;
		Primitive owner;

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
                if (owner == null || primitiveKey.Id != owner.Id || primitiveKey.Type != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(primitiveKey);
                    owner = core.UserProfiles[primitiveKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

		
		private AccessControlGrant(Core core, Item item, DataRow grantRow)
			: base(core)
		{
			this.item = item;
			ItemLoad += new ItemLoadHandler(AccessControlGrant_ItemLoad);

            loadItemInfo(grantRow);
		}
		
		private void AccessControlGrant_ItemLoad()
        {
        }

		
		// Cannot use the built-in for un-numbered stuffs 
		/*public static AccessControlPermission Create(Core core, ItemType type, string permissionName)
		{
			AccessControlGrant acg = (AccessControlPermission)Item.Create(core, typeof(AccessControlPermission),
			                                                                   new FieldValuePair("permission_item_type", type.TypeId),
			                                                                   new FieldValuePair("permission_name", permissionName));
			
			return acg;
		}*/
		
		public bool Can(User user)
		{
			bool isDenied = false;
			bool isAllowed = false;
			
			return (!isDenied && isAllowed);
		}
		
		public static List<AccessControlGrant> GetGrants(Core core, NumberedItem item)
		{
			List<AccessControlGrant> grants = new List<AccessControlGrant>();
			
			SelectQuery sQuery = Item.GetSelectQueryStub(typeof(AccessControlGrant));
			sQuery.AddCondition("grant_item_id", item.Key.Id);
			sQuery.AddCondition("grant_item_type_id", item.Key.TypeId);
			
			DataTable grantsTable = core.db.Query(sQuery);
			
			foreach (DataRow dr in grantsTable.Rows)
			{
				grants.Add(new AccessControlGrant(core, item, dr));
			}
			
			return grants;
		}

        public static List<AccessControlGrant> GetGrants(Core core, NumberedItem item, long permissionId)
        {
            List<AccessControlGrant> grants = new List<AccessControlGrant>();

            SelectQuery sQuery = Item.GetSelectQueryStub(typeof(AccessControlGrant));
            sQuery.AddCondition("grant_item_id", item.Key.Id);
            sQuery.AddCondition("grant_item_type_id", item.Key.TypeId);
            sQuery.AddCondition("grant_permission_id", permissionId);

            DataTable grantsTable = core.db.Query(sQuery);

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
	
	public class InvalidAccessControlGrantException : Exception
    {
    }
}
