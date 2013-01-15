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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("countries")]
    public class Country : Item
    {
        [DataField("country_iso", DataFieldKeys.Primary, 2)]
        private string countryIso;
        [DataField("country_name", 63)]
        private string countryName;

        public string Iso
        {
            get
            {
                return countryIso;
            }
        }

        public string Name
        {
            get
            {
                return countryName;
            }
        }

        public Country(Core core, DataRow countryRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Country_ItemLoad);

            try
            {
                loadItemInfo(countryRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCountryException();
            }
        }

        public Country(Core core, string iso)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Country_ItemLoad);

            this.countryIso = iso;

            if (core.Prose.ContainsKey("Internals", "COUNTRY_" + iso))
            {
                countryName = core.Prose.GetString("Internals", "COUNTRY_" + iso);
                return;
            }

            /* We cache this as it's quite static */
            switch (iso)
            {
                case "AD":
                    countryName = "Andorra";
                    break;
                case "AE":
                    countryName = "United Arab Emirates";
                    break;
                case "AF":
                    countryName = "Afghanistan";
                    break;
                case "AG":
                    countryName = "Antigua and Barbuda";
                    break;
                case "AI":
                    countryName = "Anguilla";
                    break;
                case "AL":
                    countryName = "Albania";
                    break;
                case "AM":
                    countryName = "Armenia";
                    break;
                case "AN":
                    countryName = "Netherlands Antilles";
                    break;
                case "AO":
                    countryName = "Angola";
                    break;
                case "AQ":
                    countryName = "Antarctica";
                    break;
                case "AR":
                    countryName = "Argentina";
                    break;
                case "AS":
                    countryName = "American Samoa";
                    break;
                case "AT":
                    countryName = "Austria";
                    break;
                case "AU":
                    countryName = "Australia";
                    break;
                case "AW":
                    countryName = "Aruba";
                    break;
                case "AZ":
                    countryName = "Azerbaijan";
                    break;
                case "BA":
                    countryName = "Bosnia and Herzegovina";
                    break;
                case "BB":
                    countryName = "Barbados";
                    break;
                case "BD":
                    countryName = "Bangladesh";
                    break;
                case "BE":
                    countryName = "Belgium";
                    break;
                    /* And a few select countries that have registered */
                case "CA":
                    countryName = "Canada";
                    break;
                    //
                case "DE":
                    countryName = "Germany";
                    break;
                    //
                case "FR":
                    countryName = "France";
                    break;
                    //
                case "GB":
                    countryName = "United Kingdom";
                    break;
                    //
                case "NL":
                    countryName = "Netherlands";
                    break;
                    //
                case "NZ":
                    countryName = "New Zealand";
                    break;
                    //
                case "US":
                    countryName = "United States";
                    break;
                default:
                    try
                    {
                        LoadItem("country_iso", iso);
                    }
                    catch (InvalidItemException)
                    {
                        throw new InvalidCountryException();
                    }
                    break;
            }
        }

        void Country_ItemLoad()
        {
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidCountryException : Exception
    {
    }
}
