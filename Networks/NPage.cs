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
using System.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Networks
{
    public abstract partial class NPage : PPage
    {
        protected string networkNetwork;

        public Network Network
        {
            get
            {
                return (Network)primitive;
            }
        }

        public NPage()
            : base()
        {
            //page = 1;
        }

        public NPage(string templateFile)
            : base(templateFile)
        {
            //page = 1;
        }

        protected void BeginNetworkPage()
        {
            networkNetwork = core.Http["nn"];

            try
            {
                primitive = new Network(core, networkNetwork);
            }
            catch (InvalidNetworkException)
            {
                core.Functions.Generate404();
                return;
            }

            Core.PagePath = Core.PagePath.Substring(Network.NetworkNetwork.Length + 1 + 8);
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = "/profile";
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Network, core.PagePath, BoxSocial.Internals.Application.GetApplications(Core, Network));

            PageTitle = Network.DisplayName;

            if (loggedInMember != null && core.Http.Query["mode"] == "activate")
            {
                try
                {
                    if (loggedInMember.UserId == long.Parse(core.Http.Query["id"]))
                    {
                        if (Network.Activate(this, loggedInMember, core.Http.Query["key"]))
                        {
                            template.Parse("REDIRECT_URI", Network.Uri);
                            core.Display.ShowMessage("Joined Network", "You have successfully joined the network.");
                            return;
                        }
                    }
                    else
                    {
                        // not the logged in user, ignore
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
