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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Networks
{
    [AccountSubModule("networks", "memberships", true)]
    public class AccountNetworksMemberships : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Memberships";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountNetworksMemberships()
        {
            this.Load += new EventHandler(AccountNetworksMemberships_Load);
            this.Show += new EventHandler(AccountNetworksMemberships_Show);
        }

        void AccountNetworksMemberships_Load(object sender, EventArgs e)
        {
            AddModeHandler("join", new ModuleModeHandler(AccountNetworksMemberships_Join));
            AddSaveHandler("join", new EventHandler(AccountNetworksMemberships_Join_Save));
            AddModeHandler("leave", new ModuleModeHandler(AccountNetworksMemberships_Leave));
            AddSaveHandler("leave", new EventHandler(AccountNetworksMemberships_Leave_Save));
        }

        void AccountNetworksMemberships_Show(object sender, EventArgs e)
        {
            SetTemplate("account_network_membership");

            List<Network> networks = new List<Network>();

            SelectQuery query = NetworkMember.GetSelectQueryStub(typeof(NetworkMember));
            query.AddFields(Network.GetFieldsPrefixed(typeof(Network)));
            query.AddFields(NetworkInfo.GetFieldsPrefixed(typeof(NetworkInfo)));
            query.AddJoin(JoinTypes.Inner, "network_keys", "network_id", "network_id");
            query.AddJoin(JoinTypes.Inner, "network_info", "network_id", "network_id");
            query.AddCondition("user_id", loggedInMember.Id);

            DataTable networksTable = db.Query(query);

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(core, dr, NetworkLoadOptions.Common));
            }

            if (networks.Count > 0)
            {
                template.Parse("NETWORK_MEMBERSHIPS", "TRUE");
            }

            foreach (Network theNetwork in networks)
            {
                VariableCollection networkVariableCollection = template.CreateChild("network_list");

                networkVariableCollection.Parse("NETWORK_DISPLAY_NAME", theNetwork.DisplayName);
                networkVariableCollection.Parse("MEMBERS", theNetwork.Members.ToString());

                networkVariableCollection.Parse("U_VIEW", theNetwork.Uri);
                networkVariableCollection.Parse("U_MEMBERLIST", theNetwork.MemberlistUri);
            }
        }

        void AccountNetworksMemberships_Join(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_network_join");
            template.Parse("S_FORM_ACTION", Linker.AppendSid("/account/", true));

            AuthoriseRequestSid();

            long networkId = Functions.RequestLong("id", 0);

            if (networkId == 0)
            {
                DisplayGenericError();
                return;
            }

            template.Parse("S_ID", networkId.ToString());

            try
            {
                Network theNetwork = new Network(core, networkId);

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

        void AccountNetworksMemberships_Join_Save(object sender, EventArgs e)
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
            Network theNetwork = new Network(core, networkId);

            string networkEmail = Request.Form["email"];

            if (!theNetwork.IsValidNetworkEmail(networkEmail))
            {
                Display.ShowMessage("Invalid e-mail for network", "You have attempted to register an e-mail that is not associated with the network you are attempting to join. The e-mail address should have the form _user_@" + theNetwork.NetworkNetwork + ". Go back and enter a valid e-mail address for this network.");
                return;
            }

            if (!NetworkMember.CheckNetworkEmailUnique(db, networkEmail))
            {
                NetworkMember member = new NetworkMember(core, (int)theNetwork.Id, (int)loggedInMember.Id);
                if (!member.IsMemberActive)
                {
                    theNetwork.ResendConfirmationKey(core, member);

                    Display.ShowMessage("Confirmation Required", "Before you are able to finish joining the network you must confirm your network e-mail address. An confirmation e-mail has been sent to your network e-mail address with a link to click. Once you confirm your e-mail address you will be able to join the network.");
                    return;
                }
                else
                {
                    Display.ShowMessage("Error", "The e-mail address you have attempted to register with the network is already in use with another account.");
                    return;
                }
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

        void AccountNetworksMemberships_Leave(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountNetworksMemberships_Leave_Save(object sender, EventArgs e)
        {
        }
    }
}
