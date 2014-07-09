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
        [DataField("message_time_last_ut")]
        private long messageTimeLast;
		[DataField("message_ip", 50)]
        private string messageIp;
        [DataField("message_thread_start_id")]
        private long messageThreadStartId;
        [DataField("message_last_id")]
        private long messagelastId;
        [DataField("message_replies")]
        private long messageReplies;

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

        public long LastId
        {
            get
            {
                return messagelastId;
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

        public DateTime GetLastMessageDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(messageTimeLast);
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

        public static Message Reply(Core core, User sender, Message threadStart, string text)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            long now = UnixTime.UnixTimeStamp();

            core.Db.BeginTransaction();

            Message newItem = (Message)Item.Create(core, typeof(Message),
                   new FieldValuePair("sender_id", sender.Id),
                   new FieldValuePair("message_subject", "RE: " + threadStart.Subject),
                   new FieldValuePair("message_text", text),
                   new FieldValuePair("message_time_ut", now),
                   new FieldValuePair("message_ip", core.Session.IPAddress.ToString()),
                   new FieldValuePair("message_draft", false),
                   new FieldValuePair("message_read", false),
                   new FieldValuePair("message_thread_start_id", threadStart.Id));

            {
                UpdateQuery uQuery = new UpdateQuery(typeof(Message));
                uQuery.AddField("message_time_last_ut", now);
                uQuery.AddField("message_replies", new QueryOperation("message_replies", QueryOperations.Addition, 1));
                uQuery.AddField("message_last_id", newItem.Id);
                uQuery.AddCondition("message_id", threadStart.Id);

                core.Db.Query(uQuery);
            }

            MailFolder folder = null;
            UpdateQuery uquery = null;

            List<MessageRecipient> recipients  = threadStart.GetRecipients();

            List<long> recipientIds = new List<long>();
            foreach (MessageRecipient user in recipients)
            {
                if (user.UserId == sender.Id)
                {
                    folder = MailFolder.GetFolder(core, FolderTypes.Outbox, user);
                    MessageRecipient.Create(core, newItem, user, RecipientType.Sender, folder);
                }
                else
                {
                    folder = MailFolder.GetFolder(core, FolderTypes.Inbox, user);
                    MessageRecipient.Create(core, newItem, user, RecipientType.To, folder);
                    recipientIds.Add(user.UserId);
                }

                uquery = new UpdateQuery(typeof(MailFolder));
                uquery.AddCondition("folder_id", folder.Id);
                uquery.AddField("folder_messages", new QueryOperation("folder_messages", QueryOperations.Addition, 1));

                core.Db.Query(uquery);

            }

            if (recipientIds.Count > 0)
            {
                uquery = new UpdateQuery(typeof(UserInfo));
                uquery.AddField("user_unseen_mail", new QueryOperation("user_unseen_mail", QueryOperations.Addition, 1));
                uquery.AddCondition("user_id", ConditionEquality.In, recipientIds);

                core.Db.Query(uquery);

                // Send notifications
            }

            core.CallingApplication.QueueNotifications(core, newItem.ItemKey, "notifyMessage");

            return newItem;
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

            long now = UnixTime.UnixTimeStamp();
			Message newItem = (Message)Item.Create(core, typeof(Message),
                   new FieldValuePair("sender_id", senderId),
                   new FieldValuePair("message_subject", subject),
			       new FieldValuePair("message_text", text),
			       new FieldValuePair("message_time_ut", now),
			       new FieldValuePair("message_ip", core.Session.IPAddress.ToString()),
                   new FieldValuePair("message_draft", draft),
                   new FieldValuePair("message_read", false),
                   new FieldValuePair("message_time_last_ut", now));

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
                            MessageRecipient.Create(core, newItem, user, recipients[user], folder);
                        }
                        else
                        {
                            folder = MailFolder.GetFolder(core, FolderTypes.Outbox, user);
                            MessageRecipient.Create(core, newItem, user, recipients[user], folder);
                        }
                        incrementFolderCount = true;
                        break;
                    default:
                        folder = MailFolder.GetFolder(core, FolderTypes.Inbox, user);
                        MessageRecipient.Create(core, newItem, user, recipients[user], folder);

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

                // Send notifications
            }

            core.CallingApplication.QueueNotifications(core, newItem.ItemKey, "notifyMessage");
			
			return newItem;
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
            if (core.Session.SignedIn && (SenderId == core.Session.LoggedInMember.Id || user.Id == core.Session.LoggedInMember.Id))
            {
                DeleteQuery dQuery = new DeleteQuery(typeof(MessageRecipient));
                dQuery.AddCondition("message_id", Id);
                dQuery.AddCondition("user_id", user.Id);
                if (type != RecipientType.Any)
                {
                    dQuery.AddCondition("recipient_type", (byte)type);
                }
                db.Query(dQuery);
            }
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

        public List<Message> GetMessages()
        {
            return GetMessages(0, false);
        }

        public List<Message> GetMessages(long lastId, bool newer)
        {
            List<Message> messages = new List<Message>();

            SelectQuery query = MessageRecipient.GetSelectQueryStub(typeof(MessageRecipient));
            query.AddFields(Item.GetFieldsPrefixed(typeof(Message)));
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(MessageRecipient), "message_id"), new DataField(typeof(Message), "message_id"));
            query.AddCondition("message_thread_start_id", Id);
            query.AddCondition("user_id", core.LoggedInMemberId);
            if (lastId > 0)
            {
                query.AddCondition(new DataField(typeof(Message), "message_id"), (newer ? ConditionEquality.GreaterThan : ConditionEquality.LessThan), lastId);
            }
            query.AddSort(SortOrder.Descending, "message_time_ut");
            /*query.LimitStart = (page - 1) * perPage;*/
            query.LimitCount = 10;
            query.LimitOrder = SortOrder.Descending;

            DataTable messagesDataTable = db.Query(query);

            foreach (DataRow row in messagesDataTable.Rows)
            {
                messages.Add(new Message(core, row));
            }

            return messages;
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
                if (messageThreadStartId == 0)
                {
                    return core.Hyperlink.BuildAccountSubModuleUri("mail", "message", messageId);
                }
                else
                {
                    return core.Hyperlink.BuildAccountSubModuleUri("mail", "message", messageThreadStartId) + "#" + messageId.ToString();
                }
            }
        }

        public static void NotifyMessage(Core core, Job job)
        {
            Message ev = new Message(core, job.ItemId);

            List<MessageRecipient> recipients = ev.GetRecipients();

            foreach (MessageRecipient recipient in recipients)
            {
                core.LoadUserProfile(recipient.UserId);
            }

            foreach (MessageRecipient recipient in recipients)
            {
                // TODO: notify everyone via push notifications

                if (ev.SenderId == recipient.UserId)
                {
                    // don't need to notify ourselves via e-mail
                    continue;
                }

                User receiver = core.PrimitiveCache[recipient.UserId];

                if (receiver.UserInfo.EmailNotifications)
                {
                    string notificationString = string.Format("[user]{0}[/user] [iurl=\"{1}\"]" + core.Prose.GetString("_SENT_YOU_A_MESSAGE") + "[/iurl]",
                        ev.SenderId, ev.Uri);

                    Template emailTemplate = new Template(core.TemplateEmailPath, "notification.html");

                    emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
                    emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
                    emailTemplate.Parse("TO_NAME", receiver.DisplayName);
                    core.Display.ParseBbcode(emailTemplate, "NOTIFICATION_MESSAGE", notificationString, receiver, false, string.Empty, string.Empty, true);

                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(notificationString))), emailTemplate);
                }
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
