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
    public enum ReleaseType
    {
        Demo = 1,
        Single = 2,
        Album = 3,
        EP = 4,
        DVD = 5,
        Compilation = 6,
    }

    public class Release : NumberedItem, IRateableItem, ICommentableItem
    {
        [DataField("release_id", DataFieldKeys.Primary)]
        private long releaseId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("release_title", 63)]
        private string releaseTitle;
        [DataField("release_date_ut")]
        private long releaseDateRaw;
        [DataField("release_cover_art", 63)]
        private long releaseCoverArt;
        [DataField("release_comments")]
        private long releaseComments;
        [DataField("release_rating")]
        private float releaseRating;
        [DataField("release_ratings")]
        private long releaseRatings;

        private Musician musician;

        public long ReleaseId
        {
            get
            {
                return releaseId;
            }
        }

        public string Title
        {
            get
            {
                return releaseTitle;
            }
        }

        public long ReleaseDateRaw
        {
            get
            {
                return releaseDateRaw;
            }
            set
            {
                SetProperty("releaseDateRaw", value);
            }
        }

        public DateTime GetReleaseDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(releaseDateRaw);
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

        public Release(Core core, long releaseId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Release_ItemLoad);

            try
            {
                LoadItem(releaseId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidReleaseException();
            }
        }

        public Release(Core core, DataRow releaseRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Release_ItemLoad);

            try
            {
                loadItemInfo(releaseRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidReleaseException();
            }
        }

        void Release_ItemLoad()
        {
        }

        public List<Track> GetTracks()
        {
            return getSubItems(typeof(Track), true).ConvertAll<Track>(new Converter<Item, Track>(convertToTrack));
        }

        public Track convertToTrack(Item input)
        {
            return (Track)input;
        }

        public override long Id
        {
            get
            {
                return releaseId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static void Show(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewrelease");

            Release release = null;

            try
            {
                release = new Release(e.Core, e.ItemId);
            }
            catch
            {
                e.Core.Functions.Generate404();
                return;
            }

            List<Track> tracks = release.GetTracks();

            foreach (Track track in tracks)
            {
                VariableCollection trackVariableCollection = e.Template.CreateChild("track_list");
                

            }

            e.Core.Display.DisplayComments(e.Template, release.Musician, release);
        }

        public float Rating
        {
            get
            {
                return releaseRating;
            }
        }

        public long Comments
        {
            get
            {
                return releaseComments;
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

        public long Ratings
        {
            get
            {
                return releaseRatings;
            }
        }
    }

    public class InvalidReleaseException : Exception
    {
    }
}