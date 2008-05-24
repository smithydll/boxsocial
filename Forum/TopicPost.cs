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

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_post")]
    public class TopicPost : Item
    {
        public const string FORUM_TOPIC_INFO_FIELDS = "ft.topic_id, ft.topic_title, ft.user_id, ft.item_id, ft.item_type, ft.topic_views, ft.topic_time, ft.topic_last_post_id, ft.topic_last_post_time";

        [DataField("post_id", DataFieldKeys.Primary)]
        private long postId;
        [DataField("topic_id")]
        private long topicId;
        [DataField("forum_id")]
        private long forumId;
        [DataField("user_id")]
        private long userId;
        [DataField("post_title", 127)]
        private string postTitle;
        [DataField("post_text", MYSQL_MEDIUM_TEXT)]
        private string postText;
        [DataField("post_time_ut")]
        private long createdRaw;
        [DataField("post_modified_ut")]
        private long modifiedRaw;

        private Member poster;

        public long PostId
        {
            get
            {
                return postId;
            }
        }

        public long TopicId
        {
            get
            {
                return topicId;
            }
        }

        public long ForumId
        {
            get
            {
                return forumId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string Title
        {
            get
            {
                return postTitle;
            }
            set
            {
                SetProperty("postTitle", value);
            }
        }

        public string Text
        {
            get
            {
                return postText;
            }
            set
            {
                SetProperty("postText", value);
            }
        }

        public long TimeCreatedRaw
        {
            get
            {
                return createdRaw;
            }
        }

        public long TimeModifiedRaw
        {
            get
            {
                return modifiedRaw;
            }
        }

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(createdRaw);
        }

        public DateTime GetModifiedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(modifiedRaw);
        }

        public TopicPost(Core core, DataRow postRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Post_ItemLoad);

            try
            {
                loadItemInfo(postRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPostException();
            }
        }

        void Post_ItemLoad()
        {

            if (poster == null || poster.Id != userId)
            {
                core.LoadUserProfile(userId);
                poster = core.UserProfiles[userId];
            }
        }

        public static Dictionary<long, TopicPost> GetPosts(Core core, List<long> postIds)
        {
            Dictionary<long, TopicPost> posts = new Dictionary<long, TopicPost>();

            SelectQuery query = new SelectQuery("forum_post");
            query.AddCondition("post_id", ConditionEquality.In, postIds);

            DataTable postsTable = core.db.Query(query);

            foreach (DataRow dr in postsTable.Rows)
            {
                TopicPost tp = new TopicPost(core, dr);
                posts.Add(tp.ForumId, tp);
            }

            return posts;
        }

        public override long Id
        {
            get
            {
                return postId;
            }
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

    public class InvalidPostException : Exception
    {
    }
}
