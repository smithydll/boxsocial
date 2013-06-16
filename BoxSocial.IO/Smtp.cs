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

namespace BoxSocial.IO
{
    /// <summary>
    /// Summary description for Email
    /// </summary>
    public class Smtp : Email
    {
        private string domain;
        private string smtpServer;
        private long userId;
        private string username;
        private string ipAddress;
        private string fromEmail;
        private string fromName;

        public Smtp(string domain, string smtpServer, long userId, string username, string ipAddress, string fromEmail, string fromName)
        {
            this.domain = domain;
            this.smtpServer = smtpServer;
            this.userId = userId;
            this.username = username;
            this.ipAddress = ipAddress;
            this.fromEmail = fromEmail;
            this.fromName = fromName;
        }

        override public void SendEmail(string toAddress, string subject, string message)
        {
            SmtpClient mailClient = new SmtpClient(smtpServer);
            Type t = Type.GetType("Mono.Runtime");
            if (t == null)
            {
                // Not needed for mono
                mailClient.UseDefaultCredentials = true;
            }
            //mailClient.c
            //SmtpMail.SmtpServer = SMTP_SERVER;

            MailMessage newMessage = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(toAddress, username));
            newMessage.Subject = subject;
            newMessage.IsBodyHtml = false;
            newMessage.Body = message;

            newMessage.Headers.Add("X-AntiAbuse", "servername - " + domain);
            if (!string.IsNullOrEmpty(ipAddress))
            {
                if (userId > 0)
                {
                    newMessage.Headers.Add("X-AntiAbuse", "User_id - " + userId.ToString());
                    newMessage.Headers.Add("X-AntiAbuse", "Username - " + username);
                }
                newMessage.Headers.Add("X-AntiAbuse", "User IP - " + ipAddress);
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

        override public void SendEmail(string toAddress, string subject, Template message)
        {
            SmtpClient mailClient = new SmtpClient(smtpServer);
            Type t = Type.GetType("Mono.Runtime");
            if (t == null)
            {
                // Not needed for mono
                mailClient.UseDefaultCredentials = true;
            }
            //mailClient.c
            //SmtpMail.SmtpServer = SMTP_SERVER;

            MailMessage newMessage = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(toAddress, username));
            newMessage.Subject = subject;
            newMessage.IsBodyHtml = true;
            newMessage.Body = message.ToString();

            newMessage.Headers.Add("X-AntiAbuse", "servername - " + domain);
            if (!string.IsNullOrEmpty(ipAddress))
            {
                if (userId > 0)
                {
                    newMessage.Headers.Add("X-AntiAbuse", "User_id - " + userId.ToString());
                    newMessage.Headers.Add("X-AntiAbuse", "Username - " + username);
                }
                newMessage.Headers.Add("X-AntiAbuse", "User IP - " + ipAddress);
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
    }
}
