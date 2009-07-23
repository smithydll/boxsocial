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
    [DataTable("music_song")]
    public class Song : NumberedItem
    {
        [DataField("song_id", DataFieldKeys.Primary)]
        private long songId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("song_recordings")]
        private long recordings;
        [DataField("song_lyrics", MYSQL_TEXT)]
        private string lyrics;
        [DataField("song_license")]
        private byte licenseId;

        private Musician musician;
        private ContentLicense license;

        public long SongId
        {
            get
            {
                return songId;
            }
        }

        public long Recordings
        {
            get
            {
                return recordings;
            }
        }

        public byte LicenseId
        {
            get
            {
                return licenseId;
            }
            set
            {
                SetProperty("licenseId", value);
            }
        }

        public ContentLicense License
        {
            get
            {
                return license;
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

        public Song(Core core, long songId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Song_ItemLoad);

            try
            {
                LoadItem(songId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSongException();
            }
        }

        public Song(Core core, DataRow songRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Song_ItemLoad);

            try
            {
                loadItemInfo(songRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSongException();
            }
        }

        void Song_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return songId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static void Show(Core core, PPage page, long songId)
        {
            page.template.SetTemplate("Musician", "viewsong");
        }
    }

    public class InvalidSongException : Exception
    {
    }
}
