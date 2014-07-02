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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("notifications")]
    public class Notification : NumberedItem
    {
        public const int NOTIFICATION_MAX_BODY = 511;

        [DataField("notification_id", DataFieldKeys.Primary)]
        private long notificationId;
        [DataField("notification_title", 63)]
        private string title;
        [DataField("notification_body", NOTIFICATION_MAX_BODY)]
        private string body;
        [DataField("notification_application", typeof(ApplicationEntry))]
        private long applicationId;
        /*[DataField("notification_primitive_id")]
        private long primitiveId;
        [DataField("notification_primitive_type", NAMESPACE)]
        private string primitiveType;*/
        [DataField("notification_primitive", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("notification_item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("notification_time_ut")]
        private long timeRaw;
        [DataField("notification_read")]
        private bool read;
        [DataField("notification_seen")]
        private bool seen;

        private Primitive owner;

        public long NotificationId
        {
            get
            {
                return notificationId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public bool IsRead
        {
            get
            {
                return read;
            }
        }

        public bool IsSeen
        {
            get
            {
                return seen;
            }
        }

        public Notification(Core core, Primitive owner, DataRow notificationRow)
            : base(core)
        {
            this.owner = owner;

            try
            {
                loadItemInfo(notificationRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNotificationException();
            }
        }

        private Notification(Core core, Primitive owner, long notificationId, string subject, string body, long timeRaw, int applicationId)
            : base(core)
        {
            this.db = db;
            this.owner = owner;

            this.notificationId = notificationId;
            this.applicationId = applicationId;
            this.title = subject;
            this.body = body;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            this.timeRaw = timeRaw;
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public void SetRead()
        {
            UpdateQuery uquery = new UpdateQuery("notifications");
        }

        internal static Notification Create(Core core, User receiver, ItemKey itemKey, string subject, string body)
        {
            return Create(core, null, receiver, itemKey, subject, body);
        }

        public static Notification Create(Core core, ApplicationEntry application, User receiver, ItemKey itemKey, string subject, string body)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            int applicationId = 0;

            if (application != null)
            {
                // TODO: ensure only internals can call a null application
                applicationId = (int)application.Id;
            }

            InsertQuery iQuery = new InsertQuery("notifications");
            iQuery.AddField("notification_primitive_id", receiver.Id);
            iQuery.AddField("notification_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
            if (itemKey != null)
            {
                iQuery.AddField("notification_item_id", itemKey.Id);
                iQuery.AddField("notification_item_type_id", itemKey.TypeId);
            }
            iQuery.AddField("notification_title", subject);
            iQuery.AddField("notification_body", body);
            iQuery.AddField("notification_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("notification_read", false);
            iQuery.AddField("notification_seen", false);
            iQuery.AddField("notification_application", applicationId);

            long notificationId = core.Db.Query(iQuery);

            UpdateQuery query = new UpdateQuery(typeof(UserInfo));
            query.AddField("user_unread_notifications", new QueryOperation("user_unread_notifications", QueryOperations.Addition, 1));
            query.AddCondition("user_id", receiver.Id);

            core.Db.Query(query);

            return new Notification(core, receiver, notificationId, subject, body, UnixTime.UnixTimeStamp(), applicationId);
        }

        public static List<Notification> GetRecentNotifications(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Notification> notificationItems = new List<Notification>();

            SelectQuery query = Notification.GetSelectQueryStub(typeof(Notification));
            query.AddCondition("notification_read", false);
            query.AddCondition("notification_primitive_id", core.LoggedInMemberId);
            query.AddCondition("notification_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
            query.AddCondition("notification_time_ut", ConditionEquality.GreaterThanEqual, UnixTime.UnixTimeStamp(DateTime.UtcNow.AddDays(-30)));
            query.AddSort(SortOrder.Descending, "notification_time_ut");
            query.LimitCount = 128;

            DataTable notificationsTable = core.Db.Query(query);

            foreach (DataRow dr in notificationsTable.Rows)
            {
                notificationItems.Add(new Notification(core, core.Session.LoggedInMember, dr));
            }

            return notificationItems;
        }

        public static long GetUnseenNotificationCount(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Notification> notificationItems = new List<Notification>();

            SelectQuery query = new SelectQuery(GetTable(typeof(Notification)));
            query.AddFields("COUNT(*) as total");
            query.AddCondition("notification_read", false);
            query.AddCondition("notification_seen", false);
            query.AddCondition("notification_primitive_id", core.LoggedInMemberId);
            query.AddCondition("notification_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
            query.AddSort(SortOrder.Descending, "notification_time_ut");

            DataTable notificationsTable = core.Db.Query(query);

            return (long)notificationsTable.Rows[0]["total"];
        }

        public override long Id
        {
            get
            {
                return notificationId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidNotificationException : Exception
    {
    }
}
