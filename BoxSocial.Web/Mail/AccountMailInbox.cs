﻿/*
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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Mail
{
    [AccountSubModule(AppPrimitives.Member, "mail", "inbox", true)]
    public class AccountMailInbox : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("MESSAGES");
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountMailInbox class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountMailInbox(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountMailInbox_Load);
            this.Show += new EventHandler(AccountMailInbox_Show);
        }

        void AccountMailInbox_Load(object sender, EventArgs e)
        {
        }

        void AccountMailInbox_Show(object sender, EventArgs e)
        {
            SetTemplate("account_mailbox");

            if (LoggedInMember.UserInfo.UnseenMail > 0)
            {
                UpdateQuery query = new UpdateQuery(typeof(UserInfo));
                query.AddField("user_unseen_mail", new QueryOperation("user_unseen_mail", QueryOperations.Subtraction, LoggedInMember.UserInfo.UnseenMail));
                query.AddCondition("user_id", LoggedInMember.Id);

                db.Query(query);

                core.Template.Parse("UNSEEN_MAIL", "FALSE");
            }

            /*List<MailFolder> folders = MailFolder.GetFolders(core, core.Session.LoggedInMember);

            foreach (MailFolder f in folders)
            {
                if (f.FolderType == FolderTypes.Inbox) continue;

                VariableCollection modulesVariableCollection = core.Template.CreateChild("account_links");
                ParentModulesVariableCollection.CreateChild("account_links", modulesVariableCollection);

                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("folder", f.FolderName);

                switch (f.FolderType)
                {
                    case FolderTypes.Draft:
                        modulesVariableCollection.Parse("TITLE", core.Prose.GetString("DRAFTS"));
                        break;
                    case FolderTypes.Outbox:
                        modulesVariableCollection.Parse("TITLE", core.Prose.GetString("OUTBOX"));
                        break;
                    case FolderTypes.SentItems:
                        modulesVariableCollection.Parse("TITLE", core.Prose.GetString("SENT_ITEMS"));
                        break;
                    default:
                        modulesVariableCollection.Parse("TITLE", f.FolderName);
                        break;
                }
                modulesVariableCollection.Parse("SUB", Key);
                modulesVariableCollection.Parse("MODULE", ModuleKey);
                modulesVariableCollection.Parse("URI", BuildUri(args));
            }*/

            string folder = "Inbox";
            if (!string.IsNullOrEmpty(core.Http.Query["folder"]))
            {
                folder = core.Http.Query["folder"];
            }


            MailFolder mailFolder = null;
            try
            {
                mailFolder = new MailFolder(core, core.Session.LoggedInMember, folder);
            }
            catch (InvalidMailFolderException)
            {
                if (folder == "Inbox")
                {
                    mailFolder = MailFolder.Create(core, FolderTypes.Inbox, folder);
                    MailFolder.Create(core, FolderTypes.Draft, core.Prose.GetString("DRAFTS"));
                    MailFolder.Create(core, FolderTypes.Outbox, core.Prose.GetString("OUTBOX"));
                    MailFolder.Create(core, FolderTypes.SentItems, core.Prose.GetString("SENT_ITEMS"));
                }
                else
                {
                    core.Functions.Generate404();
                    return;
                }
            }

            List<Message> messages = mailFolder.GetThreads(core.TopLevelPageNumber, 20);

            List<long> messageIds = new List<long>();
            List<long> lastMessageIds = new List<long>();
            Dictionary<long, MessageRecipient> readStatus = new Dictionary<long, MessageRecipient>();

            if (messages.Count > 0)
            {
                foreach (Message message in messages)
                {
                    messageIds.Add(message.Id);
                    if (message.LastId > 0)
                    {
                        lastMessageIds.Add(message.LastId);
                    }
                    else
                    {
                        lastMessageIds.Add(message.Id);
                    }
                }

                SelectQuery query = MessageRecipient.GetSelectQueryStub(core, typeof(MessageRecipient));
                query.AddCondition("user_id", core.Session.LoggedInMember.Id);
                query.AddCondition("message_id", ConditionEquality.In, lastMessageIds);

                System.Data.Common.DbDataReader recipientReader = db.ReaderQuery(query);

                while(recipientReader.Read())
                {
                    MessageRecipient recipient = new MessageRecipient(core, recipientReader);
                    readStatus.Add(recipient.MessageId, recipient);
                }

                recipientReader.Close();
                recipientReader.Dispose();
            }

            foreach (Message message in messages)
            {
                VariableCollection messageVariableCollection = template.CreateChild("mail_item");

                bool isRead = false;

                long lastId = message.LastId;
                if (lastId == 0)
                {
                    lastId = message.Id;
                }

                if (readStatus.ContainsKey(lastId))
                {
                    if (readStatus[lastId].IsRead)
                    {
                        isRead = true;
                    }
                }

                if (isRead)
                {
                    messageVariableCollection.Parse("IS_NORMAL_READ", "TRUE");
                }
                else
                {
                    messageVariableCollection.Parse("IS_NORMAL_UNREAD", "TRUE");
                }

                messageVariableCollection.Parse("SUBJECT", message.Subject);
                messageVariableCollection.Parse("URI", message.Uri);
                messageVariableCollection.Parse("U_DELETE", BuildUri("message", "delete", message.Id));

                List<MessageRecipient> recipients = message.GetRecipients();

                for (int i = 0; i < recipients.Count; i++)
                {
                    core.PrimitiveCache.LoadUserProfile(recipients[i].UserId);
                }

                switch (mailFolder.FolderType)
                {
                    case FolderTypes.Inbox:
                    case FolderTypes.Custom:
                        // INBOX show sender
                        if (message.SenderId > 0)
                        {
                            messageVariableCollection.Parse("SENDER", message.Sender.DisplayName);
                            messageVariableCollection.Parse("U_SENDER", message.Sender.Uri);
                        }
                        for (int i = 0; i < recipients.Count; i++)
                        {
                            if (recipients[i].UserId != LoggedInMember.Id)
                            {
                                VariableCollection recipientVariableCollection = messageVariableCollection.CreateChild("recipients");

                                recipientVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[recipients[i].UserId].DisplayName);
                            }
                        }

                        template.Parse("INBOX", "TRUE");
                        break;
                    case FolderTypes.Draft:
                    case FolderTypes.Outbox:
                    case FolderTypes.SentItems:
                        {
                            int i = 0;
                            while (recipients.Count > i)
                            {
                                if (recipients[i].RecipientType == RecipientType.To)
                                {
                                    messageVariableCollection.Parse("SENDER", core.PrimitiveCache[recipients[i].UserId].DisplayName);
                                    messageVariableCollection.Parse("U_SENDER", core.PrimitiveCache[recipients[i].UserId].Uri);
                                    break;
                                }
                                i++;
                            }
                        }
                        break;
                }
                messageVariableCollection.Parse("DATE", core.Tz.DateTimeToString(message.GetSentDate(core.Tz)));
                messageVariableCollection.Parse("LAST_DATE", core.Tz.DateTimeToString(message.GetLastMessageDate(core.Tz)));
            }

            Dictionary<string, string> a = new Dictionary<string,string>();
            a.Add("folder", mailFolder.FolderName);

            core.Display.ParsePagination(template, BuildUri(a), 20, mailFolder.MessageCount);
        }
    }
}
