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
using BoxSocial.Forms;
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
        //protected User loggedInMember;
        /// <summary>
        /// Account panel owner
        /// </summary>
        protected Primitive Owner;
        /// <summary>
        /// Account panel viewer
        /// </summary>
        protected User LoggedInMember;
        protected HttpRequest Request;
        protected HttpResponse Response;
        protected HttpServerUtility Server;
        private Dictionary<string, ModuleModeHandler> modes = new Dictionary<string, ModuleModeHandler>();
        private Dictionary<string, EventHandler> saveHandlers = new Dictionary<string, EventHandler>();

        public Primitive SetOwner
        {
            set
            {
                Owner = value;
            }
        }

        /// <summary>
        /// We do this so we don't have to keep re-declaring the same
        /// constructor
        /// </summary>
        /// <param name="core">Core token</param>
        public void ModuleVector(Core core)
        {
            ModuleVector(core, core.session.LoggedInMember);
        }

        /// <summary>
        /// We do this so we don't have to keep re-declaring the same
        /// constructor
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Owner</param>
        public void ModuleVector(Core core, Primitive owner)
        {
            this.core = core;
            this.db = core.db;
            this.session = core.session;
            this.tz = core.tz;
            //this.loggedInMember = session.LoggedInMember;
            this.Owner = owner;
            this.LoggedInMember = session.LoggedInMember;
            this.Request = HttpContext.Current.Request;
            this.Response = HttpContext.Current.Response;
            this.Server = HttpContext.Current.Server;

            CreateTemplate();

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

            RenderTemplate();
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
                        AccountSubModuleAttribute asmattr = (AccountSubModuleAttribute)attr;
                        if (asmattr.Name != null)
                        {
                            return asmattr.Name;
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
                        AccountSubModuleAttribute asmattr = (AccountSubModuleAttribute)attr;
                        if (asmattr.Name != null)
                        {
                            return asmattr.ModuleName;
                        }
                    }
                }

                // null key, should not happen!!!
                return null;
            }
        }

        public bool IsDefault
        {
            get
            {
                Type type = this.GetType();

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountSubModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        AccountSubModuleAttribute asmattr = (AccountSubModuleAttribute)attr;
                        if (asmattr.Name != null)
                        {
                            return asmattr.IsDefault;
                        }
                    }
                }

                // null key, should not happen!!!
                return false;
            }
        }

        public AppPrimitives Primitives
        {
            get
            {
                Type type = this.GetType();

                foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountSubModuleAttribute), false))
                {
                    if (attr != null)
                    {
                        AccountSubModuleAttribute asmattr = (AccountSubModuleAttribute)attr;
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

        public delegate void ModuleModeHandler(object sender, ModuleModeEventArgs e);

        private bool HasModeHandler(string mode)
        {
            if (modes.ContainsKey(mode) || saveHandlers.ContainsKey(mode))
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
            if (Request.Form["save"] != null)
            {
                if (saveHandlers.ContainsKey(mode))
                {
                    Save(saveHandlers[mode]);
                    return;
                }
            }
            modes[mode](this, new ModuleModeEventArgs(mode));
        }

        protected void AddModeHandler(string mode, ModuleModeHandler modeHandler)
        {
            modes.Add(mode, modeHandler);
        }

        protected void AddSaveHandler(string mode, EventHandler saveHandler)
        {
            saveHandlers.Add(mode, saveHandler);
        }

        protected void Save(EventHandler saveHandler)
        {
            if (Request.Form["save"] != null)
            {
                saveHandler(this, new EventArgs());
            }
        }

        protected void SaveMode(ModuleModeHandler saveHandler)
        {
            if (Request.Form["save"] != null)
            {
                if (Request.Form["mode"] != null)
                {
                    saveHandler(this, new ModuleModeEventArgs(Request.Form["mode"]));
                }
            }
        }

        protected event EventHandler Load;
        protected event EventHandler Show;

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        private void CreateTemplate()
        {
            template = new Template("1301.html");
            if (Owner != null)
            {
                template.Parse("U_ACCOUNT", Linker.AppendSid(Owner.AccountUriStub, true));
                template.Parse("S_ACCOUNT", Linker.AppendSid(Owner.AccountUriStub, true));
            }
            template.AddPageAssembly(Assembly.GetCallingAssembly());
            template.SetProse(core.prose);
        }

        /// <summary>
        /// Renders the template to the account panel.
        /// </summary>
        private void RenderTemplate()
        {
            core.template.ParseRaw("MODULE_CONTENT", ((Template)template).ToString());
        }

        /// <summary>
        /// Renders an error to the account panel.
        /// </summary>
        /// <param name="errorMessage"></param>
        protected void DisplayError(string errorMessage)
        {
            template = new Template("1302.html");
            template.Parse("ERROR_MESSAGE", errorMessage);
            RenderTemplate();
        }

        /// <summary>
        /// Renders an error to the account panel.
        /// </summary>
        /// <param name="errorMessage"></param>
        protected void DisplayGenericError()
        {
            template = new Template("1302.html");
            template.ParseRaw("ERROR_MESSAGE", "An error has occured accessing this account module, maybe you are accessing it incorrectly. <a href=\"javascript:history.go(-1);\">Go Back</a>");
            RenderTemplate();
        }

        protected void SetTemplate(string templateName)
        {
            template.AddPageAssembly(Assembly.GetCallingAssembly());
            template.SetTemplate(Assembly.GetCallingAssembly().GetName().Name, templateName);

            core.prose.AddApplication(Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Builds a URI to the current sub module
        /// </summary>
        /// <returns>URI built</returns>
        public string BuildUri()
        {
            return BuildUri(Key);
        }

        /// <summary>
        /// Builds a URI to a different sub module in this module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri(string sub)
        {
            return Linker.AppendSid(string.Format("{0}{1}/{2}",
                Owner.AccountUriStub, ModuleKey, sub));
        }

        public string BuildUri(string sub, long id)
        {
            return Linker.AppendSid(string.Format("{0}{1}/{2}?id={3}",
                Owner.AccountUriStub, ModuleKey, sub, id));
        }

        public string BuildUri(string sub, string mode)
        {
            return Linker.AppendSid(string.Format("{0}{1}/{2}?mode={3}",
                Owner.AccountUriStub, ModuleKey, sub, mode));
        }

        public string BuildUri(string sub, string mode, long id)
        {
            return Linker.AppendSid(string.Format("{0}{1}/{2}?mode={3}&id={4}",
                Owner.AccountUriStub, ModuleKey, sub, mode, id), true);
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

            return Linker.AppendSid(string.Format("{0}{1}/{2}{3}",
                Owner.AccountUriStub, ModuleKey, sub, argumentList));
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
        /// <param name="errorString">Error string to be displayed</param>
        protected void SetError(string errorString)
        {
            core.template.Parse("ERROR", errorString);
        }
		
		/// <summary>
		/// Set an information display.
		/// </summary>
		/// <remarks>
		/// Can be used for displaying a success in saving information on a page.
		/// </remarks>
		/// <param name="informationString">Information string to be displayed</param>
		protected void SetInformation(string informationString)
		{
			core.template.Parse("INFO", informationString);
		}

        /// <summary>
        /// Authorises a request ensuring the SID is present in the URL to
        /// prevent undesired operation of the account panel for users.
        /// </summary>
        protected void AuthoriseRequestSid()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                if (string.IsNullOrEmpty(Request.QueryString["sid"]))
                {
                    Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                    return;
                }

                SelectQuery query = new SelectQuery("user_sessions");
                query.AddFields("user_id");
                query.AddCondition("session_string", Request.QueryString["sid"]);
                query.AddCondition("user_id", session.LoggedInMember.Id);
                query.AddCondition("session_signed_in", true);
                query.AddCondition("session_ip", session.IPAddress.ToString());

                if (db.Query(query).Rows.Count == 0)
                {
                    Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
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
            if (!(obj is AccountSubModule)) return -1;
            return Order.CompareTo(((AccountSubModule)obj).Order);
        }

        protected void ParseBbcode(string templateVar, string input)
        {
            ParseBbcode(templateVar, input, null);
        }

        protected void ParseBbcode(string templateVar, string input, User owner)
        {
            if (owner != null)
            {
                template.ParseRaw(templateVar, Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember, owner));
            }
            else
            {
                template.ParseRaw(templateVar, Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember));
            }
        }

        protected void ParsePermissionsBox(string templateVar, ushort permission, List<string> permissions)
        {
            template.ParseRaw(templateVar, Functions.BuildPermissionsBox(permission, permissions));
        }

        protected void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem));
        }

        protected void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem, disabledItems));
        }

        protected void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem));
        }

        protected void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem, disabledItems));
        }

        protected void ParseSelectBox(string templateVar, string name, Dictionary<string, string> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem));
        }

        protected void ParseSelectBox(string templateVar, string name, Dictionary<string, string> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem, disabledItems));
        }

        protected void ParseLicensingBox(string templateVar, byte selectedLicense)
        {
            template.ParseRaw(templateVar, ContentLicense.BuildLicenseSelectBox(core.db, selectedLicense));
        }

        protected void ParseClassification(string templateVar, Classifications classification)
        {
            template.ParseRaw(templateVar, Classification.BuildClassificationBox(classification));
        }

        protected void ParseTimeZoneBox(string templateVar, string timeZone)
        {
            template.ParseRaw(templateVar, UnixTime.BuildTimeZoneSelectBox(timeZone));
        }

    }

    public class ModuleModeEventArgs : EventArgs
    {
        private string mode;

        public string Mode
        {
            get
            {
                return mode;
            }
        }

        public ModuleModeEventArgs(string mode)
        {
            this.mode = mode;
        }
    }
}
