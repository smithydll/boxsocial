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
        [DataField("song_title", 31)]
        private string title;
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

        public long MusicianId
        {
            get
            {
                return musicianId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
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

        public string Lyrics
        {
            get
            {
                return lyrics;
            }
            set
            {
                SetProperty("lyrics", value);
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
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.TypeString != musician.Type)
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

        public Song(Core core, Musician musician, DataRow songRow)
            : base(core)
        {
            this.musician = musician;
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

        public List<Recording> GetRecordings()
        {
            return getSubItems(typeof(Recording), true).ConvertAll<Recording>(new Converter<Item, Recording>(convertToRecording));
        }

        public List<Recording> GetRecordingsByMusician()
        {
            List<Recording> recordings = new List<Recording>();

            SelectQuery query = Recording.GetSelectQueryStub(typeof(Recording));
            query.AddCondition("song_id", SongId);
            query.AddCondition("musician_id", MusicianId);

            DataTable recordingsDataTable = db.Query(query);

            foreach (DataRow dr in recordingsDataTable.Rows)
            {
                recordings.Add(new Recording(core, dr));
            }

            return recordings;
        }

        public Recording convertToRecording(Item input)
        {
            return (Recording)input;
        }
        public static Song Create(Core core, Musician owner, string title, string lyrics, byte licenseId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (owner.IsMusicianMember(core.Session.LoggedInMember))
            {
                Item item = Item.Create(core, typeof(Song), new FieldValuePair("musician_id", owner.Id),
                    new FieldValuePair("song_title", title),
                    new FieldValuePair("song_lyrics", lyrics),
                    new FieldValuePair("song_license", licenseId));

                return (Song)item;
            }
            else
            {
                throw new UnauthorisedToCreateItemException();
            }
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
                return Musician.UriStub + "songs/" + Id.ToString();
            }
        }

        public static void Show(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewsong");
            
            Song song = null;

            try
            {
                song = new Song(e.Core, e.ItemId);
            }
            catch
            {
                e.Core.Functions.Generate404();
                return;
            }

            e.Template.Parse("TITLE", song.Title);
            e.Template.Parse("LYRICS", song.Lyrics);

            List<Recording> recordings = song.GetRecordings();

            foreach (Recording recording in recordings)
            {
                VariableCollection recordingVariableCollection = e.Template.CreateChild("recording_list");

                recordingVariableCollection.Parse("RECORDING_ID", recording.Id);
                recordingVariableCollection.Parse("RECORDING_LOCATION", recording.RecordingLocation);

                if (recording.MusicianId != song.Musician.Id)
                {
                    recordingVariableCollection.Parse("IS_COVER", "TRUE");
                    recordingVariableCollection.Parse("RECORDING_MUSICIAN_NAME", recording.Musician.DisplayName);
                    recordingVariableCollection.Parse("RECORDING_MUSICIAN_ID", recording.Musician.Id);
                    recordingVariableCollection.Parse("RECORDING_MUSICIAN_URI", recording.Musician.Uri);
                }
            }

            if (e.Page.Musician.Access.Can("COMMENT_SONGS"))
            {
                e.Template.Parse("CAN_COMMENT", "TRUE");
            }
        }
    }

    public class InvalidSongException : Exception
    {
    }
}
