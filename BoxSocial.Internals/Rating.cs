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
    }
}
