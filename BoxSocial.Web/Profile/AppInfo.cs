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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.Internals;
using BoxSocial.Forms;
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
            core.PostHooks += new Core.HookHandler(core_PostHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

        }

        public override bool ExecuteJob(Job job)
        {
            if (job.ItemId == 0)
            {
                return true;
            }

            switch (job.Function)
            {
                case "notifyFriendRequest":
                    AccountFriendManage.NotifyFriendRequest(core, job);
                    return true;
                case "notifyStatusComment":
                    StatusMessage.NotifyStatusMessageComment(core, job);
                    return true;
            }

            return false;
        }

        public override void ExecuteCall(string callName)
        {
            switch (callName)
            {
                case "settings/me":
                        core.Response.WriteObject(core.Session.LoggedInMember.UserSettings);
                    break;
                case "info/me":
                        core.Response.WriteObject(core.Session.LoggedInMember.UserInfo);
                    break;
                case "profile":
                    {
                        long userId = core.Functions.RequestLong("id", core.Session.LoggedInMember.Id);

                        User user = new User(core, userId);
                        UserProfile up = user.Profile;

                        if (user.Access.Can("VIEW"))
                        {
                            core.Response.WriteObject(user);
                        }
                        else
                        {
                        }
                    }
                    break;
                case "friends":
                    {
                        long userId = core.Functions.RequestLong("id", core.Session.LoggedInMember.Id);
                        int page = core.Functions.RequestInt("page", 1);
                        int perPage = Math.Max(Math.Min(20, core.Functions.RequestInt("per_page", 18)), 1);
                        string filter = core.Http["filter"];

                        User user = new User(core, userId);

                        if (user.Access.Can("VIEW_FRIENDS"))
                        {
                            List<Friend> friends = user.GetFriends(page, perPage, filter);

                            core.Response.WriteObject(friends);
                        }
                    }
                    break;
                case "status_post":
                    string message = core.Http.Form["message"];
                    StatusMessage newMessage = StatusFeed.SaveMessage(core, message);

                    core.Response.WriteObject(newMessage);
                    break;
                case "status":
                    {
                        long statusId = core.Functions.RequestLong("id", 0);

                        if (statusId > 0)
                        {
                            try
                            {
                                StatusMessage status = new StatusMessage(core, statusId);

                                core.Response.WriteObject(status);
                            }
                            catch (InvalidStatusMessageException)
                            {
                            }
                        }
                    }
                    break;
                case "feed":
                    {
                        long ownerId = core.Functions.RequestLong("owner_id", core.Session.LoggedInMember.Id);
                        long ownerTypeId = core.Functions.RequestLong("owner_type_id", core.Session.LoggedInMember.ItemKey.TypeId);
                        User user = new User(core, ownerId);

                        CombinedFeed.ShowMore(core, user);
                    }
                    break;
            }
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

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

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Application | AppPrimitives.Group;
        }

        [PageSlug("Profile", AppPrimitives.Member | AppPrimitives.Application)]
        [Show("profile", AppPrimitives.Member | AppPrimitives.Application)]
        private void showProfile(Core core, object sender)
        {
            if (sender is UPage)
            {
                Default.ShowProfile(sender, new ShowUPageEventArgs((UPage)sender));
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

        [PageSlug("Friends", AppPrimitives.Member)]
        [Show("contacts/friends", AppPrimitives.Member)]
        private void showFriends(Core core, object sender)
        {
            if (sender is UPage)
            {
                Default.ShowFriends(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [PageSlug("Subscribers", AppPrimitives.Member)]
        [Show("subscribers", AppPrimitives.Member)]
        private void showSubscribers(Core core, object sender)
        {
            if (sender is UPage)
            {
                Default.ShowSubscribers(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [PageSlug("Subscriptions", AppPrimitives.Member)]
        [Show("subscriptions", AppPrimitives.Member)]
        private void showSubscriptions(Core core, object sender)
        {
            if (sender is UPage)
            {
                Default.ShowSubscriptions(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [Show("contacts/family", AppPrimitives.Member)]
        private void showFamily(Core core, object sender)
        {
            if (sender is UPage)
            {
                Default.ShowFamily(sender, new ShowUPageEventArgs((UPage)sender));
            }
        }

        [PageSlug("Status Feed", AppPrimitives.Member | AppPrimitives.Application)]
        [Show("status-feed", AppPrimitives.Member | AppPrimitives.Application)]
        private void showStatusFeed(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                StatusFeed.Show(core, new ShowUPageEventArgs(page));
            }
        }

        [PageSlug("Feed", AppPrimitives.Member | AppPrimitives.Application)]
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
                }
            }
        }

        void core_PostHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                PostContent(e);
            }
        }

        void PostContent(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "poststatusmessage");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            string formSubmitUri = core.Hyperlink.AppendSid(e.Owner.AccountUriStub, true);
            template.Parse("U_ACCOUNT", formSubmitUri);
            template.Parse("S_ACCOUNT", formSubmitUri);

            template.Parse("USER_DISPLAY_NAME", e.Owner.DisplayName);

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", e.Owner.ItemKey);
            CheckBoxArray shareCheckBoxArray = new CheckBoxArray("share-radio");
            shareCheckBoxArray.Layout = Layout.Horizontal;
            CheckBox twitterSyndicateCheckBox = null;
            CheckBox tumblrSyndicateCheckBox = null;
            CheckBox facebookSyndicateCheckBox = null;

            if (e.Owner is User)
            {
                User user = (User)e.Owner;

                if (user.UserInfo.TwitterAuthenticated)
                {
                    twitterSyndicateCheckBox = new CheckBox("status-share-twitter");
                    twitterSyndicateCheckBox.Caption = "Twitter";
                    twitterSyndicateCheckBox.Icon = "https://g.twimg.com/twitter-bird-16x16.png";
                    twitterSyndicateCheckBox.IsChecked = user.UserInfo.TwitterSyndicate;
                    twitterSyndicateCheckBox.Width.Length = 0;

                    shareCheckBoxArray.Add(twitterSyndicateCheckBox);
                }

                if (user.UserInfo.TumblrAuthenticated)
                {
                    tumblrSyndicateCheckBox = new CheckBox("status-share-tumblr");
                    tumblrSyndicateCheckBox.Caption = "Tumblr";
                    tumblrSyndicateCheckBox.Icon = "https://platform.tumblr.com/v1/share_4.png";
                    tumblrSyndicateCheckBox.IsChecked = user.UserInfo.TumblrSyndicate;
                    tumblrSyndicateCheckBox.Width.Length = 0;

                    shareCheckBoxArray.Add(tumblrSyndicateCheckBox);
                }

                if (user.UserInfo.FacebookAuthenticated)
                {
                    facebookSyndicateCheckBox = new CheckBox("status-share-facebook");
                    facebookSyndicateCheckBox.Caption = "Facebook";
                    facebookSyndicateCheckBox.Icon = "https://fbstatic-a.akamaihd.net/rsrc.php/v2/yU/r/fWK1wxX-qQn.png";
                    facebookSyndicateCheckBox.IsChecked = user.UserInfo.FacebookSyndicate;
                    facebookSyndicateCheckBox.Width.Length = 0;

                    shareCheckBoxArray.Add(facebookSyndicateCheckBox);
                }

            }

            template.Parse("S_STATUS_PERMISSIONS", permissionSelectBox);

            if (shareCheckBoxArray.Count > 0)
            {
                template.Parse("S_SHARE", "TRUE");
            }
            if (twitterSyndicateCheckBox != null)
            {
                template.Parse("S_SHARE_TWITTER", twitterSyndicateCheckBox);
            }
            if (tumblrSyndicateCheckBox != null)
            {
                template.Parse("S_SHARE_TUMBLR", tumblrSyndicateCheckBox);
            }
            if (facebookSyndicateCheckBox != null)
            {
                template.Parse("S_SHARE_FACEBOOK", facebookSyndicateCheckBox);
            }

            e.core.AddPostPanel(e.core.Prose.GetString("STATUS"), template);
        }

        void ShowFriends(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todayfriendpanel");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            List<Friend> friends = e.core.Session.LoggedInMember.GetFriends(1, 10, true);

            foreach (UserRelation friend in friends)
            {
                VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendVariableCollection.Parse("ICON", friend.Icon);
                friendVariableCollection.Parse("TILE", friend.Tile);
                friendVariableCollection.Parse("SQUARE", friend.Square);
                friendVariableCollection.Parse("SUBSCRIBERS", friend.Info.Subscribers);
                friendVariableCollection.Parse("IS_ONLINE", friend.IsOnline ? "TRUE" : "FALSE");
            }

            e.core.AddSidePanel(template);
        }

        /*void ShowStatusUpdates(HookEventArgs e)
        {
        }*/
    }
}
