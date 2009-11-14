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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Pages
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
        }

        public override string Title
        {
            get
            {
                return "Pages";
            }
        }

        public override string Stub
        {
            get
            {
                return "*";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override bool UsesComments
        {
            get
            {
                return false;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return false;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return null;
            }
        }

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.HeadHooks +=new Core.HookHandler(core_HeadHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("lists", @"^/lists(|/)$", AppPrimitives.Member);
            aii.AddSlug("lists", @"^/lists/([A-Za-z0-9\-_]+)(|/)$", AppPrimitives.Member);
            aii.AddSlug("*", @"^/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group);

            aii.AddModule("pages");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("lists", "Lists");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            //core.RegisterApplicationPage(@"^/lists(|/)$", showLists, 1);
            //core.RegisterApplicationPage(@"^/lists/([A-Za-z0-9\-_]+)(|/)$", showList, 2);
            core.RegisterApplicationPage(@"^/([A-Za-z0-9\-_/]+)(|/)$", showPage, int.MaxValue);
        }

        private void showPage(Core core, object sender)
        {
            if (sender is UPage)
            {
                Page.Show(core, ((UPage)sender).User, core.PagePathParts[1].Value);
            }
            else if (sender is GPage)
            {
                Page.Show(core, ((GPage)sender).Group, core.PagePathParts[1].Value);
            }
        }

        [Show(@"^/lists(|/)$", AppPrimitives.Member)]
        private void showLists(Core core, object sender)
        {
            if (sender is PPage)
            {
                //List.ShowLists(core, (UPage)sender);
                List.ShowLists(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"^/lists/([A-Za-z0-9\-_]+)(|/)$", AppPrimitives.Member)]
        private void showList(Core core, object sender)
        {
            if (sender is PPage)
            {
                List.Show(sender, new ShowPPageEventArgs((PPage)sender, core.PagePathParts[1].Value));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group;
        }

        void core_PageHooks(HookEventArgs e)
        {
        }

        void core_HeadHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "header_navigation_tabs");

                List<NagivationTab> tabs = NagivationTab.GetTabs(core, e.Owner);

                {
                    VariableCollection tabVariableCollection = template.CreateChild("tab_list");

                    tabVariableCollection.Parse("TITLE", "Home");
                    tabVariableCollection.Parse("U_TAB", e.Owner.Uri);
                }

                foreach (NagivationTab tab in tabs)
                {
                    VariableCollection tabVariableCollection = template.CreateChild("tab_list");

                    tabVariableCollection.Parse("TITLE", tab.Page.Title);
                    tabVariableCollection.Parse("U_TAB", tab.Page.Uri);
                }

                if (tabs.Count > 0)
                {
                    e.core.AddHeadPanel(template);
                }
            }
        }
    }
}
