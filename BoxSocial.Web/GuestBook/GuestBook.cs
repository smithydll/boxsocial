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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;
using BoxSocial.Musician;

namespace BoxSocial.Applications.GuestBook
{
    public class GuestBook
    {

        public static string Uri(Core core, User member)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}profile/comments",
                member.UriStub));
        }

        public static string Uri(Core core, UserGroup thisGroup)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}comments",
                thisGroup.UriStub));
        }

        public static string Uri(Core core, Network theNetwork)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}comments",
                theNetwork.UriStub));
        }

        public static string Uri(Core core, ApplicationEntry anApplication)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}comments",
                anApplication.UriStub));
        }

        public static string Uri(Core core, Musician.Musician musician)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}comments",
                musician.UriStub));
        }

        public static void Show(Core core, UPage page)
        {
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            page.User.LoadProfileInfo();

            if (!page.User.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            /* pages */
            core.Display.ParsePageList(page.Owner, true);

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            if (core.Session.IsLoggedIn)
            {
                if (page.User.Access.Can("COMMENT"))
                {
                    core.Template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Template.Parse("IS_USER_GUESTBOOK", "TRUE");

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "profile", core.Prose.GetString("PROFILE") });
            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("GUEST_BOOK") });

            core.Display.DisplayComments(core.Template, page.User, page.User, UserGuestBookHook);
            core.Display.ParsePagination("COMMENT_PAGINATION", core.Hyperlink.BuildGuestBookUri(page.User), 10, page.User.Comments);
            page.User.ParseBreadCrumbs(breadCrumbParts);
        }

        // TODO: use user
        public static void Show(Core core, UPage page, string user)
        {
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            page.User.LoadProfileInfo();

            if (!page.User.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            /* pages */
            core.Display.ParsePageList(page.Owner, true);

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            if (core.Session.IsLoggedIn)
            {
                if (page.User.Access.Can("COMMENT"))
                {
                    core.Template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Template.Parse("IS_USER_GUESTBOOK", "TRUE");

            long userId = core.LoadUserProfile(user);

            List<User> commenters = new List<User>();
            commenters.Add(page.User);
            commenters.Add(core.PrimitiveCache[userId]);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "profile", core.Prose.GetString("PROFILE") });
            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("COMMENTS") });
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

            core.Display.DisplayComments(core.Template, page.User, page.User, commenters, comments, UserGuestBookHook);

            core.Display.ParsePagination("COMMENT_PAGINATION", core.Hyperlink.BuildGuestBookUri(page.User, core.PrimitiveCache[userId]), 10, comments);
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
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            if (core.Session.IsLoggedIn)
            {
                if (page.Group.IsGroupMember(core.Session.LoggedInMember.ItemKey))
                {
                    core.Template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            /* Pages */
            core.Display.ParsePageList(page.Owner, true);

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            core.Display.DisplayComments(core.Template, page.Group, page.Group);
            core.Display.ParsePagination("COMMENT_PAGINATION", GuestBook.Uri(core, page.Group), 10, page.Group.Comments);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("GUEST_BOOK") });

            page.Group.ParseBreadCrumbs(breadCrumbParts);
            //Prose.GetString("GuestBook");
        }

        public static void Show(Core core, NPage page)
        {
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            if (core.Session.IsLoggedIn)
            {
                if (page.Network.IsNetworkMember(core.Session.LoggedInMember.ItemKey))
                {
                    core.Template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            core.Display.DisplayComments(core.Template, page.Network, page.Network);
            core.Display.ParsePagination("COMMENT_PAGINATION", GuestBook.Uri(core, page.Network), 10,page.Network.Comments);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("GUEST_BOOK") });

            page.Network.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void Show(Core core, APage page)
        {
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            if (core.Session.IsLoggedIn)
            {
                core.Template.Parse("CAN_COMMENT", "TRUE");
            }

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            core.Display.DisplayComments(core.Template, page.AnApplication, page.AnApplication);
            core.Template.Parse("L_GUESTBOOK", page.AnApplication.DisplayNameOwnership + " Guest Book");
            core.Display.ParsePagination("COMMENT_PAGINATION", GuestBook.Uri(core, page.AnApplication), 10, page.AnApplication.Comments);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("GUEST_BOOK") });

            page.AnApplication.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void Show(Core core, MPage page)
        {
            core.Template.SetTemplate("GuestBook", "viewguestbook");

            if (core.Session.IsLoggedIn)
            {
                if (page.Musician.Access.Can("COMMENT"))
                {
                    core.Template.Parse("CAN_COMMENT", "TRUE");
                }
            }

            core.Template.Parse("PAGE_TITLE", string.Format(core.Prose.GetString("USERS_GUEST_BOOK"), page.Owner.DisplayNameOwnership));

            core.Display.DisplayComments(core.Template, page.Musician, page.Musician);
            core.Display.ParsePagination("COMMENT_PAGINATION", GuestBook.Uri(core, page.Musician), 10, page.Musician.Comments);


            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "comments", core.Prose.GetString("GUEST_BOOK") });

            page.Musician.ParseBreadCrumbs(breadCrumbParts);

            //Prose.GetString("GuestBook");
        }
    }
}
