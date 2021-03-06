﻿/*
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
        [DataField("gig_rating")]
        private float gigRating;
        [DataField("gig_ratings")]
        private long gigRatings;
        [DataField("gig_title", 31)]
        private string gigTitle;
        [DataField("gig_abstract", MYSQL_TEXT)]
        private string gigAbstract;
        [DataField("gig_cost", 31)]
        private string gigCost;
        [DataField("gig_tickets_door")]
        private bool gigTicketsAtTheDoor;
        [DataField("gig_tickets_uri", 127)]
        private string gigTicketsUri;

        private Tour tour;
        private Musician musician;

        public event CommentHandler OnCommentPosted;

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

        public string Cost
        {
            get
            {
                return gigCost;
            }
            set
            {
                SetProperty("gigCost", value);
            }
        }

        public bool TicketsAtTheDoor
        {
            get
            {
                return gigTicketsAtTheDoor;
            }
            set
            {
                SetProperty("gigTicketsAtTheDoor", value);
            }
        }

        public string TicketsUri
        {
            get
            {
                return gigTicketsUri;
            }
            set
            {
                SetProperty("gigTicketsUri", value);
            }
        }

        public Musician Musician
        {
            get
            {
                ItemKey ownerKey = new ItemKey(musicianId, ItemKey.GetTypeId(core, typeof(Musician)));
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.TypeId != musician.TypeId)
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

        public Primitive Owner
        {
            get
            {
                return (Primitive)Musician;
            }
        }

        public Tour Tour
        {
            get
            {
                if (tourId == 0)
                {
                    return null;
                }
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
            OnCommentPosted += new CommentHandler(Gig_CommentPosted);
        }

        bool Gig_CommentPosted(CommentPostedEventArgs e)
        {
            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
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
                return Musician.UriStub + "gigs/" + gigId.ToString();
            }
        }

        public static Gig Create(Core core, Musician owner, Tour tour, long time, ushort timezone, string city, string venue, string gigAbstract, bool allAges)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            // TODO: fix this
            Item item = Item.Create(core, typeof(Gig), new FieldValuePair("musician_id", owner.Id),
                new FieldValuePair("tour_id", ((tour != null) ? tour.Id : 0)),
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

        public static void ShowAll(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewgigs");

            List<Gig> gigs = e.Page.Musician.GetGigs();

            foreach (Gig gig in gigs)
            {
                VariableCollection gigVariableCollection = e.Template.CreateChild("gig_list");

                gigVariableCollection.Parse("ID", gig.Id.ToString());
                gigVariableCollection.Parse("U_GIG", gig.Uri);
                gigVariableCollection.Parse("DESCRIPTION", gig.Abstract);
                gigVariableCollection.Parse("CITY", gig.City);
                gigVariableCollection.Parse("VENUE", gig.Venue);
                gigVariableCollection.Parse("DATE", e.Core.Tz.DateTimeToDateString(gig.GetTime(e.Core.Tz)));
                gigVariableCollection.Parse("COMMENTS", e.Core.Functions.LargeIntegerToString(gig.Comments));
            }

            List<string[]> gigPath = new List<string[]>();
            gigPath.Add(new string[] { "gigs", "Gigs" });

            e.Page.Owner.ParseBreadCrumbs(gigPath);
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
            e.Template.Parse("COST", gig.Cost);
            e.Template.Parse("IS_TICKETS_AT_DOOR", (gig.TicketsAtTheDoor) ? "TRUE" : "FALSE");
            e.Template.Parse("U_TICKETS", gig.TicketsUri);
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
                gigPath.Add(new string[] { "*tours/" + gig.Tour.Id.ToString(), gig.Tour.Title });
            }
            gigPath.Add(new string[] { "gigs", "Gigs" });
            gigPath.Add(new string[] { gig.Id.ToString(), gig.Venue });

            e.Page.Owner.ParseBreadCrumbs(gigPath);
        }

        public static List<Gig> GetAll(Core core, Musician owner)
        {
            List<Gig> gigs = new List<Gig>();

            SelectQuery query = Gig.GetSelectQueryStub(core, typeof(Gig));
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
                return Info.Comments;
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

        public string Noun
        {
            get
            {
                return "gig";
            }
        }

        public bool CanComment
        {
            get
            {
                return Owner.Access.Can("COMMENT");
            }
        }
    }

    public class InvalidGigException : InvalidItemException
    {
    }
}
