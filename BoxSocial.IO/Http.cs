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
using System.Collections.Generic;
using System.Text;
using System.Web;

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
                if (GetFormValue(key) != null)
                {
                    return GetFormValue(key);
                }
                else
                {
                    return GetQueryValue(key);
                }
            }
        }
        
        public string GetFormValue(string key)
        {
            return current.Request.Form[key];
        }
        
        public string GetQueryValue(string key)
        {
            return current.Request.QueryString[key];
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
    }
}
