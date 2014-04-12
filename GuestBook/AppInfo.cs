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
using BoxSocial.Musician;

namespace BoxSocial.Applications.GuestBook
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
                return "Guest Book";
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
                return true;
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
                return null;
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
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application | AppPrimitives.Musician);

            aii.AddCommentType("USER");
            aii.AddCommentType("APPLICATION");
            aii.AddCommentType("GROUP");
            aii.AddCommentType("NETWORK");
            aii.AddCommentType("MUSIC");

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
            this.core = core;
        }

        [PageSlug("Guest Book", AppPrimitives.Member)]
        [Show(@"profile/comments", AppPrimitives.Member)]
        private void showProfileGuestBook(Core core, object sender)
        {
            if (sender is UPage)
            {
                GuestBook.Show(core, (UPage)sender);
            }
        }

        [Show(@"profile/comments/([A-Za-z0-9\-_]+)", AppPrimitives.Member)]
        private void showProfileGuestBookConversation(Core core, object sender)
        {
            if (sender is UPage)
            {
                string[] paths = core.PagePathParts[1].Value.Split(new char[] {'/'});
                GuestBook.Show(core, (UPage)sender, paths[paths.Length - 1]);
            }
        }

        [PageSlug("Guest Book", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application | AppPrimitives.Musician)]
        [Show(@"comments", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application | AppPrimitives.Musician)]
        private void showGuestBook(Core core, object sender)
        {
            if (sender is GPage)
            {
                GuestBook.Show(core, (GPage)sender);
            }
            else if (sender is NPage)
            {
                GuestBook.Show(core, (NPage)sender);
            }
            else if (sender is APage)
            {
                GuestBook.Show(core, (APage)sender);
            }
            else if (sender is MPage)
            {
                GuestBook.Show(core, (MPage)sender);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application | AppPrimitives.Musician;
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                ShowMemberGuestBook(e);
            }
            if (e.PageType == AppPrimitives.Group)
            {
                ShowGroupGuestBook(e);
            }
            if (e.PageType == AppPrimitives.Network)
            {
                ShowNetworkGuestBook(e);
            }
            if (e.PageType == AppPrimitives.Application)
            {
                ShowApplicationGuestBook(e);
            }
            if (e.PageType == AppPrimitives.Musician)
            {
                ShowMusicianGuestBook(e);
            }
        }

        public void ShowMemberGuestBook(HookEventArgs e)
        {
            User profileOwner = (User)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            if (e.core.Session.IsLoggedIn)
            {
                template.Parse("LOGGED_IN", "TRUE");
                if (profileOwner.Access.Can("COMMENT"))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Hyperlink.BuildLoginUri());

            template.Parse("IS_USER_GUESTBOOK", "TRUE");

            core.Display.DisplayComments(template, profileOwner, profileOwner, GuestBook.UserGuestBookHook);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, profileOwner));
            template.Parse("IS_PROFILE", "TRUE");

            e.core.AddMainPanel(template);
        }

        public void ShowGroupGuestBook(HookEventArgs e)
        {
            UserGroup thisGroup = (UserGroup)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            if (e.core.Session.IsLoggedIn)
            {
                if (thisGroup.IsGroupMember(e.core.Session.LoggedInMember.ItemKey))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Hyperlink.BuildLoginUri());

            core.Display.DisplayComments(template, thisGroup, thisGroup);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, thisGroup));

            e.core.AddMainPanel(template);
        }

        public void ShowNetworkGuestBook(HookEventArgs e)
        {
            Network theNetwork = (Network)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            if (e.core.Session.IsLoggedIn)
            {
                if (theNetwork.IsNetworkMember(e.core.Session.LoggedInMember.ItemKey))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Hyperlink.BuildLoginUri());

            core.Display.DisplayComments(template, theNetwork, theNetwork);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, theNetwork));

            e.core.AddMainPanel(template);
        }

        public void ShowApplicationGuestBook(HookEventArgs e)
        {
            ApplicationEntry anApplication = (ApplicationEntry)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            if (e.core.Session.IsLoggedIn)
            {
                template.Parse("CAN_COMMENT", "TRUE");
            }

            template.Parse("U_SIGNIN", core.Hyperlink.BuildLoginUri());

            core.Display.DisplayComments(template, anApplication, anApplication);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, anApplication));

            e.core.AddMainPanel(template);
        }

        public void ShowMusicianGuestBook(HookEventArgs e)
        {
            Musician.Musician musician = (Musician.Musician)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            if (e.core.Session.IsLoggedIn)
            {
                template.Parse("LOGGED_IN", "TRUE");
                if (musician.Access.Can("COMMENT"))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Hyperlink.BuildLoginUri());

            core.Display.DisplayComments(template, musician, musician);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, musician));

            e.core.AddMainPanel(template);
        }
    }
}
