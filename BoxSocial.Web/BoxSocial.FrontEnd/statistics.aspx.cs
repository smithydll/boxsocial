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
                case "item":
                    ShowItemStatistics();
                    break;
            }

            EndResponse();
        }

        private void ShowPrimitiveStatistics()
        {

        }

        private void ShowItemStatistics()
        {
            long itemId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
            long itemTypeId = core.Functions.FormLong("type", core.Functions.RequestLong("type", 0));

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

            if (!pi.Owner.GetIsMemberOfPrimitive(core.Session.LoggedInMember.ItemKey, pi.ItemKey))
            {
                core.Functions.Generate403();
                return;
            }

            SelectQuery query = ItemViewCountByHour.GetSelectQueryStub(core, typeof(ItemViewCountByHour));
            query.AddCondition("view_hourly_item_id", itemId);
            query.AddCondition("view_hourly_item_type_id", itemTypeId);
            query.AddCondition("view_hourly_time", ConditionEquality.GreaterThanEqual, UnixTime.UnixTimeStamp() - 29 * 24 * 60 * 60);
            query.AddCondition("view_hourly_time", ConditionEquality.LessThan, UnixTime.UnixTimeStamp() - 24 * 60 * 60);

            template.Parse("S_ITEM_ID", itemId.ToString());
            template.Parse("S_ITEM_TYPE_ID", itemTypeId.ToString());
        }
    }
}
