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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class _default : TPage
    {
        public _default() : base("default.html")
        {
            this.Load += new EventHandler(Page_Load);

            template.Parse("IS_CONTENT", "FALSE");

            if (session.IsLoggedIn)
            {
                template.SetTemplate("today.html");
                this.Signature = PageSignature.today;

                if (!core.IsMobile)
                {
                    List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetApplications(core, core.Session.LoggedInMember);

                    foreach (ApplicationEntry ae in applications)
                    {
                        BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Member, ae);
                    }
                }

                template.Parse("DATE_STRING", tz.Now.ToLongDateString());

                ShowUnseenNotifications();
            }
        }

        private void ShowUnseenNotifications()
        {
            Template template = new Template(core.Http.TemplatePath, "notificationspanel.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            long notifications = Notification.GetUnseenNotificationCount(core);

            // If there are unseen notifications, show them
            if (notifications > 0)
            {
                core.AddSidePanel(template);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (session != null && session.IsLoggedIn)
            {
                core.InvokeHooks(new HookEventArgs(core, AppPrimitives.None, null));
                if (!core.IsMobile)
                {
                    core.InvokePostHooks(new HookEventArgs(core, AppPrimitives.Member, core.Session.LoggedInMember));
                }

                Feed.Show(core, this, core.Session.LoggedInMember);
            }
            else
            {
                ArrayList defaultImages = new ArrayList();
                defaultImages.Add("default_1.jpg");
                defaultImages.Add("default_2.jpg");
                defaultImages.Add("default_3.jpg");
                defaultImages.Add("default_4.jpg");
                defaultImages.Add("default_5.jpg");

                Random rand = new Random((int)(DateTime.Now.Second + DateTime.Now.Minute * 60 + DateTime.Now.Hour * 60 * 60));

                template.Parse("I_DEFAULT", (string)defaultImages[rand.Next(defaultImages.Count)]);

                if (loggedInMember != null)
                {
                    template.Parse("U_INVITE", core.Hyperlink.BuildAccountSubModuleUri("friends", "invite"));
                    template.Parse("U_WRITE_BLOG", core.Hyperlink.BuildAccountSubModuleUri("blog", "write"));
                }

                template.Parse("U_FORGOT_PASSWORD", core.Hyperlink.AppendSid("/sign-in/?mode=reset-password"));
            }
            EndResponse();
        }
    }
}
