/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
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
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string comment;
            long itemId;
            string itemType;
            long commentId = -1;
            bool isAjax = false;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            if (Request.QueryString["mode"] == "quote")
            {
                template.SetTemplate("posting.comment.html");

                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    Ajax.SendRawText("errorFetchingComment", "");
                    return;
                }

                DataTable commentsTable = db.Query(string.Format("SELECT ui.user_name, c.comment_text FROM comments c LEFT JOIN user_info ui ON c.user_id = ui.user_id WHERE comment_id = {0}",
                    itemId));

                if (commentsTable.Rows.Count == 1)
                {
                    string quotedComment = string.Format("\n\n[quote=\"{0}\"]{1}[/quote]",
                        (string)commentsTable.Rows[0]["user_name"], (string)commentsTable.Rows[0]["comment_text"]);

                    template.ParseVariables("COMMENT_TEXT", HttpUtility.HtmlEncode(quotedComment));
                }
                else
                {
                    Ajax.SendRawText("errorFetchingComment", "");
                }

                EndResponse();
            }

            if (Request.QueryString["mode"] == "fetch")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    Ajax.SendRawText("errorFetchingComment", "");
                    return;
                }

                DataTable commentsTable = db.Query(string.Format("SELECT ui.user_name, c.comment_text FROM comments c LEFT JOIN user_info ui ON c.user_id = ui.user_id WHERE comment_id = {0}",
                    itemId));

                if (commentsTable.Rows.Count == 1)
                {
                    Ajax.SendRawText("errorFetchingComment", (string.Format("\n\n[quote=\"{0}\"]{1}[/quote]",
                        HttpUtility.HtmlEncode((string)commentsTable.Rows[0]["user_name"]), HttpUtility.HtmlEncode((string)commentsTable.Rows[0]["comment_text"]))));
                }
                else
                {
                    Ajax.SendRawText("errorFetchingComment", "");
                }

                return;
            }

            if (Request.QueryString["mode"] == "report")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    Ajax.ShowMessage(isAjax, "errorReportingComment", "Error", "The comment you have reported is invalid.");
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
                        Ajax.ShowMessage(isAjax, "alreadyReported", "Already Reported", "You have already reported this comment as SPAM.");
                    }
                }
                Ajax.ShowMessage(isAjax, "commentReported", "Reported Comment", "You have successfully reported a comment.");
            }

            if (Request.QueryString["mode"] == "delete")
            {
                try
                {
                    itemId = long.Parse((string)Request.QueryString["item"]);
                }
                catch
                {
                    Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                    return;
                }

                // select the comment
                try
                {
                    Comment thisComment = new Comment(core, itemId);

                    long commentItemId = thisComment.ItemId;
                    string commentItemType = thisComment.ItemType;

                    try
                    {
                        ApplicationEntry ae = new ApplicationEntry(core, new ApplicationCommentType(commentItemType));

                        BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
                    }
                    catch (InvalidApplicationException)
                    {
                        Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x01)");
                        return;
                    }

                    try
                    {
                        if (!core.CanDeleteComment(commentItemType, commentItemId))
                        {
                            Ajax.ShowMessage(isAjax, "permissionDenied", "Permission Denied", "You do not have the permissions to delete this comment.");
                        }
                    }
                    catch (InvalidItemException)
                    {
                        Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
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
                        db.UpdateQuery(string.Format("DELETE FROM comments WHERE comment_id = {0}",
                            itemId));
                    }

                    core.LoadUserProfile(thisComment.UserId);
                    Member poster = core.UserProfiles[thisComment.UserId];
                    Core.CommentDeleted(commentItemType, (long)commentItemId, thisComment, poster);
                    Core.AdjustCommentCount(commentItemType, (long)commentItemId, -1);
                }
                catch (InvalidCommentException)
                {
                    Ajax.ShowMessage(isAjax, "errorDeletingComment", "Error", "An error was encountered while deleting the comment, the comment has not been deleted.");
                }

                Ajax.ShowMessage(isAjax, "commentDeleted", "Comment Deleted", "You have successfully deleted the comment.");
            }

            try
            {
                comment = (string)Request.Form["comment"];
                itemId = long.Parse((string)Request.Form["item"]);
                itemType = (string)Request.Form["type"];
            }
            catch
            {
                Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x02)");
                return;
            }

            try
            {
                ApplicationEntry ae = new ApplicationEntry(core, new ApplicationCommentType(itemType));

                BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
            }
            catch (InvalidApplicationException)
            {
                Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x03)");
                return;
            }

            /* save comment in the database */

            try
            {
                if (!Core.CanPostComment(itemType, (long)itemId))
                {
                    Ajax.ShowMessage(isAjax, "notLoggedIn", "Permission Denied", "You do not have the permissions to post a comment to this item.");
                }
            }
            catch (InvalidItemException)
            {
                Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x04)");
            }

            try
            {
                Comment commentObject = Comment.Create(Core, itemType, (long)itemId, comment);
                commentId = commentObject.CommentId;

                Core.AdjustCommentCount(itemType, (long)itemId, 1);
                Core.CommentPosted(itemType, (long)itemId, commentObject, loggedInMember);
            }
            catch (NotLoggedInException)
            {
                Ajax.ShowMessage(isAjax, "notLoggedIn", "Not Logged In", "You must be logged in to post a comment.");
            }
            catch (CommentFloodException)
            {
                Ajax.ShowMessage(isAjax, "rejectedByFloodControl", "Posting Too Fast", "You are posting too fast. Please wait a minute and try again.");
            }
            catch (CommentTooLongException)
            {
                Ajax.ShowMessage(isAjax, "commentTooLong", "Comment Too Long", "The comment you have attempted to post is too long, maximum size is 511 characters.");
            }
            catch (CommentTooShortException)
            {
                Ajax.ShowMessage(isAjax, "commentTooShort", "Comment Too Short", "The comment you have attempted to post is too short, must be longer than two characters.");
            }
            catch (InvalidCommentException)
            {
                Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x05)");
            }
            catch (Exception ex)
            {
                Ajax.ShowMessage(isAjax, "invalidComment", "Invalid Comment", "The comment you have attempted to post is invalid. (0x06) " + ex.ToString());
            }

            if (Request.Form["ajax"] == "true")
            {
                Template ct = new Template(Server.MapPath("./templates"), "pane.comment.html");

                VariableCollection commentsVariableCollection = ct.CreateChild("comment-list");

                commentsVariableCollection.ParseVariables("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment), core.session.LoggedInMember));
                // TODO: finish comments this
                commentsVariableCollection.ParseVariables("ID", commentId.ToString());
                commentsVariableCollection.ParseVariables("USERNAME", loggedInMember.DisplayName);
                commentsVariableCollection.ParseVariables("U_PROFILE", loggedInMember.ProfileUri);
                commentsVariableCollection.ParseVariables("U_QUOTE", HttpUtility.HtmlEncode(Linker.BuildCommentQuoteUri(commentId)));
                commentsVariableCollection.ParseVariables("U_REPORT", HttpUtility.HtmlEncode(Linker.BuildCommentReportUri(commentId)));
                commentsVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildCommentDeleteUri(commentId)));
                commentsVariableCollection.ParseVariables("TIME", tz.DateTimeToString(tz.Now));
                commentsVariableCollection.ParseVariables("USER_TILE", HttpUtility.HtmlEncode(loggedInMember.UserTile));

                commentsVariableCollection.ParseVariables("NORMAL", "TRUE");

                //Response.Write(ct.ToString());
                Ajax.SendRawText("comment", ct.ToString());

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
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(redirect));
                }
                Display.ShowMessage("Comment Posted", "Your comment has been successfully posted.");
            }
        }
    }
}
