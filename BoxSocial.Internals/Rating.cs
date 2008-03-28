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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Rating
    {
        public const string RATING_INFO_FIELDS = "r.rate_item_id, r.rate_item_type, r.user_id, r.rate_time_ut, r.rate_rating, r.rate_ip";

        private Core core;
        private Mysql db;

        private long itemId;
        private string itemType;
        private long ownerId;
        private Member owner;
        private long timeRaw;
        private string ip;

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public string ItemType
        {
            get
            {
                return itemType;
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
        {
            //
            // Because this class does not have an ID, only it should
            // be able to construct itself from raw data.
            //
        }

        private static void Vote(Core core, string itemType, long itemId, int rating)
        {

            if (rating < 1 || rating > 5)
            {
                throw new InvalidRatingException();
            }

            SelectQuery query = new SelectQuery("ratings r");
            query.AddFields("user_id");
            query.AddCondition("rate_item_id", itemId);
            query.AddCondition("rate_time_type", itemType);
            QueryCondition qc1 = query.AddCondition("user_id", core.LoggedInMemberId);
            QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, "rate_ip", core.session.IPAddress.ToString());
            qc2.AddCondition("rate_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24 * 7);

            /*DataTable ratingsTable = db.SelectQuery(string.Format("SELECT user_id FROM ratings WHERE rate_item_id = {0} AND rate_item_type = '{1}' AND (user_id = {2} OR (rate_ip = '{3}' AND rate_time_ut > UNIX_TIMESTAMP() - (60 * 60 * 24 * 7)))",
                itemId, Mysql.Escape(itemType), loggedInMember.UserId, session.IPAddress.ToString()));*/

            DataTable ratingsTable = core.db.SelectQuery(query);

            if (ratingsTable.Rows.Count > 0)
            {
                throw new AlreadyRatedException();
            }

            InsertQuery iQuery = new InsertQuery("ratings");
            iQuery.AddField("rate_item_id", itemId);
            iQuery.AddField("rate_item_type", itemType);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("rate_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("rate_rating", rating);
            iQuery.AddField("rate_ip", core.session.IPAddress.ToString());

            core.db.UpdateQuery(iQuery, true);

            return;
        }
    }

    public class AlreadyRatedException : Exception
    {
    }

    public class InvalidRatingException : Exception
    {
    }
}
