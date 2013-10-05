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

namespace BoxSocial.FrontEnd
{
    public partial class comment : TPage
    {

        // TODO: 1023 max length
        public const int COMMENT_MAX_LENGTH = 511;

        public comment()
            : base("")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string comment;
            long itemId;
            long itemTypeId;
			ItemKey itemKey = null;
            ICommentableItem thisItem = null;
            long commentId = -1;
            bool isAjax = false;
            ApplicationEntry ae = null;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            string mode = Request.QueryString["mode"];

            if (mode == "quote")
            {
                template.SetTemplate("posting.comment.html");

                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    core.Ajax.SendRawText("errorFetchingComment", "");
                    return;
                }

                DataTable commentsTable = db.Query(string.Format("SELECT ui.user_name, c.comment_text FROM comments c LEFT JOIN user_info ui ON c.user_id = ui.user_id WHERE comment_id = {0}",
                    itemId));

                if (commentsTable.Rows.Count == 1)
                {
                    string quotedComment = string.Format("\n\n[quote=\"{0}\"]{1}[/quote]",
                        (string)commentsTable.Rows[0]["user_name"], (string)commentsTable.Rows[0]["comment_text"]);

                    template.Parse("COMMENT_TEXT", quotedComment);
                }
                else
                {
                    core.Ajax.SendRawText("errorFetchingComment", "");
                }

                return;
            }

            if (mode == "fetch")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    core.Ajax.SendRawText("errorFetchingComment", "");
                    return;
                }

                DataTable commentsTable = db.Query(string.Format("SELECT ui.user_name, c.comment_text FROM comments c LEFT JOIN user_info ui ON c.user_id = ui.user_id WHERE comment_id = {0}",
                    itemId));

                if (commentsTable.Rows.Count == 1)
                {
                    core.Ajax.SendRawText("commentFetched", (string.Format("\n\n[quote=\"{0}\"]{1}[/quote]",
                        (string)commentsTable.Rows[0]["user_name"], (string)commentsTable.Rows[0]["comment_text"])));
                }
                else
                {
                    core.Ajax.SendRawText("errorFetchingComment", "");
                }

                return;
            }

            if (mode == "load")
            {
                try
                {
                    itemId = long.Parse((string)core.Http.Query["item"]);
                    itemTypeId = long.Parse((string)core.Http.Query["type"]);
                }
                catch
                {
                    core.Ajax.SendRawText("errorFetchingComment", "");
                    return;
                }

                try
                {
                    // This isn't the most elegant fix, but it works
                    if (core.IsPrimitiveType(itemTypeId))
                    {
                        ae = core.GetApplication("GuestBook");
                    }
                    else
                    {
                        ItemType itemType = new ItemType(core, itemTypeId);
                        if (itemType.ApplicationId == 0)
                        {
                            ae = core.GetApplication("GuestBook");
                        }
                        else
                        {
                            ae = new ApplicationEntry(core, itemType.ApplicationId);
                        }
                    }

                    BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
                }
                catch (InvalidApplicationException)
                {
                    core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comments you have attempted to fetch are invalid. (0x01)");
                    return;
                }

                try
                {
                    thisItem = (ICommentableItem)NumberedItem.Reflect(core, new ItemKey(itemId, itemTypeId));
                }
                catch (Exception ex)
                {
                    // Only catch genuine InvalidItemException throws
                    if ((ex.GetType() == typeof(TargetInvocationException) && ex.InnerException.GetType().IsSubclassOf(typeof(InvalidItemException))) || ex.GetType().IsSubclassOf(typeof(InvalidItemException)))
                    {
                        core.Ajax.ShowMessage(isAjax, "invalidItem", "Item no longer exists", "Cannot load the comments as the item no longer exists.");
                    }
                    throw ex;
                }

                Template template = new Template("pane.comments.html");
                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                template.Parse("U_SIGNIN", Core.Hyperlink.BuildLoginUri());

                if (thisItem is IPermissibleItem)
                {
                    if (!((IPermissibleItem)thisItem).Access.Can("VIEW"))
                    {
                        core.Ajax.ShowMessage(isAjax, "accessDenied", "Access Denied", "The you do not have access to these comments");
                        return;
                    }

                    if (((IPermissibleItem)thisItem).Access.Can("COMMENT"))
                    {
                        template.Parse("CAN_COMMENT", "TRUE");
                    }
                }

                if (thisItem is IPermissibleSubItem)
                {
                    if (!((IPermissibleSubItem)thisItem).PermissiveParent.Access.Can("VIEW"))
                    {
                        core.Ajax.ShowMessage(isAjax, "accessDenied", "Access Denied", "The you do not have access to these comments");
                        return;
                    }

                    if (((IPermissibleSubItem)thisItem).PermissiveParent.Access.Can("COMMENT"))
                    {
                        template.Parse("CAN_COMMENT", "TRUE");
                    }
                }

                if (thisItem is ICommentableItem)
                {
                    core.Display.DisplayComments(template, ((ICommentableItem)thisItem).Owner, 1, (ICommentableItem)thisItem);
                    //List<Comment> comments = Comment.GetComments(core, new ItemKey(itemId, itemTypeId), SortOrder.Ascending, 1, 10, null);

                    core.Ajax.SendRawText("fetchSuccess", template.ToString());
                }
                else
                {
                    core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comments you have attempted to fetch are invalid. (0x07)");
                    return;
                }
            }

            if (mode == "report")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    core.Ajax.ShowMessage(isAjax, "errorReportingComment", "Error", "The comment you have reported is invalid.");
                    return;
                }

                // only logged in members can report comment spam
                if (session.IsLoggedIn)
                {
                    // has the user reported the comment before?
                    DataTable reportsTable = db.Query(string.Format("SELECT report_id FROM spam_reports WHERE comment_id = {0} AND user_id = {1};",
                        itemId, loggedInMember.UserId));

                    if (reportsTable.Rows.Count == 0)
                    {
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("UPDATE comments SET comment_spam_score = comment_spam_score + 2 WHERE comment_id = {0}",
                            itemId));

                        // add a log entry that the user reported this comment
                        db.UpdateQuery(string.Format("INSERT INTO spam_reports (comment_id, user_id, report_time_ut) VALUES ({0}, {1}, UNIX_TIMESTAMP());",
                            itemId, loggedInMember.UserId));
                    }
                    else
                    {
                        core.Ajax.ShowMessage(isAjax, "alreadyReported", "Already Reported", "You have already reported this comment as SPAM.");
                    }
                }
                core.Ajax.ShowMessage(isAjax, "commentReported", "Reported Comment", "You have successfully reported a comment.");
            }

            if (mode == "delete")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    core.Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                    return;
                }

                // select the comment
                try
                {
                    Comment thisComment = new Comment(core, itemId);

                    itemId = thisComment.ItemId;
                    itemTypeId = thisComment.ItemTypeId;

                    try
                    {
                        // This isn't the most elegant fix, but it works
                        if (core.IsPrimitiveType(itemTypeId))
                        {
                            ae = core.GetApplication("GuestBook");
                        }
                        else
                        {
                            ItemType itemType = new ItemType(core, itemTypeId);
                            if (itemType.ApplicationId == 0)
                            {
                                ae = core.GetApplication("GuestBook");
                            }
                            else
                            {
                                ae = new ApplicationEntry(core, itemType.ApplicationId);
                            }
                        }

                        BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
                    }
                    catch (InvalidApplicationException)
                    {
                        core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x01)");
                        return;
                    }

                    try
                    {
                        if (!thisComment.PermissiveParent.Access.Can("DELETE_COMMENTS"))
                        {
                            core.Ajax.ShowMessage(isAjax, "permissionDenied", "Permission Denied", "You do not have the permissions to delete this comment.");
                        }
                    }
                    catch (InvalidItemException)
                    {
                        core.Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                    }

                    // delete the comment
                    if (thisComment.SpamScore >= 10)
                    {
                        // keep for spam system
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("UPDATE comments SET comment_deleted = TRUE WHERE comment_id = {0}",
                            itemId));
                    }
                    else
                    {
                        
                        // do not need to keep
                        db.BeginTransaction();
                        thisComment.Delete();
                        /*db.UpdateQuery(string.Format("DELETE FROM comments WHERE comment_id = {0}",
                            itemId));*/

                        ItemType itemType = new ItemType(core, itemTypeId);
                        //Notification.DeleteItem(itemType, itemId);
                    }

                    Comment.CommentDeleted(core, thisComment.CommentedItemKey);
                }
                catch (InvalidCommentException)
                {
                    core.Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                }

                core.Ajax.SendRawText("commentDeleted", "You have successfully deleted the comment.");
            }

            try
            {
                comment = (string)Request.Form["comment"];
                itemId = core.Functions.RequestLong("item", 0);
                itemTypeId = core.Functions.RequestLong("type", 0);
				itemKey = new ItemKey(itemId, itemTypeId);
            }
            catch
            {
                core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x02)");
                return;
            }

            try
            {
                // This isn't the most elegant fix, but it works
                if (core.IsPrimitiveType(itemTypeId))
                {
                    ae = core.GetApplication("GuestBook");
                }
                else
                {
                    ItemType itemType = new ItemType(core, itemTypeId);
                    if (itemType.ApplicationId == 0)
                    {
                        ae = core.GetApplication("GuestBook");
                    }
                    else
                    {
                        ae = new ApplicationEntry(core, itemType.ApplicationId);
                    }
                }

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
            }
            catch (InvalidApplicationException)
            {
                core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x03)");
                return;
            }

            /* save comment in the database */

            Item item = null;
            try
            {
                item = NumberedItem.Reflect(core, new ItemKey(itemId, itemTypeId));
                if (item is ICommentableItem)
                {
                    thisItem = (ICommentableItem)item;

                    IPermissibleItem pItem = null;
                    if (item is IPermissibleItem)
                    {
                        pItem = (IPermissibleItem)item;
                    }
                    else if (item is IPermissibleSubItem)
                    {
                        pItem = ((IPermissibleSubItem)item).PermissiveParent;
                    }
                    else
                    {
                        pItem = thisItem.Owner;
                    }

                    if (!pItem.Access.Can("COMMENT"))
                    {
                        core.Ajax.ShowMessage(isAjax, "notLoggedIn", "Permission Denied", "You do not have the permissions to post a comment to this item.");
                    }
                }
                else
                {
                    core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Item", "The comment you have attempted to post is invalid. (0x07)");
                }
            }
            catch (InvalidItemException)
            {
                core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x04)");
            }

            Comment commentObject = null;
            try
            {
                commentObject = Comment.Create(Core, itemKey, comment);
                commentId = commentObject.CommentId;

                if (item != null)
                {
                    if (item is IActionableItem || item is IActionableSubItem)
                    {
                        //ae.TouchFeed(core.Session.LoggedInMember, item);
                    }
                    else
                    {
                        ae.PublishToFeed(core, core.Session.LoggedInMember, commentObject, new ItemKey(itemId, itemTypeId), Functions.SingleLine(core.Bbcode.Flatten(commentObject.Body)));
                    }
                    ICommentableItem citem = (ICommentableItem)item;

                    citem.CommentPosted(new CommentPostedEventArgs(commentObject, core.Session.LoggedInMember, new ItemKey(itemId, itemTypeId)));
                }

                Comment.Commented(core, itemKey);
                
            }
            catch (NotLoggedInException)
            {
                core.Ajax.ShowMessage(isAjax, "notLoggedIn", "Not Logged In", "You must be logged in to post a comment.");
            }
            catch (CommentFloodException)
            {
                core.Ajax.ShowMessage(isAjax, "rejectedByFloodControl", "Posting Too Fast", "You are posting too fast. Please wait a minute and try again.");
            }
            catch (CommentTooLongException)
            {
                core.Ajax.ShowMessage(isAjax, "commentTooLong", "Comment Too Long", "The comment you have attempted to post is too long, maximum size is 511 characters.");
            }
            catch (CommentTooShortException)
            {
                core.Ajax.ShowMessage(isAjax, "commentTooShort", "Comment Too Short", "The comment you have attempted to post is too short, must be longer than two characters.");
            }
            catch (InvalidCommentException)
            {
                core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x05)");
            }
            catch (Exception ex)
            {
                core.Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x06) " + ex.ToString());
            }

            if (Request.Form["ajax"] == "true")
            {
                Template ct = new Template(Server.MapPath("./templates"), "pane.comment.html");
                template.Medium = core.Template.Medium;
                ct.SetProse(core.Prose);

                if (core.Session.IsLoggedIn && loggedInMember != null)
                {
                    ct.Parse("LOGGED_IN", "TRUE");
                    ct.Parse("USER_DISPLAY_NAME", core.Session.LoggedInMember.DisplayName);
                    ct.Parse("USER_TILE", core.Session.LoggedInMember.UserTile);
                    ct.Parse("USER_ICON", core.Session.LoggedInMember.UserIcon);
                }

                VariableCollection commentsVariableCollection = ct.CreateChild("comment-list");

                //commentsVariableCollection.ParseRaw("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment), core.session.LoggedInMember));
                core.Display.ParseBbcode(commentsVariableCollection, "COMMENT", comment);
                // TODO: finish comments this
                commentsVariableCollection.Parse("ID", commentId.ToString());
                commentsVariableCollection.Parse("USERNAME", loggedInMember.DisplayName);
                commentsVariableCollection.Parse("U_PROFILE", loggedInMember.ProfileUri);
                commentsVariableCollection.Parse("U_QUOTE", core.Hyperlink.BuildCommentQuoteUri(commentId));
                commentsVariableCollection.Parse("U_REPORT", core.Hyperlink.BuildCommentReportUri(commentId));
                commentsVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildCommentDeleteUri(commentId));
                commentsVariableCollection.Parse("TIME", tz.DateTimeToString(tz.Now));
                commentsVariableCollection.Parse("USER_TILE", loggedInMember.UserTile);
                commentsVariableCollection.Parse("USER_ICON", loggedInMember.UserIcon);

                try
                {
                    if (core.Session.IsLoggedIn)
                    {
                        if (thisItem.Owner.CanModerateComments(loggedInMember))
                        {
                            commentsVariableCollection.Parse("MODERATE", "TRUE");
                        }

                        if (thisItem.Owner.IsItemOwner(loggedInMember))
                        {
                            commentsVariableCollection.Parse("OWNER", "TRUE");
                            commentsVariableCollection.Parse("NORMAL", "FALSE");
                        }
                        else
                        {
                            commentsVariableCollection.Parse("OWNER", "FALSE");
                            commentsVariableCollection.Parse("NORMAL", "TRUE");
                        }
                    }
                    else
                    {
                        commentsVariableCollection.Parse("OWNER", "FALSE");
                        commentsVariableCollection.Parse("NORMAL", "TRUE");
                    }
                }
                catch (Exception ex)
                {
                    commentsVariableCollection.Parse("NORMAL", "FALSE");
                }

                core.Ajax.SendRawText("comment", ct.ToString());

                if (db != null)
                {
                    db.CloseConnection();
                }
                Response.End();
                return;
            }
            else
            {
                string redirect = Request["redirect"];
                if (!string.IsNullOrEmpty(redirect))
                {
                    template.Parse("REDIRECT_URI", redirect);
                }
                core.Display.ShowMessage("Comment Posted", "Your comment has been successfully posted.");
            }
        }
    }
}
