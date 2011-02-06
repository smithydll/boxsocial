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
    public enum RecordingType : byte
    {
        Demo = 0x01,
        Studio = 0x02,
        Live = 0x03,
        Mix = 0x04,
        LiveVideo = 0x05,
        MusicVideo = 0x06,
    }

    public enum RecordingFormat : byte
    {
        NONE = 0x00,
        WAV = 0x01,
        MP3 = 0x02,
        WMA = 0x03,
        AAC = 0x04,
        OGG = 0x05,
        FLAC = 0x06,
        MIDI = 0x07,
        MPEG = 0x08,
        MPEG2 = 0x09,
        WMV = 0x0A,
        MOV = 0x0B,
        VC1 = 0x0C,
        H263 = 0x0D,
        H264 = 0x0E,
        WEBM = 0x0F,
    }

    [DataTable("music_recording")]
    public class Recording : NumberedItem
    {
        [DataField("recording_id", DataFieldKeys.Primary)]
        private long recordingId;
        [DataField("musician_id", typeof(Musician))]
        private long musicianId;
        [DataField("song_id", typeof(Song))]
        private long songId;
        [DataField("release_id", typeof(Release))]
        private long releaseId;
        [DataField("recording_location", 63)]
        private string recordingLocation;
        [DataField("recording_storage_path", 128)]
        private string storagePath;
        [DataField("recording_type")]
        private byte recordingType;
        [DataField("recording_format")]
        private byte recordingFormat;
        [DataField("recording_lyrics", MYSQL_TEXT)]
        private string specialLyrics;
        [DataField("recording_remastered")]
        private bool remastered;

        private Musician musician;

        public long MusicianId
        {
            get
            {
                return musicianId;
            }
        }

        public long SongId
        {
            get
            {
                return songId;
            }
        }

        public long ReleaseId
        {
            get
            {
                return releaseId;
            }
        }

        public string SpecialLyrics
        {
            get
            {
                return specialLyrics;
            }
            set
            {
                SetProperty("specialLyrics", value);
            }
        }

        public string RecordingLocation
        {
            get
            {
                return recordingLocation;
            }
            set
            {
                SetProperty(recordingLocation, value);
            }
        }

        public string StoragePath
        {
            get
            {
                return storagePath;
            }
        }

        public RecordingType Type
        {
            get
            {
                return (RecordingType)recordingType;
            }
            set
            {
                SetProperty("recordingType", (byte)value);
            }
        }

        public RecordingFormat Format
        {
            get
            {
                return (RecordingFormat)recordingFormat;
            }
            set
            {
                SetProperty("recordingFormat", (byte)value);
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

        public Recording(Core core, long recordingId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Recording_ItemLoad);

            try
            {
                LoadItem(recordingId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidRecordingException();
            }
        }

        public Recording(Core core, DataRow recordingRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Recording_ItemLoad);

            try
            {
                loadItemInfo(recordingRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidRecordingException();
            }
        }

        void Recording_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return recordingId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static void Show(Core core, PPage page, long recordingId)
        {
            page.template.SetTemplate("Musician", "viewrecording");
        }
    }

    public class InvalidRecordingException : Exception
    {
    }
}
