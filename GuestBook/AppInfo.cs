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
			
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(User)), userCanPostComment, userCanDeleteComment, userAdjustCommentCount, userCommentPosted, userCommentDeleted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(ApplicationEntry)), applicationCanPostComment, applicationCanDeleteComment, applicationAdjustCommentCount, applicationCommentPosted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(UserGroup)), groupCanPostComment, groupCanDeleteComment, groupAdjustCommentCount, groupCommentPosted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(Network)), networkCanPostComment, networkCanDeleteComment, networkAdjustCommentCount, networkCommentPosted);
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(Musician.Musician)), musicianCanPostComment, musicianCanDeleteComment, musicianAdjustCommentCount, musicianCommentPosted);
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

        private bool userCanPostComment(ItemKey itemKey, User member)
        {
            try
            {
                User owner = new User(core, itemKey.Id, UserLoadOptions.All);

                if (owner.Access.Can("COMMENT"))
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

        private bool userCanDeleteComment(ItemKey itemKey, User member)
        {
            if (itemKey.Id == member.UserId)
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

            if (core.Db.Query(query).Rows.Count > 0)
            {
                UpdateQuery uquery = new UpdateQuery("guestbook_comment_counts");
                uquery.AddField("comment_comments", new QueryOperation("comment_comments", QueryOperations.Addition, 1));
                uquery.AddCondition("owner_id", userProfile.Id);
                uquery.AddCondition("user_id", e.Poster.Id);

                core.Db.Query(uquery);
            }
            else
            {
                InsertQuery iquery = new InsertQuery("guestbook_comment_counts");
                iquery.AddField("comment_comments", 1);
                iquery.AddField("owner_id", userProfile.Id);
                iquery.AddField("user_id", e.Poster.Id);

                core.Db.Query(iquery);
            }

            ApplicationEntry ae = new ApplicationEntry(core, core.Session.LoggedInMember, "GuestBook");

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_guestbook_notification");
            notificationTemplate.Parse("U_PROFILE", e.Comment.BuildUri(new UserGuestBook(core, userProfile)));
            notificationTemplate.Parse("POSTER", e.Poster.DisplayName);
            notificationTemplate.Parse("COMMENT", e.Comment.Body);

            ae.SendNotification(userProfile, string.Format("[user]{0}[/user] commented on your guest book.", e.Poster.Id), notificationTemplate.ToString());
        }

        private void userCommentDeleted(CommentPostedEventArgs e)
        {
            User userProfile = new User(core, e.ItemId);

            UpdateQuery uquery = new UpdateQuery("guestbook_comment_counts");
            uquery.AddField("comment_comments", new QueryOperation("comment_comments", QueryOperations.Subtraction, 1));
            uquery.AddCondition("owner_id", userProfile.Id);
            uquery.AddCondition("user_id", e.Poster.Id);

            core.Db.Query(uquery);
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

        private void musicianCommentPosted(CommentPostedEventArgs e)
        {
        }

        private void userAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.Db.UpdateQuery(string.Format("UPDATE user_profile SET profile_comments = profile_comments + {1} WHERE user_id = {0};",
                itemKey.Id, adjustment));
        }

        private bool groupCanPostComment(ItemKey itemKey, User member)
        {
            try
            {
                UserGroup owner = new UserGroup(core, itemKey.Id);

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

        private bool groupCanDeleteComment(ItemKey itemKey, User member)
        {
            try
            {
                UserGroup owner = new UserGroup(core, itemKey.Id);

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

        private void groupAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.Db.UpdateQuery(string.Format("UPDATE group_info SET group_comments = group_comments + {1} WHERE group_id = {0};",
                itemKey.Id, adjustment));
        }

        private bool networkCanPostComment(ItemKey itemKey, User member)
        {
            try
            {
                Network owner = new Network(core, itemKey.Id);

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

        private bool networkCanDeleteComment(ItemKey itemKey, User member)
        {
            return false;
        }

        private void networkAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.Db.UpdateQuery(string.Format("UPDATE network_info SET network_comments = network_comments + {1} WHERE network_id = {0};",
                itemKey.Id, adjustment));
        }

        private bool applicationCanPostComment(ItemKey itemKey, User member)
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

        private bool applicationCanDeleteComment(ItemKey itemKey, User member)
        {
            // TODO: scrape for owner
            return false;
        }

        private void applicationAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.Db.UpdateQuery(string.Format("UPDATE applications SET application_comments = application_comments + {1} WHERE application_id = {0};",
                itemKey.Id, adjustment));
        }

        private bool musicianCanPostComment(ItemKey itemKey, User member)
        {
            try
            {
                Musician.Musician owner = new Musician.Musician(core, itemKey.Id);

                if (owner.Access.Can("COMMENT"))
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

        private bool musicianCanDeleteComment(ItemKey itemKey, User member)
        {
            try
            {
                Musician.Musician owner = new Musician.Musician(core, itemKey.Id);

                if (owner.IsMusicianMember(member))
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

        private void musicianAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.Db.UpdateQuery(string.Format("UPDATE musicians SET musician_comments = musician_comments + {1} WHERE musician_id = {0};",
                itemKey.Id, adjustment));
        }

        [PageSlug("Guest Book")]
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

        [PageSlug("Guest Book")]
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

            template.Parse("U_SIGNIN", core.Uri.BuildLoginUri());

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
                if (thisGroup.IsGroupMember(e.core.Session.LoggedInMember))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Uri.BuildLoginUri());

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
                if (theNetwork.IsNetworkMember(e.core.Session.LoggedInMember))
                {
                    template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            template.Parse("U_SIGNIN", core.Uri.BuildLoginUri());

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

            template.Parse("U_SIGNIN", core.Uri.BuildLoginUri());

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

            template.Parse("U_SIGNIN", core.Uri.BuildLoginUri());

            core.Display.DisplayComments(template, musician, musician);
            template.Parse("U_VIEW_ALL", GuestBook.Uri(core, musician));

            e.core.AddMainPanel(template);
        }
    }
}
