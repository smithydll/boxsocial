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
using System.Text;
using System.Web;
using Lachlan.Web;

namespace BoxSocial.Internals
{
    public class Feed
    {
        private string feedVerb;
        private Member feedSubject;
        private Primitive feedObject;
        private Application application;

        /*
         * The verb only has to be in the language of the current user
         * 
         * user verb primitive                      David joined the Group phpBB
         * user verb primitive using application    David slapped Lachlan using IRC Actions
         * user verb primitive application          David posted on Lachlan's Guest Book
         * user verb application                    David published a Blog Entry
         * user verb                                David is sleeping
         *                                          David uploaded a Photo
         *                                          David is attending Election 07 Box Social
         *                                          Lachlan posted on your Guest Book
         */

        public string Verb
        {
            get
            {
                return feedVerb;
            }
        }

        public Member Subject
        {
            get
            {
                return feedSubject;
            }
        }

        public Primitive Object
        {
            get
            {
                return feedObject;
            }
        }
    }
}
