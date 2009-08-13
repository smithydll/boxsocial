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

        public Gig convertToTrack(Item input)
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

        #region IRateableItem Members

        public float Rating
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region ICommentableItem Members

        public long Comments
        {
            get { throw new NotImplementedException(); }
        }

        public SortOrder CommentSortOrder
        {
            get { throw new NotImplementedException(); }
        }

        public byte CommentsPerPage
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }

    public class InvalidReleaseException : Exception
    {
    }
}
