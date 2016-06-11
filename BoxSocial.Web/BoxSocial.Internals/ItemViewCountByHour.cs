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
    [DataTable("item_view_count_hourly")]
    public class ItemViewCountByHour : NumberedItem
    {
        [DataField("view_hourly_id", DataFieldKeys.Primary)]
        private long viewHourlyId;
        [DataField("view_hourly_time_ut")]
        private long viewHourlyTimeRaw;
        [DataField("view_hourly_item")]
        private ItemKey itemKey;
        [DataField("view_hourly_item_owner")]
        private ItemKey ownerKey;
        [DataField("view_hourly_count")]
        private long viewHourlyCount;
        [DataField("view_hourly_time")]
        private long viewHourlyTime;

        public long TimeRaw
        {
            get
            {
                return viewHourlyTimeRaw;
            }
        }

        public long Timespan
        {
            get
            {
                return viewHourlyTime;
            }
        }

        public long ViewCount
        {
            get
            {
                return viewHourlyCount;
            }
        }

        public ItemViewCountByHour(Core core, DataRow viewRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemViewCountByHour_ItemLoad);

            loadItemInfo(viewRow);
        }

        protected override void loadItemInfo(DataRow viewRow)
        {
            loadValue(viewRow, "view_hourly_id", out viewHourlyId);
            loadValue(viewRow, "view_hourly_time_ut", out viewHourlyTimeRaw);
            loadValue(viewRow, "view_hourly_item", out itemKey);
            loadValue(viewRow, "view_hourly_item_owner", out ownerKey);
            loadValue(viewRow, "view_hourly_count", out viewHourlyCount);
            loadValue(viewRow, "view_hourly_time", out viewHourlyTime);

            itemLoaded(viewRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void ItemViewCountByHour_ItemLoad()
        {

        }

        public override long Id
        {
            get
            {
                return viewHourlyId;
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
}
