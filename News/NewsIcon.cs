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
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.News
{
    [DataTable("news_icon")]
    public class NewsIcon : NumberedItem
    {
        [DataField("icon_id", DataFieldKeys.Primary)]
        private long iconId;
        [DataField("icon_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("icon_title", 31)]
        protected string title;
        [DataField("icon_content_type", 31)]
        protected string contentType;
        [DataField("icon_storage_path", 128)]
        protected string storagePath;

        private Primitive owner;

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
            }
        }

        public NewsIcon(Core core, long articleId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(NewsIcon_ItemLoad);

            try
            {
                LoadItem(articleId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNewsIconException();
            }
        }

        public NewsIcon(Core core, DataRow newsIconDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(NewsIcon_ItemLoad);

            try
            {
                loadItemInfo(newsIconDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNewsIconException();
            }
        }

        void NewsIcon_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return iconId;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public override string Uri
        {
            get
            {
                return Owner.UriStub + "news/icon/" + iconId.ToString();
            }
        }

        public static NewsIcon Create(Core core, News news, string title, string storageName, string contentType)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (news == null)
            {
                throw new InvalidNewsException();
            }

            // TODO: fix this
            Item item = Item.Create(core, typeof(NewsIcon), new FieldValuePair("icon_item_id", news.Owner.Id),
                new FieldValuePair("icon_item_type_id", news.Owner.TypeId),
                new FieldValuePair("icon_title", title),
                new FieldValuePair("icon_storage_path", storageName),
                new FieldValuePair("icon_content_type", contentType));

            return (NewsIcon)item;
        }
    }

    public class InvalidNewsIconException : Exception
    {
    }
}
