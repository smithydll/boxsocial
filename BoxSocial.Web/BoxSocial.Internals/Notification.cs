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
using System.Security.Cryptography;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum NotificationType : byte
    {
        None = 0x00,
        App = 0x01,
        Email = 0x02,
        Sms = 0x04,
    }

    [DataTable("notifications")]
    public class Notification : NumberedItem
    {
        public const int NOTIFICATION_MAX_BODY = 511;

        [DataField("notification_id", DataFieldKeys.Primary)]
        private long notificationId;
        // TODO: remove
        [DataField("notification_title", NOTIFICATION_MAX_BODY)] // Legacy
        private string title;
        [DataField("notification_body", NOTIFICATION_MAX_BODY)] // Legacy
        private string body;
        // end remove
        [DataField("notification_application", typeof(ApplicationEntry))]
        private long applicationId;
        [DataField("notification_primitive", DataFieldKeys.Index)] // recipient
        private ItemKey ownerKey;
        [DataField("notification_item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("notification_item_owner")]
        private ItemKey itemOwnerKey;
        [DataField("notification_user_id")]
        private long userId;
        [DataField("notification_user_count")]
        private int userCount;
        [DataField("notification_time_ut")]
        private long timeRaw;
        [DataField("notification_read")]
        private bool read;
        [DataField("notification_seen")]
        private bool seen;
        [DataField("notification_verb", 31)]
        private string verb;
        [DataField("notification_action", 31)]
        private string action;
        [DataField("notification_url", 255)]
        private string url;
        [DataField("notification_verification_string", 32)]
        private string verificationString;

        private Primitive recipient;
        private User user;
        private INotifiableItem item;

        public INotifiableItem NotifiedItem
        {
            get
            {
                if (item == null)
                {
                    core.ItemCache.RequestItem(itemKey);
                    try
                    {
                        item = (INotifiableItem)core.ItemCache[itemKey];
                    }
                    catch
                    {
                        try
                        {
                            item = (INotifiableItem)NumberedItem.Reflect(core, itemKey);
                            HttpContext.Current.Response.Write("<br />Fallback, had to reflect: " + itemKey.ToString());
                        }
                        catch
                        {
                            item = null;
                        }
                    }
                }
                return item;
            }
        }

        // temp
        private NumberedItem nitem;
        private NumberedItem notifiedItem
        {
            get
            {
                if (nitem == null)
                {
                    core.ItemCache.RequestItem(itemKey);
                    try
                    {
                        nitem = (NumberedItem)core.ItemCache[itemKey];
                    }
                    catch
                    {
                        try
                        {
                            nitem = (NumberedItem)NumberedItem.Reflect(core, itemKey);
                            HttpContext.Current.Response.Write("<br />Fallback, had to reflect: " + itemKey.ToString());
                        }
                        catch
                        {
                            nitem = null;
                        }
                    }
                }
                return nitem;
            }
        }

        public string NotificationString
        {
            get
            {
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }

                // Force the prose to load
                string itemTitle = string.Empty;
                if (itemKey.Id > 0)
                {
                    NumberedItem item = notifiedItem;

                    if (item is INotifiableItem)
                    {
                        INotifiableItem nitem = (INotifiableItem)item;

                        itemTitle = nitem.Title;
                    }
                }

                string fragment = core.Prose.GetString(verb);
                if (itemOwnerKey.Id > 0)
                {
                    if (itemOwnerKey.TypeId == ItemType.GetTypeId(typeof(User)))
                    {
                        if (itemOwnerKey.Id == ownerKey.Id)
                        {
                            fragment = string.Format(fragment, core.Prose.GetString("_YOUR"), itemTitle);
                        }
                        else
                        {
                            fragment = string.Format(fragment, string.Format("[user ownership=true link=false]{0}[/user]", itemOwnerKey.Id), itemTitle);
                        }
                    }
                    else
                    {
                        core.PrimitiveCache.LoadPrimitiveProfile(itemOwnerKey);
                        try
                        {
                            Primitive primitive = core.PrimitiveCache[itemOwnerKey];

                            fragment = string.Format(fragment, primitive.DisplayNameOwnership, itemTitle);
                        }
                        catch
                        {
                            // the code should NEVER fall back to this
                            fragment = string.Format(fragment, core.Prose.GetString("_A"), itemTitle);
                        }
                    }
                }
                else
                {
                    fragment = string.Format(fragment, string.Empty, itemTitle);
                }

                if (userCount <= 1)
                {
                    return string.Format(core.Prose.GetString("_NOTIFICATION_PHRASE"),
                        string.Format("[user]{0}[/user]", userId),
                        string.Format("[iurl=\"{1}\"]{0}[/iurl]", fragment, url));
                }
                else if (userCount == 2)
                {
                    return string.Format(core.Prose.GetString("_NOTIFICATION_PHRASE_2"),
                        string.Format("[user]{0}[/user]", userId),
                        string.Format("[iurl=\"{1}\"]{0}[/iurl]", fragment, url),
                        userCount - 1);
                }
                else
                {
                    return string.Format(core.Prose.GetString("_NOTIFICATION_PHRASE_3"),
                        string.Format("[user]{0}[/user]", userId),
                        string.Format("[iurl=\"{1}\"]{0}[/iurl]", fragment, url),
                        userCount - 1);
                }
            }
        }

        public long NotificationId
        {
            get
            {
                return notificationId;
            }
        }

        public User User
        {
            get
            {
                if (user == null || userId != user.Id)
                {
                    core.LoadUserProfile(userId);
                    user = core.PrimitiveCache[userId];
                }
                return user;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (recipient == null || ownerKey.Id != recipient.Id || ownerKey.TypeId != recipient.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    recipient = core.PrimitiveCache[ownerKey];
                    return recipient;
                }
                else
                {
                    return recipient;
                }
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

        public string Action
        {
            get
            {
                return action;
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

        public ItemKey NotificationItemKey
        {
            get
            {
                return itemKey;
            }
        }


        public string VerificationString
        {
            get
            {
                return verificationString;
            }
        }

        public Notification(Core core, Primitive owner, DataRow notificationRow)
            : base(core)
        {
            this.recipient = owner;

            try
            {
                loadItemInfo(notificationRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNotificationException();
            }
        }

        public Notification(Core core, Primitive owner, System.Data.Common.DbDataReader notificationRow)
            : base(core)
        {
            this.recipient = owner;

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
            this.recipient = owner;

            this.notificationId = notificationId;
            this.applicationId = applicationId;
            this.title = subject;
            this.body = body;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            this.timeRaw = timeRaw;
        }

        protected override void loadItemInfo(DataRow inviteRow)
        {
            loadValue(inviteRow, "notification_id", out notificationId);
            loadValue(inviteRow, "notification_title", out title);
            loadValue(inviteRow, "notification_body", out body);
            loadValue(inviteRow, "notification_application", out applicationId);
            loadValue(inviteRow, "notification_primitive", out ownerKey);
            loadValue(inviteRow, "notification_item", out itemKey);
            loadValue(inviteRow, "notification_item_owner", out itemOwnerKey);
            loadValue(inviteRow, "notification_user_id", out userId);
            loadValue(inviteRow, "notification_user_count", out userCount);
            loadValue(inviteRow, "notification_time_ut", out timeRaw);
            loadValue(inviteRow, "notification_read", out read);
            loadValue(inviteRow, "notification_seen", out seen);
            loadValue(inviteRow, "notification_verb", out verb);
            loadValue(inviteRow, "notification_action", out action);
            loadValue(inviteRow, "notification_url", out url);
            loadValue(inviteRow, "notification_verification_string", out verificationString);

            itemLoaded(inviteRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader inviteRow)
        {
            loadValue(inviteRow, "notification_id", out notificationId);
            loadValue(inviteRow, "notification_title", out title);
            loadValue(inviteRow, "notification_body", out body);
            loadValue(inviteRow, "notification_application", out applicationId);
            loadValue(inviteRow, "notification_primitive", out ownerKey);
            loadValue(inviteRow, "notification_item", out itemKey);
            loadValue(inviteRow, "notification_item_owner", out itemOwnerKey);
            loadValue(inviteRow, "notification_user_id", out userId);
            loadValue(inviteRow, "notification_user_count", out userCount);
            loadValue(inviteRow, "notification_time_ut", out timeRaw);
            loadValue(inviteRow, "notification_read", out read);
            loadValue(inviteRow, "notification_seen", out seen);
            loadValue(inviteRow, "notification_verb", out verb);
            loadValue(inviteRow, "notification_action", out action);
            loadValue(inviteRow, "notification_url", out url);
            loadValue(inviteRow, "notification_verification_string", out verificationString);

            itemLoaded(inviteRow);
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

            Notification notification = new Notification(core, receiver, notificationId, subject, body, UnixTime.UnixTimeStamp(), applicationId);



            return notification;
        }

        public static Notification Create(Core core, ApplicationEntry application, User actionBy, User receiver, ItemKey itemOwnerKey, ItemKey itemKey, string verb, string url, string action)
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

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] randomNumber = new byte[16];
            rng.GetBytes(randomNumber);

            string rand = SessionState.HexRNG(randomNumber);
            string verificationString = SessionState.SessionMd5(rand + "bsseed" + DateTime.Now.Ticks.ToString() + core.Session.IPAddress.ToString()).ToLower();

            InsertQuery iQuery = new InsertQuery("notifications");
            iQuery.AddField("notification_primitive_id", receiver.Id);
            iQuery.AddField("notification_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
            if (itemKey != null)
            {
                iQuery.AddField("notification_item_id", itemKey.Id);
                iQuery.AddField("notification_item_type_id", itemKey.TypeId);
            }
            if (itemOwnerKey != null)
            {
                iQuery.AddField("notification_item_owner_id", itemOwnerKey.Id);
                iQuery.AddField("notification_item_owner_type_id", itemOwnerKey.TypeId);
            }
            iQuery.AddField("notification_user_id", actionBy.Id);
            iQuery.AddField("notification_user_count", 1);
            iQuery.AddField("notification_verb", verb);
            iQuery.AddField("notification_action", action);
            iQuery.AddField("notification_url", url);
            iQuery.AddField("notification_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("notification_read", false);
            iQuery.AddField("notification_seen", false);
            iQuery.AddField("notification_application", applicationId);
            iQuery.AddField("notification_verification_string", verificationString);

            long notificationId = core.Db.Query(iQuery);

            core.Db.BeginTransaction();
            UpdateQuery query = new UpdateQuery(typeof(UserInfo));
            query.AddField("user_unread_notifications", new QueryOperation("user_unread_notifications", QueryOperations.Addition, 1));
            query.AddCondition("user_id", receiver.Id);

            core.Db.Query(query);

            Notification notification = new Notification(core, receiver, notificationId, string.Empty, string.Empty, UnixTime.UnixTimeStamp(), applicationId);
            // this is not elegant
            // TODO: write appropriate constructor
            notification.userId = actionBy.Id;
            notification.verb = verb;
            notification.action = action;
            notification.url = url;
            notification.itemKey = itemKey;
            notification.itemOwnerKey = itemOwnerKey;
            notification.verificationString = verificationString;

            return notification;
        }

        public static List<Notification> GetRecentNotifications(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Notification> notificationItems = new List<Notification>();

            SelectQuery query = Notification.GetSelectQueryStub(core, typeof(Notification));
            query.AddCondition("notification_read", false);
            query.AddCondition("notification_primitive_id", core.LoggedInMemberId);
            query.AddCondition("notification_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
            query.AddCondition("notification_time_ut", ConditionEquality.GreaterThanEqual, UnixTime.UnixTimeStamp(DateTime.UtcNow.AddDays(-30)));
            query.AddSort(SortOrder.Descending, "notification_time_ut");
            query.LimitCount = 128;

            System.Data.Common.DbDataReader notificationsReader = core.Db.ReaderQuery(query);

            while(notificationsReader.Read())
            {
                notificationItems.Add(new Notification(core, core.Session.LoggedInMember, notificationsReader));
            }

            notificationsReader.Close();
            notificationsReader.Dispose();

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

            System.Data.Common.DbDataReader notificationsReader = core.Db.ReaderQuery(query);

            long newNotifications = 0;

            if (notificationsReader.HasRows)
            {
                notificationsReader.Read();

                newNotifications = (long)notificationsReader["total"];
            }

            notificationsReader.Close();
            notificationsReader.Dispose();

            return newNotifications;
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
