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
        [DataField("gig_rating")]
        private float gigRating;
        [DataField("gig_ratings")]
        private long gigRatings;
        [DataField("gig_title", 31)]
        private string gigTitle;
        [DataField("gig_abstract", MYSQL_TEXT)]
        private string gigAbstract;

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
            set
            {
                SetProperty("tourId", value);
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

        public string Title
        {
            get
            {
                return gigTitle;
            }
            set
            {
                SetProperty("gigTitle", value);
            }
        }

        public string Abstract
        {
            get
            {
                return gigAbstract;
            }
            set
            {
                SetProperty("gigAbstract", value);
            }
        }

        public Musician Musician
        {
            get
            {
                ItemKey ownerKey = new ItemKey(musicianId, ItemKey.GetTypeId(typeof(Musician)));
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.Type != musician.Type)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    musician = (Musician)core.PrimitiveCache[ownerKey];
                    return musician;
                }
                else
                {
                    return musician;
                }
            }
        }

        public Tour Tour
        {
            get
            {
                if (tour == null || tour.Id != tourId)
                {
                    tour = new Tour(core, tourId);
                }
                return tour;
            }
        }

        public UnixTime TimeZone
        {
            get
            {
                return new UnixTime(core, timeZoneCode);
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

        public Gig(Core core, Musician musician, long gigId)
            : base(core)
        {
            this.musician = musician;
            ItemLoad += new ItemLoadHandler(Gig_ItemLoad);

            try
            {
                LoadItem(gigId, new FieldValuePair("musician_id", musician.Id));
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

        public Gig(Core core, Tour tour, DataRow gigRow)
            : base(core)
        {
            this.tour = tour;
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

        public Gig(Core core, Musician musician, DataRow gigRow)
            : base(core)
        {
            this.musician = musician;
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
                return Musician.UriStub + "gig/" + gigId.ToString();
            }
        }

        public static Gig Create(Core core, Musician owner, Tour tour, long time, ushort timezone, string city, string venue, string gigAbstract, bool allAges)
        {
            // TODO: fix this
            Item item = Item.Create(core, typeof(Gig), new FieldValuePair("musician_id", owner.Id),
                new FieldValuePair("tour_id", tour.Id),
                new FieldValuePair("gig_time_ut", time),
                new FieldValuePair("gig_time_zone", timezone),
                new FieldValuePair("gig_city", city),
                new FieldValuePair("gig_venue", venue),
                new FieldValuePair("gig_abstract", gigAbstract),
                new FieldValuePair("gig_all_ages", allAges));

            Gig gig = (Gig)item;

            if (gig.TourId > 0)
            {
                UpdateQuery uQuery = new UpdateQuery(typeof(Tour));
                uQuery.AddField("tour_gigs", new QueryOperation("tour_gigs", QueryOperations.Addition, "1"));
                uQuery.AddCondition("tour_id", gig.TourId);
            }

            return gig;
        }

        public static void Show(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewgig");

            Gig gig = null;

            try
            {
                gig = new Gig(e.Core, (Musician)e.Page.Owner, e.ItemId);
            }
            catch (InvalidGigException)
            {
                e.Core.Functions.Generate404();
                return;
            }

            e.Template.Parse("CITY", gig.City);
            e.Template.Parse("VENUE", gig.Venue);
            e.Template.Parse("TIME", e.Core.Tz.DateTimeToString(gig.GetTime(e.Core.Tz)));
            e.Template.Parse("YEAR", gig.GetTime(gig.TimeZone).Year.ToString());
            e.Core.Display.ParseBbcode("ABSTRACT", gig.Abstract);

            if (e.Page.Owner.Access.Can("COMMENT_GIGS"))
            {
                e.Template.Parse("CAN_COMMENT", "TRUE");
            }

            e.Core.Display.DisplayComments(e.Template, gig.Musician, gig);

            List<string[]> gigPath = new List<string[]>();
            if (gig.Tour != null)
            {
                gigPath.Add(new string[] { "*tours", "Tours" });
                gigPath.Add(new string[] { "*tour", gig.Tour.Title });
            }
            gigPath.Add(new string[] { "*gigs", "Gigs" });
            gigPath.Add(new string[] { "gig/" + gig.Id.ToString(), gig.Venue });

            e.Page.Owner.ParseBreadCrumbs(gigPath);
        }

        public static List<Gig> GetAll(Core core, Musician owner)
        {
            List<Gig> gigs = new List<Gig>();

            SelectQuery query = Gig.GetSelectQueryStub(typeof(Gig));
            query.AddCondition("musician_id", owner.Id);

            DataTable gigTable = core.Db.Query(query);

            foreach (DataRow dr in gigTable.Rows)
            {
                gigs.Add(new Gig(core, dr));
            }

            return gigs;
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

        public long Ratings
        {
            get
            {
                return gigRatings;
            }
        }
    }

    public class InvalidGigException : Exception
    {
    }
}
