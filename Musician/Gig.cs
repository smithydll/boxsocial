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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [DataTable("music_gigs")]
    public class Gig : NumberedItem, ICommentableItem, IRateableItem
    {
        [DataField("gig_id", DataFieldKeys.Primary)]
        private long gigId;
        [DataField("tour_id", typeof(Tour))]
        private long tourId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("gig_time_ut")]
        private long gigTime;
        [DataField("gig_time_zone")]
        private ushort timeZoneCode;
        [DataField("gig_city", 31)]
        private string gigCity;
        [DataField("gig_venue", 63)]
        private string gigVenue;
        [DataField("gig_all_ages")]
        private bool gigAllAges;
        [DataField("gig_comments")]
        private long gigComments;
        [DataField("gig_Rating")]
        private float gigRating;

        private Tour tour;
        private Musician musician;

        private long GigId
        {
            get
            {
                return gigId;
            }
        }

        public long TourId
        {
            get
            {
                return tourId;
            }
        }

        public long TimeRaw
        {
            get
            {
                return gigTime;
            }
            set
            {
                SetProperty("gigTime", value);
            }
        }

        public string City
        {
            get
            {
                return gigCity;
            }
            set
            {
                SetProperty("gigCity", value);
            }
        }

        public string Venue
        {
            get
            {
                return gigVenue;
            }
            set
            {
                SetProperty("gigVenue", value);
            }
        }

        public bool AllAges
        {
            get
            {
                return gigAllAges;
            }
            set
            {
                SetProperty("gigAllAges", value);
            }
        }

        public Musician Musician
        {
            get
            {
                ItemKey ownerKey = new ItemKey(musicianId, ItemKey.GetTypeId(typeof(Musician)));
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.Type != musician.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerKey);
                    musician = (Musician)core.UserProfiles[ownerKey];
                    return musician;
                }
                else
                {
                    return musician;
                }
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(gigTime);
        }

        public Gig(Core core, long gigId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Gig_ItemLoad);

            try
            {
                LoadItem(gigId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGigException();
            }
        }

        public Gig(Core core, DataRow gigRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Gig_ItemLoad);

            try
            {
                loadItemInfo(gigRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGigException();
            }
        }

        void Gig_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return gigId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static Gig Create(Core core, Musician owner, Tour tour, long time, ushort timezone, string city, string venue, bool allAges)
        {
            // TODO: fix this
            Item item = Item.Create(core, typeof(Gig), new FieldValuePair("musician_id", owner.Id),
                new FieldValuePair("tour_id", tour.Id),
                new FieldValuePair("gig_time_ut", time),
                new FieldValuePair("gig_time_zone", timezone),
                new FieldValuePair("gig_city", city),
                new FieldValuePair("gig_venue", venue),
                new FieldValuePair("gig_all_ages", allAges));

            return (Gig)item;
        }

        public static void Show(Core core, PPage page, long gigId)
        {
            page.template.SetTemplate("Musician", "viewgig");

            Gig gig = null;

            try
            {
                gig = new Gig(core, gigId);
            }
            catch (InvalidGigException)
            {
                core.Functions.Generate404();
                return;
            }

            page.template.Parse("CITY", gig.City);
            page.template.Parse("VENUE", gig.Venue);

            core.Display.DisplayComments(page.template, gig.Musician, gig);
        }

        public long Comments
        {
            get
            {
                return gigComments;
            }
        }

        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public float Rating
        {
            get
            {
                return gigRating;
            }
        }
    }

    public class InvalidGigException : Exception
    {
    }
}
