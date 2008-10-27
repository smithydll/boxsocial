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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("tags")]
    public class Tag : NumberedItem
    {
        [DataField("tag_id", DataFieldKeys.Primary)]
        private long tagId;
        [DataField("tag_text", 31)]
        private string text;
        [DataField("tag_text_normalised", DataFieldKeys.Unique, 31)]
        private string textNormalised;
        [DataField("tag_items")]
        private long tagItems;

        public Tag(Core core, long tagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            try
            {
                LoadItem(tagId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTagException();
            }
        }

        public Tag(Core core, string textNormalised)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            try
            {
                LoadItem("tag_text_normalised", textNormalised);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTagException();
            }
        }

        public Tag(Core core, DataRow tagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tag_ItemLoad);

            loadItemInfo(tagRow);
        }

        private void Tag_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return tagId;
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

    public class InvalidTagException : Exception
    {
    }
}
