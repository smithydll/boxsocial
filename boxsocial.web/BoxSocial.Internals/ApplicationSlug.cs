/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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
        [DataField("slug_static")]
        private bool slugIsStatic;

        public long SlugId
        {
            get
            {
                return slugId;
            }
        }

        public bool IsStatic
        {
            get
            {
                return slugIsStatic;
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

        public static ApplicationSlug Create(Core core, long applicationId, string slug, string stub, bool isStatic, AppPrimitives primitives, long updatedTime)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ApplicationSlug)));
            iQuery.AddField("slug_stub", stub);
            iQuery.AddField("slug_slug_ex", slug);
            iQuery.AddField("application_id", applicationId);
            iQuery.AddField("slug_primitives", (byte)primitives);
            iQuery.AddField("slug_static", isStatic);
            iQuery.AddField("slug_updated_ut", updatedTime);

            long slugId = core.Db.Query(iQuery);

            return new ApplicationSlug(core, slugId);
        }

        public static ApplicationSlug Create(Core core, long applicationId, string slug, string stub, AppPrimitives primitives)
        {
            return Create(core, applicationId, slug, stub, false, primitives, UnixTime.UnixTimeStamp());
        }

        public static ApplicationSlug Create(Core core, long applicationId, string slug, string stub, bool isStatic)
        {
            return Create(core, applicationId, slug, stub, isStatic, AppPrimitives.None, UnixTime.UnixTimeStamp());
        }

        public static ApplicationSlug Create(Core core, long applicationId, ApplicationSlugInfo slugInfo)
        {
            return Create(core, applicationId, slugInfo.SlugEx, slugInfo.Stub, slugInfo.IsStatic, slugInfo.Primitives, UnixTime.UnixTimeStamp());
        }

        public static ApplicationSlug Create(Core core, long applicationId, ApplicationSlugInfo slugInfo, long updatedTime)
        {
            return Create(core, applicationId, slugInfo.SlugEx, slugInfo.Stub, slugInfo.IsStatic, slugInfo.Primitives, updatedTime);
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
