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
using System.Globalization;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("currencies")]
    public class Currency : NumberedItem
    {
        [DataField("currency_id", DataFieldKeys.Primary)]
        private long currencyId;
        [DataField("currency_code", DataFieldKeys.Unique, 3)]
        private string currencyCode;
        [DataField("currency_title", 31)]
        private string title;
        [DataField("currency_fraction_title", 31)]
        private string fractionTitle;
        [DataField("currency_fraction")]
        private int fraction;
        [DataField("currency_symbol", 7)]
        private string symbol;

        public string Code
        {
            get
            {
                return currencyCode;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string FractionTitle
        {
            get
            {
                return fractionTitle;
            }
        }

        public int Fraction
        {
            get
            {
                return fraction;
            }
        }

        public int FractionDigits
        {
            get
            {
                return (int)Math.Log10(fraction);
            }
        }

        public string Symbol
        {
            get
            {
                return symbol;
            }
        }

        public Currency(Core core, string currencyCode)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Currency_ItemLoad);

            this.currencyCode = currencyCode;
            this.currencyId = GetCurrencyId(currencyCode);

            GetCurrencyInfo(currencyId, ref currencyCode, ref title, ref fraction, ref symbol);
        }

        public Currency(Core core, long currencyId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Currency_ItemLoad);

            this.currencyId = currencyId;

            GetCurrencyInfo(currencyId, ref currencyCode, ref title, ref fraction, ref symbol);
        }

        void Currency_ItemLoad()
        {
        }

        private static void GetCurrencyInfo(long currencyId, ref string currencyCode, ref string title, ref int fraction, ref string symbol)
        {
            /* Information from Wikipedia:
             http://en.wikipedia.org/wiki/ISO_4217
             initially just a few globally relevant currencies */
            switch (currencyId)
            {
                case 036:
                    currencyCode = "AUD";
                    title = "Australian dollar";
                    fraction = 100;
                    symbol = "$";
                    break;
                case 124:
                    currencyCode = "CAD";
                    title = "Canadian dollar";
                    fraction = 100;
                    symbol = "$";
                    break;
                case 756:
                    currencyCode = "CHF";
                    title = "Swiss franc";
                    fraction = 100;
                    symbol = "Fr";
                    break;
                case 156:
                    currencyCode = "CNY";
                    title = "Chinese yuan";
                    fraction = 100;
                    symbol = "元";
                    break;
                case 978:
                    currencyCode = "EUR";
                    title = "Euro";
                    fraction = 100;
                    symbol = "€";
                    break;
                case 826:
                    currencyCode = "GBP";
                    title = "Pound sterling";
                    fraction = 100;
                    symbol = "£";
                    break;
                case 344:
                    currencyCode = "HKD";
                    title = "Hong Kong dollar";
                    fraction = 100;
                    symbol = "$";
                    break;
                case 392:
                    currencyCode = "JPY";
                    title = "Japanese yen";
                    fraction = 1000;
                    symbol = "¥";
                    break;
                case 752:
                    currencyCode = "NZD";
                    title = "Swedish krona";
                    fraction = 100;
                    symbol = "kr";
                    break;
                case 554:
                    currencyCode = "SEK";
                    title = "New Zealand dollar";
                    fraction = 100;
                    symbol = "$";
                    break;
                case 840:
                    currencyCode = "USD";
                    title = "United States dollar";
                    fraction = 100;
                    symbol = "$";
                    break;
            }
        }

        public static long GetCurrencyId(string currencyCode)
        {
            switch (currencyCode)
            {
                case "AUD":
                    return 36;
                case "CAD":
                    return 124;
                case "CHF":
                    return 756;
                case "CNY":
                    return 156;
                case "EUR":
                    return 978;
                case "GBP":
                    return 826;
                case "HKD":
                    return 344;
                case "JPY":
                    return 392;
                case "NZD":
                    return 554;
                case "SEK":
                    return 554;
                case "USD":
                    return 840;
                default:
                    return 0;
            }
        }

        public static SelectBox BuildTimeZoneSelectBox(string name)
        {
            SelectBox currencySelectBox = new SelectBox(name);

            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("AUD").ToString(), " Australian Dollar (AUD)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("CAD").ToString(), " Canadian dollar (CAD)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("CHF").ToString(), " Swiss franc (CHF)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("CNY").ToString(), " Chinese yuan (CNY)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("EUR").ToString(), " Euro (EUR)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("GBP").ToString(), " Pound Sterling (GBP)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("HKD").ToString(), " Hong Kong dollar (HKD)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("JPY").ToString(), " Japanese Yen (JPY)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("NZD").ToString(), " New Zealand dollar (NZD)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("SEK").ToString(), " Swedish krona (SEK)"));
            currencySelectBox.Add(new SelectBoxItem(GetCurrencyId("USD").ToString(), " United States dollar (USD)"));

            return currencySelectBox;
        }

        public string GetPriceString(int value)
        {
            switch (FractionDigits)
            {
                case 1:
                    return string.Format("{0:1} {1}", value / Fraction, Code);
                case 2:
                    return string.Format("{0:2} {1}", value / Fraction, Code);
                case 3:
                    return string.Format("{0:3} {1}", value / Fraction, Code);
                default:
                    return string.Format("{0:0} {1}", value / Fraction, Code);
            }
        }

        public override long Id
        {
            get
            {
                return currencyId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }
}
