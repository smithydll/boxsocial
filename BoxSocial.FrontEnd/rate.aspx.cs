/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class rate : TPage
    {

        public rate()
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

            int rating = Functions.RequestInt("rating", 0);
            long itemId = Functions.RequestLong("item", 0);
            string itemType = (string)Request.QueryString["type"];

            try
            {
                ApplicationEntry ae = new ApplicationEntry(db, new ApplicationCommentType(itemType));

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
            }
            catch (InvalidApplicationException)
            {
                Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to rate is invalid.");
                return;
            }

            try
            {
                Core.ItemRated(itemType, (long)itemId, rating, loggedInMember);

                Rating.Vote(core, itemType, itemId, rating);

                Ajax.SendStatus("voteAccepted");
            }
            catch (InvalidItemException)
            {
                Ajax.ShowMessage(isAjax, "invalidItem", "Invalid Item", "The item you have attempted to rate is invalid.");
            }
            catch (InvalidRatingException)
            {
                Ajax.ShowMessage(isAjax, "invalidRating", "Invalid Rating", "The rating you have attempted to give for this item is invalid.");
            }
            catch (AlreadyRatedException)
            {
                Ajax.ShowMessage(isAjax, "alreadyVoted", "Already Voted", "You have already rated this item, you cannot rate it again");
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
