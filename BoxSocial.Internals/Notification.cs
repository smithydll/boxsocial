﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Notification
    {
        public static string NOTIFICATION_FIELDS = "nt.notification_id, nt.notification_application, nt.notification_primitive_id, nt.notification_primitive_type, nt.notification_title, nt.notification_body, nt.notification_time_ut, nt.notification_read";

        private Mysql db;

        private long notificationId;
        private string title;
        private string body;
        private int applicationId;
        private Primitive owner;
        private long primitiveId;
        private long timeRaw;
        private bool read;

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

        public Notification(Mysql db, Primitive owner, DataRow notificationRow)
        {
            this.db = db;
            this.owner = owner;

            loadNotificationRow(notificationRow);
        }

        private Notification(Mysql db, Primitive owner, long notificationId, string subject, string body, long timeRaw, int applicationId)
        {
            this.db = db;
            this.owner = owner;

            this.notificationId = notificationId;
            this.applicationId = applicationId;
            this.title = subject;
            this.body = body;
            this.primitiveId = owner.Id;
            this.timeRaw = timeRaw;
            this.read = false;
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        private void loadNotificationRow(DataRow notificationRow)
        {
            notificationId = (long)notificationRow["notification_id"];
            applicationId = (int)notificationRow["notification_application"];
            title = (string)notificationRow["notification_title"];
            body = (string)notificationRow["notification_body"];
            primitiveId = (long)notificationRow["notification_primitive_id"];
            timeRaw = (long)notificationRow["notification_time_ut"];
            read = ((byte)notificationRow["notification_read"] > 0) ? true : false;
        }

        public void SetRead()
        {
            UpdateQuery uquery = new UpdateQuery("notifications");
        }

        public static Notification Create(Core core, ApplicationEntry application, Member receiver, string subject, string body)
        {
            int applicationId = 0;

            if (application != null)
            {
                // TODO: ensure only internals can call a null application
                applicationId = (int)application.Id;
            }

            InsertQuery iQuery = new InsertQuery("notifications");
            iQuery.AddField("notification_primitive_id", receiver.Id);
            iQuery.AddField("notification_primitive_type", "USER");
            iQuery.AddField("notification_title", subject);
            iQuery.AddField("notification_body", body);
            iQuery.AddField("notification_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("notification_read", false);
            iQuery.AddField("notification_application", applicationId);

            long notificationId = core.db.UpdateQuery(iQuery);

            return new Notification(core.db, receiver, notificationId, subject, body, UnixTime.UnixTimeStamp(), applicationId);
        }

        public static List<Notification> GetRecentNotifications(Core core)
        {
            List<Notification> notificationItems = new List<Notification>();

            SelectQuery query = new SelectQuery("notifications nt");
            query.AddFields(Notification.NOTIFICATION_FIELDS);
            query.AddCondition("nt.notification_read", false);
            query.AddCondition("nt.notification_primitive_id", core.LoggedInMemberId);
            query.AddCondition("nt.notification_primitive_type", "USER");
            query.AddSort(SortOrder.Descending, "nt.notification_time_ut");
            query.LimitCount = 64;

            DataTable notificationsTable = core.db.SelectQuery(query);

            foreach (DataRow dr in notificationsTable.Rows)
            {
                notificationItems.Add(new Notification(core.db, core.session.LoggedInMember, dr));
            }

            return notificationItems;
        }
    }
}