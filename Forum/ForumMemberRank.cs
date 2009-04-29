﻿/*
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
        [DataField("rank_id", DataFieldKeys.Primary)]
        private long rankId;
        [DataField("rank_owner")]
        private ItemKey rankOwner;
        [DataField("rank_colour")]
        private int rankColour;
        [DataField("rank_title", 31)]
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

        public static ForumMemberRank Create(Core core, Primitive forumOwner, string title, int posts, bool special, int colour)
        {
            Item item = Item.Create(core, typeof(ForumMemberRank), new FieldValuePair("rank_owner_id", forumOwner.Id),
                new FieldValuePair("rank_owner_type_id", forumOwner.TypeId),
                new FieldValuePair("rank_title", title),
                new FieldValuePair("rank_posts", posts),
                new FieldValuePair("rank_special", special),
                new FieldValuePair("rank_colour", colour),
			    new FieldValuePair("rank_image_id", 0));

            return (ForumMemberRank)item;
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
		
		public static Dictionary<long, ForumMemberRank> GetRanks(Core core, Primitive forumOwner, List<long> rankIds)
		{
			Dictionary<long, ForumMemberRank> ranks = new Dictionary<long, ForumMemberRank>();
			
			SelectQuery sQuery = ForumMemberRank.GetSelectQueryStub(typeof(ForumMemberRank));
			sQuery.AddCondition("rank_owner_id", forumOwner.Id);
			sQuery.AddCondition("rank_owner_type_id", forumOwner.TypeId);
			sQuery.AddCondition("rank_id", ConditionEquality.In, rankIds);
			
			DataTable ranksTable = core.db.Query(sQuery);
			
			foreach (DataRow dr in ranksTable.Rows)
			{
				ForumMemberRank fmr = new ForumMemberRank(core, dr);
				ranks.Add(fmr.Id, fmr);
			}
			
			return ranks;
		}
    }

    public class InvalidForumMemberRankException : Exception
    {
    }
}
