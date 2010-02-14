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
    [DataTable("ratings")]
    public class Rating : Item
    {
        /*[DataField("rate_item_id")]
        private long itemId;
        [DataField("rate_item_type", NAMESPACE)]
        private string itemType;*/
		[DataField("rate_item", DataFieldKeys.Index, "i_rating")]
        private ItemKey itemKey;
        [DataField("user_id", DataFieldKeys.Index, "i_rating")]
        private long ownerId;
        [DataField("rate_time_ut")]
        private long timeRaw;
        [DataField("rate_ip", 55)]
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
                return itemKey.Type;
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

        private Rating(Core core, DataRow ratingRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Rating_ItemLoad);

            //
            // Because this class does not have an ID, only it should
            // be able to construct itself from raw data.
            //

            loadItemInfo(ratingRow);
        }

        void Rating_ItemLoad()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="rating"></param>
        /// <remarks>ItemRated should implement a transaction.</remarks>
        public static void Vote(Core core, ItemKey itemKey, int rating)
        {

            if (itemKey.Id < 1)
            {
                throw new InvalidItemException();
            }

            if (rating < 1 || rating > 5)
            {
                throw new InvalidRatingException();
            }

            /* after 7 days release the IP for dynamics ip fairness */
            SelectQuery query = new SelectQuery("ratings r");
            query.AddFields("user_id");
            query.AddCondition("rate_item_id", itemKey.Id);
            query.AddCondition("rate_item_type_id", itemKey.TypeId);
            QueryCondition qc1 = query.AddCondition("user_id", core.LoggedInMemberId);
            QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, "rate_ip", core.Session.IPAddress.ToString());
            qc2.AddCondition("rate_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24 * 7);

            /*DataTable ratingsTable = db.Query(string.Format("SELECT user_id FROM ratings WHERE rate_item_id = {0} AND rate_item_type = '{1}' AND (user_id = {2} OR (rate_ip = '{3}' AND rate_time_ut > UNIX_TIMESTAMP() - (60 * 60 * 24 * 7)))",
                itemId, Mysql.Escape(itemType), loggedInMember.UserId, session.IPAddress.ToString()));*/

            DataTable ratingsTable = core.Db.Query(query);

            if (ratingsTable.Rows.Count > 0)
            {
                throw new AlreadyRatedException();
				return;
            }

            InsertQuery iQuery = new InsertQuery("ratings");
            iQuery.AddField("rate_item_id", itemKey.Id);
            iQuery.AddField("rate_item_type_id", itemKey.TypeId);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("rate_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("rate_rating", rating);
            iQuery.AddField("rate_ip", core.Session.IPAddress.ToString());

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

    public class ItemRatedEventArgs : EventArgs
    {
        private int rating;
        private ItemKey itemKey;
        private User rater;

        public int Rating
        {
            get
            {
                return rating;
            }
        }
		
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
                return itemKey.Type;
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

        public ItemRatedEventArgs(int rating, User rater, ItemKey itemKey)
        {
            this.rating = rating;
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class AlreadyRatedException : Exception
    {
    }

    public class InvalidRatingException : Exception
    {
    }
}
