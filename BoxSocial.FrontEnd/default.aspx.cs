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
            template.Parse("IS_CONTENT", "FALSE");

            if (session.IsLoggedIn)
            {
                template.SetTemplate("today.html");
                this.Signature = PageSignature.today;

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Member, new ApplicationEntry(core, session.LoggedInMember, "Calendar"));

                template.Parse("DATE_STRING", tz.Now.ToLongDateString());

                ShowUnseenNotifications();
                ShowStatusUpdates();
            }
        }

        private void ShowStatusUpdates()
        {
            Template template = new Template(core.Http.TemplatePath, "statusmessagespanel.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            /* My status */
            StatusMessage myStatusMessage = StatusFeed.GetLatest(core, loggedInMember);

            if (myStatusMessage != null)
            {
                template.Parse("USER_DISPLAY_NAME", loggedInMember.DisplayName);
                template.Parse("STATUS_MESSAGE", myStatusMessage.Message);
                template.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(myStatusMessage.GetTime(core.Tz)));
            }

            /* Friends status */
            List<StatusMessage> statusMessages = StatusFeed.GetFriendItems(core, loggedInMember, 3);

            foreach (StatusMessage statusMessage in statusMessages)
            {
                VariableCollection statusMessagesVariableCollection = template.CreateChild("status_messages");

                statusMessagesVariableCollection.Parse("USER_DISPLAY_NAME", statusMessage.Owner.DisplayName);
                statusMessagesVariableCollection.Parse("USER_NAME", statusMessage.Owner.Key);
                statusMessagesVariableCollection.Parse("STATUS_MESSAGE", statusMessage.Message);
                statusMessagesVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(statusMessage.GetTime(core.Tz)));
            }

            core.AddSidePanel(template);
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
                    template.Parse("U_INVITE", core.Uri.BuildAccountSubModuleUri("friends", "invite"));
                    template.Parse("U_WRITE_BLOG", core.Uri.BuildAccountSubModuleUri("blog", "write"));
                }

                template.Parse("U_FORGOT_PASSWORD", core.Uri.AppendSid("/sign-in/?mode=reset-password"));
            }
            EndResponse();
        }
    }
}
