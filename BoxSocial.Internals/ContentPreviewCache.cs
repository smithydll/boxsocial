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
    public sealed class ContentPreviewCache : NumberedItem
    {
        [DataField("cache_id", DataFieldKeys.Primary)]
        private long cacheId;
        [DataField("content_domain", 127)]
        private string domain;
        [DataField("content_unique", 127)]
        private string unique;
        [DataField("content_language", 8)]
        private string language;
        [DataField("content_title", 127)]
        private string title;
        [DataField("content_body", MYSQL_TEXT)]
        private string body;
        [DataField("content_image", 255)]
        private string image;
        [DataField("content_cached_time")]
        private long cachedTimeRaw;

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public string Image
        {
            get
            {
                return image;
            }
        }

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

        public static void Create(Core core, string domain, string unique, string title, string body, string language)
        {
            Create(core, domain, unique, title, body, language, string.Empty);
        }

        public static void Create(Core core, string domain, string unique, string title, string body, string language, string image)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item.Create(core, typeof(ContentPreviewCache), true, new FieldValuePair("content_domain", domain),
                new FieldValuePair("content_unique", unique),
                new FieldValuePair("content_title", title),
                new FieldValuePair("content_body", body),
                new FieldValuePair("content_image", image),
                new FieldValuePair("content_language", language),
                new FieldValuePair("content_cached_time", UnixTime.UnixTimeStamp()));
        }

        public static ContentPreviewCache GetPreview(Core core, string domain, string unique, string language)
        {
            ContentPreviewCache preview = null;

            SelectQuery query = ContentPreviewCache.GetSelectQueryStub(typeof(ContentPreviewCache));
            query.AddCondition("content_domain", domain);
            query.AddCondition("content_unique", unique);
            query.AddCondition("content_language", language);

            DataTable previewDataTable = core.Db.Query(query);

            if (previewDataTable.Rows.Count == 1)
            {
                preview = new ContentPreviewCache(core, previewDataTable.Rows[0]);
            }

            return preview;
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
