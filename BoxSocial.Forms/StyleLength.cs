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

namespace BoxSocial.Forms
{
    public enum LengthUnits
    {
        mm,
        cm,
        Pixels,
        Percentage,
        Points,
        Em,
    }

    public class StyleLength
    {
        private float length;
        private LengthUnits unit;

        public StyleLength(float length, LengthUnits unit)
        {
            this.length = length;
            this.unit = unit;
        }

        public override string ToString()
        {
            switch (unit)
            {
                case LengthUnits.mm:
                    return string.Format("{0} mm", length);
                case LengthUnits.cm:
                    return string.Format("{0} cm", length);
                case LengthUnits.Pixels:
                    return string.Format("{0} px", length);
                case LengthUnits.Percentage:
                    return string.Format("{0} %", length);
                case LengthUnits.Points:
                    return string.Format("{0} pt", length);
                case LengthUnits.Em:
                    return string.Format("{0} em", length);
                default:
                    return length.ToString();
            }
        }
    }
}
