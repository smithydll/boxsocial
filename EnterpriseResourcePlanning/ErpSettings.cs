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
    [Permission("VIEW_DOCUMENT_SOURCE", "Can view document source", PermissionTypes.View)]
    [Permission("VIEW_SUPERSEDED_DOCUMENT_SOURCE", "Can view superseded document source", PermissionTypes.View)]
    [Permission("VIEW_VENDORS", "Can view suppliers", PermissionTypes.View)]
    [Permission("VIEW_PURCHASES", "Can view purchases", PermissionTypes.View)]
    [Permission("CREATE_DOCUMENTS", "Can create documents", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_VENDORS", "Can create suppliers", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_PURCHASES", "Can create purchases", PermissionTypes.CreateAndEdit)]
    [Permission("REVISE_DOCUMENTS", "Can revise documents", PermissionTypes.CreateAndEdit)]
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
        [DataField("erp_document_templates")]
        private long documentTemplates;
        [DataField("erp_vendors")]
        private long vendors;
        [DataField("erp_purchase_orders")]
        private long purchaseOrders;

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

        public long DocumentTemplates
        {
            get
            {
                return documentTemplates;
            }
        }

        public long Vendors
        {
            get
            {
                return vendors;
            }
        }

        public long PurchaseOrders
        {
            get
            {
                return purchaseOrders;
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
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
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

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }
    }

    public class InvalidErpSettingsException : Exception
    {
    }
}
