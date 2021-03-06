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
    public enum PermissionTypes : byte
    {
        View = 0x0,
        Interact = 0x02,
        CreateAndEdit = 0x04,
        Delete = 0x08,
    }

    public struct PermissionInfo
    {
        public string Key;
        public string Description;
        public PermissionTypes PermissionType;

        public PermissionInfo(string key, string description, PermissionTypes type)
        {
            this.Key = key;
            this.Description = description;
            this.PermissionType = type;
        }
    }
    
	[DataTable("account_control_permissions")]
	public class AccessControlPermission : NumberedItem
	{
		[DataField("permission_id", DataFieldKeys.Primary)]
		long permissionId;
		[DataField("permission_name", DataFieldKeys.Unique, "u_key", 31)]
		string permissionName;
		[DataField("permission_item_type_id", DataFieldKeys.Unique, "u_key")]
		long itemTypeId;
		[DataField("permission_description", 255)]
		string permissionDescription;
        [DataField("permission_type")]
        byte permissionType;

        public AccessControlPermission Clone()
        {
            return new AccessControlPermission(core, permissionId, permissionName, itemTypeId, permissionDescription, permissionType);
        }

        private AccessControlPermission(Core core, long permissionId, string permissionName, long itemTypeId, string permissionDescription, byte permissionType)
            : base(core)
        {
            this.permissionId = permissionId;
            this.permissionName = permissionName;
            this.itemTypeId = itemTypeId;
            this.permissionDescription = permissionDescription;
            this.permissionType = permissionType;
        }

        private string permissionAssembly;
        private string permissionDescriptionCache;
		
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
            set
            {
                SetProperty("permissionName", value);
            }
		}

        public PermissionTypes PermissionType
        {
            get
            {
                return (PermissionTypes)permissionType;
            }
            set
            {
                SetProperty("permissionType", (byte)value);
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

        public string Description
        {
            get
            {
                if (permissionDescription.StartsWith("{L_", StringComparison.Ordinal) && permissionDescription.EndsWith("}", StringComparison.Ordinal))
                {
                    string key = permissionDescription.Substring(3, permissionDescription.Length - 4);
                    /*if (core.Prose.ContainsKey(key))
                    {
                        permissionDescriptionCache = core.Prose.GetString(key);
                    }*/
                    if (core.Prose.TryGetString(key, out permissionDescriptionCache))
                    {
                    }
                    else if ((!string.IsNullOrEmpty(permissionAssembly)) && core.Prose.ContainsKey(permissionAssembly, key))
                    {
                        permissionDescriptionCache = core.Prose.GetString(permissionAssembly, key);
                    }
                }
                else
                {
                    permissionDescriptionCache = permissionDescription;
                }
                return permissionDescriptionCache;
            }
            set
            {
                SetProperty("permissionType", value);
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
                AccessControlPermission acp = new AccessControlPermission(core, permissionId);
                throw new InvalidAccessControlPermissionException(acp.Name);
            }
		}

        public AccessControlPermission(Core core, long typeId, string permissionName)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(AccessControlPermission_ItemLoad);
            
            SelectQuery query = GetSelectQueryStub();
            query.AddCondition("permission_item_type_id", typeId);
            query.AddCondition("permission_name", permissionName);

            System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);

            if (reader.HasRows)
            {
                reader.Read();

                loadItemInfo(reader);

                reader.Close();
                reader.Dispose();
            }
            else
            {
                reader.Close();
                reader.Dispose();

                throw new InvalidAccessControlPermissionException();
            }
        }

        public AccessControlPermission(Core core, DataRow permissionRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(AccessControlPermission_ItemLoad);
            OnUpdate += new EventHandler(AccessControlPermission_OnUpdate);
            OnDelete += new EventHandler(AccessControlPermission_OnDelete);

            try
            {
                loadItemInfo(permissionRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidAccessControlPermissionException();
            }
        }

        public AccessControlPermission(Core core, System.Data.Common.DbDataReader permissionRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(AccessControlPermission_ItemLoad);
            OnUpdate += new EventHandler(AccessControlPermission_OnUpdate);
            OnDelete += new EventHandler(AccessControlPermission_OnDelete);

            try
            {
                loadItemInfo(permissionRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidAccessControlPermissionException();
            }
        }

        protected override void loadItemInfo(DataRow permissionRow)
        {
            loadValue(permissionRow, "permission_id", out permissionId);
            loadValue(permissionRow, "permission_name", out permissionName);
            loadValue(permissionRow, "permission_item_type_id", out itemTypeId);
            loadValue(permissionRow, "permission_description", out permissionDescription);
            loadValue(permissionRow, "permission_type", out permissionType);

            itemLoaded(permissionRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader permissionRow)
        {
            loadValue(permissionRow, "permission_id", out permissionId);
            loadValue(permissionRow, "permission_name", out permissionName);
            loadValue(permissionRow, "permission_item_type_id", out itemTypeId);
            loadValue(permissionRow, "permission_description", out permissionDescription);
            loadValue(permissionRow, "permission_type", out permissionType);

            itemLoaded(permissionRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void AccessControlPermission_OnDelete(object sender, EventArgs e)
        {
            
        }

        void AccessControlPermission_OnUpdate(object sender, EventArgs e)
        {
            
        }
		
		private void AccessControlPermission_ItemLoad()
        {
        }

        public static AccessControlPermission Create(Core core, ItemType type, string permissionName, string permissionDescription, PermissionTypes permissionType)
		{
            return Create(core, type.TypeId, permissionName, permissionDescription, permissionType);
		}

        public static AccessControlPermission Create(Core core, long typeId, string permissionName, string permissionDescription, PermissionTypes permissionType)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            AccessControlPermission acp = (AccessControlPermission)Item.Create(core, typeof(AccessControlPermission),
                                                                               new FieldValuePair("permission_item_type_id", typeId),
                                                                               new FieldValuePair("permission_name", permissionName),
                                                                               new FieldValuePair("permission_description", permissionDescription),
                                                                               new FieldValuePair("permission_type", (byte)permissionType));

            return acp;
        }

        public static List<AccessControlPermission> GetPermissions(Core core, IPermissibleItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<AccessControlPermission> permissions = new List<AccessControlPermission>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(AccessControlPermission));
            sQuery.AddCondition("permission_item_type_id", item.ItemKey.TypeId);
            sQuery.AddSort(SortOrder.Ascending, "permission_type");

            System.Data.Common.DbDataReader permissionsReader = core.Db.ReaderQuery(sQuery);

            while (permissionsReader.Read())
            {
                permissions.Add(new AccessControlPermission(core, permissionsReader));
            }

            permissionsReader.Close();
            permissionsReader.Dispose();

            return permissions;
        }
		
		public new void Delete()
		{
			base.Delete();
		}

        public void SetAssembly(Assembly value)
        {
            permissionAssembly = value.GetName().Name;
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
        public InvalidAccessControlPermissionException()
            : base("Invalid Data Supplied")
        {
        }

        public InvalidAccessControlPermissionException(string permission)
            : base ("`" + permission + "` permission not found")
        {
        }
    }
}
