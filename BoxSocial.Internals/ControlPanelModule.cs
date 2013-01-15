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
    public abstract class ControlPanelModule : MarshalByRefObject, IComparable
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
        protected Primitive Owner;
        protected User loggedInMember;
        protected Template template;
        protected UnixTime tz;
        //protected HttpRequest Request;
        //protected HttpResponse Response;
        //protected HttpServerUtility Server;
        protected SessionState session;

        public Primitive SetOwner
        {
            set
            {
                Owner = value;
            }
        }

        /// <summary>
        /// The assembly associated with the account module
        /// </summary>
        public Assembly assembly = null;

        /// <summary>
        /// Initialises an account module, registering the sub module
        /// registration handler.
        /// </summary>
        private ControlPanelModule ()
        {
            RegisterSubModule += new RegisterSubModuleHandler(OnRegisterSubModule);
        }

        /// <summary>
        /// Initialises an account module.
        /// </summary>
        /// <remarks>Binds the module to the account panel, and registers
        /// the sub module registration handler.</remarks>
        /// <param name="account"></param>
        public ControlPanelModule(Account account)
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
            db = account.core.Db;
            loggedInMember = account.core.Session.LoggedInMember;
            tz = account.core.Tz;
            session = account.core.Session;
        }

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        public void CreateTemplate()
        {
            template = new Template(core.Http.TemplatePath, "1301.html");
            template.Parse("U_ACCOUNT", core.Uri.AppendSid(Owner.AccountUriStub, true));
            if (assembly != null)
            {
                template.AddPageAssembly(assembly);
                template.SetProse(core.Prose);
            }

            core.Template.Parse("IS_CONTENT", "FALSE");
        }

        /// <summary>
        /// Renders the template to the account panel.
        /// </summary>
        public void RenderTemplate()
        {
            core.Template.ParseRaw("MODULE_CONTENT", template.ToString());
        }

        /// <summary>
        /// Renders an error to the account panel.
        /// </summary>
        /// <param name="errorMessage"></param>
        public void DisplayError(string errorMessage)
        {
            template = new Template(core.Http.TemplatePath, "1302.html");
            template.Parse("ERROR_MESSAGE", errorMessage);
            RenderTemplate();
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
            //this.RegisterSubModule(submodule);
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

        public AppPrimitives Primitives
        {
            get
            {
                Type type = this.GetType();

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        AccountModuleAttribute asmattr = (AccountModuleAttribute)attr;
                        if (asmattr.Name != null)
                        {
                            return asmattr.Primitives;
                        }
                    }
                }

                // null key, should not happen!!!
                return AppPrimitives.None;
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
            if (core.Http.Query["sid"] != session.SessionId)
            {
                if (string.IsNullOrEmpty(core.Http.Query["sid"]))
                {
                    core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                    return;
                }

                SelectQuery query = new SelectQuery("user_sessions");
                query.AddCondition("session_string", core.Http.Query["sid"]);
                query.AddCondition("user_id", session.LoggedInMember.Id);
                query.AddCondition("session_signed_in", true);
                query.AddCondition("session_ip", session.IPAddress.ToString());

                if (db.Query(query).Rows.Count == 0)
                {
                    core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                    return;
                }
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
            if (!(obj is ControlPanelModule)) return -1;
            return Order.CompareTo(((ControlPanelModule)obj).Order);
        }

        /// <summary>
        /// Builds a URI to the current module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri()
        {
            return core.Uri.AppendSid(string.Format("{0}{1}",
                Owner.AccountUriStub, Key));
        }

        /// <summary>
        /// Builds a URI to the given sub module of the current module
        /// </summary>
        /// <param name="sub"></param>
        /// <returns>URI built</returns>
        protected string BuildUri(string sub)
        {
            return core.Uri.AppendSid(string.Format("{0}{1}/{2}",
                Owner.AccountUriStub, Key, sub));
        }

        public string BuildModuleUri(string sub, string mode, bool appendSid)
        {
            return core.Uri.BuildAccountSubModuleUri(Owner, Key, sub, mode, appendSid);
        }

        public string BuildModuleUri(string sub, string mode, long id)
        {
            return core.Uri.BuildAccountSubModuleUri(Owner, Key, sub, mode, id);
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
            string argumentList = string.Empty;
            foreach (string key in arguments.Keys)
            {
                if (argumentList == string.Empty)
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

            return core.Uri.AppendSid(string.Format("{0}{1}/{2}{3}",
                Owner.AccountUriStub, Key, sub, argumentList));
        }

        public string BuildUri(string module, string sub, params string[] arguments)
        {
            return core.Uri.BuildAccountSubModuleUri(Owner, module, sub, false, arguments);
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
            core.Template.Parse("REDIRECT_URI", uri);
        }

        /// <summary>
        /// Sets an error in posting.
        /// </summary>
        /// <param name="errorString">String of error to be posted</param>
        protected void SetError(string errorString)
        {
            core.Template.Parse("ERROR", errorString);
        }

        protected void AssertFormVariable(string var)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is ControlPanelModule)
            {
                ControlPanelModule c = (ControlPanelModule)obj;

                if (c.Order == Order && c.Key == Key)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ControlPanelModule a, ControlPanelModule b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ControlPanelModule a, ControlPanelModule b)
        {
            return !a.Equals(b);
        }

        public static bool operator >(ControlPanelModule a, ControlPanelModule b)
        {
            if (a.Order > b.Order)
            {
                return true;
            }
            if (a.Order == b.Order && a.Key.CompareTo(b.Key) > 0)
            {
                return true;
            }
            return false;
        }

        public static bool operator <(ControlPanelModule a, ControlPanelModule b)
        {
            if (a.Order < b.Order)
            {
                return true;
            }
            if (a.Order == b.Order && a.Key.CompareTo(b.Key) < 0)
            {
                return true;
            }
            return false;
        }
    }

    [DataTable("account_modules")]
    public class ControlPanelModuleRegister : NumberedItem
    {
        [DataField("module_id", DataFieldKeys.Primary)]
        private long moduleId;
        [DataField("module_module", 63)]
        private string moduleKey;
        [DataField("application_id")]
        private int applicationId;
        [DataField("module_updated_ut")]
        private long updated;

        public ControlPanelModuleRegister(Core core, long moduleId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ControlPanelModuleRegister_ItemLoad);

            try
            {
                LoadItem(moduleId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidAccountModuleException();
            }
        }

        private void ControlPanelModuleRegister_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return moduleId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidAccountModuleException : Exception
    {
    }
}
