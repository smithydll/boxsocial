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
using System.Data;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_ranks")]
    public class ForumMemberRank : NumberedItem
    {
        [DataField("rank_id")]
        private long rankId;
        [DataField("rank_owner")]
        private ItemKey rankOwner;
        [DataField("rank_colour")]
        private int rankColour;
        [DataField("rank_title")]
        private string rankTitleText;
        [DataField("rank_posts")]
        private int rankPosts;
        [DataField("rank_special")]
        private bool rankSpecial;
        [DataField("rank_image_id")]
        private long rankImageId;

        public long RankId
        {
            get
            {
                return rankId;
            }
        }

        public Color RankColour
        {
            get
            {
                return Color.FromArgb(rankColour);
            }
        }

        public int RankColourRaw
        {
            get
            {
                return rankColour;
            }
            set
            {
                SetProperty("rankColour", value);
            }
        }

        public string RankTitleText
        {
            get
            {
                return rankTitleText;
            }
            set
            {
                SetProperty("rankTitleText", value);
            }
        }

        public int RankPosts
        {
            get
            {
                return rankPosts;
            }
            set
            {
                SetProperty("rankPosts", value);
            }
        }

        public bool RankSpecial
        {
            get
            {
                return rankSpecial;
            }
            set
            {
                SetProperty("rankSpecial", value);
            }
        }

        public ForumMemberRank(Core core, long rankId)
            : base(core)
        {
            try
            {
                LoadItem(rankId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumMemberRankException();
            }
        }

        public ForumMemberRank(Core core, DataRow rankDataRow)
			: base(core)
		{
            ItemLoad += new ItemLoadHandler(ForumMemberRank_ItemLoad);
			
			try
			{
                loadItemInfo(rankDataRow);
			}
		    catch (InvalidItemException)
			{
                throw new InvalidForumMemberRankException();
			}
		}

        void ForumMemberRank_ItemLoad()
		{
		}

        public static ForumMemberRank Create(Core core)
        {
            /*Item item = Item.Create(core, typeof(ForumMemberRank), new FieldValuePair("article_item_id", owner.Id),
                new FieldValuePair("article_item_type_id", owner.TypeId),
                new FieldValuePair("article_time_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("article_subject", subject),
                new FieldValuePair("article_body", body),
                new FieldValuePair("article_comments", 0),
                new FieldValuePair("user_id", core.LoggedInMemberId));

            return (Article)item;*/

            throw new NotImplementedException();
        }

        public override long Id
        {
            get
            {
                return rankId;
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

    public class InvalidForumMemberRankException : Exception
    {
    }
}
