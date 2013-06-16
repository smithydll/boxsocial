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
using System.Configuration;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace BoxSocial.IO
{
    public abstract class Email
    {
        abstract public void SendEmail(string toAddress, string subject, string message);
        abstract public void SendEmail(string toAddress, string subject, Template message);

        // http://msdn.microsoft.com/en-us/library/01escwtf.aspx
        bool invalid = false;

        public bool IsEmailAddress(string inviteeUsername)
        {
            invalid = false;
            if (String.IsNullOrEmpty(inviteeUsername))
                return false;

            // Use IdnMapping class to convert Unicode domain names. 
            try
            {
                inviteeUsername = Regex.Replace(inviteeUsername, @"(@)(.+)$", this.DomainMapper,
                                      RegexOptions.None/*, TimeSpan.FromMilliseconds(200)*/);
            }
            catch /*(RegexMatchTimeoutException)*/
            {
                return false;
            }

            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format. 
            try
            {
                return Regex.IsMatch(inviteeUsername,
                      @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                      RegexOptions.IgnoreCase /*, TimeSpan.FromMilliseconds(250)*/);
            }
            catch /*(RegexMatchTimeoutException)*/
            {
                return false;
            }

        }

        private string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

    }
}
