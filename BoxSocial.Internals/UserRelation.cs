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
        [DataField("relation_me")]
        private long relationMeId;
        [DataField("relation_you")]
        private long userId;
        [DataField("relation_order")]
        private byte relationOrder;
        [DataField("relation_type", 15)]
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
    }
}
