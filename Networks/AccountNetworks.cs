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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Networks;

namespace BoxSocial.Networks
{
    [AccountModule("networks")]
    public class AccountNetworks : AccountModule
    {
        public AccountNetworks(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManageNetworkMemberships);
            //RegisterSubModule += new RegisterSubModuleHandler(JoinNetwork);
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

        public override int Order
        {
            get
            {
                return 8;
            }
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
                Functions.ThrowError();
                return;
            }
        }

        private void LeaveNetworkSave()
        {

            long networkId = Functions.RequestLong("id", -1);

            try
            {
                Network theNetwork = new Network(core, networkId);

                if (theNetwork.IsNetworkMember(loggedInMember))
                {
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("DELETE FROM network_members WHERE network_id = {0} AND user_id = {1};",
                        theNetwork.Id, loggedInMember.UserId));

                    db.UpdateQuery(string.Format("UPDATE network_info SET network_members = network_members - 1 WHERE network_id = {0}",
                        theNetwork.Id));

                    SetRedirectUri(theNetwork.Uri);
                    Display.ShowMessage("Left Network", "You have left the network.");
                    return;
                }
                else
                {
                    SetRedirectUri(theNetwork.Uri);
                    Display.ShowMessage("Not a Member", "You cannot leave a network you are not a member of.");
                    return;
                }
            }
            catch (InvalidNetworkException)
            {
                Functions.ThrowError();
            }
        }
    }
}
