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

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "status")]
    public class AccountStatus : AccountSubModule
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

        /// <summary>
        /// Initializes a new instance of the AccountStatus class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountStatus(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountStatus_Load);
            this.Show += new EventHandler(AccountStatus_Show);
        }

        void AccountStatus_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountStatus_Delete));
        }

        void AccountStatus_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            Template template = new Template("pane.statusmessage.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            string message = core.Http.Form["message"];

            StatusMessage newMessage = StatusFeed.SaveMessage(core, message);

            AccessControlLists acl = new AccessControlLists(core, newMessage);
            acl.SaveNewItemPermissions();

            VariableCollection statusMessageVariableCollection = template.CreateChild("status_messages");

            core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(newMessage.Message), Owner);
            statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(newMessage.GetTime(core.Tz)));

            statusMessageVariableCollection.Parse("ID", newMessage.Id.ToString());
            statusMessageVariableCollection.Parse("TYPE_ID", newMessage.ItemKey.TypeId.ToString());
            statusMessageVariableCollection.Parse("USERNAME", newMessage.Poster.DisplayName);
            statusMessageVariableCollection.Parse("U_PROFILE", newMessage.Poster.ProfileUri);
            statusMessageVariableCollection.Parse("U_QUOTE", core.Uri.BuildCommentQuoteUri(newMessage.Id));
            statusMessageVariableCollection.Parse("U_REPORT", core.Uri.BuildCommentReportUri(newMessage.Id));
            statusMessageVariableCollection.Parse("U_DELETE", core.Uri.BuildCommentDeleteUri(newMessage.Id));
            statusMessageVariableCollection.Parse("U_PERMISSIONS", newMessage.Access.AclUri);
            statusMessageVariableCollection.Parse("USER_TILE", newMessage.Poster.UserTile);
            statusMessageVariableCollection.Parse("USER_ICON", newMessage.Poster.UserIcon);
            statusMessageVariableCollection.Parse("URI", newMessage.Uri);

            statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");

            if (newMessage.Access.IsPublic())
            {
                statusMessageVariableCollection.Parse("IS_PUBLIC", "TRUE");
                statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                statusMessageVariableCollection.Parse("U_SHARE", newMessage.ShareUri);
            }

            Dictionary<string, string> returnValues = new Dictionary<string, string>();

            returnValues.Add("update", "true");
            returnValues.Add("message", message);
            returnValues.Add("template", template.ToString());

            core.Ajax.SendDictionary("statusPosted", returnValues);
        }

        void AccountStatus_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long messageId = core.Functions.FormLong("id", 0);

            if (messageId > 0)
            {
                StatusMessage message = new StatusMessage(core, messageId);

                if (message.Owner.Id == Owner.Id)
                {
                    ItemKey messageKey = message.ItemKey;
                    long count = message.Delete();

                    DeleteQuery dQuery = new DeleteQuery(typeof(BoxSocial.Internals.Action));
                    dQuery.AddCondition("action_primitive_id", Owner.Id);
                    dQuery.AddCondition("action_primitive_type_id", Owner.TypeId);
                    dQuery.AddCondition("action_item_id", messageKey.Id);
                    dQuery.AddCondition("action_item_type_id", messageKey.TypeId);

                    core.Db.Query(dQuery);

                    core.Ajax.SendStatus("messageDeleted");
                    return;
                }
            }

            core.Ajax.ShowMessage(true, "permissionDenied", "Permission Denied", "You cannot delete this item.");
        }

    }
}
