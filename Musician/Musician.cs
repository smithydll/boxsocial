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

namespace Musician
{
    [DataTable("musicians", "MUSIC")]
    public class Musician : Primitive
    {
        [DataField("musician_id")]
        private long musicianId;
        [DataField("musician_name")]
        private string name;
        [DataField("musician_slug")]
        private string slug;

        public Musician(Core core)
            : base(core)
        {
        }

        public override long Id
        {
            get
            {
                return musicianId;
            }
        }

        public override string Key
        {
            get
            {
                return slug;
            }
        }

        public override string Type
        {
            get
            {
                return "MUSIC";
            }
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/music/{0}/account/",
                    Key);
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Musician;
            }
        }

        public override string UriStub
        {
            get
            {
                return string.Format("/music/{0}/",
                    Key);
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return Linker.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(UriStub);
            }
        }

        public override string TitleName
        {
            get
            {
                return name;
            }
        }

        public override string TitleNameOwnership
        {
            get { throw new NotImplementedException(); }
        }

        public override string DisplayName
        {
            get
            {
                return name;
            }
        }

        public override string DisplayNameOwnership
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanModerateComments(User member)
        {
            throw new NotImplementedException();
        }

        public override bool IsCommentOwner(User member)
        {
            throw new NotImplementedException();
        }

        public override ushort GetAccessLevel(User viewer)
        {
            throw new NotImplementedException();
        }

        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            throw new NotImplementedException();
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/music/{0}", Key);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    DisplayName, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    path += "/" + parts[i][0];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path);
                }
            }

            return output;
        }
    }
}
