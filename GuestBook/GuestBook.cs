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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.GuestBook
{
    public class GuestBook
    {
        public static string Uri(Member member)
        {
            return Linker.AppendSid(string.Format("/{0}/profile/comments",
                member.UserName.ToLower()));
        }

        public static string Uri(UserGroup thisGroup)
        {
            return Linker.AppendSid(string.Format("/group/{0}/comments",
                thisGroup.Slug));
        }

        public static string Uri(Network theNetwork)
        {
            return Linker.AppendSid(string.Format("/network/{0}/comments",
                theNetwork.NetworkNetwork));
        }

        public static string Uri(ApplicationEntry anApplication)
        {
            return Linker.AppendSid(string.Format("/application/{0}/comments",
                anApplication.AssemblyName));
        }

        public static void Show(Core core, PPage page)
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
                    page.template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }
            Display.DisplayComments(page.template, page.ProfileOwner, page.ProfileOwner.UserId, "USER", (long)page.ProfileOwner.ProfileComments, false);
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(Linker.BuildGuestBookUri(page.ProfileOwner), p, (int)Math.Ceiling(page.ProfileOwner.ProfileComments / 10.0)));
            page.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(page.ProfileOwner.UserName, "profile/comments"));
            page.template.ParseVariables("L_GUESTBOOK", HttpUtility.HtmlEncode(page.ProfileOwner.DisplayNameOwnership + " Guest Book"));
        }

        public static void Show(Core core, GPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                if (page.ThisGroup.IsGroupMember(core.session.LoggedInMember))
                {
                    page.template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(page.template, page.ThisGroup, page.ThisGroup.GroupId, "GROUP", (long)page.ThisGroup.Comments, false);
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.ThisGroup), p, (int)Math.Ceiling(page.ThisGroup.Comments / 10.0)));
            page.template.ParseVariables("BREADCRUMBS", page.ThisGroup.GenerateBreadCrumbs("comments"));
            page.template.ParseVariables("L_GUESTBOOK", HttpUtility.HtmlEncode(page.ThisGroup.DisplayNameOwnership + " Guest Book"));
        }

        public static void Show(Core core, NPage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                if (page.TheNetwork.IsNetworkMember(core.session.LoggedInMember))
                {
                    page.template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            Display.DisplayComments(page.template, page.TheNetwork, page.TheNetwork.NetworkId, "NETWORK", (long)page.TheNetwork.Comments, false);
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.TheNetwork), p, (int)Math.Ceiling(page.TheNetwork.Comments / 10.0)));
            page.template.ParseVariables("BREADCRUMBS", page.TheNetwork.GenerateBreadCrumbs("comments"));
            page.template.ParseVariables("L_GUESTBOOK", HttpUtility.HtmlEncode(page.TheNetwork.DisplayNameOwnership + " Guest Book"));
        }

        public static void Show(Core core, APage page)
        {
            page.template.SetTemplate("GuestBook", "viewguestbook");

            int p = Functions.RequestInt("p", 1);

            if (core.session.IsLoggedIn)
            {
                page.template.ParseVariables("CAN_COMMENT", "TRUE");
            }

            Display.DisplayComments(page.template, page.AnApplication, page.AnApplication.ApplicationId, "APPLICATION", (long)page.AnApplication.Comments, false);
            page.template.ParseVariables("PAGINATION", Display.GeneratePagination(GuestBook.Uri(page.AnApplication), p, (int)Math.Ceiling(page.AnApplication.Comments / 10.0)));
            page.template.ParseVariables("BREADCRUMBS", page.AnApplication.GenerateBreadCrumbs("comments"));
            page.template.ParseVariables("L_GUESTBOOK", HttpUtility.HtmlEncode(page.AnApplication.DisplayNameOwnership + " Guest Book"));
        }
    }
}
