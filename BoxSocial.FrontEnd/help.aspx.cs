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
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class help : TPage
    {
        public help()
            : base("help.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string topicSlug = Request.QueryString["topic"];

            if (!string.IsNullOrEmpty(topicSlug))
            {
                template.SetTemplate("help_topic.html");
            }
            else
            {
                DataTable helpTopicsTable = db.Query("SELECT topic_title, topic_slug, topic_id FROM help_topics ORDER BY topic_title ASC;");

                foreach (DataRow topicRow in helpTopicsTable.Rows)
                {
                    VariableCollection topicsVariableCollection = template.CreateChild("topics");

                    topicsVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/help/{0}",
                        (string)topicRow["topic_slug"]))));
                    topicsVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)topicRow["topic_title"]));
                }
            }

            EndResponse();
        }
    }
}
