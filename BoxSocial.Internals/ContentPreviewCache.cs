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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("content_preview_caches")]
    public class ContentPreviewCache : NumberedItem
    {
        [DataField("cache_id", DataFieldKeys.Primary)]
        protected long cacheId;

        public ContentPreviewCache(Core core, DataRow cacheRow)
            : base(core)
        {

            try
            {
                loadItemInfo(typeof(ContentPreviewCache), cacheRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidContentPreviewCacheException();
            }
        }

        public override long Id
        {
            get
            {
                return cacheId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidContentPreviewCacheException : Exception
    {
    }
}
