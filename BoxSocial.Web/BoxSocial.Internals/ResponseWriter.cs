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
    public abstract class ResponseWriter
    {
        protected Core core;

        public ResponseWriter(Core core)
        {
            this.core = core;
        }

        public abstract void WriteObject(object obj);

        /// <summary>
        /// Send a status code only the client has to work out what to do with.
        /// </summary>
        /// <param name="ajaxCode"></param>
        public void SendStatus(string ajaxCode)
        {
            ResponseStatus am = new ResponseStatus();
            am.ResponseCode = ajaxCode;

            WriteObject(am);
        }

        /// <summary>
        /// Send raw text the client has to work out what to do with.
        /// </summary>
        /// <param name="ajaxCode"></param>
        /// <param name="core"></param>
        /// <param name="message"></param>
        public void SendRawText(string ajaxCode, string message)
        {
            ResponseRawText am = new ResponseRawText();
            am.ResponseCode = ajaxCode;
            am.ResponseMessage = message;

            WriteObject(am);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ajax">True if to response using AJAX, false to respond conventionally.</param>
        /// <param name="ajaxCode"></param>
        /// <param name="core"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void ShowMessage(string ajaxCode, string title, string message)
        {
            ResponseMessage am = new ResponseMessage();
            am.ResponseCode = ajaxCode;
            am.ResponseTitle = title;
            am.Message = message;

            WriteObject(am);
        }

        /// <summary>
        /// Send an array of items for the client to process
        /// </summary>
        /// <param name="ajaxCore"></param>
        /// <param name="core"></param>
        /// <param name="arrayItems"></param>
        public void SendArray(string ajaxCode, string[] arrayItems)
        {
            ResponseArray am = new ResponseArray();
            am.ResponseCode = ajaxCode;
            am.Array = arrayItems;

            WriteObject(am);
        }

        public void SendDictionary(string ajaxCode, Dictionary<string, string> arrayItems)
        {
            ResponseDictionary am = new ResponseDictionary();
            am.ResponseCode = ajaxCode;

            List<ResponseDictionaryItem> ajaxArrayItems = new List<ResponseDictionaryItem>();

            foreach (string key in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new ResponseDictionaryItem(key, arrayItems[key]));
            }

            am.Dictionary = ajaxArrayItems.ToArray();

            WriteObject(am);
        }

        public void SendUserDictionary(string ajaxCode, Dictionary<long, string[]> arrayItems)
        {
            AjaxUserDictionary am = new AjaxUserDictionary();
            am.ResponseCode = ajaxCode;

            List<AjaxUserDictionaryItem> ajaxArrayItems = new List<AjaxUserDictionaryItem>();

            foreach (long id in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new AjaxUserDictionaryItem(id, arrayItems[id][0], arrayItems[id][1]));
            }

            am.ResponseDictionary = ajaxArrayItems.ToArray();

            WriteObject(am);
        }

        public void SendPermissionGroupDictionary(string ajaxCode, Dictionary<ItemKey, string[]> arrayItems)
        {
            AjaxPermissionGroupDictionary am = new AjaxPermissionGroupDictionary();
            am.ResponseCode = ajaxCode;

            List<AjaxPermissionGroupDictionaryItem> ajaxArrayItems = new List<AjaxPermissionGroupDictionaryItem>();

            foreach (ItemKey ik in arrayItems.Keys)
            {
                ajaxArrayItems.Add(new AjaxPermissionGroupDictionaryItem(ik, arrayItems[ik][0], arrayItems[ik][1]));
            }

            am.ResponseDictionary = ajaxArrayItems.ToArray();

            WriteObject(am);
        }
    }

    [XmlRoot("ajax")]
    [JsonObject("response_status")]
    public sealed class ResponseStatus
    {
        [XmlElement("type")]
        [JsonProperty("type")]
        public string AjaxType = "Status";

        [XmlElement("code")]
        [JsonProperty("code")]
        public string ResponseCode;
    }

    [XmlRoot("ajax")]
    [JsonObject("response_raw")]
    public sealed class ResponseRawText
    {
        [XmlElement("type")]
        [JsonProperty("type")]
        public string AjaxType = "Raw";

        [XmlElement("code")]
        [JsonProperty("code")]
        public string ResponseCode;

        [XmlElement("message")]
        [JsonProperty("message")]
        public string ResponseMessage;
    }

    [XmlRoot("ajax")]
    [JsonObject("response_message")]
    public sealed class ResponseMessage
    {
        [XmlElement("type")]
        [JsonProperty("type")]
        public string AjaxType = "Message";

        [XmlElement("code")]
        [JsonProperty("code")]
        public string ResponseCode;

        [XmlElement("title")]
        [JsonProperty("title")]
        public string ResponseTitle;

        [XmlElement("message")]
        [JsonProperty("message")]
        public string Message;
    }

    [XmlRoot("ajax")]
    [JsonObject("response_array")]
    public sealed class ResponseArray
    {
        [XmlElement("type")]
        [JsonProperty("type")]
        public string AjaxType = "Array";

        [XmlElement("code")]
        [JsonProperty("code")]
        public string ResponseCode;

        [XmlElement("array")]
        [JsonProperty("array")]
        public string[] Array;
    }

    [XmlRoot("ajax")]
    [JsonObject("response_dictionary")]
    public sealed class ResponseDictionary
    {
        [XmlElement("type")]
        [JsonProperty("type")]
        public string AjaxType = "Dictionary";

        [XmlElement("code")]
        [JsonProperty("code")]
        public string ResponseCode;

        [XmlArray("array")]
        [XmlArrayItem("item")]
        [JsonProperty("dictionary")]
        public ResponseDictionaryItem[] Dictionary;
    }

    public sealed class ResponseDictionaryItem
    {
        [XmlElement("key")]
        [JsonProperty("key")]
        public string Key;
        [XmlElement("value")]
        [JsonProperty("value")]
        public string Value;

        public ResponseDictionaryItem()
        {

        }

        public ResponseDictionaryItem(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
