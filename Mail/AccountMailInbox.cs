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
                return "Inbox";
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
        public AccountMailInbox(Core core)
            : base(core)
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

                core.Template.Parse("U_UNSEEN_MAIL", "FALSE");
            }

            List<MailFolder> folders = MailFolder.GetFolders(core, core.Session.LoggedInMember);

            foreach (MailFolder f in folders)
            {
                if (f.FolderType == FolderTypes.Inbox) continue;

                VariableCollection modulesVariableCollection = core.Template.CreateChild("account_links");

                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("folder", f.FolderName);

                modulesVariableCollection.Parse("TITLE", f.FolderName);
                modulesVariableCollection.Parse("SUB", Key);
                modulesVariableCollection.Parse("MODULE", ModuleKey);
                modulesVariableCollection.Parse("URI", BuildUri(args));
            }

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
                    MailFolder.Create(core, FolderTypes.Draft, "Drafts");
                    MailFolder.Create(core, FolderTypes.Outbox, "Outbox");
                    MailFolder.Create(core, FolderTypes.SentItems, "Sent Items");
                }
                else
                {
                    core.Functions.Generate404();
                    return;
                }
            }

            List<Message> messages = mailFolder.GetMessages(core.TopLevelPageNumber, 20);

            List<long> messageIds = new List<long>();
            Dictionary<long, MessageRecipient> readStatus = new Dictionary<long, MessageRecipient>();

            if (messages.Count > 0)
            {
                foreach (Message message in messages)
                {
                    messageIds.Add(message.Id);
                }

                SelectQuery query = MessageRecipient.GetSelectQueryStub(typeof(MessageRecipient));
                query.AddCondition("user_id", core.Session.LoggedInMember.Id);
                query.AddCondition("message_id", ConditionEquality.In, messageIds);

                DataTable recipientDataTable = db.Query(query);

                foreach (DataRow row in recipientDataTable.Rows)
                {
                    MessageRecipient recipient = new MessageRecipient(core, row);
                    readStatus.Add(recipient.MessageId, recipient);
                }
            }

            foreach (Message message in messages)
            {
                VariableCollection messageVariableCollection = template.CreateChild("mail_item");

                bool isRead = false;

                if (readStatus.ContainsKey(message.Id))
                {
                    if (readStatus[message.Id].IsRead)
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
                if (message.SenderId > 0)
                {
                    messageVariableCollection.Parse("SENDER", message.Sender.DisplayName);
                    messageVariableCollection.Parse("U_SENDER", message.Sender.Uri);
                }
                messageVariableCollection.Parse("DATE", core.Tz.DateTimeToString(message.GetSentDate(core.Tz)));
            }

            Dictionary<string, string> a = new Dictionary<string,string>();
            a.Add("folder", mailFolder.FolderName);

            core.Display.ParsePagination(template, BuildUri(a), 20, mailFolder.MessageCount);
        }
    }
}
