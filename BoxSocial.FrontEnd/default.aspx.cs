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
            Template template = new Template("statusmessagespanel.html");

            /* My status */
            StatusMessage myStatusMessage = StatusFeed.GetLatest(core, loggedInMember);

            if (myStatusMessage != null)
            {
                template.Parse("USER_DISPLAY_NAME", loggedInMember.DisplayName);
                template.Parse("STATUS_MESSAGE", myStatusMessage.Message);
                template.Parse("STATUS_UPDATED", core.tz.DateTimeToString(myStatusMessage.GetTime(core.tz)));
            }

            /* Friends status */
            List<StatusMessage> statusMessages = StatusFeed.GetFriendItems(core, loggedInMember, 3);

            foreach (StatusMessage statusMessage in statusMessages)
            {
                VariableCollection statusMessagesVariableCollection = template.CreateChild("status_messages");

                statusMessagesVariableCollection.Parse("USER_DISPLAY_NAME", statusMessage.Owner.DisplayName);
                statusMessagesVariableCollection.Parse("USER_NAME", statusMessage.Owner.Key);
                statusMessagesVariableCollection.Parse("STATUS_MESSAGE", statusMessage.Message);
                statusMessagesVariableCollection.Parse("STATUS_UPDATED", core.tz.DateTimeToString(statusMessage.GetTime(core.tz)));
            }

            core.AddSidePanel(template);
        }

        private void ShowUnseenNotifications()
        {
            Template template = new Template("notificationspanel.html");

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

                Feed.Show(core, this, core.session.LoggedInMember);
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

                // new_points

                /*DataTable newUsers = db.Query(string.Format("SELECT {0}, {1}, {2} FROM user_info ui INNER JOIN user_profile up ON ui.user_id = up.user_id LEFT JOIN (countries c, gallery_items gi) ON (c.country_iso = up.profile_country AND gi.gallery_item_id = ui.user_icon) WHERE up.profile_access & 4369 = 4369 AND gi.gallery_item_uri IS NOT NULL ORDER BY user_reg_date_ut DESC LIMIT 3",
                    BoxSocial.Internals.User.USER_PROFILE_FIELDS, BoxSocial.Internals.User.USER_INFO_FIELDS, BoxSocial.Internals.User.USER_ICON_FIELDS));

                for (int i = 0; i < newUsers.Rows.Count; i++)
                {
                    User newMember = new User(core, newUsers.Rows[i], UserLoadOptions.All);

                    VariableCollection newPointsVariableCollection = template.CreateChild("new_points");

                    newPointsVariableCollection.Parse("USER_DISPLAY_NAME", newMember.DisplayName));
                    newPointsVariableCollection.Parse("USER_AGE", newMember.AgeString));
                    newPointsVariableCollection.Parse("USER_COUNTRY", newMember.Country));
                    newPointsVariableCollection.Parse("USER_CAPTION", "");
                    newPointsVariableCollection.Parse("U_PROFILE", Linker.BuildHomepageUri(newMember)));
                    newPointsVariableCollection.Parse("ICON", newMember.UserIcon));
                }*/
            }
            EndResponse();
        }
    }
}
