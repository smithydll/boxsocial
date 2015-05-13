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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Mail
{
    [AccountSubModule(AppPrimitives.Member, "mail", "compose")]
    public class AccountCompose : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("NEW_CONVERSATION");
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
        /// Initializes a new instance of the AccountCompose class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCompose(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountCompose_Load);
            this.Show += new EventHandler(AccountCompose_Show);
        }

        void AccountCompose_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("reply", AccountCompose_Reply);
        }

        void AccountCompose_Show(object sender, EventArgs e)
        {
            SetTemplate("account_compose");

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

            long messageId = core.Functions.FormLong("id", 0);
            bool edit = false;

            UserSelectBox toUserSelectBox = new UserSelectBox(core, "to");
            UserSelectBox ccUserSelectBox = new UserSelectBox(core, "cc");
            TextBox subjectTextBox = new TextBox("subject");
            TextBox messageTextBox = new TextBox("message");
            messageTextBox.IsFormatted = true;

            Message message = null;
            try
            {
                message = new Message(core, messageId);
                if (message.SenderId == core.Session.LoggedInMember.Id)
                {
                    edit = true;
                }
                else
                {
                    core.Functions.Generate403();
                }
            }
            catch (InvalidMessageException)
            {
            }

            if (edit)
            {
                subjectTextBox.Value = message.Subject;
                messageTextBox.Value = message.Text;

                List<MessageRecipient> recipients = message.GetRecipients();

                foreach (MessageRecipient recipient in recipients)
                {
                    switch (recipient.RecipientType)
                    {
                        case RecipientType.To:
                            toUserSelectBox.AddUserId(recipient.UserId);
                            break;
                        case RecipientType.Cc:
                            ccUserSelectBox.AddUserId(recipient.UserId);
                            break;
                    }
                }

                if (message.Draft)
                {
                    template.Parse("SAVE_DRAFT", "TRUE");
                }
                else
                {
                    template.Parse("SAVE_DRAFT", "FALSE");
                }
            }
            else
            {
                template.Parse("SAVE_DRAFT", "TRUE");
            }

            template.Parse("S_TO", toUserSelectBox);
            template.Parse("S_CC", ccUserSelectBox);

            template.Parse("S_SUBJECT", subjectTextBox);
            template.Parse("S_MESSAGE", messageTextBox);

            if (core.Http.Form["save"] != null)
            {
                AccountCompose_Save(this, new EventArgs());
            }

            if (core.Http.Form["send"] != null)
            {
                AccountCompose_Send(this, new EventArgs());
            }
        }

        void AccountCompose_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            try
            {
                SaveOrSend(true);
            }
            catch (TooManyMessageRecipientsException)
            {
                DisplayError("Too many recipients selected.");
            }
        }

        void AccountCompose_Send(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            try
            {
                SaveOrSend(false);
            }
            catch (TooManyMessageRecipientsException)
            {
                DisplayError("Too many recipients selected.");
            }
        }

        private void AccountCompose_Reply(object sender, ModuleModeEventArgs e)
        {
            SaveMode(AccountCompose_ReplySave);
        }

        void AccountCompose_ReplySave(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            bool ajax = false;

            try
            {
                ajax = bool.Parse(core.Http["ajax"]);
            }
            catch { }

            long messageId = core.Functions.FormLong("id", 0);
            long newestId = core.Functions.FormLong("newest-id", 0);
            string text = core.Http.Form["message"];

            Message threadStart = new Message(core, messageId);

            Message newMessage = Message.Reply(core, LoggedInMember, threadStart, text);

            if (ajax)
            {
                Template template = new Template(core.CallingApplication.Assembly, "pane_message");

                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                MessageRecipient recipient = new MessageRecipient(core, core.Session.LoggedInMember, messageId);

                template.Parse("S_ID", threadStart.Id.ToString());

                List<Message> messages = threadStart.GetMessages(newestId, true);

                // IF NOT DELETED THEN
                foreach (Message message in messages)
                {
                    AccountMessage.RenderMessage(core, template, message);
                    newestId = message.Id;
                }

                threadStart.MarkRead();

                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("update", "true");
                returnValues.Add("message", newMessage.Text);
                returnValues.Add("template", template.ToString());
                returnValues.Add("newest-id", newestId.ToString());

                core.Response.SendDictionary("replySent", returnValues);
            }
            else
            {
                SetRedirectUri(BuildUri("inbox"));
                core.Display.ShowMessage("Message sent", "Your mail message has been sent.");
            }
        }

        private void SaveOrSend(bool draft)
        {
            long messageId = core.Functions.FormLong("id", 0);
            string subject = core.Http.Form["subject"];
            string text = core.Http.Form["message"];
            Dictionary<User, RecipientType> recipients = new Dictionary<User, RecipientType>();

            recipients.Add(core.Session.LoggedInMember, RecipientType.Sender);

            List<long> toRecipients = UserSelectBox.FormUsers(core, "to");
            List<long> ccRecipients = UserSelectBox.FormUsers(core, "cc");

            foreach (long id in toRecipients)
            {
                core.PrimitiveCache.LoadUserProfile(id);
            }

            foreach (long id in ccRecipients)
            {
                core.PrimitiveCache.LoadUserProfile(id);
            }

            foreach (long id in toRecipients)
            {
                if (core.PrimitiveCache[id] != null)
                {
                    recipients.Add(core.PrimitiveCache[id], RecipientType.To);
                }
            }

            foreach (long id in ccRecipients)
            {
                if (core.PrimitiveCache[id] != null)
                {
                    recipients.Add(core.PrimitiveCache[id], RecipientType.Cc);
                }
            }

            if (recipients.Count > 1)
            {
                if (messageId > 0)
                {
                    bool send = false;
                    Message message = new Message(core, messageId);
                    if (message.Draft && (!draft))
                    {
                        send = true;
                        message.Draft = draft;
                    }
                    message.Subject = subject;
                    message.Text = text;

                    /* Check recipient list */
                    List<MessageRecipient> savedRecipients = message.GetRecipients();

                    foreach (MessageRecipient r in savedRecipients)
                    {
                        bool flag = false;
                        foreach (User user in recipients.Keys)
                        {
                            if (r.UserId == user.Id && r.RecipientType == recipients[user])
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            r.Delete();
                        }
                    }

                    foreach (User user in recipients.Keys)
                    {
                        bool flag = false;
                        foreach (MessageRecipient r in savedRecipients)
                        {
                            if (r.UserId == user.Id && r.RecipientType == recipients[user])
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            message.AddRecipient(user, recipients[user]);
                        }
                    }

                    if (send)
                    {

                    }

                    message.Update();

                    SetRedirectUri(BuildUri("inbox"));
                    core.Display.ShowMessage("Message Updated", "Your mail message has been saved.");
                }
                else
                {
                    Message message = Message.Create(core, draft, subject, text, recipients);
                    if (draft)
                    {
                        SetRedirectUri(BuildUri("inbox"));
                        core.Display.ShowMessage("Message saved", "Your mail message has been saved.");
                    }
                    else
                    {
                        SetRedirectUri(BuildUri("inbox"));
                        core.Display.ShowMessage("Message sent", "Your mail message has been sent.");
                    }
                }
            }
        }
    }
}
