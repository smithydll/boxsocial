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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "dashboard", "main", true)]
    public class McpMain : ModeratorControlPanelSubModule
    {
        public override string Title
        {
            get
            {
                return "Main";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public McpMain()
        {
            this.Load += new EventHandler(McpMain_Load);
            this.Show += new EventHandler(McpMain_Show);
        }

        void McpMain_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("lock", new ModuleModeHandler(McpMain_Lock));
            this.AddModeHandler("delete", new ModuleModeHandler(McpMain_Delete));
            this.AddModeHandler("move", new ModuleModeHandler(McpMain_Move));
            this.AddModeHandler("delete-post", new ModuleModeHandler(McpMain_DeletePost));
        }

        void McpMain_Show(object sender, EventArgs e)
        {
            //AuthoriseRequestSid();

            /* */
            SubmitButton submitButton = new SubmitButton("submit", "Submit");
            
            /* */
            SelectBox actionsSelectBox = new SelectBox("actions");

            int p = core.Functions.RequestInt("p", 1);
            SetTemplate("mcp_main");

            long forumId = core.Functions.RequestLong("fid", 0);
            Forum thisForum = null;
            ForumSettings settings = null;

            try
            {
                settings = new ForumSettings(core, Owner);
                if (forumId > 0)
                {
                    thisForum = new Forum(core, settings, forumId);
                }
                else
                {
                    thisForum = new Forum(core, settings);
                }
            }
            catch (InvalidForumSettingsException)
            {
                core.Functions.Generate404();
            }
            catch (InvalidForumException)
            {
                core.Functions.Generate404();
            }

            if (thisForum.Access.Can("LOCK_TOPICS"))
            {
                actionsSelectBox.Add(new SelectBoxItem("lock", "Lock"));
                actionsSelectBox.Add(new SelectBoxItem("unlock", "Unlock"));
            }
            if (thisForum.Access.Can("MOVE_TOPICS"))
            {
                actionsSelectBox.Add(new SelectBoxItem("move", "Move"));
            }
            if (thisForum.Access.Can("DELETE_TOPICS"))
            {
                actionsSelectBox.Add(new SelectBoxItem("delete", "Delete"));
            }

            List<ForumTopic> announcements = thisForum.GetAnnouncements();
            List<ForumTopic> topics = thisForum.GetTopics(p, settings.TopicsPerPage);
            List<ForumTopic> allTopics = new List<ForumTopic>();
            allTopics.AddRange(announcements);
            allTopics.AddRange(topics);

            Dictionary<long, TopicPost> topicLastPosts;

            topicLastPosts = TopicPost.GetTopicLastPosts(core, allTopics);

            foreach (ForumTopic topic in allTopics)
            {
                core.LoadUserProfile(topic.PosterId);
            }

            foreach (ForumTopic topic in allTopics)
            {
                VariableCollection topicVariableCollection = template.CreateChild("topic_list");

                CheckBox checkBox = new CheckBox("checkbox[" + topic.Id.ToString() + "]");

                topicVariableCollection.Parse("TITLE", topic.Title);
                topicVariableCollection.Parse("URI", topic.Uri);
                topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());
                topicVariableCollection.Parse("DATE", core.Tz.DateTimeToString(topic.GetCreatedDate(core.Tz)));
                topicVariableCollection.Parse("USERNAME", core.PrimitiveCache[topic.PosterId].DisplayName);
                topicVariableCollection.Parse("U_POSTER", core.PrimitiveCache[topic.PosterId].Uri);
                topicVariableCollection.Parse("S_CHECK", checkBox);

                if (topicLastPosts.ContainsKey(topic.LastPostId))
                {
                    core.Display.ParseBbcode(topicVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                        topicLastPosts[topic.LastPostId].Uri, Functions.TrimStringToWord(topicLastPosts[topic.LastPostId].Title, 20), core.Tz.DateTimeToString(topicLastPosts[topic.LastPostId].GetCreatedDate(core.Tz))));
                }
                else
                {
                    topicVariableCollection.Parse("LAST_POST", "No posts");
                }

                switch (topic.Status)
                {
                    case TopicStates.Normal:
                        if (topic.IsRead)
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_NORMAL_READ_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_NORMAL_READ_UNLOCKED", "TRUE");
                            }
                        }
                        else
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_NORMAL_UNREAD_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_NORMAL_UNREAD_UNLOCKED", "TRUE");
                            }
                        }
                        break;
                    case TopicStates.Sticky:
                        if (topic.IsRead)
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_STICKY_READ_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_STICKY_READ_UNLOCKED", "TRUE");
                            }
                        }
                        else
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_STICKY_UNREAD_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_STICKY_UNREAD_UNLOCKED", "TRUE");
                            }
                        }

                        break;
                    case TopicStates.Announcement:
                    case TopicStates.Global:
                        if (topic.IsRead)
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_ANNOUNCEMENT_READ_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_ANNOUNCEMENT_READ_UNLOCKED", "TRUE");
                            }
                        }
                        else
                        {
                            if (topic.IsLocked)
                            {
                                topicVariableCollection.Parse("IS_ANNOUNCEMENT_UNREAD_LOCKED", "TRUE");
                            }
                            else
                            {
                                topicVariableCollection.Parse("IS_ANNOUNCEMENT_UNREAD_UNLOCKED", "TRUE");
                            }
                        }

                        break;
                }
            }

            template.Parse("S_ACTIONS", actionsSelectBox);
            template.Parse("S_SUBMIT", submitButton);
        }

        void McpMain_Lock(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long topicId = core.Functions.FormLong("t", 0);
            ForumTopic topic = null;

            try
            {
                topic = new ForumTopic(core, topicId);
            }
            catch (InvalidTopicException)
            {
                return;
            }

            if (topic.Forum.Access.Can("LOCK_TOPICS"))
            {
                topic.IsLocked = true;

                topic.Update();
            }
        }

        void McpMain_Delete(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long topicId = core.Functions.FormLong("t", 0);
            ForumTopic topic = null;

            try
            {
                topic = new ForumTopic(core, topicId);
            }
            catch (InvalidTopicException)
            {
                return;
            }

            if (topic.Forum.Access.Can("DELETE_TOPICS"))
            {
                // TODO: statistics updating
                //topic.Delete();
            }
        }

        void McpMain_Move(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long topicId = core.Functions.FormLong("t", 0);
            long forumId = core.Functions.FormLong("f", 0);

            ForumTopic topic = null;
            Forum newForum = null;
            Forum oldForum = null;

            try
            {
                topic = new ForumTopic(core, topicId);
                oldForum = topic.Forum;
            }
            catch (InvalidTopicException)
            {
                return;
            }

            try
            {
                if (forumId > 0)
                {
                    newForum = new Forum(core, forumId);
                }
                else
                {
                    newForum = new Forum(core, Owner);
                }
            }
            catch (InvalidTopicException)
            {
                return;
            }

            /* Cannot move topics outside the forum to another owner's forum */
            if (newForum.Owner.Id != Owner.Id || newForum.Owner.TypeId != Owner.TypeId)
            {
                return;
            }

            /* Attempting to move to the same forum (not a move, ignore) */
            if (oldForum.Id == newForum.Id)
            {
                return;
            }

            db.BeginTransaction();

            {
                UpdateQuery uQuery = new UpdateQuery(typeof(ForumTopic));
                uQuery.AddField("forum_id", newForum.Id);
                uQuery.AddCondition("topic_id", topic.Id);

                db.Query(uQuery);
            }

            {
                UpdateQuery uQuery = new UpdateQuery(typeof(TopicPost));
                uQuery.AddField("forum_id", newForum.Id);
                uQuery.AddCondition("topic_id", topic.Id);

                db.Query(uQuery);
            }

            {
                UpdateQuery uQuery = new UpdateQuery(typeof(TopicReadStatus));
                uQuery.AddField("forum_id", newForum.Id);
                uQuery.AddCondition("topic_id", topic.Id);

                db.Query(uQuery);
            }

            {
                if (oldForum.Id > 0)
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                    uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Subtraction, topic.Posts + 1));
                    uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Subtraction, 1));
                    uQuery.AddCondition("forum_id", oldForum.Id);

                    db.Query(uQuery);
                }
            }

            {
                if (newForum.Id > 0)
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                    uQuery.AddField("forum_posts", new QueryOperation("forum_posts", QueryOperations.Addition, topic.Posts + 1));
                    uQuery.AddField("forum_topics", new QueryOperation("forum_topics", QueryOperations.Addition, 1));
                    uQuery.AddCondition("forum_id", newForum.Id);

                    db.Query(uQuery);
                }
            }
        }

        void McpMain_DeletePost(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            Dictionary<string, string> fieldList = new Dictionary<string, string>();
            fieldList.Add("module", "dashboard");
            fieldList.Add("sub", "main");
            fieldList.Add("mode", "delete");
            fieldList.Add("m", core.Http.Query["id"]);

            core.Display.ShowConfirmBox("", "Delete Post?", "Are you sure you want to delete this post?", fieldList);
        }

        void McpMain_DeletePostSave(object sender, ModuleModeEventArgs e)
        {
        }
    }
}
