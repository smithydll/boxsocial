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
    public class Linker
    {
        private static Core core;
        public static bool SecureUrls = false;
        public static bool SidUrls = false;
        private static string sid = "";

        public static Core Core
        {
            set
            {
                core = value;
            }
        }

        public static string Sid
        {
            set
            {
                sid = value;
            }
        }

        public static string Domain
        {
            get
            {
                return WebConfigurationManager.AppSettings["boxsocial-host"].ToLower();
            }
        }

        public static string CurrentDomain
        {
            get
            {
                return HttpContext.Current.Request.Url.Host.ToLower();
            }
        }

        public static string Uri
        {
            get
            {
                return string.Format("http://{0}/", Domain);
            }
        }

        public static string BuildMarkGalleryCoverUri(long pictureId)
        {
            return BuildAccountSubModuleUri("galleries", "gallery-cover", pictureId, true);
        }

        public static string BuildMarkDisplayPictureUri(long pictureId)
        {
            return BuildAccountSubModuleUri("galleries", "display-pic", pictureId, true);
        }

        public static string BuildGuestBookUri(User member)
        {
            return AppendSid(string.Format("{0}profile/comments",
                member.UriStub));
        }

        public static string BuildGuestBookUri(User member, User member2)
        {
            return AppendSid(string.Format("{0}profile/comments/{1}",
                member.UriStub, member2.UserName.ToLower()));
        }

        public static string BuildStatusUri(User member)
        {
            return AppendSid(string.Format("{0}profile/status",
                member.UriStub));
        }

        public static string BuildListsUri(User member)
        {
            return AppendSid(string.Format("{0}lists",
                member.UriStub));
        }

        public static string BuildListUri(User member, string slug)
        {
            return AppendSid(string.Format("{0}lists/{1}",
                member.UriStub, slug));
        }

        public static string BuildDeleteListUri(long deleteId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "delete", deleteId, true);
        }

        public static string BuildEditListUri(long editId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "edit", editId, true);
        }

        public static string BuildRemoveFromListUri(long removeId)
        {
            return BuildAccountSubModuleUri("pages", "lists", "remove", removeId, true);
        }

        public static string BuildBlogRssUri(User member)
        {
            return AppendSid(string.Format("{0}blog?rss=true",
                member.UriStub));
        }

        public static string BuildBlogRssUri(User member, string category)
        {
            return AppendSid(string.Format("{0}blog/category/{1}?rss=true",
                member.UriStub, category));
        }

        public static string BuildBlogRssUri(User member, int year)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}?rss=true",
                member.UriStub, year));
        }

        public static string BuildBlogRssUri(User member, int year, int month)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}?rss=true",
                member.UriStub, year, month));
        }

        public static string BuildBlogRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UriStub, year, month, postId));
        }

        public static string BuildFriendsUri(User member)
        {
            return AppendSid(string.Format("{0}friends",
                member.UriStub));
        }

        public static string BuildGalleryUri(User member)
        {
            return AppendSid(string.Format("{0}gallery",
                member.UriStub));
        }

        public static string BuildGalleryCommentsUri(User member)
        {
            return AppendSid(string.Format("{0}gallery/comments",
                member.UriStub));
        }

        public static string BuildGalleryCommentsUri(User member, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BuildGalleryCommentsUri(member);
            }
            else
            {
                return AppendSid(string.Format("{0}gallery/{1}/comments",
                    member.UriStub, path));
            }
        }

        public static string BuildGalleryEditUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "galleries", "edit", galleryId, true);
        }

        public static string BuildGalleryDeleteUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "galleries", "delete", galleryId, true);
        }

        public static string BuildPhotoUploadUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "upload", galleryId, true);
        }

        public static string BuildPhotoEditUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "edit-photo", photoId, true);
        }

        public static string BuildPhotoRotateLeftUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=left");
        }

        public static string BuildPhotoRotateRightUri(long photoId)
        {
            return BuildAccountSubModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=right");
        }

        public static string BuildNewGalleryUri(long galleryId)
        {
            return BuildAccountSubModuleUri("galleries", "galleries", "new", galleryId, true);
        }

        public static string BuildLogoutUri()
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

        public static string BuildLoginUri()
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

        public static string BuildLoginUri(string redirectUri)
        {
            return AppendCoreSid(string.Format("/sign-in/?redirect={0}", redirectUri), true);
        }

        public static string BuildBlogPostUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}",
                member.UriStub, year, month, postId));
        }

        public static string BuildBlogPostRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("{0}blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UriStub, year, month, postId));
        }

        public static string BuildAddFriendUri(long friendId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "add", friendId, true);
        }

        public static string BuildAddFriendUri(long friendId, bool appendSid)
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

        public static string BuildAddFamilyUri(long friendId)
        {
            return BuildAccountSubModuleUri("friends", "family", "add", friendId, true);
        }

        public static string BuildBlockUserUri(long blockId)
        {
            return BuildAccountSubModuleUri("friends", "block", "block", blockId, true);
        }

        public static string BuildUnBlockUserUri(long blockId)
        {
            return BuildAccountSubModuleUri("friends", "block", "unblock", blockId, true);
        }

        public static string BuildDeleteFriendUri(long deleteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "delete", deleteId, true);
        }

        public static string BuildDeleteFamilyUri(long deleteId)
        {
            return BuildAccountSubModuleUri("friends", "family", "delete", deleteId, true);
        }

        public static string BuildPromoteFriendUri(long promoteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "promote", promoteId, true);
        }

        public static string BuildDemoteFriendUri(long demoteId)
        {
            return BuildAccountSubModuleUri("friends", "friends", "demote", demoteId, true);
        }

        public static string BuildCommentQuoteUri(long commentId)
        {
            return AppendSid(string.Format("/comment/?mode=quote&id={0}",
                commentId));
        }

        public static string BuildCommentReportUri(long commentId)
        {
            return AppendSid(string.Format("/comment/?mode=report&id={0}",
                commentId));
        }

        public static string BuildCommentDeleteUri(long commentId)
        {
            return AppendSid(string.Format("/comment/?mode=delete&id={0}",
                commentId));
        }

        public static string AppendSid(string uri)
        {
            return AppendSid(uri, false);
        }

        public static string AppendSid(string uri, bool forceSid)
        {
            if (SidUrls || forceSid)
            {
                if (uri.Contains("?sid=") || uri.Contains("&sid=") || uri.Contains("&amp;sid="))
                {
                    return Regex.Replace(uri, "sid=([a-z0-9]+)", string.Format("sid={0}", sid));
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

        public static string AppendCoreSid(string uri)
        {
            return AppendCoreSid(uri, false);
        }

        public static string AppendCoreSid(string uri, bool forceSid)
        {
            if (Domain != CurrentDomain && (!uri.StartsWith("http://")))
            {
                return AppendSid(Uri + uri.TrimStart(new char[] { '/' }), forceSid);
            }
            else
            {
                return AppendSid(uri, forceSid);
            }
        }

        public static string AppendAbsoluteSid(string uri)
        {
            return AppendAbsoluteSid(uri, false);
        }

        public static string AppendAbsoluteSid(string uri, bool forceSid)
        {
            if (!uri.StartsWith("http://"))
            {
                return AppendSid(Uri + uri.TrimStart(new char[] { '/' }), forceSid);
            }
            else
            {
                return AppendSid(uri, forceSid);
            }
        }

        public static string StripSid(string uri)
        {
            int indexOfSid = uri.IndexOf("?sid");
            if (indexOfSid < 0)
            {
                uri.IndexOf("&sid");
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

        public static string BuildHomeUri()
        {
            return AppendCoreSid("/");
        }

        public static string BuildAboutUri()
        {
            return AppendCoreSid("/about");
        }

        public static string BuildSafetyUri()
        {
            return AppendCoreSid("/safety");
        }

        public static string BuildPrivacyUri()
        {
            return AppendCoreSid("/privacy");
        }

        public static string BuildTermsOfServiceUri()
        {
            return AppendCoreSid("/terms-of-service");
        }

        public static string BuildRegisterUri()
        {
            return AppendCoreSid("/register");
        }

        public static string BuildHelpUri()
        {
            return AppendCoreSid("/help");
        }

        public static string BuildSitemapUri()
        {
            return AppendCoreSid("/site-map");
        }

        public static string BuildCopyrightUri()
        {
            return AppendCoreSid("/copyright");
        }

        public static string BuildAccountUri()
        {
            return AppendCoreSid(core.session.LoggedInMember.AccountUriStub);
        }

        public static string BuildSearchUri()
        {
            return AppendCoreSid("/search");
        }

        #region "Account Module Uri"

        public static string BuildAccountModuleUri(string key)
        {
            return BuildAccountModuleUri(core.session.LoggedInMember, key);
        }

        public static string BuildAccountModuleUri(string key, bool appendSid)
        {
            return BuildAccountModuleUri(core.session.LoggedInMember, key, appendSid);
        }

        public static string BuildAccountModuleUri(Primitive owner, string key)
        {
            return BuildAccountModuleUri(owner, key, false);
        }

        public static string BuildAccountModuleUri(Primitive owner, string key, bool appendSid)
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

        public static string BuildAccountSubModuleUri(string key, string sub)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub);
        }

        public static string BuildAccountSubModuleUri(string key, string sub, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, appendSid);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub)
        {
            return BuildAccountSubModuleUri(owner, key, sub, false);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, bool appendSid)
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

        public static string BuildAccountSubModuleUri(string key, string sub, string mode)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, mode);
        }

        public static string BuildAccountSubModuleUri(string key, string sub, string mode, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, mode, appendSid);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode)
        {
            return BuildAccountSubModuleUri(owner, key, sub, mode, false);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, bool appendSid)
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

        public static string BuildAccountSubModuleUri(string key, string sub, string mode, long id)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, mode, id);
        }

        public static string BuildAccountSubModuleUri(string key, string sub, string mode, long id, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, mode, id, appendSid);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, long id)
        {
            return BuildAccountSubModuleUri(owner, key, sub, mode, id, false);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, string mode, long id, bool appendSid)
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

        public static string BuildAccountSubModuleUri(string key, string sub, long id)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, id);
        }

        public static string BuildAccountSubModuleUri(string key, string sub, long id, bool appendSid)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, id, appendSid);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, long id)
        {
            return BuildAccountSubModuleUri(owner, key, sub, id, false);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, long id, bool appendSid)
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

        public static string BuildAccountSubModuleUri(string key, string sub, params string[] arguments)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, false, arguments);
        }

        public static string BuildAccountSubModuleUri(string key, string sub, bool appendSid, params string[] arguments)
        {
            return BuildAccountSubModuleUri(core.session.LoggedInMember, key, sub, appendSid, arguments);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, params string[] arguments)
        {
            return BuildAccountSubModuleUri(owner, key, sub, false, arguments);
        }

        public static string BuildAccountSubModuleUri(Primitive owner, string key, string sub, bool appendSid, params string[] arguments)
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
