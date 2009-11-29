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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [DataTable("music_tours")]
    public class Tour : NumberedItem
    {
        [DataField("tour_id", DataFieldKeys.Primary)]
        private long tourId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("tour_title", 127)]
        private string tourTitle;
        [DataField("tour_gigs")]
        private long tourGigCount;
        [DataField("tour_year")]
        private short tourStartYear;

        private Musician musician;

        public long TourId
        {
            get
            {
                return tourId;
            }
        }

        public string Title
        {
            get
            {
                return tourTitle;
            }
            set
            {
                SetProperty("tourTitle", value);
            }
        }

        public long Gigs
        {
            get
            {
                return tourGigCount;
            }
        }

        public short StartYear
        {
            get
            {
                return tourStartYear;
            }
            set
            {
                SetProperty("tourStartYear", value);
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

        public Tour(Core core, long tourId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tour_ItemLoad);

            try
            {
                LoadItem(tourId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTourException();
            }
        }

        public Tour(Core core, DataRow tourRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Tour_ItemLoad);

            try
            {
                loadItemInfo(tourRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTourException();
            }
        }

        void Tour_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return tourId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<Gig> GetGigs()
        {
            return getSubItems(typeof(Gig), true).ConvertAll<Gig>(new Converter<Item, Gig>(convertToGig));
        }

        public Gig convertToGig(Item input)
        {
            return (Gig)input;
        }

        public static Tour Create(Core core, Musician owner, string title, short year)
        {
            // TODO: fix this
            Item item = Item.Create(core, typeof(Tour), new FieldValuePair("musician_id", owner.Id),
                new FieldValuePair("tour_title", title),
                new FieldValuePair("tour_year", year));

            return (Tour)item;
        }

        public static void Show(Core core, PPage page)
        {
            page.template.SetTemplate("Musician", "viewtours");

            if (!(page is MPage))
            {
                core.Functions.Generate404();
                return;
            }

            List<Tour> tours = Tour.GetAll(core, (Musician)page.Owner);

            foreach (Tour tour in tours)
            {
                VariableCollection tourVariableCollection = page.template.CreateChild("tour_list");

                tourVariableCollection.Parse("ID", tour.Id.ToString());
            }

            List<string[]> tourPath = new List<string[]>();
            tourPath.Add(new string[] { "*tours", "Tours" });

            page.Owner.ParseBreadCrumbs(tourPath);
        }

        public static void Show(Core core, PPage page, long tourId)
        {
            page.template.SetTemplate("Musician", "viewtour");

            Tour tour = null;

            try
            {
                tour = new Tour(core, tourId);
            }
            catch (InvalidTourException)
            {
                core.Functions.Generate404();
                return;
            }

            List<Gig> gigs = tour.GetGigs();

            foreach (Gig gig in gigs)
            {
                VariableCollection gigVariableCollection = page.template.CreateChild("gig_list");

                gigVariableCollection.Parse("ID", gig.Id.ToString());
            }

            List<string[]> tourPath = new List<string[]>();
            tourPath.Add(new string[] { "*tours", "Tours" });
            tourPath.Add(new string[] { "tour/" + tour.Id.ToString(), tour.Title });

            page.Owner.ParseBreadCrumbs(tourPath);
        }

        public static List<Tour> GetAll(Core core, Musician owner)
        {
            List<Tour> tours = new List<Tour>();

            SelectQuery query = Tour.GetSelectQueryStub(typeof(Tour));
            query.AddCondition("musician_id", owner.Id);

            DataTable tourTable = core.db.Query(query);

            foreach (DataRow dr in tourTable.Rows)
            {
                tours.Add(new Tour(core, dr));
            }

            return tours;
        }
    }

    public class InvalidTourException : Exception
    {
    }
}
