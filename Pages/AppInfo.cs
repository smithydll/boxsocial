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
using BoxSocial.Musician;

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
                return null;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
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
            core.PrimitiveHeadHooks += new Core.HookHandler(core_PrimitiveHeadHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override bool ExecuteJob(Job job)
        {
            if (job.ItemId == 0)
            {
                return true;
            }

            switch (job.Function)
            {
                case "notifyPageComment":
                    Page.NotifyPageComment(core, job);
                    return true;
            }

            return false;
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            aii.AddSlug("*", @"^/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician);

            return aii;
        }

        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                slugs.Add("lists", new PageSlugAttribute("Lists", AppPrimitives.Member));
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            core.RegisterApplicationPage(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, @"^/([A-Za-z0-9\-_/]+)(|/)$", showPage, int.MaxValue, false);
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
            else if (sender is MPage)
            {
                Page.Show(core, ((MPage)sender).Musician, core.PagePathParts[1].Value);
            }
        }

        [Show(@"^/lists(|/)$", AppPrimitives.Member)]
        private void showLists(Core core, object sender)
        {
            if (sender is PPage)
            {
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
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician;
        }

        void core_PageHooks(HookEventArgs e)
        {
        }

        void core_HeadHooks(HookEventArgs e)
        {
        }

        void core_PrimitiveHeadHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                core.Template.Parse("TAB_LIST", "TRUE");
                Template template = new Template(Assembly.GetExecutingAssembly(), "header_navigation_tabs");
                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

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
                    e.core.AddPrimitiveHeadPanel(template);
                }
            }
        }
    }
}
