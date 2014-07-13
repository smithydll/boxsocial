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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
            //core.AddPrimitiveType(typeof(UserGroup));
        }

        public override string Title
        {
            get
            {
                return "Groups";
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
                return string.Empty;
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
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member);

            return aii;
        }

        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
        }

        [Show(@"profile", AppPrimitives.Group)]
        private void showGroup(Core core, object sender)
        {
            if (sender is GPage)
            {
                UserGroup.Show(core, (GPage)sender);
            }
        }

        [Show(@"members", AppPrimitives.Group)]
        private void showMemberlist(Core core, object sender)
        {
            if (sender is GPage)
            {
                UserGroup.ShowMemberlist(core, (GPage)sender);
            }
        }

        [Show(@"groups/([A-Za-z0-9\-_]+)", AppPrimitives.Group)]
        private void showSubGroupProfile(Core core, object sender)
        {
            if (sender is GPage)
            {
                SubUserGroup.Show(sender, new ShowGPageEventArgs((GPage)sender));
            }
        }

        [StaticShow("groups", @"^/groups/register(|/)$")]
        private void showCreateGroup(Core core, object sender)
        {
            if (sender is TPage)
            {
                UserGroup.ShowRegister(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("groups", @"^/groups(|/)$")]
        private void showDefault(Core core, object sender)
        {
            if (sender is TPage)
            {
                Default.Show(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("groups", @"^/groups/([A-Za-z0-9\-_]+)(|/)$")]
        private void showCategory(Core core, object sender)
        {
            if (sender is TPage)
            {
                Default.ShowCategory(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group;
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.None)
            {
                if (e.core.PagePath.ToLower() == "/default.aspx")
                {
                    ShowGroups(e);
                }
            }
            if (e.PageType == AppPrimitives.Member)
            {
                ShowMemberGroups(e);
            }
        }

        void core_FootHooks(HookEventArgs e)
        {
        }

        void ShowGroups(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todaygrouppanel");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            List<UserGroup> groups = UserGroup.GetUserGroups(e.core, e.core.Session.LoggedInMember, 1, 4);

            foreach (UserGroup group in groups)
            {
                VariableCollection groupVariableCollection = template.CreateChild("groups_list");

                groupVariableCollection.Parse("TITLE", group.DisplayName);
                groupVariableCollection.Parse("MEMBERS", core.Functions.LargeIntegerToString(group.Members));
                groupVariableCollection.Parse("U_GROUP", group.Uri);
                groupVariableCollection.Parse("ICON", group.Icon);
                groupVariableCollection.Parse("TILE", group.Tile);
                groupVariableCollection.Parse("SQUARE", group.GroupSquare);
            }

            e.core.AddSidePanel(template);
        }

        public void ShowMemberGroups(HookEventArgs e)
        {
            User profileOwner = (User)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilegroups");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            List<UserGroup> groups = UserGroup.GetUserGroups(e.core, profileOwner, 1, 4);
            if (groups.Count > 0)
            {
                template.Parse("HAS_GROUPS", "TRUE");
            }

            foreach(UserGroup group in groups)
            {
                VariableCollection groupVariableCollection = template.CreateChild("groups_list");

                groupVariableCollection.Parse("TITLE", group.DisplayName);
                groupVariableCollection.Parse("U_GROUP", group.Uri);
                groupVariableCollection.Parse("ICON", group.Icon);
                groupVariableCollection.Parse("TILE", group.Tile);
                groupVariableCollection.Parse("SQUARE", group.GroupSquare);
            }

            e.core.AddSidePanel(template);
        }
    }
}
