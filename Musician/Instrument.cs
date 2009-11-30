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
    [DataTable("music_instruments")]
    public class Instrument : NumberedItem
    {
        [DataField("instrument_id", DataFieldKeys.Primary)]
        private long instrumentId;
        [DataField("instrument_name")]
        private string instrumentName;
        [DataField("instrument_musicians")]
        private long instrumentMusicians;

        public long InstrumentId
        {
            get
            {
                return instrumentId;
            }
        }

        public string Name
        {
            get
            {
                return instrumentName;
            }
        }

        public long Musicians
        {
            get
            {
                return instrumentMusicians;
            }
        }

        public Instrument(Core core, long instrumentId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Instrument_ItemLoad);

            try
            {
                LoadItem(instrumentId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidInstrumentException();
            }
        }

        public Instrument(Core core, DataRow instrumentRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Instrument_ItemLoad);

            try
            {
                loadItemInfo(instrumentRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidInstrumentException();
            }
        }

        void Instrument_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return instrumentId;
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

    public class InvalidInstrumentException : Exception
    {
    }
}
