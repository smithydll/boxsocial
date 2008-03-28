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
            int rating;
            ulong itemId;
            string itemType;

            Response.ContentType = "text/plain";

            try
            {
                rating = int.Parse((string)Request.QueryString["rating"]);
                itemId = ulong.Parse((string)Request.QueryString["item"]);
                itemType = (string)Request.QueryString["type"];
            }
            catch
            {
                Response.Write("invalidVote");
                //Ajax.SendStatus("invalidVote", core);
                return;
            }

            if (!Functions.IsValidItemType(itemType))
            {
                Response.Write("invalidVote");
                return;
            }

            if (rating < 1 || rating > 5)
            {
                Response.Write("doNotCheat");
                return;
            }

            if (loggedInMember == null)
            {
                Response.Write("notLoggedIn");
                return;
            }
            else
            {
                /* TODO permissions */
                /* after 7 days release the IP for dynamics ip fairness */
                DataTable ratingsTable = db.SelectQuery(string.Format("SELECT user_id FROM ratings WHERE rate_item_id = {0} AND rate_item_type = '{1}' AND (user_id = {2} OR (rate_ip = '{3}' AND rate_time_ut > UNIX_TIMESTAMP() - (60 * 60 * 24 * 7)))",
                    itemId, Mysql.Escape(itemType), loggedInMember.UserId, session.IPAddress.ToString()));

                if (ratingsTable.Rows.Count > 0)
                {
                    //Response.Write("alreadyVoted");
                    Ajax.ShowMessage(true, "alreadyVoted", core, "Already Voted", "You have already rated this item, you cannot rate it again");
                    return;
                }
                else
                {
                    /* Register a vote */
                    /* start transaction */
                    InsertQuery iQuery = new InsertQuery("ratings");
                    iQuery.AddField("rate_item_id", itemId);
                    iQuery.AddField("rate_item_type", itemType);
                    iQuery.AddField("user_id", loggedInMember.UserId);
                    iQuery.AddField("rate_time_ut", UnixTime.UnixTimeStamp());
                    iQuery.AddField("rate_rating", rating);
                    iQuery.AddField("rate_ip", session.IPAddress.ToString());

                    db.UpdateQuery(iQuery, true);

                    switch (itemType)
                    {
                        case "PHOTO":
                            db.UpdateQuery(string.Format("UPDATE gallery_items SET gallery_item_rating = (gallery_item_rating * gallery_item_ratings + {0}) / (gallery_item_ratings + 1), gallery_item_ratings = gallery_item_ratings + 1 WHERE gallery_item_id = {1}",
                                rating, itemId), false);
                            break;
                    }

                    Ajax.SendStatus("voteAccepted", core);
                    return;
                }
            }
        }
    }
}
