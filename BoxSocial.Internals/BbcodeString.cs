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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;

namespace BoxSocial.Internals
{
    public class BbcodeString
    {
        private Core core;
        private string body;
        private bool enableSmilies;
        private bool enableBbcode;
        int length;

        public string Body
        {
            get
            {
                return body;
            }
            set
            {
                body = value;
            }
        }

        public bool EnableSmilies
        {
            get
            {
                return enableSmilies;
            }
            set
            {
                enableSmilies = value;
            }
        }

        public bool EnableBbcode
        {
            get
            {
                return enableBbcode;
            }
            set
            {
                enableBbcode = value;
            }
        }

        public BbcodeString(Core core, string body)
        {
            this.core = core;
            this.body = body;
            length = 0;
        }

        public int Length
        {
            get
            {
                if (body.Length > 0 && length == 0)
                {
                    length = core.Bbcode.Strip(body).Length;
                }

                return length;
            }
        }
    }
}
