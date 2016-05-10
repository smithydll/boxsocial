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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("item_views")]
    public class ItemView : NumberedItem
    {
        [DataField("view_id")]
        private long viewId;
        [DataField("view_item")]
        private ItemKey itemKey;
        [DataField("user_id")]
        private long userId;
        [DataField("view_ip", 50)]
        private string viewIp;
        [DataField("view_session_id", 32)]
        private string viewSessionId;
        [DataField("view_time_ut")]
        private long viewTimeRaw;
        [DataField("view_time_leave_ut")]
        private long viewTimeLeaveRaw;
        [DataField("view_referral_uri", 2000)]
        private string viewReferralUri;
        [DataField("view_http_referer", 2000)]
        private string viewHttpReferer;
        [DataField("view_http_user_agent", 255)]
        private string viewHttpUserAgent;
        [DataField("view_counted")]
        private bool viewCounted;
        [DataField("view_discounted")]
        private bool viewDiscounted;
        [DataField("view_processed")]
        private bool viewProcessed;

        public override long Id
        {
            get
            {
                return viewId;
            }
        }

        public ItemKey ViewKey
        {
            get
            {
                return itemKey;
            }
        }

        public bool Counted
        {
            get
            {
                return viewCounted;
            }
        }

        public bool Discounted
        {
            get
            {
                return viewDiscounted;
            }
        }

        public bool Processed
        {
            get
            {
                return viewProcessed;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private ItemView(Core core, DataRow viewRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemView_ItemLoad);

            loadItemInfo(viewRow);
        }

        void ItemView_ItemLoad()
        {

        }

        public static void LogView(Core core, NumberedItem item)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (item.Id < 1)
            {
                throw new InvalidItemException();
            }

            if (core.Session.IsBot)
            {
                // Do not count page views from robots
                return;
            }

            InsertQuery iQuery = new InsertQuery("item_views");
            iQuery.AddField("view_item_id", item.ItemKey.Id);
            iQuery.AddField("view_item_type_id", item.ItemKey.TypeId);
            if (core.Session.SignedIn)
            {
                iQuery.AddField("user_id", core.LoggedInMemberId);
            }
            iQuery.AddField("view_ip", core.Session.IPAddress.ToString());
            iQuery.AddField("view_session_id", core.Session.SessionId);
            iQuery.AddField("view_time_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("view_time_leave_ut", 0);
            iQuery.AddField("view_referral_uri", core.Http["ref"]);
            iQuery.AddField("view_http_referer", core.Http.UrlReferer);
            iQuery.AddField("view_http_user_agent", core.Http.UserAgent);
            iQuery.AddField("view_counted", false);
            iQuery.AddField("view_discounted", false);
            iQuery.AddField("view_processed", false);

            // commit the query
            core.Db.Query(iQuery);
        }

        public static void ProcessViews(Core core)
        {


            List<ItemView> views = new List<ItemView>();

            foreach (ItemView view in views)
            {
                ItemInfo info = new ItemInfo(core, view.ViewKey);

                // If the view unique enough to be counted?
                bool viewUnique = VerifyView(core, view);
                
                if (!view.Counted)
                {

                }
            }
        }

        private static bool VerifyView(Core core, ItemView view)
        {
            SelectQuery query = new SelectQuery(typeof(ItemView));
            query.AddField(new QueryFunction("view_id", QueryFunctions.Count, "real_views"));
            query.AddCondition("view_item_id", view.ViewKey.Id);
            query.AddCondition("view_item_type_id", view.ViewKey.TypeId);
            QueryCondition qc1 = query.AddCondition("view_ip", core.Session.IPAddress.ToString());
            qc1.AddCondition("view_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24 * 7);
            qc1.AddCondition(ConditionRelations.Or, "view_session_id", core.Session.SessionId);
            if (core.LoggedInMemberId > 0)
            {
                qc1.AddCondition(ConditionRelations.Or, "user_id", core.LoggedInMemberId);
            }

            if ((long)core.Db.Query(query).Rows[0]["real_views"] > 0)
            {
                return false;
            }

            return true;
        }
    }
}
