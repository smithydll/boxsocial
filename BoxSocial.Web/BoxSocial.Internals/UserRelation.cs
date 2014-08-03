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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_relations")]
    public class UserRelation : User
    {
        [DataField("relation_id", DataFieldKeys.Primary)]
        private long relationId;
        [DataField("relation_me", DataFieldKeys.Index, "i_relation")]
        private long relationMeId;
        [DataField("relation_you")]
        private new long userId;
        [DataField("relation_order")]
        private int relationOrder;
        [DataField("relation_type", DataFieldKeys.Index, "i_relation", 15)]
        private string relationType;
        [DataField("relation_time_ut")]
        private long relationTime;

        public long RelationId
        {
            get
            {
                return relationId;
            }
        }

        public int RelationOrder
        {
            get
            {
                return relationOrder;
            }
        }

        public UserRelation(Core core, DataRow userRow, UserLoadOptions loadOptions)
            : base(core, userRow, loadOptions)
        {
            loadItemInfo(userRow);
        }

        public UserRelation(Core core, System.Data.Common.DbDataReader userRow, UserLoadOptions loadOptions)
            : base(core, userRow, loadOptions)
        {
            loadItemInfo(userRow);
        }

        protected override void loadItemInfo(DataRow userRow)
        {
            loadValue(userRow, "relation_id", out relationId);
            loadValue(userRow, "relation_me", out relationMeId);
            loadValue(userRow, "relation_you", out userId);
            loadValue(userRow, "relation_order", out relationOrder);
            loadValue(userRow, "relation_type", out relationType);
            loadValue(userRow, "relation_time_ut", out relationTime);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader userRow)
        {
            loadValue(userRow, "relation_id", out relationId);
            loadValue(userRow, "relation_me", out relationMeId);
            loadValue(userRow, "relation_you", out userId);
            loadValue(userRow, "relation_order", out relationOrder);
            loadValue(userRow, "relation_type", out relationType);
            loadValue(userRow, "relation_time_ut", out relationTime);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        public static new SelectQuery GetSelectQueryStub(Core core, UserLoadOptions loadOptions)
        {
            SelectQuery query = new SelectQuery("user_relations");
            query.AddFields(UserRelation.GetFieldsPrefixed(core, typeof(UserRelation)));
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(ItemInfo.GetFieldsPrefixed(core, typeof(ItemInfo)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "relation_you", "user_id");

            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(UserRelation), "relation_you"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemKey.GetTypeId(typeof(User)));

            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "relation_you", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "relation_you", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            }
            /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }*/

            return query;
        }
    }
}
