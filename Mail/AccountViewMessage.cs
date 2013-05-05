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
    [AccountSubModule(AppPrimitives.Member, "mail", "read")]
    public class AccountViewMessage : AccountSubModule
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

        public AccountViewMessage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountViewMessage_Load);
            this.Show += new EventHandler(AccountViewMessage_Show);
        }

        void AccountViewMessage_Load(object sender, EventArgs e)
        {
            
        }

        void AccountViewMessage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_view_message");

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
                modulesVariableCollection.Parse("URI", BuildUri("inbox", args));
            }

            long messageId = core.Functions.RequestLong("id", 0);

            try
            {
                MessageRecipient recipient = new MessageRecipient(core, core.Session.LoggedInMember, messageId);

                // IF NOT DELETED THEN
                {
                    Message message = new Message(core, messageId);

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
                        messageVariableCollection.Parse("USER_TILE", message.Sender.UserTile);
                        messageVariableCollection.Parse("USER_ICON", message.Sender.UserIcon);
                        messageVariableCollection.Parse("USER_JOINED", core.Tz.DateTimeToString(message.Sender.UserInfo.GetRegistrationDate(core.Tz)));
                        messageVariableCollection.Parse("USER_COUNTRY", message.Sender.Profile.Country);
                        //core.Display.ParseBbcode(messageVariableCollection, "SIGNATURE", postersList[post.UserId].ForumSignature);
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

                    message.MarkRead();
                    recipient.MarkRead();
                }
            }
            catch (InvalidMessageRecipientException)
            {
                // Security, if not a recipeint, cannot see the message.
                core.Functions.Generate404();
            }
        }
    }
}
