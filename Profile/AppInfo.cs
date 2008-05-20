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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Profile
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
                return "Profile";
            }
        }

        public override string Stub
        {
            get
            {
                return "profile";
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
                return Properties.Resources.profile;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Application);
            aii.AddSlug("profile", @"^/status-feed(|/)$", AppPrimitives.Member | AppPrimitives.Application);

            aii.AddModule("profile");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("profile", "Profile");
                slugs.Add("friends", "Friends");
                slugs.Add("status-feed", "Status Feed");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/profile(|/)$", showProfile, 1);
            core.RegisterApplicationPage(@"^/status-feed(|/)$", showStatusFeed, 1);
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Application;
        }

        private void showProfile(Core core, object sender)
        {
            if (sender is PPage)
            {
                Member.ShowProfile(core, (PPage)sender);
            }
            else if (sender is APage)
            {
                ApplicationEntry.ShowPage(core, (APage)sender);
            }
        }

        private void showStatusFeed(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                StatusFeed.Show(core, page, page.ProfileOwner);
            }
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.None)
            {
                if (e.core.PagePath.ToLower() == "/default.aspx")
                {
                    //ShowStatusUpdates(e);
                }
            }
        }

        /*void ShowStatusUpdates(HookEventArgs e)
        {
        }*/
    }
}
