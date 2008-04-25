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
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.GuestBook
{
    public class AppInfo : Application
    {
        public override string Title
        {
            get
            {
                return "Guest Book";
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
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

            core.RegisterCommentHandle("USER", userCanPostComment, userCanDeleteComment, userAdjustCommentCount, userCommentPosted);
            core.RegisterCommentHandle("APPLICATION", applicationCanPostComment, applicationCanDeleteComment, applicationAdjustCommentCount, applicationCommentPosted);
            core.RegisterCommentHandle("GROUP", groupCanPostComment, groupCanDeleteComment, groupAdjustCommentCount, groupCommentPosted);
            core.RegisterCommentHandle("NETWORK", networkCanPostComment, networkCanDeleteComment, networkAdjustCommentCount, networkCommentPosted);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application);
            aii.AddSlug("profile", @"^/profile/comments(|/)$", AppPrimitives.Member);
            aii.AddSlug("profile", @"^/profile/comments/([A-Za-z0-9\-_]+)(|/)$", AppPrimitives.Member);
            aii.AddSlug("comments", @"^/comments(|/)$", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application);

            aii.AddCommentType("USER");
            aii.AddCommentType("APPLICATION");
            aii.AddCommentType("GROUP");
            aii.AddCommentType("NETWORK");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("profile/comments", "Guest Book");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/profile/comments(|/)$", showProfileGuestBook, 1);
            core.RegisterApplicationPage(@"^/comments(|/)$", showGuestBook, 2);
            core.RegisterApplicationPage(@"^/profile/comments/([A-Za-z0-9\-_]+)(|/)", showProfileGuestBookConversation, 3);
        }

        private bool userCanPostComment(long itemId, Member member)
        {
            try
            {
                Member owner = new Member(core.db, itemId, true);

                owner.ProfileAccess.SetViewer(member);

                if (owner.ProfileAccess.CanComment)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private bool userCanDeleteComment(long itemId, Member member)
        {
            if (itemId == member.UserId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void userCommentPosted(CommentPostedEventArgs e)
        {
            // Notify of a new comment
            Member userProfile = new Member(core.db, e.ItemId);

            ApplicationEntry ae = new ApplicationEntry(core.db, core.session.LoggedInMember, "GuestBook");

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_guestbook_notification");
            notificationTemplate.ParseVariables("U_PROFILE", e.Comment.BuildUri(new UserGuestBook(core, userProfile)));
            notificationTemplate.ParseVariables("POSTER", e.Poster.DisplayName);
            notificationTemplate.ParseVariables("COMMENT", e.Comment.Body);

            ae.SendNotification(userProfile, string.Format("[user]{0}[/user] commented on your guest book.", e.Poster.Id), notificationTemplate.ToString());
        }

        private void groupCommentPosted(CommentPostedEventArgs e)
        {
        }

        private void networkCommentPosted(CommentPostedEventArgs e)
        {
        }

        private void applicationCommentPosted(CommentPostedEventArgs e)
        {
        }

        private void userAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE user_profile SET profile_comments = profile_comments + {1} WHERE user_id = {0};",
                itemId, adjustment), false);
        }

        private bool groupCanPostComment(long itemId, Member member)
        {
            try
            {
                UserGroup owner = new UserGroup(core.db, itemId);

                if (owner.IsGroupMember(member))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private bool groupCanDeleteComment(long itemId, Member member)
        {
            try
            {
                UserGroup owner = new UserGroup(core.db, itemId);

                if (owner.IsGroupOperator(member))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private void groupAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE group_info SET group_comments = group_comments + {1} WHERE group_id = {0};",
                itemId, adjustment), false);
        }

        private bool networkCanPostComment(long itemId, Member member)
        {
            try
            {
                Network owner = new Network(core.db, itemId);

                if (owner.IsNetworkMember(member))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private bool networkCanDeleteComment(long itemId, Member member)
        {
            return false;
        }

        private void networkAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE network_info SET network_comments = network_comments + {1} WHERE network_id = {0};",
                itemId, adjustment), false);
        }

        private bool applicationCanPostComment(long itemId, Member member)
        {
            if (member != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool applicationCanDeleteComment(long itemId, Member member)
        {
            // TODO: scrape for owner
            return false;
        }

        private void applicationAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE applications SET application_comments = application_comments + {1} WHERE application_id = {0};",
                itemId, adjustment), false);
        }

        private void showProfileGuestBook(Core core, object sender)
        {
            if (sender is PPage)
            {
                GuestBook.Show(core, (PPage)sender);
            }
        }

        private void showProfileGuestBookConversation(Core core, object sender)
        {
            if (sender is PPage)
            {
                GuestBook.Show(core, (PPage)sender, core.PagePathParts[0].Value);
            }
        }

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
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application;
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
        }

        public void ShowMemberGuestBook(HookEventArgs e)
        {
            Member profileOwner = (Member)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");

            profileOwner.ProfileAccess.SetViewer(e.core.session.LoggedInMember);

            if (e.core.session.IsLoggedIn)
            {
                template.ParseVariables("LOGGED_IN", "TRUE");
                if (profileOwner.ProfileAccess.CanComment)
                {
                    template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(template, profileOwner, profileOwner);
            template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(profileOwner)));
            template.ParseVariables("IS_PROFILE", "TRUE");

            e.core.AddMainPanel(template);
        }

        public void ShowGroupGuestBook(HookEventArgs e)
        {
            UserGroup thisGroup = (UserGroup)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");

            if (e.core.session.IsLoggedIn)
            {
                if (thisGroup.IsGroupMember(e.core.session.LoggedInMember))
                {
                    template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(template, thisGroup, thisGroup);
            template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(thisGroup)));

            e.core.AddMainPanel(template);
        }

        public void ShowNetworkGuestBook(HookEventArgs e)
        {
            Network theNetwork = (Network)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");

            if (e.core.session.IsLoggedIn)
            {
                if (theNetwork.IsNetworkMember(e.core.session.LoggedInMember))
                {
                    template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(template, theNetwork, theNetwork);
            template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(theNetwork)));

            e.core.AddMainPanel(template);
        }

        public void ShowApplicationGuestBook(HookEventArgs e)
        {
            ApplicationEntry anApplication = (ApplicationEntry)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileguestbook");

            if (e.core.session.IsLoggedIn)
            {
                template.ParseVariables("CAN_COMMENT", "TRUE");
            }

            Display.DisplayComments(template, anApplication, anApplication);
            template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(anApplication)));

            e.core.AddMainPanel(template);
        }
    }
}
