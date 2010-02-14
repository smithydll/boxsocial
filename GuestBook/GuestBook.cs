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

        public static string Uri(Core core, User member)
        {
            return core.Uri.AppendSid(string.Format("{0}profile/comments",
                member.UriStub));
        }

        public static string Uri(Core core, UserGroup thisGroup)
        {
            return core.Uri.AppendSid(string.Format("{0}comments",
                thisGroup.UriStub));
        }

        public static string Uri(Core core, Network theNetwork)
        {
            return core.Uri.AppendSid(string.Format("{0}comments",
                theNetwork.UriStub));
        }

        public static string Uri(Core core, ApplicationEntry anApplication)
        {
            return core.Uri.AppendSid(string.Format("{0}comments",
                anApplication.UriStub));
        }

        public static void Show(Core core, UPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            page.User.LoadProfileInfo();
            int p = core.Functions.RequestInt("p", 1);

            //page.User.Access.SetViewer(core.session.LoggedInMember);

            if (!page.User.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            if (core.Session.IsLoggedIn)
            {
                if (page.User.Access.Can("COMMENT"))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            page.template.Parse("IS_USER_GUESTBOOK", "TRUE");

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "profile", "Profile" });
            breadCrumbParts.Add(new string[] { "comments", "Guest Book" });

            core.Display.DisplayComments(page.template, page.User, page.User, UserGuestBookHook);
            page.template.Parse("L_GUESTBOOK", page.User.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination(core.Uri.BuildGuestBookUri(page.User), p, (int)Math.Ceiling(page.User.Comments / 10.0));
            page.User.ParseBreadCrumbs(breadCrumbParts);
        }

        // TODO: use user
        public static void Show(Core core, UPage page, string user)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            page.User.LoadProfileInfo();
            int p = core.Functions.RequestInt("p", 1);

            //page.User.Access.SetViewer(core.session.LoggedInMember);

            if (!page.User.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            if (core.Session.IsLoggedIn)
            {
                if (page.User.Access.Can("COMMENT"))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            page.template.Parse("IS_USER_GUESTBOOK", "TRUE");

            long userId = core.LoadUserProfile(user);

            List<User> commenters = new List<User>();
            commenters.Add(page.User);
            commenters.Add(core.PrimitiveCache[userId]);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] {"profile", "Profile"});
            breadCrumbParts.Add(new string[] {"comments", "Comments"});
            breadCrumbParts.Add(new string[] {core.PrimitiveCache[userId].Key, core.PrimitiveCache[userId].DisplayName});

            // Load the comment count
            long comments = 0;

            SelectQuery query = new SelectQuery("guestbook_comment_counts");
            query.AddField(new QueryFunction("comment_comments", QueryFunctions.Sum, "comments"));

            QueryCondition qc1 = query.AddCondition("owner_id", commenters[0].Id);
            qc1.AddCondition("user_id", commenters[1].Id);

            QueryCondition qc2 = query.AddCondition(ConditionRelations.Or, "owner_id", commenters[1].Id);
            qc2.AddCondition("user_id", commenters[0].Id);

            DataTable commentCountDataTable = core.Db.Query(query);

            if (commentCountDataTable.Rows.Count > 0)
            {
                if (!(commentCountDataTable.Rows[0]["comments"] is DBNull))
                {
                    comments = (long)(Decimal)commentCountDataTable.Rows[0]["comments"];
                }
            }

            core.Display.DisplayComments(page.template, page.User, page.User, commenters, comments, UserGuestBookHook);

            //page.template.Parse("PAGINATION", Display.GeneratePagination(Linker.BuildGuestBookUri(page.ProfileOwner, core.UserProfiles[userId]), p, (int)Math.Ceiling(comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.ProfileOwner.GenerateBreadCrumbs(breadCrumbParts));
            page.template.Parse("L_GUESTBOOK", page.User.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination(core.Uri.BuildGuestBookUri(page.User, core.PrimitiveCache[userId]), p, (int)Math.Ceiling(comments / 10.0));
            page.User.ParseBreadCrumbs(breadCrumbParts);
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

            int p = core.Functions.RequestInt("p", 1);

            if (core.Session.IsLoggedIn)
            {
                if (page.Group.IsGroupMember(core.Session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Display.DisplayComments(page.template, page.Group, page.Group);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.ThisGroup), p, (int)Math.Ceiling(page.ThisGroup.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.ThisGroup.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.Group.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination(GuestBook.Uri(core, page.Group), p, (int)Math.Ceiling(page.Group.Comments / 10.0));
            page.Group.ParseBreadCrumbs("comments");
            //Prose.GetString("GuestBook");
        }

        public static void Show(Core core, NPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = core.Functions.RequestInt("p", 1);

            if (core.Session.IsLoggedIn)
            {
                if (page.Network.IsNetworkMember(core.Session.LoggedInMember))
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Display.DisplayComments(page.template, page.Network, page.Network);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.TheNetwork), p, (int)Math.Ceiling(page.TheNetwork.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.TheNetwork.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.Network.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination(GuestBook.Uri(core, page.Network), p, (int)Math.Ceiling(page.Network.Comments / 10.0));
            page.Network.ParseBreadCrumbs("comments");
        }

        public static void Show(Core core, APage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = core.Functions.RequestInt("p", 1);

            if (core.Session.IsLoggedIn)
            {
                page.template.Parse("CAN_COMMENT", "TRUE");
            }

            core.Display.DisplayComments(page.template, page.AnApplication, page.AnApplication);
            //page.template.Parse("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.AnApplication), p, (int)Math.Ceiling(page.AnApplication.Comments / 10.0)));
            //page.template.Parse("BREADCRUMBS", page.AnApplication.GenerateBreadCrumbs("comments"));
            page.template.Parse("L_GUESTBOOK", page.AnApplication.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination(GuestBook.Uri(core, page.AnApplication), p, (int)Math.Ceiling(page.AnApplication.Comments / 10.0));
            page.AnApplication.ParseBreadCrumbs("comments");
        }
    }
}
