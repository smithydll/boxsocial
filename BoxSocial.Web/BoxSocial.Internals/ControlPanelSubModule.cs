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
using System.Runtime.CompilerServices;
using System.Web;
using BoxSocial;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class ControlPanelSubModule : MarshalByRefObject, IComparable
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
        //protected HttpRequest Request;
        //protected HttpResponse Response;
        //protected HttpServerUtility Server;
        private Dictionary<string, ModuleModeHandler> modes = new Dictionary<string, ModuleModeHandler>();
        private Dictionary<string, EventHandler> saveHandlers = new Dictionary<string, EventHandler>();
        private VariableCollection parentModulesVariableCollection;

        protected Form Form;

        public Primitive SetOwner
        {
            set
            {
                Owner = value;
            }
        }

        protected VariableCollection ParentModulesVariableCollection
        {
            get
            {
                return parentModulesVariableCollection;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ControlPanelSubModule class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public ControlPanelSubModule(Core core, Primitive owner)
        {
            this.core = core;
            this.db = core.Db;
            this.session = core.Session;
            this.tz = core.Tz;
            this.LoggedInMember = session.LoggedInMember;
            this.Owner = owner;

            core.Prose.AddApplication(Assembly.GetAssembly(this.GetType()).GetName().Name);
        }

        /// <summary>
        /// We do this so we don't have to keep re-declaring the same
        /// constructor
        /// </summary>
        /// <param name="core">Core token</param>
        public void ModuleVector(Core core, VariableCollection vc)
        {
            ModuleVector(core, core.Session.LoggedInMember, vc);
        }

        /// <summary>
        /// We do this so we don't have to keep re-declaring the same
        /// constructor
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="owner">Owner</param>
        public void ModuleVector(Core core, Primitive owner, VariableCollection vc)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.core = core;
            this.db = core.Db;
            this.session = core.Session;
            this.tz = core.Tz;
            this.Owner = owner;
            this.LoggedInMember = session.LoggedInMember;
            this.parentModulesVariableCollection = vc;

            CreateTemplate();

            string mode = core.Http["mode"];

            EventHandler loadHandler = Load;
            if (loadHandler != null)
            {
                loadHandler(this, new EventArgs());
            }

            if (string.IsNullOrEmpty(mode) || !HasModeHandler(mode))
            {
                EventHandler showHandler = Show;
                if (showHandler != null)
                {
                    showHandler(this, new EventArgs());
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
        public delegate void ItemModuleModeHandler(object sender, ItemModuleModeEventArgs e);

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
            if (core.Http.Form["save"] != null)
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
            if (core.Http.Form["save"] != null)
            {
                saveHandler((object)this, new EventArgs());
            }
        }

        protected void SaveMode(ModuleModeHandler saveHandler)
        {
            if (core.Http.Form["save"] != null)
            {
                if (core.Http.Form["mode"] != null)
                {
                    saveHandler((object)this, new ModuleModeEventArgs(core.Http.Form["mode"]));
                }
            }
        }

        protected void SaveItemMode(ItemModuleModeHandler saveHandler, NumberedItem item)
        {
            if (core.Http.Form["save"] != null)
            {
                if (core.Http.Form["mode"] != null)
                {
                    saveHandler((object)this, new ItemModuleModeEventArgs(core.Http.Form["mode"], item));
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
            string formSubmitUri = string.Empty;
            template = new Template(core.Http.TemplatePath, "1301.html");
            template.Medium = core.Template.Medium;
            if (Owner != null)
            {
                formSubmitUri = core.Hyperlink.AppendSid(Owner.AccountUriStub, true);
                template.Parse("U_ACCOUNT", formSubmitUri);
                template.Parse("S_ACCOUNT", formSubmitUri);
            }
            template.AddPageAssembly(Assembly.GetCallingAssembly());
            template.SetProse(core.Prose);

            Form = new Form("control-panel", formSubmitUri);
            Form.SetValues(core.Http.Form);
            if (core.Http.Form["save"] != null)
            {
                Form.IsFormSubmission = true;
            }

            core.Template.Parse("IS_CONTENT", "FALSE");

            template.Parse("SITE_TITLE", core.Settings.SiteTitle);
        }

        /// <summary>
        /// Renders the template to the account panel.
        /// </summary>
        private void RenderTemplate()
        {
            core.Template.ParseRaw("MODULE_CONTENT", ((Template)template).ToString());
        }

        /// <summary>
        /// Renders an error to the account panel.
        /// </summary>
        /// <param name="errorMessage"></param>
        protected void DisplayError(string errorMessage)
        {
            template = new Template(core.Http.TemplatePath, "1302.html");
            template.Parse("ERROR_MESSAGE", errorMessage);
            RenderTemplate();
        }

        /// <summary>
        /// Renders an error to the account panel.
        /// </summary>
        /// <param name="errorMessage"></param>
        protected void DisplayGenericError()
        {
            template = new Template(core.Http.TemplatePath, "1302.html");
            template.ParseRaw("ERROR_MESSAGE", "An error has occured accessing this account module, maybe you are accessing it incorrectly. <a href=\"javascript:history.go(-1);\">Go Back</a>");
            RenderTemplate();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetTemplate(string templateName)
        {
            template.AddPageAssembly(Assembly.GetCallingAssembly());
            template.SetTemplate(Assembly.GetCallingAssembly().GetName().Name, templateName);

            core.Prose.AddApplication(Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Builds a URI to the current sub module
        /// </summary>
        /// <returns>URI built</returns>
        public string BuildUri()
        {
            return BuildUri(Key);
        }

        public string BuildUri(Core core)
        {
            return BuildUri(core, Key);
        }

        /// <summary>
        /// Builds a URI to a different sub module in this module
        /// </summary>
        /// <returns>URI built</returns>
        protected string BuildUri(string sub)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}",
                Owner.AccountUriStub, ModuleKey, sub));
        }

        protected string BuildUri(Core core, string sub)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}",
                Owner.AccountUriStub, ModuleKey, sub));
        }

        public string BuildUri(string sub, long id)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}?id={3}",
                Owner.AccountUriStub, ModuleKey, sub, id));
        }

        public string BuildUri(string sub, string mode)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}?mode={3}",
                Owner.AccountUriStub, ModuleKey, sub, mode));
        }

        public string BuildUri(string sub, string mode, long id)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}?mode={3}&id={4}",
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

            return core.Hyperlink.AppendSid(string.Format("{0}{1}/{2}{3}",
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
            core.Template.Parse("REDIRECT_URI", uri);
        }

        /// <summary>
        /// Sets an error in posting.
        /// </summary>
        /// <param name="errorString">Error string to be displayed</param>
        protected void SetError(string errorString)
        {
            core.Template.Parse("ERROR", errorString);
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
            core.Template.Parse("INFO", informationString);
        }

        public static bool AuthorisedRequest(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!core.Session.IsLoggedIn)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(core.Http.Query["nvid"]))
            {
                SelectQuery query = new SelectQuery(typeof(Notification));
                query.AddFields("notification_primitive_id");
                query.AddFields("notification_primitive_type_id");
                query.AddCondition("notification_verification_string", core.Http.Query["nvid"]);
                query.AddCondition("notification_primitive_id", core.Session.LoggedInMember.Id);
                query.AddCondition("notification_primitive_type_id", ItemType.GetTypeId(typeof(User)));

                if (core.Db.Query(query).Rows.Count == 0)
                {
                    return false;
                }
                else
                {
                    // This is OK
                    return true;
                }
            }

            if (core.Http.Query["sid"] != core.Session.SessionId)
            {
                if (string.IsNullOrEmpty(core.Http.Query["sid"]))
                {
                    return false;
                }

                SelectQuery query = new SelectQuery("user_sessions");
                query.AddFields("user_id");
                query.AddCondition("session_string", core.Http.Query["sid"]);
                query.AddCondition("user_id", core.Session.LoggedInMember.Id);
                query.AddCondition("session_signed_in", true);
                query.AddCondition("session_ip", core.Session.IPAddress.ToString());

                if (core.Db.Query(query).Rows.Count == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool AuthorisedRequest()
        {
            return AuthorisedRequest(core);
        }

        /// <summary>
        /// Authorises a request ensuring the SID is present in the URL to
        /// prevent undesired operation of the account panel for users.
        /// </summary>
        public static void AuthoriseRequestSid(Core core)
        {
            if (!AuthorisedRequest(core))
            {
                core.Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
            }
        }

        protected void AuthoriseRequestSid()
        {
            AuthoriseRequestSid(core);
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
            if (!(obj is ControlPanelSubModule)) return -1;
            int thisValue = Order;
            int thatValue = ((ControlPanelSubModule)obj).Order;
            if (thisValue == -1)
            {
                thisValue = int.MaxValue;
            }
            if (thatValue == -1)
            {
                thatValue = int.MaxValue;
            }
            return thisValue.CompareTo(thatValue);
        }

        protected void ParseBbcode(string templateVar, string input)
        {
            ParseBbcode(templateVar, input, null);
        }

        protected void ParseBbcode(string templateVar, string input, User owner)
        {
            if (owner != null)
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember, owner));
            }
            else
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember));
            }
        }

        /*protected void ParsePermissionsBox(string templateVar, ushort permission, List<string> permissions)
        {
            template.ParseRaw(templateVar, Functions.BuildPermissionsBox(permission, permissions));
        }*/

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
            template.ParseRaw(templateVar, ContentLicense.BuildLicenseSelectBox(core.Db, selectedLicense));
        }

        protected void ParseClassification(Core core, string templateVar, Classifications classification)
        {
            template.ParseRaw(templateVar, Classification.BuildClassificationBox(core, classification));
        }

        /*protected void ParseTimeZoneBox(string templateVar, string timeZone)
        {
            template.ParseRaw(templateVar, UnixTime.BuildTimeZoneSelectBox(timeZone));
        }*/

        protected Dictionary<string, string> GetModeHiddenFieldList()
        {
            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();

            hiddenFieldList.Add("module", this.ModuleKey);
            hiddenFieldList.Add("sub", this.Key);
            hiddenFieldList.Add("mode", core.Http["mode"]);

            return hiddenFieldList;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is ControlPanelSubModule)
            {
                ControlPanelSubModule c = (ControlPanelSubModule)obj;

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

        public static bool operator ==(ControlPanelSubModule a, ControlPanelSubModule b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ControlPanelSubModule a, ControlPanelSubModule b)
        {
            return !a.Equals(b);
        }

        public static bool operator >(ControlPanelSubModule a, ControlPanelSubModule b)
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

        public static bool operator <(ControlPanelSubModule a, ControlPanelSubModule b)
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

    public class ItemModuleModeEventArgs : EventArgs
    {
        private string mode;
        private NumberedItem item;

        public string Mode
        {
            get
            {
                return mode;
            }
        }

        public NumberedItem Item
        {
            get
            {
                return item;
            }
        }

        public ItemModuleModeEventArgs(string mode, NumberedItem item)
        {
            this.mode = mode;
            this.item = item;
        }
    }
}
