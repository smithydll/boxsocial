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
using System.Data;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Hyperlink
    {
        private Core core;
        public bool SecureUrls;
        public bool SidUrls;
        private string sid = String.Empty;

        public Hyperlink(Core core)
        {
            this.core = core;
        }

        public string Sid
        {
            set
            {
                sid = value;
            }
        }

        /// <summary>
        /// The root domain name for boxsocial
        /// </summary>
        public static string Domain
        {
            get
            {
				if (WebConfigurationManager.AppSettings != null &&  WebConfigurationManager.AppSettings.HasKeys())
				{
					return WebConfigurationManager.AppSettings["boxsocial-host"].ToLower();
				}
				else
				{
					return "localhost";
				}
            }
        }

        /// <summary>
        /// The current domain being accessed via http
        /// </summary>
        public string CurrentDomain
        {
            get
            {
				if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Url != null)
				{
					return HttpContext.Current.Request.Url.Host.ToLower();
				}
                else if (core.Http != null)
                {
                    return core.Http.Domain;
                }
				else
				{
					return "localhost";
				}
            }
        }

        public string Uri
        {
            get
            {
                string protocol = "http://";
                if (core.Http != null)
                {
                    protocol = core.Http.DefaultProtocol;
                }
                else
                {
                    if (core.Settings.UseSecureCookies)
                    {
                        protocol = "https://";
                    }
                    else
                    {
                        protocol = "http://";
                    }
                }
                return string.Format(protocol + "{0}/", Domain);
            }
        }

        public string BuildMarkGalleryCoverUri(long pictureId)
        {
            return BuildAccountSubModuleUri("galleries", "gallery-cover", pictureId, true);
        }

        public string BuildMarkDisplayPictureUri(long pictureId)
        {
            return BuildAccountSubModuleUri("galleries", "display-pic", pictureId, true);
        }

        public string BuildGuestBookUri(User member)
        {
            return AppendSid(string.Format("{0}profile/comments",
                member.UriStub));
        }

        public string BuildGuestBookUri(User member, User member2)
        {
            return AppendSid(string.Format("{0}profile/comments/{1}",
                member.UriStub, member2.UserName.ToLower()));
        }

        public string BuildStatusUri(User member)
        {
            return AppendSid(string.Format("{0}status-feed",
                member.UriStub));
        }

        public string BuildCombinedFeedUri(User member)
        {
            return AppendSid(string.Format("{0}feed",
                member.UriStub));
        }

        public string BuildListsUri(User member)
        {
            return AppendSid(string.Format("{0}lists",
                member.UriStub));
        }

        public string BuildListUri(User member, string slug)
        {
            return AppendSid(string.Format("{0}lists/{1}",
                member.UriStub, slug));
        }

        public string BuildDeleteListUri(long deleteId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "delete", deleteId, true);
        }

        public string BuildEditListUri(long editId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "edit", editId, true);
        }

        public string BuildRemoveFromListUri(long removeId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "remove", removeId, true);
        }

        public string BuildBlogRssUri(User member)
        {
            return AppendSid(string.Format("{0}blog?rss=true",
                member.UriStub));
        }

        public string BuildBlogRssUri(User member, string category)
        {
            return AppendSid(string.Format("{0}blog/category/{1}?rss=true",
                member.UriStub, category));
        }

        public string BuildBlogRssUri(User member, int year)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}?rss=true",
                member.UriStub, year));
        }

        public string BuildBlogRssUri(User member, int year, int month)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}?rss=true",
                member.UriStub, year, month));
        }

        public string BuildBlogRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UriStub, year, month, postId));
        }

        public string BuildFriendsUri(User member)
        {
            return AppendSid(string.Format("{0}friends",
                member.UriStub));
        }

        public string BuildSubscriptionsUri(User member)
        {
            return AppendSid(string.Format("{0}subscriptions",
                member.UriStub));
        }

        public string BuildSubscribersUri(User member)
        {
            return AppendSid(string.Format("{0}subscribers",
                member.UriStub));
        }

        public string BuildGalleryCommentsUri(Primitive owner)
        {
            return AppendSid(string.Format("{0}gallery/comments",
                owner.UriStub));
        }

        public string BuildGalleryCommentsUri(Primitive owner, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BuildGalleryCommentsUri(owner);
            }
            else
            {
                return AppendSid(string.Format("{0}gallery/{1}/comments",
                    owner.UriStub, path));
            }
        }

        public string BuildGalleryEditUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "galleries", "edit", galleryId, true);
        }

        public string BuildGalleryDeleteUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "galleries", "delete", galleryId, true);
        }

        public string BuildPhotoEditUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "edit-photo", photoId, true);
        }

        public string BuildPhotoRotateLeftUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=left");
        }

        public string BuildPhotoRotateRightUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=right");
        }

        public string BuildLogoutUri()
        {
            if (Domain != CurrentDomain)
            {
                return AppendCoreSid(string.Format("/sign-in/?mode=sign-out&domain={0}", CurrentDomain), true);
            }
            else
            {
                return AppendCoreSid("/sign-in/?mode=sign-out", true);
            }
        }

        public string BuildLoginUri()
        {
            if (Domain != CurrentDomain)
            {
                return AppendCoreSid(string.Format("/sign-in/?domain={0}&redirect={1}", CurrentDomain, core.PagePath), true);
            }
            else
            {
                return AppendCoreSid("/sign-in/", true);
            }
        }

        public string BuildLoginUri(string redirectUri)
        {
            return AppendCoreSid(string.Format("/sign-in/?redirect={0}", HttpUtility.UrlEncode(redirectUri)), true);
        }

        public string BuildBlogPostUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStub, year, month, postId));
        }

        public string BuildBlogPostRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UriStub, year, month, postId));
        }

        public string BuildAddFriendUri(long friendId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "add", friendId, true);
        }

        public string BuildAddFriendUri(long friendId, bool appendSid)
        {
            if (appendSid)
            {
                return BuildAddFriendUri(friendId);
            }
            else
            {
                return BuildAccountSubModuleUri("friends", "friends", "add", friendId, appendSid);
            }
        }

        public string BuildAddFamilyUri(long friendId)
        {
            return BuildAccountSubModuleUri("friends", "family", "add", friendId, true);
        }

        public string BuildBlockUserUri(long blockId)
        {
            return BuildAccountSubModuleUri("friends", "block", "block", blockId, true);
        }

        public string BuildUnBlockUserUri(long blockId)
        {
            return BuildAccountSubModuleUri("friends", "block", "unblock", blockId, true);
        }

        public string BuildDeleteFriendUri(long deleteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "delete", deleteId, true);
        }

        public string BuildDeleteFamilyUri(long deleteId)
        {
            return BuildAccountSubModuleUri("friends", "family", "delete", deleteId, true);
        }

        public string BuildPromoteFriendUri(long promoteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "promote", promoteId, true);
        }

        public string BuildDemoteFriendUri(long demoteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "demote", demoteId, true);
        }

        public string BuildCommentQuoteUri(long commentId)
        {
            return AppendSid(string.Format("/api/comment?mode=quote&id={0}",
                commentId));
        }

        public string BuildCommentReportUri(long commentId)
        {
            return AppendSid(string.Format("/api/comment?mode=report&id={0}",
                commentId));
        }

        public string BuildCommentDeleteUri(long commentId)
        {
            return AppendSid(string.Format("/api/comment?mode=delete&id={0}",
                commentId));
        }

        public string BuildLikeItemUri(long typeId, long id)
        {
            return AppendSid(string.Format("/api/like?mode=like&item={0}&type={1}",
                id, typeId));
        }

        public string AppendSid(string uri)
        {
            return AppendSid(uri, false);
        }

        public string AppendSid(string uri, bool forceSid)
        {
            return AppendSid(uri, forceSid, sid);
        }

        public string AppendSid(string uri, bool forceSid, string sid)
        {
            if (SidUrls || forceSid)
            {
                int ior = uri.IndexOf("?sid=");
                int ioo = 5;
                if (ior >= 0)
                {
                    ior = uri.IndexOf("&sid=");
                    ioo = 5;
                }
                if (ior < 0)
                {
                    ior = uri.IndexOf("&amp;sid=");
                    ioo = 9;
                }
                if (ior >= 0)
                {
                    return uri.Remove(ior + ioo, 32).Insert(ior + ioo, sid);
                    //return Regex.Replace(uri, "sid=([a-z0-9]+)", string.Format("sid={0}", sid));
                }
                else
                {
                    if (uri.Contains("?"))
                    {
                        return string.Format("{0}&sid={1}",
                            uri, sid);
                    }
                    else
                    {
                        return string.Format("{0}?sid={1}",
                            uri, sid);
                    }
                }
            }
            else
            {
                return uri;
            }
        }

        public string AppendNvid(string uri, string verificationString)
        {
            if (string.IsNullOrEmpty(verificationString) || verificationString.Length < 32)
            {
                throw new FormatException("Verification string too short");
            }

            if (uri.Contains("?"))
            {
                return string.Format("{0}&nvid={1}",
                    StripSid(uri), verificationString);
            }
            else
            {
                return string.Format("{0}?nvid={1}",
                    StripSid(uri), verificationString);
            }
        }

        public string AppendCoreSid(string uri)
        {
            return AppendCoreSid(uri, false);
        }

        public string AppendCoreSid(string uri, bool forceSid)
        {
            if (Domain != CurrentDomain && (!uri.StartsWith("http://", StringComparison.Ordinal)) && (!uri.StartsWith("https://", StringComparison.Ordinal)))
            {
                return AppendSid(Uri + uri.TrimStart(new char[] { '/' }), forceSid);
            }
            else
            {
                return AppendSid(uri, forceSid);
            }
        }

        public string AppendAbsoluteSid(string uri)
        {
            return AppendAbsoluteSid(uri, false);
        }

        public string AppendAbsoluteSid(string uri, bool forceSid)
        {
            if ((!uri.StartsWith("http://", StringComparison.Ordinal)) && (!uri.StartsWith("https://", StringComparison.Ordinal)))
            {
                return AppendSid(Uri + uri.TrimStart(new char[] { '/' }), forceSid);
            }
            else
            {
                return AppendSid(uri, forceSid);
            }
        }

        public string AppendCurrentSid(string uri)
        {
            return AppendCurrentSid(uri, false);
        }

        public string AppendCurrentSid(string uri, bool forceSid)
        {
            if ((!uri.StartsWith("http://", StringComparison.Ordinal)) && (!uri.StartsWith("https://", StringComparison.Ordinal)))
            {
                return AppendSid(core.Http.DefaultProtocol + CurrentDomain + "/" + uri.TrimStart(new char[] { '/' }), forceSid);
            }
            else
            {
                return AppendSid(uri, forceSid);
            }
        }

        public string StripSid(string uri)
        {
            int indexOfSid = uri.IndexOf("?sid");
            if (indexOfSid < 0)
            {
                indexOfSid = uri.IndexOf("&sid");
            }
            if (indexOfSid >= 0)
            {
                return uri.Remove(indexOfSid);
            }
            else
            {
                return uri;
            }
        }

        public string StripScheme(string uri)
        {
            Uri u = new Uri(uri);
            return uri.Substring(u.Scheme.Length + 3);
        }

        public string BuildHomeUri()
        {
            return AppendCoreSid("/");
        }

        public string BuildAboutUri()
        {
            return AppendCoreSid("/about");
        }

        public string BuildSafetyUri()
        {
            return AppendCoreSid("/safety");
        }

        public string BuildPrivacyUri()
        {
            return AppendCoreSid("/privacy");
        }

        public string BuildTermsOfServiceUri()
        {
            return AppendCoreSid("/terms-of-service");
        }

        public string BuildRegisterUri()
        {
            return AppendCoreSid("/register");
        }

        public string BuildRegisterUri(string key)
        {
            return AppendCoreSid("/register?key=" + key);
        }

        public string BuildRegisterUri(long groupId)
        {
            return AppendCoreSid("/register?gid=" + groupId);
        }

        public string BuildOptOutUri(string emailKey)
        {
            return AppendCoreSid("/register?mode=optout&key=" + emailKey);
        }

        public string BuildHelpUri()
        {
            return AppendCoreSid("/help");
        }

        public string BuildSitemapUri()
        {
            return AppendCoreSid("/site-map");
        }

        public string BuildCopyrightUri()
        {
            return AppendCoreSid("/copyright");
        }

        public string BuildAccountUri()
        {
            if (core.Session.LoggedInMember != null)
            {
                return AppendCoreSid(core.Session.LoggedInMember.AccountUriStub);
            }
            else
            {
                return "";
            }
        }

        public string BuildSearchUri()
        {
            return AppendCoreSid("/search");
        }

        public string BuildSearchUri(string query)
        {
            return AppendCoreSid("/search?q=" + HttpUtility.UrlEncode(query));
        }

        public string BuildGroupsUri()
        {
            return AppendCoreSid("/groups");
        }

        public string BuildNetworksUri()
        {
            return AppendCoreSid("/networks");
        }

        public string BuildMusicUri()
        {
            return AppendCoreSid("/music");
        }

        public string BuildMusicDirectoryUri()
        {
            return AppendCoreSid("/music/directory");
        }

        public string BuildMusicChartUri()
        {
            return AppendCoreSid("/music/chart");
        }

        public string BuildSubscribeUri(ItemKey itemKey)
        {
            return AppendCoreSid(string.Format("/api/subscribe?mode=subscribe&item={0}&type={1}",
                itemKey.Id, itemKey.TypeId));
        }

        public string BuildUnsubscribeUri(ItemKey itemKey)
        {
            return AppendCoreSid(string.Format("/api/subscribe?mode=unsubscribe&item={0}&type={1}",
                itemKey.Id, itemKey.TypeId));
        }

        #region "Account Module Uri"

        public string BuildAccountModuleUri(string key)
        {
            return BuildAccountModuleUri(core.Session.LoggedInMember, key);
        }

        public string BuildAccountModuleUri(string key, bool appendSid)
        {
            return BuildAccountModuleUri(core.Session.LoggedInMember, key, appendSid);
        }

        public string BuildAccountModuleUri(Primitive owner, string key)
        {
            return BuildAccountModuleUri(owner, key, false);
        }

        public string BuildAccountModuleUri(Primitive owner, string key, bool appendSid)
        {
            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}",
                    owner.AccountUriStub, key), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}",
                    "/account/", key), appendSid);
            }
        }

        #endregion

        #region "Account Sub Module Uri"

        public string BuildAccountSubModuleUri(string key, string sub)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub);
        }

        public string BuildAccountSubModuleUri(string key, string sub, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, appendSid);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub)
        {
            return BuildAccountSubModuleUri(owner, key, sub, false);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, bool appendSid)
        {
            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}/{2}",
                    owner.AccountUriStub, key, sub), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}/{2}",
                    "/account/", key, sub), appendSid);
            }
        }

        public string BuildAccountSubModuleUri(string key, string sub, string mode)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, mode);
        }

        public string BuildAccountSubModuleUri(string key, string sub, string mode, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, mode, appendSid);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode)
        {
            return BuildAccountSubModuleUri(owner, key, sub, mode, false);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, bool appendSid)
        {
            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}/{2}?mode={3}",
                    owner.AccountUriStub, key, sub, mode), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}/{2}?mode={3}",
                    "/account/", key, sub, mode), appendSid);
            }
        }

        public string BuildAccountSubModuleUri(string key, string sub, string mode, long id)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, mode, id);
        }

        public string BuildAccountSubModuleUri(string key, string sub, string mode, long id, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, mode, id, appendSid);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, long id)
        {
            return BuildAccountSubModuleUri(owner, key, sub, mode, id, false);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, long id, bool appendSid)
        {
            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}/{2}?mode={3}&id={4}",
                    owner.AccountUriStub, key, sub, mode, id), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}/{2}?mode={3}&id={4}",
                    "/account/", key, sub, mode, id), appendSid);
            }
        }

        public string BuildAccountSubModuleUri(string key, string sub, long id)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, id);
        }

        public string BuildAccountSubModuleUri(string key, string sub, long id, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, id, appendSid);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, long id)
        {
            return BuildAccountSubModuleUri(owner, key, sub, id, false);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, long id, bool appendSid)
        {
            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}/{2}?id={3}",
                    owner.AccountUriStub, key, sub, id), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}/{2}?id={3}",
                    "/account/", key, sub, id), appendSid);
            }
        }

        public string BuildAccountSubModuleUri(string key, string sub, params string[] arguments)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, false, arguments);
        }

        public string BuildAccountSubModuleUri(string key, string sub, bool appendSid, params string[] arguments)
        {
            return BuildAccountSubModuleUri(core.Session.LoggedInMember, key, sub, appendSid, arguments);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, params string[] arguments)
        {
            return BuildAccountSubModuleUri(owner, key, sub, false, arguments);
        }

        public string BuildAccountSubModuleUri(Primitive owner, string key, string sub, bool appendSid, params string[] arguments)
        {
            string argumentList = "";

            foreach (string argument in arguments)
            {
                if (argumentList == "")
                {
                    argumentList = string.Format("?{0}",
                        argument);
                }
                else
                {
                    argumentList = string.Format("{0}&{1}",
                        argumentList, argument);
                }
            }

            if (owner != null)
            {
                return AppendSid(string.Format("{0}{1}/{2}{3}",
                    owner.AccountUriStub, key, sub, argumentList), appendSid);
            }
            else
            {
                return AppendSid(string.Format("{0}{1}/{2}{3}",
                    "/account/", key, sub, argumentList), appendSid);
            }
        }

        #endregion
    }
}
