/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("item_tags")]
    public class ItemTag : NumberedItem
    {
        [DataField("item_tag_id", DataFieldKeys.Primary)]
        private long itemTagId;
        [DataField("item_id")]
        private long itemId;
        [DataField("item_type", 63)]
        private string itemType;
        [DataField("tag_id")]
        private long tagId;

        private Tag tag;

        public ItemTag(Core core, long itemTagId)
            : base(core)
        {
        }

        private void ItemTag_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return itemTagId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
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
