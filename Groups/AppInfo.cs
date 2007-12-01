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

namespace BoxSocial.Groups
{
    public class AppInfo : Application
    {
        public override string Title
        {
            get
            {
                return "Groups";
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

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Group);
            aii.AddSlug("members", @"^/members(|/)$", AppPrimitives.Group);

            aii.AddModule("groups");

            return aii;
        }

        void core_LoadApplication(Core core, object sender)
        {
            core.RegisterApplicationPage(@"^/profile(|/)$", showGroup);
            core.RegisterApplicationPage(@"^/members(|/)$", showMemberlist);
        }

        private void showGroup(Core core, object sender)
        {
            if (sender is GPage)
            {
                UserGroup.Show(core, (GPage)sender);
            }
        }

        private void showMemberlist(Core core, object sender)
        {
            if (sender is GPage)
            {
                UserGroup.ShowMemberlist(core, (GPage)sender);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group;
        }

        void core_PageHooks(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;

                switch (page.Signature)
                {
                    case PageSignature.viewprofile:
                        ShowMemberGroups(page.db, page);
                        break;
                }
            }
        }

        public void ShowMemberGroups(Mysql db, PPage page)
        {
            List<UserGroup> groups = UserGroup.GetUserGroups(db, page.ProfileOwner);
            if (groups.Count > 0)
            {
                page.template.ParseVariables("HAS_GROUPS", "TRUE");
            }

            foreach(UserGroup group in groups)
            {
                VariableCollection groupVariableCollection = page.template.CreateChild("groups_list");

                groupVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(group.DisplayName));
                groupVariableCollection.ParseVariables("U_GROUP", HttpUtility.HtmlEncode(group.Uri));
            }
        }
    }
}
