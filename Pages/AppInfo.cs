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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    public class AppInfo : Application
    {
        public override string Title
        {
            get
            {
                return "Pages";
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

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("lists", @"^/lists(|/)$", AppPrimitives.Member);
            aii.AddSlug("lists", @"^/lists/([A-Za-z0-9\-_]+)(|/)$", AppPrimitives.Member);
            aii.AddSlug("*", @"^/([A-Za-z0-9\-_/]+)(|/)$", AppPrimitives.Member);

            aii.AddModule("pages");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            core.RegisterApplicationPage(@"^/lists(|/)$", showLists, 1);
            core.RegisterApplicationPage(@"^/lists/([A-Za-z0-9\-_]+)(|/)$", showList, 2);
            core.RegisterApplicationPage(@"^/([A-Za-z0-9\-_/]+)(|/)$", showPage, int.MaxValue);
        }

        private void showPage(Core core, object sender)
        {
            if (sender is PPage)
            {
                Page.Show(core, (PPage)sender, core.PagePathParts[1].Value);
            }
        }

        private void showLists(Core core, object sender)
        {
            if (sender is PPage)
            {
                List.ShowLists(core, (PPage)sender);
            }
        }

        private void showList(Core core, object sender)
        {
            if (sender is PPage)
            {
                List.Show(core, (PPage)sender, core.PagePathParts[1].Value);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member;
        }

        void core_PageHooks(HookEventArgs eventArgs)
        {

        }
    }
}
