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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [DataTable("erp_settings")]
    [Permission("VIEW_DOCUMENTS", "Can view documents", PermissionTypes.View)]
    [Permission("VIEW_SUPPLIERS", "Can view suppliers", PermissionTypes.View)]
    [Permission("VIEW_PURCHASES", "Can view purchases", PermissionTypes.View)]
    [Permission("CREATE_DOCUMENTS", "Can create documents", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_SUPPLIERS", "Can create suppliers", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_PURCHASES", "Can create purchases", PermissionTypes.CreateAndEdit)]
    public class ErpSettings : NumberedItem, IPermissibleItem
    {
        [DataField("erp_settings_id", DataFieldKeys.Primary)]
        private long settingsId;
        [DataField("erp_item", DataFieldKeys.Unique)]
        private ItemKey ownerKey;
        [DataField("erp_projects")]
        private long projects;
        [DataField("erp_documents")]
        private long documents;

        private Primitive owner;
        private Access access;

        public long Projects
        {
            get
            {
                return projects;
            }
        }

        public long Documents
        {
            get
            {
                return documents;
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeString != owner.Type)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public ErpSettings(Core core, long settingsId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ErpSettings_ItemLoad);

            try
            {
                LoadItem(settingsId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidErpSettingsException();
            }
        }

        public ErpSettings(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(ErpSettings_ItemLoad);

            try
            {
                LoadItem("erp_item_id", "erp_item_type_id", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidErpSettingsException();
            }
        }

        void ErpSettings_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return settingsId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }


        public List<AccessControlPermission> AclPermissions
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "ERP Settings: " + Owner.DisplayName + " (" + Owner.Key + ")";
            }
        }

        public bool GetDefaultCan(string permission)
        {
            return false;
        }
    }

    public class InvalidErpSettingsException : Exception
    {
    }
}
