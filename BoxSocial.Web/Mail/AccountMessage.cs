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
    [AccountSubModule(AppPrimitives.Member, "mail", "message")]
    public class AccountMessage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return -1;
            }
        }

        public AccountMessage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountMessage_Load);
            this.Show += new EventHandler(AccountMessage_Show);
        }

        void AccountMessage_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountMessage_Delete));
            AddModeHandler("poll", new ModuleModeHandler(AccountMessage_Poll));
        }

        void AccountMessage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_view_message");

            List<MailFolder> folders = MailFolder.GetFolders(core, core.Session.LoggedInMember);

            /*foreach (MailFolder f in folders)
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
                modulesVariableCollection.Parse("URI", BuildUri("inbox", args));
            }*/

            long messageId = core.Functions.RequestLong("id", 0);

            long newestId = 0;
            try
            {
                MessageRecipient recipient = new MessageRecipient(core, core.Session.LoggedInMember, messageId);

                Message threadStart = new Message(core, messageId);

                template.Parse("S_ID", threadStart.Id.ToString());

                List<Message> messages = threadStart.GetMessages();

                if (messages.Count < 3)
                {
                    RenderMessage(core, template, threadStart);
                }

                // IF NOT DELETED THEN
                foreach (Message message in messages)
                {
                    RenderMessage(core, template, message);
                    newestId = message.Id;
                }

                threadStart.MarkRead();

                List<MessageRecipient> recipients = threadStart.GetRecipients();

                for (int i = 0; i < recipients.Count; i++)
                {
                    core.PrimitiveCache.LoadUserProfile(recipients[i].UserId);
                }

                for (int i = 0; i < recipients.Count; i++)
                {
                    VariableCollection recipientVariableCollection = template.CreateChild("recipients");

                    recipientVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[recipients[i].UserId].DisplayName);
                    if (core.PrimitiveCache[recipients[i].UserId].IsOnline)
                    {
                        recipientVariableCollection.Parse("IS_ONLINE", "TRUE");
                    }
                    else
                    {
                        recipientVariableCollection.Parse("IS_ONLINE", "FALSE");
                    }
                }

                template.Parse("NEWEST_ID", newestId);

            }
            catch (InvalidMessageRecipientException)
            {
                // Security, if not a recipeint, cannot see the message.
                core.Functions.Generate404();
            }
        }

        internal static void RenderMessage(Core core, Template template, Message message)
        {
            MessageRecipient recipient = new MessageRecipient(core, core.Session.LoggedInMember, message.Id);

            VariableCollection messageVariableCollection = template.CreateChild("post_list");

            messageVariableCollection.Parse("SUBJECT", message.Subject);
            messageVariableCollection.Parse("POST_TIME", core.Tz.DateTimeToString(message.GetSentDate(core.Tz)));
            messageVariableCollection.Parse("URI", message.Uri);
            messageVariableCollection.Parse("ID", message.Id.ToString());
            core.Display.ParseBbcode(messageVariableCollection, "MESSAGE", message.Text);
            if (message.Sender != null)
            {
                messageVariableCollection.Parse("U_USER", message.Sender.Uri);
                messageVariableCollection.Parse("USER_DISPLAY_NAME", message.Sender.UserInfo.DisplayName);
                messageVariableCollection.Parse("USER_TILE", message.Sender.Tile);
                messageVariableCollection.Parse("USER_ICON", message.Sender.Icon);
                messageVariableCollection.Parse("USER_JOINED", core.Tz.DateTimeToString(message.Sender.UserInfo.GetRegistrationDate(core.Tz)));
                messageVariableCollection.Parse("USER_COUNTRY", message.Sender.Profile.Country);
                //core.Display.ParseBbcode(messageVariableCollection, "SIGNATURE", postersList[post.UserId].ForumSignature);

                if (message.Sender.Id == core.LoggedInMemberId)
                {
                    messageVariableCollection.Parse("OWNER", "TRUE");
                }
            }
            else
            {
                messageVariableCollection.Parse("USER_DISPLAY_NAME", "Anonymous");
            }

            if (recipient.IsRead)
            {
                messageVariableCollection.Parse("IS_READ", "TRUE");
            }
            else
            {
                messageVariableCollection.Parse("IS_READ", "FALSE");
            }

            recipient.MarkRead();
        }

        void AccountMessage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long messageId = core.Functions.RequestLong("id", 0);

            if (messageId == 0)
            {
                core.Display.ShowMessage("Cannot Delete Message", "No message specified to delete. Please go back and try again.");
                return;
            }

            try
            {
                Message message = new Message(core, messageId);
                if (message.SenderId == core.Session.LoggedInMember.Id)
                {
                    message.Delete();
                }
                else
                {
                    message.RemoveRecipient(core.Session.LoggedInMember, RecipientType.Any);
                }

                SetRedirectUri(BuildUri("galleries", "galleries"));
                core.Display.ShowMessage("Message Deleted", "You have successfully deleted the message.");
            }
            catch
            {
                core.Display.ShowMessage("Cannot Delete Message", "An Error occured while trying to delete the message.");
                return;
            }
        }

        void AccountMessage_Poll(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long messageId = core.Functions.RequestLong("id", 0);

            long newestId = core.Functions.RequestLong("newest-id", 0);
            try
            {
                Template template = new Template(core.CallingApplication.Assembly, "pane_message");

                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                MessageRecipient recipient = new MessageRecipient(core, core.Session.LoggedInMember, messageId);

                Message threadStart = new Message(core, messageId);

                template.Parse("S_ID", threadStart.Id.ToString());

                List<Message> messages = threadStart.GetMessages(newestId, true);

                // IF NOT DELETED THEN
                foreach (Message message in messages)
                {
                    RenderMessage(core, template, message);
                    newestId = message.Id;
                }

                threadStart.MarkRead();

                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("update", "true");
                returnValues.Add("template", template.ToString());
                returnValues.Add("newest-id", newestId.ToString());

                core.Response.SendDictionary("newMessages", returnValues);

            }
            catch (InvalidMessageRecipientException)
            {
                // Security, if not a recipeint, cannot see the message.
                core.Functions.Generate404();
            }
        }
    }
}
