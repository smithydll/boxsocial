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
            // TODO: Manage Networks
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

        private void JoinNetwork(string submodule)
        {
            subModules.Add("join", null);
            if (submodule != "join") return;

            if (Request.Form["join"] != null)
            {
                JoinNetworkSave();
                return;
            }

            template.SetTemplate("account_network_join.html");
            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(ZzUri.AppendSid("/account/", true)));

            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage(core, "Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long networkId;

            try
            {
                networkId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage(core, "Error", "An error has occured, go back.");
                return;
            }

            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(networkId.ToString()));

            try
            {
                Network theNetwork = new Network(db, networkId);

                if (theNetwork.IsNetworkMember(loggedInMember))
                {
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(theNetwork.Uri));
                    Display.ShowMessage(core, "Already a member", "You are already a member of this network");
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
                        template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(theNetwork.Uri));
                        Display.ShowMessage(core, "Joined Network", "You have successfully joined the network.");
                        return;
                    }
                    else
                    {
                    }
                }
            }
            catch
            {
                Display.ShowMessage(core, "Error", "The network you are trying to join does not exist, go back.");
                return;
            }
        }

        private void JoinNetworkSave()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage(core, "Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long networkId;

            try
            {
                networkId = long.Parse(Request.Form["id"]);
            }
            catch
            {
                Display.ShowMessage(core, "Error", "An error has occured, go back.");
                return;
            }

            /*try
            {*/
            Network theNetwork = new Network(db, networkId);

            string networkEmail = Request.Form["email"];

            if (!Member.CheckEmailUnique(db, networkEmail))
            {
                Display.ShowMessage(core, "Error", "The e-mail address you have attempted to register with the network is already in use with another account.");
                return;
            }
            else if (theNetwork.Join(core, loggedInMember, networkEmail) != null)
            {
                if (theNetwork.RequireConfirmation)
                {
                    Display.ShowMessage(core, "Confirmation Required", "Before you are able to finish joining the network you must confirm your network e-mail address. An confirmation e-mail has been sent to your network e-mail address with a link to click. Once you confirm your e-mail address you will be able to join the network.");
                    return;
                }
                else
                {
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(theNetwork.Uri));
                    Display.ShowMessage(core, "Joined Network", "You have successfully joined the network.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage(core, "Error", "Could not join network.");
                return;
            }
            /*}
            catch
            {
            }*/
        }
    }
}
