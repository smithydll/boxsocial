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
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Forms;

namespace BoxSocial.FrontEnd
{
    public partial class share : TPage
    {
        public share()
            : base("")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAjax = false;
            long itemId;
            long itemTypeId;
            ItemKey itemKey = null;
            IShareableItem item = null;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            if (!core.Session.IsLoggedIn)
            {
                core.Ajax.ShowMessage(isAjax, "notLoggedIn", "Not Logged In", "Sign in to share this item.");
            }

            string mode = Request.QueryString["mode"];

            if (mode == "post")
            {
                template.SetTemplate("pane.share.post.html");

                try
                {
                    itemId = long.Parse((string)core.Http.Query["item"]);
                    itemTypeId = long.Parse((string)core.Http.Query["type"]);

                    itemKey = new ItemKey(itemId, itemTypeId);
                    item = (IShareableItem)NumberedItem.Reflect(core, itemKey);

                    TextBox messageTextBox = new TextBox("share-message");
                    PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "share-permissions", core.Session.LoggedInMember.ItemKey);

                    template.Parse("S_SHARE_MESSAGE", messageTextBox);
                    template.Parse("S_SHARE_PERMISSIONS", permissionSelectBox);
                    template.Parse("S_SHARED_URI", item.Info.ShareUri);
                    core.Display.ParseBbcode(template, "S_SHARED_STRING", core.Functions.Tldr("[share=\"[iurl=\"" + item.Uri + "\"]" + item.Owner.DisplayName + "[/iurl]\"]" + item.ShareString + "[/share]"), core.Session.LoggedInMember);
                }
                catch
                {
                    core.Ajax.SendRawText("errorFetchingItem", "");
                    return;
                }

                core.Ajax.SendRawText("sharingForm", template.ToString());
                return;
            }

            // Save the Share
            try
            {
                itemId = long.Parse((string)core.Http.Form["item"]);
                itemTypeId = long.Parse((string)core.Http.Form["type"]);
            }
            catch
            {
                core.Ajax.SendRawText("errorFetchingItem", "");
                return;
            }

            itemKey = new ItemKey(itemId, itemTypeId);
            item = (IShareableItem)NumberedItem.Reflect(core, itemKey);

            if (item is IPermissibleItem)
            {
                IPermissibleItem pitem = (IPermissibleItem)item;

                if (!pitem.Access.IsPublic())
                {
                    core.Ajax.ShowMessage(isAjax, "cannotShare", "Cannot Share", "You can only share public items.");
                    return;
                }
            }

            string message = (string)core.Http.Form["share-message"] + "\n\n" + core.Functions.Tldr("[share=\"[iurl=\"" + item.Uri + "\"]" + item.Owner.DisplayName + "[/iurl]\"]" + item.ShareString + "[/share]");

            StatusMessage newStatus = StatusMessage.Create(core, core.Session.LoggedInMember, message);

            AccessControlLists acl = new AccessControlLists(core, newStatus);
            acl.SaveNewItemPermissions("share-permissions");

            ApplicationEntry ae = core.GetApplication("Profile");
            ae.PublishToFeed(core, core.Session.LoggedInMember, newStatus, Functions.SingleLine(core.Bbcode.Flatten(newStatus.Message)));

            Share.ShareItem(core, itemKey);

            if (Request.Form["ajax"] == "true")
            {
                Template template = new Template("pane.statusmessage.html");
                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                VariableCollection statusMessageVariableCollection = template.CreateChild("status_messages");

                core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(newStatus.Message), core.Session.LoggedInMember, true, string.Empty, string.Empty);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(newStatus.GetTime(core.Tz)));

                statusMessageVariableCollection.Parse("ID", newStatus.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", newStatus.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", newStatus.Poster.DisplayName);
                statusMessageVariableCollection.Parse("U_PROFILE", newStatus.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", string.Empty /*core.Hyperlink.BuildCommentQuoteUri(newStatus.Id)*/);
                statusMessageVariableCollection.Parse("U_REPORT", string.Empty /*core.Hyperlink.BuildCommentReportUri(newStatus.Id)*/);
                statusMessageVariableCollection.Parse("U_DELETE", string.Empty /*core.Hyperlink.BuildCommentDeleteUri(newStatus.Id)*/);
                statusMessageVariableCollection.Parse("U_PERMISSIONS", newStatus.Access.AclUri);
                statusMessageVariableCollection.Parse("USER_TILE", newStatus.Poster.Tile);
                statusMessageVariableCollection.Parse("USER_ICON", newStatus.Poster.Icon);
                statusMessageVariableCollection.Parse("URI", newStatus.Uri);

                statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");

                if (newStatus.Access.IsPublic())
                {
                    statusMessageVariableCollection.Parse("IS_PUBLIC", "TRUE");
                    statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                    statusMessageVariableCollection.Parse("U_SHARE", newStatus.ShareUri);
                }

                Dictionary<string, string> returnValues = new Dictionary<string, string>(StringComparer.Ordinal);

                returnValues.Add("update", item.OwnerKey.Equals(newStatus.Owner.ItemKey) ? "true" : "false");
                returnValues.Add("message", message);
                returnValues.Add("template", template.ToString());

                core.Ajax.SendDictionary("statusPosted", returnValues);
                return;
            }
            else
            {
                string redirect = Request["redirect"];
                if (!string.IsNullOrEmpty(redirect))
                {
                    template.Parse("REDIRECT_URI", redirect);
                }
                core.Display.ShowMessage("Shared", "You have shared this item to your status feed.");
            }
        }
    }
}
