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

namespace BoxSocial.Networks
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
            core.AddPrimitiveType(typeof(Network));
        }

        public override string Title
        {
            get
            {
                return "Networks";
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
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
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
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            /*aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Network);
            aii.AddSlug("members", @"^/members(|/)$", AppPrimitives.Network);

            aii.AddModule("networks");*/

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
            /*core.RegisterApplicationPage(@"^/profile(|/)$", showNetwork);
            core.RegisterApplicationPage(@"^/members(|/)$", showMemberlist);*/
        }

        [Show("profile", AppPrimitives.Network)]
        private void showNetwork(Core core, object sender)
        {
            if (sender is NPage)
            {
                Network.Show(core, (NPage)sender);
            }
        }

        [Show("members", AppPrimitives.Network)]
        private void showMemberlist(Core core, object sender)
        {
            if (sender is NPage)
            {
                Network.ShowMemberlist(core, (NPage)sender);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Network;
        }

        void core_PageHooks(HookEventArgs e)
        {
        }
    }
}
