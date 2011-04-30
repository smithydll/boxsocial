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
    [DataTable("erp_list_prices")]
    public class DocumentListPrice : NumberedItem
    {
        [DataField("list_price_id", DataFieldKeys.Primary)]
        private long listPriceId;
        [DataField("document_id", typeof(Document))]
        private long documentId;
        [DataField("currency_id")]
        private long purchaseCurrency;
        [DataField("list_price_price")]
        private int listPrice;
        [DataField("list_price_quantity")]
        private int listPriceMinimumQuantity;
        [DataField("list_price_updated")]
        private long listPriceUpdated;

        public string GetPrice
        {
            get
            {
                Currency currency = new Currency(core, purchaseCurrency);
                return currency.GetPriceString(listPrice);
            }
        }

        public DocumentListPrice(Core core, long listPriceId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(DocumentListPrice_ItemLoad);

            try
            {
                LoadItem(listPriceId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentListPriceException();
            }
        }

        public DocumentListPrice(Core core, DataRow listPriceDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DocumentListPrice_ItemLoad);

            try
            {
                loadItemInfo(listPriceDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentListPriceException();
            }
        }

        void DocumentListPrice_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return listPriceId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidDocumentListPriceException : Exception
    {
    }
}
