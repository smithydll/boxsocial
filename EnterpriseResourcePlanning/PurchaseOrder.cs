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
    public class PurchaseOrder : NumberedItem
    {
        [DataField("purchase_order_id", DataFieldKeys.Primary)]
        private long purchaseOrderId;
        [DataField("currency_id")]
        private long purchaseCurrency;
        [DataField("purchase_order_price")]
        private int purchasePrice;
        [DataField("purchase_order_delivery")]
        private int deliveryPrice;
        [DataField("purchase_order_total")]
        private int totalPrice;
        [DataField("purchase_order_items")]
        private long purchaseItemCount;

        public long ItemCount
        {
            get
            {
                return purchaseItemCount;
            }
        }

        public string GetPurchasePrice
        {
            get
            {
                Currency currency = new Currency(core, purchaseCurrency);
                return currency.GetPriceString(purchasePrice);
            }
        }

        public string GetDeliveryPrice
        {
            get
            {
                Currency currency = new Currency(core, purchaseCurrency);
                return currency.GetPriceString(deliveryPrice);
            }
        }

        public string GetTotalPrice
        {
            get
            {
                Currency currency = new Currency(core, purchaseCurrency);
                return currency.GetPriceString(totalPrice);
            }
        }

        public PurchaseOrder(Core core, long purchaseOrderId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PurchaseOrder_ItemLoad);

            try
            {
                LoadItem(purchaseOrderId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPurchaseOrderException();
            }
        }
        public PurchaseOrder(Core core, DataRow purchaseOrderDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PurchaseOrder_ItemLoad);

            try
            {
                loadItemInfo(purchaseOrderDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPurchaseOrderException();
            }
        }

        void PurchaseOrder_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return purchaseOrderId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidPurchaseOrderException : Exception
    {
    }
}
