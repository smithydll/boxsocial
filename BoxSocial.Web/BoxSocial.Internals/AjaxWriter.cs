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

        /// <summary>
        /// Send a status code only the client has to work out what to do with.
        /// </summary>
        /// <param name="ajaxCode"></param>
        public void SendStatus(string ajaxCode)
        {
            XmlSerializer xs;
            StringWriter stw;

            ResponseStatus am = new ResponseStatus();
            am.ResponseCode = ajaxCode;

            xs = new XmlSerializer(typeof(ResponseStatus));

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }
            core.Http.End();
        }

        /// <summary>
        /// Send raw text the client has to work out what to do with.
        /// </summary>
        /// <param name="ajaxCode"></param>
        /// <param name="core"></param>
        /// <param name="message"></param>
        public void SendRawText(string ajaxCode, string message)
        {
            XmlSerializer xs;
            StringWriter stw;

            ResponseRawText am = new ResponseRawText();
            am.ResponseCode = ajaxCode;
            am.ResponseMessage = message;

            xs = new XmlSerializer(typeof(ResponseRawText));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ajax">True if to response using AJAX, false to respond conventionally.</param>
        /// <param name="ajaxCode"></param>
        /// <param name="core"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void ShowMessage(bool ajax, string ajaxCode, string title, string message)
        {
            if (ajax)
            {
                XmlSerializer xs;
                StringWriter stw;

                ResponseMessage am = new ResponseMessage();
                am.ResponseCode = ajaxCode;
                am.ResponseTitle = title;
                am.Message = message;

                xs = new XmlSerializer(typeof(ResponseMessage));
                stw = new StringWriter();

                core.Http.WriteXml(xs, am);

                if (core.Db != null)
                {
                    core.Db.CloseConnection();
                }

                core.Http.End();
            }
            else
            {
                core.Display.ShowMessage(title, message);
            }
        }

        /// <summary>
        /// Send an array of items for the client to process
        /// </summary>
        /// <param name="ajaxCore"></param>
        /// <param name="core"></param>
        /// <param name="arrayItems"></param>
        public void SendArray(string ajaxCode, string[] arrayItems)
        {
            XmlSerializer xs;
            StringWriter stw;

            ResponseArray am = new ResponseArray();
            am.ResponseCode = ajaxCode;
            am.Array = arrayItems;

            xs = new XmlSerializer(typeof(ResponseArray));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }

        public void SendDictionary(string ajaxCode, Dictionary<string, string> arrayItems)
        {
            XmlSerializer xs;
            StringWriter stw;

            ResponseDictionary am = new ResponseDictionary();
            am.ResponseCode = ajaxCode;

            List<ResponseDictionaryItem> ajaxArrayItems = new List<ResponseDictionaryItem>();

            foreach (string key in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new ResponseDictionaryItem(key, arrayItems[key]));
            }

            am.Dictionary = ajaxArrayItems.ToArray();

            xs = new XmlSerializer(typeof(ResponseDictionary));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }

        public void SendUserDictionary(string ajaxCode, Dictionary<long, string[]> arrayItems)
        {
            XmlSerializer xs;
            StringWriter stw;

            AjaxUserDictionary am = new AjaxUserDictionary();
            am.ResponseCode = ajaxCode;

            List<AjaxUserDictionaryItem> ajaxArrayItems = new List<AjaxUserDictionaryItem>();

            foreach (long id in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new AjaxUserDictionaryItem(id, arrayItems[id][0], arrayItems[id][1]));
            }

            am.ResponseDictionary = ajaxArrayItems.ToArray();

            xs = new XmlSerializer(typeof(AjaxUserDictionary));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }

        public void SendPermissionGroupDictionary(string ajaxCode, Dictionary<ItemKey, string[]> arrayItems)
        {
            XmlSerializer xs;
            StringWriter stw;

            AjaxPermissionGroupDictionary am = new AjaxPermissionGroupDictionary();
            am.ResponseCode = ajaxCode;

            List<AjaxPermissionGroupDictionaryItem> ajaxArrayItems = new List<AjaxPermissionGroupDictionaryItem>();

            foreach (ItemKey ik in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new AjaxPermissionGroupDictionaryItem(ik, arrayItems[ik][0], arrayItems[ik][1]));
            }

            am.ResponseDictionary = ajaxArrayItems.ToArray();

            xs = new XmlSerializer(typeof(AjaxPermissionGroupDictionary));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

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
