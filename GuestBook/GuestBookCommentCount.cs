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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.GuestBook
{
    [DataTable("guestbook_comment_counts")]
    public class GuestBookCommentCount : Item
    {
        [DataField("owner_id", DataFieldKeys.Unique, "ternary")]
        private long ownerId;
        [DataField("user_id", DataFieldKeys.Unique, "ternary")]
        private long userId;
        [DataField("comment_comments")]
        private long commentCount;

        public long Count
        {
            get
            {
                return commentCount;
            }
        }

        public GuestBookCommentCount(Core core, DataRow countRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(GuestBookCommentCount_ItemLoad);

            loadItemInfo(countRow);
        }

        void GuestBookCommentCount_ItemLoad()
        {
            
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
