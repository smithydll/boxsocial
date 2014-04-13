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
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public class Poster
    {
        private Core core;
        private PPage page;

        public Poster(Core core, PPage page)
        {
            this.core = core;
            this.page = page;
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            Poster poster = new Poster(e.Core, e.Page);

            e.Template.SetTemplate("Forum", "post");
            ForumSettings.ShowForumHeader(e.Core, e.Page);

            e.Template.Parse("USER_ICON", e.Page.Owner.Thumbnail);
            e.Template.Parse("USER_COVER_PHOTO", e.Page.Owner.CoverPhoto);
            e.Template.Parse("USER_MOBILE_COVER_PHOTO", e.Page.Owner.MobileCoverPhoto);

            if (e.Core.Http.Form["save"] != null) // DRAFT
            {
                poster.ShowPostingScreen("draft");
            }
            else if (e.Core.Http.Form["preview"] != null) // PREVIEW
            {
                poster.ShowPostingScreen("preview");
            }
            else if (e.Core.Http.Form["submit"] != null) // POST
            {
                poster.ShowPostingScreen("post");
            }
            else
            {
                poster.ShowPostingScreen("none");
            }
        }

        private void ShowPostingScreen(string submitMode)
        {
            long forumId = core.Functions.FormLong("f", core.Functions.RequestLong("f", 0));
            long topicId = core.Functions.FormLong("t", core.Functions.RequestLong("t", 0));
            long postId = core.Functions.FormLong("p", core.Functions.RequestLong("p", 0));
            string subject = core.Http.Form["subject"];
            string text = core.Http.Form["post"];
            string mode = core.Http.Query["mode"];
            string topicState = core.Http.Form["topic-state"];

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", "Forum" });

            if (string.IsNullOrEmpty(mode))
            {
                mode = core.Http.Form["mode"];
            }

            if (string.IsNullOrEmpty(topicState))
            {
                topicState = ((byte)TopicStates.Normal).ToString();
            }

            List<SelectBoxItem> sbis = new List<SelectBoxItem>();
            sbis.Add(new SelectBoxItem(((byte)TopicStates.Normal).ToString(), "Topic"));

            if (page is GPage)
            {
                core.Template.Parse("S_POST", core.Hyperlink.AppendSid(string.Format("{0}forum/post",
                    ((GPage)page).Group.UriStub), true));

                if (((GPage)page).Group.IsGroupOperator(core.Session.LoggedInMember.ItemKey) && topicId == 0)
                {
                    // TODO: Global, remember to update columns to 4
                }
            }

            if (topicId > 0)
            {
                ForumTopic thisTopic = new ForumTopic(core, topicId);

                List<TopicPost> posts = thisTopic.GetLastPosts(10);

                if (posts.Count > 0)
                {
                    core.Template.Parse("PREVIEW_TOPIC", "TRUE");
                }

                foreach (TopicPost post in posts)
                {
                    VariableCollection postVariableCollection = core.Template.CreateChild("post_list");

                    postVariableCollection.Parse("SUBJECT", post.Title);
                    postVariableCollection.Parse("POST_TIME", core.Tz.DateTimeToString(post.GetCreatedDate(core.Tz)));
                    //postVariableCollection.Parse("POST_MODIFIED", core.tz.DateTimeToString(post.GetModifiedDate(core.tz)));
                    postVariableCollection.Parse("ID", post.Id.ToString());
                    core.Display.ParseBbcode(postVariableCollection, "TEXT", post.Text);
                    postVariableCollection.Parse("U_USER", post.Poster.Uri);
                    postVariableCollection.Parse("USER_DISPLAY_NAME", post.Poster.UserInfo.DisplayName);
                    postVariableCollection.Parse("USER_TILE", post.Poster.Tile);
                    postVariableCollection.Parse("USER_ICON", post.Poster.Icon);
                    postVariableCollection.Parse("USER_JOINED", core.Tz.DateTimeToString(post.Poster.UserInfo.GetRegistrationDate(core.Tz)));
                    postVariableCollection.Parse("USER_COUNTRY", post.Poster.Profile.Country);

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

                if (thisTopic.Forum.Access.Can("CREATE_STICKY"))
                {
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Sticky).ToString(), "Sticky"));
                }
                if (thisTopic.Forum.Access.Can("CREATE_ANNOUNCEMENTS"))
                {
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Announcement).ToString(), "Announcement"));
                }

                if (thisTopic.Forum.Parents != null)
                {
                    foreach (ParentTreeNode ptn in thisTopic.Forum.Parents.Nodes)
                    {
                        breadCrumbParts.Add(new string[] { "*" + ptn.ParentId.ToString(), ptn.ParentTitle });
                    }
                }

                if (thisTopic.Forum.Id > 0)
                {
                    breadCrumbParts.Add(new string[] { thisTopic.Forum.Id.ToString(), thisTopic.Forum.Title });
                }

                breadCrumbParts.Add(new string[] { "topic-" + thisTopic.Id.ToString(), thisTopic.Title });

                breadCrumbParts.Add(new string[] { "*forum/post/?t=" + thisTopic.Id.ToString() + "&mode=reply", "Post Reply" });
            }
            else if (forumId > 0)
            {
                Forum forum = new Forum(core, forumId);
                if (!forum.Access.Can("CREATE_TOPICS"))
                {
                    core.Display.ShowMessage("Cannot create new topic", "Not authorised to create a new topic");
                    return;
                }

                if (forum.Access.Can("CREATE_STICKY"))
                {
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Sticky).ToString(), "Sticky"));
                }
                if (forum.Access.Can("CREATE_ANNOUNCEMENTS"))
                {
                    sbis.Add(new SelectBoxItem(((byte)TopicStates.Announcement).ToString(), "Announcement"));
                }

                if (forum.Parents != null)
                {
                    foreach (ParentTreeNode ptn in forum.Parents.Nodes)
                    {
                        breadCrumbParts.Add(new string[] { "*" + ptn.ParentId.ToString(), ptn.ParentTitle });
                    }
                }

                if (forum.Id > 0)
                {
                    breadCrumbParts.Add(new string[] { forum.Id.ToString(), forum.Title });
                }

                breadCrumbParts.Add(new string[] { "*forum/post/?f=" + forum.Id.ToString() + "&mode=post", "New Topic" });
            }

            core.Template.Parse("S_MODE", mode);

            if (forumId > 0)
            {
                core.Template.Parse("S_FORUM", forumId.ToString());
            }

            if (topicId > 0)
            {
                core.Template.Parse("S_TOPIC", topicId.ToString());
            }

            if (postId > 0)
            {
                core.Template.Parse("S_ID", postId.ToString());
            }

            if (!string.IsNullOrEmpty(subject))
            {
                core.Template.Parse("S_SUBJECT", subject);
            }
            else
            {
                if (topicId > 0)
                {
                    ForumTopic topic = new ForumTopic(core, topicId);
                    core.Template.Parse("S_SUBJECT", "RE: " + topic.Title);
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                core.Template.Parse("S_POST_TEXT", text);
            }

            if (sbis.Count > 1 && (mode == "post" || mode == "edit"))
            {
                core.Display.ParseRadioArray("S_TOPIC_STATE", "topic-state", sbis.Count, sbis, topicState);
            }

            if (submitMode != "none")
            {
                if (topicId == 0 && (string.IsNullOrEmpty(subject) || subject.Length < 3))
                {
                    core.Template.Parse("ERROR", "New topic must have a subject");
                    return;
                }

                if (string.IsNullOrEmpty(text) || text.Length < 3)
                {
                    core.Template.Parse("ERROR", "Post too short, must be at least three characters long");
                    return;
                }
            }

            if (submitMode == "preview")
            {
                core.Display.ParseBbcode("PREVIEW", text);
                core.Display.ParseBbcode("SUBJECT", subject);

                try
                {
                    ForumMember member = new ForumMember(core, page.Owner, page.loggedInMember);

                    core.Display.ParseBbcode("SIGNATURE", member.ForumSignature);
                }
                catch (InvalidForumMemberException)
                {
                }
            }

            if (submitMode == "draft" || submitMode == "post")
            {
                SavePost(mode, subject, text);
            }

            page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }

        private void SavePost(string mode, string subject, string text)
        {
            AccountSubModule.AuthoriseRequestSid(core);

            long postId = core.Functions.FormLong("p", 0);
            long forumId = core.Functions.FormLong("f", 0);
            long topicId = core.Functions.FormLong("t", 0);

            switch (mode)
            {
                case "edit":
                    // Edit Post
                    break;
                case "reply":
                    // Post Reply
                    try
                    {
                        ForumSettings settings = new ForumSettings(core, ((PPage)page).Owner);
                        Forum forum;

                        if (forumId == 0)
                        {
                            forum = new Forum(core, settings);
                        }
                        else
                        {
                            forum = new Forum(core, forumId);
                        }

                        if (!forum.Access.Can("REPLY_TOPICS"))
                        {
                            core.Display.ShowMessage("Cannot reply", "Not authorised to reply to topic");
                            return;
                        }
					
                        ForumTopic topic = new ForumTopic(core, forum, topicId);

                        if (topic.IsLocked)
                        {
                            core.Display.ShowMessage("Topic Locked", "The topic cannot be replied to as it has been locked.");
                            return;
                        }

                        TopicPost post = topic.AddReply(core, forum, subject, text);

                        core.Template.Parse("REDIRECT_URI", post.Uri);
                        core.Display.ShowMessage("Reply Posted", "Reply has been posted");
                        return;
                    }
                    catch (InvalidTopicException)
                    {
                        core.Display.ShowMessage("ERROR", "An error occured");
                    }
                    break;
                case "post":
                    // New Topic
                    try
                    {
                        Forum forum;

                        if (forumId == 0 && page is PPage)
                        {
                            forum = new Forum(core, ((PPage)page).Owner);
                        }
                        else
                        {
                            forum = new Forum(core, forumId);
                        }

                        if (!forum.Access.Can("CREATE_TOPICS"))
                        {
                            core.Display.ShowMessage("Cannot create new topic", "Not authorised to create a new topic");
                            return;
                        }

                        /*try
                        {*/
                        TopicStates topicState = 0;

                        if (core.Http["topic-state"] != null)
                        {
                            topicState = (TopicStates)core.Functions.FormByte("topic-state", (byte)TopicStates.Normal);
                        }

                        if (topicState == TopicStates.Announcement && (!forum.Access.Can("CREATE_ANNOUNCEMENTS")))
                        {
                            topicState = TopicStates.Normal;
                        }

                        if (topicState == TopicStates.Sticky && (!forum.Access.Can("CREATE_STICKY")))
                        {
                            topicState = TopicStates.Normal;
                        }

                        ForumTopic topic = ForumTopic.Create(core, forum, subject, text, topicState);

                        core.Template.Parse("REDIRECT_URI", topic.Uri);
                        core.Display.ShowMessage("Topic Posted", "Topic has been posted");
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
                        core.Display.ShowMessage("Cannot create new topic", "Not authorised to create a new topic");
                        return;
                    }
            }
        }
    }
}
