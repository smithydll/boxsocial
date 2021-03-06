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
                    core.Response.SendRawText("errorFetchingComment", "");
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
                    core.Response.SendRawText("errorFetchingComment", "");
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
                    core.Response.SendRawText("errorFetchingComment", "");
                    return;
                }

                DataTable commentsTable = db.Query(string.Format("SELECT ui.user_name, c.comment_text FROM comments c LEFT JOIN user_info ui ON c.user_id = ui.user_id WHERE comment_id = {0}",
                    itemId));

                if (commentsTable.Rows.Count == 1)
                {
                    core.Response.SendRawText("commentFetched", (string.Format("\n\n[quote=\"{0}\"]{1}[/quote]",
                        (string)commentsTable.Rows[0]["user_name"], (string)commentsTable.Rows[0]["comment_text"])));
                }
                else
                {
                    core.Response.SendRawText("errorFetchingComment", "");
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
                    core.Response.SendRawText("errorFetchingComment", "");
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
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comments you have attempted to fetch are invalid. (0x01)");
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
                        core.Response.ShowMessage("invalidItem", "Item no longer exists", "Cannot load the comments as the item no longer exists.");
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
                        core.Response.ShowMessage("accessDenied", "Access Denied", "The you do not have access to these comments");
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
                        core.Response.ShowMessage("accessDenied", "Access Denied", "The you do not have access to these comments");
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

                    core.Response.SendRawText("fetchSuccess", template.ToString());
                }
                else
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comments you have attempted to fetch are invalid. (0x07)");
                }
                return;
            }

            if (mode == "report")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    core.Response.ShowMessage("errorReportingComment", "Error", "The comment you have reported is invalid.");
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
                        core.Response.ShowMessage("alreadyReported", "Already Reported", "You have already reported this comment as SPAM.");
                    }
                }
                core.Response.ShowMessage("commentReported", "Reported Comment", "You have successfully reported a comment.");
                return;
            }

            if (mode == "delete")
            {
                // select the comment
                try
                {
                    Comment.Delete(core);
                }
                catch (InvalidCommentException)
                {
                    core.Response.ShowMessage("errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                }
                catch (PermissionDeniedException)
                {
                    core.Response.ShowMessage("permissionDenied", "Permission Denied", "You do not have the permissions to delete this comment.");
                }

                if (core.ResponseFormat == ResponseFormats.Xml)
                {
                    core.Response.SendRawText("commentDeleted", "You have successfully deleted the comment.");
                }
                else
                {
                    core.Response.ShowMessage("commentDeleted", "Comment Deleted", "You have successfully deleted the comment");
                }
                return;
            }

            // else we post a comment
            {
                try
                {
                    comment = (string)Request.Form["comment"];
                    itemId = core.Functions.RequestLong("item_id", 0);
                    itemTypeId = core.Functions.RequestLong("item_type_id", 0);
                    itemKey = new ItemKey(itemId, itemTypeId);
                }
                catch
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x02)");
                    return;
                }

                if (itemId == 0 || itemTypeId == 0)
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x08)");
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
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x03)");
                    return;
                }

                /* save comment in the database */

                NumberedItem item = null;
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
                            core.Response.ShowMessage("notLoggedIn", "Permission Denied", "You do not have the permissions to post a comment to this item.");
                        }
                    }
                    else
                    {
                        core.Response.ShowMessage("invalidComment", "Invalid Item", "The comment you have attempted to post is invalid. (0x07)");
                    }
                }
                catch (InvalidItemException)
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x04)");
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
                            ae.PublishToFeed(core, core.Session.LoggedInMember, commentObject, item, Functions.SingleLine(core.Bbcode.Flatten(commentObject.Body)));
                        }
                        ICommentableItem citem = (ICommentableItem)item;

                        citem.CommentPosted(new CommentPostedEventArgs(commentObject, core.Session.LoggedInMember, new ItemKey(itemId, itemTypeId)));
                    }

                    Comment.Commented(core, itemKey);

                    // Notify everyone who comments on the item by default, track this so people can unsubscribe later
                    //NotificationSubscription.Create(core, loggedInMember, itemKey);
                    try
                    {
                        Subscription.SubscribeToItem(core, itemKey);
                    }
                    catch (AlreadySubscribedException)
                    {
                        // not a problem
                    }

                }
                catch (NotLoggedInException)
                {
                    core.Response.ShowMessage("notLoggedIn", "Not Logged In", "You must be logged in to post a comment.");
                }
                catch (CommentFloodException)
                {
                    core.Response.ShowMessage("rejectedByFloodControl", "Posting Too Fast", "You are posting too fast. Please wait a minute and try again.");
                }
                catch (CommentTooLongException)
                {
                    core.Response.ShowMessage("commentTooLong", "Comment Too Long", "The comment you have attempted to post is too long, maximum size is 511 characters.");
                }
                catch (CommentTooShortException)
                {
                    core.Response.ShowMessage("commentTooShort", "Comment Too Short", "The comment you have attempted to post is too short, must be longer than two characters.");
                }
                catch (InvalidCommentException)
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x05)");
                }
                catch (Exception ex)
                {
                    core.Response.ShowMessage("invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x06) " + ex.ToString());
                }

                if (core.ResponseFormat == ResponseFormats.Xml)
                {
                    Template ct = new Template(Server.MapPath("./templates"), "pane.comment.html");
                    template.Medium = core.Template.Medium;
                    ct.SetProse(core.Prose);

                    if (core.Session.IsLoggedIn && loggedInMember != null)
                    {
                        ct.Parse("LOGGED_IN", "TRUE");
                        ct.Parse("USER_DISPLAY_NAME", core.Session.LoggedInMember.DisplayName);
                        ct.Parse("USER_TILE", core.Session.LoggedInMember.Tile);
                        ct.Parse("USER_ICON", core.Session.LoggedInMember.Icon);
                    }

                    if (item != null)
                    {
                        template.Parse("ITEM_ID", item.Id.ToString());
                        template.Parse("ITEM_TYPE", item.ItemKey.TypeId.ToString());
                    }

                    VariableCollection commentsVariableCollection = ct.CreateChild("comment-list");

                    //commentsVariableCollection.ParseRaw("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment), core.session.LoggedInMember));
                    core.Display.ParseBbcode(commentsVariableCollection, "COMMENT", comment);
                    // TODO: finish comments this
                    commentsVariableCollection.Parse("ID", commentId.ToString());
                    commentsVariableCollection.Parse("TYPE_ID", ItemKey.GetTypeId(core, typeof(Comment)));
                    commentsVariableCollection.Parse("USERNAME", loggedInMember.DisplayName);
                    commentsVariableCollection.Parse("USER_ID", loggedInMember.Id.ToString());
                    commentsVariableCollection.Parse("U_PROFILE", loggedInMember.ProfileUri);
                    commentsVariableCollection.Parse("U_QUOTE", core.Hyperlink.BuildCommentQuoteUri(commentId));
                    commentsVariableCollection.Parse("U_REPORT", core.Hyperlink.BuildCommentReportUri(commentId));
                    commentsVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildCommentDeleteUri(commentId));
                    commentsVariableCollection.Parse("TIME", tz.DateTimeToString(tz.Now));
                    commentsVariableCollection.Parse("USER_TILE", loggedInMember.Tile);
                    commentsVariableCollection.Parse("USER_ICON", loggedInMember.Icon);

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

                    core.Response.SendRawText("comment", ct.ToString());

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
}
