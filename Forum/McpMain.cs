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
            this.Load += new EventHandler(AccountOverview_Load);
            this.Show += new EventHandler(AccountOverview_Show);
        }

        void AccountOverview_Load(object sender, EventArgs e)
        {
        }

        void AccountOverview_Show(object sender, EventArgs e)
        {
            //AuthoriseRequestSid();

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

                topicVariableCollection.Parse("TITLE", topic.Title);
                topicVariableCollection.Parse("URI", topic.Uri);
                topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());
                topicVariableCollection.Parse("DATE", core.tz.DateTimeToString(topic.GetCreatedDate(core.tz)));
                topicVariableCollection.Parse("USERNAME", core.PrimitiveCache[topic.PosterId].DisplayName);
                topicVariableCollection.Parse("U_POSTER", core.PrimitiveCache[topic.PosterId].Uri);

                if (topicLastPosts.ContainsKey(topic.LastPostId))
                {
                    core.Display.ParseBbcode(topicVariableCollection, "LAST_POST", string.Format("[iurl={0}]{1}[/iurl]\n{2}",
                        topicLastPosts[topic.LastPostId].Uri, Functions.TrimStringToWord(topicLastPosts[topic.LastPostId].Title, 20), core.tz.DateTimeToString(topicLastPosts[topic.LastPostId].GetCreatedDate(core.tz))));
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
        }
    }
}
