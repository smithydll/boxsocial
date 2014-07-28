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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    public enum EmbedType
    {
        Photo,
        Video,
        Link,
        Rich,
    }

    [JsonObject("oembed")]
    [XmlRoot("oembed")]
    public class Embed
    {
        private EmbedType type = EmbedType.Rich;
        private string version = "1.0";
        private string title = null;
        private string authorName = null;
        private string authorUrl = null;
        private string providerName = null;
        private string providerUrl = null;
        private string cacheAge = null;
        private string thumbnailUrl = null;
        private string thumbnailWidth = null;
        private string thumbnailHeight = null;
        private string url = null;
        private string width = null;
        private string height = null;
        private string html = null;

        public Embed()
        {
            this.type = EmbedType.Link;
        }

        public Embed(EmbedType type)
        {
            this.type = type;
        }

        public Embed(string url, int width, int height)
        {
            this.type = EmbedType.Photo;
            this.url = url;
            this.width = width.ToString();
            this.height = height.ToString();
        }

        public Embed(EmbedType type, string html, int width, int height)
        {
            this.type = type;
            this.html = html;
            this.width = width.ToString();
            this.height = height.ToString();
        }

        [JsonProperty("type")]
        [XmlElement("type")]
        public string Type
        {
            get
            {
                switch (type)
                {
                    case EmbedType.Link:
                        return "link";
                    case EmbedType.Photo:
                        return "photo";
                    case EmbedType.Video:
                        return "video";
                    case EmbedType.Rich:
                    default:
                        return "rich";
                }
            }
            set
            {
                switch (value)
                {
                    case "link":
                        type = EmbedType.Link;
                        break;
                    case "photo":
                        type = EmbedType.Photo;
                        break;
                    case "video":
                        type = EmbedType.Video;
                        break;
                    case "rich":
                        type = EmbedType.Rich;
                        break;
                }
            }
        }

        [JsonProperty("version")]
        [XmlElement("version")]
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("title", IsNullable = false)]
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }

        [JsonProperty("author_name", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("author_name", IsNullable = false)]
        public string AuthorName
        {
            get
            {
                return authorName;
            }
            set
            {
                authorName = value;
            }
        }

        [JsonProperty("author_url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("author_url", IsNullable = false)]
        public string AuthorUrl
        {
            get
            {
                return authorUrl;
            }
            set
            {
                authorUrl = value;
            }
        }

        [JsonProperty("provider_name", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("provider_name", IsNullable = false)]
        public string ProviderName
        {
            get
            {
                return providerName;
            }
            set
            {
                providerName = value;
            }
        }

        [JsonProperty("provider_url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("provider_url", IsNullable = false)]
        public string ProviderUrl
        {
            get
            {
                return providerUrl;
            }
            set
            {
                providerUrl = value;
            }
        }

        [JsonProperty("cache_age", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("cache_age", IsNullable = false)]
        public string CacheAge
        {
            get
            {
                return cacheAge;
            }
            set
            {
                cacheAge = value;
            }
        }

        [JsonProperty("thumbnail_url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("thumbnail_url", IsNullable = false)]
        public string ThumbnailUrl
        {
            get
            {
                return thumbnailUrl;
            }
            set
            {
                thumbnailUrl = value;
            }
        }

        [JsonProperty("thumbnail_width", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("thumbnail_width", IsNullable = false)]
        public string ThumbnailWidth
        {
            get
            {
                return thumbnailWidth;
            }
            set
            {
                thumbnailWidth = value;
            }
        }

        [JsonProperty("thumbnail_height", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("thumbnail_height", IsNullable = false)]
        public string ThumbnailHeight
        {
            get
            {
                return thumbnailHeight;
            }
            set
            {
                thumbnailHeight = value;
            }
        }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("url", IsNullable = false)]
        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Include)]
        [XmlElement("width", IsNullable = true)]
        public string Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Include)]
        [XmlElement("height", IsNullable = true)]
        public string Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        [JsonProperty("html", NullValueHandling = NullValueHandling.Ignore)]
        [XmlElement("html", IsNullable = false)]
        public string Html
        {
            get
            {
                return html;
            }
            set
            {
                html = value;
            }
        }
    }
}
