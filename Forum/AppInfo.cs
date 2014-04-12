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
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
        }

        public override string Title
        {
            get
            {
                return "Forum";
            }
        }

        public override string Stub
        {
            get
            {
                return "forum";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override bool UsesComments
        {
            get
            {
                return false;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return false;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                return Properties.Resources.forum;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = base.GetInstallInfo();

            /* Hack to add Hooks until implement a better way with attributes */
            aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Group);

            return aii;

        }

        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                slugs.Add("forum", new PageSlugAttribute("Forum", AppPrimitives.Group | AppPrimitives.Network));
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Group;
        }

        [Show(@"forum/help", AppPrimitives.Group | AppPrimitives.Network)]
        private void showHelp(Core core, object sender)
        {
            if (sender is GPage)
            {
                Default.ShowHelp(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum", AppPrimitives.Group | AppPrimitives.Network)]
        private void showForums(Core core, object sender)
        {
            if (sender is GPage)
            {
                Forum.Show(core, (GPage)sender, 0);
            }
        }

        [Show(@"forum/([0-9]+)/topic\-([0-9]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showTopic(Core core, object sender)
        {
            if (sender is GPage)
            {
                ForumTopic.Show(core, (GPage)sender, long.Parse(core.PagePathParts[1].Value), long.Parse(core.PagePathParts[2].Value));
            }
        }

        [Show(@"forum/topic\-([0-9]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showRootTopic(Core core, object sender)
        {
            if (sender is GPage)
            {
                ForumTopic.Show(core, (GPage)sender, 0, long.Parse(core.PagePathParts[1].Value));
            }
        }

        [Show(@"forum/([0-9]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showForum(Core core, object sender)
        {
            if (sender is GPage)
            {
                Forum.Show(core, (GPage)sender, long.Parse(core.PagePathParts[1].Value));
            }
        }

        [Show(@"forum/post", AppPrimitives.Group | AppPrimitives.Network)]
        private void showPoster(Core core, object sender)
        {
            if (sender is PPage)
            {
                Poster.Show(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum/ucp", AppPrimitives.Group | AppPrimitives.Network)]
        private void showUCP(Core core, object sender)
        {
            if (sender is PPage)
            {
                ForumMember.ShowUCP(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum/mcp", AppPrimitives.Group | AppPrimitives.Network)]
        private void showModeratorControlPanel(Core core, object sender)
        {
            if (sender is PPage)
            {
                ModeratorControlPanel.Show(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum/mcp/([a-z\-]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showModeratorControlPanelModule(Core core, object sender)
        {
            if (sender is PPage)
            {
                ModeratorControlPanel.Show(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum/mcp/([a-z\-]+)/([a-z\-]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showModeratorControlPanelSubModule(Core core, object sender)
        {
            if (sender is PPage)
            {
                ModeratorControlPanel.Show(sender, new ShowPPageEventArgs((PPage)sender));
            }
        }

        [Show(@"forum/memberlist", AppPrimitives.Group | AppPrimitives.Network)]
        private void showMemberlist(Core core, object sender)
        {
            if (sender is GPage)
            {
                ForumMember.ShowMemberlist(core, (GPage)sender);
            }
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                ShowGroupForum(e);
            }
        }

        public void ShowGroupForum(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofileforum");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            Forum forum = new Forum(core, (UserGroup)e.Owner);
            template.Parse("U_FORUM", forum.Uri);

            List<ForumTopic> recentTopics = forum.GetTopicsFlat(0, 10);

            // PostId, TopicPost
            Dictionary<long, TopicPost> topicLastPosts;

            topicLastPosts = TopicPost.GetTopicLastPosts(core, recentTopics);

            template.Parse("TOPICS", recentTopics.Count.ToString());

            foreach (ForumTopic topic in recentTopics)
            {
                core.LoadUserProfile(topic.PosterId);
            }

            foreach (ForumTopic topic in recentTopics)
            {
                VariableCollection topicVariableCollection = template.CreateChild("topic_list");

                topicVariableCollection.Parse("TITLE", topic.Title);
                topicVariableCollection.Parse("URI", topic.Uri);
                topicVariableCollection.Parse("VIEWS", topic.Views.ToString());
                topicVariableCollection.Parse("REPLIES", topic.Posts.ToString());
                topicVariableCollection.Parse("DATE", core.Tz.DateTimeToString(topic.GetCreatedDate(core.Tz)));
                topicVariableCollection.Parse("USERNAME", core.PrimitiveCache[topic.PosterId].DisplayName);
                topicVariableCollection.Parse("U_POSTER", core.PrimitiveCache[topic.PosterId].Uri);

                if (topicLastPosts.ContainsKey(topic.LastPostId))
                {
                    topicVariableCollection.Parse("LAST_POST_URI", topicLastPosts[topic.LastPostId].Uri);
                    topicVariableCollection.Parse("LAST_POST_TITLE", topicLastPosts[topic.LastPostId].Title);
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

            e.core.AddMainPanel(template);
        }
    }
}