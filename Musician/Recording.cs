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
    public enum RecordingType
    {
        Demo,
        Studio,
        Live,
        Mix,
        LiveVideo,
        MusicVideo,
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

        private Musician musician;

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
