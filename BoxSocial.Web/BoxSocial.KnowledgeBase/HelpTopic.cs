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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.KnowledgeBase
{
    [DataTable("help_topics")]
    public class HelpTopic : NumberedItem
    {
        [DataField("topic_id", DataFieldKeys.Primary)]
        private long topicId;
        [DataField("topic_title", 63)]
        private string title;
        [DataField("topic_slug", DataFieldKeys.Unique, 63)]
        private string slug;
        [DataField("topic_text", MYSQL_MEDIUM_TEXT)]
        private string text;

        public long TopicId
        {
            get
            {
                return topicId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
            }
        }

        public string Slug
        {
            get
            {
                return slug;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                SetProperty("text", value);
            }
        }

        public HelpTopic(Core core, long topicId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(HelpTopic_ItemLoad);

            try
            {
                LoadItem(topicId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidHelpTopicException();
            }
        }

        public HelpTopic(Core core, string slug)
            : base(core)
        {
            try
            {
                LoadItem("topic_slug", slug);
            }
            catch (InvalidItemException)
            {
                throw new InvalidHelpTopicException();
            }
        }

        public HelpTopic(Core core, DataRow helpRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(HelpTopic_ItemLoad);

            loadItemInfo(helpRow);
        }

        void HelpTopic_ItemLoad()
        {
            
        }
        
        public override long Id
        {
            get
            {
                return topicId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidHelpTopicException : Exception
    {
    }
}
