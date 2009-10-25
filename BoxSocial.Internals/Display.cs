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

    public class Display
    {
        internal TPage page;

        const string RANK_ACTIVE = "/images/star-on.png";
        const string RANK_RATING = "/images/star-on.png";
        const string RANK_SHADOW = "/images/star-off.png";

        private Core core;

        public Display(Core core)
        {
            this.core = core;
        }

        public delegate void DisplayCommentHookHandler(DisplayCommentHookEventArgs e);

        public void ParsePagination(string baseUri, int currentPage, int maxPages)
        {
            ParsePagination(core.template, "PAGINATION", baseUri, currentPage, maxPages);
        }

        public void ParsePagination(string baseUri, int currentPage, int maxPages, bool isBlog)
        {
            ParsePagination(core.template, "PAGINATION", baseUri, currentPage, maxPages, isBlog);
        }

        public void ParsePagination(string templateVar, string baseUri, int currentPage, int maxPages)
        {
            ParsePagination(core.template, templateVar, baseUri, currentPage, maxPages);
        }

        public void ParsePagination(string templateVar, string baseUri, int currentPage, int maxPages, bool isBlog)
        {
            ParsePagination(core.template, templateVar, baseUri, currentPage, maxPages, isBlog);
        }

        public void ParsePagination(Template template, string templateVar, string baseUri, int currentPage, int maxPages)
        {
            template.ParseRaw(templateVar, GeneratePagination(baseUri, currentPage, maxPages));
        }

        public void ParsePagination(Template template, string templateVar, string baseUri, int currentPage, int maxPages, bool isBlog)
        {
            template.ParseRaw(templateVar, GeneratePagination(baseUri, currentPage, maxPages, isBlog));
        }

        public string GeneratePagination(string baseUri, int currentPage, int maxPages)
        {
            return GeneratePagination(baseUri, currentPage, maxPages, false);
        }

        /// <summary>
        /// Base uri is html encoded by this function, do not pre-process
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="currentPage"></param>
        /// <param name="maxPages"></param>
        /// <returns></returns>
        public string GeneratePagination(string baseUri, int currentPage, int maxPages, bool isBlog)
        {
            StringBuilder pagination = new StringBuilder();
            bool comma = false;
			bool skipped = false;
            bool baseAmp = baseUri.Contains("?");

            if (isBlog)
            {
                if (currentPage > 2)
                {
                    if (baseAmp)
                    {
                        pagination.Append(string.Format("<a style=\"float: right; display: block;\" href=\"{0}p={1}\">Next Entries &raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri + "&"), currentPage - 1));
                    }
                    else
                    {
                        pagination.Append(string.Format("<a style=\"float: right; display: block;\" href=\"{0}?p={1}\">Next Entries &raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri), currentPage - 1));
                    }
                }
                else if (currentPage > 1)
                {
                    pagination.Append(string.Format("<a style=\"float: right; display: block;\" href=\"{0}\">Next Entries &raquo;</a>",
                        HttpUtility.HtmlEncode(baseUri)));
                }
                pagination.Append("&nbsp;");
                if (currentPage < maxPages)
                {
                    if (baseAmp)
                    {
                        pagination.Append(string.Format("<a href=\"{0}p={1}\">&laquo; Previous Entries</a>",
                            HttpUtility.HtmlEncode(baseUri + "&"), currentPage + 1));
                    }
                    else
                    {
                        pagination.Append(string.Format("<a href=\"{0}?p={1}\">&laquo; Previous Entries</a>",
                            HttpUtility.HtmlEncode(baseUri), currentPage + 1));
                    }
                }
            }
            else
            {
                if (currentPage > 2)
                {
                    if (baseAmp)
                    {
                        pagination.Append(string.Format("<a href=\"{0}p={1}\">&laquo; Prev</a>",
                            HttpUtility.HtmlEncode(baseUri + "&"), currentPage - 1));
                    }
                    else
                    {
                        pagination.Append(string.Format("<a href=\"{0}?p={1}\">&laquo; Prev</a>",
                            HttpUtility.HtmlEncode(baseUri), currentPage - 1));
                    }
                    comma = true;
                }
                else if (currentPage > 1)
                {
                    pagination.Append(string.Format("<a href=\"{0}\">&laquo; Prev</a>",
                        HttpUtility.HtmlEncode(baseUri)));
                    comma = true;
                }

                for (int i = 1; i <= maxPages; i++)
                {
                    if (i != currentPage)
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
							if ((i > 3 && i < currentPage - 2) || (i < maxPages - 2 && i > currentPage + 2))
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
							
                            if (baseAmp)
                            {
                                pagination.Append(string.Format("<a href=\"{0}p={1}\">{1}</a>",
                                    HttpUtility.HtmlEncode(baseUri + "&"), i));
                            }
                            else
                            {
                                pagination.Append(string.Format("<a href=\"{0}?p={1}\">{1}</a>",
                                    HttpUtility.HtmlEncode(baseUri), i));
                            }
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

                if (currentPage < maxPages)
                {
                    pagination.Append(", ");
                    if (baseAmp)
                    {
                        pagination.Append(string.Format("<a href=\"{0}p={1}\">Next &raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri + "&"), currentPage + 1));
                    }
                    else
                    {
                        pagination.Append(string.Format("<a href=\"{0}?p={1}\">Next &raquo;</a>",
                            HttpUtility.HtmlEncode(baseUri), currentPage + 1));
                    }
                }
            }

            return pagination.ToString();
        }

        public void ShowMessage(string title, string message)
        {
            ShowMessage(title, message, ShowMessageOptions.Unprocessed);
        }

        public void ShowMessage(string title, string message, ShowMessageOptions options)
        {
            core.template.SetTemplate("std.message.html");
            core.template.Parse("MESSAGE_TITLE", title);
            switch (options)
            {
                case ShowMessageOptions.Unprocessed:
                    core.template.Parse("MESSAGE_BODY", message);
                    break;
                case ShowMessageOptions.Bbcode:
                    core.Display.ParseBbcode("MESSAGE_BODY", message);
                    break;
                case ShowMessageOptions.Stripped:
                    // TODO: stripped bbcode parse
                    core.template.Parse("MESSAGE_BODY", core.Bbcode.Strip(message));
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

            page.template.SetTemplate("std.confirm.html");

            page.template.Parse("S_FORM_ACTION", formAction);
            page.template.Parse("CONFIRM_TITLE", title);
            page.template.Parse("CONFIRM_TEXT", message);

            foreach (string key in hiddenFieldList.Keys)
            {
                VariableCollection hiddenVariableCollection = page.template.CreateChild("hidden_list");

                hiddenVariableCollection.Parse("NAME", key);
                hiddenVariableCollection.Parse("VALUE", hiddenFieldList[key]);
            }

            page.EndResponse();

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
            DisplayComments(template, owner, item, null, -1, null);
        }

        public void DisplayComments(Template template, Primitive owner, ICommentableItem item, List<User> commenters, long commentCount, DisplayCommentHookHandler hook)
        {
            Mysql db = core.db;

            int p = core.Functions.RequestInt("p", 1);
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
                    p = (int)(before / item.CommentsPerPage + 1);
                }
                else
                {
                    p = (int)(after / item.CommentsPerPage + 1);
                }
            }

            List<Comment> comments = Comment.GetComments(core, item.ItemKey, item.CommentSortOrder, p, item.CommentsPerPage, commenters);
            Comment.LoadUserInfoCache(core, comments);

            if (commentCount >= 0)
            {
                template.Parse("COMMENTS", commentCount.ToString());
            }
            else if (item.Comments >= 0)
            {
                template.Parse("COMMENTS", item.Comments.ToString());
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
                VariableCollection commentsVariableCollection = template.CreateChild("comment-list");

                //commentsVariableCollection.ParseRaw("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment.Body), core.session.LoggedInMember));
                core.Display.ParseBbcode(commentsVariableCollection, "COMMENT", comment.Body);

                try
                {
                    User commentPoster = core.PrimitiveCache[comment.UserId];

                    commentsVariableCollection.Parse("ID", comment.CommentId.ToString());
                    commentsVariableCollection.Parse("USERNAME", commentPoster.DisplayName);
                    commentsVariableCollection.Parse("U_PROFILE", commentPoster.ProfileUri);
                    commentsVariableCollection.Parse("U_QUOTE", core.Uri.BuildCommentQuoteUri(comment.CommentId));
                    commentsVariableCollection.Parse("U_REPORT", core.Uri.BuildCommentReportUri(comment.CommentId));
                    commentsVariableCollection.Parse("U_DELETE", core.Uri.BuildCommentDeleteUri(comment.CommentId));
                    commentsVariableCollection.Parse("TIME", core.tz.DateTimeToString(comment.GetTime(core.tz)));
                    commentsVariableCollection.Parse("USER_TILE", commentPoster.UserTile);

                    if (hook != null)
                    {
                        hook(new DisplayCommentHookEventArgs(core, owner, commentPoster, commentsVariableCollection));
                    }

                    if (owner.CanModerateComments(core.session.LoggedInMember))
                    {
                        commentsVariableCollection.Parse("MODERATE", "TRUE");
                    }

                    if (owner.IsCommentOwner(commentPoster))
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
                }
            }
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
            Dictionary<string, string> loopVars = new Dictionary<string, string>();

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

            loopVars.Add("U_RATE_ONE_STAR", string.Format("/rate.aspx?rating=1&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_TWO_STAR", string.Format("/rate.aspx?rating=2&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_THREE_STAR", string.Format("/rate.aspx?rating=3&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_FOUR_STAR", string.Format("/rate.aspx?rating=4&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));
            loopVars.Add("U_RATE_FIVE_STAR", string.Format("/rate.aspx?rating=5&amp;item={0}&amp;type={1}", itemKey.Id, itemKey.TypeId));

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
            Template template = page.template;
            SessionState session = page.session;

            template.Parse("TITLE", page.PageTitle); // the set page title function sanitises
            template.Parse("HEADING", WebConfigurationManager.AppSettings["boxsocial-title"]);
            template.Parse("SITE_TITLE", WebConfigurationManager.AppSettings["boxsocial-title"]);
            template.Parse("YEAR", DateTime.Now.Year.ToString());

            if (page.CanonicalUri != null)
            {
                template.Parse("CANONICAL_URI", page.CanonicalUri);
            }

            template.Parse("HEAD_COLOUR", "ffffff");
            template.Parse("HEAD_FORE_COLOUR", "black");

            /*
             * URIs
             */
            template.Parse("U_HOME", page.Core.Uri.BuildHomeUri());
            template.Parse("U_ABOUT", page.Core.Uri.BuildAboutUri());
            template.Parse("U_SAFETY", page.Core.Uri.BuildSafetyUri());
            template.Parse("U_PRIVACY", page.Core.Uri.BuildPrivacyUri());
            template.Parse("U_TOS", page.Core.Uri.BuildTermsOfServiceUri());
            template.Parse("U_SIGNIN", page.Core.Uri.BuildLoginUri());
            template.Parse("U_SIGNOUT", page.Core.Uri.BuildLogoutUri());
            template.Parse("U_REGISTER", page.Core.Uri.BuildRegisterUri());
            template.Parse("U_HELP", page.Core.Uri.BuildHelpUri());
            template.Parse("U_SITEMAP", page.Core.Uri.BuildSitemapUri());
            template.Parse("U_COPYRIGHT", page.Core.Uri.BuildCopyrightUri());
            template.Parse("U_SEARCH", page.Core.Uri.BuildSearchUri());
            template.Parse("S_SEARCH", page.Core.Uri.BuildSearchUri());

            if (session != null)
            {
                template.Parse("SID", session.SessionId);
                if (session.IsLoggedIn && session.LoggedInMember != null)
                {
                    template.Parse("LOGGED_IN", "TRUE");
                    template.Parse("L_GREETING", "G'day");
                    template.Parse("USERNAME", session.LoggedInMember.UserName);
                    template.Parse("U_USER_PROFILE", session.LoggedInMember.Uri);
                    template.Parse("U_ACCOUNT", core.Uri.BuildAccountUri());
                }
            }
        }

        public void ParsePageList(Primitive owner, bool fragment)
        {
            ParsePageList(core.template, "PAGE_LIST", owner, fragment);
        }

        public void ParsePageList(Primitive owner, bool fragment, Page current)
        {
            ParsePageList(core.template, "PAGE_LIST", owner, fragment, current);
        }

        public void ParsePageList(string templateVar, Primitive owner, bool fragment)
        {
            ParsePageList(core.template, templateVar, owner, fragment);
        }

        public void ParsePageList(Template template, string templateVar, Primitive owner, bool fragment)
        {
            ParsePageList(template, templateVar, owner, fragment, null);
        }

        public void ParsePageList(Template template, string templateVar, Primitive owner, bool fragment, Page current)
        {
            template.ParseRaw(templateVar, GeneratePageList(owner, core.session.LoggedInMember, fragment, current));
        }

        public string GeneratePageList(Primitive owner, User loggedInMember, bool fragment)
        {
            return GeneratePageList(owner, loggedInMember, fragment, null);
        }

        public string GeneratePageList(Primitive owner, User loggedInMember, bool fragment, Page current)
        {
            Database db = core.db;

            ushort readAccessLevel = owner.GetAccessLevel(loggedInMember);
            long loggedIdUid = User.GetMemberId(loggedInMember);

            SelectQuery query = Page.GetSelectQueryStub(typeof(Page));
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);
            query.AddCondition("page_status", "PUBLISH");
            //QueryCondition qc1 = query.AddCondition(new QueryOperation("page_access", QueryOperations.BinaryAnd, readAccessLevel).ToString(), ConditionEquality.NotEqual, 0);
            //qc1.AddCondition(ConditionRelations.Or, "user_id", loggedIdUid);
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
                    return "";
                }
                else
                {
                    output.Append("<ul>\n");
                }
            }

            int parents = 0;
            int nextParents = 0;

            List<Page> pages = new List<Page>();

            for (int i = 0; i < pagesTable.Rows.Count; i++)
            {
                Page page = new Page(core, owner, pagesTable.Rows[i]);

                if (page.Access.Can("VIEW"))
                {
                    pages.Add(page);
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

                if (!string.IsNullOrEmpty(pages[i].Icon))
                {
                    output.Append("<li style=\"list-style-image: url('" + HttpUtility.HtmlEncode(pages[i].Icon) + "');\"> ");
                }
                else
                {
                    output.Append("<li>");
                }
                output.Append("<a href=\"");
                output.Append(HttpUtility.HtmlEncode(pages[i].Uri));
                output.Append("\">");
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
                output.Append("</a>");

                if (!hasChildren)
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

                for (int j = nextParents; j < parents; j++)
                {
                    output.Append("</ul>\n</li>\n");
                }
            }

            if (!fragment)
            {
                output.Append("</ul>\n");
            }

            return output.ToString();
        }

        public void ParseBbcode(string templateVar, string input)
        {
            ParseBbcode(templateVar, input, null);
        }

        public void ParseBbcode(string templateVar, string input, User owner)
        {
            ParseBbcode(core.template, templateVar, input, owner);
        }

        public void ParseBbcode(Template template, string templateVar, string input)
        {
            ParseBbcode(template, templateVar, input, null);
        }

        public void ParseBbcode(Template template, string templateVar, string input, User owner)
        {
            if (owner != null)
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember, owner));
            }
            else
            {
                template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember));
            }
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input)
        {
            ParseBbcode(template, templateVar, input, null);
        }

        public void ParseBbcode(VariableCollection template, string templateVar, string input, User owner)
        {
            if (core.session.LoggedInMember == null)
            {

                if (owner != null)
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), null, owner));
                }
                else
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input)));
                }
            }
            else
            {
                if (owner != null)
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember, owner));
                }
                else
                {
                    template.ParseRaw(templateVar, core.Bbcode.Parse(HttpUtility.HtmlEncode(input), core.session.LoggedInMember));
                }
            }
        }

        /*public void ParsePermissionsBox(string templateVar, ushort permission, List<string> permissions)
        {
            ParsePermissionsBox(core.template, templateVar, permission, permissions);
        }

        public void ParsePermissionsBox(Template template, string templateVar, ushort permission, List<string> permissions)
        {
            template.ParseRaw(templateVar, Functions.BuildPermissionsBox(permission, permissions));
        }*/

        public void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            ParseRadioArray(core.template, templateVar, name, columns, items, selectedItem);
        }

        public void ParseRadioArray(Template template, string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem));
        }

        public void ParseRadioArray(string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            ParseRadioArray(core.template, templateVar, name, columns, items, selectedItem, disabledItems);
        }

        public void ParseRadioArray(Template template, string templateVar, string name, int columns, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildRadioArray(name, columns, items, selectedItem, disabledItems));
        }

        public void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem)
        {
            ParseSelectBox(core.template, templateVar, name, items, selectedItem);
        }

        public void ParseSelectBox(Template template, string templateVar, string name, List<SelectBoxItem> items, string selectedItem)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem));
        }

        public void ParseSelectBox(string templateVar, string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            ParseSelectBox(core.template, templateVar, name, items, selectedItem, disabledItems);
        }

        public void ParseSelectBox(Template template, string templateVar, string name, List<SelectBoxItem> items, string selectedItem, List<string> disabledItems)
        {
            template.ParseRaw(templateVar, Functions.BuildSelectBox(name, items, selectedItem, disabledItems));
        }

        public void ParseLicensingBox(string templateVar, byte selectedLicense)
        {
            ParseLicensingBox(core.template, templateVar, selectedLicense);
        }

        public void ParseLicensingBox(Template template, string templateVar, byte selectedLicense)
        {
            template.ParseRaw(templateVar, ContentLicense.BuildLicenseSelectBox(core.db, selectedLicense));
        }

        public void ParseClassification(string templateVar, Classifications classification)
        {
            ParseClassification(core.template, templateVar, classification);
        }

        public void ParseClassification(Template template, string templateVar, Classifications classification)
        {
            template.ParseRaw(templateVar, Classification.BuildClassificationBox(core, classification));
        }

        public void ParseTimeZoneBox(string templateVar, string timeZone)
        {
            ParseTimeZoneBox(core.template, templateVar, timeZone);
        }

        public void ParseTimeZoneBox(Template template, string templateVar, string timeZone)
        {
            template.ParseRaw(templateVar, UnixTime.BuildTimeZoneSelectBox(timeZone));
        }

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
