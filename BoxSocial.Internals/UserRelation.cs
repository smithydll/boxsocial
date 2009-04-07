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
        private byte relationOrder;
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

        public byte RelationOrder
        {
            get
            {
                return relationOrder;
            }
        }

        public UserRelation(Core core, DataRow userRow, UserLoadOptions loadOptions)
            : base(core, userRow, loadOptions)
        {
            loadItemInfo(typeof(UserRelation), userRow);
        }

        public static new SelectQuery GetSelectQueryStub(UserLoadOptions loadOptions)
        {
            SelectQuery query = new SelectQuery("user_relations");
            query.AddFields(UserRelation.GetFieldsPrefixed(typeof(UserRelation)));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "relation_you", "user_id");
            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "relation_you", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "relation_you", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            }
            if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }

            return query;
        }
    }
}
