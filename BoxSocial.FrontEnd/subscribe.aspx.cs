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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class subscribe : TPage
    {
        public subscribe()
            : base("")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAjax = false;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            string type = core.Http.Query["subscribe"];
            long itemId = core.Functions.RequestLong("item", 0);
            long itemTypeId = core.Functions.RequestLong("type", 0);
			ItemKey itemKey = null;
			
			try
			{
				itemKey = new ItemKey(itemId, itemTypeId);
			}
			catch
			{
                core.Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
                return;
			}

            try
            {
                // This isn't the most elegant fix, but it works
                ApplicationEntry ae = null;
                if (core.IsPrimitiveType(itemTypeId))
                {
                    ae = new ApplicationEntry(core, loggedInMember, "GuestBook");
                }
                else
                {
                    ItemType itemType = new ItemType(core, itemTypeId);
                    if (itemType.ApplicationId == 0)
                    {
                        ae = new ApplicationEntry(core, loggedInMember, "GuestBook");
                    }
                    else
                    {
                        ae = new ApplicationEntry(core, itemType.ApplicationId);
                    }
                }

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
            }
			catch (InvalidItemTypeException)
			{
                core.Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
                return;
			}
            catch (InvalidApplicationException)
            {
                core.Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to rate is invalid.");
                return;
            }

            try
            {
                switch (type)
                {
                    case "subscribe":
                        Subscription.SubscribeToItem(core, itemKey);
                        Core.ItemSubscribed(itemKey, loggedInMember);

                        core.Ajax.SendStatus("subscriptionAccepted");
                        break;
                    case "unsubscribe":
                        Subscription.UnsubscribeFromItem(core, itemKey);
                        Core.ItemUnsubscribed(itemKey, loggedInMember);

                        core.Ajax.SendStatus("subscriptionAccepted");
                        break;
                }
            }
            catch (InvalidItemException ex)
            {
                core.Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to subscribe to is invalid.");
            }
            catch (InvalidLikeException)
            {
                core.Ajax.ShowMessage(isAjax, "invalidSubscription", "Invalid Subscription", "The subscription is not valid.");
            }
            catch (AlreadyLikedException)
            {
                core.Ajax.ShowMessage(isAjax, "alreadySubscribed", "Already Subscribed", "You have already subscribe to this item, you cannot subscribe to it again");
            }

            //else
            //{
            //    /* TODO permissions */
            //    /* after 7 days release the IP for dynamics ip fairness */
            //    DataTable ratingsTable = db.Query(string.Format("SELECT user_id FROM ratings WHERE rate_item_id = {0} AND rate_item_type = '{1}' AND (user_id = {2} OR (rate_ip = '{3}' AND rate_time_ut > UNIX_TIMESTAMP() - (60 * 60 * 24 * 7)))",
            //        itemId, Mysql.Escape(itemType), loggedInMember.UserId, session.IPAddress.ToString()));

            //    if (ratingsTable.Rows.Count > 0)
            //    {
            //        //Response.Write("alreadyVoted");
            //        Ajax.ShowMessage(true, "alreadyVoted", "Already Voted", "You have already rated this item, you cannot rate it again");
            //        return;
            //    }
            //    else
            //    {
            //        /* Register a vote */
            //        /* start transaction */
            //        InsertQuery iQuery = new InsertQuery("ratings");
            //        iQuery.AddField("rate_item_id", itemId);
            //        iQuery.AddField("rate_item_type", itemType);
            //        iQuery.AddField("user_id", loggedInMember.UserId);
            //        iQuery.AddField("rate_time_ut", UnixTime.UnixTimeStamp());
            //        iQuery.AddField("rate_rating", rating);
            //        iQuery.AddField("rate_ip", session.IPAddress.ToString());

            //        db.UpdateQuery(iQuery, true);

            //        switch (itemType)
            //        {
            //            case "PHOTO":
            //                db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_rating = (gallery_item_rating * gallery_item_ratings + {0}) / (gallery_item_ratings + 1), gallery_item_ratings = gallery_item_ratings + 1 WHERE gallery_item_id = {1}",
            //                    rating, itemId), false);
            //                break;
            //        }

            //        Ajax.SendStatus("voteAccepted");
            //        return;
            //    }
            //}
        }
    }
}
