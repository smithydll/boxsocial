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
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [XmlRoot("parents")]
    public class ParentTree
    {
        [XmlElement("parent")]
        public List<ParentTreeNode> Nodes;

        public ParentTree()
        {
            Nodes = new List<ParentTreeNode>();
		}
    }

    public class ParentTreeNode
    {
        [XmlElement("title")]
        public string ParentTitle;

        [XmlElement("id")]
        public long ParentId;

        [XmlElement("slug")]
        public string ParentSlug;

        public ParentTreeNode()
        {
            ParentTitle = "";
            ParentSlug = "";
            ParentId = 0;
        }

        public ParentTreeNode(string title, long id)
        {
            ParentTitle = title;
            ParentSlug = "";
            ParentId = id;
        }

        public ParentTreeNode(string title, string slug, long id)
        {
            ParentTitle = title;
            ParentSlug = slug;
            ParentId = id;
        }
    }
}
