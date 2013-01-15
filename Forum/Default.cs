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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Forum
{
    public class Default
    {
        public static void ShowHelp(object sender, ShowPPageEventArgs e)
        {
            ForumSettings settings;
            try
            {
                settings = new ForumSettings(e.Core, e.Page.Owner);
            }
            catch (InvalidForumSettingsException)
            {
                ForumSettings.Create(e.Core, e.Page.Owner);
                settings = new ForumSettings(e.Core, e.Page.Owner);
            }

            e.Template.SetTemplate("Forum", "help");
            ForumSettings.ShowForumHeader(e.Core, e.Page);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", "Forum" });
            breadCrumbParts.Add(new string[] { "help", "Help" });

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }
    }
}
