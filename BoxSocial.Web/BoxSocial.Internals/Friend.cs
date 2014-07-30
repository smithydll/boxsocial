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
	
	[PseudoPrimitive]
	public class Friend : UserRelation
	{
		
		public Friend(Core core, DataRow userRow, UserLoadOptions loadOptions)
			: base(core, userRow, loadOptions)
		{
		}

        public Friend(Core core, System.Data.Common.DbDataReader userRow, UserLoadOptions loadOptions)
            : base(core, userRow, loadOptions)
        {
        }

        public static ItemKey FriendsGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(Friend)));
            }
        }

        public static ItemKey FamilyGroupKey
        {
            get
            {
                return new ItemKey(-2, ItemType.GetTypeId(typeof(Friend)));
            }
        }

        public static ItemKey BlockedGroupKey
        {
            get
            {
                return new ItemKey(-3, ItemType.GetTypeId(typeof(Friend)));
            }
        }

        public static ItemKey ColleaguesGroupKey
        {
            get
            {
                return new ItemKey(-4, ItemType.GetTypeId(typeof(Friend)));
            }
        }
	}
}
