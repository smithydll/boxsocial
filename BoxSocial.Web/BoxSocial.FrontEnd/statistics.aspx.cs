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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class statistics : TPage
    {
        public statistics()
            : base("statistics.html")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string mode = core.Http["mode"];

            switch (mode)
            {
                case "primitive":
                    ShowPrimitiveStatistics();
                    break;
                case "type":
                    ShowTypeStatistics();
                    break;
                case "item":
                    ShowItemStatistics();
                    break;
                default:
                    ShowDefault();
                    break;
            }

            EndResponse();
        }

        public void ShowDefault()
        {
            template.LoadTemplateFile("statistics_default.html");

            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
            javaScriptVariableCollection.Parse("URI", @"/scripts/chart.bundle.min.js");

            List<string[]> breadCrumbParts = new List<string[]>();

            core.Display.ParseBreadCrumbs(breadCrumbParts);
        }

        private void ShowPrimitiveStatistics()
        {
            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
            javaScriptVariableCollection.Parse("URI", @"/scripts/chart.bundle.min.js");

            long primitiveId = core.Functions.FormLong("primitive_id", core.Functions.RequestLong("primitive_id", 0));
            long primitiveTypeId = core.Functions.FormLong("primitive_type", core.Functions.RequestLong("primitive_type", 0));
            int period = Math.Min(core.Functions.RequestInt("period", core.Functions.RequestInt("period", 30)), 1000);

            if (primitiveId == 0 || primitiveTypeId == 0)
            {
                core.Functions.Generate404();
                return;
            }

            ItemKey primitiveKey = new ItemKey(primitiveId, primitiveTypeId);
            core.PrimitiveCache.LoadPrimitiveProfile(primitiveKey);
            Primitive primitive = core.PrimitiveCache[primitiveKey];

            if (!primitive.GetIsMemberOfPrimitive(core.Session.LoggedInMember.ItemKey, primitive.OwnerKey))
            {
                core.Functions.Generate403();
                return;
            }

            DateTime now = core.Tz.Now;
            DateTime firstDate = core.Tz.Now.Subtract(new TimeSpan(period + 1, now.Hour, now.Minute, now.Second));
            DateTime lastDate = core.Tz.Now.Subtract(new TimeSpan(1, now.Hour, now.Minute, now.Second));

            SelectQuery query = ItemViewCountByHour.GetSelectQueryStub(core, typeof(ItemViewCountByHour));
            query.AddCondition("view_hourly_item_owner_id", primitiveId);
            query.AddCondition("view_hourly_item_owner_type_id", primitiveTypeId);
            query.AddCondition("view_hourly_time_ut", ConditionEquality.GreaterThanEqual, core.Tz.GetUnixTimeStamp(firstDate));
            query.AddCondition("view_hourly_time_ut", ConditionEquality.LessThan, core.Tz.GetUnixTimeStamp(lastDate));

            long[] views = new long[period];
            long[] time = new long[period];

            DataTable itemViewsDataTable = core.Db.Query(query);

            foreach (DataRow row in itemViewsDataTable.Rows)
            {
                ItemViewCountByHour ivcbh = new ItemViewCountByHour(core, row);

                int index = (int)((ivcbh.TimeRaw - core.Tz.GetUnixTimeStamp(firstDate)) / (24 * 60 * 60));

                if (index >= 0 && index < period)
                {
                    views[index] += ivcbh.ViewCount;
                    time[index] += ivcbh.Timespan;
                }
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection viewsVariableCollection = core.Template.CreateChild("views_data");
                viewsVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                viewsVariableCollection.Parse("VIEWS", views[i].ToString());
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection timeVariableCollection = core.Template.CreateChild("time_data");
                timeVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                timeVariableCollection.Parse("TIME", (Math.Round(time[i] / 60.0, 2)).ToString());
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid("!/api/statistics", true), core.Prose.GetString("STATISTICS") });

            core.Display.ParseBreadCrumbs(breadCrumbParts);
        }

        private void ShowTypeStatistics()
        {
            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
            javaScriptVariableCollection.Parse("URI", @"/scripts/chart.bundle.min.js");

            long primitiveId = core.Functions.FormLong("primitive_id", core.Functions.RequestLong("primitive_id", 0));
            long primitiveTypeId = core.Functions.FormLong("primitive_type", core.Functions.RequestLong("primitive_type", 0));
            long itemTypeId = core.Functions.FormLong("type", core.Functions.RequestLong("type", 0));
            int period = Math.Min(core.Functions.RequestInt("period", core.Functions.RequestInt("period", 30)), 1000);

            if (itemTypeId == 0 || primitiveId == 0 || primitiveTypeId == 0)
            {
                core.Functions.Generate404();
                return;
            }

            ItemKey primitiveKey = new ItemKey(primitiveId, primitiveTypeId);
            core.PrimitiveCache.LoadPrimitiveProfile(primitiveKey);
            Primitive primitive = core.PrimitiveCache[primitiveKey];

            if (!primitive.GetIsMemberOfPrimitive(core.Session.LoggedInMember.ItemKey, primitive.OwnerKey))
            {
                core.Functions.Generate403();
                return;
            }

            DateTime now = core.Tz.Now;
            DateTime firstDate = core.Tz.Now.Subtract(new TimeSpan(period + 1, now.Hour, now.Minute, now.Second));
            DateTime lastDate = core.Tz.Now.Subtract(new TimeSpan(1, now.Hour, now.Minute, now.Second));

            SelectQuery query = ItemViewCountByHour.GetSelectQueryStub(core, typeof(ItemViewCountByHour));
            query.AddCondition("view_hourly_item_owner_id", primitiveId);
            query.AddCondition("view_hourly_item_owner_type_id", primitiveTypeId);
            query.AddCondition("view_hourly_item_type_id", itemTypeId);
            query.AddCondition("view_hourly_time_ut", ConditionEquality.GreaterThanEqual, core.Tz.GetUnixTimeStamp(firstDate));
            query.AddCondition("view_hourly_time_ut", ConditionEquality.LessThan, core.Tz.GetUnixTimeStamp(lastDate));

            long[] views = new long[period];
            long[] time = new long[period];

            DataTable itemViewsDataTable = core.Db.Query(query);

            foreach (DataRow row in itemViewsDataTable.Rows)
            {
                ItemViewCountByHour ivcbh = new ItemViewCountByHour(core, row);

                int index = (int)((ivcbh.TimeRaw - core.Tz.GetUnixTimeStamp(firstDate)) / (24 * 60 * 60));

                if (index >= 0 && index < period)
                {
                    views[index] += ivcbh.ViewCount;
                    time[index] += ivcbh.Timespan;
                }
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection viewsVariableCollection = core.Template.CreateChild("views_data");
                viewsVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                viewsVariableCollection.Parse("VIEWS", views[i].ToString());
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection timeVariableCollection = core.Template.CreateChild("time_data");
                timeVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                timeVariableCollection.Parse("TIME", (Math.Round(time[i] / 60.0, 2)).ToString());
            }

            template.Parse("S_ITEM_TYPE_ID", itemTypeId.ToString());

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid("!/api/statistics", true), core.Prose.GetString("STATISTICS") });
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid(string.Format("!/api/statistics?mode=primitive&primitive_id={0}&primitive_type={1}", primitive.ItemKey.Id, primitive.ItemKey.TypeId), true), primitive.DisplayName });

            core.Display.ParseBreadCrumbs(breadCrumbParts);
        }

        private void ShowItemStatistics()
        {
            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");
            javaScriptVariableCollection.Parse("URI", @"/scripts/chart.bundle.min.js");

            long itemId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
            long itemTypeId = core.Functions.FormLong("type", core.Functions.RequestLong("type", 0));
            int period = Math.Min(core.Functions.RequestInt("period", core.Functions.RequestInt("period", 30)), 1000);

            if (itemId == 0 || itemTypeId == 0)
            {
                core.Functions.Generate404();
                return;
            }

            ItemKey itemKey = null;

            try
            {
                itemKey = new ItemKey(itemId, itemTypeId);
            }
            catch (InvalidItemTypeException)
            {
                core.Functions.Generate404();
                return;
            }

            NumberedItem ni = null;

            try
            {
                ni = NumberedItem.Reflect(core, itemKey);
            }
            catch (MissingMethodException)
            {
                core.Functions.Generate404();
                return;
            }

            if (!(ni is IPermissibleItem))
            {
                core.Functions.Generate404();
                return;
            }

            IPermissibleItem pi = (IPermissibleItem)ni;

            if (!pi.Owner.GetIsMemberOfPrimitive(core.Session.LoggedInMember.ItemKey, pi.OwnerKey))
            {
                core.Functions.Generate403();
                return;
            }

            template.Parse("ITEM_TITLE", pi.DisplayTitle);

            try
            {
                if (!string.IsNullOrEmpty(pi.Uri))
                {
                    template.Parse("U_ITEM", pi.Uri);
                }
            }
            catch (NotImplementedException)
            {
            }

            DateTime now = core.Tz.Now;
            DateTime firstDate = core.Tz.Now.Subtract(new TimeSpan(period + 1, now.Hour, now.Minute, now.Second));
            DateTime lastDate = core.Tz.Now.Subtract(new TimeSpan(1, now.Hour, now.Minute, now.Second));

            SelectQuery query = ItemViewCountByHour.GetSelectQueryStub(core, typeof(ItemViewCountByHour));
            query.AddCondition("view_hourly_item_id", itemId);
            query.AddCondition("view_hourly_item_type_id", itemTypeId);
            query.AddCondition("view_hourly_time_ut", ConditionEquality.GreaterThanEqual, core.Tz.GetUnixTimeStamp(firstDate));
            query.AddCondition("view_hourly_time_ut", ConditionEquality.LessThan, core.Tz.GetUnixTimeStamp(lastDate));

            long[] views = new long[period];
            long[] time = new long[period];

            DataTable itemViewsDataTable = core.Db.Query(query);

            foreach (DataRow row in itemViewsDataTable.Rows)
            {
                ItemViewCountByHour ivcbh = new ItemViewCountByHour(core, row);

                int index = (int)((ivcbh.TimeRaw - core.Tz.GetUnixTimeStamp(firstDate)) / (24 * 60 * 60));

                if (index >= 0 && index < period)
                {
                    views[index] += ivcbh.ViewCount;
                    time[index] += ivcbh.Timespan;
                }
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection viewsVariableCollection = core.Template.CreateChild("views_data");
                viewsVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                viewsVariableCollection.Parse("VIEWS", views[i].ToString());
            }

            for (int i = 0; i < period; i++)
            {
                DateTime date = firstDate.Add(new TimeSpan(i, 0, 0, 0));

                VariableCollection timeVariableCollection = core.Template.CreateChild("time_data");
                timeVariableCollection.Parse("DATE", date.ToString("yyyy-MM-dd"));
                timeVariableCollection.Parse("TIME", (Math.Round(time[i] / 60.0, 2)).ToString());
            }

            template.Parse("S_ITEM_ID", itemId.ToString());
            template.Parse("S_ITEM_TYPE_ID", itemTypeId.ToString());

            ItemType type = new ItemType(core, itemTypeId);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid("!/api/statistics", true), core.Prose.GetString("STATISTICS") });
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid(string.Format("!/api/statistics?mode=primitive&primitive_id={0}&primitive_type={1}", pi.OwnerKey.Id, pi.OwnerKey.TypeId), true), pi.Owner.DisplayName });
            breadCrumbParts.Add(new string[] { core.Hyperlink.AppendSid(string.Format("!/api/statistics?mode=type&primitive_id={0}&primitive_type={1}&type={2}", pi.OwnerKey.Id, pi.OwnerKey.TypeId, itemTypeId), true), type.TypeNamespace });

            core.Display.ParseBreadCrumbs(breadCrumbParts);
        }
    }
}
