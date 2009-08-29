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
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace BoxSocial.IO
{
    public class Http
    {
        
        HttpContext current;
        
        public Http()
        {
            current = HttpContext.Current;
            HttpContext.Current = null;
        }
        
        public void SetToImageResponse(string contextType, DateTime lastModified)
        {
            current.Response.Clear();
            current.Response.ContentType = contextType;
            current.Response.Cache.SetLastModified(lastModified);
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
    }
}
