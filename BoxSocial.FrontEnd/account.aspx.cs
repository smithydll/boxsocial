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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Networks;

namespace BoxSocial.FrontEnd
{
    public partial class account : TPage
    {

        private List<AccountModule> accountModules = new List<AccountModule>();
        private Dictionary<string, Dictionary<string, string>> modulesList = new Dictionary<string, Dictionary<string, string>>();

        public account()
            : base("account_master.html")
        {
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
                        assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/{0}.dll", ae.AssemblyName));
                    }
                    else
                    {
                        assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/applications/{0}.dll", ae.AssemblyName));
                    }
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);

                    loadModulesFromAssembly(accountObject, assembly, module);
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

        protected void Page_Load(object sender, EventArgs e)
        {
            string module = (Request.Form["module"] != null) ? Request.Form["module"] : Request.QueryString["module"];
            string submodule = (Request.Form["sub"] != null) ? Request.Form["sub"] : Request.QueryString["sub"];

            module = (module == null) ? "" : module;
            module = (module == "") ? "dashboard" : module;
            submodule = (submodule == null) ? "" : submodule;

            if (!session.IsLoggedIn)
            {
                /*Response.Redirect(string.Format("/sign-in/?redirect=%2faccount%2f%3fmodule%3d{0}%26sub%3d{1}",
                    module, submodule));*/
                SessionState.RedirectAuthenticate();
            }

            loggedInMember.LoadProfileInfo();

            if ((loggedInMember.Permissions & 0x1111) == 0x0000)
            {
                template.ParseVariables("NO_PERMISSIONS", "You have not set any view permissions for your profile. No-one will be able to see your profile until you give they access. You can set access permissions from the <a href=\"/account/?module=profile&amp;sub=permissions\">Profile Permissions</a> panel.");
            }

            if (!loggedInMember.ShowCustomStyles && !string.IsNullOrEmpty(loggedInMember.GetUserStyle()))
            {
                template.ParseVariables("NO_CUSTOM_STYLE", "You have set a custom style for your site, yet you cannot view it as you have disabled custom styles. To view your custom style you must enable custom styles in your account <a href=\"/account/?module=&amp;sub=preferences\">preferences</a>.");
            }

            Account accountObject = new Account(Core);
            loadModules(accountObject, BoxSocial.Internals.Application.GetModuleApplications(core, session.LoggedInMember), module);
            /*Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(AccountModule)))
                    {
                        AccountModule newModule = System.Activator.CreateInstance(type, accountObject) as AccountModule;

                        if (newModule != null)
                        {
                            accountModules.Add(newModule);
                        }
                    }
                }
            }*/


            accountObject.RegisterModule += new Account.RegisterModuleHandler(OnRegisterModule);
            accountObject.RegisterAllModules();

            accountModules.Sort();
 
            foreach (AccountModule accountModule in accountModules)
            {
                VariableCollection modulesVariableCollection = template.CreateChild("module_list");

                modulesVariableCollection.ParseVariables("NAME", accountModule.Name);
                if (string.IsNullOrEmpty(accountModule.Key))
                {
                    modulesVariableCollection.ParseVariables("URI", "/account/");
                }
                else
                {
                    modulesVariableCollection.ParseVariables("URI", "/account/" + accountModule.Key);
                }

                if (module == accountModule.Key)
                {
                    accountModule.CreateTemplate();
                    accountModule.RegisterSubModules(submodule);
                    modules = accountModule.SubModules;
                    accountModule.RenderTemplate();
                }
            }

            foreach (string key in modules.Keys)
            {
                if (!string.IsNullOrEmpty(modules[key]))
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("account_links");

                    modulesVariableCollection.ParseVariables("TITLE", modules[key]);
                    modulesVariableCollection.ParseVariables("SUB", key);
                    modulesVariableCollection.ParseVariables("MODULE", module);
                }
            }

            EndResponse();
        }        
    }
}
