/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Networks;

namespace BoxSocial.Networks
{
    public class AccountNetworks : AccountModule
    {
        public AccountNetworks(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageNetworkMemberships);
            RegisterSubModule += new RegisterSubModuleHandler(JoinNetwork);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Networks";
            }
        }

        public override string Key
        {
            get
            {
                return "networks";
            }
        }

        public override int Order
        {
            get
            {
                return 8;
            }
        }

        private void ManageNetworkMemberships(string submodule)
        {
            subModules.Add("memberships", "Manage Memberships");
            if (submodule != "memberships" && !string.IsNullOrEmpty(submodule)) return;

            template.SetTemplate("Networks", "account_network_membership");

            List<Network> networks = new List<Network>();

            SelectQuery query = new SelectQuery("network_members nm");
            query.AddJoin(JoinTypes.Inner, "network_keys nk", "nm.network_id", "nk.network_id");
            query.AddJoin(JoinTypes.Inner, "network_info ni", "nk.network_id", "ni.network_id");
            query.AddFields(Network.NETWORK_INFO_FIELDS, "nk.network_network");
            query.AddCondition("nm.user_id", loggedInMember.Id);

            DataTable networksTable = db.SelectQuery(query);

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(db, dr));
            }

            if (networks.Count > 0)
            {
                template.ParseVariables("NETWORK_MEMBERSHIPS", "TRUE");
            }

            foreach (Network theNetwork in networks)
            {
                VariableCollection networkVariableCollection = template.CreateChild("network_list");

                networkVariableCollection.ParseVariables("NETWORK_DISPLAY_NAME", HttpUtility.HtmlEncode(theNetwork.DisplayName));
                networkVariableCollection.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(theNetwork.Members.ToString()));

                networkVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(theNetwork.Uri));
                networkVariableCollection.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(theNetwork.MemberlistUri));
            }
        }

        private void JoinNetwork(string submodule)
        {
            subModules.Add("join", null);
            if (submodule != "join") return;

            if (Request.Form["join"] != null)
            {
                JoinNetworkSave();
                return;
            }

            template.SetTemplate("Networks", "account_network_join");
            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));

            AuthoriseRequestSid();

            long networkId;

            try
            {
                networkId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(networkId.ToString()));

            try
            {
                Network theNetwork = new Network(db, networkId);

                if (theNetwork.IsNetworkMember(loggedInMember))
                {
                    SetRedirectUri(theNetwork.Uri);
                    Display.ShowMessage("Already a member", "You are already a member of this network");
                    return;
                }

                if (theNetwork.RequireConfirmation)
                {
                    // show form
                }
                else
                {
                    // just join the network
                    if (theNetwork.Join(core, loggedInMember, "") != null)
                    {
                        SetRedirectUri(theNetwork.Uri);
                        Display.ShowMessage("Joined Network", "You have successfully joined the network.");
                        return;
                    }
                    else
                    {
                    }
                }
            }
            catch
            {
                Display.ShowMessage("Error", "The network you are trying to join does not exist, go back.");
                return;
            }
        }

        private void JoinNetworkSave()
        {
            AuthoriseRequestSid();

            long networkId;

            try
            {
                networkId = long.Parse(Request.Form["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            /*try
            {*/
            Network theNetwork = new Network(db, networkId);

            string networkEmail = Request.Form["email"];

            if (!Member.CheckEmailUnique(db, networkEmail))
            {
                Display.ShowMessage("Error", "The e-mail address you have attempted to register with the network is already in use with another account.");
                return;
            }
            else if (theNetwork.Join(core, loggedInMember, networkEmail) != null)
            {
                if (theNetwork.RequireConfirmation)
                {
                    Display.ShowMessage("Confirmation Required", "Before you are able to finish joining the network you must confirm your network e-mail address. An confirmation e-mail has been sent to your network e-mail address with a link to click. Once you confirm your e-mail address you will be able to join the network.");
                    return;
                }
                else
                {
                    SetRedirectUri(theNetwork.Uri);
                    Display.ShowMessage("Joined Network", "You have successfully joined the network.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Error", "Could not join network.");
                return;
            }
            /*}
            catch
            {
            }*/
        }

        private void LeaveNetwork(string submodule)
        {
            subModules.Add("join", null);
            if (submodule != "join") return;

            switch (Display.GetConfirmBoxResult())
            {
                case ConfirmBoxResult.None:
                    break;
                case ConfirmBoxResult.Yes:
                    LeaveNetworkSave();
                    return;
                case ConfirmBoxResult.No:
                    return;
            }

            long networkId = Functions.RequestLong("id", -1);

            if (networkId >= 0)
            {
                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", "networks");
                hiddenFieldList.Add("sub", "leave");
                hiddenFieldList.Add("id", networkId.ToString());

                Display.ShowConfirmBox(Linker.AppendSid("/account", true), "Leave network?", "Are you sure you want to leave this network?", hiddenFieldList);
            }
            else
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }
        }

        private void LeaveNetworkSave()
        {
        }
    }
}
