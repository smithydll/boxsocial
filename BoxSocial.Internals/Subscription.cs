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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("subscriptions")]
    public class Subscription : Item
    {
        [DataField("subscription_item", DataFieldKeys.Index, "i_subscription")]
        private ItemKey itemKey;
        [DataField("user_id", DataFieldKeys.Index, "i_subscription")]
        private long ownerId;
        [DataField("subscription_time_ut")]
        private long timeRaw;
        [DataField("subscription_ip", 55)]
        private string ip;

        private User owner;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long UserId
        {
            get
            {
                return ownerId;
            }
        }

        public long TimeRaw
        {
            get
            {
                return timeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        private Subscription(Core core, DataRow subscriptionRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Subscription_ItemLoad); 

            //
            // Because this class does not have an ID, it should only
            // be able to construct itself from raw data.
            //

            loadItemInfo(subscriptionRow);
        }

        void Subscription_ItemLoad()
        {

        }

        public static void SubscribeToItem(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }
        }

        public static void UnsubscribeFromItem(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
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

    public class ItemSubscribedEventArgs : EventArgs
    {
        private ItemKey itemKey;
        private User rater;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Rater
        {
            get
            {
                return rater;
            }
        }

        public ItemSubscribedEventArgs(User rater, ItemKey itemKey)
        {
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class ItemUnsubscribedEventArgs : EventArgs
    {
        private ItemKey itemKey;
        private User rater;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Rater
        {
            get
            {
                return rater;
            }
        }

        public ItemUnsubscribedEventArgs(User rater, ItemKey itemKey)
        {
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class AlreadySubscribedException : Exception
    {
    }

    public class InvalidSubscriptionException : Exception
    {
    }
}
