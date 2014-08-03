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

        /// <summary>
        /// Initializes a new instance of the AccountNetworksMemberships class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountNetworksMemberships(Core core, Primitive owner)
            : base(core, owner)
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

            SelectQuery query = NetworkMember.GetSelectQueryStub(core, typeof(NetworkMember));
            query.AddFields(Network.GetFieldsPrefixed(core, typeof(Network)));
            query.AddFields(NetworkInfo.GetFieldsPrefixed(core, typeof(NetworkInfo)));
            query.AddJoin(JoinTypes.Inner, "network_keys", "network_id", "network_id");
            query.AddJoin(JoinTypes.Inner, "network_info", "network_id", "network_id");
            query.AddCondition("user_id", LoggedInMember.Id);

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

            AuthoriseRequestSid();

            long networkId = core.Functions.RequestLong("id", 0);

            if (networkId == 0)
            {
                DisplayGenericError();
                return;
            }

            template.Parse("S_ID", networkId.ToString());

            try
            {
                Network theNetwork = new Network(core, networkId);

                if (theNetwork.IsNetworkMember(LoggedInMember.ItemKey))
                {
                    SetRedirectUri(theNetwork.Uri);
                    core.Display.ShowMessage("Already a member", "You are already a member of this network");
                    return;
                }

                if (theNetwork.RequireConfirmation)
                {
                    // show form
                }
                else
                {
                    // just join the network
                    if (theNetwork.Join(core, LoggedInMember, "") != null)
                    {
                        SetRedirectUri(theNetwork.Uri);
                        core.Display.ShowMessage("Joined Network", "You have successfully joined the network.");
                        return;
                    }
                    else
                    {
                    }
                }
            }
            catch
            {
                core.Display.ShowMessage("Error", "The network you are trying to join does not exist, go back.");
                return;
            }
        }

        void AccountNetworksMemberships_Join_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long networkId;

            try
            {
                networkId = long.Parse(core.Http.Form["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            /*try
            {*/
            Network theNetwork = new Network(core, networkId);

            string networkEmail = core.Http.Form["email"];

            if (!theNetwork.IsValidNetworkEmail(networkEmail))
            {
                core.Display.ShowMessage("Invalid e-mail for network", "You have attempted to register an e-mail that is not associated with the network you are attempting to join. The e-mail address should have the form _user_@" + theNetwork.NetworkNetwork + ". Go back and enter a valid e-mail address for this network.");
                return;
            }

            if (!NetworkMember.CheckNetworkEmailUnique(db, networkEmail))
            {
                NetworkMember member = new NetworkMember(core, theNetwork.Id, LoggedInMember.Id);
                if (!member.IsMemberActive)
                {
                    theNetwork.ResendConfirmationKey(core, member);

                    core.Display.ShowMessage("Confirmation Required", "Before you are able to finish joining the network you must confirm your network e-mail address. An confirmation e-mail has been sent to your network e-mail address with a link to click. Once you confirm your e-mail address you will be able to join the network.");
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Error", "The e-mail address you have attempted to register with the network is already in use with another account.");
                    return;
                }
            }
            else if (theNetwork.Join(core, LoggedInMember, networkEmail) != null)
            {
                if (theNetwork.RequireConfirmation)
                {
                    core.Display.ShowMessage("Confirmation Required", "Before you are able to finish joining the network you must confirm your network e-mail address. An confirmation e-mail has been sent to your network e-mail address with a link to click. Once you confirm your e-mail address you will be able to join the network.");
                    return;
                }
                else
                {
                    SetRedirectUri(theNetwork.Uri);
                    core.Display.ShowMessage("Joined Network", "You have successfully joined the network.");
                    return;
                }
            }
            else
            {
                core.Display.ShowMessage("Error", "Could not join network.");
                return;
            }
            /*}
            catch
            {
            }*/
        }

        void AccountNetworksMemberships_Leave(object sender, ModuleModeEventArgs e)
        {
            long networkId = core.Functions.RequestLong("id", -1);

            if (networkId >= 0)
            {
                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", "networks");
                hiddenFieldList.Add("sub", "leave");
                hiddenFieldList.Add("id", networkId.ToString());

                core.Display.ShowConfirmBox(core.Hyperlink.AppendSid(Owner.AccountUriStub, true), "Leave network?", "Are you sure you want to leave this network?", hiddenFieldList);
            }
            else
            {
                DisplayGenericError();
                return;
            }
        }

        void AccountNetworksMemberships_Leave_Save(object sender, EventArgs e)
        {
            long networkId = core.Functions.RequestLong("id", -1);

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                try
                {
                    Network theNetwork = new Network(core, networkId);

                    if (theNetwork.IsNetworkMember(LoggedInMember.ItemKey))
                    {
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("DELETE FROM network_members WHERE network_id = {0} AND user_id = {1};",
                            theNetwork.Id, LoggedInMember.UserId));

                        db.UpdateQuery(string.Format("UPDATE network_info SET network_members = network_members - 1 WHERE network_id = {0}",
                            theNetwork.Id));

                        SetRedirectUri(theNetwork.Uri);
                        core.Display.ShowMessage("Left Network", "You have left the network.");
                        return;
                    }
                    else
                    {
                        SetRedirectUri(theNetwork.Uri);
                        core.Display.ShowMessage("Not a Member", "You cannot leave a network you are not a member of.");
                        return;
                    }
                }
                catch (InvalidNetworkException)
                {
                    DisplayGenericError();
                }
            }
            else
            {
            }
        }
    }
}
