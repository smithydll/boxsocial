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
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// We are going to implement OAuth 1.0a because OAuth 2.0 is a framework, not a protocol?
    /// It would therefore be easier to write an insecure OAuth 2.0 implementation than OAuth 1.0a implementation.
    /// </summary>
    public class OAuth
    {
        public static string GeneratePublic()
        {
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            byte[] randomData = new byte[16];

            rand.GetBytes(randomData);

            return Convert.ToBase64String(randomData).TrimEnd(new char[] { '=' });
        }

        public static string GenerateSecret()
        {
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            byte[] randomData = new byte[32];

            rand.GetBytes(randomData);

            return Convert.ToBase64String(randomData).TrimEnd(new char[] { '=' });
        }

        public static string ComputeSignature(string baseString, string keyString)
        {
            byte[] keyBytes = UTF8Encoding.UTF8.GetBytes(keyString);

            HMACSHA1 sha1 = new HMACSHA1(keyBytes);
            sha1.Initialize();

            byte[] baseBytes = UTF8Encoding.UTF8.GetBytes(baseString);

            byte[] text = sha1.ComputeHash(baseBytes);

            string signature = Convert.ToBase64String(text).Trim();

            return signature;
        }

        public static string UrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            Regex reg = new Regex(@"%[a-f0-9]{2}");

            return reg.Replace(HttpUtility.UrlEncode(value), m => m.Value.ToUpperInvariant()).Replace("+", "%20").Replace("*", "%2A").Replace("!", "%21").Replace("(", "%28").Replace(")", "%29");
        }
    }
}
