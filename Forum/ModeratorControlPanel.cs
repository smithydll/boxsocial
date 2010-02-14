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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public class ModeratorControlPanel
    {
        private Core core;
        private PPage page;
        private Template template;

        private List<ModeratorControlPanelModule> accountModules = new List<ModeratorControlPanelModule>();
        private List<ModeratorControlPanelSubModule> accountSubModules = new List<ModeratorControlPanelSubModule>();
        private Dictionary<string, Dictionary<string, string>> modulesList = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, string> modules = new Dictionary<string, string>();

        public ModeratorControlPanel(Core core, PPage page)
        {
            this.core = core;
            this.page = page;
            this.template = page.template;
        }

        void OnRegisterModule(object sender, EventArgs e)
        {

        }

        private void loadModulesFromAssembly(Account accountObject, Assembly assembly, string module)
        {
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(ModeratorControlPanelModule)))
                {
                    ModeratorControlPanelModule newModule = System.Activator.CreateInstance(type, accountObject) as ModeratorControlPanelModule;

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
                if (type.IsSubclassOf(typeof(ModeratorControlPanelSubModule)))
                {
                    ModeratorControlPanelSubModule newModule = System.Activator.CreateInstance(type, new object[] { }) as ModeratorControlPanelSubModule;

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

        public void ShowMCP(string module, string submodule)
        {
            module = (!String.IsNullOrEmpty(core.Http.Form["module"])) ? core.Http.Form["module"] : module;
            submodule = (!String.IsNullOrEmpty(core.Http.Form["sub"])) ? core.Http.Form["sub"] : submodule;

            module = (module == null) ? "" : module;
            module = (module == "") ? "dashboard" : module;
            submodule = (submodule == null) ? "" : submodule;

            if (!core.Session.IsLoggedIn)
            {
                SessionState.RedirectAuthenticate();
            }

            template.Parse("ACCOUNT_TITLE", "Moderator Control Panel :: " + "Forum Name");

            Account accountObject = new Account(core);

            /* Load all the MCP modules */
            {
                Assembly dashboardAssembly = Assembly.GetAssembly(typeof(ModeratorControlPanel));
                loadModulesFromAssembly(accountObject, dashboardAssembly, module);
                loadSubModulesFromAssembly(accountObject, dashboardAssembly, module);
            }

            accountObject.RegisterAllModules();

            accountModules.Sort();

            foreach (ModeratorControlPanelModule accountModule in accountModules)
            {
                VariableCollection modulesVariableCollection = template.CreateChild("module_list");

                modulesVariableCollection.Parse("NAME", accountModule.Name);
                if (string.IsNullOrEmpty(accountModule.Key))
                {
                    modulesVariableCollection.Parse("URI", this.page.Owner.AccountUriStub);
                }
                else
                {
                    modulesVariableCollection.Parse("URI", this.page.Owner.AccountUriStub + accountModule.Key);
                }

                if (module == accountModule.Key)
                {
                    accountModule.SetOwner = core.Session.LoggedInMember;
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

                        ApplicationEntry ae = new ApplicationEntry(core, this.page.Owner, accountModule.assembly.GetName().Name);

                        core.LoadUserProfile(ae.CreatorId);
                        core.Email.SendEmail(core.PrimitiveCache[ae.CreatorId].AlternateEmail, "An Error occured in your application `" + ae.Title + "` at ZinZam.com", ex.ToString());
                    }
                }
            }

            accountSubModules.Sort();

            foreach (ModeratorControlPanelSubModule asm in accountSubModules)
            {
                if (!string.IsNullOrEmpty(asm.Key) && asm.Order >= 0)
                {
                    VariableCollection modulesVariableCollection = template.CreateChild("account_links");

                    asm.SetOwner = this.page.Owner;

                    modulesVariableCollection.Parse("TITLE", asm.Title);
                    modulesVariableCollection.Parse("SUB", asm.Key);
                    modulesVariableCollection.Parse("MODULE", asm.ModuleKey);
                    modulesVariableCollection.Parse("URI", asm.BuildUri(core));
                }

                if ((asm.Key == submodule || (string.IsNullOrEmpty(submodule) && asm.IsDefault)) && asm.ModuleKey == module)
                {
                    asm.ModuleVector(core, this.page.Owner);
                }
            }
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            ModeratorControlPanel mcp = new ModeratorControlPanel(e.Core, e.Page);

            e.Page.template.SetTemplate("Forum", "mcp"); // account_master.html
            ForumSettings.ShowForumHeader(e.Core, e.Page);

            if (e.Core.PagePathParts.Count == 3)
            {
                mcp.ShowMCP(e.Core.PagePathParts[1].Value, e.Core.PagePathParts[2].Value);
            }
            else if (e.Core.PagePathParts.Count == 2)
            {
                mcp.ShowMCP(e.Core.PagePathParts[1].Value, "");
            }
            else
            {
                mcp.ShowMCP("", "");
            }
        }
    }
}
