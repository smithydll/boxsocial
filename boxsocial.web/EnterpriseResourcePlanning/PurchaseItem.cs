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
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{

    [DataTable("erp_purchase_item")]
    public class PurchaseItem : NumberedItem
    {
        [DataField("purchase_item_id", DataFieldKeys.Primary)]
        private long purchaseItemId;
        [DataField("purchase_order_id", typeof(PurchaseOrder))]
        private long purchaseOrderId;
        [DataField("document_id", typeof(Document))]
        private long documentId;
        [DataField("purchase_item_date")]
        private long purchaseDate;
        [DataField("currency_id")]
        private long purchaseCurrency;
        [DataField("purchase_item_price")]
        private int purchasePrice;

        public string GetPrice
        {
            get
            {
                Currency currency = new Currency(core, purchaseCurrency);
                return currency.GetPriceString(purchasePrice);
            }
        }

        public PurchaseItem(Core core, long purchaseItemId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(PurchaseItem_ItemLoad);

            try
            {
                LoadItem(purchaseItemId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPurchaseItemException();
            }
        }

        public PurchaseItem(Core core, DataRow purchaseItemDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PurchaseItem_ItemLoad);

            try
            {
                loadItemInfo(purchaseItemDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPurchaseItemException();
            }
        }

        void PurchaseItem_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return purchaseItemId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidPurchaseItemException : Exception
    {
    }
}
