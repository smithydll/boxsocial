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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum LikeType : sbyte
    {
        Neutral = 0,
        Like = 1,
        Dislike = -1,
    }

    [DataTable("likes")]
    public class Like : Item
    {
        [DataField("like_item", DataFieldKeys.Index, "i_like")]
        private ItemKey itemKey;
        [DataField("user_id", DataFieldKeys.Index, "i_like")]
        private long ownerId;
        [DataField("like_liking")]
        private sbyte liking;
        [DataField("like_time_ut")]
        private long timeRaw;
        [DataField("like_ip", 55)]
        private string ip;

        private User owner;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
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

        public LikeType Liking
        {
            get
            {
                return (LikeType)liking;
            }
            set
            {
                SetPropertyByRef(new { liking }, value);
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        private Like(Core core, DataRow likeRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Like_ItemLoad);

            //
            // Because this class does not have an ID, it should only
            // be able to construct itself from raw data.
            //

            loadItemInfo(likeRow);
        }

        void Like_ItemLoad()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="rating"></param>
        /// <remarks>ItemRated should implement a transaction.</remarks>
        public static void LikeItem(Core core, ItemKey itemKey, LikeType like)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (itemKey.Id < 1)
            {
                throw new InvalidItemException();
            }

            if (like < LikeType.Dislike || like > LikeType.Like)
            {
                throw new InvalidLikeException();
            }

            /* after 7 days release the IP for dynamics ip fairness */
            SelectQuery query = Like.GetSelectQueryStub(typeof(Like));
            query.AddCondition("like_item_id", itemKey.Id);
            query.AddCondition("like_item_type_id", itemKey.TypeId);
            QueryCondition qc1 = query.AddCondition("user_id", core.LoggedInMemberId);
            QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, "like_ip", core.Session.IPAddress.ToString());
            qc2.AddCondition("like_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24 * 7);

            /*DataTable ratingsTable = db.Query(string.Format("SELECT user_id FROM ratings WHERE rate_item_id = {0} AND rate_item_type = '{1}' AND (user_id = {2} OR (rate_ip = '{3}' AND rate_time_ut > UNIX_TIMESTAMP() - (60 * 60 * 24 * 7)))",
                itemId, Mysql.Escape(itemType), loggedInMember.UserId, session.IPAddress.ToString()));*/

            DataTable likesTable = core.Db.Query(query);

            ItemInfo ii = null;

            try
            {
                ii = new ItemInfo(core, itemKey);
            }
            catch (InvalidIteminfoException)
            {
                ii = ItemInfo.Create(core, itemKey);
            }

            if (likesTable.Rows.Count > 0)
            {
                Like liked = new Like(core, likesTable.Rows[0]);

                if (liked.Liking == like)
                {
                    throw new AlreadyLikedException();
                }

                switch (like)
                {
                    case LikeType.Like:
                        ii.DecrementDislikes();
                        ii.IncrementLikes();
                        break;
                    case LikeType.Dislike:
                        ii.DecrementLikes();
                        ii.IncrementDislikes();
                        break;
                }

                UpdateQuery uQuery = new UpdateQuery("likes");
                uQuery.AddField("like_time_ut", UnixTime.UnixTimeStamp());
                uQuery.AddField("like_liking", (sbyte)like);
                uQuery.AddField("like_ip", core.Session.IPAddress.ToString());
                uQuery.AddCondition("user_id", core.LoggedInMemberId);
                uQuery.AddCondition("like_item_id", itemKey.Id);
                uQuery.AddCondition("like_item_type_id", itemKey.TypeId);

                // commit the transaction
                core.Db.Query(uQuery);
                
            }
            else
            {
                switch (like)
                {
                    case LikeType.Like:
                        ii.IncrementLikes();
                        break;
                    case LikeType.Dislike:
                        ii.IncrementDislikes();
                        break;
                }

                InsertQuery iQuery = new InsertQuery("likes");
                iQuery.AddField("like_item_id", itemKey.Id);
                iQuery.AddField("like_item_type_id", itemKey.TypeId);
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("like_time_ut", UnixTime.UnixTimeStamp());
                iQuery.AddField("like_liking", (sbyte)like);
                iQuery.AddField("like_ip", core.Session.IPAddress.ToString());

                // commit the transaction
                core.Db.Query(iQuery);
            }

            return;
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ItemLikedEventArgs : EventArgs
    {
        private LikeType likeing;
        private ItemKey itemKey;
        private User rater;

        public LikeType Likeing
        {
            get
            {
                return likeing;
            }
        }

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public string ItemType
        {
            get
            {
                return itemKey.TypeString;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Rater
        {
            get
            {
                return rater;
            }
        }

        public ItemLikedEventArgs(LikeType likeing, User rater, ItemKey itemKey)
        {
            this.likeing = likeing;
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class AlreadyLikedException : Exception
    {
    }

    public class InvalidLikeException : Exception
    {
    }
}
