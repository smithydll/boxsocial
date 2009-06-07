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
using System.Collections.Generic;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Muscian
{
    public enum RecordinType
    {
        Single,
        Album,
        EP,
        DVD,
        Compilation,
    }

    public class Recording : NumberedItem, IRateableItem, ICommentableItem
    {

        public Recording(Core core)
            : base (core)
        {
        }

        public override long Id
        {
            get { throw new NotImplementedException(); }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
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
}
