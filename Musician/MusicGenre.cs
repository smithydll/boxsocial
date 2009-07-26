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
