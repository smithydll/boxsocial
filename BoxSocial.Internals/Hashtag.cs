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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("hashtags")]
    public class Hashtag : NumberedItem
    {
        [DataField("hashtag_id", DataFieldKeys.Primary)]
        private long hashtagId;
        [DataField("hashtag_text", 31)]
        private string text;
        [DataField("hashtag_text_normalised", DataFieldKeys.Unique, 31)]
        private string textNormalised;
        [DataField("hashtag_items")]
        private long hashtagItems;

        public string HashtagText
        {
            get
            {
                return text;
            }
        }

        public string HashtagTextNormalised
        {
            get
            {
                return textNormalised;
            }
        }

        public Hashtag(Core core, long tagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Hashtag_ItemLoad);

            try
            {
                LoadItem(tagId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidHashtagException();
            }
        }

        public Hashtag(Core core, string textNormalised)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Hashtag_ItemLoad);

            try
            {
                LoadItem("hashtag_text_normalised", textNormalised);
            }
            catch (InvalidItemException)
            {
                throw new InvalidHashtagException();
            }
        }

        public Hashtag(Core core, DataRow tagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Hashtag_ItemLoad);

            loadItemInfo(tagRow);
        }

        private void Hashtag_ItemLoad()
        {
        }
    }

    public class InvalidHashtagException : Exception
    {
    }
}
