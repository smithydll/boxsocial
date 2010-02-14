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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("application_slugs")]
    public class ApplicationSlug : NumberedItem
    {
        [DataField("slug_id", DataFieldKeys.Primary)]
        private long slugId;
        [DataField("slug_stub", 31)]
        private string slugStub;
        [DataField("slug_slug_ex", 255)]
        private string slugEx;
        [DataField("application_id", DataFieldKeys.Index)]
        private long applicationId;
        [DataField("slug_primitives")]
        private byte slugPrimitives;
        [DataField("slug_updated_ut")]
        private long slugUpdatedTime;

        public long SlugId
        {
            get
            {
                return slugId;
            }
        }

        public ApplicationSlug(Core core, long slugId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationSlug_ItemLoad);

            try
            {
                LoadItem(slugId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationSlugException();
            }
        }

        void ApplicationSlug_ItemLoad()
        {
        }

        public static ApplicationSlug Create(Core core, long applicationId, string slug, string stub, AppPrimitives primitives)
        {
            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ApplicationSlug)));
            iQuery.AddField("slug_stub", stub);
            iQuery.AddField("slug_slug_ex", slug);
            iQuery.AddField("application_id", applicationId);
            iQuery.AddField("slug_primitives", (byte)primitives);
            iQuery.AddField("slug_updated_ut", UnixTime.UnixTimeStamp());

            long slugId = core.Db.Query(iQuery);

            return new ApplicationSlug(core, slugId);
        }

        public static ApplicationSlug Create(Core core, long applicationId, ApplicationSlugInfo slugInfo)
        {
            return Create(core, applicationId, slugInfo.SlugEx, slugInfo.Stub, slugInfo.Primitives);
        }

        public override long Id
        {
            get
            {
                return slugId;
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

    public class InvalidApplicationSlugException : Exception
    {
    }
}
