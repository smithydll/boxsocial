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
        private static Core core;

        public static Core Core
        {
            set
            {
                core = value;
            }
        }

        public static void SendEmail(string toAddress, string subject, string message)
        {
            SmtpClient mailClient = new SmtpClient(WebConfigurationManager.AppSettings["smtp-server"]);
            Type t = Type.GetType("Mono.Runtime");
            if (t == null)
            {
                // Not needed for mono
                mailClient.UseDefaultCredentials = true;
            }
            //mailClient.c
            //SmtpMail.SmtpServer = SMTP_SERVER;

            MailMessage newMessage = new MailMessage(new MailAddress(WebConfigurationManager.AppSettings["email"], "ZinZam"), new MailAddress(toAddress));
            newMessage.Subject = subject;
            newMessage.IsBodyHtml = false;
            newMessage.Body = message;

            newMessage.Headers.Add("X-AntiAbuse", "servername - zinzam.com");
            if (core != null)
            {
                if (core.session.LoggedInMember != null)
                {
                    if (core.session.IsLoggedIn)
                    {
                        newMessage.Headers.Add("X-AntiAbuse", "User_id - " + core.session.LoggedInMember.UserId.ToString());
                        newMessage.Headers.Add("X-AntiAbuse", "Username - " + core.session.LoggedInMember.UserName);
                    }
                }
                newMessage.Headers.Add("X-AntiAbuse", "User IP - " + core.session.IPAddress.ToString());
            }

            try
            {
                mailClient.Send(newMessage);
            }
            catch (Exception ex)
            {
                Display.ShowMessage("Error sending e-mail", ex.ToString());
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
    }
}
