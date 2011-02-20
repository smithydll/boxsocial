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
    /// Represents a trackback
    /// </summary>
    [DataTable("trackbacks")]
    public class TrackBack : NumberedItem
    {
        [DataField("trackback_id", DataFieldKeys.Primary)]
        private long trackBackId;
        [DataField("post_id")]
        private long blogEntryId;
        [DataField("trackback_uri", 255)]
        private string trackBackUri;
        [DataField("trackback_blurb", 511)]
        private string trackBackBlurb;
        [DataField("trackback_time_ut")]
        private long trackBackTimeRaw;
        [DataField("trackback_ip", 50)]
        private string trackBackIp;

        public long TrackBackId
        {
            get
            {
                return trackBackId;
            }
        }

        public long BlogEntryId
        {
            get
            {
                return blogEntryId;
            }
        }

        public string TrackBackUri
        {
            get
            {
                return trackBackUri;
            }
        }

        public string TrackBackBlurb
        {
            get
            {
                return trackBackBlurb;
            }
        }

        public long TrackBackTimeRaw
        {
            get
            {
                return trackBackTimeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            if (tz == null)
            {
                return core.Tz.DateTimeFromMysql(tz);
            }

            return tz.DateTimeFromMysql(trackBackTimeRaw);
        }

        public TrackBack(Core core, long trackBackId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TrackBack_ItemLoad);

            try
            {
                LoadItem(trackBackId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTrackBackException();
            }
        }

        public TrackBack(Core core, DataRow trackBackRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(TrackBack_ItemLoad);

            loadItemInfo(trackBackRow);
        }

        private void TrackBack_ItemLoad()
        {
        }

        /// <summary>
        /// Create a new TrackBack
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="entry">Blog entry to attach trackback to</param>
        /// <param name="uri">Trackback uri</param>
        /// <param name="blurb">Trackback blurb</param>
        /// <returns></returns>
        public static TrackBack Create(Core core, BlogEntry entry, string uri, string blurb)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (entry == null)
            {
                throw new InvalidBlogEntryException();
            }

            // TODO: validate uri

            InsertQuery iquery = new InsertQuery(TrackBack.GetTable(typeof(TrackBack)));
            iquery.AddField("post_id", entry.PostId);
            iquery.AddField("trackback_uri", uri);
            iquery.AddField("trackback_blurb", Functions.TrimStringToWord(blurb, 511));
            iquery.AddField("trackback_time_ut", UnixTime.UnixTimeStamp());
            iquery.AddField("trackback_ip", core.Session.IPAddress.ToString());

            long id = core.Db.Query(iquery);

            return new TrackBack(core, id);
        }

        public override long Id
        {
            get
            {
                return trackBackId;
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

    public class InvalidTrackBackException : Exception
    {
    }
}
