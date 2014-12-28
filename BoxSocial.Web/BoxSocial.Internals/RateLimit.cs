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
    [DataTable("rate_limit")]
    public class RateLimit : NumberedItem
    {
        [DataField("rate_limit_id", DataFieldKeys.Primary)]
        private long rateLimitId;
        [DataField("user_id", typeof(User))]
        private long userId;
        [DataField("application_id", typeof(ApplicationEntry))]
        private long applicationId;
        [DataField("rate_limit_function", 32)]
        private string rateLimitFunction;
        [DataField("rate_limit_count")]
        private int rateLimitCount;
        [DataField("rate_limit_ip", 50)]
        private string rateLimitIp;
        [DataField("rate_limit_created_ut")]
        private long rateLimitCreated;
        [DataField("rate_limit_expires_ut")]
        private long rateLimitExpires;

        public string Function
        {
            get
            {
                return rateLimitFunction;
            }
        }

        public int Count
        {
            get
            {
                return rateLimitCount;
            }
        }

        public RateLimit(Core core, long rateLimitId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(RateLimit_ItemLoad);

            try
            {
                LoadItem(rateLimitId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidRateLimitException();
            }
        }

        public RateLimit(Core core, System.Data.Common.DbDataReader rateLimitRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(RateLimit_ItemLoad);

            loadItemInfo(rateLimitRow);
        }

        private void RateLimit_ItemLoad()
        {
        }

        protected override void loadItemInfo(DataRow rateLimitRow)
        {
            loadValue(rateLimitRow, "rate_limit_id", out rateLimitId);
            loadValue(rateLimitRow, "user_id", out userId);
            loadValue(rateLimitRow, "application_id", out applicationId);
            loadValue(rateLimitRow, "rate_limit_function", out rateLimitFunction);
            loadValue(rateLimitRow, "rate_limit_count", out rateLimitCount);
            loadValue(rateLimitRow, "rate_limit_ip", out rateLimitIp);
            loadValue(rateLimitRow, "rate_limit_created_ut", out rateLimitCreated);
            loadValue(rateLimitRow, "rate_limit_expires_ut", out rateLimitExpires);

            itemLoaded(rateLimitRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader rateLimitRow)
        {
            loadValue(rateLimitRow, "rate_limit_id", out rateLimitId);
            loadValue(rateLimitRow, "user_id", out userId);
            loadValue(rateLimitRow, "application_id", out applicationId);
            loadValue(rateLimitRow, "rate_limit_function", out rateLimitFunction);
            loadValue(rateLimitRow, "rate_limit_count", out rateLimitCount);
            loadValue(rateLimitRow, "rate_limit_ip", out rateLimitIp);
            loadValue(rateLimitRow, "rate_limit_created_ut", out rateLimitCreated);
            loadValue(rateLimitRow, "rate_limit_expires_ut", out rateLimitExpires);

            itemLoaded(rateLimitRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="userId"></param>
        /// <param name="applicationId"></param>
        /// <param name="function"></param>
        /// <param name="max"></param>
        /// <returns>True if rate limited, false if not rate limited.</returns>
        public bool IsRateLimit(Core core, long userId, long applicationId, string function, int max)
        {
            SelectQuery query = RateLimit.GetSelectQueryStub(core, typeof(RateLimit));
            query.AddCondition("user_id", userId);
            query.AddCondition("application_id", applicationId);
            query.AddCondition("rate_limit_function", function);
            query.AddCondition("rate_limit_expires_ut",  ConditionEquality.LessThanEqual, UnixTime.UnixTimeStamp());
            query.AddSort(SortOrder.Ascending, "rate_limit_created_ut"); // If multiple rows exist, use the earliest row

            System.Data.Common.DbDataReader rateLimitReader = core.Db.ReaderQuery(query);

            if (rateLimitReader.HasRows)
            {
                RateLimit rateLimit = new RateLimit(core, rateLimitReader);

                if (rateLimit.Count >= max)
                {
                    rateLimitReader.Close();
                    rateLimitReader.Dispose();
                    return true;
                }
            }

            rateLimitReader.Close();
            rateLimitReader.Dispose();
            return false;
        }

        public bool LimitRate(Core core, long userId, long applicationId, string function, int max, DateTime expires)
        {
            core.Db.BeginTransaction();

            InsertQuery query = new InsertQuery(typeof(RateLimit));
            query.AddField("user_id", userId);
            query.AddField("application_id", applicationId);
            query.AddField("rate_limit_function", function);
            query.AddField("rate_limit_count", 0);
            query.AddField("rate_limit_ip", core.Session.IPAddress.ToString());
            query.AddField("rate_limit_created_ut", UnixTime.UnixTimeStamp());
            query.AddField("rate_limit_exires_ut", UnixTime.UnixTimeStamp(expires));

            core.Db.Query(query);

            UpdateQuery uQuery = new UpdateQuery(typeof(RateLimit));
            uQuery.AddField("rate_limit_count", new QueryOperation("rate_limit_count", QueryOperations.Addition, 1));
            uQuery.AddCondition("user_id", userId);
            uQuery.AddCondition("application_id", applicationId);
            uQuery.AddCondition("rate_limit_function", function);
            uQuery.AddCondition("rate_limit_expires_ut", ConditionEquality.GreaterThanEqual, UnixTime.UnixTimeStamp());

            core.Db.Query(uQuery);
            core.Db.CommitTransaction();

            return IsRateLimit(core, userId, applicationId, function, max);
        }

        public override long Id
        {
            get
            {
                return rateLimitId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidRateLimitException : Exception
    {
    }
}
