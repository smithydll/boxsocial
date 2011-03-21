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
using System.Text;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    /// <summary>
    /// Represents a pingback
    /// </summary>
    [DataTable("pingbacks")]
    public class PingBack : NumberedItem
    {
        [DataField("pingback_id", DataFieldKeys.Primary)]
        private long pingBackId;
        [DataField("blog_id", typeof(Blog))]
        private long blogId;
        [DataField("post_id", typeof(BlogEntry))]
        private long blogEntryId;
        [DataField("pingback_uri", 255)]
        private string pingBackUri;
        [DataField("pingback_time_ut")]
        private long pingBackTimeRaw;
        [DataField("pingback_ip", 50)]
        private string pingBackIp;

        public long PingBackId
        {
            get
            {
                return pingBackId;
            }
        }

        public long BlogEntryId
        {
            get
            {
                return blogEntryId;
            }
        }

        public string PingBackUri
        {
            get
            {
                return pingBackUri;
            }
        }

        public long PingBackTimeRaw
        {
            get
            {
                return pingBackTimeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            if (tz == null)
            {
                return core.Tz.DateTimeFromMysql(tz);
            }

            return tz.DateTimeFromMysql(pingBackTimeRaw);
        }

        public PingBack(Core core, long pingBackId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PingBack_ItemLoad);

            try
            {
                LoadItem(pingBackId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTrackBackException();
            }
        }

        public PingBack(Core core, DataRow pingBackRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PingBack_ItemLoad);

            loadItemInfo(pingBackRow);
        }

        private void PingBack_ItemLoad()
        {
        }

        public static PingBack Create(Core core, BlogEntry entry, string uri)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (entry == null)
            {
                throw new InvalidBlogEntryException();
            }

            InsertQuery iquery = new InsertQuery(PingBack.GetTable(typeof(PingBack)));
            iquery.AddField("post_id", entry.PostId);
            iquery.AddField("pingback_uri", uri);
            iquery.AddField("pingback_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("pingback_ip", core.Session.IPAddress.ToString());

            long id = core.Db.Query(iquery);

            return new PingBack(core, id);
        }

        public override long Id
        {
            get
            {
                return pingBackId;
            }
        }

        public override string Uri
        {
            get
            {
                return "";
            }
        }
    }

    public class InvalidPingBackException : Exception
    {
    }
}
