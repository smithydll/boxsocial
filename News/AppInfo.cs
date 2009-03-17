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
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.News
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
                return "News";
            }
        }

        public override string Stub
        {
            get
            {
                return "news";
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
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
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
			
			core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(Article)), articleCanPostComment, articleCanDeleteComment, articleAdjustCommentCount, articleCommentPosted);
        }
		
        private void articleCommentPosted(CommentPostedEventArgs e)
        {
        }

        private bool articleCanPostComment(ItemKey itemKey, User member)
        {
            SelectQuery query = Article.GetSelectQueryStub(typeof(Article), false);
            query.AddCondition("article_id", itemKey.Id);

            DataTable articleTable = core.db.Query(query);

            if (articleTable.Rows.Count == 1)
            {
                Primitive owner = null;
                switch ((string)articleTable.Rows[0]["article_item_type"])
                {
                    case "GROUP":
                        owner = new UserGroup(core, (long)articleTable.Rows[0]["article_item_id"]);
                        break;
                    case "NETWORK":
                        owner = new Network(core, (long)articleTable.Rows[0]["article_item_id"]);
                        break;
                }

				/* TODO */
                /*Access articleAccess = owner.Access;
                articleAccess.SetViewer(member);

                if (articleAccess.CanComment)
                {
                    return true;
                }
                else
                {
                    return false;
                }*/
				return true;
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        private bool articleCanDeleteComment(ItemKey itemKey, User member)
        {
            SelectQuery query = Article.GetSelectQueryStub(typeof(Article), false);
            query.AddCondition("article_id", itemKey.Id);

            DataTable articleTable = core.db.Query(query);

            if (articleTable.Rows.Count == 1)
            {
                switch ((string)articleTable.Rows[0]["article_item_type"])
                {
                    case "GROUP":
                        UserGroup group = new UserGroup(core, (long)articleTable.Rows[0]["article_item_id"]);
                        if (group.IsGroupOperator(member))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case "NETWORK":
                        return false;
                    default:
                        return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        private void articleAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE articles SET article_comments = article_comments + {1} WHERE article_id = {0};",
                itemKey.Id, adjustment));
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();
			
			aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddSlug("news", @"^/news(|/)$", AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("news", @"^/news/([0-9]+)(|/)$", AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("news");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("news", "News");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/news(|/)$", showNews, 1);
            /*core.RegisterApplicationPage(@"^/forum/topic\-([0-9])(|/)$", showTopic, 2);
            core.RegisterApplicationPage(@"^/forum/([a-zA-Z0-9])/topic\-([0-9])(|/)$", showTopic, 3);
            core.RegisterApplicationPage(@"^/forum/([a-zA-Z0-9])(|/)$", showForum, 4);*/
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Group;
        }

        [Show(@"^/news(|/)$", AppPrimitives.Group | AppPrimitives.Network)]
        private void showNews(Core core, object sender)
        {
            if (sender is GPage)
            {
                News.Show(core, (GPage)sender);
            }
        }

        [Show(@"^/news/([0-9]+)(|/)$", AppPrimitives.Group | AppPrimitives.Network)]
        private void showNewsPost(Core core, object sender)
        {
            if (sender is GPage)
            {
                Article.Show(core, (GPage)sender, long.Parse(core.PagePathParts[1].Value));
            }
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                ShowGroupNews(e);
            }
        }

        public void ShowGroupNews(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilenews");

			if (e.Owner is UserGroup)
			{
	            News news = new News(e.core, (UserGroup)e.Owner);

	            List<Article> articles = news.GetArticles(1, 5);

	            foreach (Article article in articles)
	            {
	                VariableCollection articleVariableCollection = template.CreateChild("news_list");

	                articleVariableCollection.Parse("TITLE", article.ArticleSubject);
	            }
			}

            e.core.AddSidePanel(template);
        }
    }
}