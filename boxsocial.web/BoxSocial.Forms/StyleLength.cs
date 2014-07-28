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
        Default,
    }

    public class StyleLength
    {
        private float length;
        private LengthUnits unit;

        public float Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        public LengthUnits Unit
        {
            get
            {
                return unit;
            }
            set
            {
                unit = value;
            }
        }

        public StyleLength()
            : this(0F, LengthUnits.Default)
        {
        }

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
                    return string.Format("{0}mm", length);
                case LengthUnits.cm:
                    return string.Format("{0}cm", length);
                case LengthUnits.Pixels:
                    return string.Format("{0}px", length);
                case LengthUnits.Percentage:
                    return string.Format("{0}%", length);
                case LengthUnits.Points:
                    return string.Format("{0}pt", length);
                case LengthUnits.Em:
                    return string.Format("{0}em", length);
                case LengthUnits.Default:
                    return string.Empty;
                default:
                    return length.ToString();
            }
        }
    }
}
