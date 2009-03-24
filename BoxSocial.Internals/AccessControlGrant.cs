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
using System.Data;
using System.Configuration;
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
		
		public AccessControlGrant(Core core, DataRow grantRow)
			: base(core)
		{
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
