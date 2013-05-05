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
	public class MessageRecipient : Item
	{
		[DataField("message_id", typeof(Message), DataFieldKeys.Index)] 
		private long messageId;
		[DataField("user_id", typeof(User), DataFieldKeys.Index)]
		private long userId;
		[DataField("sender_id")]
		private long senderId;
		[DataField("is_deleted")]
		private bool isDeleted;
		[DataField("is_read")]
		private bool isRead;
		[DataField("has_replied")]
		private bool hasReplied;
		[DataField("is_flagged")]
		private bool isFlagged;
		[DataField("has_forwarded")]
		private bool hasForwarded;
		[DataField("message_folder_id")]
		private long messageFolderId;
        [DataField("recipient_type")]
        private byte recipientType;
        [DataField("recipient_read_time_ut")]
        private long readTime;

        public long MessageId
        {
            get
            {
                return messageId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public long SenderId
        {
            get
            {
                return senderId;
            }
        }

        public bool IsRead
        {
            get
            {
                return isRead;
            }
        }

        public long ReadTimeRaw
        {
            get
            {
                return readTime;
            }
        }

        public RecipientType RecipientType
        {
            get
            {
                return (RecipientType)recipientType;
            }
        }
		
		public MessageRecipient(Core core, DataRow recipientRow)
            : base(core)
        {
			ItemLoad += new ItemLoadHandler(MessageRecipient_ItemLoad);

            try
            {
                loadItemInfo(recipientRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMessageRecipientException();
            }
        }

        public MessageRecipient(Core core, User recipient, long messageId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MessageRecipient_ItemLoad);

            try
            {
                LoadItem(new FieldValuePair("user_id", recipient.Id), new FieldValuePair("message_id", messageId));
            }
            catch (InvalidItemException)
            {
                throw new InvalidMessageRecipientException();
            }
        }
		
		void MessageRecipient_ItemLoad()
		{
		}

		public static void Create(Core core, Message message, User recipient, RecipientType type, MailFolder folder)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			Item.Create(core, typeof(MessageRecipient), true,
                new FieldValuePair("message_id", message.Id),
                new FieldValuePair("sender_id", message.SenderId),
                new FieldValuePair("user_id", recipient.Id),
                new FieldValuePair("message_folder_id", folder.Id),
                new FieldValuePair("recipient_type", (byte)type));

		}

        public void MarkRead()
        {
            UpdateQuery uquery = new UpdateQuery(typeof(MessageRecipient));
            uquery.AddField("is_read", true);
            uquery.AddField("recipient_read_time_ut", UnixTime.UnixTimeStamp());
            uquery.AddCondition("message_id", messageId);
            uquery.AddCondition("user_id", userId);

            db.Query(uquery);
        }

		public override string Uri {
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
