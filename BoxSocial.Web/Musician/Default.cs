﻿/*
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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public class Default
    {

        public static void Show(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "music_default");

            e.Template.Parse("U_REGISTER_MUSICIAN", e.Core.Hyperlink.AppendSid("/music/register"));
        }

        public static void ShowChart(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "chart_default");


        }
    }
}
