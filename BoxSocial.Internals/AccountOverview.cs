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

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "overview", true)]
    public class AccountOverview : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("OVERVIEW");
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountOverview class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountOverview(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountOverview_Load);
            this.Show += new EventHandler(AccountOverview_Show);
        }

        void AccountOverview_Load(object sender, EventArgs e)
        {
        }

        void AccountOverview_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_landing.html");

            List<Notification> notifications = Notification.GetRecentNotifications(core);
            List<long> ids = new List<long>();

            template.Parse("USAGE_USAGE", Functions.BytesToString(core.Session.LoggedInMember.UserInfo.BytesUsed));
            template.Parse("USAGE_LIMIT", Functions.BytesToString(core.Settings.MaxUserStorage));

            if (notifications.Count > 0)
            {
                template.Parse("IS_NOTIFICATIONS", "TRUE");
            }

            if (LoggedInMember.UserInfo.UnreadNotifications > 0)
            {
                UpdateQuery query = new UpdateQuery(typeof(UserInfo));
                query.AddField("user_unread_notifications", new QueryOperation("user_unread_notifications", QueryOperations.Subtraction, LoggedInMember.UserInfo.UnreadNotifications));
                query.AddCondition("user_id", LoggedInMember.Id);

                db.Query(query);

                core.Template.Parse("UNREAD_NOTIFICATIONS", 0);
            }

            core.LoadUserProfile(core.LoggedInMemberId);
            foreach (Notification notification in notifications)
            {
                VariableCollection notificationVariableCollection = template.CreateChild("notifications_list");

                notificationVariableCollection.Parse("DATE", tz.DateTimeToString(notification.GetTime(tz)));
                core.Display.ParseBbcode(notificationVariableCollection, "ACTION", notification.NotificationString, true, null, null);
                core.Display.ParseBbcode(notificationVariableCollection, "DESCRIPTION", notification.Body, core.PrimitiveCache[core.LoggedInMemberId], true, null, null);

                if (notification.NotificationItemKey.ImplementsNotifiable)
                {
                    Dictionary<string, string> actions = notification.NotifiedItem.GetNotificationActions(notification.Action);

                    foreach (string a in actions.Keys)
                    {
                        VariableCollection actionsVariableCollection = notificationVariableCollection.CreateChild("actions_list");

                        actionsVariableCollection.Parse("ACTION", actions[a]);
                        actionsVariableCollection.Parse("U_ACTION", notification.NotifiedItem.GetNotificationActionUrl(a));
                    }
                }

                if (notification.IsSeen)
                {
                    notificationVariableCollection.Parse("SEEN", "TRUE");
                }

                ids.Add(notification.NotificationId);
            }

            if (ids.Count > 0)
            {
                UpdateQuery uQuery = new UpdateQuery("notifications");
                uQuery.AddField("notification_seen", true);
                uQuery.AddCondition("notification_id", ConditionEquality.In, ids);

                db.Query(uQuery);
            }
        }
    }
}
