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
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Email
    /// </summary>
    public class Email
    {
        private Core core;

        public Email(Core core)
        {
            this.core = core;
        }

        public void SendEmail(string toAddress, string subject, string message)
        {
			if (WebConfigurationManager.AppSettings == null || (!WebConfigurationManager.AppSettings.HasKeys()) || WebConfigurationManager.AppSettings["smtp-server"] == null)
			{
				return;
			}
			
            SmtpClient mailClient = new SmtpClient(WebConfigurationManager.AppSettings["smtp-server"]);
            Type t = Type.GetType("Mono.Runtime");
            if (t == null)
            {
                // Not needed for mono
                mailClient.UseDefaultCredentials = true;
            }
            //mailClient.c
            //SmtpMail.SmtpServer = SMTP_SERVER;

            MailMessage newMessage = new MailMessage(new MailAddress(WebConfigurationManager.AppSettings["email"], core.Settings.SiteTitle), new MailAddress(toAddress));
            newMessage.Subject = subject;
            newMessage.IsBodyHtml = false;
            newMessage.Body = message;

            newMessage.Headers.Add("X-AntiAbuse", "servername - " + Hyperlink.Domain);
            if (core != null)
            {
                if (core.Session.LoggedInMember != null)
                {
                    if (core.Session.IsLoggedIn)
                    {
                        newMessage.Headers.Add("X-AntiAbuse", "User_id - " + core.Session.LoggedInMember.UserId.ToString());
                        newMessage.Headers.Add("X-AntiAbuse", "Username - " + core.Session.LoggedInMember.UserName);
                    }
                }
                newMessage.Headers.Add("X-AntiAbuse", "User IP - " + core.Session.IPAddress.ToString());
            }

            try
            {
                mailClient.Send(newMessage);
            }
            catch (Exception ex)
            {
                // Do not show e-mail errors
                //Display.ShowMessage("Error sending e-mail", ex.ToString());
            }
            /*mailClient.SendAsync(newMessage, null);
            mailClient.SendCompleted += new SendCompletedEventHandler(mailClient_SendCompleted);*/
        }

        static void mailClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {

        }

        public Email()
        {
        }

        public static bool IsEmailAddress(string inviteeUsername)
        {
            throw new NotImplementedException();
        }
    }
}
