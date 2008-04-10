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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public class AppInfo : Application
    {
        public override string Title
        {
            get
            {
                return "Forum";
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

        public override System.IO.Stream Icon
        {
            get
            {
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
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

            aii.AddSlug("forum", @"^/forum(|/)$", AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("forum", @"^/forum/topic\-([0-9])(|/)$", AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("forum", @"^/forum/([a-zA-Z0-9])/topic\-([0-9])(|/)$", AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("forum", @"^/forum/([a-zA-Z0-9])(|/)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("forum");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("forum", "Forum");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/forum(|/)$", showForums, 1);
            core.RegisterApplicationPage(@"^/forum/topic\-([0-9])(|/)$", showTopic, 2);
            core.RegisterApplicationPage(@"^/forum/([a-zA-Z0-9])/topic\-([0-9])(|/)$", showTopic, 3);
            core.RegisterApplicationPage(@"^/forum/([a-zA-Z0-9])(|/)$", showForum, 4);
        }

        private void showForums(Core core, object sender)
        {
        }

        private void showTopic(Core core, object sender)
        {
        }

        private void showForum(Core core, object sender)
        {
        }

        void core_PageHooks(HookEventArgs e)
        {
        }
    }
}