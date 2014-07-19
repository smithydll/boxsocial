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
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    public class Default
    {
        //
        // These methods were in the User class, but it's part of the Profile
        // application, this abstracts these view methods from the core.
        //

        public static void ShowProfile(object sender, ShowUPageEventArgs e)
        {
            e.Core.Template.SetTemplate("viewprofile.html");
            e.Page.Signature = PageSignature.viewprofile;

            bool hasProfileInfo = false;

            e.Page.User.LoadProfileInfo();

            if (!e.Page.User.Access.Can("VIEW"))
            {
                e.Core.Functions.Generate403();
                return;
            }

            e.Page.CanonicalUri = e.Page.User.ProfileUri;

            if (e.Core.LoggedInMemberId == e.Page.User.Id)
            {
                e.Core.Template.Parse("OWNER", "TRUE");
            }

            string age;
            int ageInt = e.Page.User.Profile.Age;
            if (ageInt == 0)
            {
                age = "FALSE";
            }
            else
            {
                age = ageInt.ToString() + " years old";
            }

            if (e.Page.User.Access.Can("VIEW_SEXUALITY"))
            {
                e.Core.Template.Parse("USER_SEXUALITY", e.Page.User.Profile.Sexuality);
                e.Core.Template.Parse("USER_INTERESTED_IN_MEN", e.Page.User.Profile.InterestedInMen ? "TRUE" : "FALSE");
                e.Core.Template.Parse("USER_INTERESTED_IN_WOMEN", e.Page.User.Profile.InterestedInWomen ? "TRUE" : "FALSE");
            }
            e.Core.Template.Parse("USER_GENDER", e.Page.User.Profile.Gender);
            e.Core.Display.ParseBbcode("USER_AUTOBIOGRAPHY", e.Page.User.Profile.Autobiography);
            e.Core.Display.ParseBbcode("USER_MARITIAL_STATUS", e.Page.User.Profile.MaritialStatus);
            e.Core.Template.Parse("USER_AGE", age);
            e.Core.Template.Parse("USER_JOINED", e.Core.Tz.DateTimeToString(e.Page.User.UserInfo.RegistrationDate));
            e.Core.Template.Parse("USER_LAST_SEEN", e.Core.Tz.DateTimeToString(e.Page.User.UserInfo.LastOnlineTime, true));
            e.Core.Template.Parse("USER_PROFILE_VIEWS", e.Core.Functions.LargeIntegerToString(e.Page.User.Profile.ProfileViews));
            e.Core.Template.Parse("USER_SUBSCRIPTIONS", e.Core.Functions.LargeIntegerToString(e.Page.User.UserInfo.BlogSubscriptions));
            e.Core.Template.Parse("USER_COUNTRY", e.Page.User.Profile.Country);
            e.Core.Template.Parse("USER_RELIGION", e.Page.User.Profile.Religion);
            e.Core.Template.Parse("USER_TINY", e.Page.User.UserTiny);
            e.Core.Template.Parse("USER_THUMB", e.Page.User.Thumbnail);
            e.Core.Template.Parse("USER_MOBILE", e.Page.User.UserMobile);
            e.Core.Template.Parse("USER_COVER_PHOTO", e.Page.User.CoverPhoto);
            e.Core.Template.Parse("USER_MOBILE_COVER_PHOTO", e.Page.User.MobileCoverPhoto);

            e.Core.Template.Parse("U_PROFILE", e.Page.User.Uri);
            e.Core.Template.Parse("U_FRIENDS", e.Core.Hyperlink.BuildFriendsUri(e.Page.User));

            e.Core.Template.Parse("IS_PROFILE", "TRUE");

            if (e.Page.User.IsOnline)
            {
                e.Core.Template.Parse("IS_ONLINE", "TRUE");
            }
            else
            {
                e.Core.Template.Parse("IS_ONLINE", "FALSE");
            }

            hasProfileInfo = true;

            if (hasProfileInfo)
            {
                e.Core.Template.Parse("HAS_PROFILE_INFO", "TRUE");
            }

            if (e.Core.LoggedInMemberId > 0)
            {
                e.Core.Template.Parse("U_ADD_FRIEND", e.Core.Hyperlink.BuildAddFriendUri(e.Page.User.UserId));
                e.Core.Template.Parse("U_BLOCK_USER", e.Core.Hyperlink.BuildBlockUserUri(e.Page.User.UserId));
            }

            if (e.Page.User.Access.Can("VIEW_FRIENDS"))
            {
                e.Core.Template.Parse("SHOW_FRIENDS", "TRUE");
                string langFriends = (e.Page.User.UserInfo.Friends != 1) ? "friends" : "friend";

                e.Core.Template.Parse("FRIENDS", e.Page.User.UserInfo.Friends.ToString());
                e.Core.Template.Parse("L_FRIENDS", langFriends);

                List<Friend> friends = e.Page.User.GetFriends(1, 8, null);
                foreach (UserRelation friend in friends)
                {
                    VariableCollection friendVariableCollection = e.Core.Template.CreateChild("friend_list");

                    friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                    friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                    friendVariableCollection.Parse("ICON", friend.Icon);
                    friendVariableCollection.Parse("TILE", friend.Tile);
                    friendVariableCollection.Parse("SQUARE", friend.Square);
                }
            }

            /* pages */
            e.Core.Display.ParsePageList(e.Page.User, true);

            /* status */
            StatusMessage statusMessage = StatusFeed.GetLatest(e.Core, e.Page.User);

            if (statusMessage != null)
            {
                e.Core.Template.Parse("STATUS_MESSAGE", statusMessage.Message);
                e.Core.Template.Parse("STATUS_UPDATED", e.Core.Tz.DateTimeToString(statusMessage.GetTime(e.Core.Tz)));
            }

            List<UserLink> links = e.Page.User.GetLinks();

            if (links.Count > 0)
            {
                int linkCount = 0;
                foreach (UserLink link in links)
                {
                    VariableCollection linkVariableCollection = e.Core.Template.CreateChild("link_list");

                    linkVariableCollection.Parse("U_LINK", link.Uri);
                    linkVariableCollection.Parse("TITLE", link.Title);

                    if (!string.IsNullOrEmpty(link.Favicon))
                    {
                        BoxSocial.Forms.Image faviconImage = new BoxSocial.Forms.Image("favicon-" + link.Id, e.Core.Hyperlink.AppendAbsoluteSid("/images/favicons/" + link.Favicon));
                        linkVariableCollection.Parse("S_FAVICON", faviconImage);
                    }
                    linkCount++;
                    if (linkCount == 6) break;
                }
            }

            List<UserEmail> emails = e.Page.User.GetEmailAddresses();

            if (emails.Count > 0)
            {
                List<IPermissibleItem> emailsCache = new List<IPermissibleItem>();

                foreach (UserEmail email in emails)
                {
                    emailsCache.Add((IPermissibleItem)email);
                }

                e.Core.AcessControlCache.CacheGrants(emailsCache);

                foreach (UserEmail email in emails)
                {
                    if (email.Access.Can("VIEW"))
                    {
                        VariableCollection emailVariableCollection = e.Core.Template.CreateChild("email_list");

                        emailVariableCollection.Parse("U_MAILTO", "mailto:" + email.Email);
                        emailVariableCollection.Parse("EMAIL", email.Email);
                    }
                }
            }

            List<UserPhoneNumber> phoneNumbers = e.Page.User.GetPhoneNumbers();

            if (phoneNumbers.Count > 0)
            {
                List<IPermissibleItem> phoneCache = new List<IPermissibleItem>();

                foreach (UserPhoneNumber phone in phoneNumbers)
                {
                    phoneCache.Add((IPermissibleItem)phone);
                }

                e.Core.AcessControlCache.CacheGrants(phoneCache);

                foreach (UserPhoneNumber phone in phoneNumbers)
                {
                    if (phone.Access.Can("VIEW"))
                    {
                        VariableCollection phoneVariableCollection = e.Core.Template.CreateChild("phone_list");

                        phoneVariableCollection.Parse("PHONE_NUMBER", phone.PhoneNumber);
                    }
                }
            }

            if (e.Page.User.Access.Can("SEND_MESSAGE"))
            {
                e.Core.Template.Parse("U_SEND_MESSAGE", "{todo: send message link}");
            }

            e.Core.InvokeHooks(new HookEventArgs(e.Core, AppPrimitives.Member, e.Page.User));

            e.Page.User.ProfileViewed(e.Core.Session.LoggedInMember);

            List<string[]> breadCrumbParts = new List<string[]>();
            if (!e.Core.IsMobile)
            {
                breadCrumbParts.Add(new string[] { "profile", e.Core.Prose.GetString("PROFILE") });
            }

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);

            if (Subscription.IsSubscribed(e.Core, e.Page.User.ItemKey))
            {
                e.Core.Template.Parse("SUBSCRIBED", "TRUE");
            }

            e.Core.Template.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildSubscribeUri(e.Page.User.ItemKey));
            e.Core.Template.Parse("U_UNSUBSCRIBE", e.Core.Hyperlink.BuildUnsubscribeUri(e.Page.User.ItemKey));

            e.Core.Template.Parse("SUBSCRIBERS", e.Core.Functions.LargeIntegerToString(e.Page.User.Info.Subscribers));
        }

        public static void ShowSubscriptions(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("viewsubscriptions.html");

            e.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("SUBSCRIPTIONS"));

            /* pages */
            e.Core.Display.ParsePageList(e.Page.User, true);

            List<User> subscribers = Subscription.GetSubscriptions(e.Core, (User)e.Page.Owner, e.Page.TopLevelPageNumber, 18);

            foreach (User subscriber in subscribers)
            {
                VariableCollection subscriberVariableCollection = e.Template.CreateChild("subscriber_list");

                subscriberVariableCollection.Parse("USER_DISPLAY_NAME", subscriber.DisplayName);
                subscriberVariableCollection.Parse("U_PROFILE", subscriber.Uri);
                subscriberVariableCollection.Parse("ICON", subscriber.Icon);
                subscriberVariableCollection.Parse("TILE", subscriber.Tile);
                subscriberVariableCollection.Parse("MOBILE_COVER", subscriber.MobileCoverPhoto);

                subscriberVariableCollection.Parse("ID", subscriber.Id);
                subscriberVariableCollection.Parse("TYPE", subscriber.TypeId);
                subscriberVariableCollection.Parse("LOCATION", subscriber.Profile.Country);
                e.Core.Display.ParseBbcode(subscriberVariableCollection, "ABSTRACT", subscriber.Profile.Autobiography);
                subscriberVariableCollection.Parse("SUBSCRIBERS", subscriber.Info.Subscribers);

                if (Subscription.IsSubscribed(e.Core, subscriber.ItemKey))
                {
                    subscriberVariableCollection.Parse("SUBSCRIBERD", "TRUE");
                    subscriberVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildUnsubscribeUri(subscriber.ItemKey));
                }
                else
                {
                    subscriberVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildSubscribeUri(subscriber.ItemKey));
                }

                if (e.Core.Session.SignedIn && subscriber.Id == e.Core.LoggedInMemberId)
                {
                    subscriberVariableCollection.Parse("ME", "TRUE");
                }
            }

            string pageUri = e.Core.Hyperlink.BuildSubscriptionsUri(e.Page.User);
            e.Core.Display.ParsePagination(pageUri, 18, e.Page.User.Subscribers);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "subscriptions", e.Core.Prose.GetString("SUBSCRIPTIONS") });

            e.Page.User.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void ShowSubscribers(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("viewsubscribers.html");

            e.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("SUBSCRIBERS"));

            /* pages */
            e.Core.Display.ParsePageList(e.Page.User, true);

            List<User> subscribers = Subscription.GetUserSubscribers(e.Core, e.Page.Owner.ItemKey, e.Page.TopLevelPageNumber, 18);

            foreach (User subscriber in subscribers)
            {
                VariableCollection subscriberVariableCollection = e.Template.CreateChild("subscriber_list");

                subscriberVariableCollection.Parse("USER_DISPLAY_NAME", subscriber.DisplayName);
                subscriberVariableCollection.Parse("U_PROFILE", subscriber.Uri);
                subscriberVariableCollection.Parse("ICON", subscriber.Icon);
                subscriberVariableCollection.Parse("TILE", subscriber.Tile);
                subscriberVariableCollection.Parse("MOBILE_COVER", subscriber.MobileCoverPhoto);

                subscriberVariableCollection.Parse("ID", subscriber.Id);
                subscriberVariableCollection.Parse("TYPE", subscriber.TypeId);
                subscriberVariableCollection.Parse("LOCATION", subscriber.Profile.Country);
                e.Core.Display.ParseBbcode(subscriberVariableCollection, "ABSTRACT", subscriber.Profile.Autobiography);
                subscriberVariableCollection.Parse("SUBSCRIBERS", subscriber.Info.Subscribers);

                if (Subscription.IsSubscribed(e.Core, subscriber.ItemKey))
                {
                    subscriberVariableCollection.Parse("SUBSCRIBERD", "TRUE");
                    subscriberVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildUnsubscribeUri(subscriber.ItemKey));
                }
                else
                {
                    subscriberVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildSubscribeUri(subscriber.ItemKey));
                }

                if (e.Core.Session.SignedIn && subscriber.Id == e.Core.LoggedInMemberId)
                {
                    subscriberVariableCollection.Parse("ME", "TRUE");
                }
            }

            string pageUri = e.Core.Hyperlink.BuildSubscribersUri(e.Page.User);
            e.Core.Display.ParsePagination(pageUri, 18, e.Page.User.Info.Subscribers);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "subscribers", e.Core.Prose.GetString("SUBSCRIBERS") });

            e.Page.User.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void ShowFriends(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("viewfriends.html");

            if (!e.Page.User.Access.Can("VIEW_FRIENDS"))
            {
                e.Core.Functions.Generate403();
            }

            string filter = e.Core.Http["filter"];

            /* pages */
            e.Core.Display.ParsePageList(e.Page.User, true);

            string langFriends = (e.Page.User.UserInfo.Friends != 1) ? "friends" : "friend";

            e.Template.Parse("U_FILTER_ALL", GenerateFriendsUri(e.Core, e.Page.User));
            e.Template.Parse("U_FILTER_BEGINS_A", GenerateFriendsUri(e.Core, e.Page.User, "a"));
            e.Template.Parse("U_FILTER_BEGINS_B", GenerateFriendsUri(e.Core, e.Page.User, "b"));
            e.Template.Parse("U_FILTER_BEGINS_C", GenerateFriendsUri(e.Core, e.Page.User, "c"));
            e.Template.Parse("U_FILTER_BEGINS_D", GenerateFriendsUri(e.Core, e.Page.User, "d"));
            e.Template.Parse("U_FILTER_BEGINS_E", GenerateFriendsUri(e.Core, e.Page.User, "e"));
            e.Template.Parse("U_FILTER_BEGINS_F", GenerateFriendsUri(e.Core, e.Page.User, "f"));
            e.Template.Parse("U_FILTER_BEGINS_G", GenerateFriendsUri(e.Core, e.Page.User, "g"));
            e.Template.Parse("U_FILTER_BEGINS_H", GenerateFriendsUri(e.Core, e.Page.User, "h"));
            e.Template.Parse("U_FILTER_BEGINS_I", GenerateFriendsUri(e.Core, e.Page.User, "i"));
            e.Template.Parse("U_FILTER_BEGINS_J", GenerateFriendsUri(e.Core, e.Page.User, "j"));
            e.Template.Parse("U_FILTER_BEGINS_K", GenerateFriendsUri(e.Core, e.Page.User, "k"));
            e.Template.Parse("U_FILTER_BEGINS_L", GenerateFriendsUri(e.Core, e.Page.User, "l"));
            e.Template.Parse("U_FILTER_BEGINS_M", GenerateFriendsUri(e.Core, e.Page.User, "m"));
            e.Template.Parse("U_FILTER_BEGINS_N", GenerateFriendsUri(e.Core, e.Page.User, "n"));
            e.Template.Parse("U_FILTER_BEGINS_O", GenerateFriendsUri(e.Core, e.Page.User, "o"));
            e.Template.Parse("U_FILTER_BEGINS_P", GenerateFriendsUri(e.Core, e.Page.User, "p"));
            e.Template.Parse("U_FILTER_BEGINS_Q", GenerateFriendsUri(e.Core, e.Page.User, "q"));
            e.Template.Parse("U_FILTER_BEGINS_R", GenerateFriendsUri(e.Core, e.Page.User, "r"));
            e.Template.Parse("U_FILTER_BEGINS_S", GenerateFriendsUri(e.Core, e.Page.User, "s"));
            e.Template.Parse("U_FILTER_BEGINS_T", GenerateFriendsUri(e.Core, e.Page.User, "t"));
            e.Template.Parse("U_FILTER_BEGINS_U", GenerateFriendsUri(e.Core, e.Page.User, "u"));
            e.Template.Parse("U_FILTER_BEGINS_V", GenerateFriendsUri(e.Core, e.Page.User, "v"));
            e.Template.Parse("U_FILTER_BEGINS_W", GenerateFriendsUri(e.Core, e.Page.User, "w"));
            e.Template.Parse("U_FILTER_BEGINS_X", GenerateFriendsUri(e.Core, e.Page.User, "x"));
            e.Template.Parse("U_FILTER_BEGINS_Y", GenerateFriendsUri(e.Core, e.Page.User, "y"));
            e.Template.Parse("U_FILTER_BEGINS_Z", GenerateFriendsUri(e.Core, e.Page.User, "z"));

            e.Template.Parse("PAGE_TITLE", string.Format("{0} Friends", e.Page.User.DisplayNameOwnership));

            e.Template.Parse("FRIENDS", e.Page.User.UserInfo.Friends.ToString());
            e.Template.Parse("L_FRIENDS", langFriends);

            List<Friend> friends = e.Page.User.GetFriends(e.Page.TopLevelPageNumber, 18, filter);
            foreach (UserRelation friend in friends)
            {
                VariableCollection friendVariableCollection = e.Template.CreateChild("friend_list");

                friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendVariableCollection.Parse("ICON", friend.Icon);
                friendVariableCollection.Parse("TILE", friend.Tile);
                friendVariableCollection.Parse("MOBILE_COVER", friend.MobileCoverPhoto);

                friendVariableCollection.Parse("ID", friend.Id);
                friendVariableCollection.Parse("TYPE", friend.TypeId);
                friendVariableCollection.Parse("LOCATION", friend.Profile.Country);
                e.Core.Display.ParseBbcode(friendVariableCollection, "ABSTRACT", friend.Profile.Autobiography);
                friendVariableCollection.Parse("SUBSCRIBERS", friend.Info.Subscribers);

                if (Subscription.IsSubscribed(e.Core, friend.ItemKey))
                {
                    friendVariableCollection.Parse("SUBSCRIBERD", "TRUE");
                    friendVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildUnsubscribeUri(friend.ItemKey));
                }
                else
                {
                    friendVariableCollection.Parse("U_SUBSCRIBE", e.Core.Hyperlink.BuildSubscribeUri(friend.ItemKey));
                }

                if (e.Core.Session.SignedIn && friend.Id == e.Core.LoggedInMemberId)
                {
                    friendVariableCollection.Parse("ME", "TRUE");
                }
            }

            string pageUri = e.Core.Hyperlink.BuildFriendsUri(e.Page.User);
            e.Core.Display.ParsePagination(pageUri, 18, e.Page.User.UserInfo.Friends);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "contacts/friends", e.Core.Prose.GetString("FRIENDS") });

            e.Page.User.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void ShowFamily(object sender, ShowUPageEventArgs e)
        {
            e.Template.SetTemplate("viewfamily.html");

            if (!e.Page.User.Access.Can("VIEW_FAMILY"))
            {
                e.Core.Functions.Generate403();
            }
        }

        public static string GenerateFriendsUri(Core core, User primitive)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Hyperlink.AppendSid(string.Format("{0}contacts/friends",
                primitive.UriStub));
        }

        public static string GenerateFriendsUri(Core core, User primitive, string filter)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return core.Hyperlink.AppendSid(string.Format("{0}contacts/friends?filter={1}",
                primitive.UriStub, filter));
        }
    }
}
