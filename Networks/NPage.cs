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
using System.Web.Security;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Networks
{
    public abstract partial class NPage : TPage
    {
        protected string networkNetwork;
        protected Network theNetwork;

        public Network TheNetwork
        {
            get
            {
                return theNetwork;
            }
        }

        public NPage()
            : base()
        {
            page = 1;
        }

        public NPage(string templateFile)
            : base(templateFile)
        {
            page = 1;
        }

        protected void BeginNetworkPage()
        {
            networkNetwork = HttpContext.Current.Request["nn"];

            try
            {
                theNetwork = new Network(db, networkNetwork);
            }
            catch (InvalidNetworkException)
            {
                Functions.Generate404();
                return;
            }

            Core.PagePath = Core.PagePath.Substring(theNetwork.NetworkNetwork.Length + 1 + 8);
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = "/profile";
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Network, core.PagePath, BoxSocial.Internals.Application.GetApplications(Core, theNetwork));

            PageTitle = theNetwork.DisplayName;

            if (loggedInMember != null && HttpContext.Current.Request.QueryString["mode"] == "activate")
            {
                try
                {
                    if (loggedInMember.UserId == long.Parse(HttpContext.Current.Request.QueryString["id"]))
                    {
                        if (theNetwork.Activate(this, loggedInMember, HttpContext.Current.Request.QueryString["key"]))
                        {
                            template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(theNetwork.Uri));
                            Display.ShowMessage("Joined Network", "You have successfully joined the network.");
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