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
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Forum
{
    [DataTable("forum_read_status")]
    public class ForumReadStatus : Item
    {
        [DataFieldKey(DataFieldKeys.Index, "i_forum_read_forum_id")]
        [DataField("forum_id", DataFieldKeys.Unique, "frs_key")]
        private long forumId;
        [DataField("user_id", DataFieldKeys.Unique, "frs_key")]
        private long userId;
        [DataField("read_time_ut")]
        private long readTime;

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

        public long ReadTimeRaw
        {
            get
            {
                return readTime;
            }
        }

        public DateTime GetReadTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(readTime);
        }

        public ForumReadStatus(Core core, long forumId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumReadStatus_ItemLoad);

            SelectQuery query = GetSelectQueryStub();
            query.AddCondition("forum_id", forumId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            DataTable itemTable = db.Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);
            }
            else
            {
                throw new InvalidForumReadStatusException();
            }
        }

        public ForumReadStatus(Core core, DataRow dr)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumReadStatus_ItemLoad);

            try
            {
                loadItemInfo(dr);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumReadStatusException();
            }
        }

        public ForumReadStatus(Core core, System.Data.Common.DbDataReader dr)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumReadStatus_ItemLoad);

            try
            {
                loadItemInfo(dr);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumReadStatusException();
            }
        }

        protected override void loadItemInfo(DataRow readStatusRow)
        {
            try
            {
                loadValue(readStatusRow, "forum_id", out forumId);
                loadValue(readStatusRow, "user_id", out userId);
                loadValue(readStatusRow, "read_time_ut", out readTime);

                itemLoaded(readStatusRow);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader readStatusRow)
        {
            try
            {
                loadValue(readStatusRow, "forum_id", out forumId);
                loadValue(readStatusRow, "user_id", out userId);
                loadValue(readStatusRow, "read_time_ut", out readTime);

                itemLoaded(readStatusRow);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        void ForumReadStatus_ItemLoad()
        {
        }

        internal static void Create(Core core, Forum forum)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (core.LoggedInMemberId > 0)
            {
                InsertQuery iQuery = new InsertQuery(GetTable(typeof(ForumReadStatus)));
                iQuery.AddField("forum_id", forum.Id);
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("read_time_ut", UnixTime.UnixTimeStamp()); // forum.LastPostTimeRaw

                core.Db.Query(iQuery);
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

    public class InvalidForumReadStatusException : Exception
    {
    }
}
