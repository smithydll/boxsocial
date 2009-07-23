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

        public static Tour Create(Core core, Musician owner, string title, short year)
        {
            // TODO: fix this
            Item item = Item.Create(core, typeof(Tour), new FieldValuePair("musician_id", owner.Id),
                new FieldValuePair("tour_title", title),
                new FieldValuePair("tour_year", year));

            return (Tour)item;
        }

        public static void Show(Core core, PPage page, long tourId)
        {
            page.template.SetTemplate("Musician", "viewtour");
        }
    }

    public class InvalidTourException : Exception
    {
    }
}
