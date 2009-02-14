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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Mail
{
    [DataTable("mail_messages")]
    public class Message : NumberedItem
    {
		[DataField("message_id", DataFieldKeys.Primary)] 
		private long messageId;
		[DataField("sender_id")]
		private long senderId;
		[DataField("message_subject", 127)]
		private string subject;
		[DataField("message_text", MYSQL_MEDIUM_TEXT)]
		private string text;
		[DataField("message_time_ut")]
		private long messageTime;
		[DataField("message_ip", 50)]
        private string messageIp;
		
		public long MessageId
		{
			get
			{
				return messageId;
			}
		}
		
		public long SenderId
		{
			get
			{
				return senderId;
			}
		}
		
		public string Subject
		{
			get
			{
				return subject;
			}
			set
			{
				SetProperty("subject", value);
			}
		}
		
		public string Text
		{
			get
			{
				return text;
			}
			set
			{
				SetProperty("text", value);
			}
		}
		
		public long MessageTimeRaw
		{
			get
			{
				return messageTime;
			}
		}
		
		public DateTime GetMessageTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(messageTime);
        }

        public Message(Core core, long messageId)
            : base(core)
        {
			ItemLoad += new ItemLoadHandler(Message_ItemLoad);

            try
            {
                LoadItem(messageId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMessageException();
            }
        }
		
		void Message_ItemLoad()
		{
		}
		
		public static Message Create(Core core, string subject, string text, Dictionary<User, RecipientType> recipients)
		{
			Item newItem = Item.Create(core, typeof(Message),
			                           new FieldValuePair("message_subject", subject),
			                           new FieldValuePair("message_text", text),
			                           new FieldValuePair("message_time_ut", UnixTime.UnixTimeStamp()),
			                           new FieldValuePair("message_ip", core.session.IPAddress.ToString()));
			
			foreach (User user in recipients.Keys)
			{
				MessageRecipient.Create(core, (Message)newItem, user, recipients[user], true);
			}
			
			return (Message)newItem;
		}

        public override long Id
        {
            get
            {
                return messageId;
            }
        }

        public override string Uri
        {
            get
            {
				return Linker.BuildAccountSubModuleUri("mail", "read", messageId);
            }
        }
    }
	
	public class InvalidMessageException : Exception
	{
	}
}
