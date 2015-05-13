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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class AjaxWriter : ResponseWriter
    {
        public AjaxWriter(Core core) : base(core)
        {
        }

        public override void WriteObject(object obj)
        {
            XmlSerializer xs;
            StringWriter stw;

            xs = new XmlSerializer(obj.GetType());
            stw = new StringWriter();

            core.Http.WriteXml(xs, obj);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }
    }

    [XmlRoot("ajax")]
    public class AjaxUserDictionary
    {
        [XmlElement("type")]
        public string AjaxType = "UserDictionary";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlArray("array")]
        [XmlArrayItem("item")]
        public AjaxUserDictionaryItem[] ResponseDictionary;
    }

    public class AjaxUserDictionaryItem
    {
        [XmlElement("id")]
        public long Id;
        [XmlElement("value")]
        public string Value;
        [XmlElement("tile")]
        public string Tile;

        public AjaxUserDictionaryItem()
        {

        }

        public AjaxUserDictionaryItem(long id, string value, string tile)
        {
            this.Id = id;
            this.Value = value;
            this.Tile = tile;
        }
    }

    [XmlRoot("ajax")]
    public class AjaxPermissionGroupDictionary
    {
        [XmlElement("type")]
        public string AjaxType = "PermissionGroupDictionary";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlArray("array")]
        [XmlArrayItem("item")]
        public AjaxPermissionGroupDictionaryItem[] ResponseDictionary;
    }

    public class AjaxPermissionGroupDictionaryItem
    {
        [XmlElement("id")]
        public long Id;
        [XmlElement("type-id")]
        public long TypeId;
        [XmlElement("value")]
        public string Value;
        [XmlElement("tile")]
        public string Tile;

        public AjaxPermissionGroupDictionaryItem()
        {

        }

        public AjaxPermissionGroupDictionaryItem(ItemKey ik, string value, string tile)
        {
            this.Id = ik.Id;
            this.TypeId = ik.TypeId;
            this.Value = value;
            this.Tile = tile;
        }
    }
}
