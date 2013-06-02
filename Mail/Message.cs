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
    [DataTable("mail_messages")]
    public class Message : NumberedItem
    {
		[DataField("message_id", DataFieldKeys.Primary)] 
		private long messageId;
		[DataField("sender_id")]
		private long senderId;
        [DataField("message_draft")]
        private bool draft;
        [DataField("message_read")]
        private bool read;
		[DataField("message_to", MYSQL_TEXT)]
		private string to;
		[DataField("message_cc", MYSQL_TEXT)]
		private string cc;
		[DataField("message_bcc", MYSQL_TEXT)]
		private string bcc;
		[DataField("message_subject", 127)]
		private string subject;
		[DataField("message_text", MYSQL_MEDIUM_TEXT)]
		private string text;
		[DataField("message_time_ut")]
		private long messageTime;
		[DataField("message_ip", 50)]
        private string messageIp;

        private User sender;

		public long MessageId
		{
			get
			{
				return messageId;
			}
		}

        public bool Draft
        {
            get
            {
                return draft;
            }
            set
            {
                SetPropertyByRef(new { draft }, value);
            }
        }

        public bool IsRead
        {
            get
            {
                return read;
            }
            private set
            {
                SetPropertyByRef(new { read }, value);
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
                SetPropertyByRef(new { subject }, value);
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
                SetPropertyByRef(new { text }, value);
			}
		}
		
		public long MessageTimeRaw
		{
			get
			{
				return messageTime;
			}
		}

        public DateTime GetSentDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(messageTime);
        }

        public User Sender
        {
            get
            {
                if (sender == null || senderId != sender.Id)
                {
                    core.PrimitiveCache.LoadUserProfile(senderId);
                    sender = core.PrimitiveCache[senderId];
                    return sender;
                }
                else
                {
                    return sender;
                }
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

        public Message(Core core, DataRow messageRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Message_ItemLoad);

            try
            {
                loadItemInfo(messageRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMessageException();
            }
        }
		
		void Message_ItemLoad()
		{
		}
		
		public static Message Create(Core core, bool draft, string subject, string text, Dictionary<User, RecipientType> recipients)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (recipients.Count > 11) // Plus one for sender
            {
                throw new TooManyMessageRecipientsException();
            }

            long senderId = 0;
            foreach (User user in recipients.Keys)
            {
                if (recipients[user] == RecipientType.Sender)
                {
                    senderId = user.Id;
                }
            }

			Item newItem = Item.Create(core, typeof(Message),
                   new FieldValuePair("sender_id", senderId),
                   new FieldValuePair("message_subject", subject),
			       new FieldValuePair("message_text", text),
			       new FieldValuePair("message_time_ut", UnixTime.UnixTimeStamp()),
			       new FieldValuePair("message_ip", core.Session.IPAddress.ToString()),
                   new FieldValuePair("message_draft", draft),
                   new FieldValuePair("message_read", false));

            MailFolder folder = null;
            UpdateQuery uquery = null;

            List<long> recipientIds = new List<long>();
			foreach (User user in recipients.Keys)
			{
                bool incrementFolderCount = false;
                switch (recipients[user])
                {
                    case RecipientType.Sender:
                        if (draft)
                        {
                            folder = MailFolder.GetFolder(core, FolderTypes.Draft, user);
                            MessageRecipient.Create(core, (Message)newItem, user, recipients[user], folder);
                        }
                        else
                        {
                            folder = MailFolder.GetFolder(core, FolderTypes.Outbox, user);
                            MessageRecipient.Create(core, (Message)newItem, user, recipients[user], folder);
                        }
                        incrementFolderCount = true;
                        break;
                    default:
                        folder = MailFolder.GetFolder(core, FolderTypes.Inbox, user);
                        MessageRecipient.Create(core, (Message)newItem, user, recipients[user], folder);

                        if (!draft)
                        {
                            incrementFolderCount = true;
                            recipientIds.Add(user.Id);
                        }
                        break;
                }

                if (incrementFolderCount)
                {
                    uquery = new UpdateQuery(typeof(MailFolder));
                    uquery.AddCondition("folder_id", folder.Id);
                    uquery.AddField("folder_messages", new QueryOperation("folder_messages", QueryOperations.Addition, 1));

                    core.Db.Query(uquery);

                }
			}

            if (recipientIds.Count > 0)
            {
                uquery = new UpdateQuery(typeof(UserInfo));
                uquery.AddField("user_unseen_mail", new QueryOperation("user_unseen_mail", QueryOperations.Addition, 1));
                uquery.AddCondition("user_id", ConditionEquality.In, recipientIds);

                core.Db.Query(uquery);
            }
			
			return (Message)newItem;
		}

        public void AddRecipient(User user, RecipientType type)
        {
            bool incrementFolderCount = false;
            MailFolder folder = null;
            switch (type)
            {
                case RecipientType.Sender:
                    if (draft)
                    {
                        folder = MailFolder.GetFolder(core, FolderTypes.Draft, user);
                        MessageRecipient.Create(core, this, user, type, folder);
                    }
                    else
                    {
                        folder = MailFolder.GetFolder(core, FolderTypes.Outbox, user);
                        MessageRecipient.Create(core, this, user, type, folder);
                    }
                    incrementFolderCount = true;
                    break;
                default:
                    folder = MailFolder.GetFolder(core, FolderTypes.Inbox, user);
                    MessageRecipient.Create(core, this, user, type, folder);

                    if (!draft)
                    {
                        incrementFolderCount = true;
                    }
                    break;
            }

            if (incrementFolderCount)
            {
                UpdateQuery uquery = new UpdateQuery(typeof(MailFolder));
                uquery.AddCondition("folder_id", folder.Id);
                uquery.AddField("folder_messages", new QueryOperation("folder_messages", QueryOperations.Addition, 1));

                core.Db.Query(uquery);
            }
        }

        public void RemoveRecipient(User user, RecipientType type)
        {
        }

        public void MarkRead()
        {
            if (core.Session.LoggedInMember.Id != SenderId)
            {
                if (!IsRead)
                {
                    MailFolder senderOutbox = MailFolder.GetFolder(core, FolderTypes.Outbox, Sender);
                    MailFolder senderSentItems = MailFolder.GetFolder(core, FolderTypes.SentItems, Sender);

                    db.BeginTransaction();
                    UpdateQuery uquery = new UpdateQuery(typeof(MessageRecipient));
                    uquery.AddCondition("message_id", Id);
                    uquery.AddCondition("user_id", SenderId);
                    uquery.AddField("message_folder_id", senderSentItems.Id);

                    db.Query(uquery);

                    uquery = new UpdateQuery(typeof(MailFolder));
                    uquery.AddCondition("folder_id", senderOutbox.Id);
                    uquery.AddField("folder_messages", new QueryOperation("folder_messages", QueryOperations.Subtraction, 1));

                    db.Query(uquery);

                    uquery = new UpdateQuery(typeof(MailFolder));
                    uquery.AddCondition("folder_id", senderSentItems.Id);
                    uquery.AddField("folder_messages", new QueryOperation("folder_messages", QueryOperations.Addition, 1));

                    db.Query(uquery);

                    this.IsRead = true;
                    this.Update();
                }
            }
        }

        public List<MessageRecipient> GetRecipients()
        {
            List<Item> items = getSubItems(typeof(MessageRecipient), false);

            List<MessageRecipient> recipients = new List<MessageRecipient>();

            /*foreach (Item item in items)
            {
                core.PrimitiveCache.LoadUserProfile(((MessageRecipient)item).UserId);
            }*/

            foreach (Item item in items)
            {
                //recipients.Add(core.PrimitiveCache[((MessageRecipient)item).UserId], ((MessageRecipient)item).RecipientType);
                recipients.Add((MessageRecipient)item);
            }

            return recipients;
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
                return core.Hyperlink.BuildAccountSubModuleUri("mail", "read", messageId);
            }
        }
    }
	
	public class InvalidMessageException : Exception
	{
	}

    public class TooManyMessageRecipientsException : Exception
    {
    }
}
