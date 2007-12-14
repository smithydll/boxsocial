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
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class ZzUri
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

        public static string BuildMarkGalleryCoverUri(long pictureId)
        {
            return AppendSid(string.Format("/account/?module=galleries&sub=gallery-cover&id={0}",
                pictureId), true);
        }

        public static string BuildMarkDisplayPictureUri(long pictureId)
        {
            return AppendSid(string.Format("/account/?module=galleries&sub=display-pic&id={0}",
                pictureId), true);
        }

        public static string BuildHomepageUri(Member member)
        {
            return AppendSid(string.Format("/{0}",
                member.UserName.ToLower()));
        }

        public static string BuildProfileUri(Member member)
        {
            if (member.ProfileHomepage == "/profile")
            {
                return AppendSid(string.Format("/{0}",
                    member.UserName.ToLower())); 
            }
            else
            {
                return AppendSid(string.Format("/{0}/profile",
                    member.UserName.ToLower()));
            }
        }

        public static string BuildGuestBookUri(Member member)
        {
            return AppendSid(string.Format("/{0}/profile/comments",
                member.UserName.ToLower()));
        }

        public static string BuildListsUri(Member member)
        {
            return AppendSid(string.Format("/{0}/lists",
                member.UserName.ToLower()));
        }

        public static string BuildListUri(Member member, string slug)
        {
            return AppendSid(string.Format("/{0}/lists/{1}",
                member.UserName.ToLower(), slug));
        }

        public static string BuildDeleteListUri(long deleteId)
        {
            return AppendSid(string.Format("/account/?module=pages&sub=lists&mode=delete&id={0}",
                deleteId), true);
        }

        public static string BuildEditListUri(long deleteId)
        {
            return AppendSid(string.Format("/account/?module=pages&sub=lists&mode=edit&id={0}",
                deleteId));
        }

        public static string BuildRemoveFromListUri(long deleteId)
        {
            return AppendSid(string.Format("/account/?module=pages&sub=lists&mode=remove&id={0}",
                deleteId), true);
        }

        public static string BuildBlogUri(Member member)
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

        public static string BuildBlogRssUri(Member member)
        {
            return AppendSid(string.Format("/{0}/blog?rss=true",
                member.UserName.ToLower()));
        }

        public static string BuildBlogUri(Member member, string category)
        {
            return AppendSid(string.Format("/{0}/blog/category/{1}",
                member.UserName.ToLower(), category));
        }

        public static string BuildBlogRssUri(Member member, string category)
        {
            return AppendSid(string.Format("/{0}/blog/category/{1}?rss=true",
                member.UserName.ToLower(), category));
        }

        public static string BuildBlogUri(Member member, int year)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}",
                member.UserName.ToLower(), year));
        }

        public static string BuildBlogRssUri(Member member, int year)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}?rss=true",
                member.UserName.ToLower(), year));
        }

        public static string BuildBlogUri(Member member, int year, int month)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}",
                member.UserName.ToLower(), year, month));
        }

        public static string BuildBlogRssUri(Member member, int year, int month)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}?rss=true",
                member.UserName.ToLower(), year, month));
        }

        public static string BuildBlogUri(Member member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildBlogRssUri(Member member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildFriendsUri(Member member)
        {
            return AppendSid(string.Format("/{0}/friends",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryUri(Member member)
        {
            return AppendSid(string.Format("/{0}/gallery",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryCommentsUri(Member member)
        {
            return AppendSid(string.Format("/{0}/gallery/comments",
                member.UserName.ToLower()));
        }

        public static string BuildGalleryCommentsUri(Member member, string path)
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
            return AppendSid(string.Format("/account/?module=galleries&sub=galleries&mode=edit&id={0}",
                galleryId), true);
        }

        public static string BuildGalleryDeleteUri(long galleryId)
        {
            return AppendSid(string.Format("/account/?module=galleries&sub=galleries&mode=delete&id={0}",
                galleryId), true);
        }

        public static string BuildPageUri(Member member, string pageSlug)
        {
            return AppendSid(string.Format("/{0}/{1}",
                member.UserName.ToLower(), pageSlug));
        }

        public static string BuildPhotoUploadUri(long galleryId)
        {
            return AppendSid(string.Format("/account/?module=galleries&sub=upload&id={0}",
                galleryId), true);
        }

        public static string BuildPhotoEditUri(long photoId)
        {
            return AppendSid(string.Format("/account/?module=galleries&sub=edit-photo&id={0}",
                photoId), true);
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
            return AppendSid(string.Format("/account/?module=galleries&sub=new&id={0}",
                galleryId), true);
        }

        public static string BuildLogoutUri()
        {
            return AppendSid("/sign-in/?mode=sign-out", true);
        }

        public static string BuildLoginUri()
        {
            return AppendSid("/sign-in/", true);
        }

        public static string BuildBlogPostUri(Member member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildBlogPostRssUri(Member member, int year, int month, long postId)
        {
            return AppendSid(string.Format("/{0}/blog/{1:0000}/{2:00}/{3}?rss=true",
                member.UserName.ToLower(), year, month, postId));
        }

        public static string BuildAddFriendUri(long friendId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=friends&mode=add&id={0}",
                friendId), true);
        }

        public static string BuildAddFamilyUri(long friendId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=family&mode=add&id={0}",
                friendId), true);
        }

        public static string BuildBlockUserUri(long blockId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=block&mode=block&id={0}",
                blockId), true);
        }

        public static string BuildUnBlockUserUri(long blockId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=block&mode=unblock&id={0}",
                blockId), true);
        }

        public static string BuildDeleteFriendUri(long deleteId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=friends&mode=delete&id={0}",
                deleteId), true);
        }

        public static string BuildDeleteFamilyUri(long deleteId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=family&mode=delete&id={0}",
                deleteId), true);
        }

        public static string BuildPromoteFriendUri(long promoteId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=friends&mode=promote&id={0}",
                promoteId), true);
        }

        public static string BuildDemoteFriendUri(long demoteId)
        {
            return AppendSid(string.Format("/account/?module=friends&sub=friends&mode=demote&id={0}",
                demoteId), true);
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
            else
            {
                return uri;
            }
        }

        // DONE
        /*public static string BuildGroupUri(Group newGroup)
        {
            return AppendSid(string.Format("/group/{0}",
                newGroup.Slug));
        }

        public static string BuildGroupEditUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=edit&id={0}",
                newGroup.GroupId));
        }

        public static string BuildGroupDeleteUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=delete&id={0}",
                newGroup.GroupId));
        }

        public static string BuildJoinGroupUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=join&id={0}",
                newGroup.GroupId), true);
        }

        public static string BuildLeaveGroupUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=leave&id={0}",
                newGroup.GroupId), true);
        }

        public static string BuildInviteGroupUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=invite&id={0}",
                newGroup.GroupId), true);
        }

        public static string BuildGroupMakeOperatorUri(Group newGroup, Member member)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=make-operator&id={0},{1}",
                newGroup.GroupId, member.UserId), true);
        }

        public static string BuildGroupResignOperatorUri(Group newGroup)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=resign-operator&id={0}",
                newGroup.GroupId), true);
        }

        public static string BuildGroupMakeOfficerUri(Group newGroup, Member member)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=make-officer&id={0},{1}",
                newGroup.GroupId, member.UserId), true);
        }

        public static string BuildGroupRemoveOfficerUri(Group newGroup, Member member, string title)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=remove-officer&id={0},{1},{2}",
                newGroup.GroupId, member.UserId, HttpUtility.UrlEncode(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(title)))), true);
        }

        public static string BuildGroupApprovalUri(Group newGroup, Member member)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=approve&id={0},{1}",
                newGroup.GroupId, member.UserId), true);
        }

        public static string BuildGroupBanUri(Group newGroup, Member member)
        {
            return AppendSid(string.Format("/account/?module=groups&sub=ban&id={0},{1}",
                newGroup.GroupId, member.UserId), true);
        }

        public static string BuildGroupMemberList(Group thisGroup)
        {
            return AppendSid(string.Format("/group/{0}/members",
                thisGroup.Slug));
        }*/

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
