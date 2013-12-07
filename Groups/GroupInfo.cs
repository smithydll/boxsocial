﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    [DataTable("group_info", "GROUP")]
    public class UserGroupInfo : NumberedItem
    {

        [DataField("group_id", DataFieldKeys.Unique)]
        private long groupId;
        [DataField("group_name", DataFieldKeys.Unique, 64)]
        private string groupSlug;
        [DataField("group_name_display", 64)]
        private string displayName;
        [DataField("group_type", 15)]
        private string groupType;
        [DataField("group_abstract", MYSQL_TEXT)]
        private string groupDescription;
        [DataField("group_reg_date_ut")]
        private long timestampCreated;
        [DataField("group_reg_ip", 50)]
        private string registrationIp;
        [DataField("group_operators")]
        private long groupOperators;
        [DataField("group_officers")]
        private long groupOfficers;
        [DataField("group_members")]
        private long groupMembers;
        [DataField("group_category")]
        private short rawCategory;
        [DataField("group_gallery_items")]
        private long galleryItems;
        [DataField("group_home_page", MYSQL_TEXT)]
        private string groupHomepage;
        [DataField("group_style", MYSQL_MEDIUM_TEXT)]
        private string groupStyle;
        [DataField("group_icon")]
        private long groupIcon;
        [DataField("group_bytes")]
        private ulong groupBytes;
        [DataField("group_views")]
        private long groupViews;
		[DataField("group_news_articles")]
        private long groupNewsArticles;

        private string displayNameOwnership;
        private string category;

        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                SetProperty("displayName", value);
            }
        }

        public string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != string.Empty) ? displayName : groupSlug;

                    if (displayNameOwnership.EndsWith("s"))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
            }
        }

        public string GroupType
        {
            get
            {
                return groupType;
            }
            set
            {
                SetProperty("groupType", value);
            }
        }

        public string Description
        {
            get
            {
                return groupDescription;
            }
            set
            {
                SetProperty("groupDescription", value);
            }
        }

        public long Members
        {
            get
            {
                return groupMembers;
            }
        }

        public long Officers
        {
            get
            {
                return groupOfficers;
            }
        }

        public long Operators
        {
            get
            {
                return groupOperators;
            }
        }

        /// <summary>
        /// Gets the user's display picture Id
        /// </summary>
        public long DisplayPictureId
        {
            get
            {
                return groupIcon;
            }
            set
            {
                SetPropertyByRef(new { groupIcon }, value);
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
        }

        public short RawCategory
        {
            get
            {
                return rawCategory;
            }
            set
            {
                SetProperty("rawCategory", value);
            }
        }

        public long GalleryItems
        {
            get
            {
                return galleryItems;
            }
        }
		
		public long NewsArticles
		{
			get
			{
				return groupNewsArticles;
			}
		}

        public string GroupHomepage
        {
            get
            {
                return groupHomepage;
            }
            set
            {
                SetProperty("groupHomepage", value);
            }
        }

        public string Style
        {
            get
            {
                return groupStyle;
            }
            set
            {
                SetProperty("groupStyle", value);
            }
        }

        public DateTime DateCreated(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timestampCreated);
        }

        internal UserGroupInfo(Core core, long groupId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroupInfo_ItemLoad);

            try
            {
                LoadItem("group_id", groupId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGroupException();
            }
        }

        internal UserGroupInfo(Core core, DataRow groupRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserGroupInfo_ItemLoad);

            try
            {
                loadItemInfo(groupRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGroupException();
            }
        }

        void UserGroupInfo_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return groupId;
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
