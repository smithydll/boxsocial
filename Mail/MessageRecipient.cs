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
	public enum RecipientType : byte
	{
		Sender = 0x01,
		To = 0x02,
		Cc = 0x04,
		Bcc = 0x08,
	}
	
	[DataTable("mail_message_recipients")]
	public class MessageRecipient : NumberedItem
	{
		[DataField("recipient_id", DataFieldKeys.Primary)] 
		private long recipientId;
		
		public MessageRecipient(Core core, long recipientId)
            : base(core)
        {
			ItemLoad += new ItemLoadHandler(MessageRecipient_ItemLoad);

            try
            {
                LoadItem(recipientId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMessageRecipientException();
            }
        }
		
		void MessageRecipient_ItemLoad()
		{
		}
		
		public static MessageRecipient Create(Core core, Message message, User recipient, RecipientType type)
		{
			return Create(core, message, recipient, type, false);
		}
			
		public static MessageRecipient Create(Core core, Message message, User recipient, RecipientType type, bool suppress)
		{
			Item newItem = Item.Create(core, typeof(MessageRecipient), suppress, new FieldValuePair("message_id", message.Id), new FieldValuePair("user_id", recipient.Id), new FieldValuePair("recipient_type", (byte)type));
			
			if (newItem != null)
			{
				return (MessageRecipient)newItem;
			}
			else
			{
				return null;
			}
		}
		
		public override long Id
        {
            get
            {
                return recipientId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
	}
	
	public class InvalidMessageRecipientException : Exception
	{
	}
}
