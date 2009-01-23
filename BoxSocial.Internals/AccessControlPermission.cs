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
		[DataField("permission_name", 31)]
		string permissionName;
		
		public AccessControlPermission(Core core)
			: base (core)
		{
			
		}
		
		public static AccessControlPermission Create(Core core, string permissionName)
		{
			AccessControlPermission acp = (AccessControlPermission)Item.Create(core, typeof(AccessControlPermission),
			                                                                   new FieldValuePair("permission_name", permissionName));
			
			return acp;
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
}
