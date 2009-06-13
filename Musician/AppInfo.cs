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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
            core.AddPrimitiveType(typeof(Musician));
        }

        public override string Title
        {
            get
            {
                return "Music";
            }
        }

        public override string Stub
        {
            get
            {
                return "music";
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
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.FootHooks += new Core.HookHandler(core_FootHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Musician);
            aii.AddSlug("members", @"^/members(|/)$", AppPrimitives.Musician);

            aii.AddModule("music");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                //slugs.Add("profile", "Profile");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            core.RegisterApplicationPage(@"^/profile(|/)$", showMusician);
            core.RegisterApplicationPage(@"^/members(|/)$", showMemberlist);
        }

        private void showMusician(Core core, object sender)
        {
            if (sender is MPage)
            {
                //UserGroup.Show(core, (MPage)sender);
            }
        }

        private void showMemberlist(Core core, object sender)
        {
            if (sender is MPage)
            {
                //UserGroup.ShowMemberlist(core, (GPage)sender);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Musician;
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                ShowMemberMusicians(e);
            }
        }

        void core_FootHooks(HookEventArgs e)
        {
        }

        public void ShowMemberMusicians(HookEventArgs e)
        {
            User profileOwner = (User)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilemusicians");

            /*List<UserGroup> groups = UserGroup.GetUserGroups(e.core, profileOwner);
            if (groups.Count > 0)
            {
                template.Parse("HAS_GROUPS", "TRUE");
            }

            foreach (UserGroup group in groups)
            {
                VariableCollection groupVariableCollection = template.CreateChild("groups_list");

                groupVariableCollection.Parse("TITLE", group.DisplayName);
                groupVariableCollection.Parse("U_GROUP", group.Uri);
            }*/

            e.core.AddSidePanel(template);
        }
    }
}
