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
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_settings")]
    public class ForumSettings : Item
    {
        [DataField("forum_item_id", DataFieldKeys.Unique)]
        private long ownerId;
        [DataField("forum_item_type", DataFieldKeys.Unique, 63)]
        private string ownerType;
        [DataField("forum_topics")]
        private long topics;
        [DataField("forum_posts")]
        private long posts;

        private Primitive owner;

        public ForumSettings(Core core, UserGroup owner)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(ForumSettings_ItemLoad);

            try
            {
                LoadItem("forum_item_id", "forum_item_type", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumSettingsException();
            }
        }

        void ForumSettings_ItemLoad()
        {
        }

        public override long Id
        {
            get { throw new NotImplementedException(); }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidForumSettingsException : Exception
    {
    }
}
