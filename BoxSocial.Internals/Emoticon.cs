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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using BoxSocial.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    [DataTable("emoticons")]
    public class Emoticon : NumberedItem
    {
        [DataField("emoticon_id", DataFieldKeys.Primary)]
        private long emoticonId;
        [DataField("emoticon_title", 63)]
        private string title;
        [DataField("emoticon_code", 7)]
        private string code;
        [DataField("emoticon_file", 63)]
        private string file;
        [DataField("emoticon_category", DataFieldKeys.Index, "category", 31)]
        private string category;

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Code
        {
            get
            {
                return code;
            }
        }

        public string File
        {
            get
            {
                return file;
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
        }

        public Emoticon(Core core, long emoticonId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Emoticon_ItemLoad);

            try
            {
                LoadItem(emoticonId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEmoticonException();
            }
        }

        public Emoticon(Core core, DataRow emoticonRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Emoticon_ItemLoad);

            try
            {
                loadItemInfo(emoticonRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEmoticonException();
            }
        }

        public Emoticon(Core core, System.Data.Common.DbDataReader emoticonRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Emoticon_ItemLoad);

            try
            {
                loadItemInfo(emoticonRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEmoticonException();
            }
        }

        public Emoticon(Core core, HibernateItem emoticonRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Emoticon_ItemLoad);

            try
            {
                loadItemInfo(emoticonRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEmoticonException();
            }
        }

        protected override void loadItemInfo(DataRow emoticonRow)
        {
            emoticonId = (long)emoticonRow["emoticon_id"];
            loadValue(emoticonRow, "emoticon_title", out title);
            loadValue(emoticonRow, "emoticon_code", out code);
            loadValue(emoticonRow, "emoticon_file", out file);
            loadValue(emoticonRow, "emoticon_category", out category);

            itemLoaded(emoticonRow);
            //core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader emoticonRow)
        {
            emoticonId = (long)emoticonRow["emoticon_id"];
            loadValue(emoticonRow, "emoticon_title", out title);
            loadValue(emoticonRow, "emoticon_code", out code);
            loadValue(emoticonRow, "emoticon_file", out file);
            loadValue(emoticonRow, "emoticon_category", out category);

            itemLoaded(emoticonRow);
            //core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(HibernateItem emoticonRow)
        {
            emoticonId = (long)emoticonRow["emoticon_id"];
            loadValue(emoticonRow, "emoticon_title", out title);
            loadValue(emoticonRow, "emoticon_code", out code);
            loadValue(emoticonRow, "emoticon_file", out file);
            loadValue(emoticonRow, "emoticon_category", out category);

            itemLoaded(emoticonRow);
            //core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void Emoticon_ItemLoad()
        {
        }

        private static Emoticon Create(Core core, string title, string code, string file, string category)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item item = Item.Create(core, typeof(Emoticon), new FieldValuePair("emoticon_title", title),
                new FieldValuePair("emoticon_code", code),
                new FieldValuePair("emoticon_file", file),
                new FieldValuePair("emoticon_category", category));

            return (Emoticon)item;
        }

        public static string BuildEmoticonSelectBox(string name)
        {
            return string.Empty;
        }

        // Install https://github.com/Genshin/PhantomOpenEmoji
        // The installer process will Download the svg files and convert to png
        public static void InstallEmoji(Core core, string json)
        {
            List<Dictionary<string, object>> emoji = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            foreach (Dictionary<string, object> emoticon in emoji)
            {
                if (emoticon.ContainsKey("moji"))
                {
                    bool flag = true;

                    foreach (Emoticon e in core.Emoticons)
                    {
                        if (e.Code == (string)emoticon["moji"])
                        {
                            flag = false;
                            continue;
                        }
                    }

                    if (flag)
                    {
                        Emoticon.Create(core, (string)emoticon["name"], (string)emoticon["moji"], @"/images/emoticons/" + (string)emoticon["name"] + ".png", (string)emoticon["category"]);
                    }
                }
            }
        }

        public override long Id
        {
            get
            {
                return emoticonId;
            }
        }

        public override string Uri
        {
            get
            {
                return file;
            }
        }

        string toString = null;
        public override string ToString()
        {
            if (toString == null)
            {
                toString = "<img alt=\"" + Title + "\" title=\"" + Title + "\" src=\"" + Uri + "\" />";
            }
            return toString;
        }
    }

    public class InvalidEmoticonException : Exception
    {
    }
}
