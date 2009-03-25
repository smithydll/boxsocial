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
	[DataTable("account_control_permissions")]
	public class AccessControlPermission : NumberedItem
	{
		[DataField("permission_id", DataFieldKeys.Primary)]
		long permissionId;
		[DataField("permission_name", DataFieldKeys.Unique, "u_key", 31)]
		string permissionName;
		[DataField("permission_item_type_id", DataFieldKeys.Unique, "u_key")]
		long itemTypeId;
		[DataField("permission_description", 31)]
		string permissionDescription;
		
		public long PermissionId
		{
			get
			{
				return permissionId;
			}
		}
		
		public string Name
		{
			get
			{
				return permissionName;
			}
		}
		
		public long ItemTypeId
		{
			get
			{
				return itemTypeId;
			}
		}
		
		public ItemType ItemType
		{
			get
			{
				return new ItemType(core, itemTypeId);
			}
		}
		
		public AccessControlPermission(Core core, long permissionId)
			: base (core)
		{
            ItemLoad += new ItemLoadHandler(AccessControlPermission_ItemLoad);

            try
            {
                LoadItem(permissionId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidAccessControlPermissionException();
            }
		}
		
		private void AccessControlPermission_ItemLoad()
        {
        }
		
		public static AccessControlPermission Create(Core core, ItemType type, string permissionName)
		{
			AccessControlPermission acp = (AccessControlPermission)Item.Create(core, typeof(AccessControlPermission),
			                                                                   new FieldValuePair("permission_item_type", type.TypeId),
			                                                                   new FieldValuePair("permission_name", permissionName));
			
			return acp;
		}
		
		public void Delete()
		{
			base.Delete();
		}
		
		public override long Id 
		{
			get 
			{
				return permissionId;
			}
		}
		
		public override string Uri 
		{
			get
			{
				throw new NotImplementedException();
			}
		}

	}
	
	public class InvalidAccessControlPermissionException : Exception
    {
    }
}
