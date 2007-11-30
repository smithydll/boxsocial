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

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

            core.RegisterCommentHandle("USER", userCanPostComment, userCanDeleteComment, userAdjustCommentCount);
            core.RegisterCommentHandle("APPLICATION", applicationCanPostComment, applicationCanDeleteComment, applicationAdjustCommentCount);
            core.RegisterCommentHandle("GROUP", groupCanPostComment, groupCanDeleteComment, groupAdjustCommentCount);
            core.RegisterCommentHandle("NETWORK", networkCanPostComment, networkCanDeleteComment, networkAdjustCommentCount);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application);
            aii.AddSlug("profile", @"^/profile/comments(|/)$", AppPrimitives.Member);
            aii.AddSlug("comments", @"^/comments(|/)$", AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application);

            aii.AddCommentType("USER");
            aii.AddCommentType("APPLICATION");
            aii.AddCommentType("GROUP");
            aii.AddCommentType("NETWORK");

            return aii;
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/profile/comments(|/)$", showProfileGuestBook, 1);
            core.RegisterApplicationPage(@"^/comments(|/)$", showGuestBook, 2);
        }

        private bool userCanPostComment(long itemId, Member member)
        {
            try
            {
                Member owner = new Member(core.db, itemId);
                owner.LoadProfileInfo(); // TODO: reduce query count by one

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

        private void userAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE user_profile SET profile_comments = profile_comments + {1} WHERE user_id = {0};",
                itemId, adjustment), false);

            if (adjustment == 1)
            {
                // Notify of a new comment

                Member userProfile = new Member(core.db, (long)itemId);
                if (userProfile.EmailNotifications)
                {
                    Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "guestbook_notification.eml");

                    emailTemplate.ParseVariables("TO_NAME", userProfile.DisplayName);
                    emailTemplate.ParseVariables("FROM_NAME", core.session.LoggedInMember.DisplayName);
                    emailTemplate.ParseVariables("FROM_USERNAME", core.session.LoggedInMember.UserName);
                    emailTemplate.ParseVariables("U_GUESTBOOK", "http://zinzam.com" + ZzUri.BuildGuestBookUri(userProfile));

                    Email.SendEmail(core, userProfile.AlternateEmail, string.Format("{0} commented on your guest book",
                        core.session.LoggedInMember.DisplayName),
                        emailTemplate.ToString());
                }
            }
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

        void core_PageHooks(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;

                switch (page.Signature)
                {
                    case PageSignature.viewprofile:
                        ShowMemberGuestBook(page.db, page);
                        break;
                }
            }
            if (sender is GPage)
            {
                GPage page = (GPage)sender;

                switch (page.Signature)
                {
                    case PageSignature.viewgroup:
                        ShowGroupGuestBook(page.db, page);
                        break;
                }
            }
            if (sender is NPage)
            {
                NPage page = (NPage)sender;

                switch (page.Signature)
                {
                    case PageSignature.viewnetwork:
                        ShowNetworkGuestBook(page.db, page);
                        break;
                }
            }
            if (sender is APage)
            {
                APage page = (APage)sender;

                switch (page.Signature)
                {
                    case PageSignature.viewapplication:
                        ShowApplicationGuestBook(page.db, page);
                        break;
                }
            }
        }

        public void ShowMemberGuestBook(Mysql db, PPage page)
        {
            Display.DisplayComments(page, page.ProfileOwner, page.ProfileOwner.UserId, "USER", (long)page.ProfileOwner.ProfileComments, false);
            page.template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(page.ProfileOwner)));
        }

        public void ShowGroupGuestBook(Mysql db, GPage page)
        {
            Display.DisplayComments(page, page.ThisGroup, page.ThisGroup.GroupId, "GROUP", (long)page.ThisGroup.Comments, false);
            page.template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(page.ThisGroup)));
        }

        public void ShowNetworkGuestBook(Mysql db, NPage page)
        {
            Display.DisplayComments(page, page.TheNetwork, page.TheNetwork.NetworkId, "NETWORK", (long)page.TheNetwork.Comments, false);
            page.template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(page.TheNetwork)));
        }

        public void ShowApplicationGuestBook(Mysql db, APage page)
        {
            Display.DisplayComments(page, page.AnApplication, page.AnApplication.ApplicationId, "APPLICATION", (long)page.AnApplication.Comments, false);
            page.template.ParseVariables("U_VIEW_ALL", HttpUtility.HtmlEncode(GuestBook.Uri(page.AnApplication)));
        }
    }
}
