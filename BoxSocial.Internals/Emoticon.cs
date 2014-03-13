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
        [DataField("emoticon_title", 31)]
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

        void Emoticon_ItemLoad()
        {
        }

        public static string BuildEmoticonSelectBox(string name)
        {
            return string.Empty;
        }

        // Install https://github.com/Genshin/PhantomOpenEmoji
        // The installer process will Download the svg files and convert to png
        public static void InstallEmoji(string json)
        {
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
                return string.Empty;
            }
        }
    }

    public class InvalidEmoticonException : Exception
    {
    }
}
