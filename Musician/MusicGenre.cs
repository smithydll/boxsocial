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
    [DataTable("genres")]
    public class MusicGenre : NumberedItem
    {
        [DataField("genre_id", DataFieldKeys.Primary)]
        private long genreId;
        [DataField("genre_slug", DataFieldKeys.Unique, 31)]
        private string genreSlug;
        [DataField("genre_is_sub")]
        private bool isSubGenre;
        [DataField("parent_id", typeof(MusicGenre))]
        private long parentGenreId;
        [DataField("genre_name", 31)]
        private string name;
        [DataField("genre_musicians")]
        private long musicians;
        [DataField("genre_recordings")]
        private long recordings;

        public long GenreId
        {
            get
            {
                return genreId;
            }
        }

        public bool IsSubGenre
        {
            get
            {
                return isSubGenre;
            }
        }

        public long ParentId
        {
            get
            {
                return parentGenreId;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Slug
        {
            get
            {
                return genreSlug;
            }
        }

        public long Musicians
        {
            get
            {
                return musicians;
            }
        }

        public long Recordings
        {
            get
            {
                return recordings;
            }
        }

        public MusicGenre(Core core, long genreId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MusicGenre_ItemLoad);

            try
            {
                LoadItem(genreId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicGenreException();
            }
        }

        public MusicGenre(Core core, string genreSlug)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MusicGenre_ItemLoad);

            try
            {
                LoadItem("genre_slug", genreSlug);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicGenreException();
            }
        }

        public MusicGenre(Core core, DataRow genreRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MusicGenre_ItemLoad);

            try
            {
                loadItemInfo(genreRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicGenreException();
            }
        }

        void MusicGenre_ItemLoad()
        {
        }

        public static List<MusicGenre> GetGenres(Core core)
        {
            List<MusicGenre> genres = new List<MusicGenre>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicGenre));
            query.AddCondition("genre_is_sub", false);
            query.AddCondition("parent_id", 0);
            query.AddSort(SortOrder.Ascending, "genre_slug");

            DataTable genresDataTable = core.Db.Query(query);

            foreach (DataRow dr in genresDataTable.Rows)
            {
                genres.Add(new MusicGenre(core, dr));
            }

            return genres;
        }

        public static Dictionary<long, MusicGenre> GetGenres(Core core, List<Musician> musicians)
        {
            Dictionary<long, MusicGenre> genres = new Dictionary<long, MusicGenre>();

            if (musicians == null || musicians.Count == 0)
            {
                return genres;
            }

            List<long> genreIds = new List<long>();

            foreach (Musician musician in musicians)
            {
                if (!genreIds.Contains(musician.GenreRaw))
                {
                    genreIds.Add(musician.GenreRaw);
                }

                if (!genreIds.Contains(musician.SubGenreRaw))
                {
                    genreIds.Add(musician.SubGenreRaw);
                }
            }

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicGenre));
            query.AddCondition("genre_id", ConditionEquality.In, genreIds);
            query.AddSort(SortOrder.Ascending, "genre_id");

            DataTable genresDataTable = core.Db.Query(query);

            foreach (DataRow dr in genresDataTable.Rows)
            {
                MusicGenre genre = new MusicGenre(core, dr);
                genres.Add(genre.Id, genre);
            }

            return genres;
        }

        public static List<MusicGenre> GetAllGenres(Core core)
        {
            List<MusicGenre> genres = new List<MusicGenre>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicGenre));
            query.AddCondition("genre_is_sub", false);
            query.AddSort(SortOrder.Ascending, "parent_id");
            query.AddSort(SortOrder.Ascending, "genre_slug");

            DataTable genresDataTable = core.Db.Query(query);

            foreach (DataRow dr in genresDataTable.Rows)
            {
                genres.Add(new MusicGenre(core, dr));
            }

            return genres;
        }

        public static List<MusicGenre> GetAllSubGenres(Core core)
        {
            List<MusicGenre> genres = new List<MusicGenre>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicGenre));
            query.AddCondition("genre_is_sub", true);
            query.AddCondition("parent_id", ConditionEquality.GreaterThan, 0);
            query.AddSort(SortOrder.Ascending, "parent_id");
            query.AddSort(SortOrder.Ascending, "genre_slug");

            DataTable genresDataTable = core.Db.Query(query);

            foreach (DataRow dr in genresDataTable.Rows)
            {
                genres.Add(new MusicGenre(core, dr));
            }

            return genres;
        }

        public List<MusicGenre> GetSubGenres()
        {
            List<MusicGenre> genres = new List<MusicGenre>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicGenre));
            query.AddCondition("genre_is_sub", true);
            query.AddCondition("parent_id", genreId);
            query.AddSort(SortOrder.Ascending, "genre_slug");

            DataTable genresDataTable = core.Db.Query(query);

            foreach (DataRow dr in genresDataTable.Rows)
            {
                genres.Add(new MusicGenre(core, dr));
            }

            return genres;
        }

        public List<Musician> GetMusicians()
        {
            return Musician.GetMusicians(core, null, this, -1);
        }

        public List<Musician> GetMusicians(string firstLetter, int page)
        {
            return Musician.GetMusicians(core, firstLetter, this, page);
        }

        public override long Id
        {
            get
            {
                return genreId;
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

    public class InvalidMusicGenreException : Exception
    {
    }
}
