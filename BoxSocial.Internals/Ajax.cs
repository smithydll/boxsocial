/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Ajax
    {
        private Core core;

        public Ajax(Core core)
        {
            this.core = core;
        }

        /// <summary>
        /// Send a status code only the client has to work out what to do with.
        /// </summary>
        /// <param name="ajaxCode"></param>
        public void SendStatus(string ajaxCode)
        {
            XmlSerializer xs;
            StringWriter stw;

            AjaxStatus am = new AjaxStatus();
            am.ResponseCode = ajaxCode;

            xs = new XmlSerializer(typeof(AjaxStatus));

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

            AjaxRawText am = new AjaxRawText();
            am.ResponseCode = ajaxCode;
            am.ResponseMessage = message;

            xs = new XmlSerializer(typeof(AjaxRawText));
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

                AjaxMessage am = new AjaxMessage();
                am.ResponseCode = ajaxCode;
                am.ResponseTitle = title;
                am.ResponseMessage = message;

                xs = new XmlSerializer(typeof(AjaxMessage));
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

            AjaxArray am = new AjaxArray();
            am.ResponseCode = ajaxCode;
            am.ResponseArray = arrayItems;

            xs = new XmlSerializer(typeof(AjaxArray));
            stw = new StringWriter();

            core.Http.WriteXml(xs, am);

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }

        public void SendDictionary(string ajaxCode, Dictionary<long, string> arrayItems)
        {
            XmlSerializer xs;
            StringWriter stw;

            AjaxDictionary am = new AjaxDictionary();
            am.ResponseCode = ajaxCode;
            am.ResponseDictionary = arrayItems;

            xs = new XmlSerializer(typeof(AjaxDictionary));
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
    public class AjaxStatus
    {
        [XmlElement("type")]
        public string AjaxType = "Status";

        [XmlElement("code")]
        public string ResponseCode;
    }

    [XmlRoot("ajax")]
    public class AjaxRawText
    {
        [XmlElement("type")]
        public string AjaxType = "Raw";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlElement("message")]
        public string ResponseMessage;
    }

    [XmlRoot("ajax")]
    public class AjaxMessage
    {
        [XmlElement("type")]
        public string AjaxType = "Message";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlElement("title")]
        public string ResponseTitle;

        [XmlElement("message")]
        public string ResponseMessage;
    }

    [XmlRoot("ajax")]
    public class AjaxArray
    {
        [XmlElement("type")]
        public string AjaxType = "Array";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlElement("array")]
        public string[] ResponseArray;
    }

    [XmlRoot("ajax")]
    public class AjaxDictionary
    {
        [XmlElement("type")]
        public string AjaxType = "Array";

        [XmlElement("code")]
        public string ResponseCode;

        [XmlElement("array")]
        public Dictionary<long, string> ResponseDictionary;
    }
}
