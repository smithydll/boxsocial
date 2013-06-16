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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public enum ReleaseType : byte
    {
        Demo = 1,
        Single = 2,
        Album = 3,
        EP = 4,
        DVD = 5,
        Compilation = 6,
    }

    [DataTable("music_releases")]
    public class Release : NumberedItem, IRateableItem, ICommentableItem
    {
        [DataField("release_id", DataFieldKeys.Primary)]
        private long releaseId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("release_title", 63)]
        private string releaseTitle;
        [DataField("release_slug", DataFieldKeys.Index, 63)]
        private string releaseSlug;
        [DataField("release_type")]
        private byte releaseType;
        [DataField("release_date_ut")]
        private long releaseDateRaw;
        [DataField("release_cover_art")]
        private long releaseCoverArt;
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
            set
            {
                SetProperty("releaseTitle", value);
            }
        }

        public string Slug
        {
            get
            {
                return releaseSlug;
            }
        }

        public ReleaseType ReleaseType
        {
            get
            {
                return (ReleaseType)releaseType;
            }
            set
            {
                SetProperty("releaseType", (byte)value);
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

        public Release(Core core, string slug)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Release_ItemLoad);

            try
            {
                LoadItem("release_slug", slug);
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

        public static Release Create(Core core, Musician musician, ReleaseType releaseType, string title, long coverId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            string slug = title;
            Navigation.GenerateSlug(title, ref slug);

            slug = Functions.TrimStringToWord(slug);

            // TODO: fix this
            Item item = Item.Create(core, typeof(Release), new FieldValuePair("musician_id", musician.Id),
                new FieldValuePair("release_title", title),
                new FieldValuePair("release_slug", slug),
                new FieldValuePair("release_type", (byte)releaseType),
                new FieldValuePair("release_date_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("release_cover_art", coverId));

            return (Release)item;
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
                switch (ReleaseType)
                {
                    case BoxSocial.Musician.ReleaseType.Demo:
                        return Musician.UriStub + "discography/demo/" + Slug.ToString();
                    case BoxSocial.Musician.ReleaseType.Single:
                        return Musician.UriStub + "discography/single/" + Slug.ToString();
                    case BoxSocial.Musician.ReleaseType.Album:
                        return Musician.UriStub + "discography/album/" + Slug.ToString();
                    case BoxSocial.Musician.ReleaseType.EP:
                        return Musician.UriStub + "discography/ep/" + Slug.ToString();
                    case BoxSocial.Musician.ReleaseType.DVD:
                        return Musician.UriStub + "discography/dvd/" + Slug.ToString();
                    default:
                        return Musician.UriStub + "discography/release/" + Id.ToString();
                }
            }
        }

        public static void ShowDiscography(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewdiscography");

            List<Release> releases = e.Page.Musician.GetReleases();

            foreach (Release release in releases)
            {
                VariableCollection releaseVariableCollection = null;
                switch (release.ReleaseType)
                {
                    case ReleaseType.Album:
                        releaseVariableCollection = e.Template.CreateChild("album_list");
                        break;

                    case ReleaseType.Compilation:
                        releaseVariableCollection = e.Template.CreateChild("compilation_list");
                        break;
                    case ReleaseType.Demo:
                        releaseVariableCollection = e.Template.CreateChild("demo_list");
                        break;
                    case ReleaseType.DVD:
                        releaseVariableCollection = e.Template.CreateChild("dvd_list");
                        break;
                    case ReleaseType.EP:
                        releaseVariableCollection = e.Template.CreateChild("ep_list");
                        break;
                    case ReleaseType.Single:
                        releaseVariableCollection = e.Template.CreateChild("single_list");
                        break;
                    default:
                        // do nothing
                        break;
                }

                if (releaseVariableCollection != null)
                {
                    releaseVariableCollection.Parse("TITLE", release.Title);
                    releaseVariableCollection.Parse("URI", release.Uri);
                }
            }
        }

        public static void Show(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewrelease");

            Release release = null;

            try
            {
                release = new Release(e.Core, e.Slug);
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

        public long Ratings
        {
            get
            {
                return releaseRatings;
            }
        }

        public string Noun
        {
            get
            {
                switch (ReleaseType)
                {
                    case BoxSocial.Musician.ReleaseType.Album:
                        return "album";
                    case BoxSocial.Musician.ReleaseType.Compilation:
                        return "compilation";
                    case BoxSocial.Musician.ReleaseType.Demo:
                        return "demo";
                    case BoxSocial.Musician.ReleaseType.DVD:
                        return "DVD";
                    case BoxSocial.Musician.ReleaseType.EP:
                        return "EP";
                    case BoxSocial.Musician.ReleaseType.Single:
                        return "single";
                    default:
                        return "release";
                }
            }
        }
    }

    public class InvalidReleaseException : Exception
    {
    }
}
