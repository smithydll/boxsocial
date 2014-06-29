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
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace BoxSocial.IO
{
    public class Http
    {
        
        HttpContext current;

        private bool forceDomain;
        
        public Http()
        {
            current = HttpContext.Current;
            forceDomain = false;
            //HttpContext.Current = null;
        }
        
        public void SetToImageResponse(string contextType, DateTime lastModified)
        {
            current.Response.Clear();
            current.Response.ContentType = contextType;
            current.Response.Cache.SetLastModified(lastModified);
            current.Response.Cache.SetExpires(DateTime.Now.AddDays(1));
            current.Response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
        }
        
        public void SwitchContextType(string contextType)
        {
            current.Response.Clear();
            current.Response.ContentType = contextType;
        }
        
        public void WriteXml(XmlSerializer serializer, object obj)
        {
            SwitchContextType("text/xml");
            HttpContext.Current.Response.ContentEncoding = Encoding.UTF8;
            
            StringWriter sw = new StringWriter();
            
            serializer.Serialize(sw, obj);
            
            Write(sw.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
        }
        
        public void TransmitFile(string fileName)
        {
            current.Response.TransmitFile(fileName);
        }
        
        public void WriteStream(System.IO.MemoryStream stream)
        {
            stream.WriteTo(HttpContext.Current.Response.OutputStream);
        }
        
        internal void Write(string input)
        {
            current.Response.Write(input);
        }
        
        public void Write(Template template)
        {
            Write(template.ToString());
        }
        
        public void WriteAndEndResponse(Template template)
        {
            Write(template);
            End();
        }
        
        public void End()
        {
            current.Response.End();
        }
        
        public string this[string key]
        {
            get
            {
                if (Form[key] != null)
                {
                    return Form[key];
                }
                else
                {
                    return Query[key];
                }
            }
        }
        
        public NameValueCollection Form
        {
            get
            {
                return current.Request.Form;
            }
        }

        public List<string> FormArray(string var)
        {
            List<string> array = new List<string>();

            foreach (string value in current.Request.Form)
            {
                if (value.StartsWith(var + "[") && value.EndsWith("]"))
                {
                    array.Add(value.Substring(var.Length + 1, value.Length - var.Length - 2));
                }
            }

            return array;
        }

        public string HttpMethod
        {
            get
            {
                return current.Request.HttpMethod;
            }
        }

        public string UrlReferer
        {
            get
            {
                return current.Request.UrlReferrer.ToString();
            }
        }
        
        public NameValueCollection Query
        {
            get
            {
                return current.Request.QueryString;
            }
        }
        
        public HttpFileCollection Files
        {
            get
            {
                return current.Request.Files;
            }
        }
        
        public HttpCookie GetCookieValue(string key)
        {
            return current.Request.Cookies[key];
        }
        
        public void SetCookieValue(HttpCookie cookie)
        {
            current.Response.Cookies.Add(cookie);
        }

        public string RawUrl
        {
            get
            {
                return current.Request.RawUrl;
            }
        }
        
        public string Status
        {
            get
            {
                return current.Response.Status;
            }
            set
            {
                current.Response.Status = value;
            }
        }
        
        public int StatusCode
        {
            get
            {
                return current.Response.StatusCode;
            }
            set
            {
                current.Response.StatusCode = value;
            }
        }
        
        public string StatusDescription
        {
            get
            {
                return current.Response.StatusDescription;
            }
            set
            {
                current.Response.StatusDescription = value;
            }
        }
        
        public string AssemblyPath
        {
            get
            {
                return current.Server.MapPath("./bin/");
            }
        }
        
        public string LanguagePath
        {
            get
            {
                return current.Server.MapPath("./language/");
            }
        }
        
        public string TemplatePath
        {
            get
            {
                return current.Server.MapPath("./templates/");
            }
        }
        
        public string TemplateEmailPath
        {
            get
            {
                return current.Server.MapPath("./templates/emails/");
            }
        }
        
        public string Domain
        {
            get
            {
                return current.Request.Url.Host.ToLower();
            }
        }
        
        public void Redirect(string location)
        {
            current.Response.Redirect(location);
        }

        public string MapPath(string path)
        {
            return current.Server.MapPath(path);
        }

        public bool IsSecure
        {
            get
            {
                return current.Request.IsSecureConnection;
            }
        }

        public string DefaultProtocol
        {
            get
            {
                if (IsSecure)
                {
                    return "https://";
                }
                else
                {
                    return "http://";
                }
            }
        }

        public bool ForceDomain
        {
            get
            {
                return forceDomain;
            }
            set
            {
                forceDomain = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return current.Request.UserAgent;
            }
        }
    }
}
