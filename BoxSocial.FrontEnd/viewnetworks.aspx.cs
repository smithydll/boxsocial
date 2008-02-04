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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
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
                    networks = Network.GetNetworks(db, NetworkTypes.Workplace);
                    break;
                case "schools":
                    networks = Network.GetNetworks(db, NetworkTypes.School);
                    break;
                case "universities":
                    networks = Network.GetNetworks(db, NetworkTypes.University);
                    break;
                case "regions":
                default:
                    networks = Network.GetNetworks(db, NetworkTypes.Country);
                    break;
            }

            if (networks != null)
            {
                template.ParseVariables("NETWORKS", networks.Count.ToString());
                foreach (Network network in networks)
                {
                    VariableCollection networkVariableCollection = template.CreateChild("networks_list");

                    networkVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(network.DisplayName));
                    networkVariableCollection.ParseVariables("U_NETWORK", HttpUtility.HtmlEncode(network.Uri));
                }
            }

            EndResponse();
        }
    }
}
