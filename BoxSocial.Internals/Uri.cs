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
        public static bool SecureUrls = false;
        public static bool SidUrls = false;
        private static string sid = "";

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

        public static string Uri
        {
            get
            {
                return string.Format("http://{0}/", Domain);
            }
        }

        public static string BuildMarkGalleryCoverUri(long pictureId)
        {
            return AppendSid(string.Format("/account/galleries/gallery-cover?id={0}",
                pictureId), true);
        }

        public static string BuildMarkDisplayPictureUri(long pictureId)
        {
            return AppendSid(string.Format("/account/galleries/display-pic?id={0}",
                pictureId), true);
        }

        public static string BuildGuestBookUri(User member)
        {
            return AppendSid(string.Format("/{0}/profile/comments",
                member.UserName.ToLower()));
        }

        public static string BuildGuestBookUri(User member, User member2)
        {
            return AppendSid(string.Format("/{0}/profile/comments/{1}",
                member.UserName.ToLower(), member2.UserName.ToLower()));
        }

        public static string BuildStatusUri(User member)
        {
            return AppendSid(string.Format("/{0}/profile/status",
                member.UserName.ToLower()));
        }

        public static string BuildListsUri(User member)
        {
            return AppendSid(string.Format("/{0}/lists",
                member.UserName.ToLower()));
        }

        public static string BuildListUri(User member, string slug)
        {
            return AppendSid(string.Format("/{0}/lists/{1}",
                member.UserName.ToLower(), slug));
        }

        public static string BuildDeleteListUri(long deleteId)
        {
            return AccountModule.BuildModuleUri("pages", "lists", "delete", deleteId);
        }

        public static string BuildEditListUri(long editId)
        {
            return AccountModule.BuildModuleUri("pages", "lists", "edit", editId);
        }

        public static string BuildRemoveFromListUri(long removeId)
        {
            return AccountModule.BuildModuleUri("pages", "lists", "remove", removeId);
        }

        public static string BuildBlogUri(User member)
        {
            if (member.ProfileHomepage == "/blog")
            {
                return AppendSid(string.Format("/{0}",
                    member.UserName.ToLower()));
            }
            else
            {
                return AppendSid(string.Format("/{0}/blog",
                    member.UserName.ToLower()));
            }
        }

        public static string BuildBlogRssUri(User member)
        {
            return AppendSid(string.Format("/{0}/blog?rss=true",
                member.UserName.ToLower()));
        }

        public static string BuildBlogUri(User member, string category)
        {
            return AppendSid(string.Format("/{0}/blog/category/{1}",
                member.UserName.ToLower(), category));
        }

        public static string BuildBlogRssUri(User member, string category)
        {
            return AppendSid(string.Format("/{0}/blog/category/{1}?rss=true",
                member.UserName.ToLower(), category));
        }

        public static string BuildBlogUri(User member, int year)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}",
                member.UserName.ToLower(), year));
        }

        public static string BuildBlogRssUri(User member, int year)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}?rss=true",
                member.UserName.ToLower(), year));
        }

        public static string BuildBlogUri(User member, int year, int month)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}",
                member.UserName.ToLower(), year, month));
        }

        public static string BuildBlogRssUri(User member, int year, int month)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}?rss=true",
                member.UserName.ToLower(), year, month));
        }

        public static string BuildBlogUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildBlogRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildFriendsUri(User member)
        {
            return AppendSid(string.Format("/{0}/friends",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryUri(User member)
        {
            return AppendSid(string.Format("/{0}/gallery",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryCommentsUri(User member)
        {
            return AppendSid(string.Format("/{0}/gallery/comments",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryCommentsUri(User member, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BuildGalleryCommentsUri(member);
            }
            else
            {
                return AppendSid(string.Format("/{0}/gallery/{1}/comments",
                    member.UserName.ToLower(), path));
            }
        }

        public static string BuildGalleryEditUri(long galleryId)
        {
            return AccountModule.BuildModuleUri("galleries", "galleries", "edit", galleryId);
        }

        public static string BuildGalleryDeleteUri(long galleryId)
        {
            return AccountModule.BuildModuleUri("galleries", "galleries", "delete", galleryId);
        }

        public static string BuildPageUri(User member, string pageSlug)
        {
            return AppendSid(string.Format("/{0}/{1}",
                member.UserName.ToLower(), pageSlug));
        }

        public static string BuildPhotoUploadUri(long galleryId)
        {
            return AccountModule.BuildModuleUri("galleries", "upload", true, string.Format("id={0}", galleryId));
        }

        public static string BuildPhotoEditUri(long photoId)
        {
            return AccountModule.BuildModuleUri("galleries", "edit-photo", true, string.Format("id={0}", photoId));
        }

        public static string BuildPhotoRotateLeftUri(long photoId)
        {
            return AccountModule.BuildModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=left");
        }

        public static string BuildPhotoRotateRightUri(long photoId)
        {
            return AccountModule.BuildModuleUri("galleries", "rotate-photo", true, string.Format("id={0}", photoId), "rotation=right");
        }

        public static string BuildNewGalleryUri(long galleryId)
        {
            return AccountModule.BuildModuleUri("galleries", "galleries", "new", galleryId);
        }

        public static string BuildLogoutUri()
        {
            return AppendSid("/sign-in/?mode=sign-out", true);
        }

        public static string BuildLoginUri()
        {
            return AppendSid("/sign-in/", true);
        }

        public static string BuildBlogPostUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildBlogPostRssUri(User member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildAddFriendUri(long friendId)
        {
            return AppendSid(string.Format("/account/friends/friends?mode=add&id={0}",
                friendId), true);
        }

        public static string BuildAddFriendUri(long friendId, bool appendSid)
        {
            if (appendSid)
            {
                return BuildAddFriendUri(friendId);
            }
            else
            {
                return string.Format("/account/friends/friends?mode=add&id={0}",
                    friendId);
            }
        }

        public static string BuildAddFamilyUri(long friendId)
        {
            return AccountModule.BuildModuleUri("friends", "family", "add", friendId);
        }

        public static string BuildBlockUserUri(long blockId)
        {
            return AccountModule.BuildModuleUri("friends", "block", "block", blockId);
        }

        public static string BuildUnBlockUserUri(long blockId)
        {
            return AccountModule.BuildModuleUri("friends", "block", "unblock", blockId);
        }

        public static string BuildDeleteFriendUri(long deleteId)
        {
            return AccountModule.BuildModuleUri("friends", "friends", "delete", deleteId);
        }

        public static string BuildDeleteFamilyUri(long deleteId)
        {
            return AccountModule.BuildModuleUri("friends", "family", "delete", deleteId);
        }

        public static string BuildPromoteFriendUri(long promoteId)
        {
            return AccountModule.BuildModuleUri("friends", "friends", "promote", promoteId);
        }

        public static string BuildDemoteFriendUri(long demoteId)
        {
            return AccountModule.BuildModuleUri("friends", "friends", "demote", demoteId);
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
            return AppendSid("/");
        }

        public static string BuildAboutUri()
        {
            return AppendSid("/about");
        }

        public static string BuildSafetyUri()
        {
            return AppendSid("/safety");
        }

        public static string BuildPrivacyUri()
        {
            return AppendSid("/privacy");
        }

        public static string BuildTermsOfServiceUri()
        {
            return AppendSid("/terms-of-service");
        }

        public static string BuildRegisterUri()
        {
            return AppendSid("/register");
        }

        public static string BuildHelpUri()
        {
            return AppendSid("/help");
        }

        public static string BuildSitemapUri()
        {
            return AppendSid("/site-map");
        }

        public static string BuildCopyrightUri()
        {
            return AppendSid("/copyright");
        }

        public static string BuildAccountUri()
        {
            return AppendSid("/account");
        }
    }
}
