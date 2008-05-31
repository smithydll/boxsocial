/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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

/*
 * 
 * TODO: SQL
CREATE TABLE `zinzam0_zinzam`.`guestbook_comment_counts` (
  `owner_id` BIGINT NOT NULL,
  `user_id` BIGINT NOT NULL,
  `comment_comments` BIGINT NOT NULL
)
ENGINE = InnoDB;

ALTER TABLE `zinzam0_zinzam`.`guestbook_comment_counts` ADD UNIQUE INDEX `i_guestbook_count`(`owner_id`, `user_id`);
 
INSERT INTO guestbook_comment_counts (SELECT comment_item_id AS owner_id, user_id, COUNT(*) AS comment_comments FROM comments WHERE comment_item_type = 'USER' GROUP BY comment_item_id, user_id);

 */

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
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
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

            core.RegisterCommentHandle("USER", userCanPostComment, userCanDeleteComment, userAdjustCommentCount, userCommentPosted, userCommentDeleted);
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

        private bool userCanPostComment(long itemId, User member)
        {
            try
            {
                User owner = new User(core, itemId, UserLoadOptions.All);

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

        private bool userCanDeleteComment(long itemId, User member)
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
            User userProfile = new User(core, e.ItemId);

            SelectQuery query = new SelectQuery("guestbook_comment_counts gcc");
            query.AddFields("comment_comments");
            query.AddCondition("owner_id", userProfile.Id);
            query.AddCondition("user_id", e.Poster.Id);

            if (core.db.Query(query).Rows.Count > 0)
            {
                UpdateQuery uquery = new UpdateQuery("guestbook_comment_counts");
                uquery.AddField("comment_comments", new QueryOperation("comment_comments", QueryOperations.Addition, 1));
                uquery.AddCondition("owner_id", userProfile.Id);
                uquery.AddCondition("user_id", e.Poster.Id);

                core.db.Query(uquery);
            }
            else
            {
                InsertQuery iquery = new InsertQuery("guestbook_comment_counts");
                iquery.AddField("comment_comments", 1);
                iquery.AddField("owner_id", userProfile.Id);
                iquery.AddField("user_id", e.Poster.Id);

                core.db.Query(iquery);
            }

            ApplicationEntry ae = new ApplicationEntry(core, core.session.LoggedInMember, "GuestBook");

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_guestbook_notification");
            notificationTemplate.ParseVariables("U_PROFILE", e.Comment.BuildUri(new UserGuestBook(core, userProfile)));
            notificationTemplate.ParseVariables("POSTER", e.Poster.DisplayName);
            notificationTemplate.ParseVariables("COMMENT", e.Comment.Body);

            ae.SendNotification(userProfile, string.Format("[user]{0}[/user] commented on your guest book.", e.Poster.Id), notificationTemplate.ToString());
        }

        private void userCommentDeleted(CommentPostedEventArgs e)
        {
            User userProfile = new User(core, e.ItemId);

            UpdateQuery uquery = new UpdateQuery("guestbook_comment_counts");
            uquery.AddField("comment_comments", new QueryOperation("comment_comments", QueryOperations.Subtraction, 1));
            uquery.AddCondition("owner_id", userProfile.Id);
            uquery.AddCondition("user_id", e.Poster.Id);

            core.db.Query(uquery);
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
                itemId, adjustment));
        }

        private bool groupCanPostComment(long itemId, User member)
        {
            try
            {
                UserGroup owner = new UserGroup(core, itemId);

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

        private bool groupCanDeleteComment(long itemId, User member)
        {
            try
            {
                UserGroup owner = new UserGroup(core, itemId);

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
                itemId, adjustment));
        }

        private bool networkCanPostComment(long itemId, User member)
        {
            try
            {
                Network owner = new Network(core, itemId);

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

        private bool networkCanDeleteComment(long itemId, User member)
        {
            return false;
        }

        private void networkAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE network_info SET network_comments = network_comments + {1} WHERE network_id = {0};",
                itemId, adjustment));
        }

        private bool applicationCanPostComment(long itemId, User member)
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

        private bool applicationCanDeleteComment(long itemId, User member)
        {
            // TODO: scrape for owner
            return false;
        }

        private void applicationAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE applications SET application_comments = application_comments + {1} WHERE application_id = {0};",
                itemId, adjustment));
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
                string[] paths = core.PagePathParts[1].Value.Split(new char[] {'/'});
                GuestBook.Show(core, (PPage)sender, paths[paths.Length - 1]);
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
            User profileOwner = (User)e.Owner;
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

            template.ParseVariables("IS_USER_GUESTBOOK", "TRUE");

            Display.DisplayComments(template, profileOwner, profileOwner, GuestBook.UserGuestBookHook);
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
