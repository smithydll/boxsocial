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
using BoxSocial.Forms;
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
            else
            {
                ShowPostingScreen("none");
            }
        }

        private static void ShowPostingScreen(string submitMode)
        {
            long forumId = Functions.FormLong("f", Functions.RequestLong("f", 0));
            long topicId = Functions.FormLong("t", Functions.RequestLong("t", 0));
            long postId = Functions.FormLong("p", Functions.RequestLong("p", 0));
            string subject = HttpContext.Current.Request.Form["subject"];
            string text = HttpContext.Current.Request.Form["post"];
            string mode = HttpContext.Current.Request.QueryString["mode"];
            string topicState = HttpContext.Current.Request.Form["topic-state"];

            if (string.IsNullOrEmpty(mode))
            {
                mode = HttpContext.Current.Request.Form["mode"];
            }

            if (string.IsNullOrEmpty(topicState))
            {
                topicState = TopicStates.Normal.ToString();
            }

            if (page is GPage)
            {
                page.template.Parse("S_POST", string.Format("{0}forum/post",
                    ((GPage)page).ThisGroup.UriStub));

                if (((GPage)page).ThisGroup.IsGroupOperator(core.session.LoggedInMember) && topicId == 0)
                {
                    
                    List<SelectBoxItem> sbis = new List<SelectBoxItem>();
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Normal).ToString(), "Topic"));
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Sticky).ToString(), "Sticky"));
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Announcement).ToString(), "Announcement"));
                    // TODO: Global, remember to update columns to 4

                    Display.ParseRadioArray("S_TOPIC_STATE", "topic-state", 3, sbis, topicState);
                }
            }

            if (topicId > 0)
            {
                ForumTopic thisTopic = new ForumTopic(core, topicId);

                List<TopicPost> posts = thisTopic.GetLastPosts(10);

                if (posts.Count > 0)
                {
                    page.template.Parse("PREVIEW_TOPIC", "TRUE");
                }

                foreach (TopicPost post in posts)
                {
                    VariableCollection postVariableCollection = page.template.CreateChild("post_list");

                    postVariableCollection.Parse("SUBJECT", post.Title);
                    postVariableCollection.Parse("POST_TIME", core.tz.DateTimeToString(post.GetCreatedDate(core.tz)));
                    //postVariableCollection.Parse("POST_MODIFIED", core.tz.DateTimeToString(post.GetModifiedDate(core.tz)));
                    postVariableCollection.Parse("ID", post.Id.ToString());
                    Display.ParseBbcode(postVariableCollection, "TEXT", post.Text);
                    postVariableCollection.Parse("U_USER", post.Poster.Uri);
                    postVariableCollection.Parse("USER_DISPLAY_NAME", post.Poster.Info.DisplayName);
                    postVariableCollection.Parse("USER_TILE", post.Poster.UserIcon);
                    postVariableCollection.Parse("USER_JOINED", core.tz.DateTimeToString(post.Poster.Info.GetRegistrationDate(core.tz)));
                    postVariableCollection.Parse("USER_COUNTRY", post.Poster.Country);

                    if (thisTopic.ReadStatus == null)
                    {
                        postVariableCollection.Parse("IS_READ", "FALSE");
                    }
                    else
                    {
                        if (thisTopic.ReadStatus.ReadTimeRaw < post.TimeCreatedRaw)
                        {
                            postVariableCollection.Parse("IS_READ", "FALSE");
                        }
                        else
                        {
                            postVariableCollection.Parse("IS_READ", "TRUE");
                        }
                    }
                }
            }

            page.template.Parse("S_MODE", mode);

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
            else
            {
                if (topicId > 0)
                {
                    ForumTopic topic = new ForumTopic(core, topicId);
                    page.template.Parse("S_SUBJECT", "RE: " + topic.Title);
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                page.template.Parse("S_POST_TEXT", text);
            }

            if (submitMode != "none")
            {
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
            }

            if (submitMode == "preview")
            {
                Display.ParseBbcode("PREVIEW", text);
            }

            if (submitMode == "draft" || submitMode == "post")
            {
                SavePost(mode, subject, text);
            }
        }

        private static void SavePost(string mode, string subject, string text)
        {
            long postId = Functions.FormLong("p", 0);
            long forumId = Functions.FormLong("f", 0);
            long topicId = Functions.FormLong("t", 0);

            switch (mode)
            {
                case "edit":
                    // Edit Post
                    break;
                case "reply":
                    // Post Reply
                    try
                    {
                        Forum forum;

                        if (page is GPage)
                        {
                            if (forumId == 0)
                            {
                                forum = new Forum(core, ((GPage)page).ThisGroup);
                            }
                            else
                            {
                                forum = new Forum(core, ((GPage)page).ThisGroup, forumId);
                            }
                        }
                        else
                        {
                            forum = new Forum(core, forumId);
                        }

                        if (!forum.Access.CanCreate)
                        {
                            Display.ShowMessage("Cannot reply", "Not authorised to reply to topic");
                            return;
                        }

                        ForumTopic topic = new ForumTopic(core, forum, topicId);

                        TopicPost post = topic.AddReply(core, forum, subject, text);

                        page.template.Parse("REDIRECT_URI", post.Uri);
                        Display.ShowMessage("Reply Posted", "Reply has been posted");
                        return;
                    }
                    catch (InvalidTopicException)
                    {
                        Display.ShowMessage("ERROR", "An error occured");
                    }
                    break;
                case "post":
                    // New Topic
                    try
                    {
                        Forum forum;

                        if (page is GPage)
                        {
                            if (forumId == 0)
                            {
                                forum = new Forum(core, ((GPage)page).ThisGroup);
                            }
                            else
                            {
                                forum = new Forum(core, ((GPage)page).ThisGroup, forumId);
                            }
                        }
                        else
                        {
                            forum = new Forum(core, forumId);
                        }

                        if (!forum.Access.CanCreate)
                        {
                            Display.ShowMessage("Cannot create new topic", "Not authorised to create a new topic");
                            return;
                        }

                        /*try
                        {*/
                        ForumTopic topic = ForumTopic.Create(core, forum, subject, text, (TopicStates)Functions.FormByte("topic-state", (byte)TopicStates.Normal));

                        page.template.Parse("REDIRECT_URI", topic.Uri);
                        Display.ShowMessage("Topic Posted", "Topic has been posted");
                        return;
                        /*}
                        catch
                        {
                            Display.ShowMessage("Error", "Error creating new topic.");
                            return;
                        }*/
                    }
                    catch (InvalidForumException)
                    {
                        Display.ShowMessage("Cannot create new topic", "Not authorised to create a new topic");
                        return;
                    }
            }
        }
    }
}
