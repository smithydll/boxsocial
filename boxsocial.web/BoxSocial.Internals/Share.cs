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
    [DataTable("shares")]
    public class Share : Item
    {
        [DataField("share_item", DataFieldKeys.Index, "i_share")]
        private ItemKey itemKey;
        [DataField("user_id", DataFieldKeys.Index, "i_share")]
        private long ownerId;
        [DataField("share_status_id")]
        private long statusId;
        [DataField("share_time_ut")]
        private long timeRaw;
        [DataField("share_ip", 55)]
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

        private Share(Core core, DataRow shareRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Share_ItemLoad);

            //
            // Because this class does not have an ID, it should only
            // be able to construct itself from raw data.
            //

            loadItemInfo(shareRow);
        }

        void Share_ItemLoad()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <remarks>ItemRated should implement a transaction.</remarks>
        public static void ShareItem(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (itemKey.Id < 1)
            {
                throw new InvalidItemException();
            }

            ItemInfo ii = null;

            try
            {
                ii = new ItemInfo(core, itemKey);
            }
            catch (InvalidIteminfoException)
            {
                ii = ItemInfo.Create(core, itemKey);
            }

            ii.IncrementSharedTimes();

            InsertQuery iQuery = new InsertQuery(typeof(Share));
            iQuery.AddField("share_item_id", itemKey.Id);
            iQuery.AddField("share_item_type_id", itemKey.TypeId);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("share_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("share_ip", core.Session.IPAddress.ToString());

            // commit the transaction
            core.Db.Query(iQuery);

            return;
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ItemSharedEventArgs : EventArgs
    {
        private ItemKey itemKey;
        private User sharer;

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

        public User Sharer
        {
            get
            {
                return sharer;
            }
        }

        public ItemSharedEventArgs(User sharer, ItemKey itemKey)
        {
            this.sharer = sharer;
            this.itemKey = itemKey;
        }
    }

    public class InvalidShareException : Exception
    {
    }
}
