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
    public class ForumMemberRank : NumberedItem, IPermissibleItem
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
        [DataField("rank_simple_permissions")]
        private bool simplePermissions;

        private Primitive owner;
        private Access access;

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

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }

                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || (rankOwner.Id != owner.Id && rankOwner.TypeId != owner.TypeId))
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(rankOwner);
                    owner = core.PrimitiveCache[rankOwner];
                    return owner;
                }
                else
                {
                    return owner;
                }
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

        protected override void loadItemInfo(DataRow rankRow)
        {
            loadValue(rankRow, "rank_id", out rankId);
            loadValue(rankRow, "rank_owner", out rankOwner);
            loadValue(rankRow, "rank_colour", out rankColour);
            loadValue(rankRow, "rank_title", out rankTitleText);
            loadValue(rankRow, "rank_posts", out rankPosts);
            loadValue(rankRow, "rank_special", out rankSpecial);
            loadValue(rankRow, "rank_image_id", out rankImageId);
            loadValue(rankRow, "rank_simple_permissions", out simplePermissions);

            itemLoaded(rankRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void ForumMemberRank_ItemLoad()
		{
		}

        public static ForumMemberRank Create(Core core, Primitive forumOwner, string title, int posts, bool special, int colour)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

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
			
			DataTable ranksTable = core.Db.Query(sQuery);
			
			foreach (DataRow dr in ranksTable.Rows)
			{
				ForumMemberRank fmr = new ForumMemberRank(core, dr);
				ranks.Add(fmr.Id, fmr);
			}
			
			return ranks;
		}

        public static Dictionary<long, ForumMemberRank> GetRanks(Core core, Primitive forumOwner)
        {
            Dictionary<long, ForumMemberRank> ranks = new Dictionary<long, ForumMemberRank>();

            SelectQuery sQuery = ForumMemberRank.GetSelectQueryStub(typeof(ForumMemberRank));
            sQuery.AddCondition("rank_owner_id", forumOwner.Id);
            sQuery.AddCondition("rank_owner_type_id", forumOwner.TypeId);

            DataTable ranksTable = core.Db.Query(sQuery);

            foreach (DataRow dr in ranksTable.Rows)
            {
                ForumMemberRank fmr = new ForumMemberRank(core, dr);
                ranks.Add(fmr.Id, fmr);
            }

            return ranks;
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return rankOwner;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "Forum Rank: " + RankTitleText;
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }
    }

    public class InvalidForumMemberRankException : Exception
    {
    }
}
