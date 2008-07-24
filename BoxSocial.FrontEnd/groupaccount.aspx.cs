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
                        assemblyPath = HttpContext.Current.Server.MapPath(string.Format("{0}bin{0}{1}.dll", Path.DirectorySeparatorChar, ae.AssemblyName));
                    }
                    else
                    {
                        assemblyPath = HttpContext.Current.Server.MapPath(string.Format("{0}bin{0}applications{0}{1}.dll", Path.DirectorySeparatorChar, ae.AssemblyName));
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
            }
        }

        private void loadSubModulesFromAssembly(Account accountObject, Assembly assembly, string module)
        {
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(AccountSubModule)))
                {
                    AccountSubModule newModule = System.Activator.CreateInstance(type, new object[] { }) as AccountSubModule;

                    if (newModule != null)
                    {
                        if (newModule.ModuleKey == module)
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

            if (!thisGroup.IsGroupOperator(loggedInMember))
            {
                Display.ShowMessage("Unauthorised", "You are unauthorised to manage this group.");
            }

            Account accountObject = new Account(Core);
            loadModules(accountObject, BoxSocial.Internals.Application.GetModuleApplications(core, thisGroup), module);

            accountObject.RegisterModule += new Account.RegisterModuleHandler(OnRegisterModule);
            accountObject.RegisterAllModules();

            accountModules.Sort();

            foreach (AccountModule accountModule in accountModules)
            {
                VariableCollection modulesVariableCollection = template.CreateChild("module_list");

                modulesVariableCollection.Parse("NAME", accountModule.Name);
                if (string.IsNullOrEmpty(accountModule.Key))
                {
                    modulesVariableCollection.Parse("URI", "/group/" + thisGroup.Key + "/account/");
                }
                else
                {
                    modulesVariableCollection.Parse("URI", "/group/" + thisGroup.Key + "/account/" + accountModule.Key);
                }

                if (module == accountModule.Key)
                {
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

                        ApplicationEntry ae = new ApplicationEntry(core, thisGroup, accountModule.assembly.GetName().Name);

                        core.LoadUserProfile(ae.CreatorId);
                        Email.SendEmail(core.UserProfiles[ae.CreatorId].AlternateEmail, "An Error occured in your application `" + ae.Title + "` at ZinZam.com", ex.ToString());
                    }
                }
            }

            accountSubModules.Sort();

            foreach (AccountSubModule asm in accountSubModules)
            {
                if (!string.IsNullOrEmpty(asm.Key) && asm.Order >= 0)
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("account_links");

                    asm.SetOwner = thisGroup;

                    modulesVariableCollection.Parse("TITLE", asm.Title);
                    modulesVariableCollection.Parse("SUB", asm.Key);
                    modulesVariableCollection.Parse("MODULE", asm.ModuleKey);
                    modulesVariableCollection.Parse("URI", asm.BuildUri());
                }

                if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                {
                    asm.ModuleVector(core, thisGroup);
                }
            }

            EndResponse();
        }
    }
}