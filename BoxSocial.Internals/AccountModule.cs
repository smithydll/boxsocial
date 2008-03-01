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
    public abstract class AccountModule : MarshalByRefObject, IComparable
    {
        public delegate void RegisterSubModuleHandler(string submodule);
        public event RegisterSubModuleHandler RegisterSubModule;

        protected Dictionary<string, string> subModules = new Dictionary<string, string>();

        protected Core core;
        protected TPage page;
        protected Mysql db;
        protected Member loggedInMember;
        protected Template template;
        protected UnixTime tz;
        protected HttpRequest Request;
        protected HttpResponse Response;
        protected HttpServerUtility Server;
        protected SessionState session;

        public Assembly assembly = null;

        private AccountModule()
        {
            RegisterSubModule += new RegisterSubModuleHandler(OnRegisterSubModule);
        }

        public AccountModule(Account account)
            : base()
        {
            Bind(account);
        }

        private void Bind(Account account)
        {
            account.RegisterModule += new Account.RegisterModuleHandler(RegisterModule);

            core = account.core;
            page = account.core.page;
            db = account.core.db;
            loggedInMember = account.core.session.LoggedInMember;
            //template = account.core.template;
            tz = account.core.tz;
            Request = HttpContext.Current.Request;
            Response = HttpContext.Current.Response;
            session = account.core.session;
            Server = HttpContext.Current.Server;
        }

        public void CreateTemplate()
        {
            template = new Template("1301.html");
            if (assembly != null)
            {
                template.AddPageAssembly(assembly);
            }
        }

        public void RenderTemplate()
        {
            core.template.ParseVariables("MODULE_CONTENT", template.ToString());
        }

        void OnRegisterSubModule(string submodule)
        {
            
        }

        protected abstract void RegisterModule(Core core, EventArgs e);

        public void RegisterSubModules(string submodule)
        {
            this.RegisterSubModule(submodule);
        }

        public abstract string Name
        {
            get;
        }

        public abstract string Key
        {
            get;
        }

        public abstract int Order
        {
            get;
        }

        public Dictionary<string, string> SubModules
        {
            get
            {
                return subModules;
            }
        }

        public void AuthoriseRequestSid()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is AccountModule)) return -1;
            return Order.CompareTo(((AccountModule)obj).Order);
        }

        public static string BuildModuleUri(string module)
        {
            return Linker.AppendSid(string.Format("/account/{0}",
                module));
        }

        public static string BuildModuleUri(string module, string sub)
        {
            return Linker.AppendSid(string.Format("/account/{0}/{1}",
                module, sub));
        }

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

        public static string BuildModuleUri(string module, string sub, params string[] arguments)
        {
            return BuildModuleUri(module, sub, false, arguments);
        }

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

        public void SetRedirectUri(string uri)
        {
            core.template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(uri));
        }

        public void SetError(string errorString)
        {
            core.template.ParseVariables("ERROR", errorString);
        }
    }
}
