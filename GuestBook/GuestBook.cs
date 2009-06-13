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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.GuestBook
{
    public class GuestBook
    {

        public static string Uri(User member)
        {
            return Linker.AppendSid(string.Format("{0}profile/comments",
                member.UriStub));
        }

        public static string Uri(UserGroup thisGroup)
        {
            return Linker.AppendSid(string.Format("{0}comments",
                thisGroup.UriStub));
        }

        public static string Uri(Network theNetwork)
        {
            return Linker.AppendSid(string.Format("{0}comments",
                theNetwork.UriStub));
        }

        public static string Uri(ApplicationEntry anApplication)
        {
            return Linker.AppendSid(string.Format("{0}comments",
                anApplication.UriStub));
        }

        public static void Show(Core core, UPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            page.ProfileOwner.LoadProfileInfo();
            int p = Functions.RequestInt("p", 1);

            page.ProfileOwner.ProfileAccess.SetViewer(core.session.LoggedInMember);

            if (!page.ProfileOwner.ProfileAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            if (core.session.IsLoggedIn)
            {
                if (page.ProfileOwner.ProfileAccess.CanComment)
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            page.template.Parse("IS_USER_GUESTBOOK", "TRUE");

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "profile", "Profile" });
            breadCrumbParts.Add(new string[] { "comments", "Comments" });

            Display.DisplayComments(page.template, page.ProfileOwner, page.ProfileOwner, UserGuestBookHook);
            page.template.Parse("L_GUESTBOOK", page.ProfileOwner.DisplayNameOwnership + " Guest Book");
            Display.ParsePagination(Linker.BuildGuestBookUri(page.ProfileOwner), p, (int)Math.Ceiling(page.ProfileOwner.ProfileComments / 10.0));
            page.ProfileOwner.ParseBreadCrumbs(breadCrumbParts);
        }

        // TODO: use user
        public static void Show(Core core, UPage page, string user)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            page.ProfileOwner.LoadProfileInfo();
            int p = Functions.RequestInt("p", 1);

            page.ProfileOwner.ProfileAccess.SetViewer(core.session.LoggedInMember);

            if (!page.ProfileOwner.ProfileAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            if (core.session.IsLoggedIn)
            {
                if (page.ProfileOwner.ProfileAccess.CanComment)
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            page.template.Parse("IS_USER_GUESTBOOK", "TRUE");

            long userId = core.LoadUserProfile(user);

            List<User> commenters = new List<User>();
            commenters.Add(page.ProfileOwner);
            commenters.Add(core.UserProfiles[userId]);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] {"profile", "Profile"});
            breadCrumbParts.Add(new string[] {"comments", "Comments"});
            breadCrumbParts.Add(new string[] {core.UserProfiles[userId].Key, core.UserProfiles[userId].DisplayName});

            // Load the comment count
            long comments = 0;

            SelectQuery query = new SelectQuery("guestbook_comment_counts");
            query.AddField(new QueryFunction("comment_comments", QueryFunctions.Sum, "comments"));

            QueryCondition qc1 = query.AddCondition("owner_id", commenters[0].Id);
            qc1.AddCondition("user_id", commenters[1].Id);

            QueryCondition qc2 = query.AddCondition(ConditionRelations.Or, "owner_id", commenters[1].Id);
            qc2.AddCondition("user_id", commenters[0].Id);

            DataTable commentCountDataTable = core.db.Query(query);

            if (commentCountDataTable.Rows.Count > 0)
            {
                if (!(commentCountDataTable.Rows[0]["comments"] is DBNull))
                {
                    comments = (long)(Decimal)commentCountDataTable.Rows[0]["comments"];
                }
            }

            Display.DisplayComments(page.template, page.ProfileOwner, page.ProfileOwner, commenters, comments, UserGuestBookHook);

            //page.template.Parse("PAGINATION", Display.GeneratePagination(Linker.BuildGuestBookUri(page.ProfileOwner, core.UserProfiles[userId]), p, (int)Math.Ceiling(comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.ProfileOwner.GenerateBreadCrumbs(breadCrumbParts));
            page.template.Parse("L_GUESTBOOK", page.ProfileOwner.DisplayNameOwnership + " Guest Book");
            Display.ParsePagination(Linker.BuildGuestBookUri(page.ProfileOwner, core.UserProfiles[userId]), p, (int)Math.Ceiling(comments / 10.0));
            page.ProfileOwner.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void UserGuestBookHook(DisplayCommentHookEventArgs e)
        {
            if (e.Owner.GetType() == typeof(User))
            {
                if (e.Owner.Id != e.Poster.Id)
                {
                    UserGuestBook guestBook = new UserGuestBook(e.Core, (User)e.Owner);
                    UserGuestBook posterGuestBook = new UserGuestBook(e.Core, e.Poster);
                    e.CommentVariableCollection.Parse("U_CONVERSATION", guestBook.BuildConversationUri(e.Poster));
                    e.CommentVariableCollection.Parse("U_REPLY", posterGuestBook.Uri);
                }
            }
        }

        public static void Show(Core core, GPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(page.template, page.ThisGroup, page.ThisGroup);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.ThisGroup), p, (int)Math.Ceiling(page.ThisGroup.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.ThisGroup.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.ThisGroup.DisplayNameOwnership + " Guest Book");
            Display.ParsePagination(GuestBook.Uri(page.ThisGroup), p, (int)Math.Ceiling(page.ThisGroup.Comments / 10.0));
            page.ThisGroup.ParseBreadCrumbs("comments");
            //Prose.GetString("GuestBook");
        }

        public static void Show(Core core, NPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                if (page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(page.template, page.TheNetwork, page.TheNetwork);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.TheNetwork), p, (int)Math.Ceiling(page.TheNetwork.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.TheNetwork.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.TheNetwork.DisplayNameOwnership + " Guest Book");
            Display.ParsePagination(GuestBook.Uri(page.TheNetwork), p, (int)Math.Ceiling(page.TheNetwork.Comments / 10.0));
            page.TheNetwork.ParseBreadCrumbs("comments");
        }

        public static void Show(Core core, APage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                page.template.Parse("CAN_COMMENT", "TRUE");
            }

            Display.DisplayComments(page.template, page.AnApplication, page.AnApplication);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.AnApplication), p, (int)Math.Ceiling(page.AnApplication.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.AnApplication.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.AnApplication.DisplayNameOwnership + " Guest Book");
            Display.ParsePagination(GuestBook.Uri(page.AnApplication), p, (int)Math.Ceiling(page.AnApplication.Comments / 10.0));
            page.AnApplication.ParseBreadCrumbs("comments");
        }
    }
}
