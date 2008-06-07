/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
    public abstract class Primitive : Item
    {

        protected Primitive(Core core) : base(core)
        {
        }

        public override abstract long Id
        {
            get;
        }

        public abstract string Key
        {
            get;
        }

        public abstract string Type
        {
            get;
        }

        public override abstract string Namespace
        {
            get;
        }

        public abstract AppPrimitives AppPrimitive
        {
            get;
        }

        public override abstract string Uri
        {
            get;
        }

        public abstract string TitleName
        {
            get;
        }

        public abstract string TitleNameOwnership
        {
            get;
        }

        public abstract string DisplayName
        {
            get;
        }

        public abstract string DisplayNameOwnership
        {
            get;
        }

        public abstract bool CanModerateComments(User member);

        public abstract bool IsCommentOwner(User member);

        public abstract ushort GetAccessLevel(User viewer);

        public abstract void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange);

        public bool GetCanRead(ushort accessBits, User viewer)
        {
            bool canRead, canComment, canCreate, canChange;
            GetCan(accessBits, viewer, out canRead, out canComment, out canCreate, out canChange);
            return canRead;
        }

        public bool GetCanComment(ushort accessBits, User viewer)
        {
            bool canRead, canComment, canCreate, canChange;
            GetCan(accessBits, viewer, out canRead, out canComment, out canCreate, out canChange);
            return canComment;
        }

        public bool GetCanCreate(ushort accessBits, User viewer)
        {
            bool canRead, canComment, canCreate, canChange;
            GetCan(accessBits, viewer, out canRead, out canComment, out canCreate, out canChange);
            return canCreate;
        }

        public bool GetCanChange(ushort accessBits, User viewer)
        {
            bool canRead, canComment, canCreate, canChange;
            GetCan(accessBits, viewer, out canRead, out canComment, out canCreate, out canChange);
            return canChange;
        }

        public void ParseBreadCrumbs(string path)
        {
            ParseBreadCrumbs("BREADCRUMBS", path);
        }

        public void ParseBreadCrumbs(string templateVar, string path)
        {
            ParseBreadCrumbs(core.template, templateVar, path);
        }

        public void ParseBreadCrumbs(Template template, string templateVar, string path)
        {
            template.ParseRaw(templateVar, GenerateBreadCrumbs(path));
        }

        public void ParseBreadCrumbs(List<string[]> parts)
        {
            ParseBreadCrumbs("BREADCRUMBS", parts);
        }

        public void ParseBreadCrumbs(string templateVar, List<string[]> parts)
        {
            ParseBreadCrumbs(core.template, templateVar, parts);
        }

        public void ParseBreadCrumbs(Template template, string templateVar, List<string[]> parts)
        {
            template.ParseRaw(templateVar, GenerateBreadCrumbs(parts));
        }

        public abstract string GenerateBreadCrumbs(List<string[]> parts);

        public string GenerateBreadCrumbs(string path)
        {
            return GenerateBreadCrumbs(BreadCrumbsFromPath(path));
        }

        protected List<string[]> BreadCrumbsFromPath(string path)
        {
            string[] pathParts = path.Split('/');

            List<string[]> parts = new List<string[]>();

            foreach (string pathPart in pathParts)
            {
                string partTitle = pathPart;
                partTitle.Insert(0, char.ToUpper(partTitle[0]).ToString());
                partTitle.Remove(1, 1);
                parts.Add(new string[] { pathPart, partTitle });
            }

            return parts;
        }
    }
}
