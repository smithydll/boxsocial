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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public static class Poster
    {
        private static Core core;
        private static TPage page;

        public static void Show(Core core, GPage page)
        {
            Poster.core = core;
            Poster.page = page;

            page.template.SetTemplate("Forum", "post");

            if (HttpContext.Current.Request.Form["save"] != null) // DRAFT
            {
                ShowPostingScreen("draft");
            }
            else if (HttpContext.Current.Request.Form["preview"] != null) // PREVIEW
            {
                ShowPostingScreen("preview");
            }
            else if (HttpContext.Current.Request.Form["submit"] != null) // POST
            {
                ShowPostingScreen("post");
            }
        }

        private static void ShowPostingScreen(string mode)
        {
            long forumId = Functions.FormLong("f", 0);
            long topicId = Functions.FormLong("t", 0);
            long postId = Functions.FormLong("p", 0);
            string subject = HttpContext.Current.Request.Form["subject"];
            string text = HttpContext.Current.Request.Form["post"];

            if (forumId > 0)
            {
                page.template.Parse("S_FORUM", forumId.ToString());
            }

            if (topicId > 0)
            {
                page.template.Parse("S_TOPIC", topicId.ToString());
            }

            if (postId > 0)
            {
                page.template.Parse("S_ID", postId.ToString());
            }

            if (!string.IsNullOrEmpty(subject))
            {
                page.template.Parse("S_SUBJECT", subject);
            }

            if (!string.IsNullOrEmpty(text))
            {
                page.template.Parse("S_POST_TEXT", text);
            }

            if (topicId == 0 && (string.IsNullOrEmpty(subject) || subject.Length < 3))
            {
                page.template.Parse("ERROR", "New topic must have a subject");
                return;
            }

            if (string.IsNullOrEmpty(text) || text.Length < 3)
            {
                page.template.Parse("ERROR", "Post too short, must be at least three characters long");
                return;
            }

            if (mode == "draft" || mode == "post")
            {
                SavePost(subject, text);
            }
        }

        private static void SavePost(string subject, string text)
        {
            long postId = Functions.FormLong("p", 0);
            long forumId = Functions.FormLong("f", 0);
            long topicId = Functions.FormLong("t", 0);

            if (postId > 0)
            {
                // Edit Post
            }
            else
            {
                if (topicId > 0)
                {
                    // Post Reply
                    try
                    {
                        ForumTopic topic = new ForumTopic(core, topicId);
                    }
                    catch (InvalidTopicException)
                    {
                        Display.ShowMessage("ERROR", "An error occured");
                    }
                }
                else
                {
                    // New Topic
                }
            }
        }
    }
}
