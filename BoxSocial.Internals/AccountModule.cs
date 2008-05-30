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
using System.Reflection;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{

    /// <summary>
    /// Defines the base class for account modules, which are components
    /// which applications use to plug into the account management system.
    /// </summary>
    public abstract class AccountModule : MarshalByRefObject, IComparable
    {
        public delegate void RegisterSubModuleHandler(string submodule);
        public event RegisterSubModuleHandler RegisterSubModule;

        /// <summary>
        /// A list of submodules registered in the current module
        /// </summary>
        protected Dictionary<string, string> subModules = new Dictionary<string, string>();

        protected Core core;
        protected TPage page;
        protected Mysql db;
        protected User loggedInMember;
        protected Template template;
        protected UnixTime tz;
        protected HttpRequest Request;
        protected HttpResponse Response;
        protected HttpServerUtility Server;
        protected SessionState session;

        /// <summary>
        /// The assembly associated with the account module
        /// </summary>
        public Assembly assembly = null;

        /// <summary>
        /// Initialises an account module, registering the sub module
        /// registration handler.
        /// </summary>
        private AccountModule()
        {
            RegisterSubModule += new RegisterSubModuleHandler(OnRegisterSubModule);
        }

        /// <summary>
        /// Initialises an account module.
        /// </summary>
        /// <remarks>Binds the module to the account panel, and registers
        /// the sub module registration handler.</remarks>
        /// <param name="account"></param>
        public AccountModule(Account account)
            : base()
        {
            Bind(account);
        }

        /// <summary>
        /// Bind the module to the account panel.
        /// </summary>
        /// <param name="account"></param>
        private void Bind(Account account)
        {
            account.RegisterModule += new Account.RegisterModuleHandler(RegisterModule);

            core = account.core;
            page = account.core.page;
            db = account.core.db;
            loggedInMember = account.core.session.LoggedInMember;
            tz = account.core.tz;
            Request = HttpContext.Current.Request;
            Response = HttpContext.Current.Response;
            session = account.core.session;
            Server = HttpContext.Current.Server;
        }

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        public void CreateTemplate()
        {
            template = new Template("1301.html");
            template.ParseVariables("U_ACCOUNT", Linker.AppendSid("/account", true));
            if (assembly != null)
            {
                template.AddPageAssembly(assembly);
            }
        }

        /// <summary>
        /// Renders the template to the account panel.
        /// </summary>
        public void RenderTemplate()
        {
            core.template.ParseVariables("MODULE_CONTENT", template.ToString());
        }

        /// <summary>
        /// Callback on registration of a sub module in the account module.
        /// </summary>
        /// <param name="submodule">The sub module having been registered.</param>
        private void OnRegisterSubModule(string submodule)
        {
            
        }

        /// <summary>
        /// Callback on registration of the module in the account panel.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected abstract void RegisterModule(Core core, EventArgs e);

        /// <summary>
        /// Registers the sub modules in the account module with the account
        /// panel.
        /// </summary>
        /// <param name="submodule">The sub module having been called.</param>
        public void RegisterSubModules(string submodule)
        {
            this.RegisterSubModule(submodule);
        }

        /// <summary>
        /// Display name of the module.
        /// </summary>
        public abstract string Name
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

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        if (((AccountModuleAttribute)attr).Name != null)
                        {
                            return ((AccountModuleAttribute)attr).Name;
                        }
                    }
                }

                // null key, should not happen!!!
                return null;
            }
        }

        /// <summary>
        /// The order the module is to appear along the tab display.
        /// </summary>
        public abstract int Order
        {
            get;
        }

        /// <summary>
        /// Returns a list of sub modules registered with the account
        /// module.
        /// </summary>
        public Dictionary<string, string> SubModules
        {
            get
            {
                return subModules;
            }
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

        /// <summary>
        /// Builds a URI to the current module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri()
        {
            return Linker.AppendSid(string.Format("/account/{0}",
                Key));
        }

        /// <summary>
        /// Builds a URI to the module key given.
        /// </summary>
        /// <param name="module">Module key</param>
        /// <returns>URI built</returns>
        public static string BuildModuleUri(string module)
        {
            return Linker.AppendSid(string.Format("/account/{0}",
                module));
        }

        /// <summary>
        /// Builds a URI to the given sub module of the current module
        /// </summary>
        /// <param name="sub"></param>
        /// <returns>URI built</returns>
        protected string BuildUri(string sub)
        {
            return Linker.AppendSid(string.Format("/account/{0}/{1}",
                Key, sub));
        }

        /// <summary>
        /// Builds a URI to the sub module key given of a module key given.
        /// </summary>
        /// <param name="module">Module key</param>
        /// <param name="sub">Sub module key</param>
        /// <returns>URI built</returns>
        public static string BuildModuleUri(string module, string sub)
        {
            return Linker.AppendSid(string.Format("/account/{0}/{1}",
                module, sub));
        }

        /// <summary>
        /// Builds a URI to the sub module key given of a module key given,
        /// appending additional query string arguments given.
        /// </summary>
        /// <param name="module">Module key</param>
        /// <param name="sub">Sub module key</param>
        /// <param name="arguments">Additional query string arguments</param>
        /// <returns>URI built</returns>
        public static string BuildModuleUri(string module, string sub, Dictionary<string, string> arguments)
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
                module, sub, argumentList));
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
                Key, sub, argumentList));
        }

        /// <summary>
        /// Builds a URI to the sub module key given of a module key given,
        /// appending additional query string arguments given.
        /// </summary>
        /// <param name="module">Module key</param>
        /// <param name="sub">Sub module key</param>
        /// <param name="arguments">Additional query string arguments</param>
        /// <returns>URI built</returns>
        public static string BuildModuleUri(string module, string sub, params string[] arguments)
        {
            return BuildModuleUri(module, sub, false, arguments);
        }

        /// <summary>
        /// Builds a URI to the sub module key given of a module key given,
        /// appending additional query string arguments given.
        /// </summary>
        /// <param name="module">Module key</param>
        /// <param name="sub">Sub module key</param>
        /// <param name="appendSid">True if force appending SID, otherwise false</param>
        /// <param name="arguments">Additional query string arguments</param>
        /// <returns>URI built</returns>
        public static string BuildModuleUri(string module, string sub, bool appendSid, params string[] arguments)
        {
            string argumentList = "";

            foreach (string argument in arguments)
            {
                if (argumentList == "")
                {
                    argumentList = string.Format("?{0}",
                        argument);
                }
                else
                {
                    argumentList = string.Format("{0}&{1}",
                        argumentList, argument);
                }
            }

            return Linker.AppendSid(string.Format("/account/{0}/{1}{2}",
                module, sub, argumentList), appendSid);
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
            core.template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(uri));
        }

        /// <summary>
        /// Sets an error in posting.
        /// </summary>
        /// <param name="errorString">String of error to be posted</param>
        protected void SetError(string errorString)
        {
            core.template.ParseVariables("ERROR", errorString);
        }

        protected void AssertFormVariable(string var)
        {
        }
    }
}
