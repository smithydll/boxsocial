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
                return true;
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
                return null;
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
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            return aii;
        }

        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                slugs.Add("news", new PageSlugAttribute("News", AppPrimitives.Group | AppPrimitives.Network));
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

        [Show(@"news", AppPrimitives.Group | AppPrimitives.Network)]
        private void showNews(Core core, object sender)
        {
            if (sender is GPage)
            {
                News.Show(sender, new ShowGPageEventArgs((GPage)sender));
            }
        }

        [Show(@"news/([0-9]+)", AppPrimitives.Group | AppPrimitives.Network)]
        private void showNewsPost(Core core, object sender)
        {
            if (sender is GPage)
            {
                Article.Show(sender, new ShowGPageEventArgs((GPage)sender, long.Parse(core.PagePathParts[1].Value)));
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
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

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