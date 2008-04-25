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
using System.Web.Security;
using System.Web.Services;
using System.Web.Services.Protocols;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ConfirmBoxResult
    {
        None,
        Yes,
        No,
    }

    public static class Display
    {
        internal static TPage page;

        const string RANK_ACTIVE = "/images/star-on.png";
        const string RANK_RATING = "/images/star-on.png";
        const string RANK_SHADOW = "/images/star-off.png";

        private static Core core;

        public static Core Core
        {
            set
            {
                core = value;
            }
        }

        public static string GeneratePagination(string baseUri, int currentPage, int maxPages)
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
        public static string GeneratePagination(string baseUri, int currentPage, int maxPages, bool isBlog)
        {
            StringBuilder pagination = new StringBuilder();
            bool comma = false;
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
                    if (comma)
                    {
                        pagination.Append(", ");
                    }
                    else
                    {
                        comma = true;
                    }
                    if (i != currentPage)
                    {
                        if (i == 1)
                        {
                            pagination.Append(string.Format("<a href=\"{0}\">{1}</a>",
                                HttpUtility.HtmlEncode(baseUri), i));
                        }
                        else
                        {
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
                    else
                    {
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

        public static void ShowMessage(string title, string message)
        {
            core.template.SetTemplate("std.message.html");
            core.template.ParseVariables("MESSAGE_TITLE", title);
            core.template.ParseVariables("MESSAGE_BODY", message);

            /* Something wrong here, .net likes to end the page TWICE! */
            /* Fixed by adding a flag to EndResponse to only execute once. */
            core.EndResponse();
        }

        public static ConfirmBoxResult GetConfirmBoxResult()
        {
            if (HttpContext.Current.Request.Form["1"] != null)
            {
                return ConfirmBoxResult.Yes;
            }
            else if (HttpContext.Current.Request.Form["0"] != null)
            {
                return ConfirmBoxResult.No;
            }
            else
            {
                return ConfirmBoxResult.None;
            }
        }

        public static ConfirmBoxResult ShowConfirmBox(string formAction, string title, string message, Dictionary<string, string> hiddenFieldList)
        {
            if (HttpContext.Current.Request.Form["1"] != null)
            {
                return ConfirmBoxResult.Yes;
            }
            else if (HttpContext.Current.Request.Form["0"] != null)
            {
                return ConfirmBoxResult.No;
            }

            page.template.SetTemplate("std.confirm.html");

            page.template.ParseVariables("S_FORM_ACTION", formAction);
            page.template.ParseVariables("CONFIRM_TITLE", title);
            page.template.ParseVariables("CONFIRM_TEXT", message);

            foreach (string key in hiddenFieldList.Keys)
            {
                VariableCollection hiddenVariableCollection = page.template.CreateChild("hidden_list");

                hiddenVariableCollection.ParseVariables("NAME", key);
                hiddenVariableCollection.ParseVariables("VALUE", hiddenFieldList[key]);
            }

            page.EndResponse();

            return ConfirmBoxResult.None;
        }

        public static void DisplayComments(Template template, Member profileOwner, ICommentableItem item)
        {
            DisplayComments(template, (Primitive)profileOwner, item);
        }

        public static void DisplayComments(Template template, Primitive owner, ICommentableItem item)
        {
            DisplayComments(template, owner, item, null);
        }

        public static void DisplayComments(Template template, Primitive owner, ICommentableItem item, List<Member> commenters)
        {
            Mysql db = core.db;

            int p = Functions.RequestInt("p", 1);
            long c = Functions.RequestLong("c", 0);

            if (c > 0)
            {
                SelectQuery query = new SelectQuery("comments");
                query.AddFields("COUNT(*) AS total");
                query.AddCondition("comment_item_id", item.Id);
                query.AddCondition("comment_item_type", item.Namespace);
                query.AddCondition("comment_id", ConditionEquality.LessThanEqual, c);

                if (commenters != null)
                {
                    if (commenters.Count > 0)
                    {
                        List<long> commentersIds = new List<long>();

                        foreach (Member commenter in commenters)
                        {
                            commentersIds.Add(commenter.Id);
                        }

                        query.AddCondition("user_id", ConditionEquality.In, commentersIds);
                    }
                }

                query.AddSort(SortOrder.Ascending, "comment_time_ut");

                DataRow commentsRow = db.SelectQuery(query).Rows[0];

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

            List<Comment> comments = Comment.GetComments(db, item.Namespace, item.Id, item.CommentSortOrder, p, item.CommentsPerPage);
            Comment.LoadUserInfoCache(core, comments);

            if (item.Comments >= 0)
            {
                template.ParseVariables("COMMENTS", item.Comments.ToString());
            }
            else
            {
                template.ParseVariables("COMMENTS", comments.Count.ToString());
            }
            template.ParseVariables("ITEM_ID", item.Id.ToString());
            template.ParseVariables("ITEM_TYPE", item.Namespace);

            if (item.CommentSortOrder == SortOrder.Ascending)
            {
                template.ParseVariables("COMMENTS_ASC", "TRUE");
                template.ParseVariables("COMMENT_SORT", "asc");
            }
            else
            {
                template.ParseVariables("COMMENTS_DESC", "TRUE");
                template.ParseVariables("COMMENT_SORT", "desc");
            }

            foreach (Comment comment in comments)
            {
                VariableCollection commentsVariableCollection = template.CreateChild("comment-list");

                commentsVariableCollection.ParseVariables("COMMENT", Bbcode.Parse(HttpUtility.HtmlEncode(comment.Body), core.session.LoggedInMember));

                try
                {
                    Member commentPoster = core.UserProfiles[comment.UserId];

                    commentsVariableCollection.ParseVariables("ID", comment.CommentId.ToString());
                    commentsVariableCollection.ParseVariables("USERNAME", commentPoster.DisplayName);
                    commentsVariableCollection.ParseVariables("U_PROFILE", commentPoster.ProfileUri);
                    commentsVariableCollection.ParseVariables("U_QUOTE", HttpUtility.HtmlEncode(Linker.BuildCommentQuoteUri(comment.CommentId)));
                    commentsVariableCollection.ParseVariables("U_REPORT", HttpUtility.HtmlEncode(Linker.BuildCommentReportUri(comment.CommentId)));
                    commentsVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildCommentDeleteUri(comment.CommentId)));
                    commentsVariableCollection.ParseVariables("TIME", core.tz.DateTimeToString(comment.GetTime(core.tz)));
                    commentsVariableCollection.ParseVariables("USER_TILE", HttpUtility.HtmlEncode(commentPoster.UserTile));

                    if (owner.CanModerateComments(core.session.LoggedInMember))
                    {
                        commentsVariableCollection.ParseVariables("MODERATE", "TRUE");
                    }

                    if (owner.IsCommentOwner(commentPoster))
                    {
                        commentsVariableCollection.ParseVariables("OWNER", "TRUE");
                        commentsVariableCollection.ParseVariables("NORMAL", "FALSE");
                    }
                    else
                    {
                        commentsVariableCollection.ParseVariables("OWNER", "FALSE");
                        commentsVariableCollection.ParseVariables("NORMAL", "TRUE");
                    }

                    if (comment.SpamScore >= 10)
                    {
                        commentsVariableCollection.ParseVariables("IS_SPAM", "TRUE");
                    }
                }
                catch
                {
                    // if userid is 0, anonymous
                }
            }
        }

        public static void RatingBlock(float existingRating, Template template, long itemId, string itemType)
        {
            RatingBlock(existingRating, null, template, itemId, itemType);
        }

        public static void RatingBlock(float existingRating, VariableCollection variables, long itemId, string itemType)
        {
            RatingBlock(existingRating, variables, null, itemId, itemType);
        }

        private static void RatingBlock(float existingRating, VariableCollection variables, Template template, long itemId, string itemType)
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

            loopVars.Add("U_RATE_ONE_STAR", string.Format("/rate.aspx?rating=1&amp;item={0}&amp;type={1}", itemId, itemType));
            loopVars.Add("U_RATE_TWO_STAR", string.Format("/rate.aspx?rating=2&amp;item={0}&amp;type={1}", itemId, itemType));
            loopVars.Add("U_RATE_THREE_STAR", string.Format("/rate.aspx?rating=3&amp;item={0}&amp;type={1}", itemId, itemType));
            loopVars.Add("U_RATE_FOUR_STAR", string.Format("/rate.aspx?rating=4&amp;item={0}&amp;type={1}", itemId, itemType));
            loopVars.Add("U_RATE_FIVE_STAR", string.Format("/rate.aspx?rating=5&amp;item={0}&amp;type={1}", itemId, itemType));

            loopVars.Add("RATE_RATING", string.Format("{0:0.0}", existingRating));
            loopVars.Add("RATE_ACTIVE", RANK_ACTIVE);
            loopVars.Add("RATE_TYPE", itemType);
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

        public static Member fillLoggedInMember(System.Security.Principal.IPrincipal User, Member loggedInMember)
        {
            if (loggedInMember == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return loggedInMember = new Member(core.db, User.Identity.Name, false);
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
        public static void Header(TPage page)
        {
            Template template = page.template;
            SessionState session = page.session;

            template.ParseVariables("TITLE", page.PageTitle); // the set page title function sanitises
            template.ParseVariables("HEADING", WebConfigurationManager.AppSettings["boxsocial-title"]);
            template.ParseVariables("SITE_TITLE", WebConfigurationManager.AppSettings["boxsocial-title"]);
            template.ParseVariables("YEAR", DateTime.Now.Year.ToString());

            /*string bgColour = "";

            if (page.tz == null)
            {
                page.tz = new UnixTime(UnixTime.UTC_CODE);
            }
            double hour = page.tz.Now.Hour + page.tz.Now.Minute / 60.0;

            if (hour > 12)
            {
                hour = 24 - hour;
            }
            float lum = (float)(Math.Sin(hour / 12.0 * Math.PI - Math.PI / 2) / 3.0 + 2 / 3.0);
            int newhue = (int)(page.tz.Now.DayOfYear * (360.0 / 366.0)) % 360;
            Color headColour = Display.HlsToRgb(newhue, 0.3F, lum);

            bgColour = string.Format("{0:x2}{1:x2}{2:x2}", headColour.R, headColour.G, headColour.B);

            template.ParseVariables("HEAD_COLOUR", bgColour);
            template.ParseVariables("HEAD_FORE_COLOUR", ((lum < 0.5) ? "white" : "black"));*/

            template.ParseVariables("HEAD_COLOUR", "ffffff");
            template.ParseVariables("HEAD_FORE_COLOUR", "black");

            /*
             * URIs
             */
            template.ParseVariables("U_HOME", HttpUtility.HtmlEncode(Linker.BuildHomeUri()));
            template.ParseVariables("U_ABOUT", HttpUtility.HtmlEncode(Linker.BuildAboutUri()));
            template.ParseVariables("U_SAFETY", HttpUtility.HtmlEncode(Linker.BuildSafetyUri()));
            template.ParseVariables("U_PRIVACY", HttpUtility.HtmlEncode(Linker.BuildPrivacyUri()));
            template.ParseVariables("U_TOS", HttpUtility.HtmlEncode(Linker.BuildTermsOfServiceUri()));
            template.ParseVariables("U_SIGNIN", HttpUtility.HtmlEncode(Linker.BuildLoginUri()));
            template.ParseVariables("U_SIGNOUT", HttpUtility.HtmlEncode(Linker.BuildLogoutUri()));
            template.ParseVariables("U_REGISTER", HttpUtility.HtmlEncode(Linker.BuildRegisterUri()));
            template.ParseVariables("U_HELP", HttpUtility.HtmlEncode(Linker.BuildHelpUri()));
            template.ParseVariables("U_SITEMAP", HttpUtility.HtmlEncode(Linker.BuildSitemapUri()));
            template.ParseVariables("U_COPYRIGHT", HttpUtility.HtmlEncode(Linker.BuildCopyrightUri()));
            template.ParseVariables("U_ACCOUNT", HttpUtility.HtmlEncode(Linker.BuildAccountUri()));

            if (session != null)
            {
                template.ParseVariables("SID", session.SessionId);
                if (session.IsLoggedIn)
                {
                    template.ParseVariables("LOGGED_IN", "TRUE");
                    template.ParseVariables("L_GREETING", HttpUtility.HtmlEncode("G'day"));
                    template.ParseVariables("USERNAME", HttpUtility.HtmlEncode(session.LoggedInMember.UserName));
                    template.ParseVariables("U_USER_PROFILE", HttpUtility.HtmlEncode(Linker.BuildHomepageUri(session.LoggedInMember)));
                }
            }
        }

        public static string GeneratePageList(Member owner, Member loggedInMember, bool fragment)
        {
            Mysql db = core.db;

            ushort readAccessLevel = owner.GetAccessLevel(loggedInMember);
            long loggedIdUid = Member.GetMemberId(loggedInMember);

            DataTable pagesTable = db.SelectQuery(string.Format("SELECT upg.page_parent_path, upg.page_slug, upg.page_title, upg.page_icon FROM user_pages upg WHERE upg.user_id = {0} AND upg.page_status = 'PUBLISH' AND (page_access & {1:0} = {1:0} OR user_id = {2}) ORDER BY upg.page_order",
                owner.UserId, readAccessLevel, loggedIdUid));
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

            for (int i = 0; i < pagesTable.Rows.Count; i++)
            {
                bool hasChildren = false;
                if (i + 1 < pagesTable.Rows.Count)
                {
                    if ((string)pagesTable.Rows[i + 1]["page_parent_path"] == "")
                    {
                        nextParents = 0;
                    }
                    else
                    {
                        nextParents = ((string)pagesTable.Rows[i + 1]["page_parent_path"]).Split('/').Length;
                    }
                }
                else
                {
                    nextParents = 0;
                }

                if ((string)pagesTable.Rows[i]["page_parent_path"] == "")
                {
                    parents = 0;
                }
                else
                {
                    parents = ((string)pagesTable.Rows[i]["page_parent_path"]).Split('/').Length;
                }

                if (nextParents > parents)
                {
                    hasChildren = true;
                }

                if ((string)pagesTable.Rows[i]["page_icon"] != "")
                {
                    output.Append("<li style=\"list-style-image: url('" + (string)pagesTable.Rows[i]["page_icon"] + "');\"> ");
                }
                else
                {
                    output.Append("<li>");
                }
                output.Append("<a href=\"");
                output.Append("/" + owner.UserName);
                if ((string)pagesTable.Rows[i]["page_parent_path"] != "")
                {
                    output.Append("/" + (string)pagesTable.Rows[i]["page_parent_path"]);
                }
                output.Append("/" + (string)pagesTable.Rows[i]["page_slug"]);
                output.Append("\">");
                output.Append((string)pagesTable.Rows[i]["page_title"]);
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

        /// <summary>
        /// http://www.vbaccelerator.com/home/VB/Code/vbMedia/Colour_Models/Hue__Luminance_and_Saturation/article.asp
        /// Automatically converted to c#
        /// <para>This seems to have bugs deeply rooted in it</para>
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static Color HlsToRgb(float h, float s, float l)
        {
            if (h < 360.0)
            {
                h += 360F;
            }
            if (h > 360.0)
            {
                h -= 360F;
            }
            h = h / 60F; // convert an angle to a number between 0 and 6
            float rR;
            float rG;
            float rB;
            float Min;
            float Max;
            if (s == 0)
            {
                rR = l;
                rG = l;
                rB = l;
            }
            else
            {
                if (l <= 0.5)
                {
                    Min = l * (1 - s);
                }
                else
                {
                    Min = l - s * (1 - l);
                }
                Max = 2 * l - Min;
                if ((h < 1))
                {
                    rR = Max;
                    if ((h < 0))
                    {
                        rG = Min;
                        rB = rG - h * (Max - Min);
                    }
                    else
                    {
                        rB = Min;
                        rG = h * (Max - Min) + rB;
                    }
                }
                else if ((h < 3))
                {
                    rG = Max;
                    if ((h < 2))
                    {
                        rB = Min;
                        rR = rB - (h - 2) * (Max - Min);
                    }
                    else
                    {
                        rR = Min;
                        rB = (h - 2) * (Max - Min) + rR;
                    }
                }
                else
                {
                    rB = Max;
                    if ((h < 4))
                    {
                        rR = Min;
                        rG = rR - (h - 4) * (Max - Min);
                    }
                    else
                    {
                        rG = Min;
                        rR = (h - 4) * (Max - Min) + rG;
                    }
                }
            }
            return Color.FromArgb((int)(rR * 255) % 256, (int)(rG * 255), (int)(rB * 255));
        }

        public static string SqlEscape(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
