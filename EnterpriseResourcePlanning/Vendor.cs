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
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidVendorException : Exception
    {
    }
}
