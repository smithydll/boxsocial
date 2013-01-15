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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Networks;

namespace BoxSocial.FrontEnd
{
    public partial class viewnetworks : TPage
    {
        public viewnetworks()
            : base("viewnetworks.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string type = (string)Request.QueryString["type"];
            List<Network> networks;

            switch (type)
            {
                case "workplaces":
                    networks = Network.GetNetworks(core, NetworkTypes.Workplace);
                    break;
                case "schools":
                    networks = Network.GetNetworks(core, NetworkTypes.School);
                    break;
                case "universities":
                    networks = Network.GetNetworks(core, NetworkTypes.University);
                    break;
                case "regions":
                default:
                    networks = Network.GetNetworks(core, NetworkTypes.Country);
                    break;
            }

            if (networks != null)
            {
                template.Parse("NETWORKS", networks.Count.ToString());
                foreach (Network network in networks)
                {
                    VariableCollection networkVariableCollection = template.CreateChild("networks_list");

                    networkVariableCollection.Parse("TITLE", network.DisplayName);
                    networkVariableCollection.Parse("U_NETWORK", network.Uri);
                }
            }

            EndResponse();
        }
    }
}
