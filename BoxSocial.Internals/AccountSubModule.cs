﻿/*
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
using System.Reflection;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class AccountSubModule : MarshalByRefObject, IComparable
    {
        protected Core core;
        protected Mysql db;
        protected Template template;
        protected SessionState session;
        protected UnixTime tz;
        protected User loggedInMember;
        protected HttpRequest Request;
        protected HttpResponse Response;
        protected HttpServerUtility Server;
        private Dictionary<string, EventHandler> modes = new Dictionary<string,EventHandler>();

        /// <summary>
        /// We do this so we don't have to keep re-declaring the same
        /// constructor
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="template">Module inner template</param>
        public void ModuleVector(Core core, Template template)
        {
            this.core = core;
            this.db = core.db;
            this.template = template;
            this.session = core.session;
            this.tz = core.tz;
            this.loggedInMember = session.LoggedInMember;
            this.Request = HttpContext.Current.Request;
            this.Response = HttpContext.Current.Response;
            this.Server = HttpContext.Current.Server;

            string mode = HttpContext.Current.Request["mode"];

            if (Load != null)
            {
                Load(this, new EventArgs());
            }

            if (string.IsNullOrEmpty(mode) || !HasModeHandler(mode))
            {
                if (Show != null)
                {
                    Show(this, new EventArgs());
                }
            }
            else if (!string.IsNullOrEmpty(mode))
            {
                ShowMode(mode);
            }
        }

        /// <summary>
        /// The title of the sub-module, null for an invisible sub-module
        /// </summary>
        public abstract string Title
        {
            get;
        }

        /// <summary>
        /// The order the module is to appear along the tab display.
        /// </summary>
        public abstract int Order
        {
            get;
        }

        /// <summary>
        /// The unique key used to identify the module in requests.
        /// </summary>
        public string Key
        {
            get
            {
                Type type = this.GetType();

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountSubModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        if (((AccountSubModuleAttribute)attr).Name != null)
                        {
                            return ((AccountSubModuleAttribute)attr).Name;
                        }
                    }
                }

                // null key, should not happen!!!
                return null;
            }
        }

        public string ModuleKey
        {
            get
            {
                Type type = this.GetType();

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountSubModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        if (((AccountSubModuleAttribute)attr).Name != null)
                        {
                            return ((AccountSubModuleAttribute)attr).ModuleName;
                        }
                    }
                }

                // null key, should not happen!!!
                return null;
            }
        }

        private bool HasModeHandler(string mode)
        {
            if (modes.ContainsKey(mode))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ShowMode(string mode)
        {
            modes[mode](this, new EventArgs());
        }

        protected void AddModeHandler(string mode, EventHandler modeHandler)
        {
            modes.Add(mode, modeHandler);
        }

        protected void Save(EventHandler saveHandler)
        {
            if (Request.Form["save"] != null)
            {
                saveHandler(this, new EventArgs());
            }
        }

        protected event EventHandler Load;
        protected event EventHandler Show;

        protected void SetTemplate(string templateName)
        {
            template.SetTemplate(Assembly.GetCallingAssembly().GetName().Name, templateName);
        }

        /// <summary>
        /// Builds a URI to the current sub module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri()
        {
            return BuildUri(Key);
        }

        /// <summary>
        /// Builds a URI to a different sub module in this module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri(string sub)
        {
            return Linker.AppendSid(string.Format("/account/{1}/{0}",
                ModuleKey, sub));
        }

        /// <summary>
        /// Builds a URI to the sub module key given of the current module,
        /// appending additional query string arguments given.
        /// </summary>
        /// <param name="sub">Sub module key</param>
        /// <param name="arguments">Additional query string arguments</param>
        /// <returns>URI built</returns>
        public string BuildUri(Dictionary<string, string> arguments)
        {
            return BuildUri(Key, arguments);
        }

        /// <summary>
        /// Builds a URI to the sub module key given of the current module,
        /// appending additional query string arguments given.
        /// </summary>
        /// <param name="sub">Sub module key</param>
        /// <param name="arguments">Additional query string arguments</param>
        /// <returns>URI built</returns>
        public string BuildUri(string sub, Dictionary<string, string> arguments)
        {
            string argumentList = "";
            foreach (string key in arguments.Keys)
            {
                if (argumentList == "")
                {
                    argumentList = string.Format("?{0}={1}",
                        key, arguments[key]);
                }
                else
                {
                    argumentList = string.Format("{0}&{1}={2}",
                        argumentList, key, arguments[key]);
                }
            }

            return Linker.AppendSid(string.Format("/account/{0}/{1}{2}",
                ModuleKey, sub, argumentList));
        }

        /// <summary>
        /// Sets the redirect URI.
        /// </summary>
        /// <remarks>
        /// Useful for redirecting from a message box after posting a form.
        /// </remarks>
        /// <param name="uri">URI to redirect to</param>
        protected void SetRedirectUri(string uri)
        {
            core.template.Parse("REDIRECT_URI", uri);
        }

        /// <summary>
        /// Sets an error in posting.
        /// </summary>
        /// <param name="errorString">String of error to be posted</param>
        protected void SetError(string errorString)
        {
            core.template.Parse("ERROR", errorString);
        }

        /// <summary>
        /// Authorises a request ensuring the SID is present in the URL to
        /// prevent undesired operation of the account panel for users.
        /// </summary>
        protected void AuthoriseRequestSid()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }
        }

        /// <summary>
        /// Implements CompareTo
        /// </summary>
        /// <remarks>
        /// Comparison based on Order. Can be used to sort a list of
        /// modules in the desired display order.
        /// </remarks>
        /// <param name="obj">Object to compare with</param>
        /// <returns>Comparisson value</returns>
        public int CompareTo(object obj)
        {
            if (!(obj is AccountModule)) return -1;
            return Order.CompareTo(((AccountModule)obj).Order);
        }
    }
}
