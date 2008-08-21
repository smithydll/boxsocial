﻿/*
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
        [DataField("forum_item_id", DataFieldKeys.Unique, "fs_key")]
        private long ownerId;
        [DataField("forum_item_type", DataFieldKeys.Unique, "fs_key", 63)]
        private string ownerType;
        [DataField("forum_topics")]
        private long topics;
        [DataField("forum_posts")]
        private long posts;
        [DataField("forum_topics_per_page")]
        private int topicsPerPage;
        [DataField("forum_posts_per_page")]
        private int postsPerPage;
        [DataField("forum_allow_topics_root")]
        private bool allowTopicsAtRoot;

        private Primitive owner;

        public long Posts
        {
            get
            {
                return posts;
            }
        }

        public long Topics
        {
            get
            {
                return topics;
            }
        }

        public int TopicsPerPage
        {
            get
            {
                return topicsPerPage;
            }
            set
            {
                SetProperty("topicsPerPage", value);
            }
        }

        public int PostsPerPage
        {
            get
            {
                return postsPerPage;
            }
            set
            {
                SetProperty("postsPerPage", value);
            }
        }

        public bool AllowTopicsAtRoot
        {
            get
            {
                return allowTopicsAtRoot;
            }
            set
            {
                SetProperty("allowTopicsAtRoot", value);
            }
        }

        public ForumSettings(Core core, Primitive owner)
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

        public static void Create(Core core, UserGroup thisGroup)
        {
            if (!thisGroup.IsGroupOperator(core.session.LoggedInMember))
            {
                // todo: throw new exception
                throw new UnauthorisedToCreateItemException();
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ForumSettings)));
            iQuery.AddField("forum_item_id", thisGroup.Id);
            iQuery.AddField("forum_item_type", thisGroup.Type);
            iQuery.AddField("forum_topics", 0);
            iQuery.AddField("forum_posts", 0);
            iQuery.AddField("forum_topics_per_page", 10);
            iQuery.AddField("forum_posts_per_page", 10);
            iQuery.AddField("forum_allow_topics_root", true);

            core.db.Query(iQuery);
        }

        public new long Update()
        {
            if (owner is UserGroup)
            {
                if (!((UserGroup)owner).IsGroupOperator(core.session.LoggedInMember))
                {
                    // todo: throw new exception
                    throw new UnauthorisedToCreateItemException();
                }
            }

            UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(ForumSettings)));
            uQuery.AddField("forum_topics_per_page", topicsPerPage);
            uQuery.AddField("forum_posts_per_page", postsPerPage);
            uQuery.AddField("forum_allow_topics_root", allowTopicsAtRoot);

            uQuery.AddCondition("forum_item_id", ownerId);
            uQuery.AddCondition("forum_item_type", ownerType);

            return core.db.Query(uQuery);
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