﻿/*
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
    public partial class groupaccount : GPage
    {

        private List<AccountModule> accountModules = new List<AccountModule>();
        private List<AccountSubModule> accountSubModules = new List<AccountSubModule>();
        private Dictionary<string, Dictionary<string, string>> modulesList = new Dictionary<string, Dictionary<string, string>>();

        public groupaccount()
            : base("account_master.html")
        {
            this.Load += new EventHandler(Page_Load);
            BeginGroupPage();
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
                    AccountSubModule newModule = System.Activator.CreateInstance(type, new object[] { Core }) as AccountSubModule;

                    if (newModule != null)
                    {
                        if (newModule.ModuleKey == module && (newModule.Primitives & AppPrimitives.Group) == AppPrimitives.Group)
                        {
                            accountSubModules.Add(newModule);
                        }
                    }
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string module = (!String.IsNullOrEmpty(Request.Form["module"])) ? Request.Form["module"] : Request.QueryString["module"];
            string submodule = (!String.IsNullOrEmpty(Request.Form["sub"])) ? Request.Form["sub"] : Request.QueryString["sub"];

            module = (module == null) ? "" : module;
            module = (module == "") ? "dashboard" : module;
            submodule = (submodule == null) ? "" : submodule;

            if (!session.IsLoggedIn)
            {
                SessionState.RedirectAuthenticate();
            }

            if (!Group.IsGroupOperator(loggedInMember.ItemKey))
            {
                core.Display.ShowMessage("Unauthorised", "You are unauthorised to manage this group.");
            }

            template.Parse("ACCOUNT_TITLE", "Group Control Panel");
            template.Parse("PRIMITIVE_TITLE", Group.DisplayName);
            template.Parse("U_PRIMITIVE", Group.Uri);

            template.Parse("IS_CONTENT", "FALSE");

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "account", "Group Control Panel" });

            Owner.ParseBreadCrumbs(breadCrumbParts);

            Account accountObject = new Account(Core);
            loadModules(accountObject, BoxSocial.Internals.Application.GetModuleApplications(core, Group), module);

            accountObject.RegisterModule += new Account.RegisterModuleHandler(OnRegisterModule);
            accountObject.RegisterAllModules();

            accountModules.Sort();

            foreach (AccountModule accountModule in accountModules)
            {
                if ((accountModule.Primitives & AppPrimitives.Group) == AppPrimitives.Group)
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("module_list");

                    modulesVariableCollection.Parse("NAME", accountModule.Name);
                    if (string.IsNullOrEmpty(accountModule.Key))
                    {
                        modulesVariableCollection.Parse("URI", Group.AccountUriStub);
                    }
                    else
                    {
                        modulesVariableCollection.Parse("URI", Group.AccountUriStub + accountModule.Key);
                    }

                    if (module == accountModule.Key)
                    {
                        ApplicationEntry ae = null;
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
                            ///Response.Write("<hr />" + ex.ToString() + "<hr />");
                            accountModule.DisplayError("");

                            core.LoadUserProfile(ae.CreatorId);
                            core.Email.SendEmail(core.PrimitiveCache[ae.CreatorId].UserInfo.PrimaryEmail, "An Error occured in your application `" + ae.Title + "` at " + Hyperlink.Domain, ex.ToString());
                        }

                        modulesVariableCollection.Parse("CURRENT", "TRUE");
                        if (ae != null && ae.HasJavascript)
                        {
                            VariableCollection javaScriptVariableCollection = template.CreateChild("javascript_list");

                            javaScriptVariableCollection.Parse("URI", @"/scripts/" + ae.Key + @".js");
                        }
                    }
                }
            }

            accountSubModules.Sort();

            foreach (AccountSubModule asm in accountSubModules)
            {
                if (!string.IsNullOrEmpty(asm.Key) && asm.Order >= 0)
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("account_links");

                    asm.SetOwner = Group;

                    modulesVariableCollection.Parse("TITLE", asm.Title);
                    modulesVariableCollection.Parse("SUB", asm.Key);
                    modulesVariableCollection.Parse("MODULE", asm.ModuleKey);
                    modulesVariableCollection.Parse("URI", asm.BuildUri(core));

                    if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                    {
                        modulesVariableCollection.Parse("CURRENT", "TRUE");
                    }
                }

                if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                {
                    asm.ModuleVector(core, Group);
                }
            }

            EndResponse();
        }
    }
}
