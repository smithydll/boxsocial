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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Networks;

namespace BoxSocial.FrontEnd
{
    public partial class account : UPage
    {

        private List<AccountModule> accountModules = new List<AccountModule>();
        private List<AccountSubModule> accountSubModules = new List<AccountSubModule>();
        private Dictionary<string, Dictionary<string, string>> modulesList = new Dictionary<string, Dictionary<string, string>>();

        public account()
            : base("account_master.html")
        {
            this.Load += new EventHandler(Page_Load);
        }

        void OnRegisterModule(object sender, EventArgs e)
        {
            
        }

        public void AddModule(string token, Dictionary<string, string> subModules)
        {
            modulesList.Add(token, subModules);
        }

        Dictionary<string, string> modules = new Dictionary<string, string>();

        private void loadModules(Account accountObject, List<ApplicationEntry> applicationsList, string module)
        {
            /*
             * Dashboard
             */
            Assembly dashboardAssembly = Assembly.GetAssembly(typeof(AccountDashboard));
            loadModulesFromAssembly(accountObject, dashboardAssembly, module);
            loadSubModulesFromAssembly(accountObject, dashboardAssembly, module);

            /*
             * Applications
             */
            foreach (ApplicationEntry ae in applicationsList)
            {
                if (ae.Modules != null && ae.Modules.Count > 0)
                {
                    string assemblyPath;
                    if (ae.IsPrimitive)
                    {
                        assemblyPath = Path.Combine(core.Http.AssemblyPath, string.Format("{0}.dll", ae.AssemblyName));
                    }
                    else
                    {
                        assemblyPath = Path.Combine(core.Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", ae.AssemblyName)));
                    }
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);

                    loadModulesFromAssembly(accountObject, assembly, module);
                    loadSubModulesFromAssembly(accountObject, assembly, module);
                }
            }
        }

        private void loadModulesFromAssembly(Account accountObject, Assembly assembly, string module)
        {
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(AccountModule)))
                {
					try
					{
	                    AccountModule newModule = System.Activator.CreateInstance(type, accountObject) as AccountModule;

	                    if (newModule != null)
	                    {
	                        newModule.assembly = assembly;
	                        accountModules.Add(newModule);
	                        if (newModule.Key == module)
	                        {
	                            core.AddPageAssembly(assembly);
	                        }
	                    }
					}
					catch (TargetInvocationException)
					{
						continue;
					}
                }
            }
        }

        private void loadSubModulesFromAssembly(Account accountObject, Assembly assembly, string module)
        {
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(AccountSubModule)))
                {
                    AccountSubModule newModule = System.Activator.CreateInstance(type, new object[] { Core, User }) as AccountSubModule;

                    if (newModule != null)
                    {
                        if (newModule.ModuleKey == module && (newModule.Primitives & AppPrimitives.Member) == AppPrimitives.Member)
                        {
                            accountSubModules.Add(newModule);
                        }
                    }
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string module = (!String.IsNullOrEmpty(core.Http.Form["module"])) ? core.Http.Form["module"] : core.Http.Query["module"];
            string submodule = (!String.IsNullOrEmpty(core.Http.Form["sub"])) ? core.Http.Form["sub"] : core.Http.Query["sub"];

            module = (module == null) ? "" : module;
            module = (module == "") ? "dashboard" : module;
            submodule = (submodule == null) ? "" : submodule;

            List<string> args = new List<string>();

            if ((!session.IsLoggedIn) || loggedInMember == null)
            {
                foreach (string key in Request.QueryString.Keys)
                {
					if (key != null)
					{
	                    if (key.ToLower() != "sid" && key.ToLower() != "module" && key.ToLower() != "sub")
	                    {
	                        args.Add(string.Format("{0}={1}", key, Request.QueryString[key]));
	                    }
					}
                }

                core.Http.Redirect(core.Hyperlink.BuildLoginUri(core.Hyperlink.StripSid(core.Hyperlink.BuildAccountSubModuleUri((Primitive)null, module, submodule, args.ToArray()))));
                return;
            }

            loggedInMember.LoadProfileInfo();

            template.Parse("ACCOUNT_TITLE", core.Prose.GetString("MY_ACCOUNT"));

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "account", core.Prose.GetString("MY_ACCOUNT") });

            ParseCoreBreadCrumbs(breadCrumbParts);
            //core.Display.ParseBreadCrumbs(breadCrumbParts);

            /*if ((loggedInMember.Permissions & 0x1111) == 0x0000)
            {
                template.ParseRaw("NO_PERMISSIONS", "You have not set any view permissions for your profile. No-one will be able to see your profile until you give they access. You can set access permissions from the <a href=\"/account/profile/permissions\">Profile Permissions</a> panel.");
            }*/

            if (!loggedInMember.UserInfo.ShowCustomStyles && !string.IsNullOrEmpty(loggedInMember.Style.RawCss))
            {
                template.ParseRaw("NO_CUSTOM_STYLE", "You have set a custom style for your site, yet you cannot view it as you have disabled custom styles. To view your custom style you must enable custom styles in your account <a href=\"/account/dashboard/preferences\">preferences</a>.");
            }

            Account accountObject = new Account(Core);
            loadModules(accountObject, BoxSocial.Internals.Application.GetModuleApplications(core, session.LoggedInMember), module);

            accountObject.RegisterModule += new Account.RegisterModuleHandler(OnRegisterModule);
            accountObject.RegisterAllModules();

            accountModules.Sort();

            VariableCollection parentModulesVariableCollection = null;
 
            ApplicationEntry ae = null;
            foreach (AccountModule accountModule in accountModules)
            {
                VariableCollection modulesVariableCollection = template.CreateChild("module_list");

                modulesVariableCollection.Parse("NAME", accountModule.Name);
                if (string.IsNullOrEmpty(accountModule.Key))
                {
                    modulesVariableCollection.Parse("URI", core.Hyperlink.AppendSid(loggedInMember.AccountUriStub));
                }
                else
                {
                    modulesVariableCollection.Parse("URI", core.Hyperlink.AppendSid(loggedInMember.AccountUriStub + accountModule.Key));
                }

                if (module == accountModule.Key)
                {
                    if (accountModule.assembly.GetName().Name != "BoxSocial.Internals")
                    {
                        ae = core.GetApplication(accountModule.assembly.GetName().Name);
                    }

                    accountModule.SetOwner = loggedInMember;
                    accountModule.CreateTemplate();
                    // catch all errors, don't want a single application to crash the account panel
                    try
                    {
                        accountModule.RegisterSubModules(submodule);
                        modules = accountModule.SubModules;
                        //accountModule.RenderTemplate();
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        // ignore this informational exception
                    }
                    catch (Exception ex)
                    {
                        // TODO: e-mail application author of the error details
                        accountModule.DisplayError("");

                        core.LoadUserProfile(ae.CreatorId);
                        core.Email.SendEmail(core.PrimitiveCache[ae.CreatorId].UserInfo.PrimaryEmail, "An Error occured in your application `" + ae.Title + "` at " + Hyperlink.Domain, ex.ToString());
                    }

                    modulesVariableCollection.Parse("CURRENT", "TRUE");
                    parentModulesVariableCollection = modulesVariableCollection;
                    if (ae != null && ae.HasJavascript)
                    {
                        //VariableCollection javaScriptVariableCollection = template.CreateChild("javascript_list");

                        //javaScriptVariableCollection.Parse("URI", @"/scripts/" + ae.Key + @".js");
                    }
                }
            }

            accountSubModules.Sort();

            bool orphan = true;

            foreach (AccountSubModule asm in accountSubModules)
            {
                if (!string.IsNullOrEmpty(asm.Key) && asm.Order >= 0)
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("account_links");
                    if (parentModulesVariableCollection != null)
                    {
                        parentModulesVariableCollection.CreateChild("account_links", modulesVariableCollection);
                    }

                    asm.SetOwner = loggedInMember;

                    modulesVariableCollection.Parse("TITLE", asm.Title);
                    modulesVariableCollection.Parse("SUB", asm.Key);
                    modulesVariableCollection.Parse("MODULE", asm.ModuleKey);
                    modulesVariableCollection.Parse("URI", asm.BuildUri(core));


                    if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                    {
                        modulesVariableCollection.Parse("CURRENT", "TRUE");
                        orphan = false;
                    }
                }

                if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                {
                    //try
                    if (ae != null)
                    {
                        if (ae.HasStyleSheet)
                        {
                            VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");

                            styleSheetVariableCollection.Parse("URI", @"/styles/applications/" + ae.Key + @".css");
                        }

                        if (ae.HasJavascript)
                        {
                            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");

                            javaScriptVariableCollection.Parse("URI", @"/scripts/" + ae.Key + @".js");
                        }
                    }
                    asm.ModuleVector(core, parentModulesVariableCollection);
                    /*catch (Exception ex)
                    {
                        throw new Exception(ex.ToString() + "\n\n\n" + db.ErrorList + "\n\n" + db.QueryList);
                    }*/
                }
            }

            if (orphan)
            {
                template.Parse("ORPHAN", "TRUE");
            }

            EndResponse();
        }        
    }
}
