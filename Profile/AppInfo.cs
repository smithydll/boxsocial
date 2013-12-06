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
                return Properties.Resources.icon;
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
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

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
            this.core = core;
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Application;
        }

        [PageSlug("Profile")]
        [Show("profile", AppPrimitives.Member | AppPrimitives.Application)]
        private void showProfile(Core core, object sender)
        {
            if (sender is UPage)
            {
                User.ShowProfile(core, (UPage)sender);
            }
            else if (sender is APage)
            {
                ApplicationEntry.ShowPage(core, (APage)sender);
            }
        }

        [Show("contacts", AppPrimitives.Member)]
        private void showContacts(Core core, object sender)
        {
            if (sender is UPage)
            {
                Contact.ShowAll(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [Show("contacts/([0-9]+)", AppPrimitives.Member)]
        private void showContact(Core core, object sender)
        {
            if (sender is UPage)
            {
                Contact.ShowAll(sender, new ShowUPageEventArgs((UPage)sender, long.Parse(core.PagePathParts[1].Value)));
            }
        }

        [PageSlug("Friends")]
        [Show("contacts/friends", AppPrimitives.Member)]
        private void showFriends(Core core, object sender)
        {
            if (sender is UPage)
            {
                User.ShowFriends(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [Show("contacts/family", AppPrimitives.Member)]
        private void showFamily(Core core, object sender)
        {
            if (sender is UPage)
            {
                User.ShowFamily(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [PageSlug("Status Feed")]
        [Show("status-feed", AppPrimitives.Member | AppPrimitives.Application)]
        private void showStatusFeed(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                StatusFeed.Show(core, new ShowUPageEventArgs(page));
            }
        }

        [PageSlug("Feed")]
        [Show("feed", AppPrimitives.Member | AppPrimitives.Application)]
        private void showFeed(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                CombinedFeed.Show(core, page, page.User);
            }
        }

        [Show("status-feed/([0-9]+)", AppPrimitives.Member | AppPrimitives.Application)]
        private void showStatusMessage(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                StatusMessage.Show(core, new ShowUPageEventArgs((UPage)sender, long.Parse(core.PagePathParts[1].Value)));
            }
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.None)
            {
                if (e.core.PagePath.ToLower() == "/default.aspx")
                {
                    //ShowStatusUpdates(e);
                    ShowFriends(e);
                    PostContent(e);
                }
            }
        }

        void PostContent(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "poststatusmessage");

            e.core.AddPostPanel("Status", template);
        }

        void ShowFriends(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todayfriendpanel");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            List<Friend> friends = e.core.Session.LoggedInMember.GetFriends(1, 9, null);

            foreach (UserRelation friend in friends)
            {
                VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendVariableCollection.Parse("ICON", friend.UserIcon);
                friendVariableCollection.Parse("TILE", friend.UserTile);
                friendVariableCollection.Parse("SQUARE", friend.UserSquare);
            }

            e.core.AddSidePanel(template);
        }

        /*void ShowStatusUpdates(HookEventArgs e)
        {
        }*/
    }
}
