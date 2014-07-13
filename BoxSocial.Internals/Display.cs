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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Services;
using System.Web.Services.Protocols;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ConfirmBoxResult
    {
        None,
        Yes,
        No,
    }

    public enum ShowMessageOptions
    {
        Unprocessed,
        Bbcode,
        Stripped,
    }

    public enum PaginationOptions
    {
        Normal = 0x01,
        Blog = 0x02,
        Minimal = 0x04,
    }

    public class Display
    {
        const string RANK_ACTIVE = "/images/star-on.png";
        const string RANK_RATING = "/images/star-on.png";
        const string RANK_SHADOW = "/images/star-off.png";

        private Core core;

        public Display(Core core)
        {
            this.core = core;
        }

        public delegate void DisplayCommentHookHandler(DisplayCommentHookEventArgs e);

        // Normal
        public void ParsePagination(Template template, string baseUri, int itemsPerPage, long totalItems)
        {
            ParsePagination(template, "PAGINATION", baseUri, 0, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(Template template, string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            ParsePagination(template, "PAGINATION", baseUri, pageLevel, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(string baseUri, int itemsPerPage, long totalItems)
        {
            ParsePagination(core.Template, "PAGINATION", baseUri, 0, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            ParsePagination(core.Template, "PAGINATION", baseUri, pageLevel, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(string templateVar, string baseUri, int itemsPerPage, long totalItems)
        {
            ParsePagination(core.Template, templateVar, baseUri, 0, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(string templateVar, string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            ParsePagination(core.Template, templateVar, baseUri, pageLevel, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(Template template, string templateVar, string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            ParsePagination(template, templateVar, baseUri, pageLevel, itemsPerPage, totalItems, false);
        }

        public void ParsePagination(Template template, string templateVar, string baseUri, int pageLevel, int itemsPerPage, long totalItems, bool minimal)
        {
            int maxPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            template.ParseRaw(templateVar, GeneratePagination(baseUri, pageLevel, core.PageNumber, core.PageOffset, maxPages, (minimal ? PaginationOptions.Minimal : PaginationOptions.Normal)));
        }

        // Minimal
        public void ParseMinimalPagination(VariableCollection template, string templateVar, string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            int maxPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            template.ParseRaw(templateVar, GeneratePagination(baseUri, pageLevel, core.PageNumber, core.PageOffset, maxPages, PaginationOptions.Minimal));
        }

        public void ParseMinimalPagination(Template template, string templateVar, string baseUri, int pageLevel, int itemsPerPage, long totalItems)
        {
            int maxPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            template.ParseRaw(templateVar, GeneratePagination(baseUri, pageLevel, core.PageNumber, core.PageOffset, maxPages, PaginationOptions.Minimal));
        }

        // Blog
        public void ParseBlogPagination(Template template, string templateVar, string baseUri, int pageLevel, long offsetItemId)
        {
            template.ParseRaw(templateVar, GenerateBlogPagination(baseUri, pageLevel, core.PageNumber, core.PageOffset, offsetItemId));
        }

        private string BuildPageAndOffset(int pageLevel, int[] currentPage, long[] currentOffset, int newPage)
        {
            return BuildPageAndOffset(pageLevel, currentPage, currentOffset, newPage, -1);
        }

        private string BuildPageAndOffset(int pageLevel, int[] currentPage, long[] currentOffset, int newPage, long newOffset)
        {
            string page = string.Empty;
            string offset = string.Empty;

            for (int i = 0; i < currentPage.Length; i++)
            {
                if (i > 0)
                {
                    page += ",";
                }
                if (i == pageLevel)
                {
                    page += newPage.ToString();
                }
                else
                {
                    page += currentPage[i];
                }
            }

            for (int i = 0; i < currentOffset.Length; i++)
            {
                if (i > 0)
                {
                    offset += ",";
                }
                if (i == pageLevel)
                {
                    if (newOffset > 0)
                    {
                        offset += newOffset.ToString();
                    }
                }
                else
                {
                    if (currentOffset[i] > 0)
                    {
                        offset += currentOffset[i];
                    }
                }
            }

            if (offset.Replace(",", "") != string.Empty)
            {
                return string.Format("p={0}&amp;o={1}", page, offset);
            }
            else
            {
                return string.Format("p={0}", page);
            }
        }

        private bool AllFirstPage(int pageLevel, int[] currentPage)
        {
            for (int i = 0; i < currentPage.Length; i++)
            {
                if (i == pageLevel) continue;
                if (currentPage[i] != 1) return false;
            }

            return true;
        }

        private string GenerateBlogPagination(string baseUri, int pageLevel, int[] currentPage, long[] currentOffset, long offsetItemId)
        {
            StringBuilder pagination = new StringBuilder();
            bool baseAmp = baseUri.Contains("?");
            string ampSymbol = "?";

            if (baseAmp)
            {
                ampSymbol = "&";
            }

            if (core.IsMobile)
            {
                if (offsetItemId > 0)
                {
                    pagination.Append(string.Format("<a href=\"{0}{1}\" data-role=\"button\" data-inline=\"true\">&laquo; Older Entries</a>",
                        HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, currentPage[pageLevel] + 1, offsetItemId)));
                }
                pagination.Append("&nbsp;");
                if (currentPage[pageLevel] > 2)
                {
                    pagination.Append(string.Format("<a href=\"{0}{1}\" data-role=\"button\" data-inline=\"true\">Newer Entries &raquo;</a>",
                        HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, currentPage[pageLevel] - 1, 0)));
                }
                else if (currentPage[pageLevel] > 1)
                {
                    pagination.Append(string.Format("<a href=\"{0}\" data-role=\"button\" data-inline=\"true\">Newer Entries &raquo;</a>",
                        HttpUtility.HtmlEncode(baseUri)));
                }
            }
            else
            {
                if (currentPage[pageLevel] > 2)
                {
                    pagination.Append(string.Format("<a style=\"float: right; display: block;\" href=\"{0}{1}\">Newer Entries &raquo;</a>",
                        HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, currentPage[pageLevel] - 1, 0)));
                }
                else if (currentPage[pageLevel] > 1)
                {
                    pagination.Append(string.Format("<a style=\"float: right; display: block;\" href=\"{0}\">Newer Entries &raquo;</a>",
                        HttpUtility.HtmlEncode(baseUri)));
                }
                pagination.Append("&nbsp;");
                if (offsetItemId > 0)
                {
                    pagination.Append(string.Format("<a href=\"{0}{1}\">&laquo; Older Entries</a>",
                        HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, currentPage[pageLevel] + 1, offsetItemId)));
                }
            }

            return pagination.ToString();
        }

        /// <summary>
        /// Base uri is html encoded by this function, do not pre-process
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="currentPage"></param>
        /// <param name="maxPages"></param>
        /// <returns></returns>
        private string GeneratePagination(string baseUri, int pageLevel, int[] currentPage, long[] currentOffset, int maxPages, PaginationOptions options)
        {
            StringBuilder pagination = new StringBuilder();
            bool comma = false;
            bool skipped = false;
            bool baseAmp = baseUri.Contains("?");
            string ampSymbol = "?";

            if (baseAmp)
            {
                ampSymbol = "&";
            }

            int page = 1;
            if (pageLevel < currentPage.Length)
            {
                page = currentPage[pageLevel];
            }
            else
            {
                int[] oldPage = currentPage;
                currentPage = new int[pageLevel + 1];
                oldPage.CopyTo(currentPage, 0);
                currentPage[pageLevel] = page;
            }

            if (core.IsMobile)
            {
                if (maxPages > 1)
                {
                    // Previous
                    if (page > 1)
                    {
                        pagination.Append(string.Format("<a href=\"{0}p={1}\" data-role=\"button\" data-inline=\"true\">&laquo;</a>",
                            HttpUtility.HtmlEncode(baseUri + ampSymbol), page - 1));
                    }
                    else
                    {
                        pagination.Append("<a href=\"#\" class=\"ui-disabled\" data-role=\"button\" data-inline=\"true\">&laquo;</a>");
                    }

                    // First
                    pagination.Append(string.Format("<a href=\"{0}p={1}\" data-role=\"button\" data-inline=\"true\">{1}</a>",
                        HttpUtility.HtmlEncode(baseUri + ampSymbol), 1));

                    if (maxPages > 1)
                    {
                        // Select box

                        // Last
                        pagination.Append(string.Format("<a href=\"{0}p={1}\" data-role=\"button\" data-inline=\"true\">{1}</a>",
                            HttpUtility.HtmlEncode(baseUri + ampSymbol), maxPages));
                    }

                    // Next
                    if (page < maxPages)
                    {
                        pagination.Append(string.Format("<a href=\"{0}p={1}\" data-role=\"button\" data-inline=\"true\">&raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri + ampSymbol), page + 1));
                    }
                    else
                    {
                        pagination.Append("<a href=\"#\" class=\"ui-disabled\" data-role=\"button\" data-inline=\"true\">&raquo;</a>");
                    }
                }
            }
            else
            {
                if ((options & PaginationOptions.Normal) == PaginationOptions.Normal)
                {
                    if (page > 2 || ((!AllFirstPage(pageLevel, currentPage)) && page > 1 && currentPage.Length > 1))
                    {
                        pagination.Append(string.Format("<a href=\"{0}{1}\">&laquo; Prev</a>",
                            HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, page - 1)));
                        comma = true;
                    }
                    else if (page > 1)
                    {
                        pagination.Append(string.Format("<a href=\"{0}\">&laquo; Prev</a>",
                            HttpUtility.HtmlEncode(baseUri)));
                        comma = true;
                    }
                }

                int firstCount = 3;
                if ((options & PaginationOptions.Minimal) == PaginationOptions.Minimal)
                {
                    firstCount = 1;
                }

                for (int i = 1; i <= maxPages; i++)
                {
                    if (i != page)
                    {
                        if (i == 1)
                        {
                            if (comma)
                            {
                                pagination.Append(", ");
                            }
                            else
                            {
                                comma = true;
                            }

                            pagination.Append(string.Format("<a href=\"{0}\">{1}</a>",
                                HttpUtility.HtmlEncode(baseUri), i));
                        }
                        else
                        {
                            if ((i > firstCount && i < page - (firstCount - 1)) || (i < maxPages - 2 && i > page + 2))
                            {
                                skipped = true;
                                continue;
                            }

                            if (comma)
                            {
                                pagination.Append(", ");
                            }
                            else
                            {
                                comma = true;
                            }

                            if (skipped)
                            {
                                pagination.Append("... ");
                                skipped = false;
                            }

                            pagination.Append(string.Format("<a href=\"{0}{1}\">{2}</a>",
                                HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, i), i));
                        }
                    }
                    else // current page
                    {
                        if (comma)
                        {
                            pagination.Append(", ");
                        }
                        else
                        {
                            comma = true;
                        }

                        pagination.Append(string.Format("<strong>{0}</strong>",
                            i));
                    }
                }

                if ((options & PaginationOptions.Normal) == PaginationOptions.Normal)
                {
                    if (page < maxPages)
                    {
                        pagination.Append(", ");
                        pagination.Append(string.Format("<a href=\"{0}{1}\">Next &raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri + ampSymbol), BuildPageAndOffset(pageLevel, currentPage, currentOffset, page + 1)));
                    }
                }
            }
            return pagination.ToString();
        }

        public void ParseBreadCrumbs(List<string[]> parts)
        {
            ParseBreadCrumbs("BREADCRUMBS", parts);
        }

        public void ParseBreadCrumbs(string templateVar, List<string[]> parts)
        {
            ParseBreadCrumbs(core.Template, templateVar, parts);
        }

        public void ParseBreadCrumbs(Template template, string templateVar, List<string[]> parts)
        {
            template.ParseRaw(templateVar, GenerateBreadCrumbs(parts));
        }

        public string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = "/";

            if (core.IsMobile)
            {
                if (parts.Count > 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        parts[parts.Count - 2][1], path + parts[parts.Count - 2][0].TrimStart(new char[] { '*' }));
                }
                if (parts.Count == 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        Hyperlink.Domain, path);
                }
            }
            else
            {
                output = string.Format("<a href=\"{1}\">{0}</a>",
                        Hyperlink.Domain, path);

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i][0] != "")
                    {
                        output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                            parts[i][1], path + parts[i][0].TrimStart(new char[] { '*' }));
                        if (!parts[i][0].StartsWith("*"))
                        {
                            path += parts[i][0] + "/";
                        }
                    }
                }
            }

            return output;
        }

        public void ShowMessage(string title, string message)
        {
            ShowMessage(title, message, ShowMessageOptions.Unprocessed);
        }

        public void ShowMessage(string title, string message, ShowMessageOptions options)
        {
            core.Template.SetTemplate("std.message.html");
            core.Template.Parse("IS_CONTENT", "FALSE");
            core.Template.Parse("MESSAGE_TITLE", title);
            switch (options)
            {
                case ShowMessageOptions.Unprocessed:
                    core.Template.Parse("MESSAGE_BODY", message);
                    break;
                case ShowMessageOptions.Bbcode:
                    core.Display.ParseBbcode("MESSAGE_BODY", message);
                    break;
                case ShowMessageOptions.Stripped:
                    // TODO: stripped bbcode parse
                    core.Template.Parse("MESSAGE_BODY", core.Bbcode.Flatten(message));
                    break;
            }

            /* Something wrong here, .net likes to end the page TWICE! */
            /* Fixed by adding a flag to EndResponse to only execute once. */
            core.EndResponse();
        }

        public ConfirmBoxResult GetConfirmBoxResult()
        {
            if (core.Http.Form["1"] != null)
            {
                return ConfirmBoxResult.Yes;
            }
            else if (core.Http.Form["0"] != null)
            {
                return ConfirmBoxResult.No;
            }
            else
            {
                return ConfirmBoxResult.None;
            }
        }

        public ConfirmBoxResult ShowConfirmBox(string formAction, string title, string message, Dictionary<string, string> hiddenFieldList)
        {
            if (core.Http.Form["1"] != null)
            {
                return ConfirmBoxResult.Yes;
            }
            else if (core.Http.Form["0"] != null)
            {
                return ConfirmBoxResult.No;
            }

            core.Template.SetTemplate("std.confirm.html");
            core.Template.Parse("IS_CONTENT", "FALSE");

            core.Template.Parse("S_FORM_ACTION", formAction);
            core.Template.Parse("CONFIRM_TITLE", title);
            core.Template.Parse("CONFIRM_TEXT", message);

            foreach (string key in hiddenFieldList.Keys)
            {
                VariableCollection hiddenVariableCollection = core.Template.CreateChild("hidden_list");

                hiddenVariableCollection.Parse("NAME", key);
                hiddenVariableCollection.Parse("VALUE", hiddenFieldList[key]);
            }

            core.page.EndResponse();

            return ConfirmBoxResult.None;
        }

        public void DisplayComments(Template template, User profileOwner, ICommentableItem item)
        {
            DisplayComments(template, (Primitive)profileOwner, item);
        }

        public void DisplayComments(Template template, User profileOwner, ICommentableItem item, DisplayCommentHookHandler hook)
        {
            DisplayComments(template, (Primitive)profileOwner, item, null, -1, hook);
        }

        public void DisplayComments(Template template, Primitive owner, ICommentableItem item)
        {
            int page = core.CommentPageNumber;

            DisplayComments(template, owner, page, item);
        }

        public void DisplayComments(Template template, Primitive owner, int page, ICommentableItem item)
        {
            DisplayComments(template, owner, item, null, page, -1, null);
        }

        public void DisplayComments(Template template, Primitive owner, ICommentableItem item, List<User> commenters, long commentCount, DisplayCommentHookHandler hook)
        {
            int page = core.CommentPageNumber;

            DisplayComments(template, owner, item, commenters, page, commentCount, hook);
        }

        public void DisplayComments(Template template, Primitive owner, ICommentableItem item, List<User> commenters, int page, long commentCount, DisplayCommentHookHandler hook)
        {
            Mysql db = core.Db;

            long c = core.Functions.RequestLong("c", 0);

            if (c > 0)
            {
                SelectQuery query = new SelectQuery("comments");
                query.AddFields("COUNT(*) AS total");
                query.AddCondition("comment_item_id", item.Id);
                query.AddCondition("comment_item_type_id", item.ItemKey.TypeId);
                query.AddCondition("comment_id", ConditionEquality.LessThanEqual, c);

                if (commenters != null)
                {
                    if (commenters.Count == 2)
                    {
                        if (item.Namespace == "USER")
                        {
                            QueryCondition qc1 = query.AddCondition("c.comment_item_id", commenters[0].Id);
                            qc1.AddCondition("user_id", commenters[1].Id);

                            QueryCondition qc2 = query.AddCondition(ConditionRelations.Or, "c.comment_item_id", commenters[1].Id);
                            qc2.AddCondition("user_id", commenters[0].Id);

                            query.AddCondition("c.comment_item_type_id", item.ItemKey.TypeId);
                        }
                        else
                        {
                            query.AddCondition("comment_item_id", item.Id);
                            query.AddCondition("comment_item_type_id", item.ItemKey.TypeId);
                        }
                    }
                    else
                    {
                        query.AddCondition("comment_item_id", item.Id);
                        query.AddCondition("comment_item_type_id", item.ItemKey.TypeId);
                    }
                }
                else
                {
                    query.AddCondition("comment_item_id", item.Id);
                    query.AddCondition("comment_item_type_id", item.ItemKey.TypeId);
                }

                query.AddSort(SortOrder.Ascending, "comment_time_ut");

                DataRow commentsRow = db.Query(query).Rows[0];

                long before = (long)commentsRow["total"];
                long after = item.Comments - before - 1;

                if (item.CommentSortOrder == SortOrder.Ascending)
                {
                    page = (int)(before / item.CommentsPerPage + 1);
                }
                else
                {
                    page = (int)(after / item.CommentsPerPage + 1);
                }
            }

            if (core.Session.IsLoggedIn && core.Session.LoggedInMember != null)
            {
                template.Parse("LOGGED_IN", "TRUE");
                template.Parse("USER_DISPLAY_NAME", core.Session.LoggedInMember.DisplayName);
                template.Parse("USER_ICON", core.Session.LoggedInMember.Icon);
                template.Parse("USER_TILE", core.Session.LoggedInMember.Tile);
                template.Parse("USER_SQUARE", core.Session.LoggedInMember.UserSquare);
            }

            List<Comment> comments = Comment.GetComments(core, item.ItemKey, item.CommentSortOrder, page, item.CommentsPerPage, commenters);
            Comment.LoadUserInfoCache(core, comments);

            if (commentCount >= 0)
            {
                template.Parse("COMMENTS", commentCount.ToString());
            }
            else if (((NumberedItem)item).Info.Comments >= 0)
            {
                template.Parse("COMMENTS", ((NumberedItem)item).Info.Comments.ToString());
            }
            else
            {
                template.Parse("COMMENTS", comments.Count.ToString());
            }

            template.Parse("ITEM_ID", item.Id.ToString());
            template.Parse("ITEM_TYPE", item.ItemKey.TypeId.ToString());

            if (item.CommentSortOrder == SortOrder.Ascending)
            {
                template.Parse("COMMENTS_ASC", "TRUE");
                template.Parse("COMMENT_SORT", "asc");
            }
            else
            {
                template.Parse("COMMENTS_DESC", "TRUE");
                template.Parse("COMMENT_SORT", "desc");
            }

            foreach (Comment comment in comments)
            {
                core.PrimitiveCache.LoadUserProfile(comment.UserId);
            }

            long lastId = 0;

            foreach (Comment comment in comments)
            {
                VariableCollection commentsVariableCollection = template.CreateChild("comment-list");

                //commentsVariableCollection.ParseRaw("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment.Body), core.session.LoggedInMember));
                /*if ((!core.IsMobile) && (!string.IsNullOrEmpty(comment.BodyCache)))
                {
                    core.Display.ParseBbcodeCache(commentsVariableCollection, "COMMENT", comment.BodyCache);
                }
                else*/
                {
                    core.Display.ParseBbcode(commentsVariableCollection, "COMMENT", comment.Body, true, null, null);
                }

                try
                {
                    User commentPoster = core.PrimitiveCache[comment.UserId];

                    lastId = comment.Id;

                    commentsVariableCollection.Parse("ID", comment.Id.ToString());
                    commentsVariableCollection.Parse("TYPE_ID", ItemKey.GetTypeId(typeof(Comment)));
                    commentsVariableCollection.Parse("USERNAME", commentPoster.DisplayName);
                    commentsVariableCollection.Parse("USER_ID", commentPoster.Id.ToString());
                    commentsVariableCollection.Parse("USER_DISPLAY_NAME", commentPoster.DisplayName);
                    commentsVariableCollection.Parse("U_PROFILE", commentPoster.ProfileUri);
                    commentsVariableCollection.Parse("U_QUOTE", core.Hyperlink.BuildCommentQuoteUri(comment.Id));
                    commentsVariableCollection.Parse("U_REPORT", core.Hyperlink.BuildCommentReportUri(comment.Id));
                    commentsVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildCommentDeleteUri(comment.Id));
                    commentsVariableCollection.Parse("U_LIKE", core.Hyperlink.BuildLikeItemUri(comment.ItemTypeId, comment.Id));
                    commentsVariableCollection.Parse("TIME", core.Tz.DateTimeToString(comment.GetTime(core.Tz)));
                    commentsVariableCollection.Parse("USER_ICON", commentPoster.Icon);
                    commentsVariableCollection.Parse("USER_TILE", commentPoster.Tile);
                    commentsVariableCollection.Parse("USER_SQUARE", commentPoster.UserSquare);

                    if (comment.Info.Likes > 0)
                    {
                        commentsVariableCollection.Parse("LIKES", string.Format("{0:d} ", comment.Info.Likes));
                    }

                    if (comment.Info.Dislikes > 0)
                    {
                        commentsVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", comment.Info.Dislikes));
                    }

                    if (comment.Info.SharedTimes > 0)
                    {
                        commentsVariableCollection.Parse("SHARES", string.Format(" ({0:d})", comment.Info.SharedTimes));
                    }

                    if (hook != null)
                    {
                        hook(new DisplayCommentHookEventArgs(core, owner, commentPoster, commentsVariableCollection));
                    }

                    if (core.Session.IsLoggedIn)
                    {
                        if (owner.CanModerateComments(core.Session.LoggedInMember))
                        {
                            commentsVariableCollection.Parse("MODERATE", "TRUE");
                        }
                    }

                    if (owner.IsItemOwner(commentPoster))
                    {
                        commentsVariableCollection.Parse("OWNER", "TRUE");
                        commentsVariableCollection.Parse("NORMAL", "FALSE");
                    }
                    else
                    {
                        commentsVariableCollection.Parse("OWNER", "FALSE");
                        commentsVariableCollection.Parse("NORMAL", "TRUE");
                    }

                    if (comment.SpamScore >= 10)
                    {
                        commentsVariableCollection.Parse("IS_SPAM", "TRUE");
                    }
                }
                catch
                {
                    // if userid is 0, anonymous
                    commentsVariableCollection.Parse("USERNAME", "Anonymous");
                    commentsVariableCollection.Parse("TIME", core.Tz.DateTimeToString(comment.GetTime(core.Tz)));

                    commentsVariableCollection.Parse("OWNER", "FALSE");
                    commentsVariableCollection.Parse("NORMAL", "TRUE");
                }
            }

            template.Parse("LAST_ID", "lastId");
        }

        public static void RatingBlock(float existingRating, Template template, ItemKey itemKey)
        {
            RatingBlock(existingRating, null, template, itemKey);
        }

        public static void RatingBlock(float existingRating, VariableCollection variables, ItemKey itemKey)
        {
            RatingBlock(existingRating, variables, null, itemKey);
        }

        private static void RatingBlock(float existingRating, VariableCollection variables, Template template, ItemKey itemKey)
        {
            int starsRating = 0;
            string one, two, three, four, five;
            string one_class, two_class, three_class, four_class, five_class;
            one = two = three = four = five = RANK_SHADOW;
            one_class = two_class = three_class = four_class = five_class = "rank-off";

            starsRating = (int)Math.Round(existingRating);

            switch (starsRating)
            {
                case 5:
                    one = two = three = four = five = RANK_RATING;
                    one_class = two_class = three_class = four_class = five_class = "rank-on";
                    break;
                case 4:
                    one = two = three = four = RANK_RATING;
                    one_class = two_class = three_class = four_class = "rank-on";
                    break;
                case 3:
                    one = two = three = RANK_RATING;
                    one_class = two_class = three_class = "rank-on";
                    break;
                case 2:
                    one = two = RANK_RATING;
                    one_class = two_class = "rank-on";
                    break;
                case 1:
                    one = RANK_RATING;
                    one_class = "rank-on";
                    break;
            }

            /*if (template != null)
            {
                loopVars = new Dictionary<string, string>();
            }*/
            Dictionary<string, string> loopVars = new Dictionary<string, string>(StringComparer.Ordinal);

            loopVars.Add("STAR_ONE", one);
            loopVars.Add("STAR_TWO", two);
            loopVars.Add("STAR_THREE", three);
            loopVars.Add("STAR_FOUR", four);
            loopVars.Add("STAR_FIVE", five);

            loopVars.Add("STAR_ONE_CLASS", one_class);
            loopVars.Add("STAR_TWO_CLASS", two_class);
            loopVars.Add("STAR_THREE_CLASS", three_class);
            loopVars.Add("STAR_FOUR_CLASS", four_class);
            loopVars.Add("STAR_FIVE_CLASS", five_class);

            loopVars.Add("U_RATE_ONE_STAR", string.Format("/api/rate?rating=1&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_TWO_STAR", string.Format("/api/rate?rating=2&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_THREE_STAR", string.Format("/api/rate?rating=3&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_FOUR_STAR", string.Format("/api/rate?rating=4&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_FIVE_STAR", string.Format("/api/rate?rating=5&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));

            loopVars.Add("RATE_RATING", string.Format("{0:0.0}", existingRating));
            loopVars.Add("RATE_ACTIVE", RANK_ACTIVE);
            loopVars.Add("RATE_TYPE", itemKey.TypeId.ToString());
            loopVars.Add("S_RATEBAR", "TRUE");

            if (template != null)
            {
                template.ParseVariables(loopVars);
            }
            else
            {
                variables.ParseVariables(loopVars);
            }

        }

        public User fillLoggedInMember(System.Security.Principal.IPrincipal User, User loggedInMember)
        {
            if (loggedInMember == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return loggedInMember = new User(core, User.Identity.Name, UserLoadOptions.Info);
                }
            }
            return null;
        }

        /// <summary>
        /// Do all header preparation tasks
        /// </summary>
        /// <param name="template">The template that represents the current page</param>
        /// <param name="User"></param>
        /// <param name="loggedInMember"></param>
        public void Header(TPage page)
        {
            Template template = core.Template;
            SessionState session = page.session;

            string noHeader = core.Http["no-header"];
            if (noHeader == null || noHeader.ToLower() != "true")
            {
                template.Parse("S_HEADER", "TRUE");
            }

            template.Parse("TITLE", page.PageTitle); // the set page title function sanitises
            template.Parse("HEADING", page.Core.Settings.SiteTitle);
            template.Parse("SITE_TITLE", page.Core.Settings.SiteTitle);
            template.Parse("SITE_SLOGAN", page.Core.Settings.SiteSlogan);
            template.Parse("YEAR", DateTime.Now.Year.ToString());

            if (page.CanonicalUri != null)
            {
                template.Parse("CANONICAL_URI", page.CanonicalUri);
            }

            template.Parse("HEAD_COLOUR", "ffffff");
            template.Parse("HEAD_FORE_COLOUR", "black");

            if (core.Settings.UseCdn && !string.IsNullOrEmpty(page.Core.Settings.CdnStaticBucketDomain))
            {
                if (core.Http.IsSecure)
                {
                    template.Parse("U_STATIC", "https://" + page.Core.Settings.CdnStaticBucketDomain.TrimEnd(new char[] { '/' }));
                }
                else
                {
                    template.Parse("U_STATIC", "http://" + page.Core.Settings.CdnStaticBucketDomain.TrimEnd(new char[] { '/' }));
                }
            }

            /*
             * URIs
             */
            template.Parse("U_HOME", page.Core.Hyperlink.BuildHomeUri());
            template.Parse("U_ABOUT", page.Core.Hyperlink.BuildAboutUri());
            template.Parse("U_SAFETY", page.Core.Hyperlink.BuildSafetyUri());
            template.Parse("U_PRIVACY", page.Core.Hyperlink.BuildPrivacyUri());
            template.Parse("U_TOS", page.Core.Hyperlink.BuildTermsOfServiceUri());
            template.Parse("U_SIGNIN", page.Core.Hyperlink.BuildLoginUri());
            template.Parse("U_SIGNOUT", page.Core.Hyperlink.BuildLogoutUri());
            template.Parse("U_REGISTER", page.Core.Hyperlink.BuildRegisterUri());
            template.Parse("U_HELP", page.Core.Hyperlink.BuildHelpUri());
            template.Parse("U_SITEMAP", page.Core.Hyperlink.BuildSitemapUri());
            template.Parse("U_COPYRIGHT", page.Core.Hyperlink.BuildCopyrightUri());
            template.Parse("U_SEARCH", page.Core.Hyperlink.BuildSearchUri());
            template.Parse("S_SEARCH", page.Core.Hyperlink.BuildSearchUri());

            template.Parse("U_FOOT_GROUPS", page.Core.Hyperlink.BuildGroupsUri());
            template.Parse("U_FOOT_NETWORKS", page.Core.Hyperlink.BuildNetworksUri());

            template.Parse("U_FOOT_MUSIC", page.Core.Hyperlink.BuildMusicUri());
            template.Parse("U_FOOT_MUSIC_DIRECTORY", page.Core.Hyperlink.BuildMusicDirectoryUri());
            template.Parse("U_FOOT_MUSIC_CHART", page.Core.Hyperlink.BuildMusicChartUri());

            if (session != null)
            {
                template.Parse("SID", session.SessionId);
                if (session.IsLoggedIn && session.LoggedInMember != null)
                {
                    template.Parse("LOGGED_IN", "TRUE");
                    template.Parse("USERNAME", session.LoggedInMember.UserName);
                    template.Parse("USER_ID", session.LoggedInMember.Id.ToString());
                    template.Parse("USER_DISPLAY_NAME", session.LoggedInMember.DisplayName);
                    template.Parse("USER_TILE", session.LoggedInMember.Tile);
                    template.Parse("USER_ICON", session.LoggedInMember.Icon);
                    template.Parse("U_USER_PROFILE", session.LoggedInMember.Uri);
                    template.Parse("U_ACCOUNT", core.Hyperlink.BuildAccountUri());

                    string formSubmitUri = core.Hyperlink.AppendSid(session.LoggedInMember.AccountUriStub, true);
                    template.Parse("S_ACCOUNT", formSubmitUri);

                    template.Parse("UNREAD_NOTIFICATIONS", session.LoggedInMember.UserInfo.UnreadNotifications);

                    if (session.LoggedInMember.UserInfo.UnseenMail > 0)
                    {
                        template.Parse("UNSEEN_MAIL", "TRUE");
                    }
                    template.Parse("U_UNSEEN_MAIL", core.Hyperlink.BuildAccountSubModuleUri("mail", "inbox"));
                }
                if (!core.Hyperlink.SidUrls)
                {
                    template.Parse("S_TRIM_SID", "TRUE");
                }
            }

            template.Parse("IS_CONTENT", "TRUE");

            if (WebConfigurationManager.AppSettings != null && WebConfigurationManager.AppSettings.HasKeys())
            {
                template.Parse("ANALYTICS_CODE", WebConfigurationManager.AppSettings["analytics-code"]);

                template.ParseRaw("ADSENSE_CODE_HEADER", WebConfigurationManager.AppSettings["adsense-code-header"]);
                template.ParseRaw("ADSENSE_CODE_FOOTER", WebConfigurationManager.AppSettings["adsense-code-footer"]);
            }

            foreach (string name in core.Meta.Keys)
            {
                VariableCollection metaVariableCollection = template.CreateChild("meta_list");

                metaVariableCollection.Parse("NAME", name);
                metaVariableCollection.ParseRaw("CONTENT", core.Meta[name]);
            }
        }

        public void ParsePageList(Primitive owner, bool fragment)
        {
            ParsePageList(core.Template, "PAGE_LIST", owner, fragment);
        }

        public void ParsePageList(Primitive owner, bool fragment, Page current)
        {
            ParsePageList(core.Template, "PAGE_LIST", owner, fragment, current);
        }

        public void ParsePageList(string templateVar, Primitive owner, bool fragment)
        {
            ParsePageList(core.Template, templateVar, owner, fragment);
        }

        public void ParsePageList(Template template, string templateVar, Primitive owner, bool fragment)
        {
            ParsePageList(template, templateVar, owner, fragment, null);
        }

        public void ParsePageList(Template template, string templateVar, Primitive owner, bool fragment, Page current)
        {
            //template.Parse("PAGE_LIST_HOW", Environment.StackTrace);
            template.ParseRaw(templateVar, GeneratePageList(owner, core.Session.LoggedInMember, fragment, current));
        }

        public string GeneratePageList(Primitive owner, User loggedInMember, bool fragment)
        {
            return GeneratePageList(owner, loggedInMember, fragment, null);
        }

        public string GeneratePageList(Primitive owner, User loggedInMember, bool fragment, Page current)
        {
            Database db = core.Db;

            //ushort readAccessLevel = owner.GetAccessLevel(loggedInMember);
            long loggedIdUid = User.GetMemberId(loggedInMember);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);
            query.AddCondition("page_status", "PUBLISH");
            QueryCondition qc1 = query.AddCondition("page_parent_id", 0);
            if (current != null)
            {
                ParentTree pt = current.GetParents();
                if (pt != null)
                {
                    foreach (ParentTreeNode ptn in pt.Nodes)
                    {
                        qc1.AddCondition(ConditionRelations.Or, "page_parent_id", ptn.ParentId);
                    }
                }

                qc1.AddCondition(ConditionRelations.Or, "page_parent_id", current.Id);
            }
            query.AddSort(SortOrder.Ascending, "page_order");

            DataTable pagesTable = db.Query(query);
            
            StringBuilder output = new StringBuilder();

            if (!fragment)
            {
                if (pagesTable.Rows.Count == 0)
                {
                    return string.Empty;
                }
                else
                {
                    output.Append("<ul>\n");
                }
            }

            int parents = 0;
            int nextParents = 0;

            List<IPermissibleItem> tempPages = new List<IPermissibleItem>();
            List<Page> pages = new List<Page>();

            for (int i = 0; i < pagesTable.Rows.Count; i++)
            {
                tempPages.Add(new Page(core, owner, pagesTable.Rows[i]));
            }

            core.AcessControlCache.CacheGrants(tempPages);

            foreach (IPermissibleItem page in tempPages)
            {
                if (page.Access.Can("VIEW"))
                {
                    pages.Add((Page)page);
                }
            }

            for (int i = 0; i < pages.Count; i++)
            {
                bool hasChildren = false;
                if (i + 1 < pages.Count)
                {
                    if (pages[i + 1].ParentId == 0)
                    {
                        nextParents = 0;
                    }
                    else
                    {
                        nextParents = pages[i + 1].ParentPath.Split('/').Length;
                    }
                }
                else
                {
                    nextParents = 0;
                }

                if (pages[i].ParentId == 0)
                {
                    parents = 0;
                }
                else
                {
                    parents = pages[i].ParentPath.Split('/').Length;
                }

                if (nextParents > parents)
                {
                    hasChildren = true;
                }

                if (core.IsMobile)
                {
                    output.Append("<li class=\"page-li\">");
                }
                else
                {
                    if (!string.IsNullOrEmpty(pages[i].Icon))
                    {
                        output.Append("<li style=\"background-image: url('" + HttpUtility.HtmlEncode(pages[i].Icon) + "');\" class=\"page-li\"> ");
                    }
                    else
                    {
                        output.Append("<li class=\"page-li\">");
                    }
                }
                output.Append("<a href=\"");
                output.Append(HttpUtility.HtmlEncode(pages[i].Uri));
                output.Append("\">");
                if (core.IsMobile && (!string.IsNullOrEmpty(pages[i].Icon)))
                {
                    output.Append("<img src=\"" + HttpUtility.HtmlEncode(pages[i].Icon) + "\" class=\"ui-li-icon\" />");
                }
                output.Append("<span>");
                if (current != null)
                {
                    if (pages[i].Id == current.Id)
                    {
                        output.Append("<b>");
                    }
                    output.Append(HttpUtility.HtmlEncode(pages[i].Title));
                    if (pages[i].Id == current.Id)
                    {
                        output.Append("</b>");
                    }
                }
                else
                {
                    output.Append(HttpUtility.HtmlEncode(pages[i].Title));
                }
                output.Append("</span></a>");

                if ((!hasChildren) || core.IsMobile)
                {
                    output.Append("</li>\n");
                }
                else
                {
                    for (int j = parents; j < nextParents; j++)
                    {
                        if (j > parents)
                        {
                            output.Append("<li class=\"empty\">");
                        }
                        output.Append("<ul>\n");
                    }

                    if (parents == nextParents)
                    {
                        output.Append("\n<ul>\n");
                    }
                }

                if (!core.IsMobile)
                {
                    for (int j = nextParents; j < parents; j++)
                    {
                        output.Append("</ul>\n</li>\n");
                    }
                }
            }

            if (!fragment)
            {
                output.Append("</ul>\n");
            }

            return output.ToString();
        }

        public void ParseBbcodeCache(string templateVar, string input)
        {
            core.Template.ParseRaw(templateVar, input);
        }

        public void ParseBbcodeCache(VariableCollection template, string templateVar, string input)
        {
            template.ParseRaw(templateVar, input);
        }

        public void ParseBbcode(string templateVar, string input)
        {
            ParseBbcode(templateVar, input, null);
        }

        public void ParseBbcode(string templateVar, string input, Primitive owner)
        {
            ParseBbcode(core.Template, templateVar, input, owner);
        }

        public void ParseBbcode(Template template, string templateVar, string input)
        {
            ParseBbcode(template, templateVar, input, null);
        }

        public void ParseBbcode(Template template, string templateVar, string input, Primitive owner)
        {
            ParseBbcode(template, templateVar, input, owner, false, string.Empty, string.Empty);
        }

        public void ParseBbcode(Template template, string templateVar, string input, Primitive owner, bool appendP, string id, string styleClass)
        {
            ParseBbcode(template, templateVar, input, owner, appendP, id, styleClass, false);
        }

        public void ParseBbcode(Template template, string templateVar, string input, Primitive owner, bool appendP, string id, string styleClass, bool fullInternalUrls)
        {
            if (owner != null)
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember, owner, appendP, id, styleClass, fullInternalUrls));
            }
            else
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember, appendP, id, styleClass, fullInternalUrls));
            }
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input)
        {
            ParseBbcode(template, templateVar, input, null);
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input, bool appendP, string id, string styleClass)
        {
            ParseBbcode(template, templateVar, input, null, appendP, id, styleClass);
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input, Primitive owner)
        {
            ParseBbcode(template, templateVar, input, owner, false, string.Empty, string.Empty);
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input, Primitive owner, bool appendP, string id, string styleClass)
        {
            ParseBbcode(template, templateVar, input, owner, false, string.Empty, string.Empty, false);
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input, Primitive owner, bool appendP, string id, string styleClass, bool fullInternalUrls)
        {
            if (core.Session.LoggedInMember == null)
            {

                if (owner != null)
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), null, owner, appendP, id, styleClass, fullInternalUrls));
                }
                else
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), appendP, id, styleClass, fullInternalUrls));
                }
            }
            else
            {
                if (owner != null)
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember, owner, appendP, id, styleClass, fullInternalUrls));
                }
                else
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.Session.LoggedInMember, appendP, id, styleClass, fullInternalUrls));
                }
            }
        }

        public void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            ParseRadioArray(core.Template, templateVar, name, columns, items, selectedItem);
        }

        public void ParseRadioArray(Template template, string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem));
        }

        public void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            ParseRadioArray(core.Template, templateVar, name, columns, items, selectedItem, disabledItems);
        }

        public void ParseRadioArray(Template template, string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem, disabledItems));
        }

        public void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem)
        {
            ParseSelectBox(core.Template, templateVar, name, items, selectedItem);
        }

        public void ParseSelectBox(Template template, string templateVar, string name, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem));
        }

        public void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            ParseSelectBox(core.Template, templateVar, name, items, selectedItem, disabledItems);
        }

        public void ParseSelectBox(Template template, string templateVar, string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem, disabledItems));
        }

        public void ParseLicensingBox(string templateVar, byte selectedLicense)
        {
            ParseLicensingBox(core.Template, templateVar, selectedLicense);
        }

        public void ParseLicensingBox(Template template, string templateVar, byte selectedLicense)
        {
            template.ParseRaw(templateVar, ContentLicense.BuildLicenseSelectBox(core.Db, selectedLicense));
        }

        public void ParseClassification(string templateVar, Classifications classification)
        {
            ParseClassification(core.Template, templateVar, classification);
        }

        public void ParseClassification(Template template, string templateVar, Classifications classification)
        {
            template.ParseRaw(templateVar, Classification.BuildClassificationBox(core, classification));
        }

        /*public void ParseTimeZoneBox(string templateVar, string timeZone)
        {
            ParseTimeZoneBox(core.Template, templateVar, timeZone);
        }*/

        /*public void ParseTimeZoneBox(Template template, string templateVar, string timeZone)
        {
            template.ParseRaw(templateVar, UnixTime.BuildTimeZoneSelectBox(timeZone));
        }*/

        /// <summary>
        /// Algorithm from http://en.wikipedia.org/wiki/HSL_and_HSV
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static Color HlsToRgb(double h, double s, double l)
        {
            h = (h) % 360.0;

            double q = 0;
            if (l < 1.0 / 2)
            {
                q = l * (1 + s);
            }
            else
            {
                q = l + s - (l * s);
            }
            double p = 2.0 * l - q;
            double hk = h / 360.0;
            double tR = hk + 1.0 / 3;
            double tG = hk;
            double tB = hk - 1.0 / 3;

            double[] C = { tR, tG, tB };

            for (int i = 0; i < C.Length; i++)
            {
                double tC  = C[i];

                if (tC < 0.0)
                {
                    tC = tC + 1.0;
                }
                else if (tC > 1.0)
                {
                    tC = tC - 1.0;
                }

                double c;
                if (tC < 1.0 / 6)
                {
                    c = p + ((q - p) * 6 * tC);
                }
                else if (1.0 / 6 <= tC && tC < 1.0 / 2)
                {
                    c = q;
                }
                else if (1.0 / 2 <= tC && tC < 2.0 / 3)
                {
                    c = p + ((q - p) * 6 * (2.0 / 3 - tC));
                }
                else
                {
                    c = p;
                }
                C[i] = c;
            }
            
            return Color.FromArgb((int)(C[0] * 255) % 256, (int)(C[1] * 255) % 256, (int)(C[2] * 255) % 256);
        }

        public static string SqlEscape(string input)
        {
            return input.Replace("'", "''");
        }
    }

    public class DisplayCommentHookEventArgs : EventArgs
    {
        private Core core;
        private VariableCollection commentVariableCollection;
        private Primitive owner;
        private User poster;

        public DisplayCommentHookEventArgs(Core core, Primitive owner, User poster, VariableCollection variableCollection)
        {
            this.core = core;
            this.owner = owner;
            this.poster = poster;
            this.commentVariableCollection = variableCollection;
        }

        public Core Core
        {
            get
            {
                return core;
            }
        }

        public Primitive Owner
        {
            get
            {
                return owner;
            }
        }

        public User Poster
        {
            get
            {
                return poster;
            }
        }

        public VariableCollection CommentVariableCollection
        {
            get
            {
                return commentVariableCollection;
            }
        }
    }
}
