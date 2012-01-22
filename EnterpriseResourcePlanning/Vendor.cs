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
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [DataTable("erp_vendors")]
    public class Vendor : NumberedItem
    {
        [DataField("vendor_id", DataFieldKeys.Primary)]
        private long vendorId;
        [DataField("vendor_item")]
        private ItemKey ownerKey;
        [DataField("vendor_title", 31)]
        private string vendorTitle;
        [DataField("vendor_abn", 15)]
        private string vendorABN;
        [DataField("vendor_phone", 15)]
        private string vendorPhoneNumber;
        [DataField("vendor_fax", 15)]
        private string vendorFaxNumber;
        [DataField("vendor_address", 15)]
        private string vendorAddress;

        private Primitive owner;

        public long VendorId
        {
            get
            {
                return vendorId;
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

        public string Title
        {
            get
            {
                return vendorTitle;
            }
            set
            {
                SetProperty("vendorTitle", value);
            }
        }

        public string ABN
        {
            get
            {
                return vendorABN;
            }
            set
            {
                SetProperty("vendorABN", value);
            }
        }

        public string PhoneNumber
        {
            get
            {
                return vendorPhoneNumber;
            }
            set
            {
                SetProperty("vendorPhoneNumber", value);
            }
        }

        public string FaxNumber
        {
            get
            {
                return vendorFaxNumber;
            }
            set
            {
                SetProperty("vendorFaxNumber", value);
            }
        }

        public string Address
        {
            get
            {
                return vendorAddress;
            }
            set
            {
                SetProperty("vendorAddress", value);
            }
        }

        public Vendor(Core core, long vendorId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Vendor_ItemLoad);

            try
            {
                LoadItem(vendorId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidVendorException();
            }
        }

        public Vendor(Core core, DataRow vendorDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Vendor_ItemLoad);

            try
            {
                loadItemInfo(vendorDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidVendorException();
            }
        }

        void Vendor_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return vendorId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}vendor/{1}",
                        Owner.UriStub, VendorId));
            }
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.SetTemplate("viewvendor");

            ErpSettings settings = new ErpSettings(e.Core, e.Page.Owner);

            if (!settings.Access.Can("VIEW_VENDORS"))
            {
                e.Core.Functions.Generate403();
            }

            Vendor vendor = null;
            try
            {
                vendor = new Vendor(e.Core, e.ItemId);
            }
            catch (InvalidVendorException)
            {
                e.Core.Functions.Generate404();
            }
        }
    }

    public class InvalidVendorException : Exception
    {
    }
}
